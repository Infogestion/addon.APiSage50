using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace S50APIService.Api
{
    /// <summary>
    /// Marca un texto de la query como obligatorio (un string no anulable en interface.s50c). El parámetro se declara con
    /// "= null": si no tuviera valor por defecto, Web API 2 respondería 404 cuando falta en vez de dejar validar.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ObligatorioAttribute : Attribute
    {
    }

    /// <summary>
    /// Valida los parámetros de las acciones como un [ApiController] de ASP.NET Core y responde su mismo 400:
    /// los int, los bool, los decimal y las fechas de la query (p. ej. page, pageSize o fecha) que no se pueden convertir, con
    /// "The value 'x' is not valid." (o "is invalid." si viene vacío), en vez de seguir con el valor por defecto como hace
    /// Web API 2; y los textos en blanco, con "The x field is required.", si son de la ruta o llevan <see cref="ObligatorioAttribute"/>.
    /// Un texto opcional en blanco llega a la acción como null. Si un int o una fecha se repite se convierte el primer valor,
    /// pero el mensaje los muestra todos.
    /// Los errores salen en el orden del ModelState de ASP.NET Core: por longitud del nombre y luego alfabético.
    /// </summary>
    public sealed class ValidacionParametros : ActionFilterAttribute
    {
        private static readonly TypeConverter ConversorInt = TypeDescriptor.GetConverter(typeof(int));

        private static readonly Dictionary<Type, Func<string, object>> Conversores = new Dictionary<Type, Func<string, object>>
        {
            [typeof(int)] = valor => ConversorInt.ConvertFrom(null, CultureInfo.InvariantCulture, valor),
            [typeof(bool)] = valor => bool.Parse(valor),
            [typeof(decimal)] = valor => decimal.Parse(valor, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture),
            [typeof(DateTime)] = valor => DateTime.Parse(valor, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces),
        };

        /// <summary>Antes que los parámetros se lee el formulario: si está mal formado, ASP.NET Core solo responde ese error.</summary>
        public override async Task OnActionExecutingAsync(HttpActionContext contexto, CancellationToken cancelacion)
        {
            var admitido = contexto.ActionDescriptor.GetCustomAttributes<ConsumeAttribute>().FirstOrDefault();
            if (admitido != null && admitido.Tipo == "multipart/form-data")
                contexto.Response = await Formulario.Leer(contexto.Request);
            if (contexto.Response == null)
                OnActionExecuting(contexto);
        }

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
                    contexto.ActionArguments.TryGetValue(nombre, out object texto);
                    if (!string.IsNullOrWhiteSpace(enRuta ? ruta[nombre] as string : texto as string))
                        continue;
                    if (enRuta || parametro.GetCustomAttributes<ObligatorioAttribute>().Count > 0)
                        errores[nombre] = new[] { $"The {nombre} field is required." };
                    else
                        contexto.ActionArguments[nombre] = null;
                    continue;
                }

                var tipo = Nullable.GetUnderlyingType(parametro.ParameterType);
                bool admiteNull = tipo != null;
                if (enRuta || !Conversores.TryGetValue(tipo ?? parametro.ParameterType, out var conversor))
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
                    contexto.ActionArguments[nombre] = conversor(valores[0]);
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
