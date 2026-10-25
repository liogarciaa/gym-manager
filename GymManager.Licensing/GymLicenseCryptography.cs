using System.Security.Cryptography;
using System.Text.Json;

namespace GymManager.Licensing;

public static class GymLicenseCryptography
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Sign(GymLicensePayload payload, RSA privateKey)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(privateKey);
        ValidatePayloadForIssuing(payload);

        var normalizedPayload = payload with
        {
            GymName = payload.GymName.Trim(),
            LicenseId = payload.LicenseId.Trim(),
            InstallationId = payload.InstallationId.Trim(),
            Features = payload.Features
                .Where(feature => !string.IsNullOrWhiteSpace(feature))
                .Select(feature => feature.Trim())
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray()
        };

        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(normalizedPayload, JsonOptions);
        byte[] signatureBytes = privateKey.SignData(
            payloadBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);

        var signedLicense = new SignedGymLicense(
            Convert.ToBase64String(payloadBytes),
            Convert.ToBase64String(signatureBytes));

        return JsonSerializer.Serialize(signedLicense, JsonOptions);
    }

    public static LicenseValidationResult Validate(
        string signedLicenseJson,
        string expectedInstallationId,
        RSA publicKey)
    {
        if (string.IsNullOrWhiteSpace(signedLicenseJson))
            return LicenseValidationResult.Invalid("El archivo de licencia está vacío.");

        if (string.IsNullOrWhiteSpace(expectedInstallationId))
            return LicenseValidationResult.Invalid("No se pudo obtener el identificador de esta PC.");

        try
        {
            var signedLicense = JsonSerializer.Deserialize<SignedGymLicense>(signedLicenseJson, JsonOptions);
            if (signedLicense is null ||
                string.IsNullOrWhiteSpace(signedLicense.Payload) ||
                string.IsNullOrWhiteSpace(signedLicense.Signature))
                return LicenseValidationResult.Invalid("El formato de la licencia no es válido.");

            byte[] payloadBytes = Convert.FromBase64String(signedLicense.Payload);
            byte[] signatureBytes = Convert.FromBase64String(signedLicense.Signature);
            if (!publicKey.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
                return LicenseValidationResult.Invalid("La firma de la licencia no es válida.");

            var payload = JsonSerializer.Deserialize<GymLicensePayload>(payloadBytes, JsonOptions);
            if (payload is null || payload.SchemaVersion != 1 || payload.Features is null)
                return LicenseValidationResult.Invalid("La versión de la licencia no es compatible.");

            if (string.IsNullOrWhiteSpace(payload.LicenseId) || string.IsNullOrWhiteSpace(payload.GymName))
                return LicenseValidationResult.Invalid("La licencia no identifica al gimnasio.");

            if (!string.Equals(payload.InstallationId, expectedInstallationId, StringComparison.Ordinal))
                return LicenseValidationResult.Invalid("La licencia pertenece a otra PC.");

            return new LicenseValidationResult(true, "Licencia válida.", payload);
        }
        catch (Exception exception) when (exception is JsonException or FormatException or CryptographicException or ArgumentException)
        {
            return LicenseValidationResult.Invalid("No se pudo leer o verificar la licencia.");
        }
    }

    private static void ValidatePayloadForIssuing(GymLicensePayload payload)
    {
        if (payload.SchemaVersion != 1)
            throw new ArgumentException("La versión de licencia debe ser 1.", nameof(payload));
        if (string.IsNullOrWhiteSpace(payload.LicenseId))
            throw new ArgumentException("La licencia debe tener un identificador.", nameof(payload));
        if (string.IsNullOrWhiteSpace(payload.GymName))
            throw new ArgumentException("La licencia debe tener el nombre del gimnasio.", nameof(payload));
        if (string.IsNullOrWhiteSpace(payload.InstallationId))
            throw new ArgumentException("La licencia debe estar ligada a una PC.", nameof(payload));
        if (payload.Features is null)
            throw new ArgumentException("La lista de funciones no puede ser nula.", nameof(payload));
    }
}