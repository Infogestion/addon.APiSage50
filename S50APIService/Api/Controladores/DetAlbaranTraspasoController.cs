using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/{year:int}/DetAlbaranTraspaso")]
    [Autorizar]
    public sealed class DetAlbaranTraspasoController : ApiController
    {
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(DAlbatr))]
        public HttpResponseMessage Create(string year, [FromBody] DetAlbaranTraspasoRequest item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                DAlbatrService.Add(year, item);
                return Respuestas.Json(item);
            });
        }
    }
}
