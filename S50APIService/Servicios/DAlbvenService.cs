using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Líneas de los albaranes de venta (tabla d_albven del ejercicio).</summary>
    internal static class DAlbvenService
    {
        /// <summary>Las líneas del albarán; sin <paramref name="emptyArticles"/>, solo las que tienen artículo y descripción.</summary>
        public static List<DAlbven> Select(string year, string empresa, string numero, string letra, bool emptyArticles = true)
        {
            var filtro = DelAlbaran(empresa, numero, letra);
            if (!emptyArticles)
                filtro.Distinto("ARTICULO", "").Distinto("DEFINICION", "");

            return Db.Lector.LeerEjercicio<DAlbven>(year, "d_albven", filtro);
        }

        public static DAlbven Select(string year, string empresa, string numero, string letra, int linia)
        {
            var filtro = DelAlbaran(empresa, numero, letra).Exacto("LINIA", linia.ToString(CultureInfo.InvariantCulture));
            return Db.Lector.LeerEjercicio<DAlbven>(year, "d_albven", filtro).FirstOrDefault();
        }

        /// <summary>
        /// Añade una línea al albarán con la clase de albaranes de venta de Sage, que le pone el IVA del artículo, descuenta
        /// sus unidades del stock del almacén del albarán y recalcula los totales. El precio y los descuentos son los de la
        /// petición o, si vienen a cero, los de la tarifa del cliente. Sin artículo la línea es una nota y solo lleva la
        /// descripción. Devuelve la línea como queda guardada. Lanza el motivo si no se puede.
        /// </summary>
        public static DAlbven Add(string year, DAlbven item)
        {
            Db.Lector.ComprobarEjercicio(year);
            var cabecera = CAlbvenService.Select(year, item.Empresa, item.Numero, item.Letra);
            if (cabecera == null)
                throw new InvalidOperationException($"No se encontró Cabecera Albaran de Venta para Empresa='{item.Empresa}' Numero='{item.Numero}' Letra='{item.Letra}'");
            if (string.IsNullOrEmpty(item.Articulo) && string.IsNullOrEmpty(item.Definicion))
                throw new InvalidOperationException("Para insertar una linea como nota es necesario enviar el campo definicion");
            if (!string.IsNullOrEmpty(item.Articulo) && ArticulosService.Select(year, item.Articulo) == null)
                throw new InvalidOperationException($"No se encontró Artículo para Código='{item.Articulo}'");
            ComprobarSinTallaNiColor(item);
            if (!string.IsNullOrEmpty(item.Articulo))
                ComprobarStock(year, cabecera, item.Articulo, item.Unidades);

            string motivo = Contexto.Escritor.AnadirLineaAlbaranVenta(year, item.Empresa.Trim(), item.Numero, item.Letra, (item.Articulo ?? "").Trim(), item.Unidades,
                SiViene(item.Precio), SiViene(item.Dto1), SiViene(item.Dto2), item.Definicion, out int linea);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            var filtro = DelAlbaran(item.Empresa, item.Numero, item.Letra).Exacto("LINIA", linea.ToString(CultureInfo.InvariantCulture));
            FilaSql.Actualizar(year, "d_albven", filtro, new Dictionary<string, object> { ["USUARIO"] = CAlbcomService.Usuario });
            return Select(year, item.Empresa, item.Numero, item.Letra, linea);
        }

        /// <summary>
        /// Cambia las unidades, el precio, los dos descuentos y la descripción de la línea con la clase de albaranes de
        /// venta de Sage, que ajusta el stock con la diferencia y recalcula el importe y los totales, y la devuelve como
        /// queda guardada. El artículo no se puede cambiar. Lanza el motivo si no se puede.
        /// </summary>
        public static DAlbven Update(string year, DAlbven item)
        {
            ComprobarSinTallaNiColor(item);
            var guardada = Select(year, item.Empresa, item.Numero, item.Letra, item.Linia);
            if (guardada.Articulo.Trim() != (item.Articulo ?? "").Trim())
                throw new InvalidOperationException("No se puede cambiar el artículo de una línea: hay que borrarla y añadir otra.");
            if (!string.IsNullOrWhiteSpace(guardada.Articulo))
                ComprobarStock(year, CAlbvenService.Select(year, item.Empresa, item.Numero, item.Letra), guardada.Articulo, item.Unidades - guardada.Unidades);

            string motivo = Contexto.Escritor.ModificarLineaAlbaranVenta(year, item.Empresa.Trim(), item.Numero, item.Letra, item.Linia, item.Unidades, item.Precio,
                item.Dto1, item.Dto2, item.Definicion);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            return Select(year, item.Empresa, item.Numero, item.Letra, item.Linia);
        }

        /// <summary>
        /// Borra la línea con la clase de albaranes de venta de Sage, que devuelve sus unidades al stock y recalcula los
        /// totales. Lanza el motivo si Sage no lo permite.
        /// </summary>
        public static void Remove(string year, DAlbven item)
        {
            string motivo = Contexto.Escritor.BorrarLineaAlbaranVenta(year, item.Empresa.Trim(), item.Numero, item.Letra, item.Linia);
            if (motivo != null)
                throw new InvalidOperationException(motivo);
        }

        /// <summary>
        /// Rechaza la venta si el almacén del albarán no tiene esas unidades del artículo (Sage, sin pantalla, la dejaría
        /// pasar con stock negativo). No se comprueba en las devoluciones ni en los artículos que no llevan control de stock.
        /// </summary>
        private static void ComprobarStock(string year, CAlbven cabecera, string articulo, decimal unidades)
        {
            if (unidades <= 0)
                return;
            if (Db.Lector.LeerEjercicio<ControlStock>(year, "articulo", new Filtro { ["CODIGO"] = articulo }).Any(a => a.Stock))
                return;

            var enAlmacen = new Filtro { ["EMPRESA"] = cabecera.Empresa, ["ARTICULO"] = articulo, ["ALMACEN"] = cabecera.Almacen };
            decimal hay = Db.Lector.LeerEjercicio<Stocks2>(year, "stocks2", enAlmacen).Sum(s => s.Final);
            if (unidades > hay)
                throw new InvalidOperationException($"No hay stock suficiente del artículo en el almacén {cabecera.Almacen.Trim()}: hay {hay:0.##} y se piden {unidades:0.##}.");
        }

        private static decimal? SiViene(decimal valor) => valor == 0 ? (decimal?)null : valor;

        private static void ComprobarSinTallaNiColor(DAlbven item)
        {
            if (!string.IsNullOrWhiteSpace(item.Talla) || !string.IsNullOrWhiteSpace(item.Color))
                throw new InvalidOperationException("Las ventas de artículos con talla y color no están admitidas.");
        }

        private static Filtro DelAlbaran(string empresa, string numero, string letra)
        {
            return new Filtro { ["EMPRESA"] = empresa, ["NUMERO"] = numero, ["LETRA"] = letra };
        }
    }
}
