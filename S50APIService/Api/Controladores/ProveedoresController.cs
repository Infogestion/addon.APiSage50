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
    [RoutePrefix("api/Proveedores")]
    [Autorizar]
    public sealed class ProveedoresController : ApiController
    {
        /// <summary>
        /// Los proveedores están en la base de datos del ejercicio, pero la ruta no lleva {year}:
        /// interface.s50c usa el año en curso.
        /// </summary>
        private static string Ejercicio => DateTime.Now.Year.ToString();

        /// <summary>
        /// Permite obtener todos los Proveedores
        /// </summary>
        [HttpGet]
        [Route("")]
        public List<Proveed> Get(int page = 1, int pageSize = 100)
        {
            return Contexto.Lector.LeerEjercicio<Proveed>(Ejercicio, "proveed");
        }

        /// <summary>
        /// Permite obtener un proveedor
        /// </summary>
        [HttpGet]
        [Route("ByCode")]
        [ResponseType(typeof(Proveed))]
        public HttpResponseMessage GetByCode(string code = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Respuestas.ErrorValidacion(new Dictionary<string, string[]> { ["code"] = new[] { "The code field is required." } });

            var proveedor = Contexto.Lector.LeerEjercicio<Proveed>(Ejercicio, "proveed", "LTRIM(RTRIM([CODIGO])) = @code",
                new Dictionary<string, string> { ["@code"] = code.Trim() }).FirstOrDefault();
            return proveedor == null ? Request.CreateResponse(HttpStatusCode.NoContent) : Respuestas.Json(proveedor);
        }
    }
}
