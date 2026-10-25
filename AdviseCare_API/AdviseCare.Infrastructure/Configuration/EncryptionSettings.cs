namespace AdviseCare.Infrastructure.Configuration
{
    // Bound from the "Encryption" section of appsettings.json (on MonsterASP: environment variable Encryption__MasterKey)
    public class EncryptionSettings
    {
        // 32 random bytes in base64; wraps every attribute key. Never stored in the database; losing it makes every encrypted value unreadable
        public string MasterKey { get; set; } = string.Empty;

        // The API refuses to start unless the master key is exactly 32 bytes of valid base64
        public static bool IsValidMasterKey(string masterKey)
        {
            var bytes = new byte[32];
            return Convert.TryFromBase64String(masterKey ?? string.Empty, bytes, out var written) && written == 32;
        }
    }
}
