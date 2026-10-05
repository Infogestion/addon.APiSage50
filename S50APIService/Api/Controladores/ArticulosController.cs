using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Sage;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/{year:int}/Articulos")]
    [Autorizar]
    public sealed class ArticulosController : ApiController
    {
        /// <summary>
        /// Permite obtener los artículos de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage Get(string year, int page, int pagesize)
        {
            return Respuestas.Json(ArticulosService.Select(year, page, pagesize));
        }

        /// <summary>
        /// Permite buscar los artículos por código o nombre de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}/{codigo_nombre}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage Get(string year, int page, int pagesize, string codigo_nombre)
        {
            return Respuestas.Json(ArticulosService.Select(year, page, pagesize, codigo_nombre.UrlDecode()));
        }

        /// <summary>
        /// Permite buscar los artículos por código de barras
        /// </summary>
        [HttpGet]
        [Route("barras/{codigo}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage GetByBarras(string year, string codigo)
        {
            return Respuestas.Json(ArticulosService.SelectByBarras(year, codigo.UrlDecode()));
        }

        /// <summary>
        /// Permite obtener un artículo según su id
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        [ResponseType(typeof(Articulo))]
        public HttpResponseMessage Get(string year, string id)
        {
            return Respuestas.JsonONada(ArticulosService.Select(year, id.UrlDecode()));
        }

        /// <summary>
        /// Búsqueda de artículos por nombre o código de proveedor
        /// </summary>
        [HttpGet]
        [Route("search/{query}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage GetArticles(string year, string query, int pageNumber = 0, int pageSize = 0)
        {
            try
            {
                return Respuestas.Json(ArticulosService.SelectSearch(year, query, pageNumber, pageSize));
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
            return Respuestas.Json(Stocks2Service.Select(year, id.UrlDecode()));
        }

        /// <summary>
        /// Permite obtener los stocks de un artículo por un almacen
        /// </summary>
        [HttpGet]
        [Route("{id}/stocks/{almacen}")]
        [ResponseType(typeof(List<Stocks2>))]
        public HttpResponseMessage GetStocks(string year, string id, string almacen)
        {
            return Respuestas.Json(Stocks2Service.Select(year, id.UrlDecode(), almacen.UrlDecode()));
        }

        /// <summary>
        /// Permite buscar artículos devolviendo su stock en el almacen solicitado
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}/warehouse/{warehouse}")]
        [ResponseType(typeof(List<ArticuloWithStock>))]
        public HttpResponseMessage GetWithStockByWarehouse(string year, int page, int pagesize, string warehouse)
        {
            try
            {
                return Respuestas.Json(ArticulosService.SelectWithStockByWarehouse(year, page, pagesize, warehouse.UrlDecode()));
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
            return Respuestas.Json(ComprasService.SelectSeriesDisponiblesPorAlmacen(page, pagesize, articulo, almacen));
        }
    }
}
