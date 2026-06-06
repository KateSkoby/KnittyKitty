using System.Globalization;
using KnittyKitty.Core.Exceptions;
using KnittyKitty.Core.Models;
using Microsoft.Data.Sqlite;

namespace KnittyKitty.Core.Repositories;

public sealed class SqliteUserRepository : IUserRepository
{
    private readonly string _databasePath;

    /// Создаёт репозиторий пользователей и запоминает путь к SQLite-базе.
    public SqliteUserRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    /// Загружает данные из SQLite-базы и преобразует их в доменные модели.
    public async Task<IReadOnlyList<UserRecord>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_databasePath))
        {
            throw new StoreException($"User database was not found: {_databasePath}");
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await SqliteStoreSchema.EnsureAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Email, PasswordHash, CardBalance, CashBalance, BonusPoints
            FROM Users
            ORDER BY Name, Id;
            """;

        var users = new List<UserRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new UserRecord
            {
                Id = ReadString(reader, 0),
                Name = ReadString(reader, 1),
                Email = ReadString(reader, 2),
                PasswordHash = ReadString(reader, 3),
                CardBalance = ReadDecimal(reader, 4, "CardBalance"),
                CashBalance = ReadDecimal(reader, 5, "CashBalance"),
                BonusPoints = ReadDecimal(reader, 6, "BonusPoints")
            });
        }

        return users;
    }

    /// Находит пользователя в SQLite-базе по email без учёта регистра.
    public async Task<UserRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await SqliteStoreSchema.EnsureAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Email, PasswordHash, CardBalance, CashBalance, BonusPoints
            FROM Users
            WHERE Email = $email
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$email", email.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadUser(reader) : null;
    }

    /// Добавляет нового пользователя в SQLite-базу.
    public async Task AddAsync(UserRecord user, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await SqliteStoreSchema.EnsureAsync(connection, cancellationToken);
        await InsertUserAsync(connection, null, user, cancellationToken);
    }

    /// Обновляет данные существующего пользователя в SQLite-базе.
    public async Task UpdateAsync(UserRecord user, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await SqliteStoreSchema.EnsureAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET Name = $name,
                Email = $email,
                PasswordHash = $passwordHash,
                CardBalance = $cardBalance,
                CashBalance = $cashBalance,
                BonusPoints = $bonusPoints
            WHERE Id = $id;
            """;

        AddUserParameters(command, user);
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rowsAffected == 0)
        {
            throw new StoreException($"User was not found: {user.Id}");
        }
    }

    /// Сохраняет актуальное состояние коллекции в SQLite-базу.
    public async Task SaveAsync(IEnumerable<UserRecord> users, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await SqliteStoreSchema.EnsureAsync(connection, cancellationToken);

        await using var transaction = connection.BeginTransaction();

        await ExecuteAsync(connection, transaction, "DELETE FROM Users;", cancellationToken);

        foreach (var user in users)
        {
            await InsertUserAsync(connection, transaction, user, cancellationToken);
        }

        transaction.Commit();
    }

    /// Создаёт SQLite-подключение к рабочей базе данных.
    private SqliteConnection CreateConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        return new SqliteConnection(builder.ToString());
    }

    /// Выполняет SQL-команду в SQLite-подключении с поддержкой отмены.
    private static async Task ExecuteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// Добавляет запись пользователя внутри активной SQLite-транзакции.
    private static async Task InsertUserAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        UserRecord user,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Users (
                Id, Name, Email, PasswordHash, CardBalance, CashBalance, BonusPoints
            ) VALUES (
                $id, $name, $email, $passwordHash, $cardBalance, $cashBalance, $bonusPoints
            );
            """;

        AddUserParameters(command, user);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// Изменяет данные и сохраняет актуальное состояние.
    private static void AddUserParameters(SqliteCommand command, UserRecord user)
    {
        command.Parameters.AddWithValue("$id", user.Id);
        command.Parameters.AddWithValue("$name", user.Name);
        command.Parameters.AddWithValue("$email", user.Email);
        command.Parameters.AddWithValue("$passwordHash", user.PasswordHash);
        command.Parameters.AddWithValue("$cardBalance", FormatDecimal(user.CardBalance));
        command.Parameters.AddWithValue("$cashBalance", FormatDecimal(user.CashBalance));
        command.Parameters.AddWithValue("$bonusPoints", FormatDecimal(user.BonusPoints));
    }

    /// Считывает строковое значение из SQLite-reader с защитой от NULL.
    private static string ReadString(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    /// Считывает decimal из SQLite-reader и сообщает об ошибке некорректного значения.
    private static decimal ReadDecimal(SqliteDataReader reader, int ordinal, string columnName)
    {
        var value = ReadString(reader, ordinal);
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        throw new StoreException($"User database contains an invalid {columnName} value: '{value}'.");
    }

    /// Форматирует decimal для сохранения в SQLite без зависимости от культуры.
    private static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// Считывает значение из источника данных.
    private static UserRecord ReadUser(SqliteDataReader reader)
    {
        return new UserRecord
        {
            Id = ReadString(reader, 0),
            Name = ReadString(reader, 1),
            Email = ReadString(reader, 2),
            PasswordHash = ReadString(reader, 3),
            CardBalance = ReadDecimal(reader, 4, "CardBalance"),
            CashBalance = ReadDecimal(reader, 5, "CashBalance"),
            BonusPoints = ReadDecimal(reader, 6, "BonusPoints")
        };
    }
}
