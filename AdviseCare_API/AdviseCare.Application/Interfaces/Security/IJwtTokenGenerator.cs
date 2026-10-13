namespace AdviseCare.Application.Interfaces.Security
{
    public interface IJwtTokenGenerator
    {
        // Takes plain values instead of the Account entity because ACCOUNTS is not scaffolded yet.
        // Can be changed to GenerateToken(Account account, IEnumerable<string> roles) after scaffolding.
        string GenerateToken(Guid accountId, string email, string fullName, IEnumerable<string> roles);
    }
}
