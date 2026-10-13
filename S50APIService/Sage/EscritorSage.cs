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

        /// <summary>Ver <see cref="TrabajadorSage.NuevoNumeroTraspaso"/>.</summary>
        public string NuevoNumeroTraspaso(string ejercicio, string empresa, out string numero)
        {
            string reservado = null;
            string motivo = _sesion.Ejecutar(t => t.NuevoNumeroTraspaso(ejercicio, empresa, out reservado), _timeout,
                $"reservar un número de traspaso en {ejercicio}/{empresa}");
            numero = reservado;
            return motivo;
        }

        /// <summary>Ver <see cref="TrabajadorSage.AnadirLineaTraspaso"/>.</summary>
        public string AnadirLineaTraspaso(string ejercicio, string empresa, string numero, string articulo, decimal unidades, string[] series, out int linea)
        {
            int nueva = 0;
            string motivo = _sesion.Ejecutar(t => t.AnadirLineaTraspaso(ejercicio, empresa, numero, articulo, unidades, series, out nueva), _timeout,
                $"añadir una línea al traspaso {ejercicio}/{empresa}/{numero}");
            linea = nueva;
            return motivo;
        }

        /// <summary>Ver <see cref="TrabajadorSage.BorrarTraspaso"/>.</summary>
        public string BorrarTraspaso(string ejercicio, string empresa, string numero)
        {
            return _sesion.Ejecutar(t => t.BorrarTraspaso(ejercicio, empresa, numero), _timeout,
                $"borrar el traspaso {ejercicio}/{empresa}/{numero}");
        }

        /// <summary>Ver <see cref="TrabajadorSage.NuevoNumeroRegularizacion"/>.</summary>
        public string NuevoNumeroRegularizacion(string ejercicio, string empresa, out string numero)
        {
            string reservado = null;
            string motivo = _sesion.Ejecutar(t => t.NuevoNumeroRegularizacion(ejercicio, empresa, out reservado), _timeout,
                $"reservar un número de regularización en {ejercicio}/{empresa}");
            numero = reservado;
            return motivo;
        }

        /// <summary>Ver <see cref="TrabajadorSage.ModificarRegularizacion"/>.</summary>
        public string ModificarRegularizacion(string ejercicio, string empresa, string numero, DateTime fecha, string operario, string tipo, string observaciones)
        {
            return _sesion.Ejecutar(t => t.ModificarRegularizacion(ejercicio, empresa, numero, fecha, operario, tipo, observaciones), _timeout,
                $"modificar la regularización {ejercicio}/{empresa}/{numero}");
        }

        /// <summary>Ver <see cref="TrabajadorSage.AnadirLineaRegularizacion"/>.</summary>
        public string AnadirLineaRegularizacion(string ejercicio, string empresa, string numero, string articulo, decimal unidades, out int linea)
        {
            int nueva = 0;
            string motivo = _sesion.Ejecutar(t => t.AnadirLineaRegularizacion(ejercicio, empresa, numero, articulo, unidades, out nueva), _timeout,
                $"añadir una línea a la regularización {ejercicio}/{empresa}/{numero}");
            linea = nueva;
            return motivo;
        }

        /// <summary>Ver <see cref="TrabajadorSage.ModificarLineaRegularizacion"/>.</summary>
        public string ModificarLineaRegularizacion(string ejercicio, string empresa, string numero, int linea, string articulo, decimal unidades)
        {
            return _sesion.Ejecutar(t => t.ModificarLineaRegularizacion(ejercicio, empresa, numero, linea, articulo, unidades), _timeout,
                $"modificar la línea {linea} de la regularización {ejercicio}/{empresa}/{numero}");
        }

        /// <summary>Ver <see cref="TrabajadorSage.BorrarLineaRegularizacion"/>.</summary>
        public string BorrarLineaRegularizacion(string ejercicio, string empresa, string numero, int linea)
        {
            return _sesion.Ejecutar(t => t.BorrarLineaRegularizacion(ejercicio, empresa, numero, linea), _timeout,
                $"borrar la línea {linea} de la regularización {ejercicio}/{empresa}/{numero}");
        }

        /// <summary>Ver <see cref="TrabajadorSage.BorrarRegularizacion"/>.</summary>
        public string BorrarRegularizacion(string ejercicio, string empresa, string numero)
        {
            return _sesion.Ejecutar(t => t.BorrarRegularizacion(ejercicio, empresa, numero), _timeout,
                $"borrar la regularización {ejercicio}/{empresa}/{numero}");
        }

        /// <summary>Ver <see cref="TrabajadorSage.NuevoAlbaranCompra"/>.</summary>
        public string NuevoAlbaranCompra(string ejercicio, string empresa, string proveedor, string numero, string almacen, DateTime fecha, string observaciones, out string numeroNuevo)
        {
            string creado = null;
            string motivo = _sesion.Ejecutar(t => t.NuevoAlbaranCompra(ejercicio, empresa, proveedor, numero, almacen, fecha, observaciones, out creado), _timeout,
                $"crear un albarán de compra del proveedor {proveedor} en {ejercicio}/{empresa}");
            numeroNuevo = creado;
            return motivo;
        }

        /// <summary>Ver <see cref="TrabajadorSage.ModificarAlbaranCompra"/>.</summary>
        public string ModificarAlbaranCompra(string ejercicio, string empresa, string proveedor, string numero, string almacen, DateTime? fecha, string operario,
            string observaciones, out bool sinLineas)
        {
            bool vacio = false;
            string motivo = _sesion.Ejecutar(t => t.ModificarAlbaranCompra(ejercicio, empresa, proveedor, numero, almacen, fecha, operario, observaciones, out vacio), _timeout,
                $"modificar el albarán de compra {ejercicio}/{empresa}/{proveedor}/{numero}");
            sinLineas = vacio;
            return motivo;
        }

        /// <summary>Ver <see cref="TrabajadorSage.BorrarAlbaranCompra"/>.</summary>
        public string BorrarAlbaranCompra(string ejercicio, string empresa, string proveedor, string numero)
        {
            return _sesion.Ejecutar(t => t.BorrarAlbaranCompra(ejercicio, empresa, proveedor, numero), _timeout,
                $"borrar el albarán de compra {ejercicio}/{empresa}/{proveedor}/{numero}");
        }
    }
}
