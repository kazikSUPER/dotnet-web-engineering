namespace Platform.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string Provider { get; set; } = "identity"; // identity | duende | keycloak
    public string Authority { get; set; } = "http://localhost:5300";
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = "SuperSecretAuthSigningKeyWithAtLeast32BytesForHmacSha256Security!";
    public bool RequireHttpsMetadata { get; set; } = false;
}
