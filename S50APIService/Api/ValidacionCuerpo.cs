using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.ModelBinding;

namespace S50APIService.Api
{
    /// <summary>
    /// La validación automática de ASP.NET Core para el cuerpo JSON de una acción ([FromBody]): con Nullable activado en
    /// interface.s50c, el cuerpo y sus textos y listas son obligatorios; solo fallan si faltan (null), porque el [Required]
    /// implícito admite textos vacíos.
    /// </summary>
    internal static class ValidacionCuerpo
    {
        /// <summary>
        /// El 415 si la petición no dice que su cuerpo es JSON, el 400 con los errores en el mismo orden que en
        /// interface.s50c, o null si el cuerpo es válido. Sin cuerpo, "" y "request"; con JSON no válido, "request" y la
        /// ruta del error (FormateadorJson la deja en el ModelState como "request.$..."), salvo si es la raíz "$", que va
        /// antes; con un campo sin valor, los que faltan por longitud del nombre y luego alfabético.
        /// <paramref name="nombre"/> es el del parámetro de la acción en interface.s50c.
        /// <paramref name="camposObligatorios"/> es false para los modelos de interface.s50c con "#nullable disable" (los
        /// generados de la base de datos), que admiten cualquier campo sin valor.
        /// </summary>
        public static HttpResponseMessage Error(HttpRequestMessage peticion, object request, ModelStateDictionary modelState, string nombre = "request",
            bool camposObligatorios = true)
        {
            string tipo = peticion.Content?.Headers.ContentType?.MediaType;
            if (!string.Equals(tipo, "application/json", StringComparison.OrdinalIgnoreCase) && !string.Equals(tipo, "text/json", StringComparison.OrdinalIgnoreCase))
                return Respuestas.TipoNoAdmitido();

            var errores = new Dictionary<string, string[]>();
            if (request == null)
            {
                var errorJson = modelState.FirstOrDefault(e => e.Value.Errors.Count > 0);
                if (errorJson.Key == null)
                {
                    errores[""] = new[] { "A non-empty request body is required." };
                    errores[nombre] = new[] { $"The {nombre} field is required." };
                }
                else
                {
                    string ruta = errorJson.Key.StartsWith(nombre + ".") ? errorJson.Key.Substring(nombre.Length + 1) : errorJson.Key;
                    var mensajes = errorJson.Value.Errors.Select(e => e.ErrorMessage).ToArray();
                    bool enRaiz = ruta.IndexOfAny(new[] { '.', '[' }) < 0;
                    if (enRaiz)
                        errores[ruta] = mensajes;
                    errores[nombre] = new[] { $"The {nombre} field is required." };
                    if (!enRaiz)
                        errores[ruta] = mensajes;
                }
            }
            else if (camposObligatorios)
            {
                var sinValor = request.GetType().GetProperties()
                    .Where(p => !p.PropertyType.IsValueType && p.GetValue(request) == null)
                    .Select(p => p.Name)
                    .OrderBy(n => n.Length).ThenBy(n => n, StringComparer.OrdinalIgnoreCase);
                foreach (string campo in sinValor)
                    errores[campo] = new[] { $"The {campo} field is required." };
            }
            return errores.Count == 0 ? null : Respuestas.ErrorValidacion(errores);
        }
    }
}
