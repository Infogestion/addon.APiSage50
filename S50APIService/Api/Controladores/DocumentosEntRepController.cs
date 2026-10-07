using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Sage;
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

        /// <summary>
        /// Permite editar un doc de reparto desde la app de repartos
        /// </summary>
        [HttpPut]
        [Route("")]
        [Consume("multipart/form-data")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage SetDocReparto([Obligatorio] string num = null, bool? entregado = null, string fechaEntregado = null, string observaciones = null,
            string obsInt = null, bool? pagado = null, string incidencia = null, string nombre = null, string dni = null, string email = null, bool? enviarFac = null)
        {
            return Respuestas.Json(DocEntRepService.UpdateRepartos(num, entregado, fechaEntregado, observaciones, obsInt, pagado, incidencia, nombre, dni, email, enviarFac,
                Formulario.Ficheros(Request, "Firma"), Formulario.Ficheros(Request, "FirmaSeller"), Formulario.Ficheros(Request, "Photos")));
        }

        /// <summary>
        /// Permite editar un doc de reparto desde la app de repartos
        /// </summary>
        [HttpPut]
        [Route("MoveLinesToCopyDelivery")]
        [Consume("multipart/form-data")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage MoveLinesToCopyDelivery([Obligatorio] string num = null, [Obligatorio] string lines = null)
        {
            return Respuestas.Json(DocEntRepService.MoveLinesToCopyDelivery(num, lines));
        }

        /// <summary>
        /// Permite dar de baja las series en la tabla Compras tras una entrega
        /// </summary>
        [HttpPost]
        [Route("dar-baja-series")]
        public HttpResponseMessage DarBajaSeries([FromBody] BajaSeriesRequest request)
        {
            var invalido = ValidacionCuerpo.Error(Request, request, ModelState);
            if (invalido != null)
                return invalido;

            try
            {
                if (request.Series.Count == 0)
                    return Respuestas.Json(new { result = true, message = "No hay series" });

                ComprasService.DarBajaSeries(request);
                return Respuestas.Json(new { result = true, message = "Series dadas de baja en Compras correctamente" });
            }
            catch (ErrorSqlException ex)
            {
                return Respuestas.ErrorValidacion(ex.MensajeSql);
            }
            catch (Exception ex)
            {
                return Respuestas.ErrorValidacion(ex.Message);
            }
        }
    }
}
