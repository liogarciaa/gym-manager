namespace GymManager.Licensing;

public sealed record GymLicensePayload
{
    public int SchemaVersion { get; init; } = 1;
    public string LicenseId { get; init; } = string.Empty;
    public string GymName { get; init; } = string.Empty;
    public string InstallationId { get; init; } = string.Empty;
    public string[] Features { get; init; } = [];
    public DateTimeOffset IssuedUtc { get; init; }
}

public static class LicenseFeatures
{
    public const string NfcAccess = "nfc-access";
    public const string CustomerDisplay = "customer-display";
}

public sealed record SignedGymLicense(string Payload, string Signature);

public sealed record LicenseValidationResult(bool IsValid, string Message, GymLicensePayload? License)
{
    public static LicenseValidationResult Invalid(string message) => new(false, message, null);
}