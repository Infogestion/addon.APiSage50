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
    /// <summary>Documentos de entrega y reparto de la app de repartos. La lógica está en los servicios, como en interface.s50c.</summary>
    [RoutePrefix("api/nadilux-repartos/{year:int}/DocumentosEntRep")]
    [Autorizar]
    public sealed class DocumentosEntRepController : ApiController
    {
        /// <summary>
        /// Permite obtener todos los documentos de entrega y reparto
        /// </summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<CDocEntRep>))]
        public HttpResponseMessage Get([Obligatorio] string type = null)
        {
            bool porTipo = type.ToUpper().Equals("E") || type.ToUpper().Equals("R");
            return Respuestas.Json(porTipo ? DocEntRepService.SelectType(type) : DocEntRepService.Select());
        }

        /// <summary>
        /// Permite obtener todos los documentos de entrega y reparto asociados
        /// a un conductor ordenados por la fecha prevista de entrega
        /// </summary>
        [HttpGet]
        [Route("by-conductor")]
        [ResponseType(typeof(List<DocReparto>))]
        public HttpResponseMessage GetByConductor(string year, [Obligatorio] string conductor = null, DateTime? fechaEntregaPrev = null, bool onlyNotDelivered = true,
            bool returnLineas = false, bool onlyLast = false, string dateLastSync = "")
        {
            return Respuestas.Json(DocEntRepService.SelectRepartosByConductor(year, conductor, fechaEntregaPrev, returnLineas, onlyNotDelivered, onlyLast, dateLastSync));
        }

        /// <summary>
        /// Permite obtener todas las incidencias
        /// </summary>
        [HttpGet]
        [Route("Incidencias")]
        [ResponseType(typeof(List<inciden>))]
        public HttpResponseMessage GetIncidencias()
        {
            return Respuestas.Json(IncidenciService.Select());
        }
    }
}
