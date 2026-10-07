using System;
using System.Collections.Generic;

namespace S50APIService.Api.Modelos
{
    /// <summary>Cuerpo de dar-baja-series: las series de un artículo que se han entregado en un albarán de venta.</summary>
    public sealed class BajaSeriesRequest
    {
        public string Empresa { get; set; }
        public string Articulo { get; set; }
        public string AlbaranVenta { get; set; }
        public string Cliente { get; set; }
        public DateTime FechaVenta { get; set; }
        public List<string> Series { get; set; }
    }
}
