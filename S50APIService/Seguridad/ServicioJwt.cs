using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace S50APIService.Seguridad
{
    /// <summary>
    /// Tokens JWT idénticos a los de interface.s50c (AuthenticationService.GenerateToken y la validación de JwtBearer):
    /// HS256, emisor y audiencia de interface.s50c, claim unique_name y 24 horas de validez.
    /// </summary>
    public sealed class ServicioJwt
    {
        public const string Emisor = "interface.s50c.Auth.issuer";
        public const string Audiencia = "interface.s50c.Auth.audience";
        private static readonly TimeSpan Validez = TimeSpan.FromHours(24);

        private readonly SymmetricSecurityKey _clave;
        private readonly JwtSecurityTokenHandler _manejador = new JwtSecurityTokenHandler();

        /// <summary>True si la clave se ha generado al arrancar porque no había ninguna configurada.</summary>
        public bool ClaveTemporal { get; }

        /// <param name="clave">
        /// Clave secreta. Con la misma clave que interface.s50c, los tokens de una API valen en la otra (útil durante la migración).
        /// Vacía = se genera una aleatoria en cada arranque.
        /// </param>
        public ServicioJwt(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave))
            {
                var aleatoria = new byte[32];
                using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(aleatoria);
                _clave = new SymmetricSecurityKey(aleatoria);
                ClaveTemporal = true;
            }
            else
            {
                _clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
            }
        }

        public string GenerarToken(string usuario)
        {
            return Generar(new Claim(JwtRegisteredClaimNames.UniqueName, usuario));
        }

        /// <summary>Token de las apps de Nadilux (GenerateTokenNadiluxRepartos): claims "usrname" y "business" en vez de unique_name.</summary>
        public string GenerarTokenNadilux(string usuario, string business)
        {
            return Generar(new Claim("usrname", usuario), new Claim("business", business));
        }

        private string Generar(params Claim[] claims)
        {
            var cabecera = new JwtHeader(new SigningCredentials(_clave, SecurityAlgorithms.HmacSha256));
            var datos = new JwtPayload(
                issuer: Emisor,
                audience: Audiencia,
                claims: claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.Add(Validez));
            return _manejador.WriteToken(new JwtSecurityToken(cabecera, datos));
        }

        /// <summary>
        /// Valida el token con los mismos parámetros que JwtBearer en interface.s50c (por defecto: firma, emisor,
        /// audiencia y caducidad con 5 minutos de margen). Lanza la excepción de validación si no es válido.
        /// </summary>
        public ClaimsPrincipal Validar(string token)
        {
            return _manejador.ValidateToken(token, new TokenValidationParameters
            {
                IssuerSigningKey = _clave,
                ValidIssuer = Emisor,
                ValidAudience = Audiencia,
            }, out _);
        }
    }
}
