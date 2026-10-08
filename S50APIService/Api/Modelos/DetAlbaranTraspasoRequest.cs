using System.Collections.Generic;

namespace S50APIService.Api.Modelos
{
    public sealed class DetAlbaranTraspasoRequest : DAlbatr
    {
        public List<Compras> Series { get; set; }
    }
}
