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
    [RoutePrefix("api/{year:int}/DetAlbaranRegularizacion")]
    [Autorizar]
    public sealed class DetAlbaranRegularizacionController : ApiController
    {
        /// <summary>
        /// Permite obtener todas las líneas del Albarán Regularización
        /// </summary>
        [HttpGet]
        [Route("{empresa}/{numero}/{almacen}/{page:int}/{pageSize:int}")]
        [ResponseType(typeof(List<object>))]
        public HttpResponseMessage GetPag(string year, string empresa, string numero, string almacen, int page, int pageSize)
        {
            return Respuestas.Json(DAlbareService.SelectConArticulo(year, empresa.UrlDecode(), numero.UrlDecode(), almacen, page, pageSize));
        }

        /// <summary>
        /// Permite obtener la línea del Albarán Regularización por su empresa, número y linea
        /// </summary>
        [HttpGet]
        [Route("{empresa}/{numero}/{linia:int}")]
        [ResponseType(typeof(DAlbare))]
        public HttpResponseMessage Get(string year, string empresa, string numero, int linia)
        {
            return Respuestas.JsonONada(DAlbareService.Select(year, empresa.UrlDecode(), numero.UrlDecode(), linia));
        }

        /// <summary>
        /// Permite obtener la línea del Albarán Regularización por su empresa, número y artículo
        /// </summary>
        [HttpGet]
        [Route("GetArticleFromLine")]
        [ResponseType(typeof(DAlbare))]
        public HttpResponseMessage Get(string year, [Obligatorio] string empresa = null, [Obligatorio] string numero = null, [Obligatorio] string articulo = null)
        {
            return Escritura.Intentar(() =>
                Respuestas.JsonONada(DAlbareService.SelectByArticulo(year, empresa.UrlDecode(), numero.UrlDecode(), articulo)));
        }

        /// <summary>
        /// Permite obtener los artículos relacionados con el Albarán Regularización de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{empresa}/{numero}/Articulos/{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage Get(string year, string empresa, string numero, int page, int pagesize)
        {
            return Respuestas.Json(ArticulosService.SelectDAlbare(year, page, pagesize, empresa.UrlDecode(), numero.UrlDecode()));
        }

        /// <summary>
        /// Permite obtener los artículos relacionados con el Albarán Regularización de forma paginada, con su stock en un
        /// almacén. Si el ejercicio no existe responde 500 sin cuerpo en vez del 404 de las demás rutas, como
        /// interface.s50c, que aquí no pasa por su Repository.
        /// </summary>
        [HttpGet]
        [Route("{empresa}/{numero}/Articulos/{page:int}/{pagesize:int}/warehouse/{warehouse}")]
        [ResponseType(typeof(List<ArticuloWithStock>))]
        public HttpResponseMessage DetAlbaranRegularizacionWithStockByWarehouse(string year, string empresa, string numero, int page, int pagesize, string warehouse)
        {
            try
            {
                return Respuestas.Json(ArticulosService.SelectDAlbareWithStockByWarehouse(year, page, pagesize, empresa.UrlDecode(), numero.UrlDecode(), warehouse.UrlDecode()));
            }
            catch (EjercicioNoEncontradoException)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Permite crear una línea de Albarán Regularización
        /// </summary>
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(DAlbare))]
        public HttpResponseMessage Create(string year, [FromBody] DAlbare item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
                Respuestas.Json(DAlbareService.Add(year, item)));
        }

        /// <summary>
        /// Permite modificar una línea de Albarán Regularización
        /// </summary>
        [HttpPut]
        [Route("")]
        [ResponseType(typeof(DAlbare))]
        public HttpResponseMessage Update(string year, [FromBody] DAlbare item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (DAlbareService.Select(year, item.Empresa, item.Numero, item.Linia) == null)
                    return Respuestas.NoEncontrado(item);

                return Respuestas.Json(DAlbareService.Update(year, item));
            });
        }

        /// <summary>
        /// Permite eliminar una línea de Albarán Regularización
        /// </summary>
        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(DAlbare))]
        public HttpResponseMessage Delete(string year, [FromBody] DAlbare item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (DAlbareService.Select(year, item.Empresa, item.Numero, item.Linia) == null)
                    return Respuestas.NoEncontrado(item);

                DAlbareService.Remove(year, item);
                return Respuestas.Json(item);
            });
        }
    }
}
