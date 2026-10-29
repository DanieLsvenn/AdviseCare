namespace AdviseCare.Infrastructure.Configuration
{
    // Bound from the "Encryption" section of appsettings.json (on MonsterASP: environment variables Encryption__MasterKeys__1, Encryption__CurrentVersion, ...)
    public class EncryptionSettings
    {
        // Master key versions: version number -> 32 random bytes in base64. Never stored in the database; losing a version makes the values wrapped under it unreadable
        public Dictionary<string, string> MasterKeys { get; set; } = new();
        // The version every new or rewrapped attribute key is wrapped with
        public int CurrentVersion { get; set; } = 0;

        // The API refuses to start unless every version is a positive number holding exactly 32 bytes of base64, and the current version exists
        public static bool IsValid(EncryptionSettings settings)
        {
            if (settings.MasterKeys.Count == 0 || !settings.MasterKeys.ContainsKey(settings.CurrentVersion.ToString()))
                return false;

            return settings.MasterKeys.All(k =>
                int.TryParse(k.Key, out var version) && version > 0 &&
                Convert.TryFromBase64String(k.Value ?? string.Empty, new byte[32], out var written) && written == 32);
        }
    }
}
