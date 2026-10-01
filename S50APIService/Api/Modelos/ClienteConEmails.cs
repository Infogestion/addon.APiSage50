using System.Collections.Generic;

namespace S50APIService.Api.Modelos
{
    public sealed class ClienteConEmails
    {
        public Clientes cli { get; set; }
        public List<ContCli> contCli { get; set; }
    }
}