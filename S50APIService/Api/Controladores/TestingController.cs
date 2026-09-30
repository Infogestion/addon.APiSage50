using System.Net.Http;
using System.Web.Http;
using S50APIService.Api.Autenticacion;

namespace S50APIService.Api.Controladores
{
    // TestingController de interface.s50c.
    [RoutePrefix("api/testing")]
    public sealed class TestingController : ApiController
    {
        /// <summary>Prueba de Hello sin autentificación</summary>
        [HttpGet]
        [Route("hello")]
        public HttpResponseMessage Hello()
        {
            return Respuestas.Texto("Hello!");
        }

        /// <summary>Prueba de Hello con autentificación</summary>
        [HttpGet]
        [Route("helloAuth")]
        [Autorizar]
        public HttpResponseMessage HelloAuth()
        {
            return Respuestas.Texto("Hello with authorization!");
        }
    }
}
