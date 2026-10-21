using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Application.Services;
using AdviseCare.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AdviseCare.Infrastructure.Security
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _config;

        public JwtTokenGenerator(
           IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(Account account, Guid sessionId, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Sid, account.AccountId.ToString()),
                new("sessionId", sessionId.ToString()),
                new(ClaimTypes.Email, account.Email),
                new(ClaimTypes.Name, account.FullName)
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // The JWT lives as long as its session (1 month); logout ends the session earlier
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(SessionService.LifetimeDays),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}
