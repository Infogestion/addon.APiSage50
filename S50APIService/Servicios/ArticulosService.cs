using System.Collections.Generic;
using System.Linq;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// Artículos (tabla articulo del ejercicio). Los listados paginados no llevan ORDER BY, como en interface.s50c: las
    /// condiciones de <see cref="Select(string, int, int, string)"/> y <see cref="SelectWithStockByWarehouse"/> están escritas
    /// como las genera EF para que SQL Server devuelva las mismas páginas.
    /// </summary>
    internal static class ArticulosService
    {
        private const string Tabla = "{articulo} AS [a]";

        public static ListaJson Select(string year, int page, int pagesize)
        {
            return Db.Lector.LeerEjercicioJson<Articulo>(year, Articulos().Pagina(page, pagesize));
        }

        public static Articulo Select(string year, string id)
        {
            return Db.Lector.LeerEjercicio<Articulo>(year, Articulos(new Filtro { ["a.CODIGO"] = id })).FirstOrDefault();
        }

        /// <summary>Los artículos cuyo código o nombre contiene el texto, sin distinguir mayúsculas.</summary>
        public static ListaJson Select(string year, int page, int pagesize, string codigo_nombre)
        {
            var filtro = new Filtro().Sql(
                "(({0} LIKE '') OR (CHARINDEX({0}, LOWER([a].[CODIGO])) > 0)) OR (({0} LIKE '') OR (CHARINDEX({0}, LOWER([a].[NOMBRE])) > 0))",
                codigo_nombre.ToLower());
            return Db.Lector.LeerEjercicioJson<Articulo>(year, Articulos(filtro).Pagina(page, pagesize));
        }

        /// <summary>Los artículos con su stock en un almacén (0 si no tienen ninguna fila de stock).</summary>
        public static ListaJson SelectWithStockByWarehouse(string year, int page, int pagesize, string warehouse)
        {
            const string sinStock = "(((([s].[EMPRESA] IS NULL) OR ([s].[ARTICULO] IS NULL)) OR ([s].[TALLA] IS NULL)) OR ([s].[COLOR] IS NULL)) OR ([s].[ALMACEN] IS NULL)";
            return Db.Lector.LeerEjercicioJson<ArticuloWithStock>(year, new Consulta
            {
                Origen = Tabla + " LEFT JOIN {stocks2} AS [s] ON [a].[CODIGO] = [s].[ARTICULO]",
                Alias = "a",
                Expresiones = new Dictionary<string, string> { ["STOCKBYALMACEN"] = Stocks2Service.SiHayStock("[s].[FINAL]", "0.0") },
                Filtro = new Filtro().Sql("(" + sinStock + ") OR (LTRIM(RTRIM([s].[ALMACEN])) = {0})", warehouse.Trim()),
            }.Pagina(page, pagesize));
        }

        public static ListaJson SelectByBarras(string year, string codigo)
        {
            return Db.Lector.LeerEjercicioJson<Articulo>(year, new Consulta
            {
                Origen = Tabla + " INNER JOIN {barras} AS [b] ON [a].[CODIGO] = [b].[ARTICULO]",
                Alias = "a",
                Filtro = new Filtro().Exacto("b.BARRAS", codigo),
            });
        }

        /// <summary>
        /// Los artículos cuyo nombre contiene el texto; si no hay ninguno, los que tienen una referencia de proveedor que lo
        /// contiene (uno por cada referencia).
        /// </summary>
        public static ListaJson SelectSearch(string year, string query, int pageNumber, int pageSize)
        {
            string texto = query.Trim();
            var porNombre = Db.Lector.LeerEjercicioJson<Articulo>(year,
                Articulos(new Filtro().Contiene("a.NOMBRE", texto)).Pagina(pageNumber, pageSize));
            if (!porNombre.Vacia)
                return porNombre;

            return Db.Lector.LeerEjercicioJson<Articulo>(year, new Consulta
            {
                Origen = "{referpro} AS [r] INNER JOIN " + Tabla + " ON LTRIM(RTRIM([r].[ARTICULO])) = LTRIM(RTRIM([a].[CODIGO]))",
                Alias = "a",
                Filtro = new Filtro().Contiene("r.PROVEEDOR", texto),
            }.Pagina(pageNumber, pageSize));
        }

        private static Consulta Articulos(Filtro filtro = null)
        {
            return new Consulta { Origen = Tabla, Alias = "a", Filtro = filtro };
        }
    }
}
