using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class Articulo
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Abrev { get; set; }
        public string Familia { get; set; }
        public string Marca { get; set; }
        public decimal Minimo { get; set; }
        public decimal Maximo { get; set; }
        public bool Aviso { get; set; }
        public bool Baja { get; set; }
        [Columna("TIPO_IVA")]
        public string TipoIva { get; set; }
        public string Retencion { get; set; }
        [Columna("IVA_INC")]
        public bool IvaInc { get; set; }
        [Columna("COST_ULT1")]
        public decimal CostUlt1 { get; set; }
        [Columna("FECHA_ULT")]
        public DateTime? FechaUlt { get; set; }
        [Columna("ULT_FECHA")]
        public DateTime? UltFecha { get; set; }
        public decimal Pmcom1 { get; set; }
        public string Imagen { get; set; }
        public string Carac { get; set; }
        public DateTime Fechaalta { get; set; }
        public DateTime Fechabaja { get; set; }
        public string Ubicacion { get; set; }
        public string Medidas { get; set; }
        public string Peso { get; set; }
        public string Litros { get; set; }
        public string Observacio { get; set; }
        public decimal Unicaja { get; set; }
        public int Desglose { get; set; }
        public decimal Aranceles { get; set; }
        public string Definicion2 { get; set; }
        public string Subfamilia { get; set; }
        public bool Internet { get; set; }
        public bool? Vista { get; set; }
        public string Fpag { get; set; }
        public bool Pverde { get; set; }
        [Columna("P_IMPORTE")]
        public decimal PImporte { get; set; }
        [Columna("P_TAN")]
        public int PTan { get; set; }
        public bool Lcolor { get; set; }
        public decimal Margen { get; set; }
        public string Tcp { get; set; }
        public bool Venserie { get; set; }
        public decimal Puntos { get; set; }
        [Columna("DES_ESC")]
        public bool DesEsc { get; set; }
        [Columna("TIPO_ART")]
        public int TipoArt { get; set; }
        public string Modelo { get; set; }
        public bool Cocina { get; set; }
        public bool Stock { get; set; }
        [Columna("ART_IMPUES")]
        public string ArtImpues { get; set; }
        public string Nombre2 { get; set; }
        [Columna("COLOR_ART")]
        public string ColorArt { get; set; }
        [Columna("TIPO_PVP")]
        public decimal TipoPvp { get; set; }
        [Columna("COST_ESCAN")]
        public int CostEscan { get; set; }
        [Columna("TIPO_ESCAN")]
        public int TipoEscan { get; set; }
        [Columna("ART_CANON")]
        public bool ArtCanon { get; set; }
        [Columna("ACTUA_COLO")]
        public int ActuaColo { get; set; }
        [Columna("FACT_AREPE")]
        public bool FactArepe { get; set; }
        public int Garantia { get; set; }
        public bool Alquiler { get; set; }
        public int Orden { get; set; }
        [Columna("C_ENT")]
        public string CEnt { get; set; }
        public string Cn8 { get; set; }
        public bool Ivalot { get; set; }
        public string Artant { get; set; }
        public string Reportetiq { get; set; }
        public DateTime? Importar { get; set; }
        public decimal Dto1 { get; set; }
        public decimal Dto2 { get; set; }
        public decimal Dto3 { get; set; }
        public bool Isp { get; set; }
        public int Grupoiva { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public bool Suplidos { get; set; }
        public string Csuplido { get; set; }
        public string Contrapar { get; set; }
        public string Contrapco { get; set; }
        public int Tipo { get; set; }
        public string Codintradi { get; set; }
        public bool Segurointr { get; set; }
    }
}
