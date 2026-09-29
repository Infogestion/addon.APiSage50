using System;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using S50APIService.Sage;

namespace S50APIService.Http
{
    /// <summary>Servidor HTTP de la API. Cada petición se atiende en un hilo del ThreadPool; las llamadas a Sage van a su cola.</summary>
    public sealed class ServidorHttp : IDisposable
    {
        private static readonly TimeSpan TimeoutSalud = TimeSpan.FromSeconds(10);

        private readonly HttpListener _listener = new HttpListener();
        private readonly SesionSage _sesion;
        private readonly Func<string> _errorConexion;
        private Thread _hilo;

        /// <param name="errorConexion">Devuelve el motivo por el que Sage no está conectado (vacío si lo está).</param>
        public ServidorHttp(string url, SesionSage sesion, Func<string> errorConexion)
        {
            _listener.Prefixes.Add(url);
            _sesion = sesion;
            _errorConexion = errorConexion;
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
                if (metodo == "GET" && ruta == "/api/salud")
                    Salud(contexto);
                else
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
