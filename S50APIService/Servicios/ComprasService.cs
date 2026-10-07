using System;
using System.Collections.Generic;
using System.Linq;
using S50APIService.Api;
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

        /// <summary>La serie de un artículo comprado por una empresa, o null si no existe.</summary>
        public static Compras SelectBySerie(string empresa, string articulo, string serie)
        {
            if (serie == null)
                throw new InvalidOperationException("Object reference not set to an instance of an object.");

            return Db.Lector.Leer<Compras>(Db.Comunes, "compras", new Filtro { ["CODEMPCOM"] = empresa, ["ARTICULO"] = articulo, ["SERIE"] = serie }).FirstOrDefault();
        }

        /// <summary>
        /// Da de baja las series de la petición que existen, apuntando en cada una el albarán de venta, el cliente y la
        /// fecha; las que no existen se saltan. Sage no tiene un método para esto sin la línea del albarán (él mismo
        /// lo hace con un UPDATE al guardar el documento), así que se escribe por su capa de datos.
        /// </summary>
        public static void DarBajaSeries(BajaSeriesRequest request)
        {
            foreach (string serie in request.Series)
            {
                var compra = SelectBySerie(request.Empresa, request.Articulo, serie);
                if (compra != null)
                    DarBaja(compra, request);
            }
        }

        private static void DarBaja(Compras compra, BajaSeriesRequest request)
        {
            if (request.FechaVenta.Year < 1753)
                throw new InvalidOperationException("SqlDateTime overflow. Must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");

            // La sesión de Sage tiene ANSI_WARNINGS apagado: sin encenderlo, un valor que no cabe se guarda cortado y una fecha fuera de rango como NULL.
            Contexto.Escritor.Ejecutar(Db.Comunes,
                "SET ANSI_WARNINGS ON;"
                + " UPDATE {compras} SET [BAJA] = 'S', [CLIENTE] = @cliente, [CODEMP] = @empresa, [ALB_VENTA] = @albaran,"
                + " [FEC_VENTA] = CONVERT(datetime, @fecventa, 126), [FBAJA] = CONVERT(datetime, @fbaja, 126)"
                + " WHERE [SERIE] = @serie AND [ORDEN] = @orden;"
                + " SET ANSI_WARNINGS OFF;",
                new Dictionary<string, string>
                {
                    ["@albaran"] = request.AlbaranVenta,
                    ["@empresa"] = request.Empresa,
                    ["@cliente"] = request.Cliente,
                    ["@fecventa"] = Fecha(request.FechaVenta),
                    ["@fbaja"] = Fecha(DateTime.Now),
                    ["@serie"] = compra.Serie,
                    ["@orden"] = compra.Orden.ToString(),
                });
        }

        private static string Fecha(DateTime fecha) => fecha.ToString("yyyy-MM-ddTHH:mm:ss.fff");
    }
}
