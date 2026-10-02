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
