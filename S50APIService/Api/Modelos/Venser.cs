using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class Venser
    {
        public string Empresa { get; set; }
        public string Albaran { get; set; }
        public string Articulo { get; set; }
        public int Linea { get; set; }
        public string Serie { get; set; }
        public bool? Vista { get; set; }
        public string Numero { get; set; }
        public bool Modelo { get; set; }
        public string Letra { get; set; }
        public DateTime? Fechalog { get; set; }
        public string Lote { get; set; }
        public string Ubica { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
