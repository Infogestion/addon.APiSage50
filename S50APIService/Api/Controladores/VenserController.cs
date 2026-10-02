using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;

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
            var invalido = Respuestas.FaltanObligatorios(("albaran", albaran), ("empresa", empresa));
            if (invalido != null)
                return invalido;

            return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Venser>(year, "venser",
                "LTRIM(RTRIM([EMPRESA])) = @empresa AND LTRIM(RTRIM([ALBARAN])) = @albaran",
                new Dictionary<string, string> { ["@empresa"] = Valor(empresa), ["@albaran"] = Valor(albaran) }));
        }

        [HttpGet]
        [Route("item/{empresa}/{albaran}/{linea:int}/{serie}")]
        [ResponseType(typeof(Venser))]
        public HttpResponseMessage GetItem(string year, string empresa, string albaran, int linea, string serie)
        {
            var invalido = Respuestas.FaltanObligatorios(("serie", serie), ("albaran", albaran), ("empresa", empresa));
            if (invalido != null)
                return invalido;

            var item = Contexto.Lector.LeerEjercicio<Venser>(year, "venser",
                "LTRIM(RTRIM([EMPRESA])) = @empresa AND LTRIM(RTRIM([ALBARAN])) = @albaran AND [LINEA] = @linea AND LTRIM(RTRIM([SERIE])) = @serie",
                new Dictionary<string, string>
                {
                    ["@empresa"] = Valor(empresa),
                    ["@albaran"] = Valor(albaran),
                    ["@linea"] = linea.ToString(CultureInfo.InvariantCulture),
                    ["@serie"] = Valor(serie),
                }).FirstOrDefault();
            return item == null ? Request.CreateResponse(HttpStatusCode.NoContent) : Respuestas.Json(item);
        }

        /// <summary>interface.s50c vuelve a decodificar los valores de la ruta (UrlDecode) y les quita los espacios de los lados.</summary>
        private static string Valor(string deLaRuta)
        {
            return WebUtility.UrlDecode(deLaRuta).Trim();
        }
    }
}
