using System.Net;
using System.Net.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using S50APIService.Sage;

namespace S50APIService.Api
{
    /// <summary>
    /// Respuestas de las excepciones que no son errores del servicio. Equivale al catch (DBNotFoundException) que repite
    /// cada controlador de interface.s50c: si la base de datos del {year} no existe, 404 con su mismo cuerpo.
    /// El resto sigue siendo un 500 sin cuerpo.
    /// </summary>
    public sealed class ManejadorExcepciones : ExceptionHandler
    {
        public override void Handle(ExceptionHandlerContext contexto)
        {
            contexto.Result = new ResponseMessageResult(contexto.Exception is EjercicioNoEncontradoException
                ? Respuestas.EjercicioNoEncontrado()
                : new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}
