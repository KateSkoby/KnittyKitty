using Microsoft.Data.Sqlite;

namespace KnittyKitty.Core.Repositories;

internal static class SqliteStoreSchema
{
    private const string SchemaScript = """
        PRAGMA foreign_keys = ON;

        CREATE TABLE IF NOT EXISTS Products (
            Id TEXT NOT NULL PRIMARY KEY,
            Type TEXT NOT NULL CHECK (Type IN ('toy', 'weighted')),
            Name TEXT NOT NULL,
            Category TEXT NOT NULL,
            Description TEXT NOT NULL,
            UnitPrice TEXT NOT NULL,
            StockAmount TEXT NOT NULL,
            UnitName TEXT NOT NULL,
            ImagePath TEXT NOT NULL,
            SortOrder INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ProductColors (
            ProductId TEXT NOT NULL,
            ColorName TEXT NOT NULL,
            SortOrder INTEGER NOT NULL,
            PRIMARY KEY (ProductId, ColorName),
            FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS Users (
            Id TEXT NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL,
            Email TEXT NOT NULL COLLATE NOCASE,
            PasswordHash TEXT NOT NULL,
            CardBalance TEXT NOT NULL,
            CashBalance TEXT NOT NULL,
            BonusPoints TEXT NOT NULL,
            UNIQUE (Email),
            CHECK (length(trim(Id)) > 0),
            CHECK (length(trim(Name)) > 0),
            CHECK (length(trim(Email)) > 0),
            CHECK (length(trim(PasswordHash)) > 0)
        );
        """;

    /// Создаёт необходимые таблицы SQLite, если они ещё отсутствуют.
    public static void Ensure(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = SchemaScript;
        command.ExecuteNonQuery();
    }

    /// Создаёт необходимые таблицы SQLite, если они ещё отсутствуют.
    public static async Task EnsureAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SchemaScript;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
