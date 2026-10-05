using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/Operarios")]
    [Autorizar]
    public sealed class OperariosController : ApiController
    {
        /// <summary>Permite obtener todos los operarios</summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<Operario>))]
        public HttpResponseMessage Get()
        {
            return Respuestas.Json(OperariosService.Select());
        }
    }
}
