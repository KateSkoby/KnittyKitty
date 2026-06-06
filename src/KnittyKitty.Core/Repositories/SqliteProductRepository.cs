using System.Globalization;
using KnittyKitty.Core.Exceptions;
using KnittyKitty.Core.Models;
using Microsoft.Data.Sqlite;

namespace KnittyKitty.Core.Repositories;

public sealed class SqliteProductRepository : IProductRepository
{
    private readonly string _databasePath;

    /// Создаёт репозиторий товаров и запоминает путь к SQLite-базе.
    public SqliteProductRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    /// Загружает данные из SQLite-базы и преобразует их в доменные модели.
    public async Task<IReadOnlyList<ProductBase>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_databasePath))
        {
            throw new StoreException($"Product database was not found: {_databasePath}");
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await SqliteStoreSchema.EnsureAsync(connection, cancellationToken);

        var records = new List<ProductRecord>();
        var colorsByProductId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT Id, Type, Name, Category, Description, UnitPrice, StockAmount, UnitName, ImagePath
                FROM Products
                ORDER BY SortOrder, Id;
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var record = new ProductRecord
                {
                    Id = ReadString(reader, 0),
                    Type = ReadString(reader, 1),
                    Name = ReadString(reader, 2),
                    Category = ReadString(reader, 3),
                    Description = ReadString(reader, 4),
                    UnitPrice = ReadDecimal(reader, 5, "UnitPrice"),
                    StockAmount = ReadDecimal(reader, 6, "StockAmount"),
                    UnitName = ReadString(reader, 7),
                    ImagePath = ReadString(reader, 8)
                };

                records.Add(record);
                colorsByProductId[record.Id] = new List<string>();
            }
        }

        if (records.Count == 0)
        {
            throw new StoreException("Product database is empty.");
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT ProductId, ColorName
                FROM ProductColors
                ORDER BY ProductId, SortOrder, ColorName;
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var productId = ReadString(reader, 0);
                if (colorsByProductId.TryGetValue(productId, out var colors))
                {
                    colors.Add(ReadString(reader, 1));
                }
            }
        }

        foreach (var record in records)
        {
            record.AvailableColors = colorsByProductId[record.Id].ToArray();
        }

        return records.Select(ProductFactory.Create).ToList();
    }

    /// Сохраняет актуальное состояние коллекции в SQLite-базу.
    public async Task SaveAsync(IEnumerable<ProductBase> products, CancellationToken cancellationToken = default)
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

        await ExecuteAsync(connection, transaction, "DELETE FROM ProductColors;", cancellationToken);
        await ExecuteAsync(connection, transaction, "DELETE FROM Products;", cancellationToken);

        var index = 0;
        foreach (var product in products)
        {
            var record = ProductFactory.ToRecord(product);
            await InsertProductAsync(connection, transaction, record, index, cancellationToken);
            await InsertColorsAsync(connection, transaction, record, cancellationToken);
            index++;
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

    /// Добавляет запись товара внутри активной SQLite-транзакции.
    private static async Task InsertProductAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ProductRecord record,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Products (
                Id, Type, Name, Category, Description, UnitPrice, StockAmount, UnitName, ImagePath, SortOrder
            ) VALUES (
                $id, $type, $name, $category, $description, $unitPrice, $stockAmount, $unitName, $imagePath, $sortOrder
            );
            """;

        command.Parameters.AddWithValue("$id", record.Id);
        command.Parameters.AddWithValue("$type", record.Type);
        command.Parameters.AddWithValue("$name", record.Name);
        command.Parameters.AddWithValue("$category", record.Category);
        command.Parameters.AddWithValue("$description", record.Description);
        command.Parameters.AddWithValue("$unitPrice", FormatDecimal(record.UnitPrice));
        command.Parameters.AddWithValue("$stockAmount", FormatDecimal(record.StockAmount));
        command.Parameters.AddWithValue("$unitName", record.UnitName);
        command.Parameters.AddWithValue("$imagePath", record.ImagePath);
        command.Parameters.AddWithValue("$sortOrder", sortOrder);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// Сохраняет доступные цвета товара внутри активной SQLite-транзакции.
    private static async Task InsertColorsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ProductRecord record,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < record.AvailableColors.Length; index++)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO ProductColors (ProductId, ColorName, SortOrder)
                VALUES ($productId, $colorName, $sortOrder);
                """;
            command.Parameters.AddWithValue("$productId", record.Id);
            command.Parameters.AddWithValue("$colorName", record.AvailableColors[index]);
            command.Parameters.AddWithValue("$sortOrder", index);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
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

        throw new StoreException($"Product database contains an invalid {columnName} value: '{value}'.");
    }

    /// Форматирует decimal для сохранения в SQLite без зависимости от культуры.
    private static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
