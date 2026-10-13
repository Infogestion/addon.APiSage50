using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Líneas de los albaranes de compra (tabla d_albcom del ejercicio).</summary>
    internal static class DAlbcomService
    {
        /// <summary>Las líneas del albarán; sin <paramref name="emptyArticles"/>, solo las que tienen artículo y descripción.</summary>
        public static List<DAlbcom> Select(string year, string empresa, string proveedor, string numero, bool emptyArticles = true)
        {
            var filtro = DelAlbaran(empresa, proveedor, numero);
            if (!emptyArticles)
                filtro.Distinto("ARTICULO", "").Distinto("DEFINICION", "");

            return Db.Lector.LeerEjercicio<DAlbcom>(year, "d_albcom", filtro);
        }

        public static DAlbcom Select(string year, string empresa, string proveedor, string numero, int linia)
        {
            var filtro = DelAlbaran(empresa, proveedor, numero).Exacto("LINIA", linia.ToString(CultureInfo.InvariantCulture));
            return Db.Lector.LeerEjercicio<DAlbcom>(year, "d_albcom", filtro).FirstOrDefault();
        }

        /// <summary>
        /// Añade una línea al albarán con la clase de albaranes de compra de Sage, que le pone el precio de la tarifa del
        /// proveedor y el IVA del artículo, suma sus unidades al stock del almacén del albarán y recalcula los totales. De
        /// la petición solo se usan la empresa, el proveedor, el número, el artículo, las unidades y la descripción;
        /// devuelve la línea como queda guardada. Lanza el motivo si no se puede.
        /// </summary>
        public static DAlbcom Add(string year, DAlbcom item)
        {
            Db.Lector.ComprobarEjercicio(year);
            ComprobarSinTallaNiColor(item);

            string motivo = Contexto.Escritor.AnadirLineaAlbaranCompra(year, item.Empresa.Trim(), item.Proveedor.Trim(), item.Numero, item.Articulo.Trim(),
                item.Unidades, item.Definicion, out int linea);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            var filtro = DelAlbaran(item.Empresa, item.Proveedor, item.Numero).Exacto("LINIA", linea.ToString(CultureInfo.InvariantCulture));
            FilaSql.Actualizar(year, "d_albcom", filtro, new Dictionary<string, object> { ["USUARIO"] = CAlbcomService.Usuario });
            return Select(year, item.Empresa, item.Proveedor, item.Numero, linea);
        }

        /// <summary>
        /// Cambia las unidades, el precio, los dos descuentos y la descripción de la línea con la clase de albaranes de
        /// compra de Sage, que ajusta el stock con la diferencia y recalcula el importe y los totales, y la devuelve como
        /// queda guardada. El artículo no se puede cambiar. Lanza el motivo si no se puede.
        /// </summary>
        public static DAlbcom Update(string year, DAlbcom item)
        {
            ComprobarSinTallaNiColor(item);
            var guardada = Select(year, item.Empresa, item.Proveedor, item.Numero, item.Linia);
            if (guardada.Articulo.Trim() != item.Articulo.Trim())
                throw new InvalidOperationException("No se puede cambiar el artículo de una línea: hay que borrarla y añadir otra.");

            string motivo = Contexto.Escritor.ModificarLineaAlbaranCompra(year, item.Empresa.Trim(), item.Proveedor.Trim(), item.Numero, item.Linia, item.Unidades,
                item.Precio, item.Dto1, item.Dto2, item.Definicion);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            return Select(year, item.Empresa, item.Proveedor, item.Numero, item.Linia);
        }

        /// <summary>Cambia solo las unidades de la línea, con la clase de albaranes de compra de Sage. Lanza el motivo si no se puede.</summary>
        public static void UpdateUnidades(string year, DAlbcom linea, decimal unidades)
        {
            string motivo = Contexto.Escritor.ModificarLineaAlbaranCompra(year, linea.Empresa.Trim(), linea.Proveedor.Trim(), linea.Numero, linea.Linia, unidades);
            if (motivo != null)
                throw new InvalidOperationException(motivo);
        }

        /// <summary>
        /// Borra la línea con la clase de albaranes de compra de Sage, que quita sus unidades del stock y recalcula los
        /// totales. Lanza el motivo si Sage no lo permite.
        /// </summary>
        public static void Remove(string year, DAlbcom item)
        {
            string motivo = Contexto.Escritor.BorrarLineaAlbaranCompra(year, item.Empresa.Trim(), item.Proveedor.Trim(), item.Numero, item.Linia);
            if (motivo != null)
                throw new InvalidOperationException(motivo);
        }

        private static void ComprobarSinTallaNiColor(DAlbcom item)
        {
            if (!string.IsNullOrWhiteSpace(item.Talla) || !string.IsNullOrWhiteSpace(item.Color))
                throw new InvalidOperationException("Las compras de artículos con talla y color no están admitidas.");
        }

        private static Filtro DelAlbaran(string empresa, string proveedor, string numero)
        {
            return new Filtro { ["EMPRESA"] = empresa, ["PROVEEDOR"] = proveedor, ["NUMERO"] = numero };
        }
    }
}
