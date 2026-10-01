using System;
using System.Web.Http.ExceptionHandling;
using S50APIService.Sage;

namespace S50APIService.Api
{
    /// <summary>
    /// Escribe en la consola las excepciones no controladas de los controladores. La respuesta sigue siendo un 500 sin
    /// detalles (como en interface.s50c), así que sin esto no quedaría rastro de por qué ha fallado una petición.
    /// No registra las que <see cref="ManejadorExcepciones"/> convierte en respuestas normales.
    /// </summary>
    public sealed class RegistroExcepciones : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext contexto)
        {
            if (contexto.Exception is EjercicioNoEncontradoException)
                return;
            var peticion = contexto.Request;
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR {peticion?.Method} {peticion?.RequestUri?.AbsolutePath}: {contexto.Exception}");
        }
    }
}
