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
    [RoutePrefix("api/{year:int}/CabAlbaranCompra")]
    [Autorizar]
    public sealed class CabAlbaranCompraController : ApiController
    {
        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes de Compra de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<CAlbare>))]
        public HttpResponseMessage Get(string year, int page, int pagesize)
        {
            return Respuestas.Json(CAlbcomService.Select(year, page, pagesize));
        }

        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes de Compra de forma paginada y con su extensión
        /// </summary>
        [HttpGet]
        [Route("ext/{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<AlbComComplete>))]
        public HttpResponseMessage GetExt(string year, int page, int pagesize, bool enviarAPP = false)
        {
            var result = CAlbcomService.Select(year, page, pagesize);
            var result2 = CAlbcomExtService.Select(page, pagesize);
            return Respuestas.Json(CAlbcomExtService.Completar(result, result2, enviarAPP, mismoProveedor: false));
        }

        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes de Compra comprendidos entre las fechas datefrom/dateto.
        /// Máscara de datefrom/dateto = dd.mm.yyyy
        /// </summary>
        [HttpGet]
        [Route("{datefrom:datetime}/{dateto:datetime}")]
        [ResponseType(typeof(List<CAlbcom>))]
        public HttpResponseMessage Get(string year, DateTime datefrom, DateTime dateto)
        {
            return Respuestas.Json(CAlbcomService.Select(year, datefrom, dateto));
        }

        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes de Compra comprendidos entre las fechas datefrom/dateto .
        /// Máscara de datefrom/dateto = dd.mm.yyyy de una empresa concreta
        /// </summary>
        [HttpGet]
        [Route("{business}/{datefrom:datetime}/{dateto:datetime}")]
        [ResponseType(typeof(List<CAlbcom>))]
        public HttpResponseMessage GetByBusiness(string year, DateTime datefrom, DateTime dateto, string business, [Obligatorio] string warehouse = null)
        {
            return Respuestas.Json(CAlbcomService.SelectByBusiness(year, datefrom, dateto, business, warehouse));
        }

        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes de compra con extensión comprendidos entre las fechas datefrom/dateto.
        /// Máscara de datefrom/dateto = dd.mm.yyyy. Incluye el día dateto entero y ninguno posterior (interface.s50c
        /// devolvía también los albaranes del día siguiente).
        /// </summary>
        [HttpGet]
        [Route("ext/{business}/{datefrom:datetime}/{dateto:datetime}")]
        [ResponseType(typeof(List<AlbComComplete>))]
        public HttpResponseMessage GetExt(string year, DateTime datefrom, DateTime dateto, string business, [Obligatorio] string warehouse = null, bool enviarAPP = false)
        {
            DateTime finDelDia = dateto.Date.AddDays(1).AddMilliseconds(-3);
            var result = CAlbcomService.SelectByBusiness(year, datefrom, finDelDia, business, warehouse);
            var result2 = CAlbcomExtService.Select();
            return Respuestas.Json(CAlbcomExtService.Completar(result, result2, enviarAPP, mismoProveedor: true));
        }

        /// <summary>
        /// Permite obtener la cabecera de Albarán de Compra por su empresa proveedor y número
        /// </summary>
        [HttpGet]
        [Route("Get/{empresa}/{proveedor}/{numero}")]
        [ResponseType(typeof(List<CAlbcom>))]
        public HttpResponseMessage Get(string year, string empresa, string proveedor, string numero)
        {
            return Respuestas.JsonONada(CAlbcomService.Select(year, empresa.UrlDecode(), proveedor.UrlDecode(), numero.UrlDecode()));
        }

        /// <summary>
        /// Permite obtener la cabecera de Albarán de Compra por su empresa proveedor y número
        /// </summary>
        [HttpGet]
        [Route("GetExt/{empresa}/{numero}/{proveed}")]
        [ResponseType(typeof(List<ExtAlbcom>))]
        public HttpResponseMessage GetExt(string year, string empresa, string numero, string proveed)
        {
            return Respuestas.JsonONada(CAlbcomExtService.Select(year, empresa.UrlDecode(), numero.UrlDecode(), proveed.UrlDecode()));
        }

        /// <summary>
        /// Permite crear una cabecera de Albarán Compra
        /// </summary>
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(CAlbcom))]
        public HttpResponseMessage Create(string year, [FromBody] CAlbcom item, bool enviarAPP = false)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                var result = CAlbcomService.Add(year, item);
                CAlbcomExtService.Enviar(year, result, enviarAPP);
                return Respuestas.Json(result);
            });
        }

        /// <summary>
        /// Permite modificar una cabecera de Albarán Venta
        /// </summary>
        [HttpPut]
        [Route("UpdateObs")]
        [ResponseType(typeof(CAlbcom))]
        public HttpResponseMessage UpdateObs(string year, [Obligatorio] string empresa = null, [Obligatorio] string numero = null,
            [Obligatorio] string proveedor = null, [Obligatorio] string obs = null)
        {
            return Escritura.Intentar(() =>
            {
                if (CAlbcomService.Select(year, empresa, proveedor, numero) == null)
                    return Respuestas.NoEncontrado(null);

                return Respuestas.Json(CAlbcomService.UpdateObs(year, empresa, proveedor, numero, obs));
            });
        }

        /// <summary>
        /// Permite modificar una cabecera de Albarán Compra
        /// </summary>
        [HttpPut]
        [Route("")]
        [ResponseType(typeof(CAlbcom))]
        public HttpResponseMessage Update(string year, [FromBody] CAlbcom item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (CAlbcomService.Select(year, item.Empresa, item.Proveedor, item.Numero) == null)
                    return Respuestas.NoEncontrado(item);

                return Respuestas.Json(CAlbcomService.Update(year, item));
            });
        }

        /// <summary>
        /// Permite modificar una cabecera de Albarán Compra
        /// </summary>
        [HttpPut]
        [Route("ext")]
        [ResponseType(typeof(ExtAlbcom))]
        public HttpResponseMessage UpdateExt(string year, [FromBody] ExtAlbcom item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item)) ?? Escritura.Intentar(() =>
                Respuestas.Json(CAlbcomExtService.Update(year, item)));
        }

        /// <summary>
        /// Permite eliminar una cabecera de Albarán Compra
        /// </summary>
        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(CAlbcom))]
        public HttpResponseMessage Delete(string year, [FromBody] CAlbcom item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (CAlbcomService.Select(year, item.Empresa, item.Proveedor, item.Numero) == null)
                    return Respuestas.NoEncontrado(item);

                CAlbcomService.Remove(year, item);
                CAlbcomExtService.Remove(year, item);
                return Respuestas.Json(item);
            });
        }
    }
}
