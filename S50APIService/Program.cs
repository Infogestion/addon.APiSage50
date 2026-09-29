using System;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
using S50APIService.Http;
using S50APIService.Sage;
using S50APIService.Seguridad;

namespace S50APIService
{
    /// <summary>
    /// API de Sage 50 sin interfaz gráfica. De momento se ejecuta como aplicación de consola (Ctrl+C para parar);
    /// más adelante se instalará como servicio de Windows.
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
            var jwt = new ServicioJwt(cfg["JwtClave"], TimeSpan.FromHours(int.Parse(cfg["JwtHorasValidez"])));

            Console.WriteLine($"S50APIService · terminal {terminal} · grupo {grupo} · empresa {empresa}");
            if (jwt.ClaveTemporal)
                Console.WriteLine("AVISO: no hay JwtClave en App.config; se usa una clave temporal y los tokens dejarán de valer al reiniciar.");
            using (var sesion = new SesionSage(terminal, cfg["SageLibrerias"]))
            {
                // Si Sage no conecta, la API arranca igualmente y /api/salud informa del error.
                string errorConexion = "";
                var sw = Stopwatch.StartNew();
                try
                {
                    Console.WriteLine($"Conectando con Sage ({sesion.CarpetaLibrerias})...");
                    sesion.Conectar(terminal, grupo, empresa, timeout);
                    Console.WriteLine($"Sage conectado en {sw.Elapsed.TotalSeconds:0.0} s");
                }
                catch (Exception ex)
                {
                    errorConexion = ex.Message;
                    Console.WriteLine("ERROR al conectar con Sage: " + errorConexion);
                }

                using (var servidor = new ServidorHttp(url, sesion, () => errorConexion, jwt))
                using (var parar = new ManualResetEventSlim())
                {
                    servidor.Iniciar();
                    Console.WriteLine($"API escuchando en {url} (prueba: {url}api/salud). Ctrl+C para parar.");
                    Console.CancelKeyPress += (s, e) => { e.Cancel = true; parar.Set(); };
                    parar.Wait();
                }
                Console.WriteLine("Parando...");
            }
            return 0;
        }
    }
}
