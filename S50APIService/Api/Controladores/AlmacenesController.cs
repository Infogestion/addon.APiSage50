using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

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
            return Respuestas.Json(AlmacenesService.Select(year));
        }

        /// <summary>
        /// Permite obtener un almacen
        /// </summary>
        [HttpGet]
        [Route("GetAlmacen")]
        [ResponseType(typeof(List<Almacen>))]
        public HttpResponseMessage GetAlmacen(string year, [Obligatorio] string codigo = null)
        {
            return Respuestas.JsonONada(AlmacenesService.Select(year, codigo));
        }
    }
}
