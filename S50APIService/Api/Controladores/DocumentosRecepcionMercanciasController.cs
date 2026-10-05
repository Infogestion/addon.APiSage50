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
    /// <summary>
    /// Recepciones de mercancías de la app de reposición, con envíos y sin envíos. La lógica está en los servicios, como en
    /// interface.s50c; aquí los cuatro son dos, cada uno con su clase de <see cref="Recepcion"/>.
    /// </summary>
    [RoutePrefix("api/nadilux-mercancias/{year:int}/DocumentosRecepcionMercancias")]
    [Autorizar]
    public sealed class DocumentosRecepcionMercanciasController : ApiController
    {
        private static readonly CRecepcionService<CRepMerc> _service = new CRecepcionService<CRepMerc>(Recepcion.ConEnvios);
        private static readonly DRecepcionService<DRepMerc> _serviceDetails = new DRecepcionService<DRepMerc>(Recepcion.ConEnvios);
        private static readonly CRecepcionService<CRepArt> _cRecepcionSinEnviosService = new CRecepcionService<CRepArt>(Recepcion.SinEnvios);
        private static readonly DRecepcionService<DRepArt> _dRecepcionSinEnviosService = new DRecepcionService<DRepArt>(Recepcion.SinEnvios);

        [HttpGet]
        [Route("recepciones-by-operator")]
        [ResponseType(typeof(CRepMerc))]
        public HttpResponseMessage GetRecepcionesByOperator([Obligatorio] string operador = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null)
        {
            return Respuestas.Json(_service.GetRecepcionesByOperator(operador, ejercicio, empresa));
        }

        [HttpGet]
        [Route("recepciones-sinenvio-by-operator")]
        [ResponseType(typeof(CRepArt))]
        public HttpResponseMessage GetRecepcionesSinEnviosByOperator([Obligatorio] string operador = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null)
        {
            return Respuestas.Json(_cRecepcionSinEnviosService.GetRecepcionesByOperator(operador, ejercicio, empresa));
        }

        [HttpGet]
        [Route("recepcion")]
        [ResponseType(typeof(RecepcionCompletaDto))]
        public HttpResponseMessage GetRecepcionByNumero(string year, [Obligatorio] string numero = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null,
            int page = 1, int pageSize = 50, string barra = null, string proveedor = null, string codArt = null, string descripcion = null)
        {
            try
            {
                return Respuestas.Json(new RecepcionCompletaDto
                {
                    Cabecera = _service.GetRecepcionByNumero(numero, ejercicio, empresa),
                    Detalles = _serviceDetails.GetRecepcionDetails(year, numero, ejercicio, empresa, page, pageSize, barra, proveedor, codArt, descripcion),
                });
            }
            catch (EjercicioNoEncontradoException)
            {
                // interface.s50c no convierte aquí el ejercicio inexistente en 404: responde 500 sin cuerpo.
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        [Route("recepcion-sinenvios")]
        [ResponseType(typeof(RecepcionSinEnviosCompletaDto))]
        public HttpResponseMessage GetRecepcionSinEnviosByNumero(string year, [Obligatorio] string numero = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null,
            int page = 1, int pageSize = 50, string barra = null, string proveedor = null, string codArt = null, string descripcion = null)
        {
            try
            {
                return Respuestas.Json(new RecepcionSinEnviosCompletaDto
                {
                    Cabecera = _cRecepcionSinEnviosService.GetRecepcionByNumero(numero, ejercicio, empresa),
                    Detalles = _dRecepcionSinEnviosService.GetRecepcionDetails(year, numero, ejercicio, empresa, page, pageSize, barra, proveedor, codArt, descripcion),
                });
            }
            catch (EjercicioNoEncontradoException)
            {
                // interface.s50c no convierte aquí el ejercicio inexistente en 404: responde 500 sin cuerpo.
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Permite cambiar el estado de un documento de repeción de mercancías,
        /// </summary>
        [HttpPut]
        [Route("cambiar-estado")]
        [Consume("multipart/form-data")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage CambiarEstado([Obligatorio] string num = null, [Obligatorio] string estado = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null)
        {
            return Respuestas.Json(_service.CambiarEstado(num, estado, ejercicio, empresa));
        }

        /// <summary>
        /// Permite cambiar el estado de un documento de repeción de mercancías,
        /// </summary>
        [HttpPut]
        [Route("cambiar-estado-sinenvios")]
        [Consume("multipart/form-data")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage CambiarEstadoSinEnvios([Obligatorio] string num = null, [Obligatorio] string estado = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null)
        {
            return Respuestas.Json(_cRecepcionSinEnviosService.CambiarEstado(num, estado, ejercicio, empresa));
        }

        /// <summary>
        /// Permite cambiar las unidades traspasadas y las unidades a cambiar dé una línea,
        /// </summary>
        [HttpPut]
        [Route("cambiar-unidades-traspasadas")]
        [Consume("multipart/form-data")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage CambiarUnidadesTraspasadas([Obligatorio] string num = null, int linea = 0, decimal udstraspasadas = 0, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null, decimal udsTotalesTraspasadas = 0)
        {
            return Respuestas.Json(_serviceDetails.CambiarUnidadesTraspasadas(num, linea, udstraspasadas, ejercicio, empresa, udsTotalesTraspasadas));
        }

        /// <summary>
        /// Permite cambiar las unidades traspasadas y las unidades a cambiar dé una línea,
        /// </summary>
        [HttpPut]
        [Route("cambiar-unidades-traspasadas-sinenvios")]
        [Consume("multipart/form-data")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage CambiarUnidadesTraspasadasSinEnvios([Obligatorio] string num = null, int linea = 0, decimal udstraspasadas = 0, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null, decimal udsTotalesTraspasadas = 0)
        {
            return Respuestas.Json(_dRecepcionSinEnviosService.CambiarUnidadesTraspasadas(num, linea, udstraspasadas, ejercicio, empresa, udsTotalesTraspasadas));
        }
    }
}
