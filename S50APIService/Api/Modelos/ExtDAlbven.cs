using System.ComponentModel.DataAnnotations;

namespace S50APIService.Api.Modelos
{
    public sealed class ExtDAlbven
    {
        public string Ejercicio { get; set; }

        [Required(AllowEmptyStrings = true)]
        public string Empresa { get; set; }

        [Required(AllowEmptyStrings = true)]
        public string Numero { get; set; }

        public int Linea { get; set; }
        public decimal Unidadespreparadas { get; set; }

        [Required(AllowEmptyStrings = true)]
        public string Letra { get; set; }
    }
}
