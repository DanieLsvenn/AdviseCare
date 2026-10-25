namespace AdviseCare.Application.Interfaces.Security;

public interface IKeyManagementService
{
    Task CreateKeyAsync(string keyManagementKeyId, CancellationToken cancellationToken = default);

    Task<string> WrapDataKeyAsync(
        string keyManagementKeyId,
        ReadOnlyMemory<byte> dataKey,
        CancellationToken cancellationToken = default);

    Task<byte[]> UnwrapDataKeyAsync(
        string keyManagementKeyId,
        string wrappedKey,
        CancellationToken cancellationToken = default);

    Task EnsureKeyCanBeDestroyedAsync(string keyManagementKeyId, CancellationToken cancellationToken = default);

    Task DestroyKeyAsync(string keyManagementKeyId, CancellationToken cancellationToken = default);
}
