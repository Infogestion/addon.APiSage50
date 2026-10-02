using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace S50APIService.Api
{
    /// <summary>
    /// Valida los parámetros de las acciones como un [ApiController] de ASP.NET Core y responde su mismo 400:
    /// los int de la query (p. ej. page y pageSize) que no son un entero, con "The value 'x' is not valid." (o "is invalid."
    /// si viene vacío), en vez de seguir con el valor por defecto como hace Web API 2; y los textos de la ruta en blanco,
    /// con "The x field is required.". Si un int se repite se convierte el primer valor, pero el mensaje los muestra todos.
    /// Los errores salen en el orden del ModelState de ASP.NET Core: por longitud del nombre y luego alfabético.
    /// </summary>
    public sealed class ValidacionParametros : ActionFilterAttribute
    {
        private static readonly TypeConverter Conversor = TypeDescriptor.GetConverter(typeof(int));

        public override void OnActionExecuting(HttpActionContext contexto)
        {
            var query = contexto.Request.GetQueryNameValuePairs().ToList();
            var ruta = contexto.ControllerContext.RouteData.Values;
            var errores = new Dictionary<string, string[]>();

            foreach (var parametro in contexto.ActionDescriptor.GetParameters())
            {
                string nombre = parametro.ParameterName;
                bool enRuta = ruta.ContainsKey(nombre);

                if (parametro.ParameterType == typeof(string))
                {
                    if (enRuta && string.IsNullOrWhiteSpace(ruta[nombre] as string))
                        errores[nombre] = new[] { $"The {nombre} field is required." };
                    continue;
                }

                bool admiteNull = Nullable.GetUnderlyingType(parametro.ParameterType) == typeof(int);
                if (enRuta || (parametro.ParameterType != typeof(int) && !admiteNull))
                    continue;

                var valores = query.Where(p => string.Equals(p.Key, nombre, StringComparison.OrdinalIgnoreCase)).Select(p => p.Value).ToList();
                if (valores.Count == 0)
                    continue;

                string todos = string.Join(",", valores);
                if (string.IsNullOrWhiteSpace(valores[0]))
                {
                    if (admiteNull)
                        contexto.ActionArguments[nombre] = null;
                    else
                        errores[nombre] = new[] { $"The value '{todos}' is invalid." };
                    continue;
                }

                try
                {
                    contexto.ActionArguments[nombre] = Conversor.ConvertFrom(null, CultureInfo.InvariantCulture, valores[0]);
                }
                catch (Exception)
                {
                    errores[nombre] = new[] { $"The value '{todos}' is not valid." };
                }
            }

            if (errores.Count > 0)
                contexto.Response = Respuestas.ErrorValidacion(errores
                    .OrderBy(e => e.Key.Length).ThenBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(e => e.Key, e => e.Value));
        }
    }
}
