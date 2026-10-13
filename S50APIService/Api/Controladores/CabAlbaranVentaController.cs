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
    [RoutePrefix("api/{year:int}/CabAlbaranVenta")]
    [Autorizar]
    public sealed class CabAlbaranVentaController : ApiController
    {
        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes de Venta de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<CAlbare>))]
        public HttpResponseMessage Get(string year, int page, int pagesize)
        {
            return Respuestas.Json(CAlbvenService.Select(year, page, pagesize));
        }

        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes de Venta de forma paginada y con su extensión
        /// </summary>
        [HttpGet]
        [Route("ext/{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<AlbVenComplete>))]
        public HttpResponseMessage GetExt(string year, int page, int pagesize, bool enviarAPP = false)
        {
            var result = CAlbvenService.Select(year, page, pagesize);
            var result2 = CAlbvenExtService.Select(page, pagesize);
            return Respuestas.Json(CAlbvenExtService.Completar(result, result2, enviarAPP));
        }

        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes de Venta comprendidos entre las fechas datefrom/dateto.
        /// Máscara de datefrom/dateto = dd.mm.yyyy
        /// </summary>
        [HttpGet]
        [Route("{datefrom:datetime}/{dateto:datetime}")]
        [ResponseType(typeof(List<CAlbven>))]
        public HttpResponseMessage Get(string year, DateTime datefrom, DateTime dateto)
        {
            return Respuestas.Json(CAlbvenService.Select(year, datefrom, dateto));
        }

        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes de Venta con extensión comprendidos entre las fechas datefrom/dateto.
        /// Máscara de datefrom/dateto = dd.mm.yyyy
        /// </summary>
        [HttpGet]
        [Route("ext/{business}/{datefrom:datetime}/{dateto:datetime}")]
        [ResponseType(typeof(List<AlbVenComplete>))]
        public HttpResponseMessage GetExt(string year, DateTime datefrom, DateTime dateto, string business, [Obligatorio] string warehouse = null, bool enviarAPP = false)
        {
            var result = CAlbvenService.SelectByBusiness(year, datefrom, dateto, business, warehouse);
            var result2 = CAlbvenExtService.Select();
            return Respuestas.Json(CAlbvenExtService.Completar(result, result2, enviarAPP));
        }

        /// <summary>
        /// Permite obtener la extensión de la cabecera de Albarán de Venta por empresa, número y letra
        /// </summary>
        [HttpGet]
        [Route("GetExt/{empresa}/{numero}")]
        [ResponseType(typeof(ExtAlbven))]
        public HttpResponseMessage GetExt(string year, string empresa, string numero, [Obligatorio] string letra = null)
        {
            return Respuestas.JsonONada(CAlbvenExtService.Select(year, empresa.UrlDecode(), numero.UrlDecode(), letra.UrlDecode()));
        }

        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes de Venta comprendidos entre las fechas datefrom/dateto .
        /// Máscara de datefrom/dateto = dd.mm.yyyy de una empresa concreta
        /// </summary>
        [HttpGet]
        [Route("{business}/{datefrom:datetime}/{dateto:datetime}")]
        [ResponseType(typeof(List<CAlbven>))]
        public HttpResponseMessage GetByBusiness(string year, DateTime datefrom, DateTime dateto, string business, [Obligatorio] string warehouse = null)
        {
            return Respuestas.Json(CAlbvenService.SelectByBusiness(year, datefrom, dateto, business, warehouse));
        }

        /// <summary>
        /// Permite obtener la cabecera de Albarán de Venta por empresa, número y letra
        /// </summary>
        [HttpGet]
        [Route("Get/{empresa}/{numero}/{letra}")]
        [ResponseType(typeof(CAlbven))]
        public HttpResponseMessage Get(string year, string empresa, string numero, string letra)
        {
            return Respuestas.JsonONada(CAlbvenService.Select(year, empresa.UrlDecode(), numero.UrlDecode(), letra.UrlDecode()));
        }

        /// <summary>
        /// Permite crear una cabecera de Albarán Venta
        /// </summary>
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(CAlbven))]
        public HttpResponseMessage Create(string year, [FromBody] CAlbven item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
                Respuestas.Json(CAlbvenService.Add(year, item)));
        }

        /// <summary>
        /// Permite modificar las observaciones de una cabecera de Albarán Venta
        /// </summary>
        [HttpPut]
        [Route("UpdateObs")]
        [ResponseType(typeof(CAlbven))]
        public HttpResponseMessage UpdateObs(string year, [Obligatorio] string empresa = null, [Obligatorio] string numero = null,
            [Obligatorio] string letra = null, [Obligatorio] string obs = null)
        {
            return Escritura.Intentar(() =>
            {
                if (CAlbvenService.Select(year, empresa, numero, letra) == null)
                    return Respuestas.NoEncontrado(null);

                return Respuestas.Json(CAlbvenService.UpdateObs(year, empresa, numero, letra, obs));
            });
        }

        /// <summary>
        /// Permite modificar una cabecera de Albarán Venta
        /// </summary>
        [HttpPut]
        [Route("")]
        [ResponseType(typeof(CAlbven))]
        public HttpResponseMessage Update(string year, [FromBody] CAlbven item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (CAlbvenService.Select(year, item.Empresa, item.Numero, item.Letra) == null)
                    return Respuestas.NoEncontrado(item);

                return Respuestas.Json(CAlbvenService.Update(year, item));
            });
        }

        /// <summary>
        /// Permite eliminar una cabecera de Albarán Venta
        /// </summary>
        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(CAlbven))]
        public HttpResponseMessage Delete(string year, [FromBody] CAlbven item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (CAlbvenService.Select(year, item.Empresa, item.Numero, item.Letra) == null)
                    return Respuestas.NoEncontrado(item);

                CAlbvenService.Remove(year, item);
                CAlbvenExtService.Remove(year, item);
                return Respuestas.Json(item);
            });
        }

        /// <summary>
        /// Permite modificar la extensión de una cabecera de Albarán Venta
        /// </summary>
        [HttpPut]
        [Route("ext")]
        [ResponseType(typeof(ExtAlbven))]
        public HttpResponseMessage UpdateExt(string year, [FromBody] ExtAlbven item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item)) ?? Escritura.Intentar(() =>
                Respuestas.Json(CAlbvenExtService.Update(year, item)));
        }
    }
}
