using System.Net.Http;
using System.Web.Http;
using S50APIService.Api.Autenticacion;

namespace S50APIService.Api.Controladores
{
    /// <summary>TestingController de interface.s50c.</summary>
    [RoutePrefix("api/testing")]
    public sealed class TestingController : ApiController
    {
        [HttpGet]
        [Route("hello")]
        public HttpResponseMessage Hello()
        {
            return Respuestas.Texto("Hello!");
        }

        [HttpGet]
        [Route("helloAuth")]
        [Autorizar]
        public HttpResponseMessage HelloAuth()
        {
            return Respuestas.Texto("Hello with authorization!");
        }
    }
}
