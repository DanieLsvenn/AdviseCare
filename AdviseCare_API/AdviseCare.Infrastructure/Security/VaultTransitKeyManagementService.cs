using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace AdviseCare.Infrastructure.Security;

public sealed class VaultTransitKeyManagementService : IKeyManagementService
{
    private readonly HttpClient _httpClient;
    private readonly VaultTransitSettings _settings;
    private readonly ILogger<VaultTransitKeyManagementService> _logger;
    private const int DataKeyLengthBytes = 32;

    public VaultTransitKeyManagementService(
        HttpClient httpClient,
        IOptions<VaultTransitSettings> settings,
        ILogger<VaultTransitKeyManagementService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        if (!Uri.TryCreate(_settings.Address, UriKind.Absolute, out var address) ||
            (address.Scheme != Uri.UriSchemeHttps &&
             !(address.Scheme == Uri.UriSchemeHttp && address.IsLoopback)) ||
            !string.IsNullOrEmpty(address.UserInfo) ||
            !string.IsNullOrEmpty(address.Query) ||
            !string.IsNullOrEmpty(address.Fragment))
        {
            throw new InvalidOperationException(
                "VaultTransit:Address must be an absolute HTTPS URL; HTTP is permitted only for loopback development.");
        }

        if (!IsPathSegment(_settings.MountPath) || !IsKeyName(_settings.KeyNamePrefix))
        {
            throw new InvalidOperationException("Vault Transit mount path and key prefix contain unsupported characters.");
        }

        _httpClient.BaseAddress = new Uri(address.AbsoluteUri.TrimEnd('/') + "/", UriKind.Absolute);
    }

    public async Task CreateKeyAsync(
        string keyManagementKeyId,
        CancellationToken cancellationToken = default)
    {
        using var content = JsonContent.Create(new
        {
            type = "aes256-gcm96",
            deletion_allowed = true
        });
        using var response = await SendAsync(
            HttpMethod.Post,
            GetKeyPath(keyManagementKeyId),
            cancellationToken,
            content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Vault Transit key creation failed with HTTP status {StatusCode}.", response.StatusCode);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<string> WrapDataKeyAsync(
        string keyManagementKeyId,
        ReadOnlyMemory<byte> dataKey,
        CancellationToken cancellationToken = default)
    {
        if (dataKey.Length != DataKeyLengthBytes)
        {
            throw new ArgumentException("The data-encryption key must be 256 bits.", nameof(dataKey));
        }

        using var content = JsonContent.Create(new
        {
            plaintext = Convert.ToBase64String(dataKey.Span)
        });
        using var response = await SendAsync(
            HttpMethod.Post,
            GetTransitPath("encrypt", keyManagementKeyId),
            cancellationToken,
            content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Vault Transit data-key wrapping failed with HTTP status {StatusCode}.", response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        return await ReadDataStringAsync(response, "ciphertext", cancellationToken);
    }

    public async Task<byte[]> UnwrapDataKeyAsync(
        string keyManagementKeyId,
        string wrappedKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(wrappedKey))
        {
            throw new ArgumentException("A wrapped data key is required.", nameof(wrappedKey));
        }

        using var content = JsonContent.Create(new { ciphertext = wrappedKey });
        using var response = await SendAsync(
            HttpMethod.Post,
            GetTransitPath("decrypt", keyManagementKeyId),
            cancellationToken,
            content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Vault Transit data-key unwrapping failed with HTTP status {StatusCode}.", response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        var plaintext = await ReadDataStringAsync(response, "plaintext", cancellationToken);
        byte[] dataKey;
        try
        {
            dataKey = Convert.FromBase64String(plaintext);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("Vault Transit returned a data key that was not valid base64.", exception);
        }

        if (dataKey.Length == DataKeyLengthBytes)
        {
            return dataKey;
        }

        CryptographicOperations.ZeroMemory(dataKey);
        throw new InvalidDataException("Vault Transit returned a data key with an unexpected length.");
    }

    public async Task EnsureKeyCanBeDestroyedAsync(
        string keyManagementKeyId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            GetKeyPath(keyManagementKeyId),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException("The configured Vault Transit key was not found; erasure was not started.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Vault Transit key preflight failed with HTTP status {StatusCode}.", response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("deletion_allowed", out var deletionAllowed) ||
            deletionAllowed.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new InvalidDataException("Vault Transit returned key metadata without a valid deletion_allowed value.");
        }

        if (!deletionAllowed.GetBoolean())
        {
            throw new InvalidOperationException(
                "The Vault Transit key is not configured for deletion; erasure was not started.");
        }
    }

    public async Task DestroyKeyAsync(
        string keyManagementKeyId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            GetKeyPath(keyManagementKeyId),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation(
                "The Vault Transit key is already absent; continuing an idempotent erasure retry.");
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Vault Transit key destruction failed with HTTP status {StatusCode}.",
                response.StatusCode);
            response.EnsureSuccessStatusCode();
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken,
        HttpContent? content = null)
    {
        if (string.IsNullOrWhiteSpace(_settings.Token))
        {
            throw new InvalidOperationException(
                "Vault Transit token is not configured. Set it through the VaultTransit:Token configuration provider.");
        }

        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Vault-Token", _settings.Token);
        request.Content = content;
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private string GetKeyPath(string keyManagementKeyId)
    {
        if (!IsKeyName(keyManagementKeyId) ||
            !keyManagementKeyId.StartsWith(_settings.KeyNamePrefix, StringComparison.Ordinal) ||
            keyManagementKeyId.Length == _settings.KeyNamePrefix.Length)
        {
            throw new InvalidOperationException("The key reference is not a valid AdviseCare Vault Transit key name.");
        }

        return $"v1/{_settings.MountPath}/keys/{Uri.EscapeDataString(keyManagementKeyId)}";
    }

    private string GetTransitPath(string operation, string keyManagementKeyId)
    {
        if (!IsPathSegment(operation))
        {
            throw new ArgumentException("The Vault Transit operation is invalid.", nameof(operation));
        }

        _ = GetKeyPath(keyManagementKeyId);
        return $"v1/{_settings.MountPath}/{operation}/{Uri.EscapeDataString(keyManagementKeyId)}";
    }

    private static async Task<string> ReadDataStringAsync(
        HttpResponseMessage response,
        string propertyName,
        CancellationToken cancellationToken)
    {
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidDataException(
                $"Vault Transit returned a response without a valid {propertyName} value.");
        }

        return value.GetString()!;
    }

    private static bool IsPathSegment(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    private static bool IsKeyName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
