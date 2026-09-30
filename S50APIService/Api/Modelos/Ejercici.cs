using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class Ejercici
    {
        public string Any { get; set; }
        public string Ruta { get; set; }
        public string Rutaser { get; set; }
        public string Conexion { get; set; }
        public bool Predet { get; set; }
        public DateTime Periodoini { get; set; }
        public DateTime Periodofin { get; set; }
        public bool? Vista { get; set; }
        public string Anterior { get; set; }
        public string Grupo { get; set; }
        [Columna("NO_ACT")]
        public bool NoAct { get; set; }
    }
}
