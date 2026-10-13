using System;
using System.ComponentModel.DataAnnotations;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class CAlbare
    {
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string Empresa { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string Numero { get; set; }

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string Almacen { get; set; }

        public DateTime Fecha { get; set; }
        public string Observacio { get; set; }
        public bool? Vista { get; set; }
        public bool Comms { get; set; }

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string Operario { get; set; }

        public DateTime Fechastock { get; set; }

        [StringLength(2, MinimumLength = 2)]
        [Columna("TIPO_REG")]
        public string TipoReg { get; set; }
    }
}
