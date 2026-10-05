using System.Collections.Generic;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// De dónde leen los servicios de documentos de mercancías: las tablas del addon GESTIONMERC (c_doc, d_doc,
    /// alma_solicitud, alma_envio) y las del ejercicio del año en curso, porque sus rutas no llevan {year}.
    /// </summary>
    internal static class MercanciasDb
    {
        public const string Addon = "GESTIONMERC";

        public static List<T> DelAddon<T>(string tabla, Filtro filtro = null) where T : new()
        {
            return Db.Lector.LeerEjercicio<T>(Addon, tabla, filtro);
        }

        public static List<T> DelEjercicio<T>(string tabla, Filtro filtro = null) where T : new()
        {
            return Db.Lector.LeerEjercicio<T>(Db.EjercicioActual, tabla, filtro);
        }

        public static List<T> DelEjercicio<T>(Consulta consulta) where T : new()
        {
            return Db.Lector.LeerEjercicio<T>(Db.EjercicioActual, consulta);
        }
    }
}
