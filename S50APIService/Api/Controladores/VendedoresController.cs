using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/Vendedores")]
    [Autorizar]
    public sealed class VendedoresController : ApiController
    {
        /// <summary>
        /// Permite obtener todos los Vendedores
        /// </summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<Vendedor>))]
        public HttpResponseMessage Get()
        {
            return Respuestas.Json(VendedorService.Select());
        }

        /// <summary>
        /// Permite obtener un vendedor
        /// </summary>
        [HttpGet]
        [Route("ByCode")]
        [ResponseType(typeof(Vendedor))]
        public HttpResponseMessage GetByCode([Obligatorio] string code = null)
        {
            return Respuestas.JsonONada(VendedorService.Select(code));
        }
    }
}
