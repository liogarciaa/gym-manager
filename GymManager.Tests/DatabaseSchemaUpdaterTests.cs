using Microsoft.Data.Sqlite;
using Xunit;
using GymManager.Data;

namespace GymManager.Tests;

public sealed class DatabaseSchemaUpdaterTests
{
    [Fact]
    public void ActualizaEsquemaAntiguoSinPerderSociosNiPagosYEsIdempotente()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        Execute(connection, """
            CREATE TABLE "Socios" (
                "Id" INTEGER NOT NULL PRIMARY KEY,
                "Nombre" TEXT NOT NULL,
                "Apellido" TEXT NOT NULL,
                "Dni" TEXT NOT NULL,
                "Telefono" TEXT NOT NULL,
                "FechaAlta" TEXT NOT NULL,
                "EstaActivo" INTEGER NOT NULL,
                "FechaVencimiento" TEXT NULL
            );
            INSERT INTO "Socios" VALUES (42, 'Ana', 'Prueba', '12345', '', '2024-01-02', 1, NULL);
            CREATE TABLE "Pagos" (
                "Id" INTEGER NOT NULL PRIMARY KEY,
                "SocioId" INTEGER NOT NULL,
                "FechaPago" TEXT NOT NULL,
                "Monto" REAL NOT NULL,
                "FormaDePago" TEXT NOT NULL,
                "FechaInicio" TEXT NOT NULL,
                "FechaVencimiento" TEXT NOT NULL,
                "Observaciones" TEXT NULL
            );
            INSERT INTO "Pagos" VALUES (7, 42, '2024-01-02', 1000, 'Efectivo', '2024-01-02', '2024-02-01', NULL);
            """);

        DatabaseSchemaUpdater.EnsureCurrentSchema(connection);
        DatabaseSchemaUpdater.EnsureCurrentSchema(connection);

        Assert.Equal("Ana", Scalar(connection, "SELECT \"Nombre\" FROM \"Socios\" WHERE \"Id\" = 42"));
        Assert.Equal(0L, ScalarInt64(connection, "SELECT \"Eliminado\" FROM \"Socios\" WHERE \"Id\" = 42"));
        Assert.Equal("2024-01-02", Scalar(connection, "SELECT \"FechaRegistro\" FROM \"Socios\" WHERE \"Id\" = 42"));
        Assert.Equal("0", Scalar(connection, "SELECT \"MontoTotal\" FROM \"Pagos\" WHERE \"Id\" = 7"));
        Assert.Equal(1L, ScalarInt64(connection, "SELECT COUNT(*) FROM \"Configuraciones\""));
        Assert.Equal(0L, ScalarInt64(connection, "SELECT COUNT(*) FROM \"Asistencias\""));
        Assert.Equal(0L, ScalarInt64(connection, "SELECT COUNT(*) FROM \"LlaverosMaestros\""));
    }

    [Fact]
    public void CreaEsquemaCompletoCuandoLaBaseEstaVacia()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        DatabaseSchemaUpdater.EnsureCurrentSchema(connection);

        Assert.Equal(1L, ScalarInt64(connection, "SELECT COUNT(*) FROM \"Configuraciones\""));
        Assert.Equal(0L, ScalarInt64(connection, "SELECT COUNT(*) FROM \"Socios\""));
        Assert.Equal(0L, ScalarInt64(connection, "SELECT COUNT(*) FROM \"Pagos\""));
        Assert.Equal(0L, ScalarInt64(connection, "SELECT COUNT(*) FROM \"Asistencias\""));
        Assert.Equal(0L, ScalarInt64(connection, "SELECT COUNT(*) FROM \"LlaverosMaestros\""));

        Execute(connection, "INSERT INTO \"LlaverosMaestros\" (\"Nombre\", \"CodigoAcceso\") VALUES ('Ana', 'UID-1');");
        Assert.Throws<SqliteException>(() => Execute(connection,
            "INSERT INTO \"LlaverosMaestros\" (\"Nombre\", \"CodigoAcceso\") VALUES ('Luis', 'UID-1');"));
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(command.ExecuteScalar())!;
    }

    private static long ScalarInt64(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }
}