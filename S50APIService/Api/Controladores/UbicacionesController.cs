using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Api.Controladores
{
    /// <summary>
    /// Las tablas ubicaciones y rel_ubi_alm son del addon Nadilux SGA (<see cref="Contexto.AddonSga"/>). Si su base de datos
    /// no existe, la respuesta es el mismo 404 que el de un ejercicio inexistente, como en interface.s50c.
    /// </summary>
    [RoutePrefix("api/{year:int}/Ubicaciones")]
    [Autorizar]
    public sealed class UbicacionesController : ApiController
    {
        /// <summary>
        /// Permite obtener las ubicaciones de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<Ubicaciones>))]
        public HttpResponseMessage Get(string year, int page, int pagesize)
        {
            return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Ubicaciones>(Contexto.AddonSga, new Consulta
            {
                Origen = "{ubicaciones}",
                Saltar = unchecked(pagesize * (page - 1)),
                Tomar = pagesize,
            }));
        }

        /// <summary>
        /// Permite obtener una ubicación concreta
        /// </summary>
        [HttpGet]
        [Route("{code}")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage GetByCode(string year, string code)
        {
            var ubicacion = Contexto.Lector.LeerEjercicio<Ubicaciones>(Contexto.AddonSga, "ubicaciones", "LTRIM(RTRIM([CODIGO])) = @code",
                new Dictionary<string, string> { ["@code"] = code.Trim() }).FirstOrDefault();
            return ubicacion == null ? Request.CreateResponse(HttpStatusCode.NoContent) : Respuestas.Json(ubicacion);
        }

        /// <summary>
        /// Permite obtener los artículos de una ubicación y sus almacenes
        /// </summary>
        [HttpGet]
        [Route("rel/{code}/{emp}")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage GetByArticuloRel(string year, string code, string emp)
        {
            var relaciones = Relaciones("UBICACION", code, emp);
            var almacenes = Nombres(year, "almacen");
            var articulos = Nombres(year, "articulo");
            var ubicaciones = Nombres(Contexto.AddonSga, "ubicaciones");

            return Respuestas.Json(relaciones.Select(r => new
            {
                r.Articulo,
                NombreArticulo = articulos.TryGetValue(r.Articulo, out string articulo) ? articulo : null,
                r.Linea,
                r.Almacen,
                r.Empresa,
                r.Ubicacion,
                NombreUbicacion = ubicaciones.TryGetValue(r.Ubicacion, out string ubicacion) ? ubicacion : null,
                NombreAlmacen = almacenes.TryGetValue(r.Almacen, out string almacen) ? almacen : null,
            }).ToList());
        }

        /// <summary>
        /// Permite obtener las ubicaciones por almacén y stock de un artículo
        /// </summary>
        [HttpGet]
        [Route("rel-art/{code}/{emp}")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage GetByArticuloRelAr(string year, string code, string emp)
        {
            const string conStock = "(((([s].[EMPRESA] IS NOT NULL) AND ([s].[ARTICULO] IS NOT NULL)) AND ([s].[TALLA] IS NOT NULL)) AND ([s].[COLOR] IS NOT NULL)) AND ([s].[ALMACEN] IS NOT NULL)";

            var relaciones = Relaciones("ARTICULO", code, emp);
            var almacenes = Nombres(year, "almacen");
            var articulos = Nombres(year, "articulo");
            var stocks = Contexto.Lector.LeerEjercicio<StockAlmacen>(year, new Consulta
            {
                Origen = "{articulo} AS [a] LEFT JOIN {stocks2} AS [s] ON [a].[CODIGO] = [s].[ARTICULO]",
                Expresiones = new Dictionary<string, string>
                {
                    ["ALMACEN"] = "CASE WHEN " + conStock + " THEN [s].[ALMACEN] ELSE '' END",
                    ["STOCK"] = "CASE WHEN " + conStock + " THEN [s].[FINAL] ELSE 0.0 END",
                },
                Condicion = "LTRIM(RTRIM([a].[CODIGO])) = @code",
                Parametros = new Dictionary<string, string> { ["@code"] = code.Trim() },
            }).ToDictionary(s => s.Almacen, s => s.Stock);
            var ubicaciones = Nombres(Contexto.AddonSga, "ubicaciones");

            return Respuestas.Json(relaciones.Select(r => new
            {
                r.Articulo,
                NombreArticulo = articulos.TryGetValue(r.Articulo, out string articulo) ? articulo : null,
                r.Linea,
                r.Almacen,
                r.Empresa,
                r.Ubicacion,
                NombreUbicacion = ubicaciones.TryGetValue(r.Ubicacion, out string ubicacion) ? ubicacion : null,
                NombreAlmacen = almacenes.TryGetValue(r.Almacen, out string almacen) ? almacen : null,
                Stock = stocks.TryGetValue(r.Almacen, out decimal stock) ? stock : 0,
            }).ToList());
        }

        private static List<RelUbiAlm> Relaciones(string columna, string code, string emp)
        {
            return Contexto.Lector.LeerEjercicio<RelUbiAlm>(Contexto.AddonSga, "rel_ubi_alm",
                "(LTRIM(RTRIM([" + columna + "])) = @code) AND (LTRIM(RTRIM([EMPRESA])) = @emp)",
                new Dictionary<string, string> { ["@code"] = code.Trim(), ["@emp"] = emp.Trim() });
        }

        /// <summary>
        /// Código → nombre de todas las filas de una tabla. Los códigos se buscan tal cual están guardados (con sus espacios),
        /// como los diccionarios de interface.s50c.
        /// </summary>
        private static Dictionary<string, string> Nombres(string baseDatos, string tabla)
        {
            return Contexto.Lector.LeerEjercicio<CodigoNombre>(baseDatos, tabla).ToDictionary(f => f.Codigo, f => f.Nombre);
        }
    }
}
