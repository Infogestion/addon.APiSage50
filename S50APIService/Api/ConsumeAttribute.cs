using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace S50APIService.Api
{
    /// <summary>
    /// [Consumes] de ASP.NET Core: la acción solo admite ese Content-Type. Lo comprueba <see cref="ValidacionContentType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ConsumeAttribute : Attribute
    {
        public string Tipo { get; }

        public ConsumeAttribute(string tipo)
        {
            Tipo = tipo;
        }
    }

    /// <summary>
    /// Responde 415 sin cuerpo si la petición trae un Content-Type que no es el del <see cref="ConsumeAttribute"/> de la acción.
    /// Sin Content-Type la petición se acepta. Es un filtro global para que se ejecute antes de comprobar el token, como en
    /// interface.s50c: allí el 415 sale antes que el 401 y que el 400 de los parámetros.
    /// </summary>
    public sealed class ValidacionContentType : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext contexto)
        {
            var admitido = contexto.ActionDescriptor.GetCustomAttributes<ConsumeAttribute>().FirstOrDefault();
            var cabeceras = contexto.Request.Content?.Headers;
            if (admitido == null || cabeceras == null || !cabeceras.TryGetValues("Content-Type", out var valores) || valores.All(string.IsNullOrWhiteSpace))
                return;
            if (!string.Equals(cabeceras.ContentType?.MediaType, admitido.Tipo, StringComparison.OrdinalIgnoreCase))
                contexto.Response = new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType);
        }
    }
}
