using System.Collections.Generic;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// Ubicaciones del almacén (tabla ubicaciones del addon Nadilux SGA, <see cref="Contexto.AddonSga"/>). Si la base de datos
    /// del addon no existe, la respuesta es el mismo 404 que el de un ejercicio inexistente, como en interface.s50c.
    /// </summary>
    internal static class UbicacionService
    {
        public static ListaJson Select(int page, int pagesize)
        {
            return Db.Lector.LeerEjercicioJson<Ubicaciones>(Contexto.AddonSga, new Consulta { Origen = "{ubicaciones}" }.Pagina(page, pagesize));
        }

        public static Ubicaciones SelectByCode(string code)
        {
            return Db.Lector.LeerEjercicio<Ubicaciones>(Contexto.AddonSga, "ubicaciones", new Filtro { ["CODIGO"] = code }).FirstOrDefault();
        }

        /// <summary>Los artículos que hay en una ubicación, con los nombres del artículo, la ubicación y el almacén.</summary>
        public static object SelectByArticuloRel(string year, string code, string emp)
        {
            var relaciones = UbicacionRelService.SelectByCode(code, emp);
            var almacenes = Nombres(year, "almacen");
            var articulos = Nombres(year, "articulo");
            var ubicaciones = Nombres(Contexto.AddonSga, "ubicaciones");

            return relaciones.Select(r => new
            {
                r.Articulo,
                NombreArticulo = Nombre(articulos, r.Articulo),
                r.Linea,
                r.Almacen,
                r.Empresa,
                r.Ubicacion,
                NombreUbicacion = Nombre(ubicaciones, r.Ubicacion),
                NombreAlmacen = Nombre(almacenes, r.Almacen),
            }).ToList();
        }

        /// <summary>Las ubicaciones de un artículo, con los mismos nombres y su stock en el almacén de cada una.</summary>
        public static object SelectAllArticle(string year, string code, string emp)
        {
            var relaciones = UbicacionRelService.SelectByCodeAr(code, emp);
            var almacenes = Nombres(year, "almacen");
            var articulos = Nombres(year, "articulo");
            var stocks = Stocks(year, code);
            var ubicaciones = Nombres(Contexto.AddonSga, "ubicaciones");

            return relaciones.Select(r => new
            {
                r.Articulo,
                NombreArticulo = Nombre(articulos, r.Articulo),
                r.Linea,
                r.Almacen,
                r.Empresa,
                r.Ubicacion,
                NombreUbicacion = Nombre(ubicaciones, r.Ubicacion),
                NombreAlmacen = Nombre(almacenes, r.Almacen),
                Stock = stocks.TryGetValue(r.Almacen, out decimal stock) ? stock : 0,
            }).ToList();
        }

        /// <summary>
        /// Código → nombre de todas las filas de una tabla. Los códigos se buscan tal cual están guardados (con sus espacios),
        /// como los diccionarios de interface.s50c.
        /// </summary>
        private static Dictionary<string, string> Nombres(string baseDatos, string tabla)
        {
            return Db.Lector.LeerEjercicio<CodigoNombre>(baseDatos, tabla).ToDictionary(f => f.Codigo, f => f.Nombre);
        }

        private static string Nombre(Dictionary<string, string> nombres, string codigo)
        {
            return nombres.TryGetValue(codigo, out string nombre) ? nombre : null;
        }

        /// <summary>Almacén → stock del artículo.</summary>
        private static Dictionary<string, decimal> Stocks(string year, string articulo)
        {
            return Db.Lector.LeerEjercicio<StockAlmacen>(year, new Consulta
            {
                Origen = "{articulo} AS [a] LEFT JOIN {stocks2} AS [s] ON [a].[CODIGO] = [s].[ARTICULO]",
                Expresiones = new Dictionary<string, string>
                {
                    ["ALMACEN"] = Stocks2Service.SiHayStock("[s].[ALMACEN]", "''"),
                    ["STOCK"] = Stocks2Service.SiHayStock("[s].[FINAL]", "0.0"),
                },
                Filtro = new Filtro { ["a.CODIGO"] = articulo },
            }).ToDictionary(s => s.Almacen, s => s.Stock);
        }
    }
}
