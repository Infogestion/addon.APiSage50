using System.Collections.Generic;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Qué artículo hay en cada ubicación (tabla rel_ubi_alm del addon Nadilux SGA).</summary>
    internal static class UbicacionRelService
    {
        /// <summary>Las relaciones de una ubicación.</summary>
        public static List<RelUbiAlm> SelectByCode(string code, string emp)
        {
            return Db.Lector.LeerEjercicio<RelUbiAlm>(Contexto.AddonSga, "rel_ubi_alm", new Filtro { ["UBICACION"] = code, ["EMPRESA"] = emp });
        }

        /// <summary>Las relaciones de un artículo.</summary>
        public static List<RelUbiAlm> SelectByCodeAr(string code, string emp)
        {
            return Db.Lector.LeerEjercicio<RelUbiAlm>(Contexto.AddonSga, "rel_ubi_alm", new Filtro { ["ARTICULO"] = code, ["EMPRESA"] = emp });
        }
    }
}
