using System.Security.Cryptography;
using GymManager.Licensing;

internal static class Program
{
    private static readonly string PrivateKeyPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GymManager",
        "LicenseIssuer",
        "private-key.pem");

    private static readonly string RepositoryRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static readonly string PublicKeyPath = Path.Combine(
        RepositoryRoot,
        "GymManager.Licensing",
        "TrustedIssuerPublicKey.pem");

    private static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            PrintUsage();
            return 2;
        }

        return args[0] switch
        {
            "init-keys" => InitializeKeys(),
            "issue" => IssueLicense(),
            _ => PrintUsageAndFail()
        };
    }

    private static int InitializeKeys()
    {
        if (File.Exists(PrivateKeyPath))
        {
            Console.Error.WriteLine("Ya existe la clave privada. No se reemplazará para conservar las licencias emitidas.");
            return 1;
        }

        string existingPublicKey = File.Exists(PublicKeyPath) ? File.ReadAllText(PublicKeyPath) : string.Empty;
        if (existingPublicKey.Contains("BEGIN PUBLIC KEY", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Ya existe una clave pública configurada. No se generará otro par automáticamente.");
            return 1;
        }

        string passphrase = ReadSecret("Creá una frase secreta para cifrar la clave privada: ");
        if (passphrase.Length < 12)
        {
            Console.Error.WriteLine("La frase secreta debe tener al menos 12 caracteres.");
            return 1;
        }

        string confirmation = ReadSecret("Repetí la frase secreta: ");
        if (!string.Equals(passphrase, confirmation, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Las frases no coinciden.");
            return 1;
        }

        using var rsa = RSA.Create(3072);
        var parameters = new PbeParameters(
            PbeEncryptionAlgorithm.Aes256Cbc,
            HashAlgorithmName.SHA256,
            210_000);
        string encryptedPrivateKey = rsa.ExportEncryptedPkcs8PrivateKeyPem(passphrase, parameters);
        string publicKey = rsa.ExportSubjectPublicKeyInfoPem();

        Directory.CreateDirectory(Path.GetDirectoryName(PrivateKeyPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(PublicKeyPath)!);
        File.WriteAllText(PrivateKeyPath, encryptedPrivateKey);
        File.WriteAllText(PublicKeyPath, publicKey);

        Console.WriteLine("Par de claves creado.");
        Console.WriteLine($"Clave privada cifrada (no la compartas): {PrivateKeyPath}");
        Console.WriteLine($"Clave pública integrada en la app: {PublicKeyPath}");
        Console.WriteLine("Hacé una copia segura de la clave privada cifrada y guardá la frase secreta por separado.");
        Console.WriteLine("Compilá GymManager en Release antes de emitir o instalar licencias.");
        return 0;
    }

    private static int IssueLicense()
    {
        if (!File.Exists(PrivateKeyPath))
        {
            Console.Error.WriteLine("No existe la clave privada. Ejecutá primero: init-keys");
            return 1;
        }

        string gymName = ReadRequired("Nombre del gimnasio: ");
        string installationId = ReadRequired("Identificador de instalación (64 caracteres hexadecimales): ").Trim();
        if (installationId.Length != 64 || !installationId.All(Uri.IsHexDigit))
        {
            Console.Error.WriteLine("El identificador de instalación no tiene el formato esperado.");
            return 1;
        }

        Console.Write("¿Habilitar NFC? (s/N): ");
        bool enableNfc = string.Equals(Console.ReadLine()?.Trim(), "s", StringComparison.OrdinalIgnoreCase);
        string passphrase = ReadSecret("Frase secreta de la clave emisora: ");

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromEncryptedPem(File.ReadAllText(PrivateKeyPath), passphrase);

            var payload = new GymLicensePayload
            {
                LicenseId = Guid.NewGuid().ToString("N"),
                GymName = gymName,
                InstallationId = installationId.ToUpperInvariant(),
                Features = enableNfc ? [LicenseFeatures.NfcAccess] : [],
                IssuedUtc = DateTimeOffset.UtcNow
            };

            string signedLicense = GymLicenseCryptography.Sign(payload, rsa);
            string outputDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GymManagerLicenses");
            Directory.CreateDirectory(outputDirectory);
            string safeName = string.Concat(gymName.Select(character =>
                Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
            string outputPath = Path.Combine(outputDirectory, $"{safeName}_{payload.LicenseId}.gymlicense");
            File.WriteAllText(outputPath, signedLicense);

            Console.WriteLine("Licencia emitida.");
            Console.WriteLine($"Gimnasio: {payload.GymName}");
            Console.WriteLine($"NFC: {(enableNfc ? "habilitado" : "no habilitado")}");
            Console.WriteLine($"Archivo: {outputPath}");
            return 0;
        }
        catch (CryptographicException)
        {
            Console.Error.WriteLine("No se pudo abrir la clave privada. Revisá la frase secreta.");
            return 1;
        }
    }

    private static string ReadRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string value = Console.ReadLine()?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(value)) return value;
            Console.Error.WriteLine("Este campo es obligatorio.");
        }
    }

    private static string ReadSecret(string prompt)
    {
        Console.Write(prompt);
        var secret = new System.Text.StringBuilder();
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (secret.Length > 0) secret.Length--;
                continue;
            }
            if (!char.IsControl(key.KeyChar)) secret.Append(key.KeyChar);
        }
        Console.WriteLine();
        return secret.ToString();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("GymManager License Issuer");
        Console.WriteLine("  init-keys  Genera una clave emisora (hacerlo una sola vez)");
        Console.WriteLine("  issue      Emite una licencia para una PC");
    }

    private static int PrintUsageAndFail()
    {
        PrintUsage();
        return 2;
    }
}