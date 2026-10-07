using System.Net.Http;
using System.Web.Http;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;

namespace S50APIService.Api.Controladores
{
    /// <summary>
    /// Los usuarios y los tokens están en la carpeta Seguridad: <see cref="Seguridad.UsuariosApi"/> y
    /// <see cref="Seguridad.ServicioJwt"/> hacen de AuthenticationService de interface.s50c, y
    /// <see cref="Seguridad.UsuariosNadilux"/> de los login de ConductorService y OperariosService.
    /// </summary>
    public sealed class AuthenticationController : ApiController
    {
        /// <summary>GetToken</summary>
        [HttpPost]
        [Route("api/token/{username}/{password}")]
        public HttpResponseMessage GetToken(string username, string password)
        {
            if (Contexto.Usuarios.Autenticar(username, password))
                return Respuestas.Texto(Contexto.Jwt.GenerarToken(username));
            return Respuestas.NoAutorizado();
        }

        /// <summary>GetTokenAppNadiluxRepartos</summary>
        [HttpPost]
        [Route("api/nadilux-repartos/{business}/token")]
        public HttpResponseMessage GetTokenAppNadiluxRepartos([FromBody] LoginNadRepartosRequest request, string business)
        {
            var invalido = ValidacionCuerpo.Error(Request, request, ModelState);
            if (invalido != null)
                return invalido;

            string codigo = Contexto.UsuariosNadilux.AutenticarConductor(request.Username, request.Password);
            if (codigo == null)
                return Respuestas.NoAutorizado();
            return Respuestas.Json(new { token = Contexto.Jwt.GenerarTokenNadilux(request.Username, business), driverCode = codigo });
        }

        /// <summary>ValidateTokenApi</summary>
        [HttpGet]
        [Route("api/validate-token")]
        [Autorizar]
        public bool ValidateTokenApi()
        {
            return true;
        }

        /// <summary>ValidateToken</summary>
        [HttpGet]
        [Route("api/nadilux-repartos/validate-token")]
        [Autorizar]
        public bool ValidateToken()
        {
            return true;
        }

        /// <summary>Prueba de conexíón a la api</summary>
        [HttpGet]
        [Route("api/has-connection")]
        public bool HasConnectionApi()
        {
            return true;
        }

        /// <summary>Prueba de conexíón a la api</summary>
        [HttpGet]
        [Route("api/nadilux-repartos/has-connection")]
        public bool HasConnection()
        {
            return true;
        }

        /// <summary>GetTokenOperarioGestionMercancia</summary>
        [HttpPost]
        [Route("api/nadilux-mercancias/{business}/token")]
        public HttpResponseMessage GetTokenOperarioGestionMercancia([FromBody] LoginNadRepartosRequest request, string business)
        {
            var invalido = ValidacionCuerpo.Error(Request, request, ModelState);
            if (invalido != null)
                return invalido;

            string codigo = Contexto.UsuariosNadilux.AutenticarOperario(request.Username, request.Password.TrimEnd('\n', '\r'));
            if (codigo == null)
                return Respuestas.NoAutorizado();
            return Respuestas.Json(new { token = Contexto.Jwt.GenerarTokenNadilux(request.Username, business), operarioCode = codigo });
        }
    }
}
