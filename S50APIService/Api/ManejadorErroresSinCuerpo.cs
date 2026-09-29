using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace S50APIService.Api
{
    /// <summary>
    /// Web API 2 acompaña sus propios errores (ruta inexistente, método no permitido, excepción no controlada) de un JSON
    /// {"Message": ...}. ASP.NET Core, en interface.s50c, responde a esos casos con el mismo código pero sin cuerpo,
    /// y en el 405 añade la cabecera Allow con los métodos que sí admite la ruta.
    /// Este manejador ajusta esas respuestas; no toca las que devuelven los controladores.
    /// </summary>
    public sealed class ManejadorErroresSinCuerpo : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage peticion, CancellationToken cancelacion)
        {
            var respuesta = await base.SendAsync(peticion, cancelacion);
            if (respuesta.Content is ObjectContent contenido && contenido.Value is HttpError)
            {
                respuesta.Content = new ByteArrayContent(new byte[0]);
                respuesta.Content.Headers.ContentLength = 0;
                if (respuesta.StatusCode == HttpStatusCode.MethodNotAllowed)
                    foreach (string metodo in MetodosPermitidos(peticion))
                        respuesta.Content.Headers.Allow.Add(metodo);
            }
            return respuesta;
        }

        /// <summary>Métodos de las acciones cuya ruta coincide con la URL pedida.</summary>
        private static string[] MetodosPermitidos(HttpRequestMessage peticion)
        {
            string raiz = peticion.GetRequestContext()?.VirtualPathRoot ?? "/";
            return peticion.GetConfiguration().Services.GetApiExplorer().ApiDescriptions
                .Where(d => d.Route.GetRouteData(raiz, peticion) != null)
                .Select(d => d.HttpMethod.Method)
                .Distinct()
                .ToArray();
        }
    }
}
