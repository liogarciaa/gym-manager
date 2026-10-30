using System.Data.Common;

namespace GymManager.Data;

public static class DatabaseSchemaUpdater
{
    public static void EnsureCurrentSchema(DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        bool closeConnection = connection.State != System.Data.ConnectionState.Open;
        if (closeConnection) connection.Open();

        try
        {
            using var transaction = connection.BeginTransaction();

            EnsureTable(connection, transaction, "Socios", """
                CREATE TABLE "Socios" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Socios" PRIMARY KEY AUTOINCREMENT,
                    "Nombre" TEXT NOT NULL,
                    "Apellido" TEXT NOT NULL,
                    "Dni" TEXT NOT NULL,
                    "Telefono" TEXT NOT NULL,
                    "FechaAlta" TEXT NOT NULL,
                    "EstaActivo" INTEGER NOT NULL,
                    "Eliminado" INTEGER NOT NULL DEFAULT 0,
                    "FechaVencimiento" TEXT NULL,
                    "FechaRegistro" TEXT NOT NULL,
                    "CodigoAcceso" TEXT NULL
                );
                """);

            EnsureColumn(connection, transaction, "Socios", "Nombre", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(connection, transaction, "Socios", "Apellido", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(connection, transaction, "Socios", "Dni", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(connection, transaction, "Socios", "Telefono", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(connection, transaction, "Socios", "FechaAlta", "TEXT NULL");
            EnsureColumn(connection, transaction, "Socios", "EstaActivo", "INTEGER NOT NULL DEFAULT 1");
            EnsureColumn(connection, transaction, "Socios", "Eliminado", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(connection, transaction, "Socios", "FechaVencimiento", "TEXT NULL");
            EnsureColumn(connection, transaction, "Socios", "FechaRegistro", "TEXT NULL");
            EnsureColumn(connection, transaction, "Socios", "CodigoAcceso", "TEXT NULL");
            Execute(connection, transaction, """
                UPDATE "Socios"
                SET "FechaAlta" = COALESCE("FechaAlta", "FechaRegistro", CURRENT_TIMESTAMP),
                    "FechaRegistro" = COALESCE("FechaRegistro", "FechaAlta", CURRENT_TIMESTAMP);
                """);

            EnsureTable(connection, transaction, "Pagos", """
                CREATE TABLE "Pagos" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Pagos" PRIMARY KEY AUTOINCREMENT,
                    "SocioId" INTEGER NOT NULL,
                    "FechaPago" TEXT NOT NULL,
                    "Monto" REAL NOT NULL,
                    "MontoTotal" TEXT NOT NULL DEFAULT '0',
                    "FormaDePago" TEXT NOT NULL,
                    "FechaInicio" TEXT NOT NULL,
                    "FechaVencimiento" TEXT NOT NULL,
                    "Observaciones" TEXT NULL,
                    CONSTRAINT "FK_Pagos_Socios_SocioId" FOREIGN KEY ("SocioId") REFERENCES "Socios" ("Id") ON DELETE CASCADE
                );
                """);

            EnsureColumn(connection, transaction, "Pagos", "SocioId", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(connection, transaction, "Pagos", "FechaPago", "TEXT NULL");
            EnsureColumn(connection, transaction, "Pagos", "Monto", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(connection, transaction, "Pagos", "MontoTotal", "TEXT NOT NULL DEFAULT '0'");
            EnsureColumn(connection, transaction, "Pagos", "FormaDePago", "TEXT NOT NULL DEFAULT 'Efectivo'");
            EnsureColumn(connection, transaction, "Pagos", "FechaInicio", "TEXT NULL");
            EnsureColumn(connection, transaction, "Pagos", "FechaVencimiento", "TEXT NULL");
            EnsureColumn(connection, transaction, "Pagos", "Observaciones", "TEXT NULL");
            Execute(connection, transaction, """
                UPDATE "Pagos"
                SET "FechaPago" = COALESCE("FechaPago", CURRENT_TIMESTAMP),
                    "FechaInicio" = COALESCE("FechaInicio", "FechaPago", CURRENT_TIMESTAMP),
                    "FechaVencimiento" = COALESCE("FechaVencimiento", "FechaInicio", CURRENT_TIMESTAMP);
                """);

            EnsureTable(connection, transaction, "Asistencias", """
                CREATE TABLE "Asistencias" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Asistencias" PRIMARY KEY AUTOINCREMENT,
                    "SocioId" INTEGER NOT NULL,
                    "FechaHora" TEXT NOT NULL,
                    CONSTRAINT "FK_Asistencias_Socios_SocioId" FOREIGN KEY ("SocioId") REFERENCES "Socios" ("Id") ON DELETE CASCADE
                );
                """);

            EnsureColumn(connection, transaction, "Asistencias", "SocioId", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(connection, transaction, "Asistencias", "FechaHora", "TEXT NULL");
            Execute(connection, transaction, """UPDATE "Asistencias" SET "FechaHora" = CURRENT_TIMESTAMP WHERE "FechaHora" IS NULL;""");

            EnsureTable(connection, transaction, "Configuraciones", """
                CREATE TABLE "Configuraciones" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Configuraciones" PRIMARY KEY AUTOINCREMENT,
                    "NombreGimnasio" TEXT NOT NULL DEFAULT 'Mi Gimnasio',
                    "TelefonoContacto" TEXT NOT NULL DEFAULT '',
                    "Direccion" TEXT NOT NULL DEFAULT '',
                    "PrecioCuotaMensual" TEXT NOT NULL DEFAULT '25000',
                    "DiasToleranciaMora" INTEGER NOT NULL DEFAULT 0,
                    "MensajePieRecibo" TEXT NOT NULL DEFAULT '¡Gracias por entrenar con nosotros!',
                    "ClaveAdmin" TEXT NOT NULL DEFAULT ''
                );
                """);

            EnsureColumn(connection, transaction, "Configuraciones", "NombreGimnasio", "TEXT NOT NULL DEFAULT 'Mi Gimnasio'");
            EnsureColumn(connection, transaction, "Configuraciones", "TelefonoContacto", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(connection, transaction, "Configuraciones", "Direccion", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(connection, transaction, "Configuraciones", "PrecioCuotaMensual", "TEXT NOT NULL DEFAULT '25000'");
            EnsureColumn(connection, transaction, "Configuraciones", "DiasToleranciaMora", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(connection, transaction, "Configuraciones", "MensajePieRecibo", "TEXT NOT NULL DEFAULT '¡Gracias por entrenar con nosotros!'");
            EnsureColumn(connection, transaction, "Configuraciones", "ClaveAdmin", "TEXT NOT NULL DEFAULT ''");
            Execute(connection, transaction, """
                INSERT OR IGNORE INTO "Configuraciones"
                    ("Id", "NombreGimnasio", "TelefonoContacto", "Direccion", "PrecioCuotaMensual", "DiasToleranciaMora", "MensajePieRecibo", "ClaveAdmin")
                VALUES (1, 'Mi Gimnasio', '', '', '25000', 0, '¡Gracias por entrenar con nosotros!', '');
                """);

            EnsureTable(connection, transaction, "LlaverosMaestros", """
                CREATE TABLE "LlaverosMaestros" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_LlaverosMaestros" PRIMARY KEY AUTOINCREMENT,
                    "Nombre" TEXT NOT NULL,
                    "CodigoAcceso" TEXT NOT NULL
                );
                """);
            EnsureColumn(connection, transaction, "LlaverosMaestros", "Nombre", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(connection, transaction, "LlaverosMaestros", "CodigoAcceso", "TEXT NOT NULL DEFAULT ''");

            Execute(connection, transaction, """CREATE INDEX IF NOT EXISTS "IX_Pagos_SocioId" ON "Pagos" ("SocioId");""");
            Execute(connection, transaction, """CREATE INDEX IF NOT EXISTS "IX_Asistencias_SocioId" ON "Asistencias" ("SocioId");""");
            Execute(connection, transaction, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_LlaverosMaestros_CodigoAcceso" ON "LlaverosMaestros" ("CodigoAcceso");""");
            transaction.Commit();
        }
        finally
        {
            if (closeConnection) connection.Close();
        }
    }

    private static void EnsureTable(DbConnection connection, DbTransaction transaction, string table, string createSql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $table);";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$table";
        parameter.Value = table;
        command.Parameters.Add(parameter);
        if (Convert.ToInt64(command.ExecuteScalar()) == 0)
            Execute(connection, transaction, createSql);
    }

    private static void EnsureColumn(DbConnection connection, DbTransaction transaction, string table, string column, string definition)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA table_info(\"{table}\");";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader["name"]?.ToString(), column, StringComparison.OrdinalIgnoreCase))
                return;
        }

        reader.Close();
        Execute(connection, transaction, $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition};");
    }

    private static void Execute(DbConnection connection, DbTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}