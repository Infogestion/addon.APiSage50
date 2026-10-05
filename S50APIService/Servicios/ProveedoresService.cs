using System.Linq;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Proveedores (tabla proveed del ejercicio en curso).</summary>
    internal static class ProveedoresService
    {
        public static ListaJson Select()
        {
            return Db.Lector.LeerEjercicioJson<Proveed>(Db.EjercicioActual, "proveed");
        }

        public static Proveed Select(string codigo)
        {
            return Db.Lector.LeerEjercicio<Proveed>(Db.EjercicioActual, "proveed", new Filtro { ["CODIGO"] = codigo }).FirstOrDefault();
        }
    }
}
