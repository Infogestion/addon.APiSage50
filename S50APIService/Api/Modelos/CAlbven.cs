using System;
using System.ComponentModel.DataAnnotations;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class CAlbven
    {
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string Empresa { get; set; }

        [Required]
        public string Cliente { get; set; }

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string Letra { get; set; }

        [StringLength(10, MinimumLength = 10)]
        public string Numero { get; set; }

        [StringLength(2, MinimumLength = 2)]
        public string Almacen { get; set; }

        [StringLength(2, MinimumLength = 2)]
        public string Vendedor { get; set; }

        [StringLength(2, MinimumLength = 2)]
        public string Fpag { get; set; }

        [StringLength(2, MinimumLength = 2)]
        public string Operario { get; set; }

        [StringLength(12, MinimumLength = 12)]
        public string Pedido { get; set; }

        public DateTime Fecha { get; set; }

        [Columna("ENV_CLI")]
        public int EnvCli { get; set; }

        [StringLength(10, MinimumLength = 10)]
        public string Presup { get; set; }

        public decimal Pronto { get; set; }

        [StringLength(2, MinimumLength = 2)]
        public string Ruta { get; set; }

        [Columna("IVA_INC")]
        public bool IvaInc { get; set; }

        [StringLength(10, MinimumLength = 10)]
        public string Factura { get; set; }

        [Columna("FECHA_FAC")]
        public DateTime? FechaFac { get; set; }

        [StringLength(20, MinimumLength = 20)]
        public string Asi { get; set; }

        public decimal Importe { get; set; }
        public string Observacio { get; set; }

        [Columna("BANC_CLI")]
        public int BancCli { get; set; }

        [StringLength(3, MinimumLength = 3)]
        public string Divisa { get; set; } = "000";

        public decimal Cambio { get; set; } = 1;
        public decimal Impdivisa { get; set; }
        public decimal Finan { get; set; }
        public bool? Vista { get; set; }
        public decimal Coste { get; set; }
        public decimal Peso { get; set; }
        public decimal Litros { get; set; }

        [StringLength(5, MinimumLength = 5)]
        public string Obra { get; set; }

        public bool Traspasado { get; set; }
        public bool Recequiv { get; set; }
        public bool Tag { get; set; }
        public bool? Facturable { get; set; }

        [Columna("COT_PUNT")]
        public decimal CotPunt { get; set; }

        public decimal Puntos { get; set; }
        public bool Comms { get; set; }

        [Columna("SEND_FRA")]
        public bool SendFra { get; set; }

        [StringLength(10, MinimumLength = 10)]
        public string Clifinal { get; set; }

        [StringLength(14, MinimumLength = 14)]
        public string Keycopy { get; set; }

        public bool Impreso { get; set; }

        [StringLength(10, MinimumLength = 10)]
        [Columna("LIBRE_1")]
        public string Libre1 { get; set; }

        [StringLength(10, MinimumLength = 10)]
        [Columna("LIBRE_2")]
        public string Libre2 { get; set; }

        [StringLength(10, MinimumLength = 10)]
        [Columna("LIBRE_3")]
        public string Libre3 { get; set; }

        public int Certific { get; set; }

        [Columna("STOCK_COEF")]
        public decimal StockCoef { get; set; }

        public decimal Tpcretnofi { get; set; }
        public bool Edi { get; set; }
        public int Enviado { get; set; }
        public DateTime Fechastock { get; set; }
        public bool? Gastos { get; set; }

        [Columna("LIBRE_4")]
        public int Libre4 { get; set; }

        public DateTime? Exportar { get; set; }

        [StringLength(35, MinimumLength = 35)]
        public string Mandato { get; set; }

        [StringLength(15, MinimumLength = 15)]
        public string Clienteerp { get; set; }

        public bool Recc { get; set; }
        public decimal Totaldoc { get; set; }

        [StringLength(10, MinimumLength = 10)]
        public string Codpost { get; set; }

        [StringLength(10, MinimumLength = 10)]
        public string Canal { get; set; }

        public bool? Trasperp { get; set; } = false;
        public decimal Totaldiv { get; set; }

        [Columna("PORCEN_RET")]
        public decimal PorcenRet { get; set; }

        public int Calculo { get; set; }
        public string Descfac { get; set; }

        [StringLength(25, MinimumLength = 25)]
        public string Refercli { get; set; }

        public bool Fradirecta { get; set; }

        [Columna("MODO_RET")]
        public int ModoRet { get; set; }
    }
}
