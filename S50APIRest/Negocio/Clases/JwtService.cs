
using System;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;


namespace sage.addons.S50APIRest.Negocio.Clases
{
    

    public class JwtService
    {
        private const string SecretKey = "tu_clave_secreta_muy_larga_y_segura_123456";
        private const int ExpirationHours = 24;

        public string GenerarToken(string usuario)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.Name, usuario),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var token = new JwtSecurityToken(
                issuer: "SageApi",
                audience: "SageApi",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(ExpirationHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool ValidarToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
                var handler = new JwtSecurityTokenHandler();

                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = "SageApi",
                    ValidateAudience = true,
                    ValidAudience = "SageApi",
                    ValidateLifetime = true
                }, out _);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
