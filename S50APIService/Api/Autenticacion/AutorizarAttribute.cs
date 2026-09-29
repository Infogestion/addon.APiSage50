using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace S50APIService.Api.Autenticacion
{
    /// <summary>
    /// Equivale a [Authorize] con JwtBearer en interface.s50c. Si no hay usuario, responde como el "challenge" de JwtBearer:
    /// 401 sin cuerpo y "WWW-Authenticate: Bearer" (con error="invalid_token" y su descripción si el token no era válido).
    /// </summary>
    public sealed class AutorizarAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(HttpActionContext contexto)
        {
            var respuesta = new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new ByteArrayContent(new byte[0]) };
            respuesta.Content.Headers.ContentLength = 0;

            string parametro = null;
            if (contexto.Request.Properties.TryGetValue(ManejadorJwt.ClaveErrorToken, out object descripcion))
            {
                parametro = "error=\"invalid_token\"";
                if (descripcion is string texto)
                    parametro += ", error_description=\"" + texto + "\"";
            }
            respuesta.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Bearer", parametro));
            contexto.Response = respuesta;
        }
    }
}
