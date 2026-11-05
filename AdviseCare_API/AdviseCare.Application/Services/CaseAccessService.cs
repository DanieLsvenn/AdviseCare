using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class CaseAccessService : ICaseAccessService
    {
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Referral> _referralRepository;

        public CaseAccessService(
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Referral> referralRepository)
        {
            _accountRoleRepository = accountRoleRepository;
            _referralRepository = referralRepository;
        }

        public async Task<bool> CanViewCaseAsStaffAsync(Case CaseRecord, Guid CallerId)
        {
            if (CaseRecord.AdvisorId == CallerId)
                return true;
            if (CaseRecord.StudentId == CallerId)
                return false;
            if (await _accountRoleRepository.ExistsAsync(
                    accountRole => accountRole.AccountId == CallerId &&
                                   accountRole.Role.RoleName == "Programme Head"))
                return true;

            return await _referralRepository.ExistsAsync(
                referral => referral.CaseId == CaseRecord.CaseId &&
                            (referral.FromAccountId == CallerId || referral.ToAccountId == CallerId));
        }
    }
}
