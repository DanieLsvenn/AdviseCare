using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Interfaces.Security;

public interface IAuthenticatedEnvelopeEncryptionService
{
    Task<ProvisionedEncryptedValue> ProvisionAndEncryptAsync(
        Guid studentId,
        Guid dataAttributeId,
        Guid payloadId,
        string fieldName,
        string plaintext,
        CancellationToken cancellationToken = default);

    Task<EncryptedValue> EncryptAsync(
        Attributekey attributeKey,
        Guid payloadId,
        string fieldName,
        string plaintext,
        CancellationToken cancellationToken = default);

    Task<string> DecryptAsync(
        Attributekey attributeKey,
        Guid payloadId,
        string fieldName,
        ReadOnlyMemory<byte> ciphertext,
        ReadOnlyMemory<byte> expectedHmac,
        CancellationToken cancellationToken = default);
}

public sealed record EncryptedValue(byte[] Ciphertext, byte[] Hmac);

public sealed record ProvisionedEncryptedValue(
    Attributekey AttributeKey,
    byte[] Ciphertext,
    byte[] Hmac);
