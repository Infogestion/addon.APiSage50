using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using S50APIService.Sage;
using S50APIService.Seguridad;

namespace S50APIService.Http
{
    /// <summary>Servidor HTTP de la API. Cada petición se atiende en un hilo del ThreadPool; las llamadas a Sage van a su cola.</summary>
    public sealed class ServidorHttp : IDisposable
    {
        private static readonly TimeSpan TimeoutSalud = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan TimeoutLogin = TimeSpan.FromSeconds(30);

        private readonly HttpListener _listener = new HttpListener();
        private readonly SesionSage _sesion;
        private readonly Func<string> _errorConexion;
        private readonly ServicioJwt _jwt;
        private Thread _hilo;

        /// <param name="errorConexion">Devuelve el motivo por el que Sage no está conectado (vacío si lo está).</param>
        public ServidorHttp(string url, SesionSage sesion, Func<string> errorConexion, ServicioJwt jwt)
        {
            _listener.Prefixes.Add(url);
            _sesion = sesion;
            _errorConexion = errorConexion;
            _jwt = jwt;
        }

        public void Iniciar()
        {
            _listener.Start();
            _hilo = new Thread(Escuchar) { Name = "Http", IsBackground = true };
            _hilo.Start();
        }

        private void Escuchar()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext contexto;
                try { contexto = _listener.GetContext(); }
                catch (HttpListenerException) { return; } // Detener() cierra el listener
                catch (ObjectDisposedException) { return; }
                ThreadPool.QueueUserWorkItem(_ => Atender(contexto));
            }
        }

        private void Atender(HttpListenerContext contexto)
        {
            string metodo = contexto.Request.HttpMethod.ToUpperInvariant();
            string ruta = contexto.Request.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();
            try
            {
                // Rutas públicas.
                if (metodo == "GET" && ruta == "/api/salud")
                {
                    Salud(contexto);
                    return;
                }
                if (metodo == "POST" && ruta == "/api/auth/login")
                {
                    Login(contexto);
                    return;
                }

                // El resto de rutas exigen "Authorization: Bearer <token>".
                string usuario = UsuarioDelToken(contexto.Request);
                if (usuario == null)
                {
                    contexto.Response.AddHeader("WWW-Authenticate", "Bearer");
                    Responder(contexto, 401, new { error = "Falta el token o no es válido" });
                    return;
                }

                Responder(contexto, 404, new { error = "No encontrado" });
            }
            catch (Exception ex)
            {
                Responder(contexto, 500, new { error = ex.Message });
            }
        }

        /// <summary>200 si Sage está conectado y 503 si no.</summary>
        private void Salud(HttpListenerContext contexto)
        {
            string error = _errorConexion();
            if (!string.IsNullOrEmpty(error))
            {
                Responder(contexto, 503, new { estado = "error", error });
                return;
            }
            try
            {
                var sage = _sesion.Ejecutar(t => t.Estado(), TimeoutSalud, "salud");
                Responder(contexto, 200, new { estado = "ok", sage });
            }
            catch (Exception ex)
            {
                Responder(contexto, 503, new { estado = "error", error = ex.Message });
            }
        }

        /// <summary>Cuerpo: {"usuario": "...", "password": "..."} con un usuario de Sage. 200 con el token o 401.</summary>
        private void Login(HttpListenerContext contexto)
        {
            string errorConexion = _errorConexion();
            if (!string.IsNullOrEmpty(errorConexion))
            {
                Responder(contexto, 503, new { error = "Sage no está conectado: " + errorConexion });
                return;
            }

            SolicitudLogin solicitud;
            try
            {
                using (var lector = new StreamReader(contexto.Request.InputStream, Encoding.UTF8))
                    solicitud = JsonConvert.DeserializeObject<SolicitudLogin>(lector.ReadToEnd());
            }
            catch (JsonException)
            {
                solicitud = null;
            }
            if (string.IsNullOrWhiteSpace(solicitud?.Usuario) || solicitud.Password == null)
            {
                Responder(contexto, 400, new { error = "El cuerpo debe ser {\"usuario\": \"...\", \"password\": \"...\"}" });
                return;
            }

            if (!_sesion.Ejecutar(t => t.ValidarUsuario(solicitud.Usuario, solicitud.Password), TimeoutLogin, "login"))
            {
                // Mismo mensaje si no existe el usuario o si falla la contraseña, para no revelar qué usuarios existen.
                Responder(contexto, 401, new { error = "Usuario o contraseña incorrectos" });
                return;
            }

            string token = _jwt.GenerarToken(solicitud.Usuario.Trim().ToUpperInvariant(), out DateTime expira);
            Responder(contexto, 200, new { token, expira });
        }

        private sealed class SolicitudLogin
        {
            public string Usuario { get; set; }
            public string Password { get; set; }
        }

        private string UsuarioDelToken(HttpListenerRequest peticion)
        {
            string cabecera = peticion.Headers["Authorization"];
            const string prefijo = "Bearer ";
            if (cabecera == null || !cabecera.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                return null;
            return _jwt.Validar(cabecera.Substring(prefijo.Length).Trim());
        }

        private static void Responder(HttpListenerContext contexto, int codigo, object cuerpo)
        {
            byte[] datos = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cuerpo, Formatting.Indented));
            contexto.Response.StatusCode = codigo;
            contexto.Response.ContentType = "application/json; charset=utf-8";
            contexto.Response.ContentLength64 = datos.Length;
            contexto.Response.OutputStream.Write(datos, 0, datos.Length);
            contexto.Response.OutputStream.Close();
        }

        public void Dispose()
        {
            if (_listener.IsListening)
                _listener.Stop();
            _listener.Close();
        }
    }
}
