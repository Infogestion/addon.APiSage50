using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class CAlbatr
    {
        public string Usuario { get; set; }
        public string Empresa { get; set; }
        public string Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string Observacio { get; set; }
        public string Almdest { get; set; }
        public string Almorig { get; set; }
        public bool? Vista { get; set; }
        public bool Comms { get; set; }
        public string Operario { get; set; }
        public decimal Estado { get; set; }
        public string Keycopy { get; set; }
        public bool? Actualizarstock { get; set; }
        public DateTime Fechastock { get; set; }
        [Columna("LIBRE_1")]
        public string Libre1 { get; set; }
        [Columna("LIBRE_2")]
        public string Libre2 { get; set; }
        [Columna("LIBRE_3")]
        public string Libre3 { get; set; }
        public string Obra { get; set; }
        public DateTime? Exportar { get; set; }
        [Columna("GUID_EXP")]
        public string GuidExp { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
