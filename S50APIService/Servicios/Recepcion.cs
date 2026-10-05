using System;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// Las dos clases de recepción de mercancías del addon FERRETERIATIA: con envíos y sin envíos. Funcionan igual y solo
    /// cambian las tablas, la columna del operario y la clase de negocio de Sage; por eso cada servicio se escribe una vez
    /// y recibe la suya (en interface.s50c son dos servicios copiados por cada clase).
    /// </summary>
    internal sealed class Recepcion
    {
        public const string Addon = "FERRETERIATIA";

        public static readonly Recepcion ConEnvios = new Recepcion("c_repmerc", "d_repmerc", "OPERARIO", "DocReposicionMerc");
        public static readonly Recepcion SinEnvios = new Recepcion("c_repart", "d_repart", "OPERADOR", "ReposicionArt");

        public string Cabeceras { get; }
        public string Lineas { get; }
        public string ColumnaOperario { get; }
        private readonly string _claseSage;

        private Recepcion(string cabeceras, string lineas, string columnaOperario, string claseSage)
        {
            Cabeceras = cabeceras;
            Lineas = lineas;
            ColumnaOperario = columnaOperario;
            _claseSage = claseSage;
        }

        /// <summary>El filtro de un documento, para su cabecera o para sus líneas.</summary>
        public static Filtro Documento(string numero, string ejercicio, string empresa)
        {
            return new Filtro
            {
                ["NUMERO"] = numero,
                ["EMPRESA"] = empresa,
                ["EJERCICIO"] = ejercicio,
            };
        }

        /// <summary>Lo que se necesita de la cabecera para guardar el documento; null si no existe.</summary>
        public CabeceraRecepcion Cabecera(string numero, string ejercicio, string empresa)
        {
            var cabeceras = Db.Lector.LeerEjercicio<CabeceraRecepcion>(Addon, Cabeceras, Documento(numero, ejercicio, empresa));
            return cabeceras.Count == 0 ? null : cabeceras[0];
        }

        /// <summary>Los cambios de un documento, vacíos: se rellenan y se guardan con <see cref="Guardar"/>.</summary>
        public CambiosDocumento Cambios(CabeceraRecepcion cabecera)
        {
            return new CambiosDocumento
            {
                Libreria = "sage.addons.FerreteriaTia",
                Clase = "sage.addons.FerreteriaTia.Negocio.Documentos." + _claseSage,
                Ejercicio = cabecera.Ejercicio.Trim(),
                Empresa = cabecera.Empresa.Trim(),
                Numero = cabecera.Numero.Trim(),
            };
        }

        /// <summary>
        /// Guarda el documento con su clase de negocio de Sage. Si Sage no lo guarda (p. ej. porque alguien lo tiene abierto)
        /// devuelve false y deja el motivo en la consola, porque interface.s50c solo responde true o false.
        /// </summary>
        public bool Guardar(CambiosDocumento cambios)
        {
            string motivo = Contexto.Escritor.GuardarDocumento(cambios);
            if (motivo != null)
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} No se ha guardado la recepción {cambios.Ejercicio}/{cambios.Empresa}/{cambios.Numero}: {motivo}");
            return motivo == null;
        }
    }
}
