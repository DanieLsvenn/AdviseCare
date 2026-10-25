namespace AdviseCare.Application.Interfaces.Repository;

public interface IErasureWorkflowRepository
{
    Task<(Guid AttributeKeyId, string KeyManagementKeyId)> ExecuteErasureAsync(
        Guid erasureRequestId,
        Guid executedBy,
        CancellationToken cancellationToken = default);

    Task ConfirmKmsKeyDestructionAsync(
        Guid erasureRequestId,
        string keyManagementKeyId,
        Guid executedBy,
        CancellationToken cancellationToken = default);
}
