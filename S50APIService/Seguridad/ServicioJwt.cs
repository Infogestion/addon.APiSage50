using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace S50APIService.Seguridad
{
    /// <summary>Genera y valida los tokens JWT de la API (HMAC-SHA256).</summary>
    public sealed class ServicioJwt
    {
        private const string Emisor = "S50APIService";
        private readonly SymmetricSecurityKey _clave;
        private readonly TimeSpan _validez;

        /// <summary>True si la clave se ha generado al arrancar porque no había ninguna configurada.</summary>
        public bool ClaveTemporal { get; }

        /// <param name="clave">Clave secreta (mínimo 32 caracteres). Vacía = se genera una aleatoria en cada arranque.</param>
        public ServicioJwt(string clave, TimeSpan validez)
        {
            if (string.IsNullOrWhiteSpace(clave))
            {
                // Sin clave configurada no se usa ninguna fija en el código: los tokens dejan de valer al reiniciar.
                var aleatoria = new byte[32];
                using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(aleatoria);
                _clave = new SymmetricSecurityKey(aleatoria);
                ClaveTemporal = true;
            }
            else
            {
                if (clave.Length < 32)
                    throw new ArgumentException("La clave JWT debe tener al menos 32 caracteres.");
                _clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
            }
            _validez = validez;
        }

        public string GenerarToken(string usuario, out DateTime expira)
        {
            expira = DateTime.UtcNow.Add(_validez);
            var token = new JwtSecurityToken(
                issuer: Emisor,
                audience: Emisor,
                claims: new[]
                {
                    new Claim(ClaimTypes.Name, usuario),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                },
                expires: expira,
                signingCredentials: new SigningCredentials(_clave, SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Devuelve el usuario del token, o null si el token no es válido o ha caducado.</summary>
        public string Validar(string token)
        {
            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _clave,
                    ValidIssuer = Emisor,
                    ValidAudience = Emisor,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                }, out _);
                return principal.Identity?.Name;
            }
            // Solo los errores del token (firma, caducidad, formato...). Cualquier otro fallo, p. ej. que falte
            // una DLL, debe verse como error 500 y no disfrazarse de "token no válido".
            catch (SecurityTokenException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
