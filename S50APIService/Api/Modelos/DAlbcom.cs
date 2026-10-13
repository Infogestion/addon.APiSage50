using System;
using System.ComponentModel.DataAnnotations;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class DAlbcom
    {
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string Empresa { get; set; }

        [Required]
        public string Numero { get; set; }

        [Required]
        public string Articulo { get; set; }

        public string Definicion { get; set; }
        public decimal Unidades { get; set; }
        public decimal Precio { get; set; }
        public decimal Dto1 { get; set; }
        public decimal Dto2 { get; set; }
        public decimal Importe { get; set; }

        [StringLength(2, MinimumLength = 2)]
        [Columna("TIPO_IVA")]
        public string TipoIva { get; set; }

        public decimal Coste { get; set; }

        [StringLength(10, MinimumLength = 10)]
        public string Cuenta { get; set; }

        [StringLength(10, MinimumLength = 10)]
        public string Pedido { get; set; }

        public DateTime Fecha { get; set; }
        public int Linia { get; set; }

        [Required]
        public string Proveedor { get; set; }

        public decimal Cajas { get; set; }
        public decimal Preciodiv { get; set; }
        public decimal Importediv { get; set; }
        public DateTime? Garantia { get; set; }
        public bool? Vista { get; set; }
        public decimal Peso { get; set; }
        public int Doc { get; set; }

        [Columna("DOC_LIN")]
        public int DocLin { get; set; }

        [Columna("DOC_UNID")]
        public decimal DocUnid { get; set; }

        [Columna("DOC_NUM")]
        public string DocNum { get; set; }

        public string Cliente { get; set; }
        public string Color { get; set; }
        public string Talla { get; set; }

        [Columna("DTO3_IMP")]
        public decimal Dto3Imp { get; set; }

        [Columna("DTO3_IMPDI")]
        public decimal Dto3Impdi { get; set; }

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
        public decimal Dto4 { get; set; }
        public decimal Dto5 { get; set; }
        public decimal Dto6 { get; set; }
        public decimal Dto7 { get; set; }
        public bool Lote { get; set; }

        [Columna("LIBRE_6")]
        public decimal Libre6 { get; set; }

        [Columna("LIBRE_7")]
        public decimal Libre7 { get; set; }

        public decimal Rappel { get; set; }
        public string Albven { get; set; }

        [Columna("DOC_CAJA")]
        public decimal DocCaja { get; set; }

        [Required]
        public string Almacen { get; set; }

        public decimal Tipoprec { get; set; }

        [StringLength(2, MinimumLength = 2)]
        [Columna("TIPO_ART")]
        public string TipoArt { get; set; }

        public decimal Unimedida { get; set; }
        public decimal Premedida { get; set; }
        public string Escandal { get; set; }
    }
}
