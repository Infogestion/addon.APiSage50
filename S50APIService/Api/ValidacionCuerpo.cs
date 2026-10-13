using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
        /// generados de la base de datos), que admiten cualquier campo sin valor salvo los que el modelo marca con [Required]
        /// o [StringLength].
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
            else
            {
                var anotados = request.GetType().GetProperties().OrderBy(p => p.Name.Length).ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
                foreach (var campo in anotados)
                {
                    var mensajes = ErroresDeAnotaciones(campo, campo.GetValue(request) as string).ToArray();
                    if (mensajes.Length > 0)
                        errores[campo.Name] = mensajes;
                }
            }
            return errores.Count == 0 ? null : Respuestas.ErrorValidacion(errores);
        }

        /// <summary>
        /// Los mensajes de ASP.NET Core para un texto que no cumple el [Required] o el [StringLength] de su propiedad. Se
        /// escriben aquí porque los de .NET Framework salen en el idioma de Windows.
        /// </summary>
        private static IEnumerable<string> ErroresDeAnotaciones(PropertyInfo campo, string texto)
        {
            if (campo.GetCustomAttribute<RequiredAttribute>() != null && string.IsNullOrWhiteSpace(texto))
                yield return $"The {campo.Name} field is required.";

            var longitud = campo.GetCustomAttribute<StringLengthAttribute>();
            if (longitud != null && texto != null && (texto.Length < longitud.MinimumLength || texto.Length > longitud.MaximumLength))
                yield return $"The field {campo.Name} must be a string with a minimum length of {longitud.MinimumLength} and a maximum length of {longitud.MaximumLength}.";
        }
    }
}
