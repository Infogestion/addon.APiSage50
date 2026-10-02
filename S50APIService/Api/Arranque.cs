using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Microsoft.Owin;
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
            config.Filters.Add(new ValidacionParametros());

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Never;

            Swagger.Configurar(app);
            AppBuilderUseExtensions.Use(app, EscaparPorcentajes);
            app.UseWebApi(config);
            config.EnsureInitialized();
        }

        /// <summary>
        /// HTTP.sys entrega la ruta ya decodificada y Web API la decodifica otra vez al sacar los valores de ruta, mientras que
        /// ASP.NET Core solo lo hace una vez. Escapando los % que quedan, un valor codificado dos veces llega como a interface.s50c.
        /// </summary>
        private static Task EscaparPorcentajes(IOwinContext contexto, Func<Task> siguiente)
        {
            string ruta = contexto.Request.Path.Value;
            if (ruta.IndexOf('%') >= 0)
                contexto.Request.Path = new PathString(ruta.Replace("%", "%25"));
            return siguiente();
        }
    }
}
