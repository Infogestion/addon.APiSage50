using System;
using System.Web.Http.ExceptionHandling;

namespace S50APIService.Api
{
    /// <summary>
    /// Escribe en la consola las excepciones no controladas de los controladores. La respuesta sigue siendo un 500 sin
    /// detalles (como en interface.s50c), así que sin esto no quedaría rastro de por qué ha fallado una petición.
    /// </summary>
    public sealed class RegistroExcepciones : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext contexto)
        {
            var peticion = contexto.Request;
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR {peticion?.Method} {peticion?.RequestUri?.AbsolutePath}: {contexto.Exception}");
        }
    }
}
