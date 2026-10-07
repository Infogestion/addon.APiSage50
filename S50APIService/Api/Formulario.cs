using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace S50APIService.Api
{
    /// <summary>Un fichero de un formulario multipart/form-data: el campo en el que viene, su nombre y su contenido.</summary>
    public sealed class FicheroFormulario
    {
        public string Campo { get; set; }
        public string Nombre { get; set; }
        public byte[] Contenido { get; set; }
    }

    /// <summary>
    /// Lee el formulario de las acciones con [Consume("multipart/form-data")] como ASP.NET Core: si está mal formado
    /// responde su mismo 400 ("Failed to read the request form. ...") y, si no, deja sus ficheros para la acción.
    /// Sin Content-Type o sin cuerpo no hay formulario y la petición sigue.
    /// </summary>
    internal static class Formulario
    {
        private const string Clave = "S50APIService.Formulario";

        /// <summary>Los ficheros de ese campo del formulario, en el orden en que vienen.</summary>
        public static List<FicheroFormulario> Ficheros(HttpRequestMessage peticion, string campo)
        {
            peticion.Properties.TryGetValue(Clave, out object ficheros);
            return ((List<FicheroFormulario>)ficheros ?? new List<FicheroFormulario>())
                .Where(f => string.Equals(f.Campo, campo, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>Lee el formulario de la petición. Devuelve null si se puede seguir y, si no, la respuesta de error.</summary>
        public static async Task<HttpResponseMessage> Leer(HttpRequestMessage peticion)
        {
            var ficheros = new List<FicheroFormulario>();
            peticion.Properties[Clave] = ficheros;

            var tipo = peticion.Content?.Headers.ContentType;
            if (tipo == null || !string.Equals(tipo.MediaType, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
                return null;

            string cuerpo = Encoding.GetEncoding("ISO-8859-1").GetString(await peticion.Content.ReadAsByteArrayAsync());
            if (cuerpo.Length == 0)
                return null;

            string boundary = tipo.Parameters.FirstOrDefault(p => string.Equals(p.Name, "boundary", StringComparison.OrdinalIgnoreCase))?.Value;
            if (string.IsNullOrWhiteSpace(boundary?.Trim('"')))
                return Error("Missing content-type boundary.");

            // Un formulario sin partes (solo el cierre) es válido en ASP.NET Core, pero ReadAsMultipartAsync lo rechaza.
            string inicio = "--" + boundary.Trim('"');
            int primero = cuerpo.IndexOf(inicio, StringComparison.Ordinal);
            if (primero >= 0 && string.CompareOrdinal(cuerpo, primero + inicio.Length, "--", 0, 2) == 0)
                return null;

            MultipartMemoryStreamProvider partes;
            try
            {
                partes = await peticion.Content.ReadAsMultipartAsync();
            }
            catch (Exception)
            {
                return Error("Unexpected end of Stream, the content may have already been read by another component. ");
            }

            foreach (var parte in partes.Contents)
            {
                if (!parte.Headers.Contains("Content-Disposition"))
                    return Error("Form section has invalid Content-Disposition value: ");

                var disposicion = parte.Headers.ContentDisposition;
                string nombre = disposicion?.FileNameStar ?? disposicion?.FileName;
                if (nombre == null || !string.Equals(disposicion.DispositionType, "form-data", StringComparison.OrdinalIgnoreCase))
                    continue;

                ficheros.Add(new FicheroFormulario
                {
                    Campo = (disposicion.Name ?? "").Trim('"'),
                    Nombre = nombre.Trim('"'),
                    Contenido = await parte.ReadAsByteArrayAsync(),
                });
            }
            return null;
        }

        private static HttpResponseMessage Error(string detalle)
        {
            return Respuestas.ErrorValidacion(new Dictionary<string, string[]> { [""] = new[] { "Failed to read the request form. " + detalle } });
        }
    }
}
