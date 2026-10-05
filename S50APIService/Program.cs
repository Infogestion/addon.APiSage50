using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Owin.Hosting;
using S50APIService.Api;
using S50APIService.Sage;
using S50APIService.Seguridad;

namespace S50APIService
{
    /// <summary>
    /// Réplica de interface.s50c que trabaja con las librerías de Sage 50, sin interfaz gráfica.
    /// De momento se ejecuta como aplicación de consola (Ctrl+C para parar); más adelante se instalará como servicio de Windows.
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            var cfg = ConfigurationManager.AppSettings;
            string url = cfg["Url"];
            string terminal = cfg["SageTerminal"];
            string grupo = cfg["SageGrupo"];
            string empresa = cfg["SageEmpresa"];
            var timeout = TimeSpan.FromSeconds(int.Parse(cfg["SageTimeoutSegundos"]));

            Contexto.Jwt = new ServicioJwt(cfg["JwtClave"]);
            Contexto.Usuarios = new UsuariosApi(cfg);
            Contexto.AddonSga = cfg["SageAddonSga"];

            Console.WriteLine($"S50APIService · terminal {terminal} · grupo {grupo} · empresa {empresa}");
            if (Contexto.Jwt.ClaveTemporal)
                Console.WriteLine("AVISO: no hay JwtClave configurada; se usa una clave temporal y los tokens dejarán de valer al reiniciar.");
            if (Contexto.Usuarios.Cantidad == 0)
                Console.WriteLine("AVISO: no hay usuarios de la API configurados (ApiUsuario:...); POST /api/token siempre responderá 401.");

            using (var sesion = new SesionSage(terminal, cfg["SageLibrerias"]))
            {
                Contexto.Sesion = sesion;
                Contexto.Lector = new LectorSage(sesion, timeout);
                Contexto.Escritor = new EscritorSage(sesion, timeout);
                Contexto.UsuariosNadilux = new UsuariosNadilux(sesion, timeout);
                var sw = Stopwatch.StartNew();
                try
                {
                    Console.WriteLine($"Conectando con Sage ({sesion.CarpetaLibrerias})...");
                    sesion.Conectar(terminal, grupo, empresa, timeout);
                    var estado = sesion.Ejecutar(t => t.Estado(), timeout, "estado");
                    Console.WriteLine($"Sage conectado en {sw.Elapsed.TotalSeconds:0.0} s · "
                        + string.Join(" · ", estado.Select(kv => kv.Key + " " + kv.Value)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR al conectar con Sage: " + ex.Message);
                }

                using (WebApp.Start(url, Arranque.Configurar))
                using (var parar = new ManualResetEventSlim())
                {
                    Console.WriteLine($"API escuchando en {url}. Ctrl+C para parar.");
                    Console.CancelKeyPress += (s, e) => { e.Cancel = true; parar.Set(); };
                    parar.Wait();
                }
                Console.WriteLine("Parando...");
            }
            return 0;
        }
    }
}
