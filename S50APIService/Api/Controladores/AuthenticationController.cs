using System.Net.Http;
using System.Web.Http;
using S50APIService.Api.Autenticacion;

namespace S50APIService.Api.Controladores
{
    // Rutas básicas de AuthenticationController de interface.s50c (las de Nadilux se añadirán más adelante).
    // Los controladores se llaman igual que en interface.s50c: Swagger los agrupa por ese nombre.
    // Los <summary> son los textos que muestra Swagger en interface.s50c (incluidas sus erratas).
    public sealed class AuthenticationController : ApiController
    {
        // Token Bearer para llamar al resto de rutas: 200 con el token como texto o 401.
        /// <summary>GetToken</summary>
        [HttpPost]
        [Route("api/token/{username}/{password}")]
        public HttpResponseMessage GetToken(string username, string password)
        {
            if (Contexto.Usuarios.Autenticar(username, password))
                return Respuestas.Texto(Contexto.Jwt.GenerarToken(username));
            return Respuestas.NoAutorizado();
        }

        // true si el token es válido; si no, 401 (lo resuelve [Autorizar]).
        /// <summary>ValidateTokenApi</summary>
        [HttpGet]
        [Route("api/validate-token")]
        [Autorizar]
        public bool ValidateTokenApi()
        {
            return true;
        }

        // Prueba de conexión con la API: siempre true, igual que interface.s50c.
        /// <summary>Prueba de conexíón a la api</summary>
        [HttpGet]
        [Route("api/has-connection")]
        public bool HasConnectionApi()
        {
            return true;
        }
    }
}
