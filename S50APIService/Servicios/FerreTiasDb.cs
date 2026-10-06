using System.Collections.Generic;
using System.Linq;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>De dónde leen los servicios de la app de repartos: las tablas del addon FERRETERIATIA (el DbFerreTiasContext de interface.s50c).</summary>
    internal static class FerreTiasDb
    {
        public const string Addon = "FERRETERIATIA";

        public static List<T> Leer<T>(string tabla, Filtro filtro = null) where T : new()
        {
            return Db.Lector.LeerEjercicio<T>(Addon, tabla, filtro);
        }

        /// <summary>La primera fila cuya columna vale eso, sin contar los espacios de alrededor; null si no hay ninguna.</summary>
        public static T Primero<T>(string tabla, string columna, string valor) where T : new()
        {
            return Leer<T>(tabla, new Filtro { [columna] = valor }).FirstOrDefault();
        }

        public static ListaJson LeerJson<T>(string tabla, Filtro filtro = null)
        {
            return Db.Lector.LeerEjercicioJson<T>(Addon, tabla, filtro);
        }
    }
}
