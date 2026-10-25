namespace AdviseCare.Application.Interfaces.Security;

public interface IKeyManagementService
{
    Task EnsureKeyCanBeDestroyedAsync(string keyManagementKeyId, CancellationToken cancellationToken = default);

    Task DestroyKeyAsync(string keyManagementKeyId, CancellationToken cancellationToken = default);
}
