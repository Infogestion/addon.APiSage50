using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.ModelBinding;
using S50APIService.Api.Modelos;

namespace S50APIService.Api
{
    /// <summary>
    /// La validación automática de ASP.NET Core para el cuerpo de los login de Nadilux: con Nullable activado en
    /// interface.s50c, el cuerpo y sus dos campos son obligatorios; solo falla si faltan (null), porque el [Required]
    /// implícito admite textos vacíos.
    /// </summary>
    internal static class ValidacionLogin
    {
        /// <summary>
        /// El 400 con los errores en el mismo orden que en interface.s50c, o null si el login es válido. Sin cuerpo, "" y
        /// "request"; con JSON no válido, "request" y la ruta del error (FormateadorJson la deja en el ModelState como
        /// "request.$..."), salvo si es la raíz "$", que va antes.
        /// </summary>
        public static HttpResponseMessage Error(LoginNadRepartosRequest request, ModelStateDictionary modelState)
        {
            var errores = new Dictionary<string, string[]>();
            if (request == null)
            {
                var errorJson = modelState.FirstOrDefault(e => e.Value.Errors.Count > 0);
                if (errorJson.Key == null)
                {
                    errores[""] = new[] { "A non-empty request body is required." };
                    errores["request"] = new[] { "The request field is required." };
                }
                else
                {
                    string ruta = errorJson.Key.StartsWith("request.") ? errorJson.Key.Substring("request.".Length) : errorJson.Key;
                    var mensajes = errorJson.Value.Errors.Select(e => e.ErrorMessage).ToArray();
                    bool enRaiz = ruta.IndexOfAny(new[] { '.', '[' }) < 0;
                    if (enRaiz)
                        errores[ruta] = mensajes;
                    errores["request"] = new[] { "The request field is required." };
                    if (!enRaiz)
                        errores[ruta] = mensajes;
                }
            }
            else
            {
                if (request.Password == null)
                    errores["Password"] = new[] { "The Password field is required." };
                if (request.Username == null)
                    errores["Username"] = new[] { "The Username field is required." };
            }
            return errores.Count == 0 ? null : Respuestas.ErrorValidacion(errores);
        }
    }
}
