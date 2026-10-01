using System.Web.Http;
using System.Web.Http.ExceptionHandling;
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
            config.MapHttpAttributeRoutes();

            config.Formatters.Clear();
            config.Formatters.Add(new FormateadorJson());

            config.Services.Add(typeof(IExceptionLogger), new RegistroExcepciones());
            config.Services.Replace(typeof(IExceptionHandler), new ManejadorExcepciones());
            config.MessageHandlers.Add(new ManejadorErroresSinCuerpo());
            config.MessageHandlers.Add(new ManejadorJwt(Contexto.Jwt));
            config.Filters.Add(new ValidacionEnteros());

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Never;

            Swagger.Configurar(app);
            app.UseWebApi(config);
            config.EnsureInitialized();
        }
    }
}
