using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/{year:int}/CabAlbaranTraspaso")]
    [Autorizar]
    public sealed class CabAlbaranTraspasoController : ApiController
    {
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(CAlbatr))]
        public HttpResponseMessage Create(string year, [FromBody] CAlbatr item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                CAlbatrService.Add(year, item);
                return Respuestas.Texto(item.Numero);
            });
        }

        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(CAlbatr))]
        public HttpResponseMessage Delete(string year, [FromBody] CAlbatr item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                var itemDeleting = CAlbatrService.Select(year, item.Empresa, item.Numero);
                if (itemDeleting == null)
                    return Respuestas.NoEncontrado(item);

                CAlbatrService.Remove(year, itemDeleting);
                return Respuestas.Json(item);
            });
        }
    }
}
