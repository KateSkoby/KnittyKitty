using KnittyKitty.Core.Models;
using KnittyKitty.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace KnittyKitty.Tests;

[TestClass]
public sealed class SqliteProductRepositoryTests
{
    /// Проверяет сохранение и загрузку товаров через SQLite-репозиторий.
    [TestMethod]
    public async Task RepositorySavesAndLoadsProductsFromSqliteDatabase()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"knitty-kitty-{Guid.NewGuid():N}.db");
        var repository = new SqliteProductRepository(databasePath);
        var products = new ProductBase[]
        {
            new PlushToy(
                "cat",
                "Cat",
                "Toys",
                "Plush cat",
                700m,
                2m,
                "pcs",
                "Assets/Products/cat.png",
                new[] { "Pink", "Milk" }),
            new WeightedPlushMaterial(
                "filler",
                "Filler",
                "Materials",
                "Soft filler",
                1.5m,
                500m,
                "g",
                "Assets/Products/filler.png")
        };

        try
        {
            await repository.SaveAsync(products);

            var loadedProducts = await repository.LoadAsync();

            Assert.HasCount(2, loadedProducts);
            Assert.IsInstanceOfType(loadedProducts[0], typeof(PlushToy));
            Assert.IsInstanceOfType(loadedProducts[1], typeof(WeightedPlushMaterial));
            Assert.AreEqual("cat", loadedProducts[0].Id);
            Assert.AreEqual(700m, loadedProducts[0].UnitPrice);
            Assert.AreEqual("Assets/Products/cat.png", loadedProducts[0].ImagePath);
            CollectionAssert.AreEqual(new[] { "Pink", "Milk" }, loadedProducts[0].AvailableColors.ToArray());
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

    /// Проверяет, что стартовые данные очищают лишние цвета и оставляют варианты только у товаров с цветными картинками.
    [TestMethod]
    public async Task SeedDataRestoresCanonicalProductColors()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"knitty-kitty-{Guid.NewGuid():N}.db");
        var repository = new SqliteProductRepository(databasePath);
        var products = new ProductBase[]
        {
            new PlushToy(
                "bear-honey",
                "Bear",
                "Toys",
                "Bear without color variants",
                900m,
                2m,
                "pcs",
                "Assets/Products/bear-honey.png",
                new[] { "Лайм", "Молочный", "Сиреневый" }),
            new PlushToy(
                "bunny-dream",
                "Bunny",
                "Toys",
                "Bunny with color variants",
                850m,
                3m,
                "pcs",
                "Assets/Products/bunny-dream.png",
                new[] { "Лайм" }),
            new PlushToy(
                "cat-scarf",
                "Cat scarf",
                "Toys",
                "Cat scarf with color variants",
                780m,
                3m,
                "pcs",
                "Assets/Products/cat-scarf.png",
                new[] { "Молочный" }),
            new WeightedPlushMaterial(
                "plush-yarn",
                "Yarn",
                "Materials",
                "Yarn with color variants",
                1.6m,
                500m,
                "g",
                "Assets/Products/plush-yarn.png",
                new[] { "Графит" })
        };

        try
        {
            await repository.SaveAsync(products);

            SqliteSeedData.Ensure(databasePath);

            var loadedProducts = (await repository.LoadAsync())
                .ToDictionary(product => product.Id, StringComparer.OrdinalIgnoreCase);

            CollectionAssert.AreEqual(Array.Empty<string>(), loadedProducts["bear-honey"].AvailableColors.ToArray());
            CollectionAssert.AreEqual(
                new[] { "Розовый", "Зеленый", "Голубой" },
                loadedProducts["bunny-dream"].AvailableColors.ToArray());
            CollectionAssert.AreEqual(
                new[] { "Зеленый", "Голубой" },
                loadedProducts["cat-scarf"].AvailableColors.ToArray());
            CollectionAssert.AreEqual(
                new[] { "Розовый", "Зеленый", "Голубой" },
                loadedProducts["plush-yarn"].AvailableColors.ToArray());
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
}
