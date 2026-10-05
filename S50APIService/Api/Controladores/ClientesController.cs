using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/Clientes")]
    [Autorizar]
    public sealed class ClientesController : ApiController
    {
        /// <summary>
        /// Permite obtener todos los Clientes
        /// </summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<Clientes>))]
        public HttpResponseMessage Get()
        {
            return Respuestas.Json(ClientesService.Select());
        }

        /// <summary>
        /// Permite obtener un cliente
        /// </summary>
        [HttpGet]
        [Route("ByCode")]
        [ResponseType(typeof(Clientes))]
        public HttpResponseMessage GetByCode([Obligatorio] string code = null)
        {
            return Respuestas.JsonONada(ClientesService.Select(code));
        }

        /// <summary>
        /// Permite obtener un cliente
        /// </summary>
        [HttpGet]
        [Route("Emails")]
        [ResponseType(typeof(ClienteConEmails))]
        public HttpResponseMessage GetEmails([Obligatorio] string code = null)
        {
            return Respuestas.Json(ClientesService.SelectEmails(code));
        }
    }
}
