using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/Clientes")]
    [Autorizar]
    public sealed class ClientesController : ApiController
    {
        /// <summary>
        /// Los clientes están en la base de datos del ejercicio, pero la ruta no lleva {year}:
        /// interface.s50c usa el año en curso.
        /// </summary>
        private static string Ejercicio => DateTime.Now.Year.ToString();

        /// <summary>
        /// Permite obtener todos los Clientes
        /// </summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<Clientes>))]
        public HttpResponseMessage Get()
        {
            return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Clientes>(Ejercicio, "clientes"));
        }

        /// <summary>
        /// Permite obtener un cliente
        /// </summary>
        [HttpGet]
        [Route("ByCode")]
        [ResponseType(typeof(Clientes))]
        public HttpResponseMessage GetByCode(string code = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                return CodigoObligatorio();

            var cliente = LeerCliente(code);
            return cliente == null ? Request.CreateResponse(HttpStatusCode.NoContent) : Respuestas.Json(cliente);
        }

        /// <summary>
        /// Permite obtener un cliente
        /// </summary>
        [HttpGet]
        [Route("Emails")]
        [ResponseType(typeof(ClienteConEmails))]
        public HttpResponseMessage GetEmails(string code = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                return CodigoObligatorio();

            var resultado = new ClienteConEmails { cli = LeerCliente(code) };
            if (resultado.cli != null)
                resultado.contCli = Contexto.Lector.LeerEjercicio<ContCli>(Ejercicio, "cont_cli", "LTRIM(RTRIM([CLIENTE])) = @code",
                    new Dictionary<string, string> { ["@code"] = code.Trim() });
            return Respuestas.Json(resultado);
        }

        private static Clientes LeerCliente(string code)
        {
            return Contexto.Lector.LeerEjercicio<Clientes>(Ejercicio, "clientes", "LTRIM(RTRIM([CODIGO])) = @code",
                new Dictionary<string, string> { ["@code"] = code.Trim() }).FirstOrDefault();
        }

        private static HttpResponseMessage CodigoObligatorio()
        {
            return Respuestas.ErrorValidacion(new Dictionary<string, string[]> { ["code"] = new[] { "The code field is required." } });
        }
    }
}
