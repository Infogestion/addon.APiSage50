using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Líneas de los albaranes de regularización de stock (tabla d_albare del ejercicio).</summary>
    internal static class DAlbareService
    {
        /// <summary>Las líneas del albarán, paginadas; con página y tamaño negativos, todas.</summary>
        public static List<DAlbare> Select(string year, string empresa, string numero, int page, int pageSize)
        {
            var consulta = new Consulta { Origen = "{d_albare}", Filtro = DelAlbaran(empresa, numero) };
            if (page >= 0 || pageSize >= 0)
                consulta.Pagina(page, pageSize);

            return Db.Lector.LeerEjercicio<DAlbare>(year, consulta);
        }

        public static DAlbare Select(string year, string empresa, string numero, int linia)
        {
            var filtro = DelAlbaran(empresa, numero).Exacto("LINIA", linia.ToString(CultureInfo.InvariantCulture));
            return Db.Lector.LeerEjercicio<DAlbare>(year, "d_albare", filtro).FirstOrDefault();
        }

        /// <summary>La primera línea del albarán con ese artículo.</summary>
        public static DAlbare SelectByArticulo(string year, string empresa, string numero, string articulo)
        {
            var filtro = DelAlbaran(empresa, numero).Igual("ARTICULO", articulo);
            return Db.Lector.LeerEjercicio<DAlbare>(year, "d_albare", filtro).FirstOrDefault();
        }

        /// <summary>Cada línea del albarán con la ficha de su artículo y su stock en el almacén (vacío si no tiene fila de stock en él).</summary>
        public static List<object> SelectConArticulo(string year, string empresa, string numero, string almacen, int page, int pageSize)
        {
            return Select(year, empresa, numero, page, pageSize).Select(linea => (object)new
            {
                linea,
                articulo = ArticulosService.SelectWithStockByArticulo(year, linea.Articulo).Where(a => a.Almacen.Trim() == almacen.Trim()).ToList(),
            }).ToList();
        }

        /// <summary>
        /// Añade una línea al albarán con la clase de regularizaciones de Sage, que le pone el coste del artículo y
        /// descuenta sus unidades del stock del almacén. De la petición solo se usan la empresa, el número, el artículo y
        /// las unidades; devuelve la línea como queda guardada. Lanza el motivo si no se puede.
        /// </summary>
        public static DAlbare Add(string year, DAlbare item)
        {
            Db.Lector.ComprobarEjercicio(year);
            ComprobarSinTallaNiColor(item);

            string motivo = Contexto.Escritor.AnadirLineaRegularizacion(year, item.Empresa.Trim(), item.Numero, item.Articulo.Trim(), item.Unidades, out int linea);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            return Select(year, item.Empresa, item.Numero, linea);
        }

        /// <summary>
        /// Cambia el artículo y las unidades de la línea con la clase de regularizaciones de Sage, que ajusta el stock con
        /// la diferencia, y la devuelve como queda guardada. Lanza el motivo si no se puede.
        /// </summary>
        public static DAlbare Update(string year, DAlbare item)
        {
            ComprobarSinTallaNiColor(item);

            string motivo = Contexto.Escritor.ModificarLineaRegularizacion(year, item.Empresa.Trim(), item.Numero, item.Linia, item.Articulo.Trim(), item.Unidades);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            return Select(year, item.Empresa, item.Numero, item.Linia);
        }

        /// <summary>
        /// Borra la línea con la clase de regularizaciones de Sage, que devuelve sus unidades al stock. Lanza el motivo si
        /// Sage no lo permite.
        /// </summary>
        public static void Remove(string year, DAlbare item)
        {
            string motivo = Contexto.Escritor.BorrarLineaRegularizacion(year, item.Empresa.Trim(), item.Numero, item.Linia);
            if (motivo != null)
                throw new InvalidOperationException(motivo);
        }

        private static void ComprobarSinTallaNiColor(DAlbare item)
        {
            if (!string.IsNullOrWhiteSpace(item.Talla) || !string.IsNullOrWhiteSpace(item.Color))
                throw new InvalidOperationException("Las regularizaciones de artículos con talla y color no están admitidas.");
        }

        private static Filtro DelAlbaran(string empresa, string numero)
        {
            return new Filtro { ["EMPRESA"] = empresa, ["NUMERO"] = numero };
        }
    }
}
