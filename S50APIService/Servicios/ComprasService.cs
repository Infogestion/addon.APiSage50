using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Series de los artículos (tabla compras de COMUNES, común a todos los ejercicios).</summary>
    internal static class ComprasService
    {
        /// <summary>Las series de un artículo en un almacén que no están dadas de baja.</summary>
        public static ListaJson SelectSeriesDisponiblesPorAlmacen(int page, int pagesize, string articulo, string almacen)
        {
            return Db.Lector.LeerJson<Compras>(Db.Comunes, new Consulta
            {
                Origen = "{compras}",
                Filtro = new Filtro { ["ARTICULO"] = articulo, ["ALMACEN"] = almacen }.Sql("UPPER(LTRIM(RTRIM([BAJA]))) <> 'S'"),
            }.Pagina(page, pagesize));
        }
    }
}
