using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.IdentityModel.Tokens;
using S50APIService.Seguridad;

namespace S50APIService.Api.Autenticacion
{
    /// <summary>
    /// Equivale a app.UseAuthentication() con JwtBearer en interface.s50c: si la petición trae "Authorization: Bearer ...",
    /// valida el token y deja el usuario en la petición. Si el token no vale, guarda el motivo para que la respuesta 401
    /// lleve la misma cabecera WWW-Authenticate que JwtBearer.
    /// </summary>
    public sealed class ManejadorJwt : DelegatingHandler
    {
        public const string ClaveErrorToken = "S50APIService.ErrorToken";
        private readonly ServicioJwt _jwt;

        public ManejadorJwt(ServicioJwt jwt)
        {
            _jwt = jwt;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage peticion, CancellationToken cancelacion)
        {
            var cabecera = peticion.Headers.Authorization;
            if (cabecera != null && string.Equals(cabecera.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(cabecera.Parameter))
            {
                try
                {
                    peticion.GetRequestContext().Principal = _jwt.Validar(cabecera.Parameter);
                }
                catch (Exception ex)
                {
                    peticion.Properties[ClaveErrorToken] = DescripcionError(ex);
                }
            }
            return base.SendAsync(peticion, cancelacion);
        }

        /// <summary>Mismos textos que JwtBearerHandler.CreateErrorDescription (ASP.NET Core 6). Null = sin descripción.</summary>
        private static string DescripcionError(Exception ex)
        {
            switch (ex)
            {
                case SecurityTokenInvalidAudienceException e:
                    return $"The audience '{e.InvalidAudience ?? "(null)"}' is invalid";
                case SecurityTokenInvalidIssuerException e:
                    return $"The issuer '{e.InvalidIssuer ?? "(null)"}' is invalid";
                case SecurityTokenNoExpirationException _:
                    return "The token has no expiration";
                case SecurityTokenInvalidLifetimeException e:
                    return "The token lifetime is invalid; NotBefore: "
                        + $"'{e.NotBefore?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}'"
                        + $", Expires: '{e.Expires?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}'";
                case SecurityTokenNotYetValidException e:
                    return $"The token is not valid before '{e.NotBefore.ToString(CultureInfo.InvariantCulture)}'";
                case SecurityTokenExpiredException e:
                    return $"The token expired at '{e.Expires.ToString(CultureInfo.InvariantCulture)}'";
                case SecurityTokenSignatureKeyNotFoundException _:
                    return "The signature key was not found";
                case SecurityTokenInvalidSignatureException _:
                    return "The signature is invalid";
                default:
                    return null;
            }
        }
    }
}
