using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class Stocks2
    {
        public decimal Albaregu { get; set; }
        public string Almacen { get; set; }
        public string Articulo { get; set; }
        public string Color { get; set; }
        public decimal Compra { get; set; }
        public decimal Deposito { get; set; }
        public decimal Devolpro { get; set; }
        public string Empresa { get; set; }
        [Columna("ENTRADA_PROD")]
        public decimal EntradaProd { get; set; }
        [Columna("ENTRADA_TRANS")]
        public decimal EntradaTrans { get; set; }
        public decimal Entradas { get; set; }
        public decimal Final { get; set; }
        public decimal Inicial { get; set; }
        public decimal Montajes { get; set; }
        public decimal Pmcom { get; set; }
        public decimal Regulariza { get; set; }
        [Columna("SALIDA_PROD")]
        public decimal SalidaProd { get; set; }
        [Columna("SALIDA_TRANS")]
        public decimal SalidaTrans { get; set; }
        public decimal Salidas { get; set; }
        public string Talla { get; set; }
        [Columna("TRASP_ENT")]
        public decimal TraspEnt { get; set; }
        [Columna("TRASP_SAL")]
        public decimal TraspSal { get; set; }
        public string Usuario { get; set; }
        public decimal Venta { get; set; }
        public bool? Vista { get; set; }
        [Columna("ULT_ACT")]
        public DateTime UltAct { get; set; }
        public decimal Depcom { get; set; }
        [Columna("COM_ESTADO")]
        public int ComEstado { get; set; }
        public DateTime? Exportar { get; set; }
    }
}
