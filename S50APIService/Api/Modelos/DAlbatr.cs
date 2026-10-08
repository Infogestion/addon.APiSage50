using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public class DAlbatr
    {
        public string Usuario { get; set; }
        public string Empresa { get; set; }
        public string Numero { get; set; }
        public string Articulo { get; set; }
        public decimal Unidades { get; set; }
        public int Linia { get; set; }
        public bool? Vista { get; set; }
        public decimal Pmcom { get; set; }
        public string Serie { get; set; }
        public string Nombre { get; set; }
        public string Color { get; set; }
        public string Talla { get; set; }
        public int Tipo { get; set; }
        public decimal Peso { get; set; }
        [Columna("LIBRE_1")]
        public string Libre1 { get; set; }
        [Columna("LIBRE_2")]
        public string Libre2 { get; set; }
        [Columna("LIBRE_3")]
        public string Libre3 { get; set; }
        [Columna("LIBRE_4")]
        public string Libre4 { get; set; }
        [Columna("LIBRE_5")]
        public string Libre5 { get; set; }
        public string Asi { get; set; }
        public decimal Cajas { get; set; }
        public string Escandal { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
