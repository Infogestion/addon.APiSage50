using System.Globalization;
using System.Linq;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Series vendidas en los albaranes de venta (tabla venser del ejercicio).</summary>
    internal static class VenserService
    {
        public static ListaJson Select(string year, string empresa, string albaran)
        {
            return Db.Lector.LeerEjercicioJson<Venser>(year, "venser", new Filtro
            {
                ["EMPRESA"] = empresa,
                ["ALBARAN"] = albaran,
            });
        }

        public static Venser Select(string year, string empresa, string albaran, int linea, string serie)
        {
            return Db.Lector.LeerEjercicio<Venser>(year, "venser", new Filtro
            {
                ["EMPRESA"] = empresa,
                ["ALBARAN"] = albaran,
                ["SERIE"] = serie,
            }.Exacto("LINEA", linea.ToString(CultureInfo.InvariantCulture))).FirstOrDefault();
        }
    }
}
