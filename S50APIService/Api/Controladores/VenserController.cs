using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/nadilux-repartos/{year:int}/Venser")]
    [Autorizar]
    public sealed class VenserController : ApiController
    {
        [HttpGet]
        [Route("{empresa}/{albaran}")]
        [ResponseType(typeof(List<Venser>))]
        public HttpResponseMessage GetByAlbaran(string year, string empresa, string albaran)
        {
            return Respuestas.Json(VenserService.Select(year, empresa.UrlDecode(), albaran.UrlDecode()));
        }

        [HttpGet]
        [Route("item/{empresa}/{albaran}/{linea:int}/{serie}")]
        [ResponseType(typeof(Venser))]
        public HttpResponseMessage GetItem(string year, string empresa, string albaran, int linea, string serie)
        {
            return Respuestas.JsonONada(VenserService.Select(year, empresa.UrlDecode(), albaran.UrlDecode(), linea, serie.UrlDecode()));
        }

        [HttpPost]
        [Route("")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage Post(string year, [FromBody] Venser item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                VenserService.Add(year, item);
                return Respuestas.Json(item);
            });
        }

        [HttpPut]
        [Route("")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage Put(string year, [FromBody] Venser item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item), camposObligatorios: false) ?? Escritura.Intentar(() =>
            {
                VenserService.Update(year, item);
                return Respuestas.Json(item);
            });
        }

        [HttpDelete]
        [Route("{empresa}/{albaran}/{linea:int}/{serie}")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage Delete(string year, string empresa, string albaran, int linea, string serie)
        {
            return Escritura.Intentar(() =>
            {
                var item = VenserService.Select(year, empresa.UrlDecode(), albaran.UrlDecode(), linea, serie.UrlDecode());
                if (item == null)
                    return Respuestas.ErrorNoEncontrado("Item not found");

                VenserService.AnularVenta(year, item);
                return Respuestas.Json(new { result = true });
            });
        }

        [HttpPost]
        [Route("alta-series")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage AltaSeries(string year, [FromBody] AltaVenserRequest request)
        {
            return ValidacionCuerpo.Error(Request, request, ModelState) ?? Escritura.Intentar(() =>
            {
                if (request.Series.Count == 0)
                    return Respuestas.Json(new { result = true, message = "No hay series" });

                foreach (string serieStr in request.Series)
                    VenserService.Vender(year, request, serieStr);

                return Respuestas.Json(new { result = true, message = "Series registradas en venser" });
            });
        }
    }
}
