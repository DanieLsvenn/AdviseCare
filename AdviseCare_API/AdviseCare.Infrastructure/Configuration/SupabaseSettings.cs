namespace AdviseCare.Infrastructure.Configuration
{
    // Bound from the "Supabase" section of appsettings.json
    public class SupabaseSettings
    {
        public string Url { get; set; } = string.Empty;
        // Secret key (sb_secret_...) or the legacy service_role key; server-side only, never sent to the web app
        public string SecretKey { get; set; } = string.Empty;
        public string AvatarBucket { get; set; } = "avatars";
    }
}
