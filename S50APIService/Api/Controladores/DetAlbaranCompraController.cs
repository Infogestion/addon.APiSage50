using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/{year:int}/DetAlbaranCompra")]
    [Autorizar]
    public sealed class DetAlbaranCompraController : ApiController
    {
        /// <summary>
        /// Permite obtener todas las líneas del Albarán de Compra
        /// </summary>
        [HttpGet]
        [Route("{empresa}/{proveedor}/{numero}")]
        [ResponseType(typeof(List<DAlbcom>))]
        public HttpResponseMessage Get(string year, string empresa, string proveedor, string numero, bool emptyArticles = true)
        {
            return Respuestas.Json(DAlbcomService.Select(year, empresa.UrlDecode(), proveedor.UrlDecode(), numero.UrlDecode(), emptyArticles));
        }

        /// <summary>
        /// Permite obtener los artículos relacionados con el Albarán de Compra de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{empresa}/{numero}/{proveed}/Articulos/{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage GetAr(string year, string empresa, string numero, int page, int pagesize, string proveed)
        {
            return Respuestas.Json(ArticulosService.SelectDAlbcom(year, page, pagesize, empresa.UrlDecode(), numero.UrlDecode(), proveed.UrlDecode()));
        }

        /// <summary>
        /// Permite obtener todas las líneas del Albarán de Compra por empresa, número y proveedor con su extensión
        /// </summary>
        [HttpGet]
        [Route("ext/{empresa}/{numero}/{proveed}")]
        [ResponseType(typeof(List<ExtDAlbcom>))]
        public HttpResponseMessage GetExt(string year, string empresa, string numero, string proveed)
        {
            return Respuestas.Json(DAlbcomExtService.Select(year, empresa.UrlDecode(), numero.UrlDecode(), proveed.UrlDecode()));
        }

        /// <summary>
        /// Permite obtener la línea del Albarán de Compra por su empresa, proveedor, número y linea
        /// </summary>
        [HttpGet]
        [Route("{empresa}/{proveedor}/{numero}/{linia:int}")]
        [ResponseType(typeof(DAlbcom))]
        public HttpResponseMessage Get(string year, string empresa, string proveedor, string numero, int linia)
        {
            return Respuestas.JsonONada(DAlbcomService.Select(year, empresa.UrlDecode(), proveedor.UrlDecode(), numero.UrlDecode(), linia));
        }

        /// <summary>
        /// Permite crear una línea de Albarán Compra
        /// </summary>
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(DAlbcom))]
        public HttpResponseMessage Create(string year, [FromBody] DAlbcom item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
                Respuestas.Json(DAlbcomService.Add(year, item)));
        }

        /// <summary>
        /// Permite crear la extensión de una línea de Albarán Compra
        /// </summary>
        [HttpPost]
        [Route("ext")]
        [ResponseType(typeof(ExtDAlbcom))]
        public HttpResponseMessage CreateExt(string year, [FromBody] ExtDAlbcom item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
                Respuestas.Json(DAlbcomExtService.Add(item)));
        }

        /// <summary>
        /// Permite modificar una línea de Albarán Compra
        /// </summary>
        [HttpPut]
        [Route("")]
        [ResponseType(typeof(DAlbcom))]
        public HttpResponseMessage Update(string year, [FromBody] DAlbcom item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (DAlbcomService.Select(year, item.Empresa, item.Proveedor, item.Numero, item.Linia) == null)
                    return Respuestas.NoEncontrado(item);

                return Respuestas.Json(DAlbcomService.Update(year, item));
            });
        }

        /// <summary>
        /// Permite apuntar las unidades recibidas de una línea de Albarán Compra, que pasan a ser también las unidades de
        /// la línea si no viene de un pedido
        /// </summary>
        [HttpPut]
        [Route("ext")]
        [ResponseType(typeof(ExtDAlbcom))]
        public HttpResponseMessage UpdateExt(string year, [FromBody] ExtDAlbcom item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                var itemDetUpdating = DAlbcomService.Select(year, item.Empresa, item.Proveed, item.Numero, item.Linea);
                if (itemDetUpdating == null)
                    throw new InvalidOperationException("No se encontró la línea del albarán de compra.");

                if (string.IsNullOrWhiteSpace(itemDetUpdating.DocNum) && itemDetUpdating.Unidades != item.Unidadespreparadas)
                    DAlbcomService.UpdateUnidades(year, itemDetUpdating, item.Unidadespreparadas);

                return Respuestas.Json(DAlbcomExtService.Update(year, item));
            });
        }

        /// <summary>
        /// Permite eliminar una línea de Albarán Compra
        /// </summary>
        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(DAlbcom))]
        public HttpResponseMessage Delete(string year, [FromBody] DAlbcom item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (DAlbcomService.Select(year, item.Empresa, item.Proveedor, item.Numero, item.Linia) == null)
                    return Respuestas.NoEncontrado(item);

                DAlbcomService.Remove(year, item);
                return Respuestas.Json(item);
            });
        }

        /// <summary>
        /// Permite eliminar la extensión de una línea de Albarán Compra
        /// </summary>
        [HttpDelete]
        [Route("ext")]
        [ResponseType(typeof(ExtDAlbcom))]
        public HttpResponseMessage DeleteExt(string year, [FromBody] ExtDAlbcom item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                var itemDeleting = DAlbcomExtService.SelectOne(year, item.Empresa, item.Numero, item.Linea, item.Proveed);
                if (itemDeleting == null)
                    return Respuestas.NoEncontrado(item);

                DAlbcomExtService.Remove(itemDeleting);
                return Respuestas.Json(item);
            });
        }
    }
}
