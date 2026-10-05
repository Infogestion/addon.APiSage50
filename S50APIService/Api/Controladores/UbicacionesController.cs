using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/{year:int}/Ubicaciones")]
    [Autorizar]
    public sealed class UbicacionesController : ApiController
    {
        /// <summary>
        /// Permite obtener las ubicaciones de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<Ubicaciones>))]
        public HttpResponseMessage Get(string year, int page, int pagesize)
        {
            return Respuestas.Json(UbicacionService.Select(page, pagesize));
        }

        /// <summary>
        /// Permite obtener una ubicación concreta
        /// </summary>
        [HttpGet]
        [Route("{code}")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage GetByCode(string year, string code)
        {
            return Respuestas.JsonONada(UbicacionService.SelectByCode(code));
        }

        /// <summary>
        /// Permite obtener los artículos de una ubicación y sus almacenes
        /// </summary>
        [HttpGet]
        [Route("rel/{code}/{emp}")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage GetByArticuloRel(string year, string code, string emp)
        {
            return Respuestas.Json(UbicacionService.SelectByArticuloRel(year, code, emp));
        }

        /// <summary>
        /// Permite obtener las ubicaciones por almacén y stock de un artículo
        /// </summary>
        [HttpGet]
        [Route("rel-art/{code}/{emp}")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage GetByArticuloRelAr(string year, string code, string emp)
        {
            return Respuestas.Json(UbicacionService.SelectAllArticle(year, code, emp));
        }
    }
}
