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
    [RoutePrefix("api/Vendedores")]
    [Autorizar]
    public sealed class VendedoresController : ApiController
    {
        /// <summary>
        /// Los vendedores están en la base de datos del ejercicio, pero la ruta no lleva {year}:
        /// interface.s50c usa el año en curso.
        /// </summary>
        private static string Ejercicio => DateTime.Now.Year.ToString();

        /// <summary>
        /// Permite obtener todos los Vendedores
        /// </summary>
        [HttpGet]
        [Route("")]
        public List<Vendedor> Get()
        {
            return Contexto.Lector.LeerEjercicio<Vendedor>(Ejercicio, "vendedor");
        }

        /// <summary>
        /// Permite obtener un vendedor
        /// </summary>
        [HttpGet]
        [Route("ByCode")]
        [ResponseType(typeof(Vendedor))]
        public HttpResponseMessage GetByCode(string code = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Respuestas.ErrorValidacion(new Dictionary<string, string[]> { ["code"] = new[] { "The code field is required." } });

            var vendedor = Contexto.Lector.LeerEjercicio<Vendedor>(Ejercicio, "vendedor", "LTRIM(RTRIM([CODIGO])) = @code",
                new Dictionary<string, string> { ["@code"] = code.Trim() }).FirstOrDefault();
            return vendedor == null ? Request.CreateResponse(HttpStatusCode.NoContent) : Respuestas.Json(vendedor);
        }
    }
}
