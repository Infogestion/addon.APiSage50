using System.Collections.Generic;

namespace S50APIService.Api.Modelos
{
    public sealed class AltaVenserRequest
    {
        public string Empresa { get; set; }
        public string Albaran { get; set; }
        public string Letra { get; set; }
        public int Linea { get; set; }
        public string Articulo { get; set; }
        public List<string> Series { get; set; }
    }
}
