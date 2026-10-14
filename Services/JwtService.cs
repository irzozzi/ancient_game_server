using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AncientServer.Models;
using Microsoft.IdentityModel.Tokens;

namespace AncientServer.Services
{
    public class JwtService
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expireHours;

        public JwtService(IConfiguration configuration)
        {
            _key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
            _issuer = configuration["Jwt:Issuer"] ?? "AncientGameServer";
            _audience = configuration["Jwt:Audience"] ?? "AncientGameClient";
            _expireHours = int.Parse(configuration["Jwt:ExpireHours"] ?? "24");
        }

        public string GenerateToken(Player player)
        {
            var claims = new List<Claim>
            {
                new Claim("playerId", player.Id),
                new Claim(ClaimTypes.Name, player.Username),
                new Claim("level", player.Level.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_expireHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}