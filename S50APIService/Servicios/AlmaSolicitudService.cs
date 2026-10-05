using System.Collections.Generic;
using System.Linq;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Unidades solicitadas por almacén de los artículos de un documento (tabla alma_solicitud).</summary>
    internal static class AlmaSolicitudService
    {
        /// <summary>El texto JSON con las unidades por almacén de un artículo del documento, o "[]" si no tiene.</summary>
        public static string GetWarehouseDetail(string numero, string empresa, string ejercicio, string articulo)
        {
            var solicitud = MercanciasDb.DelAddon<AlmaUnidades>("alma_solicitud", new Filtro
            {
                ["NUMERO"] = numero,
                ["ARTICULO"] = articulo,
                ["EMPRESA"] = empresa,
                ["EJERCICIO"] = ejercicio,
            }).FirstOrDefault();
            return solicitud == null ? "[]" : solicitud.Unidades;
        }

        public static List<AlmaUnidades> GetDetails(string numero, string empresa, string ejercicio)
        {
            return MercanciasDb.DelAddon<AlmaUnidades>("alma_solicitud", new Filtro
            {
                ["NUMERO"] = numero,
                ["EMPRESA"] = empresa,
                ["EJERCICIO"] = ejercicio,
            });
        }
    }
}
