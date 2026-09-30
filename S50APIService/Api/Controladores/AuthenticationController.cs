using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;

namespace S50APIService.Api.Controladores
{
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
            var invalido = ValidarLogin(request);
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
        /// son obligatorios: solo falla si faltan (null), porque el [Required] implícito admite textos vacíos.
        /// Los errores van en el mismo orden que allí: sin cuerpo, "" y "request"; con JSON no válido, "request" y la ruta
        /// del error (FormateadorJson la deja en el ModelState como "request.$..."), salvo si es la raíz "$", que va antes.
        /// Null = válido.
        /// </summary>
        private HttpResponseMessage ValidarLogin(LoginNadRepartosRequest request)
        {
            var errores = new Dictionary<string, string[]>();
            if (request == null)
            {
                var errorJson = ModelState.FirstOrDefault(e => e.Value.Errors.Count > 0);
                if (errorJson.Key == null)
                {
                    errores[""] = new[] { "A non-empty request body is required." };
                    errores["request"] = new[] { "The request field is required." };
                }
                else
                {
                    string ruta = errorJson.Key.StartsWith("request.") ? errorJson.Key.Substring("request.".Length) : errorJson.Key;
                    var mensajes = errorJson.Value.Errors.Select(e => e.ErrorMessage).ToArray();
                    bool enRaiz = ruta.IndexOfAny(new[] { '.', '[' }) < 0;
                    if (enRaiz)
                        errores[ruta] = mensajes;
                    errores["request"] = new[] { "The request field is required." };
                    if (!enRaiz)
                        errores[ruta] = mensajes;
                }
            }
            else
            {
                if (request.Password == null)
                    errores["Password"] = new[] { "The Password field is required." };
                if (request.Username == null)
                    errores["Username"] = new[] { "The Username field is required." };
            }
            return errores.Count == 0 ? null : Respuestas.ErrorValidacion(errores);
        }
    }
}
