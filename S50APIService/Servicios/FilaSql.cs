using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using S50APIService.Api;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// Guardar una fila tal cual llega en la petición, como hacía EF en interface.s50c, para las tablas del ejercicio que
    /// Sage no deja escribir con una clase. Va por la capa de datos de Sage.
    /// </summary>
    internal static class FilaSql
    {
        /// <summary>
        /// Las columnas de la tabla con el valor que traen en <paramref name="item"/>, en el orden en que EF las escribe:
        /// primero las de la clave y luego las demás, cada grupo por el nombre de su propiedad.
        /// </summary>
        public static Dictionary<string, object> Columnas(object item, params string[] clave)
        {
            return item.GetType().GetProperties()
                .OrderBy(p => clave.Contains(p.Name) ? 0 : 1).ThenBy(p => p.Name, StringComparer.Ordinal)
                .ToDictionary(p => p.GetCustomAttribute<ColumnaAttribute>()?.Nombre ?? p.Name.ToUpperInvariant(), p => p.GetValue(item));
        }

        /// <summary>
        /// Crea la fila como el INSERT de EF: las columnas que no vienen en la petición se quedan con el valor por defecto
        /// que tengan en la tabla.
        /// </summary>
        public static void Insertar(string year, string tabla, IEnumerable<KeyValuePair<string, object>> columnas)
        {
            var parametros = new Dictionary<string, string>();
            var valores = columnas.Where(c => !Falta(c.Value)).ToDictionary(c => "[" + c.Key + "]", c => Valor(c.Value, parametros));
            Escribir(year, $"INSERT INTO {{{tabla}}} ({string.Join(", ", valores.Keys)}) VALUES ({string.Join(", ", valores.Values)})", parametros);
        }

        /// <summary>True si el campo no viene en la petición: sin valor o, en una fecha obligatoria, sin fecha.</summary>
        public static bool Falta(object valor) => valor == null || valor.Equals(default(DateTime));

        /// <summary>El valor como va en la instrucción: NULL o un parámetro, que se añade a <paramref name="parametros"/>.</summary>
        public static string Valor(object valor, Dictionary<string, string> parametros)
        {
            if (valor == null)
                return "NULL";

            string nombre = "@p" + parametros.Count;
            if (valor is DateTime fecha)
            {
                if (fecha.Year < 1753)
                    throw new InvalidOperationException("SqlDateTime overflow. Must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");

                parametros[nombre] = fecha.ToString("yyyy-MM-ddTHH:mm:ss.fff");
                return $"CONVERT(datetime, {nombre}, 126)";
            }

            parametros[nombre] = valor is bool si ? (si ? "1" : "0") : Convert.ToString(valor, CultureInfo.InvariantCulture);
            return nombre;
        }

        /// <summary>
        /// Escribe en una tabla del ejercicio. La sesión de Sage tiene ANSI_WARNINGS apagado: se enciende para que un valor
        /// que no cabe en su columna sea un error de SQL Server, como con EF, y no se guarde cortado.
        /// </summary>
        public static void Escribir(string year, string sql, Dictionary<string, string> parametros)
        {
            Contexto.Escritor.Ejecutar(year, "SET ANSI_WARNINGS ON; " + sql + "; SET ANSI_WARNINGS OFF;", parametros);
        }
    }
}
