using System.Linq;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Clientes y sus contactos (tablas clientes y cont_cli del ejercicio en curso).</summary>
    internal static class ClientesService
    {
        public static ListaJson Select()
        {
            return Db.Lector.LeerEjercicioJson<Clientes>(Db.EjercicioActual, "clientes");
        }

        public static Clientes Select(string codigo)
        {
            return Db.Lector.LeerEjercicio<Clientes>(Db.EjercicioActual, "clientes", new Filtro { ["CODIGO"] = codigo }).FirstOrDefault();
        }

        /// <summary>El cliente con sus contactos. Si el cliente no existe, los dos van a null.</summary>
        public static ClienteConEmails SelectEmails(string codigo)
        {
            var resultado = new ClienteConEmails { cli = Select(codigo) };
            if (resultado.cli != null)
                resultado.contCli = Db.Lector.LeerEjercicio<ContCli>(Db.EjercicioActual, "cont_cli", new Filtro { ["CLIENTE"] = codigo });
            return resultado;
        }
    }
}
