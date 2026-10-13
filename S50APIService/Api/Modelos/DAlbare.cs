using System.ComponentModel.DataAnnotations;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class DAlbare
    {
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string Empresa { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string Numero { get; set; }

        public int Linia { get; set; }

        [Required]
        public string Articulo { get; set; }

        public decimal Unidades { get; set; }

        [StringLength(4, MinimumLength = 4)]
        public string Talla { get; set; }

        [StringLength(2, MinimumLength = 2)]
        public string Color { get; set; }

        public bool? Vista { get; set; }
        public decimal Peso { get; set; }
        public decimal Cajas { get; set; }
        public decimal Coste { get; set; }
        public int Tipo { get; set; }

        [StringLength(15, MinimumLength = 15)]
        [Columna("LIBRE_1")]
        public string Libre1 { get; set; }

        [StringLength(15, MinimumLength = 15)]
        [Columna("LIBRE_2")]
        public string Libre2 { get; set; }

        [StringLength(15, MinimumLength = 15)]
        [Columna("LIBRE_3")]
        public string Libre3 { get; set; }

        public string Serie { get; set; }

        [StringLength(30, MinimumLength = 30)]
        public string Escandal { get; set; }
    }
}
