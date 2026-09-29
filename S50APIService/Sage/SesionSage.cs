using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace S50APIService.Sage
{
    /// <summary>
    /// Una sesión de Sage: un AppDomain propio con sus librerías y un único hilo STA que ejecuta,
    /// una detrás de otra, todas las llamadas.
    /// - Sage guarda su estado en variables static (conexión, usuario, empresa), que son por AppDomain.
    /// - Sus objetos son de WinForms/COM y esperan que siempre los use el mismo hilo, en modo STA.
    /// Las peticiones HTTP llegan en paralelo y se ponen en cola.
    /// </summary>
    public sealed class SesionSage : IDisposable
    {
        private readonly BlockingCollection<Action> _cola = new BlockingCollection<Action>();
        private readonly Thread _hilo;
        private AppDomain _dominio;
        private TrabajadorSage _trabajador;

        public string CarpetaLibrerias { get; }
        public bool Conectada => _trabajador != null;

        public SesionSage(string terminal, string carpetaLibrerias)
        {
            CarpetaLibrerias = string.IsNullOrWhiteSpace(carpetaLibrerias) ? BuscarLibrerias(terminal) : carpetaLibrerias;
            _hilo = new Thread(Bucle) { Name = "Sage", IsBackground = true };
            _hilo.SetApartmentState(ApartmentState.STA);
            _hilo.Start();
        }

        /// <summary>Crea el AppDomain de Sage y conecta con el grupo y la empresa.</summary>
        public void Conectar(string terminal, string grupo, string empresa, TimeSpan timeout)
        {
            Ejecutar(() =>
            {
                var setup = new AppDomainSetup
                {
                    ApplicationBase = CarpetaLibrerias,
                    ConfigurationFile = Path.Combine(CarpetaLibrerias, "sage.50.exe.config"),
                };
                _dominio = AppDomain.CreateDomain("Sage50", null, setup);
                var trabajador = (TrabajadorSage)_dominio.CreateInstanceFromAndUnwrap(
                    Assembly.GetExecutingAssembly().Location, typeof(TrabajadorSage).FullName);
                trabajador.Conectar(terminal, grupo, empresa);
                _trabajador = trabajador;
                return 0;
            }, timeout, "conectar");
        }

        /// <summary>Ejecuta una operación en el hilo de Sage y espera el resultado como mucho <paramref name="timeout"/>.</summary>
        public T Ejecutar<T>(Func<TrabajadorSage, T> operacion, TimeSpan timeout, string nombre)
        {
            if (_trabajador == null)
                throw new InvalidOperationException("La sesión de Sage no está conectada.");
            return Ejecutar(() => operacion(_trabajador), timeout, nombre);
        }

        private T Ejecutar<T>(Func<T> funcion, TimeSpan timeout, string nombre)
        {
            T resultado = default;
            Exception error = null;
            using (var hecho = new ManualResetEventSlim())
            {
                _cola.Add(() =>
                {
                    try { resultado = funcion(); }
                    catch (Exception ex) { error = ex; }
                    finally { hecho.Set(); }
                });
                if (!hecho.Wait(timeout))
                    throw new TimeoutException($"Sage no ha respondido a '{nombre}' en {timeout.TotalSeconds:0} s (¿un diálogo modal?).");
            }
            if (error != null)
                throw error;
            return resultado;
        }

        private void Bucle()
        {
            foreach (var accion in _cola.GetConsumingEnumerable())
                accion();
        }

        /// <summary>Descarga el AppDomain: equivale a cerrar Sage sin cerrar el servicio.</summary>
        public void Dispose()
        {
            _cola.CompleteAdding();
            _trabajador = null;
            if (_dominio != null)
            {
                AppDomain.Unload(_dominio);
                _dominio = null;
            }
        }

        /// <summary>Igual que sage.50.exe: la subcarpeta de versión más alta del terminal que contenga sage.50.exe.</summary>
        private static string BuscarLibrerias(string terminal)
        {
            var carpeta = Directory.GetDirectories(terminal)
                .Select(d => new { Dir = d, Ok = Version.TryParse(Path.GetFileName(d), out var v), Ver = v })
                .Where(x => x.Ok && File.Exists(Path.Combine(x.Dir, "sage.50.exe")))
                .OrderByDescending(x => x.Ver)
                .FirstOrDefault();
            if (carpeta == null)
                throw new DirectoryNotFoundException("No hay librerías de Sage en " + terminal);
            return carpeta.Dir;
        }
    }
}
