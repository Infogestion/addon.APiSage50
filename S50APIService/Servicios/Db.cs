using System;
using System.Net;
using S50APIService.Api;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Lo que comparten los servicios para leer de Sage.</summary>
    internal static class Db
    {
        public const string Comunes = "COMUNES";

        public static LectorSage Lector => Contexto.Lector;

        /// <summary>
        /// El ejercicio de las rutas que no llevan {year} pero leen tablas del ejercicio (vendedores, proveedores, clientes,
        /// documentos de mercancías): el ejercicio activo de Sage, que Sage conoce como "GESTION". interface.s50c usaba el
        /// año del calendario.
        /// </summary>
        public const string EjercicioActual = "GESTION";

        /// <summary>El UrlDecode() que interface.s50c vuelve a aplicar a algunos valores de la ruta: un "+" pasa a ser un espacio.</summary>
        public static string UrlDecode(this string valorDeRuta) => WebUtility.UrlDecode(valorDeRuta);
    }
}
