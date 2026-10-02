using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text.Json;

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

    /// <summary>La base de datos del ejercicio pedido no existe (DBNotFoundException en interface.s50c).</summary>
    public sealed class EjercicioNoEncontradoException : Exception
    {
        public EjercicioNoEncontradoException(string ejercicio) : base($"No existe la base de datos del ejercicio {ejercicio}.") { }
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

        /// <summary>
        /// Las filas de <paramref name="tabla"/> que cumplen <paramref name="condicion"/> (todas si es null), en el orden en que
        /// las devuelve SQL Server (como EF sin OrderBy). Ver <see cref="TrabajadorSage.LeerTabla"/> para la condición y sus parámetros.
        /// </summary>
        public List<T> Leer<T>(string baseDatos, string tabla, string condicion = null, Dictionary<string, string> parametros = null) where T : new()
        {
            return Leer<T>(baseDatos, DeTabla(tabla, condicion, parametros));
        }

        /// <summary>Como <see cref="Leer{T}(string, string, string, Dictionary{string, string})"/>, con las uniones y la paginación de <paramref name="consulta"/>.</summary>
        public List<T> Leer<T>(string baseDatos, Consulta consulta) where T : new()
        {
            var propiedades = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite).ToArray();
            var columnas = propiedades.Select(p => p.GetCustomAttribute<ColumnaAttribute>()?.Nombre ?? p.Name.ToUpperInvariant()).ToArray();

            var filas = _sesion.Ejecutar(t => t.LeerConsulta(baseDatos, consulta, columnas), _timeout, $"leer {baseDatos}: {consulta.Origen}");

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

        /// <summary>
        /// Como <see cref="Leer{T}(string, Consulta)"/>, pero de la base de datos del ejercicio <paramref name="ejercicio"/> (el {year} de las rutas).
        /// Si no existe, lanza <see cref="EjercicioNoEncontradoException"/>, que la API convierte en el 404 de interface.s50c.
        /// </summary>
        public List<T> LeerEjercicio<T>(string ejercicio, string tabla, string condicion = null, Dictionary<string, string> parametros = null) where T : new()
        {
            return LeerEjercicio<T>(ejercicio, DeTabla(tabla, condicion, parametros));
        }

        public List<T> LeerEjercicio<T>(string ejercicio, Consulta consulta) where T : new()
        {
            ComprobarEjercicio(ejercicio);
            return Leer<T>(ejercicio, consulta);
        }

        /// <summary>
        /// Las mismas filas que <see cref="Leer{T}(string, Consulta)"/>, ya como JSON (el array de modelos <typeparamref name="T"/>) en bloques de bytes.
        /// Para los listados: el JSON se escribe dentro de Sage y no se crea un objeto por fila.
        /// </summary>
        public List<byte[]> LeerJson<T>(string baseDatos, string tabla, string condicion = null, Dictionary<string, string> parametros = null)
        {
            return LeerJson<T>(baseDatos, DeTabla(tabla, condicion, parametros));
        }

        public List<byte[]> LeerJson<T>(string baseDatos, Consulta consulta)
        {
            var campos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite).Select(p => new CampoJson
            {
                Columna = p.GetCustomAttribute<ColumnaAttribute>()?.Nombre ?? p.Name.ToUpperInvariant(),
                Nombre = JsonNamingPolicy.CamelCase.ConvertName(p.Name),
                Tipo = Type.GetTypeCode(Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType),
                AdmiteNull = !p.PropertyType.IsValueType || Nullable.GetUnderlyingType(p.PropertyType) != null,
            }).ToArray();

            var destino = new BloquesJson();
            try
            {
                _sesion.Ejecutar(t => { t.EscribirConsultaJson(baseDatos, consulta, campos, destino); return 0; },
                    _timeout, $"leer {baseDatos}: {consulta.Origen}");
                return destino.Tomar();
            }
            finally
            {
                RemotingServices.Disconnect(destino);
            }
        }

        /// <summary>Como <see cref="LeerJson{T}(string, Consulta)"/>, pero del ejercicio <paramref name="ejercicio"/> (ver <see cref="LeerEjercicio{T}(string, Consulta)"/>).</summary>
        public List<byte[]> LeerEjercicioJson<T>(string ejercicio, string tabla, string condicion = null, Dictionary<string, string> parametros = null)
        {
            return LeerEjercicioJson<T>(ejercicio, DeTabla(tabla, condicion, parametros));
        }

        public List<byte[]> LeerEjercicioJson<T>(string ejercicio, Consulta consulta)
        {
            ComprobarEjercicio(ejercicio);
            return LeerJson<T>(ejercicio, consulta);
        }

        private static Consulta DeTabla(string tabla, string condicion, Dictionary<string, string> parametros)
        {
            return new Consulta { Origen = "{" + tabla + "}", Condicion = condicion, Parametros = parametros };
        }

        private void ComprobarEjercicio(string ejercicio)
        {
            if (!_sesion.Ejecutar(t => t.ExisteBaseDatos(ejercicio), _timeout, $"comprobar el ejercicio {ejercicio}"))
                throw new EjercicioNoEncontradoException(ejercicio);
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
