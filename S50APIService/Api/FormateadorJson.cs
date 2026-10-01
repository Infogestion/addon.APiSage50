using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace S50APIService.Api
{
    /// <summary>
    /// JSON con System.Text.Json y las mismas opciones que ASP.NET Core (JsonSerializerDefaults.Web), en vez de Newtonsoft,
    /// para que las respuestas salgan byte a byte como en interface.s50c: nombres en camelCase, caracteres especiales
    /// escapados igual, mismo formato de fechas y decimales. También lee los cuerpos de las peticiones
    /// igual que ASP.NET Core (nombres sin distinguir mayúsculas y minúsculas).
    /// </summary>
    public sealed class FormateadorJson : MediaTypeFormatter
    {
        /// <summary>Las opciones de ASP.NET Core MVC: su codificador escapa U+00A0 pero no las letras acentuadas.</summary>
        public static readonly JsonSerializerOptions Opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public FormateadorJson()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));
            SupportedEncodings.Add(new UTF8Encoding(false));
        }

        public override bool CanReadType(Type type) => true;
        public override bool CanWriteType(Type type) => true;

        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            return JsonSerializer.SerializeAsync(writeStream, value, type, Opciones);
        }

        public override async Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            if (content?.Headers.ContentLength == 0)
                return null;
            try
            {
                return await JsonSerializer.DeserializeAsync(readStream, type, Opciones);
            }
            catch (JsonException ex)
            {
                formatterLogger?.LogError(ex.Path ?? "$", ex.Message);
                return null;
            }
        }
    }
}
