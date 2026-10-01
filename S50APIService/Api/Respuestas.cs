using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace S50APIService.Api
{
    /// <summary>Respuestas con el mismo formato exacto que produce ASP.NET Core en interface.s50c.</summary>
    internal static class Respuestas
    {
        /// <summary>Ok(string) en ASP.NET Core: el texto tal cual, como text/plain.</summary>
        public static HttpResponseMessage Texto(string texto)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(texto, new UTF8Encoding(false), "text/plain"),
            };
        }

        /// <summary>
        /// Unauthorized() dentro de un [ApiController]: ASP.NET Core lo convierte en un ProblemDetails
        /// (application/problem+json) con el tipo de la RFC 7235 y un traceId.
        /// </summary>
        public static HttpResponseMessage NoAutorizado()
        {
            return Problema(HttpStatusCode.Unauthorized, "https://tools.ietf.org/html/rfc7235#section-3.1", "Unauthorized");
        }

        /// <summary>Ok(objeto) en ASP.NET Core: el objeto como application/json; charset=utf-8.</summary>
        public static HttpResponseMessage Json(object valor)
        {
            var contenido = new StringContent(JsonSerializer.Serialize(valor, FormateadorJson.Opciones), new UTF8Encoding(false));
            contenido.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = contenido };
        }

        /// <summary>Como <see cref="Json"/>, con el JSON ya escrito en bloques (los listados de <see cref="Sage.LectorSage.LeerJson{T}"/>).</summary>
        public static HttpResponseMessage JsonBloques(List<byte[]> bloques)
        {
            var contenido = new ContenidoBloques(bloques);
            contenido.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = contenido };
        }

        /// <summary>
        /// El 400 automático de un [ApiController] cuando el modelo no es válido (ValidationProblemDetails):
        /// <paramref name="errores"/> son los campos con sus mensajes, en el orden en que se muestran.
        /// </summary>
        public static HttpResponseMessage ErrorValidacion(IDictionary<string, string[]> errores)
        {
            return Problema(HttpStatusCode.BadRequest, "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "One or more validation errors occurred.", errores);
        }

        /// <summary>
        /// ValidationProblem(DBNotFoundException.Message, null, 404) de interface.s50c: la base de datos del ejercicio de la
        /// ruta ({year}) no existe. Lleva "detail" y un "errors" vacío.
        /// </summary>
        public static HttpResponseMessage EjercicioNoEncontrado()
        {
            return Problema(HttpStatusCode.NotFound, "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                "One or more validation errors occurred.", new Dictionary<string, string[]>(), "The db for the requested year does not exist");
        }

        private static HttpResponseMessage Problema(HttpStatusCode estado, string tipo, string titulo, IDictionary<string, string[]> errores = null, string detalle = null)
        {
            var problema = new Dictionary<string, object>
            {
                ["type"] = tipo,
                ["title"] = titulo,
                ["status"] = (int)estado,
            };
            if (detalle != null)
                problema["detail"] = detalle;
            problema["traceId"] = NuevoTraceId();
            if (errores != null)
                problema["errors"] = errores;
            var contenido = new StringContent(JsonSerializer.Serialize(problema, FormateadorJson.Opciones), new UTF8Encoding(false));
            contenido.Headers.ContentType = new MediaTypeHeaderValue("application/problem+json") { CharSet = "utf-8" };
            return new HttpResponseMessage(estado) { Content = contenido };
        }

        /// <summary>Formato W3C que usa ASP.NET Core 6 (Activity.Id): 00-{32 hex}-{16 hex}-00.</summary>
        private static string NuevoTraceId()
        {
            string Hex(int bytes)
            {
                var b = Guid.NewGuid().ToByteArray();
                return BitConverter.ToString(b, 0, bytes).Replace("-", "").ToLowerInvariant();
            }
            return $"00-{Hex(16)}-{Hex(8)}-00";
        }
    }
}
