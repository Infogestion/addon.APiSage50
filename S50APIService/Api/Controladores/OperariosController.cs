using System.Collections.Generic;
using System.Web.Http;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/Operarios")]
    [Autorizar]
    public sealed class OperariosController : ApiController
    {
        /// <summary>Permite obtener todos los operarios</summary>
        [HttpGet]
        [Route("")]
        public List<Operario> Get()
        {
            return Contexto.Lector.Leer<Operario>("COMUNES", "operario");
        }
    }
}
