using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class ContCli
    {
        public string Cliente { get; set; }
        public string Persona { get; set; }
        public string Cargo { get; set; }
        public string Email { get; set; }
        public int Linea { get; set; }
        public bool Orden { get; set; }
        public bool? Vista { get; set; }
        public string Telefono { get; set; }
        public string Guid { get; set; }
        public DateTime? Exportar { get; set; }
        public DateTime? Importar { get; set; }
        [Columna("GUID_EXP")]
        public string GuidExp { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
