using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Stock de los artículos por almacén (tabla stocks2 del ejercicio).</summary>
    internal static class Stocks2Service
    {
        public static ListaJson Select(string year, string id)
        {
            return Db.Lector.LeerEjercicioJson<Stocks2>(year, "stocks2", new Filtro { ["ARTICULO"] = id });
        }

        public static ListaJson Select(string year, string articulo, string almacen)
        {
            return Db.Lector.LeerEjercicioJson<Stocks2>(year, "stocks2", new Filtro
            {
                ["ARTICULO"] = articulo,
                ["ALMACEN"] = almacen,
            });
        }

        /// <summary>
        /// La expresión SQL de una columna calculada en las consultas que unen articulo con stocks2 (alias [s]) por LEFT JOIN:
        /// vale <paramref name="conStock"/> si el artículo tiene fila de stock y <paramref name="sinStock"/> si no. Es la que
        /// genera EF en interface.s50c.
        /// </summary>
        public static string SiHayStock(string conStock, string sinStock)
        {
            const string hayStock = "(((([s].[EMPRESA] IS NOT NULL) AND ([s].[ARTICULO] IS NOT NULL)) AND ([s].[TALLA] IS NOT NULL)) AND ([s].[COLOR] IS NOT NULL)) AND ([s].[ALMACEN] IS NOT NULL)";
            return "CASE WHEN " + hayStock + " THEN " + conStock + " ELSE " + sinStock + " END";
        }
    }
}
