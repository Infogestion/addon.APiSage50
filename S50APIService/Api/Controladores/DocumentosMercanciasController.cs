using System;
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
    /// <summary>Documentos de mercancías de la app de reposición. La lógica está en los servicios, como en interface.s50c.</summary>
    [RoutePrefix("api/nadilux-mercancias/DocumentosMercancias")]
    [Autorizar]
    public sealed class DocumentosMercanciasController : ApiController
    {
        /// <summary>
        /// Permite obtener todos los documentos de gestión de mercancías
        /// </summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<DocMercancia>))]
        public HttpResponseMessage Get(string operario = null, DateTime? fecha = null, string proveedor = null, string albaran = null)
        {
            return Respuestas.Json(DocMercanciasService.Select(operario, fecha, proveedor, albaran));
        }

        [HttpGet]
        [Route("by-number")]
        [ResponseType(typeof(DocMercancia))]
        public HttpResponseMessage Get([Obligatorio] string numero = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null)
        {
            return Respuestas.JsonONada(DocMercanciasService.SelectOne(numero, ejercicio, empresa));
        }

        [HttpGet]
        [Route("detail-by-barcode")]
        [ResponseType(typeof(DDocMercanciaExt))]
        public HttpResponseMessage GetDetailByBarcode([Obligatorio] string numero = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null, [Obligatorio] string barcode = null)
        {
            return Respuestas.JsonONada(DocMercanciasService.GetDetailByBarcode(numero, ejercicio, empresa, barcode));
        }

        [HttpGet]
        [Route("detail")]
        [ResponseType(typeof(DDocMercanciaExt))]
        public HttpResponseMessage GetDetail([Obligatorio] string numero = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null, [Obligatorio] string code = null)
        {
            return Respuestas.JsonONada(DocMercanciasService.GetDetail(numero, ejercicio, empresa, code));
        }

        [HttpGet]
        [Route("detail-sends")]
        [ResponseType(typeof(string))]
        public HttpResponseMessage GetDetailSends([Obligatorio] string numero = null, [Obligatorio] string empresa = null, [Obligatorio] string ejercicio = null, [Obligatorio] string code = null)
        {
            return Respuestas.Texto(AlmaEnvioService.GetWarehouseDetail(numero, empresa, ejercicio, code));
        }

        /// <summary>
        /// Permite editar las observaciones del documento
        /// </summary>
        [HttpPut]
        [Route("")]
        [Consume("multipart/form-data")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage SetDocMercancia([Obligatorio] string numero = null, [Obligatorio] string empresa = null, [Obligatorio] string ejercicio = null, [Obligatorio] string state = null, string obs = null)
        {
            return Respuestas.Json(DocMercanciasService.UpdateMercancia(numero, empresa, ejercicio, obs, state));
        }

        /// <summary>
        /// Permite editar las unidades del documento de mercancía
        /// </summary>
        [HttpPut]
        [Route("detail")]
        [Consume("multipart/form-data")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage SetDocDetailMercancia([Obligatorio] string numero = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null, [Obligatorio] string art = null, [Obligatorio] string units = null)
        {
            return Respuestas.Json(AlmaEnvioService.UpdateDetailMercancia(numero, empresa, ejercicio, art, units));
        }

        [HttpGet]
        [Route("details-search")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage GetDetailsSearch([Obligatorio] string numero = null, [Obligatorio] string empresa = null, [Obligatorio] string ejercicio = null, [Obligatorio] string query = null)
        {
            try
            {
                return Respuestas.Json(DocMercanciasService.GetDetailsSearch(numero, ejercicio, empresa, query));
            }
            catch (EjercicioNoEncontradoException)
            {
                // interface.s50c no convierte aquí el ejercicio inexistente en 404: responde 500 sin cuerpo.
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
    }
}
