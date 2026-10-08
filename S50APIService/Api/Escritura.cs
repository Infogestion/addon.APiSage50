using System;
using System.Net.Http;
using S50APIService.Sage;

namespace S50APIService.Api
{
    internal static class Escritura
    {
        /// <summary>
        /// Como el try/catch de las escrituras de interface.s50c: cualquier error es un 400 con su mensaje (el de SQL Server
        /// si viene de la base de datos), salvo que falte la base de datos del ejercicio, que es el 404 de siempre.
        /// </summary>
        public static HttpResponseMessage Intentar(Func<HttpResponseMessage> escritura)
        {
            try
            {
                return escritura();
            }
            catch (EjercicioNoEncontradoException)
            {
                throw;
            }
            catch (ErrorSqlException ex)
            {
                return Respuestas.ErrorValidacion(ex.MensajeSql);
            }
            catch (Exception ex)
            {
                return Respuestas.ErrorValidacion(ex.Message);
            }
        }
    }
}
