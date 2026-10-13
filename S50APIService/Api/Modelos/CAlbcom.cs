using System;
using System.ComponentModel.DataAnnotations;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class CAlbcom
    {
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string Empresa { get; set; }

        [Required]
        public string Numero { get; set; }

        [Required]
        public string Proveedor { get; set; }

        [StringLength(12)]
        public string Albven { get; set; }

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string Almacen { get; set; }

        public DateTime Fecha { get; set; }

        [StringLength(20, MinimumLength = 20)]
        public string Asi { get; set; }

        public decimal Pronto { get; set; }

        [StringLength(20, MinimumLength = 20)]
        public string Factura { get; set; }

        [Columna("FECHA_FAC")]
        public DateTime? FechaFac { get; set; }

        [StringLength(2, MinimumLength = 2)]
        public string Fpag { get; set; }

        public decimal Importe { get; set; }
        public string Observacio { get; set; }

        [StringLength(3, MinimumLength = 3)]
        public string Divisa { get; set; }

        public decimal Impdivisa { get; set; }
        public decimal Cambio { get; set; } = 1;
        public bool? Vista { get; set; }
        public bool Traspasado { get; set; }
        public bool Recequiv { get; set; }

        [StringLength(5)]
        public string Obra { get; set; }

        [StringLength(2, MinimumLength = 2)]
        public string Operario { get; set; }

        public bool Comms { get; set; }

        [Columna("LIBRE_1")]
        public string Libre1 { get; set; }

        [Columna("LIBRE_2")]
        public string Libre2 { get; set; }

        [Columna("LIBRE_3")]
        public string Libre3 { get; set; }

        [StringLength(10)]
        public string Grupo { get; set; }

        [StringLength(14)]
        public string Keycopy { get; set; }

        [Columna("STOCK_COEF")]
        public int StockCoef { get; set; }

        public bool? Facturable { get; set; }
        public DateTime Fechastock { get; set; }
        public bool Isp { get; set; }
        public bool Recc { get; set; }
        public decimal Totaldoc { get; set; }

        [StringLength(30)]
        public string Encargado { get; set; }

        public bool Impreso { get; set; }
        public decimal Totaldiv { get; set; }
        public decimal Retencio { get; set; }

        [Columna("RET_DIV")]
        public decimal RetDiv { get; set; }

        [Columna("PORCEN_RET")]
        public decimal PorcenRet { get; set; }

        public bool Traspejer { get; set; }
        public string Descfac { get; set; }
        public bool Fradirecta { get; set; }

        [Columna("MODO_RET")]
        public int ModoRet { get; set; }
    }
}
