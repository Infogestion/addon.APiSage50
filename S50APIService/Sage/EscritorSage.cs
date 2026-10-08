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

        /// <summary>Ver <see cref="TrabajadorSage.GuardarDocumento"/>.</summary>
        public string GuardarDocumento(CambiosDocumento cambios)
        {
            return _sesion.Ejecutar(t => t.GuardarDocumento(cambios), _timeout,
                $"guardar el documento {cambios.Clase} {cambios.Ejercicio}/{cambios.Empresa}/{cambios.Numero}");
        }

        /// <summary>Ver <see cref="TrabajadorSage.SepararReparto"/>.</summary>
        public string SepararReparto(string ejercicio, string empresa, string numero, string numeroNuevo, Dictionary<int, decimal> lineas)
        {
            return _sesion.Ejecutar(t => t.SepararReparto(ejercicio, empresa, numero, numeroNuevo, lineas), _timeout,
                $"separar el reparto {ejercicio}/{empresa}/{numero}");
        }

        /// <summary>Ver <see cref="TrabajadorSage.VenderSerie"/>.</summary>
        public string VenderSerie(string ejercicio, string empresa, string albaran, string letra, int linea, string articulo, string serie)
        {
            return _sesion.Ejecutar(t => t.VenderSerie(ejercicio, empresa, albaran, letra, linea, articulo, serie), _timeout,
                $"vender la serie {serie} en el albarán {ejercicio}/{empresa}/{albaran}");
        }

        /// <summary>Ver <see cref="TrabajadorSage.AnularVentaSerie"/>.</summary>
        public string AnularVentaSerie(string ejercicio, string empresa, string albaran, string letra, int linea, string articulo, string serie)
        {
            return _sesion.Ejecutar(t => t.AnularVentaSerie(ejercicio, empresa, albaran, letra, linea, articulo, serie), _timeout,
                $"anular la venta de la serie {serie} en el albarán {ejercicio}/{empresa}/{albaran}");
        }
    }
}
