using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Interfaces.Security
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Account account, Guid sessionId, IEnumerable<string> roles);
    }
}
