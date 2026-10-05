using System.Linq;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Ejercicios de Sage (tabla ejercici de COMUNES).</summary>
    internal static class EjercicioService
    {
        public static ListaJson Select()
        {
            return Db.Lector.LeerJson<Ejercici>(Db.Comunes, "ejercici");
        }

        /// <summary>El ejercicio predeterminado; null si no hay ninguno marcado.</summary>
        public static Ejercici SelectDefault()
        {
            return Db.Lector.Leer<Ejercici>(Db.Comunes, "ejercici").FirstOrDefault(e => e.Predet);
        }
    }
}
