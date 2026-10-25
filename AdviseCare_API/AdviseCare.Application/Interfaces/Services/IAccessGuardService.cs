using AdviseCare.Application.DTOs;

namespace AdviseCare.Application.Interfaces.Services
{
    // Decides whether staff may read a student's data attributes ("if you consent, we serve") and writes the access log.
    // Not tied to one table, so it has no CRUD methods; Member C calls it before showing or exporting any indicator value.
    public interface IAccessGuardService
    {
        Task<List<AccessDecision>> CheckAccessAsync(Guid AccessorId, Guid StudentId, List<Guid> DataAttributeIds, Guid? ConsentPurposeId);
        Task<List<AccessDecision>> CheckAccessForStudentsAsync(
            Guid AccessorId,
            List<Guid> StudentIds,
            List<Guid> DataAttributeIds,
            Guid? ConsentPurposeId = null);
        Task LogAccessAsync(Guid AccessorId, Guid StudentId, Guid ConsentPurposeId, string AccessedResource);
        Task LogAccessBatchAsync(Guid AccessorId, IReadOnlyCollection<AccessLogBatchEntry> Entries);
    }
}
