using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class Compras
    {
        public string Usuario { get; set; }
        public string Serie { get; set; }
        public string Articulo { get; set; }
        public string Nombre { get; set; }
        public decimal Coste { get; set; }
        public string Baja { get; set; }
        public string Familia { get; set; }
        public DateTime? Garantia { get; set; }
        public decimal Venta { get; set; }
        public string Modelo { get; set; }
        public DateTime Fecha { get; set; }
        public string Albaran { get; set; }
        public string Proveedor { get; set; }
        [Columna("ALB_VENTA")]
        public string AlbVenta { get; set; }
        [Columna("FEC_VENTA")]
        public DateTime? FecVenta { get; set; }
        public string Devolucion { get; set; }
        public string Tecnico { get; set; }
        public string Any { get; set; }
        [Columna("ALB_DEPO")]
        public string AlbDepo { get; set; }
        [Columna("FEC_DEPO")]
        public DateTime? FecDepo { get; set; }
        public string Empresa { get; set; }
        public int Linea { get; set; }
        public bool? Vista { get; set; }
        public string Almacen { get; set; }
        public string Codemp { get; set; }
        public string Codempcom { get; set; }
        public string Talla { get; set; }
        public string Color { get; set; }
        public DateTime? Fbaja { get; set; }
        public string Deposito { get; set; }
        public string Almaini { get; set; }
        public int Tipo { get; set; }
        public string Cliente { get; set; }
        public int Orden { get; set; }
        [Columna("LIBRE_1")]
        public string Libre1 { get; set; }
        [Columna("LIBRE_2")]
        public string Libre2 { get; set; }
        [Columna("LIBRE_3")]
        public string Libre3 { get; set; }
        [Columna("LIBRE_4")]
        public string Libre4 { get; set; }
        public int Linord { get; set; }
        public string Lote { get; set; }
        public string Ubica { get; set; }
        public DateTime? Exportar { get; set; }
        [Columna("GUID_EXP")]
        public string GuidExp { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
