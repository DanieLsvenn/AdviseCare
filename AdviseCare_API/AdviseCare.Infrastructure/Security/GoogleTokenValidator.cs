using AdviseCare.Application.DTOs;
using AdviseCare.Application.Interfaces.Security;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace AdviseCare.Infrastructure.Security
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly string[] _validAudiences;

        public GoogleTokenValidator(IConfiguration configuration)
        {
            // Read the expected client id(s) from configuration.
            // If you need multiple ids, store as comma/semicolon-separated and split here.
            string clientId = configuration["Authentication:Google:ClientId"]!;
            _validAudiences = new[] { clientId };
        }

        public async Task<GoogleUserInfo> ValidateAsync(string idToken)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = _validAudiences
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUserInfo
            {
                Email = payload.Email,
                Name = payload.Name,
                GoogleId = payload.Subject
            };
        }
    }
}
