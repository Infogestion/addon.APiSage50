using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Incidencias de los repartos (tabla inciden del addon FERRETERIATIA).</summary>
    internal static class IncidenciService
    {
        public static ListaJson Select()
        {
            return FerreTiasDb.LeerJson<inciden>("inciden");
        }

        public static inciden Select(string codigo)
        {
            return FerreTiasDb.Primero<inciden>("inciden", "CODIGO", codigo);
        }
    }
}
