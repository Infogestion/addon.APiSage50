using System.Web.Http;
using Newtonsoft.Json.Serialization;
using Owin;
using S50APIService.Api.Autenticacion;

namespace S50APIService.Api
{
    /// <summary>Configuración de Web API 2 (equivale al Program.cs de interface.s50c: AddControllers, JwtBearer y MapControllers).</summary>
    public static class Arranque
    {
        public static void Configurar(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            // Rutas por atributos ([Route], [HttpGet]...), como los controladores de interface.s50c.
            config.MapHttpAttributeRoutes();

            // Solo JSON, con nombres en camelCase como System.Text.Json en ASP.NET Core.
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            config.MessageHandlers.Add(new ManejadorErroresSinCuerpo());
            config.MessageHandlers.Add(new ManejadorJwt(Contexto.Jwt));

            // En producción ASP.NET Core no devuelve detalles de las excepciones.
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Never;

            app.UseWebApi(config);
            config.EnsureInitialized();
        }
    }
}
