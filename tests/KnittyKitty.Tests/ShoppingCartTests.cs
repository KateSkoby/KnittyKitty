using KnittyKitty.Core.Exceptions;
using KnittyKitty.Core.Models;
using KnittyKitty.Core.Services;

namespace KnittyKitty.Tests;

[TestClass]
public sealed class ShoppingCartTests
{
    /// Проверяет запрет добавления весового товара без предварительного взвешивания.
    [TestMethod]
    public void AddWeightedProductWithoutWeighingThrows()
    {
        var cart = new ShoppingCart();
        var yarn = new WeightedPlushMaterial("yarn", "Plush yarn", "Materials", "Soft yarn", 2.5m, 1000m);

        Assert.ThrowsExactly<ProductMustBeWeightedException>(() => cart.Add(yarn, 200m, wasWeighed: false));
    }

    /// Проверяет, что добавление игрушки увеличивает итог корзины.
    [TestMethod]
    public void AddToyIncreasesCartTotal()
    {
        var cart = new ShoppingCart();
        var toy = new PlushToy("cat", "Cat", "Toys", "Small cat", 1200m, 3m);

        cart.Add(toy, 2m, wasWeighed: false);

        Assert.AreEqual(2400m, cart.Total);
        Assert.HasCount(1, cart.Items);
    }

    /// Проверяет, что одинаковый товар разных цветов попадает в отдельные строки корзины.
    [TestMethod]
    public void AddSameProductWithDifferentColorsCreatesSeparateCartLines()
    {
        var cart = new ShoppingCart();
        var hat = new PlushToy(
            "hat",
            "Kitty hat",
            "Clothes",
            "Soft hat",
            900m,
            4m,
            availableColors: new[] { "Pink", "Blue" });

        cart.Add(hat, 1m, wasWeighed: false, selectedColor: "Pink");
        cart.Add(hat, 1m, wasWeighed: false, selectedColor: "Blue");

        Assert.HasCount(2, cart.Items);
        Assert.AreEqual("Pink", cart.Items[0].SelectedColor);
        Assert.AreEqual("Blue", cart.Items[1].SelectedColor);
    }
}
