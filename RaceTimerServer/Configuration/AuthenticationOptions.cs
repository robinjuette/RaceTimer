namespace RaceTimerServer.Configuration;

public sealed class AuthenticationOptions
{
    public bool Enabled { get; set; }
    public string? ConnectionString { get; set; }
    public string Issuer { get; set; } = "https://localhost:5001/";
    public string BootstrapToken { get; set; } = "";
    public string WebClientId { get; set; } = "racetimer-web";
    public string? WebClientSecret { get; set; }
    public string DatabaseProvider { get; set; } = "PostgreSql";
    public string? SigningCertificatePath { get; set; }
    public string? EncryptionCertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public bool RequireHttps { get; set; } = true;
}

public sealed class PublicAccessOptions
{
    public bool EventDiscovery { get; set; }
    public bool LiveEvents { get; set; }
    public bool FinishedResults { get; set; }
    public bool ParticipantDetails { get; set; }
}
