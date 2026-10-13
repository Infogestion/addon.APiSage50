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
    [RoutePrefix("api/{year:int}/CabAlbaranRegularizacion")]
    [Autorizar]
    public sealed class CabAlbaranRegularizacionController : ApiController
    {
        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes Regularización de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<CAlbare>))]
        public HttpResponseMessage Get(string year, int page, int pagesize)
        {
            return Respuestas.Json(CAlbareService.Select(year, page, pagesize));
        }

        /// <summary>
        /// Permite obtener todas las cabeceras de los Albaranes Regularización comprendidos entre las fechas datefrom/dateto.
        /// Máscara de datefrom/dateto = yyyy.mm.dd
        /// </summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<CAlbare>))]
        public HttpResponseMessage Get(string year, [NoAnulable] DateTime? datefrom = null, [NoAnulable] DateTime? dateto = null, string warehouse = null)
        {
            var from = datefrom.GetValueOrDefault().Date;
            var to = dateto.GetValueOrDefault().Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            return Respuestas.Json(CAlbareService.Select(year, from, to, warehouse));
        }

        /// <summary>
        /// Permite obtener la cabecera de Albarán Regularización por su empresa y número
        /// </summary>
        [HttpGet]
        [Route("Get/{empresa}/{numero}")]
        [ResponseType(typeof(CAlbare))]
        public HttpResponseMessage Get(string year, string empresa, string numero)
        {
            return Respuestas.JsonONada(CAlbareService.Select(year, empresa.UrlDecode(), numero.UrlDecode()));
        }

        /// <summary>
        /// Permite crear una cabecera de Albarán Regularización
        /// Numero: Es autogenerado, enviar con espacios.
        /// </summary>
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(CAlbare))]
        public HttpResponseMessage Create(string year, [FromBody] CAlbare item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
                Respuestas.Json(CAlbareService.Add(year, item)));
        }

        /// <summary>
        /// Permite modificar una cabecera de Albarán Regularización
        /// </summary>
        [HttpPut]
        [Route("")]
        [ResponseType(typeof(CAlbare))]
        public HttpResponseMessage Update(string year, [FromBody] CAlbare item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                var itemUpdating = CAlbareService.Select(year, item.Empresa, item.Numero);
                if (itemUpdating == null)
                    return Respuestas.NoEncontrado(item);

                return Respuestas.Json(CAlbareService.Update(year, itemUpdating, item));
            });
        }

        /// <summary>
        /// Permite eliminar una cabecera de Albarán Regularización
        /// </summary>
        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(CAlbare))]
        public HttpResponseMessage Delete(string year, [FromBody] CAlbare item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                if (CAlbareService.Select(year, item.Empresa, item.Numero) == null)
                    return Respuestas.NoEncontrado(item);

                CAlbareService.Remove(year, item);
                return Respuestas.Json(item);
            });
        }
    }
}
