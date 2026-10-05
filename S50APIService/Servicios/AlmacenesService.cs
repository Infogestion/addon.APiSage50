using System.Linq;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Almacenes (tabla almacen del ejercicio).</summary>
    internal static class AlmacenesService
    {
        public static ListaJson Select(string year)
        {
            return Db.Lector.LeerEjercicioJson<Almacen>(year, "almacen");
        }

        public static Almacen Select(string year, string code)
        {
            return Db.Lector.LeerEjercicio<Almacen>(year, "almacen", new Filtro { ["CODIGO"] = code }).FirstOrDefault();
        }
    }
}
