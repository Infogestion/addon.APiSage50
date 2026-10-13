using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/{year:int}/DetAlbaranVenta")]
    [Autorizar]
    public sealed class DetAlbaranVentaController : ApiController
    {
        /// <summary>
        /// Permite obtener todas las líneas del Albarán de Venta por empresa, número y letra
        /// </summary>
        [HttpGet]
        [Route("{empresa}/{numero}/{letra}")]
        [ResponseType(typeof(List<DAlbven>))]
        public HttpResponseMessage Get(string year, string empresa, string numero, string letra, bool emptyArticles = true)
        {
            return Respuestas.Json(DAlbvenService.Select(year, empresa.UrlDecode(), numero.UrlDecode(), letra.UrlDecode(), emptyArticles));
        }

        /// <summary>
        /// Permite obtener los artículos relacionados con el Albarán Venta de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{empresa}/{numero}/Articulos/{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage GetAr(string year, string empresa, string numero, int page, int pagesize, [Obligatorio] string letra = null, bool emptyArticles = true)
        {
            return Respuestas.Json(ArticulosService.SelectDAlbven(year, page, pagesize, empresa.UrlDecode(), numero.UrlDecode(), letra.UrlDecode(), emptyArticles));
        }

        /// <summary>
        /// Permite obtener todas las líneas del Albarán de Venta por empresa, número y letra con su extensión
        /// </summary>
        [HttpGet]
        [Route("ext/{empresa}/{numero}/{letra}")]
        [ResponseType(typeof(List<ExtDAlbven>))]
        public HttpResponseMessage GetExt(string year, string empresa, string numero, string letra)
        {
            return Respuestas.Json(DAlbvenExtService.Select(year, empresa.UrlDecode(), numero.UrlDecode(), letra.UrlDecode()));
        }

        /// <summary>
        /// Permite obtener la línea del Albarán de Venta por su empresa, número, letra y linea
        /// </summary>
        [HttpGet]
        [Route("{empresa}/{numero}/{letra}/{linia:int}")]
        [ResponseType(typeof(DAlbven))]
        public HttpResponseMessage Get(string year, string empresa, string numero, string letra, int linia)
        {
            return Respuestas.JsonONada(DAlbvenService.Select(year, empresa.UrlDecode(), numero.UrlDecode(), letra.UrlDecode(), linia));
        }

        /// <summary>
        /// Permite crear una línea de Albarán Venta
        /// </summary>
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(DAlbven))]
        public HttpResponseMessage Create(string year, [FromBody] DAlbven item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
                Respuestas.Json(DAlbvenService.Add(year, item)));
        }

        /// <summary>
        /// Permite modificar una línea de Albarán Venta
        /// </summary>
        [HttpPut]
        [Route("")]
        [ResponseType(typeof(DAlbven))]
        public HttpResponseMessage Update(string year, [FromBody] DAlbven item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (DAlbvenService.Select(year, item.Empresa, item.Numero, item.Letra, item.Linia) == null)
                    return Respuestas.NoEncontrado(item);

                return Respuestas.Json(DAlbvenService.Update(year, item));
            });
        }

        /// <summary>
        /// Permite eliminar una línea de Albarán Venta
        /// </summary>
        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(DAlbven))]
        public HttpResponseMessage Delete(string year, [FromBody] DAlbven item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (DAlbvenService.Select(year, item.Empresa, item.Numero, item.Letra, item.Linia) == null)
                    return Respuestas.NoEncontrado(item);

                DAlbvenService.Remove(year, item);
                return Respuestas.Json(item);
            });
        }

        /// <summary>
        /// Permite crear la extensión de una línea de Albarán Venta
        /// </summary>
        [HttpPost]
        [Route("ext")]
        [ResponseType(typeof(ExtDAlbven))]
        public HttpResponseMessage CreateExt(string year, [FromBody] ExtDAlbven item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
                Respuestas.Json(DAlbvenExtService.Add(item)));
        }

        /// <summary>
        /// Permite apuntar las unidades preparadas de una línea de Albarán Venta
        /// </summary>
        [HttpPut]
        [Route("ext")]
        [ResponseType(typeof(ExtDAlbven))]
        public HttpResponseMessage UpdateExt(string year, [FromBody] ExtDAlbven item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
                Respuestas.Json(DAlbvenExtService.Update(year, item)));
        }

        /// <summary>
        /// Permite eliminar la extensión de una línea de Albarán Venta
        /// </summary>
        [HttpDelete]
        [Route("ext")]
        [ResponseType(typeof(ExtDAlbven))]
        public HttpResponseMessage DeleteExt(string year, [FromBody] ExtDAlbven item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                var itemDeleting = DAlbvenExtService.SelectOne(year, item.Empresa, item.Numero, item.Letra, item.Linea);
                if (itemDeleting == null)
                    return Respuestas.NoEncontrado(item);

                DAlbvenExtService.Remove(itemDeleting);
                return Respuestas.Json(item);
            });
        }
    }
}
