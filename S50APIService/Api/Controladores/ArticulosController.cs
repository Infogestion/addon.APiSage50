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
    [RoutePrefix("api/{year:int}/Articulos")]
    [Autorizar]
    public sealed class ArticulosController : ApiController
    {
        private const string Tabla = "{articulo} AS [a]";

        /// <summary>
        /// Permite obtener los artículos de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage Get(string year, int page, int pagesize)
        {
            return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Articulo>(year, Pagina(Articulos(), page, pagesize)));
        }

        /// <summary>
        /// Permite buscar los artículos por código o nombre de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}/{codigo_nombre}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage Get(string year, int page, int pagesize, string codigo_nombre)
        {
            var consulta = Articulos(
                "((@texto LIKE '') OR (CHARINDEX(@texto, LOWER([a].[CODIGO])) > 0)) OR ((@texto LIKE '') OR (CHARINDEX(@texto, LOWER([a].[NOMBRE])) > 0))",
                new Dictionary<string, string> { ["@texto"] = WebUtility.UrlDecode(codigo_nombre).ToLower() });
            return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Articulo>(year, Pagina(consulta, page, pagesize)));
        }

        /// <summary>
        /// Permite buscar los artículos por código de barras
        /// </summary>
        [HttpGet]
        [Route("barras/{codigo}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage GetByBarras(string year, string codigo)
        {
            return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Articulo>(year, new Consulta
            {
                Origen = Tabla + " INNER JOIN {barras} AS [b] ON [a].[CODIGO] = [b].[ARTICULO]",
                Alias = "a",
                Condicion = "[b].[BARRAS] = @codigo",
                Parametros = new Dictionary<string, string> { ["@codigo"] = WebUtility.UrlDecode(codigo) },
            }));
        }

        /// <summary>
        /// Permite obtener un artículo según su id
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        [ResponseType(typeof(Articulo))]
        public HttpResponseMessage Get(string year, string id)
        {
            var articulo = Contexto.Lector.LeerEjercicio<Articulo>(year, Articulos("LTRIM(RTRIM([a].[CODIGO])) = @id",
                new Dictionary<string, string> { ["@id"] = WebUtility.UrlDecode(id).Trim() })).FirstOrDefault();
            return articulo == null ? Request.CreateResponse(HttpStatusCode.NoContent) : Respuestas.Json(articulo);
        }

        /// <summary>
        /// Búsqueda de artículos por nombre o código de proveedor
        /// </summary>
        [HttpGet]
        [Route("search/{query}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage GetArticles(string year, string query, int pageNumber = 0, int pageSize = 0)
        {
            var patron = new Dictionary<string, string> { ["@patron"] = "%" + query.Trim().ToLower() + "%" };
            try
            {
                var porNombre = Contexto.Lector.LeerEjercicioJson<Articulo>(year,
                    Pagina(Articulos("LOWER([a].[NOMBRE]) LIKE @patron", patron), pageNumber, pageSize));
                if (!EstaVacio(porNombre))
                    return Respuestas.JsonBloques(porNombre);

                return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Articulo>(year, Pagina(new Consulta
                {
                    Origen = "{referpro} AS [r] INNER JOIN " + Tabla + " ON LTRIM(RTRIM([r].[ARTICULO])) = LTRIM(RTRIM([a].[CODIGO]))",
                    Alias = "a",
                    Condicion = "LOWER([r].[PROVEEDOR]) LIKE @patron",
                    Parametros = patron,
                }, pageNumber, pageSize)));
            }
            catch (EjercicioNoEncontradoException)
            {
                // interface.s50c no convierte aquí el ejercicio inexistente en 404: responde 500 sin cuerpo.
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Permite obtener los stocks de un artículo por cada almacen
        /// </summary>
        [HttpGet]
        [Route("{id}/stocks")]
        [ResponseType(typeof(List<Stocks2>))]
        public HttpResponseMessage GetStocks(string year, string id)
        {
            return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Stocks2>(year, "stocks2", "LTRIM(RTRIM([ARTICULO])) = @id",
                new Dictionary<string, string> { ["@id"] = WebUtility.UrlDecode(id).Trim() }));
        }

        /// <summary>
        /// Permite obtener los stocks de un artículo por un almacen
        /// </summary>
        [HttpGet]
        [Route("{id}/stocks/{almacen}")]
        [ResponseType(typeof(List<Stocks2>))]
        public HttpResponseMessage GetStocks(string year, string id, string almacen)
        {
            return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Stocks2>(year, "stocks2",
                "(LTRIM(RTRIM([ARTICULO])) = @id) AND (LTRIM(RTRIM([ALMACEN])) = @almacen)",
                new Dictionary<string, string> { ["@id"] = WebUtility.UrlDecode(id).Trim(), ["@almacen"] = WebUtility.UrlDecode(almacen).Trim() }));
        }

        /// <summary>
        /// Permite buscar artículos devolviendo su stock en el almacen solicitado
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}/warehouse/{warehouse}")]
        [ResponseType(typeof(List<ArticuloWithStock>))]
        public HttpResponseMessage GetWithStockByWarehouse(string year, int page, int pagesize, string warehouse)
        {
            const string sinStock = "(((([s].[EMPRESA] IS NULL) OR ([s].[ARTICULO] IS NULL)) OR ([s].[TALLA] IS NULL)) OR ([s].[COLOR] IS NULL)) OR ([s].[ALMACEN] IS NULL)";
            const string conStock = "(((([s].[EMPRESA] IS NOT NULL) AND ([s].[ARTICULO] IS NOT NULL)) AND ([s].[TALLA] IS NOT NULL)) AND ([s].[COLOR] IS NOT NULL)) AND ([s].[ALMACEN] IS NOT NULL)";
            try
            {
                return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<ArticuloWithStock>(year, Pagina(new Consulta
                {
                    Origen = Tabla + " LEFT JOIN {stocks2} AS [s] ON [a].[CODIGO] = [s].[ARTICULO]",
                    Alias = "a",
                    Expresiones = new Dictionary<string, string> { ["STOCKBYALMACEN"] = "CASE WHEN " + conStock + " THEN [s].[FINAL] ELSE 0.0 END" },
                    Condicion = "(" + sinStock + ") OR (LTRIM(RTRIM([s].[ALMACEN])) = @almacen)",
                    Parametros = new Dictionary<string, string> { ["@almacen"] = WebUtility.UrlDecode(warehouse).Trim() },
                }, page, pagesize)));
            }
            catch (EjercicioNoEncontradoException)
            {
                // interface.s50c no pasa aquí por su Repository: el ejercicio inexistente llega como este error de EF.
                return Respuestas.ErrorInterno("An exception has been raised that is likely due to a transient failure. "
                    + "Consider enabling transient error resiliency by adding 'EnableRetryOnFailure' to the 'UseSqlServer' call.");
            }
            catch (ErrorSqlException ex)
            {
                return Respuestas.ErrorInterno(ex.MensajeSql);
            }
        }

        /// <summary>
        /// Permite obtener las series de un artículo y un almacén concreto disponibles
        /// </summary>
        [HttpGet]
        [Route("series/{articulo}/{almacen}/{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<Compras>))]
        public HttpResponseMessage GetSeries(string year, string articulo, string almacen, int page, int pagesize)
        {
            return Respuestas.JsonBloques(Contexto.Lector.LeerJson<Compras>("COMUNES", Pagina(new Consulta
            {
                Origen = "{compras}",
                Condicion = "((LTRIM(RTRIM([ARTICULO])) = @articulo) AND (LTRIM(RTRIM([ALMACEN])) = @almacen)) AND (UPPER(LTRIM(RTRIM([BAJA]))) <> 'S')",
                Parametros = new Dictionary<string, string> { ["@articulo"] = articulo.Trim(), ["@almacen"] = almacen.Trim() },
            }, page, pagesize)));
        }

        private static Consulta Articulos(string condicion = null, Dictionary<string, string> parametros = null)
        {
            return new Consulta { Origen = Tabla, Alias = "a", Condicion = condicion, Parametros = parametros };
        }

        /// <summary>El Skip(pagesize * (page - 1)).Take(pagesize) de interface.s50c, con su mismo desbordamiento de int.</summary>
        private static Consulta Pagina(Consulta consulta, int page, int pagesize)
        {
            consulta.Saltar = unchecked(pagesize * (page - 1));
            consulta.Tomar = pagesize;
            return consulta;
        }

        /// <summary>True si el JSON es el array sin elementos ("[]").</summary>
        private static bool EstaVacio(List<byte[]> bloques)
        {
            return bloques.Sum(b => b.Length) == 2;
        }
    }
}
