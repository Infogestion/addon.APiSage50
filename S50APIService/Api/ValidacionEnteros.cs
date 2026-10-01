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
    /// Los parámetros int de la query (p. ej. page y pageSize) se validan como en ASP.NET Core: si el valor no es un entero,
    /// 400 con "The value 'x' is not valid." (o "is invalid." si viene vacío), en vez de seguir con el valor por defecto
    /// como hace Web API 2. Se convierte el primer valor si el parámetro se repite, pero el mensaje los muestra todos.
    /// </summary>
    public sealed class ValidacionEnteros : ActionFilterAttribute
    {
        private static readonly TypeConverter Conversor = TypeDescriptor.GetConverter(typeof(int));

        public override void OnActionExecuting(HttpActionContext contexto)
        {
            var query = contexto.Request.GetQueryNameValuePairs().ToList();
            var errores = new Dictionary<string, string[]>();

            foreach (var parametro in contexto.ActionDescriptor.GetParameters())
            {
                bool admiteNull = Nullable.GetUnderlyingType(parametro.ParameterType) == typeof(int);
                if (parametro.ParameterType != typeof(int) && !admiteNull)
                    continue;

                string nombre = parametro.ParameterName;
                if (contexto.ControllerContext.RouteData.Values.ContainsKey(nombre))
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
                contexto.Response = Respuestas.ErrorValidacion(errores);
        }
    }
}
