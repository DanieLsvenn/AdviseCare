using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ICaseAccessService
    {
        Task<bool> CanViewCaseAsStaffAsync(Case CaseRecord, Guid CallerId);
    }
}
