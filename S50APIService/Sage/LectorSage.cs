using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace S50APIService.Sage
{
    /// <summary>
    /// Nombre de la columna de la tabla de Sage cuando no es el de la propiedad en mayúsculas
    /// (el HasColumnName de las entidades de interface.s50c, p. ej. GuidId → "GUID_ID").
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ColumnaAttribute : Attribute
    {
        public string Nombre { get; }
        public ColumnaAttribute(string nombre) { Nombre = nombre; }
    }

    /// <summary>
    /// Lee tablas de Sage y las convierte en objetos de los modelos de la API (equivale a los DbSet de EF en interface.s50c).
    /// Cada propiedad pública del modelo es una columna: su nombre en mayúsculas, o el de <see cref="ColumnaAttribute"/>.
    /// </summary>
    public sealed class LectorSage
    {
        private readonly SesionSage _sesion;
        private readonly TimeSpan _timeout;

        public LectorSage(SesionSage sesion, TimeSpan timeout)
        {
            _sesion = sesion;
            _timeout = timeout;
        }

        /// <summary>Todas las filas de <paramref name="tabla"/>, en el orden en que las devuelve SQL Server (como EF sin OrderBy).</summary>
        public List<T> Leer<T>(string baseDatos, string tabla) where T : new()
        {
            var propiedades = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite).ToArray();
            var columnas = propiedades.Select(p => p.GetCustomAttribute<ColumnaAttribute>()?.Nombre ?? p.Name.ToUpperInvariant()).ToArray();

            var filas = _sesion.Ejecutar(t => t.LeerTabla(baseDatos, tabla, columnas), _timeout, $"leer {baseDatos}.{tabla}");

            var resultado = new List<T>(filas.Count);
            foreach (var fila in filas)
            {
                var objeto = new T();
                for (int i = 0; i < propiedades.Length; i++)
                    propiedades[i].SetValue(objeto, Convertir(fila[i], propiedades[i].PropertyType));
                resultado.Add(objeto);
            }
            return resultado;
        }

        /// <summary>El valor de SQL al tipo de la propiedad (p. ej. un smallint a int); null solo si la propiedad lo admite.</summary>
        private static object Convertir(object valor, Type tipo)
        {
            var subyacente = Nullable.GetUnderlyingType(tipo);
            if (valor == null)
                return subyacente != null || !tipo.IsValueType ? null : Activator.CreateInstance(tipo);
            var destino = subyacente ?? tipo;
            return destino.IsInstanceOfType(valor) ? valor : Convert.ChangeType(valor, destino);
        }
    }
}
