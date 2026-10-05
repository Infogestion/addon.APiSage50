using System.Linq;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Vendedores (tabla vendedor del ejercicio en curso).</summary>
    internal static class VendedorService
    {
        public static ListaJson Select()
        {
            return Db.Lector.LeerEjercicioJson<Vendedor>(Db.EjercicioActual, "vendedor");
        }

        public static Vendedor Select(string codigo)
        {
            return Db.Lector.LeerEjercicio<Vendedor>(Db.EjercicioActual, "vendedor", new Filtro { ["CODIGO"] = codigo }).FirstOrDefault();
        }
    }
}
