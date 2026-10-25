namespace AdviseCare.Infrastructure.Configuration;

public sealed class VaultTransitSettings
{
    public string Address { get; set; } = "http://127.0.0.1:8200/";

    public string MountPath { get; set; } = "transit";

    public string KeyNamePrefix { get; set; } = "advisecare-student-attribute-";

    public string? Token { get; set; }
}
