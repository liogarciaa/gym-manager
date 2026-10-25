using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace GymManager.Licensing;

public static class LicenseRuntime
{
    private const string PublicKeyResource = "GymManager.Licensing.TrustedIssuerPublicKey.pem";
    private static readonly object Sync = new();
    private static GymLicensePayload? _current;

    public static string LicensePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GymManager",
        "license.gymlicense");

    public static GymLicensePayload? Current
    {
        get
        {
            lock (Sync) return _current;
        }
    }

    public static bool HasFeature(string feature)
    {
        lock (Sync)
            return _current?.Features.Contains(feature, StringComparer.Ordinal) == true;
    }

    public static LicenseValidationResult LoadCurrent()
    {
        try
        {
            if (!File.Exists(LicensePath))
                return LicenseValidationResult.Invalid("No hay una licencia instalada en esta PC.");

            return Validate(File.ReadAllText(LicensePath));
        }
        catch (IOException)
        {
            return LicenseValidationResult.Invalid("No se pudo leer el archivo de licencia.");
        }
        catch (UnauthorizedAccessException)
        {
            return LicenseValidationResult.Invalid("No hay permiso para leer el archivo de licencia.");
        }
    }

    public static LicenseValidationResult Validate(string licenseJson)
    {
        try
        {
            using var publicKey = RSA.Create();
            publicKey.ImportFromPem(ReadPublicKey());
            return GymLicenseCryptography.Validate(licenseJson, InstallationIdentity.GetCurrent(), publicKey);
        }
        catch (Exception exception) when (exception is CryptographicException or InvalidOperationException or IOException)
        {
            return LicenseValidationResult.Invalid("No se pudo validar la licencia en esta PC.");
        }
    }

    public static LicenseValidationResult Install(string licenseJson)
    {
        var result = Validate(licenseJson);
        if (!result.IsValid || result.License is null) return result;

        try
        {
            string? directory = Path.GetDirectoryName(LicensePath);
            if (string.IsNullOrEmpty(directory))
                return LicenseValidationResult.Invalid("No se pudo determinar dónde guardar la licencia.");

            Directory.CreateDirectory(directory);
            string temporaryPath = LicensePath + ".tmp";
            File.WriteAllText(temporaryPath, licenseJson, Encoding.UTF8);
            File.Move(temporaryPath, LicensePath, overwrite: true);
            SetCurrent(result.License);
            return result;
        }
        catch (IOException)
        {
            return LicenseValidationResult.Invalid("No se pudo guardar la licencia en esta PC.");
        }
        catch (UnauthorizedAccessException)
        {
            return LicenseValidationResult.Invalid("No hay permiso para guardar la licencia.");
        }
    }

    public static void SetCurrent(GymLicensePayload license)
    {
        ArgumentNullException.ThrowIfNull(license);
        lock (Sync) _current = license;
    }

    private static string ReadPublicKey()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PublicKeyResource)
            ?? throw new IOException("No se encontró la clave pública de licencias.");
        using var reader = new StreamReader(stream, Encoding.ASCII);
        string publicKey = reader.ReadToEnd();
        if (!publicKey.Contains("BEGIN PUBLIC KEY", StringComparison.Ordinal))
            throw new IOException("La clave pública todavía no está inicializada.");
        return publicKey;
    }
}