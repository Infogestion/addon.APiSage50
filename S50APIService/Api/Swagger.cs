using System;
using System.Threading.Tasks;
using NSwag;
using NSwag.AspNet.Owin;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using Owin;

namespace S50APIService.Api
{
    /// <summary>
    /// Swagger con las mismas direcciones y el mismo contenido principal que interface.s50c (Swashbuckle):
    /// "/" y "/docs" → 302 a /swagger; /swagger → 301 a swagger/index.html; documento en /swagger/v1/swagger.json.
    /// </summary>
    internal static class Swagger
    {
        private const string RutaDocumento = "/swagger/v1/swagger.json";

        /// <summary>Versión que se muestra en la descripción de Swagger.</summary>
        public const string VersionApi = "1.1.4";
        public const string TipoVersionApi = "rc";

        public static void Configurar(IAppBuilder app)
        {
            // Llamada explícita a la extensión de OWIN: app.Use(método) elegiría la sobrecarga Use(object).
            AppBuilderUseExtensions.Use(app, Redirecciones);

            app.UseOpenApi(typeof(Swagger).Assembly, s =>
            {
                s.DocumentPath = RutaDocumento;
                Generador(s.GeneratorSettings);
                s.PostProcess = documento => documento.Servers.Clear();
            });
            app.UseSwaggerUi(typeof(Swagger).Assembly, s =>
            {
                s.Path = "/swagger";
                s.DocumentPath = RutaDocumento;
                // Ruta absoluta: NSwag convierte una relativa ("v1/swagger.json") en "/v1/swagger.json", que no existe.
                s.SwaggerRoutes.Add(new SwaggerUiRoute("interface.s50c.WebAPI v1", RutaDocumento));
                s.DocExpansion = "none";
                Generador(s.GeneratorSettings);
            });
        }

        private static void Generador(NSwag.Generation.WebApi.WebApiOpenApiDocumentGeneratorSettings g)
        {
            g.Title = "interface.s50c";
            g.Version = "v1";
            string nl = Environment.NewLine;
            g.Description = $"v{VersionApi}{TipoVersionApi}{nl}{nl}"
                + $"Todas las llamadas, excepto 'POST/api/token', tienen Autorización mediante BearerToken siendo enviado en la cabecera HTTP de cada endpoint.{nl}\r\n"
                + $"Para obtener dicho Token debe utilizar 'POST /api/token/{{username}}/{{password}}'.{nl}. Duración del token 24h.\r\n"
                + $"https://datatracker.ietf.org/doc/html/rfc6750#section-2.1{nl}\r\n";
            g.DocumentProcessors.Add(new SeguridadBearer());
            g.SchemaSettings = new NJsonSchema.Generation.SystemTextJsonSchemaGeneratorSettings
            {
                SchemaType = NJsonSchema.SchemaType.OpenApi3,
                SerializerOptions = FormateadorJson.Opciones,
            };
        }

        /// <summary>
        /// Esquema "Bearer" y requisito global de seguridad, como AddSecurityDefinition/AddSecurityRequirement.
        /// Diferencias conocidas con Swashbuckle (solo de documentación): NSwag escribe "openapi": "3.0.0" y añade operationId.
        /// </summary>
        private sealed class SeguridadBearer : IDocumentProcessor
        {
            public void Process(DocumentProcessorContext contexto)
            {
                var documento = contexto.Document;
                documento.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.Http,
                    Scheme = "Bearer",
                    Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                };
                documento.Security.Add(new OpenApiSecurityRequirement { { "Bearer", new string[0] } });
            }
        }

        /// <summary>Redirecciones de DefaultController y de la interfaz de Swagger en interface.s50c.</summary>
        private static Task Redirecciones(Microsoft.Owin.IOwinContext contexto, Func<Task> siguiente)
        {
            string ruta = contexto.Request.Path.Value ?? "";
            string destino = null;
            int codigo = 0;
            if (ruta == "/" || ruta.Equals("/docs", StringComparison.OrdinalIgnoreCase) || ruta.Equals("/docs/", StringComparison.OrdinalIgnoreCase))
            {
                destino = "/swagger";
                codigo = 302;
            }
            else if (ruta.Equals("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                destino = ruta.Substring(1) + "/index.html";
                codigo = 301;
            }
            else if (ruta.Equals("/swagger/", StringComparison.OrdinalIgnoreCase))
            {
                destino = "index.html";
                codigo = 301;
            }
            if (destino == null)
                return siguiente();

            contexto.Response.StatusCode = codigo;
            contexto.Response.Headers.Set("Location", destino);
            contexto.Response.ContentLength = 0;
            return Task.CompletedTask;
        }
    }
}
