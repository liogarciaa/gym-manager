using System.Security.Cryptography;
using System.IO;
using System.Reflection;
using System.Text.Json;
using GymManager.Licensing;
using Xunit;

namespace GymManager.Tests;

public sealed class LicenciasFirmadasTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void LicenciaFirmadaSeValidaParaLaPcAutorizada()
    {
        using var rsa = RSA.Create(3072);
        var payload = CrearLicencia([LicenseFeatures.NfcAccess, LicenseFeatures.CustomerDisplay]);
        string licenseJson = GymLicenseCryptography.Sign(payload, rsa);

        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(rsa.ExportSubjectPublicKeyInfoPem());
        var result = GymLicenseCryptography.Validate(licenseJson, payload.InstallationId, publicKey);

        Assert.True(result.IsValid);
        Assert.Contains(LicenseFeatures.NfcAccess, result.License!.Features);
        Assert.Contains(LicenseFeatures.CustomerDisplay, result.License.Features);
    }

    [Fact]
    public void LicenciaNoSePuedeUsarEnOtraPc()
    {
        using var rsa = RSA.Create(3072);
        var payload = CrearLicencia([]);
        string licenseJson = GymLicenseCryptography.Sign(payload, rsa);

        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(rsa.ExportSubjectPublicKeyInfoPem());
        var result = GymLicenseCryptography.Validate(licenseJson, new string('B', 64), publicKey);

        Assert.False(result.IsValid);
        Assert.Equal("La licencia pertenece a otra PC.", result.Message);
    }

    [Fact]
    public void ModificarElContenidoInvalidaLaFirma()
    {
        using var rsa = RSA.Create(3072);
        var payload = CrearLicencia([LicenseFeatures.NfcAccess]);
        string licenseJson = GymLicenseCryptography.Sign(payload, rsa);
        var signedLicense = JsonSerializer.Deserialize<SignedGymLicense>(licenseJson, JsonOptions)!;
        byte[] payloadBytes = Convert.FromBase64String(signedLicense.Payload);
        payloadBytes[^1] ^= 1;
        string tamperedLicense = JsonSerializer.Serialize(signedLicense with
        {
            Payload = Convert.ToBase64String(payloadBytes)
        }, JsonOptions);

        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(rsa.ExportSubjectPublicKeyInfoPem());
        var result = GymLicenseCryptography.Validate(tamperedLicense, payload.InstallationId, publicKey);

        Assert.False(result.IsValid);
        Assert.Equal("La firma de la licencia no es válida.", result.Message);
    }

    [Fact]
    public void ArchivoIncompletoSeRechazaSinLanzarExcepcion()
    {
        using var publicKey = RSA.Create();

        var result = GymLicenseCryptography.Validate("{}", new string('A', 64), publicKey);

        Assert.False(result.IsValid);
        Assert.Equal("El formato de la licencia no es válido.", result.Message);
    }

    [Fact]
    public void ClavePublicaEmpotradaSePuedeImportar()
    {
        using Stream stream = typeof(GymLicenseCryptography).Assembly
            .GetManifestResourceStream("GymManager.Licensing.TrustedIssuerPublicKey.pem")!;
        using var reader = new StreamReader(stream);
        using var rsa = RSA.Create();

        rsa.ImportFromPem(reader.ReadToEnd());

        Assert.True(rsa.KeySize >= 3072);
    }

    [Fact]
    public void IdentificadorDeInstalacionTieneFormatoEsperadoPorElEmisor()
    {
        string installationId = InstallationIdentity.GetCurrent();

        Assert.Equal(64, installationId.Length);
        Assert.All(installationId, character => Assert.True(Uri.IsHexDigit(character)));
    }

    private static GymLicensePayload CrearLicencia(string[] features) => new()
    {
        LicenseId = Guid.NewGuid().ToString("N"),
        GymName = "Gimnasio de prueba",
        InstallationId = new string('A', 64),
        Features = features,
        IssuedUtc = DateTimeOffset.UtcNow
    };
}