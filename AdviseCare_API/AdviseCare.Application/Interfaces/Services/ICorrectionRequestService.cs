using AdviseCare.Application.Contracts.Requests.CorrectionRequest;
using AdviseCare.Application.Contracts.Responses.CorrectionRequest;

namespace AdviseCare.Application.Interfaces.Services;

public interface ICorrectionRequestService
{
    Task<GetMyCorrectionRequestResponse> GetMineAsync(
        Guid studentId,
        Guid correctionRequestId,
        CancellationToken cancellationToken);

    Task<CreateCorrectionRequestResponse> CreateAsync(
        Guid studentId,
        CreateCorrectionRequest request,
        CancellationToken cancellationToken);

    Task<List<CorrectionRequestReviewResponse>> SearchForAdvisorAsync(
        Guid advisorId,
        string status,
        CancellationToken cancellationToken);

    Task<CorrectionRequestReviewResponse> ApproveAsync(
        Guid advisorId,
        Guid correctionRequestId,
        CorrectionDecisionRequest request,
        CancellationToken cancellationToken);

    Task<CorrectionRequestReviewResponse> RejectAsync(
        Guid advisorId,
        Guid correctionRequestId,
        CorrectionDecisionRequest request,
        CancellationToken cancellationToken);

    Task WithdrawAsync(
        Guid studentId,
        Guid correctionRequestId,
        CancellationToken cancellationToken);
}
