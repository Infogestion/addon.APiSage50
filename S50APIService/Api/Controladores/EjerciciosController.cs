using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/Ejercicios")]
    [Autorizar]
    public sealed class EjerciciosController : ApiController
    {
        /// <summary>
        /// Permite obtener todos los años acumulados hasta hoy.
        /// Estos son los años que puede especificar en cada endpoint (que lo especifique) para realizar la consulta sobre dicho año.
        /// Cada año representa a una DB de Sage50c por ejercicio.
        /// Esta lista no implica que la DB exista en el servidor.
        /// Recuerde que la DB debe existir en el servidor para poder ser consultada. En caso de que no se encuentre se le espeficicará en el endpoint correspondiente.
        /// </summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<Ejercici>))]
        public HttpResponseMessage Get()
        {
            return Respuestas.Json(EjercicioService.Select());
        }

        /// <summary>
        /// Permite obtener el año predeterminado.
        /// </summary>
        [HttpGet]
        [Route("Default")]
        [ResponseType(typeof(Ejercici))]
        public HttpResponseMessage GetDefault()
        {
            return Respuestas.JsonONada(EjercicioService.SelectDefault());
        }
    }
}
