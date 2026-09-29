using System.Net.Http;
using System.Web.Http;
using S50APIService.Api.Autenticacion;

namespace S50APIService.Api.Controladores
{
    /// <summary>Rutas básicas de AuthenticationController de interface.s50c (las de Nadilux se añadirán más adelante).</summary>
    public sealed class AutenticacionController : ApiController
    {
        /// <summary>Token Bearer para llamar al resto de rutas. 200 con el token como texto o 401.</summary>
        [HttpPost]
        [Route("api/token/{username}/{password}")]
        public HttpResponseMessage GetToken(string username, string password)
        {
            if (Contexto.Usuarios.Autenticar(username, password))
                return Respuestas.Texto(Contexto.Jwt.GenerarToken(username));
            return Respuestas.NoAutorizado();
        }

        /// <summary>true si el token es válido; si no, 401 (lo resuelve [Autorizar]).</summary>
        [HttpGet]
        [Route("api/validate-token")]
        [Autorizar]
        public bool ValidateTokenApi()
        {
            return true;
        }

        /// <summary>Prueba de conexión con la API: siempre true, igual que interface.s50c.</summary>
        [HttpGet]
        [Route("api/has-connection")]
        public bool HasConnectionApi()
        {
            return true;
        }
    }
}
