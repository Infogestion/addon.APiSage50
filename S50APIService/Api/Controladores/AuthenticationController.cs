using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;

namespace S50APIService.Api.Controladores
{
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

        // Login de la app Nadilux Repartos con un conductor: 200 con {"token", "driverCode"}, 401 o 400 si falta el cuerpo.
        /// <summary>GetTokenAppNadiluxRepartos</summary>
        [HttpPost]
        [Route("api/nadilux-repartos/{business}/token")]
        public HttpResponseMessage GetTokenAppNadiluxRepartos([FromBody] LoginNadRepartosRequest request, string business)
        {
            var invalido = ValidarLogin(request);
            if (invalido != null)
                return invalido;

            string codigo = Contexto.UsuariosNadilux.AutenticarConductor(request.Username, request.Password);
            if (codigo == null)
                return Respuestas.NoAutorizado();
            return Respuestas.Json(new { token = Contexto.Jwt.GenerarTokenNadilux(request.Username, business), driverCode = codigo });
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

        // Igual que api/validate-token; interface.s50c la mantiene para la app de repartos.
        /// <summary>ValidateToken</summary>
        [HttpGet]
        [Route("api/nadilux-repartos/validate-token")]
        [Autorizar]
        public bool ValidateToken()
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

        // Igual que api/has-connection; interface.s50c la mantiene para la app de repartos.
        /// <summary>Prueba de conexíón a la api</summary>
        [HttpGet]
        [Route("api/nadilux-repartos/has-connection")]
        public bool HasConnection()
        {
            return true;
        }

        // Login de la app Nadilux Mercancías con un operario: 200 con {"token", "operarioCode"}, 401 o 400 si falta el cuerpo.
        // El token es el mismo que el de repartos (interface.s50c usa GenerateTokenNadiluxRepartos en los dos).
        /// <summary>GetTokenOperarioGestionMercancia</summary>
        [HttpPost]
        [Route("api/nadilux-mercancias/{business}/token")]
        public HttpResponseMessage GetTokenOperarioGestionMercancia([FromBody] LoginNadRepartosRequest request, string business)
        {
            var invalido = ValidarLogin(request);
            if (invalido != null)
                return invalido;

            string codigo = Contexto.UsuariosNadilux.AutenticarOperario(request.Username, request.Password.TrimEnd('\n', '\r'));
            if (codigo == null)
                return Respuestas.NoAutorizado();
            return Respuestas.Json(new { token = Contexto.Jwt.GenerarTokenNadilux(request.Username, business), operarioCode = codigo });
        }

        /// <summary>
        /// La validación automática de ASP.NET Core: con Nullable activado en interface.s50c, el cuerpo y sus dos campos
        /// son obligatorios (vacío o solo espacios cuenta como que falta). Null = válido.
        /// </summary>
        private static HttpResponseMessage ValidarLogin(LoginNadRepartosRequest request)
        {
            var errores = new Dictionary<string, string[]>();
            if (request == null)
            {
                errores[""] = new[] { "A non-empty request body is required." };
                errores["request"] = new[] { "The request field is required." };
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Username))
                    errores["Username"] = new[] { "The Username field is required." };
                if (string.IsNullOrWhiteSpace(request.Password))
                    errores["Password"] = new[] { "The Password field is required." };
            }
            return errores.Count == 0 ? null : Respuestas.ErrorValidacion(errores);
        }
    }
}
