using System.IO;
using Microsoft.Data.Sqlite;

namespace GymManager.Data;

public static class DatabasePaths
{
    public static string DatabaseDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GymManager");

    public static string DatabasePath
    {
        get
        {
            Directory.CreateDirectory(DatabaseDirectory);
            return Path.Combine(DatabaseDirectory, "GymManager.db");
        }
    }

    public static bool PrepareApplicationDatabase()
    {
        string applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string workingDirectory = Directory.GetCurrentDirectory();
        return Initialize(DatabasePath,
        [
            Path.Combine(applicationDirectory, "GymManager.db"),
            Path.Combine(workingDirectory, "GymManager.db")
        ]);
    }

    public static bool Initialize(string databasePath, IEnumerable<string> legacyPaths)
    {
        string fullDatabasePath = Path.GetFullPath(databasePath);
        string? databaseDirectory = Path.GetDirectoryName(fullDatabasePath);
        if (string.IsNullOrEmpty(databaseDirectory))
            throw new ArgumentException("La ruta de la base de datos no tiene una carpeta válida.", nameof(databasePath));

        Directory.CreateDirectory(databaseDirectory);
        if (File.Exists(fullDatabasePath)) return false;

        foreach (string legacyPath in legacyPaths)
        {
            string fullLegacyPath = Path.GetFullPath(legacyPath);
            if (string.Equals(fullLegacyPath, fullDatabasePath, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(fullLegacyPath))
                continue;

            string temporaryPath = fullDatabasePath + ".migrating";
            try
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);

                using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = fullLegacyPath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString()))
                using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = temporaryPath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Pooling = false
                }.ToString()))
                {
                    source.Open();
                    destination.Open();
                    source.BackupDatabase(destination);
                }

                if (File.Exists(fullDatabasePath)) return false;
                File.Move(temporaryPath, fullDatabasePath);
                return true;
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        return false;
    }
}