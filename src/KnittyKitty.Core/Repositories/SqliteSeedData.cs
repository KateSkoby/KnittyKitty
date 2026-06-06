using KnittyKitty.Core.Security;
using Microsoft.Data.Sqlite;

namespace KnittyKitty.Core.Repositories;

public static class SqliteSeedData
{
    public const string MashaEmail = "masha@yandex.ru";
    public const string MashaPassword = "masha_123";

    private const string MashaId = "seed-masha";
    private const string MashaName = "Маша";

    private static readonly IReadOnlyList<(string ProductId, string[] Colors)> ProductColors = new[]
    {
        ("cat-scarf", new[] { "Зеленый", "Голубой" }),
        ("bunny-dream", new[] { "Розовый", "Зеленый", "Голубой" }),
        ("ear-hat", new[] { "Розовый", "Зеленый", "Голубой" }),
        ("marshmallow-scarf", new[] { "Розовый", "Зеленый", "Голубой" }),
        ("plush-yarn", new[] { "Розовый", "Зеленый", "Голубой" })
    };

    /// Гарантирует наличие стартового аккаунта и корректных цветовых вариантов каталога.
    public static void Ensure(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = CreateConnection(databasePath);
        connection.Open();

        SqliteStoreSchema.Ensure(connection);
        EnsureMashaUser(connection);
        EnsureProductColors(connection);
    }

    private static SqliteConnection CreateConnection(string databasePath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        return new SqliteConnection(builder.ToString());
    }

    private static void EnsureMashaUser(SqliteConnection connection)
    {
        using (var lookupCommand = connection.CreateCommand())
        {
            lookupCommand.CommandText = "SELECT PasswordHash FROM Users WHERE Email = $email LIMIT 1;";
            lookupCommand.Parameters.AddWithValue("$email", MashaEmail);

            if (lookupCommand.ExecuteScalar() is string passwordHash)
            {
                if (!PasswordHasher.Verify(MashaPassword, passwordHash))
                {
                    using var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = """
                        UPDATE Users
                        SET Name = $name,
                            PasswordHash = $passwordHash
                        WHERE Email = $email;
                        """;
                    updateCommand.Parameters.AddWithValue("$name", MashaName);
                    updateCommand.Parameters.AddWithValue("$email", MashaEmail);
                    updateCommand.Parameters.AddWithValue("$passwordHash", PasswordHasher.Hash(MashaPassword));
                    updateCommand.ExecuteNonQuery();
                }

                return;
            }
        }

        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO Users (Id, Name, Email, PasswordHash, CardBalance, CashBalance, BonusPoints)
            VALUES ($id, $name, $email, $passwordHash, $cardBalance, $cashBalance, $bonusPoints);
            """;
        insertCommand.Parameters.AddWithValue("$id", MashaId);
        insertCommand.Parameters.AddWithValue("$name", MashaName);
        insertCommand.Parameters.AddWithValue("$email", MashaEmail);
        insertCommand.Parameters.AddWithValue("$passwordHash", PasswordHasher.Hash(MashaPassword));
        insertCommand.Parameters.AddWithValue("$cardBalance", "7000");
        insertCommand.Parameters.AddWithValue("$cashBalance", "3000");
        insertCommand.Parameters.AddWithValue("$bonusPoints", "100");
        insertCommand.ExecuteNonQuery();
    }

    private static void EnsureProductColors(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();

        using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM ProductColors;";
            deleteCommand.ExecuteNonQuery();
        }

        foreach (var (productId, colors) in ProductColors)
        {
            if (!ProductExists(connection, transaction, productId))
            {
                continue;
            }

            for (var index = 0; index < colors.Length; index++)
            {
                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = """
                    INSERT INTO ProductColors (ProductId, ColorName, SortOrder)
                    VALUES ($productId, $colorName, $sortOrder);
                    """;
                insertCommand.Parameters.AddWithValue("$productId", productId);
                insertCommand.Parameters.AddWithValue("$colorName", colors[index]);
                insertCommand.Parameters.AddWithValue("$sortOrder", index);
                insertCommand.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    private static bool ProductExists(SqliteConnection connection, SqliteTransaction transaction, string productId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM Products WHERE Id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", productId);
        return command.ExecuteScalar() is not null;
    }
}
