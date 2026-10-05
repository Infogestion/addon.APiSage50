using System;
using System.Collections.Generic;

namespace S50APIService.Sage
{
    /// <summary>
    /// Escribe en Sage: con sus clases de negocio cuando las hay y, para las tablas de addons que no tienen, con SQL por su
    /// capa de datos (equivale al SaveChanges de EF en interface.s50c).
    /// </summary>
    public sealed class EscritorSage
    {
        private readonly SesionSage _sesion;
        private readonly TimeSpan _timeout;

        public EscritorSage(SesionSage sesion, TimeSpan timeout)
        {
            _sesion = sesion;
            _timeout = timeout;
        }

        /// <summary>Ver <see cref="TrabajadorSage.Ejecutar"/>.</summary>
        public int Ejecutar(string baseDatos, string sql, Dictionary<string, string> parametros = null)
        {
            return _sesion.Ejecutar(t => t.Ejecutar(baseDatos, sql, parametros), _timeout, $"escribir en {baseDatos}: {sql}");
        }

        /// <summary>Ver <see cref="TrabajadorSage.GuardarDocumentoMercancia"/>.</summary>
        public string GuardarDocumentoMercancia(string ejercicio, string empresa, string numero, Dictionary<string, string> cambios)
        {
            return _sesion.Ejecutar(t => t.GuardarDocumentoMercancia(ejercicio, empresa, numero, cambios), _timeout,
                $"guardar el documento de mercancías {ejercicio}/{empresa}/{numero}");
        }
    }
}
