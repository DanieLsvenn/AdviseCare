using AdviseCare.Application.Interfaces.Storage;
using AdviseCare.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace AdviseCare.Infrastructure.Storage
{
    // Uploads files to a public Supabase Storage bucket through the Storage REST API
    public class SupabaseStorageService : IStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseSettings _settings;

        public SupabaseStorageService(HttpClient httpClient, IOptions<SupabaseSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<string> UploadAvatarAsync(Stream content, string path, string contentType)
        {
            var baseUrl = _settings.Url.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/storage/v1/object/{_settings.AvatarBucket}/{path}");
            request.Headers.Add("apikey", _settings.SecretKey);
            // The legacy service_role key is a JWT and also goes in the Authorization header; new sb_secret_ keys only go in apikey
            if (!_settings.SecretKey.StartsWith("sb_", StringComparison.Ordinal))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SecretKey);
            request.Headers.Add("x-upsert", "true");
            request.Content = new StreamContent(content);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Supabase upload failed ({(int)response.StatusCode}): {error}");
            }

            return $"{baseUrl}/storage/v1/object/public/{_settings.AvatarBucket}/{path}";
        }
    }
}
