using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

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

        private static HttpResponseMessage Problema(HttpStatusCode estado, string tipo, string titulo)
        {
            // Mismo orden de propiedades que el ProblemDetails de ASP.NET Core 6.
            string json = JsonConvert.SerializeObject(new
            {
                type = tipo,
                title = titulo,
                status = (int)estado,
                traceId = NuevoTraceId(),
            });
            var contenido = new StringContent(json, new UTF8Encoding(false));
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
