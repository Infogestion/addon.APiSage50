using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Líneas de los albaranes de traspaso entre almacenes (tabla d_albatr del ejercicio).</summary>
    internal static class DAlbatrService
    {
        /// <summary>
        /// Añade una línea al traspaso con la clase de traspasos de Sage, que pone el nombre y el coste del artículo, mueve
        /// el stock del almacén de origen al de destino y traspasa las series. De la petición solo se usan la empresa, el
        /// número, el artículo, las unidades y las series; la línea guardada vuelve en <paramref name="item"/>. Lanza el
        /// motivo si no se puede.
        /// </summary>
        public static void Add(string year, DetAlbaranTraspasoRequest item)
        {
            Db.Lector.ComprobarEjercicio(year);

            var cabecera = CAlbatrService.Select(year, item.Empresa, item.Numero);
            if (cabecera == null)
                throw new InvalidOperationException("No se encontró el traspaso.");
            if (string.IsNullOrWhiteSpace(item.Articulo))
                throw new InvalidOperationException("No se encontró el artículo.");
            if (!string.IsNullOrWhiteSpace(item.Talla) || !string.IsNullOrWhiteSpace(item.Color))
                throw new InvalidOperationException("Los traspasos de artículos con talla y color no están admitidos.");

            var series = (item.Series ?? new List<Compras>()).Select(s => s?.Serie?.Trim()).ToList();
            foreach (string serie in series)
                ComprobarSerie(item, cabecera, serie);
            if (series.Distinct().Count() != series.Count)
                throw new InvalidOperationException("Hay series repetidas.");

            string motivo = Contexto.Escritor.AnadirLineaTraspaso(year, item.Empresa.Trim(), item.Numero, item.Articulo.Trim(), item.Unidades,
                series.ToArray(), out int linea);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            var guardada = Select(year, item.Empresa, item.Numero, linea);
            foreach (var campo in typeof(DAlbatr).GetProperties())
                campo.SetValue(item, campo.GetValue(guardada));
        }

        private static DAlbatr Select(string year, string empresa, string numero, int linea)
        {
            return Db.Lector.LeerEjercicio<DAlbatr>(year, "d_albatr", new Filtro
            {
                ["EMPRESA"] = empresa,
                ["NUMERO"] = numero,
            }.Exacto("LINIA", linea.ToString(CultureInfo.InvariantCulture))).First();
        }

        /// <summary>
        /// Lo que Sage comprueba al teclear una serie en un traspaso y no al guardarla: que es del artículo, que no está
        /// dada de baja y que está en el almacén de origen.
        /// </summary>
        private static void ComprobarSerie(DetAlbaranTraspasoRequest item, CAlbatr cabecera, string serie)
        {
            var compra = ComprasService.SelectBySerie(item.Empresa, item.Articulo, serie);
            if (compra == null)
                throw new InvalidOperationException($"La serie {serie} no existe para el artículo {item.Articulo.Trim()}.");
            if (compra.Baja.Trim().ToUpperInvariant() == "S")
                throw new InvalidOperationException($"La serie {serie} está dada de baja.");
            if (compra.Almacen.Trim() != cabecera.Almorig.Trim())
                throw new InvalidOperationException($"La serie {serie} está en el almacén {compra.Almacen.Trim()}, no en el de origen ({cabecera.Almorig.Trim()}).");
        }
    }
}
