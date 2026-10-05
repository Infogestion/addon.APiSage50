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
    }
}
