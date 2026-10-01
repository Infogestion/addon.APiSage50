using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/{year:int}/Almacenes")]
    [Autorizar]
    public sealed class AlmacenesController : ApiController
    {
        /// <summary>
        /// Permite obtener todos los almacenes
        /// </summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<Almacen>))]
        public HttpResponseMessage Get(string year)
        {
            return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Almacen>(year, "almacen"));
        }

        /// <summary>
        /// Permite obtener un almacen
        /// </summary>
        [HttpGet]
        [Route("GetAlmacen")]
        [ResponseType(typeof(List<Almacen>))]
        public HttpResponseMessage GetAlmacen(string year, string codigo = null)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return Respuestas.ErrorValidacion(new Dictionary<string, string[]> { ["codigo"] = new[] { "The codigo field is required." } });

            var almacen = Contexto.Lector.LeerEjercicio<Almacen>(year, "almacen", "LTRIM(RTRIM([CODIGO])) = @codigo",
                new Dictionary<string, string> { ["@codigo"] = codigo.Trim() }).FirstOrDefault();
            return almacen == null ? Request.CreateResponse(HttpStatusCode.NoContent) : Respuestas.Json(almacen);
        }
    }
}
