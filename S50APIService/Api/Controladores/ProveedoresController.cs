using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/Proveedores")]
    [Autorizar]
    public sealed class ProveedoresController : ApiController
    {
        /// <summary>
        /// Permite obtener todos los Proveedores
        /// </summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<Proveed>))]
        public HttpResponseMessage Get(int page = 1, int pageSize = 100)
        {
            // interface.s50c recibe la página pero no pagina: devuelve siempre todos.
            return Respuestas.Json(ProveedoresService.Select());
        }

        /// <summary>
        /// Permite obtener un proveedor
        /// </summary>
        [HttpGet]
        [Route("ByCode")]
        [ResponseType(typeof(Proveed))]
        public HttpResponseMessage GetByCode([Obligatorio] string code = null)
        {
            return Respuestas.JsonONada(ProveedoresService.Select(code));
        }
    }
}
