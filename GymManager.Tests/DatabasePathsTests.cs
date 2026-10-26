using System.IO;
using Microsoft.Data.Sqlite;
using Xunit;
using GymManager.Data;

namespace GymManager.Tests;

public sealed class DatabasePathsTests
{
    [Fact]
    public void CopiaLaBaseAntiguaSinBorrarElOriginal()
    {
        string directory = CreateTemporaryDirectory();
        string sourcePath = Path.Combine(directory, "legacy", "GymManager.db");
        string targetPath = Path.Combine(directory, "LocalAppData", "GymManager.db");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        CreateDatabaseWithValue(sourcePath, "Socios", "gimnasio antiguo");

        bool copied = DatabasePaths.Initialize(targetPath, [sourcePath]);

        Assert.True(copied);
        Assert.True(File.Exists(sourcePath));
        Assert.Equal("gimnasio antiguo", ReadValue(targetPath, "Socios"));
        Assert.Equal("gimnasio antiguo", ReadValue(sourcePath, "Socios"));
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void NoSobrescribeUnaBaseNuevaConUnaAntigua()
    {
        string directory = CreateTemporaryDirectory();
        string sourcePath = Path.Combine(directory, "legacy.db");
        string targetPath = Path.Combine(directory, "GymManager.db");
        CreateDatabaseWithValue(sourcePath, "Socios", "datos antiguos");
        CreateDatabaseWithValue(targetPath, "Socios", "datos nuevos");

        bool copied = DatabasePaths.Initialize(targetPath, [sourcePath]);

        Assert.False(copied);
        Assert.Equal("datos nuevos", ReadValue(targetPath, "Socios"));
        Directory.Delete(directory, recursive: true);
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "GymManagerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CreateDatabaseWithValue(string path, string table, string value)
    {
        using var connection = new SqliteConnection($"Data Source={path};Pooling=false");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE TABLE {table} (Value TEXT NOT NULL); INSERT INTO {table} (Value) VALUES ($value);";
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private static string ReadValue(string path, string table)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=false");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Value FROM {table}";
        return (string)command.ExecuteScalar()!;
    }
}