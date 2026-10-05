using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Operarios de Sage (tabla operario de COMUNES).</summary>
    internal static class OperariosService
    {
        public static ListaJson Select()
        {
            return Db.Lector.LeerJson<Operario>(Db.Comunes, "operario");
        }
    }
}
