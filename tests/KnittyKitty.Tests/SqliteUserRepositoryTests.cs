using KnittyKitty.Core.Models;
using KnittyKitty.Core.Repositories;
using KnittyKitty.Core.Security;
using Microsoft.Data.Sqlite;

namespace KnittyKitty.Tests;

[TestClass]
public sealed class SqliteUserRepositoryTests
{
    /// Проверяет сохранение и загрузку пользователей через SQLite-репозиторий.
    [TestMethod]
    public async Task RepositorySavesAndLoadsUsersFromSqliteDatabase()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"knitty-kitty-users-{Guid.NewGuid():N}.db");
        var repository = new SqliteUserRepository(databasePath);
        var users = new[]
        {
            new UserRecord
            {
                Id = "user-1",
                Name = "Mira",
                Email = "mira@example.com",
                PasswordHash = "sha256:demo",
                CardBalance = 2500m,
                CashBalance = 500m,
                BonusPoints = 125m
            }
        };

        try
        {
            await repository.SaveAsync(users);

            var loadedUsers = await repository.LoadAsync();

            Assert.HasCount(1, loadedUsers);
            Assert.AreEqual("user-1", loadedUsers[0].Id);
            Assert.AreEqual("Mira", loadedUsers[0].Name);
            Assert.AreEqual("mira@example.com", loadedUsers[0].Email);
            Assert.AreEqual("sha256:demo", loadedUsers[0].PasswordHash);
            Assert.AreEqual(2500m, loadedUsers[0].CardBalance);
            Assert.AreEqual(500m, loadedUsers[0].CashBalance);
            Assert.AreEqual(125m, loadedUsers[0].BonusPoints);

            CollectionAssert.AreEqual(
                new[]
                {
                    "Id",
                    "Name",
                    "Email",
                    "PasswordHash",
                    "CardBalance",
                    "CashBalance",
                    "BonusPoints"
                },
                await LoadUserColumnNamesAsync(databasePath));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    /// Проверяет поиск пользователя по email и обновление его балансов.
    [TestMethod]
    public async Task RepositoryFindsUsersByEmailAndUpdatesBalances()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"knitty-kitty-users-{Guid.NewGuid():N}.db");
        var repository = new SqliteUserRepository(databasePath);

        try
        {
            await repository.AddAsync(new UserRecord
            {
                Id = "user-2",
                Name = "Nika",
                Email = "nika@example.com",
                PasswordHash = PasswordHasher.Hash("secret123"),
                CardBalance = 1000m,
                CashBalance = 300m,
                BonusPoints = 25m
            });

            var user = await repository.FindByEmailAsync("NIKA@example.com");

            Assert.IsNotNull(user);
            Assert.IsTrue(PasswordHasher.Verify("secret123", user.PasswordHash));

            user.CardBalance = 750m;
            user.CashBalance = 150m;
            user.BonusPoints = 40m;
            await repository.UpdateAsync(user);

            var updatedUser = await repository.FindByEmailAsync("nika@example.com");

            Assert.IsNotNull(updatedUser);
            Assert.AreEqual(750m, updatedUser.CardBalance);
            Assert.AreEqual(150m, updatedUser.CashBalance);
            Assert.AreEqual(40m, updatedUser.BonusPoints);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    /// Проверяет, что стартовые данные создают аккаунт Маши для входа в сборку приложения.
    [TestMethod]
    public async Task SeedDataCreatesMashaAccount()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"knitty-kitty-users-{Guid.NewGuid():N}.db");
        var repository = new SqliteUserRepository(databasePath);

        try
        {
            SqliteSeedData.Ensure(databasePath);

            var masha = await repository.FindByEmailAsync(SqliteSeedData.MashaEmail);

            Assert.IsNotNull(masha);
            Assert.AreEqual("Маша", masha.Name);
            Assert.IsTrue(PasswordHasher.Verify(SqliteSeedData.MashaPassword, masha.PasswordHash));
            Assert.AreEqual(7000m, masha.CardBalance);
            Assert.AreEqual(3000m, masha.CashBalance);
            Assert.AreEqual(100m, masha.BonusPoints);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    /// Загружает имена столбцов таблицы пользователей из тестовой SQLite-базы.
    private static async Task<List<string>> LoadUserColumnNamesAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(Users);";

        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }
}
