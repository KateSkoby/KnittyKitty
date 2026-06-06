using KnittyKitty.Core.Exceptions;
using KnittyKitty.Core.Models;
using KnittyKitty.Core.Receipts;
using KnittyKitty.Core.Services;

namespace KnittyKitty.Tests;

[TestClass]
public sealed class CheckoutServiceTests
{
    /// Проверяет, что заказ оплачивается несколькими способами и начисляет кешбэк.
    [TestMethod]
    public async Task CheckoutUsesPartialPaymentsAndAddsCashback()
    {
        var customer = CreateCustomer(cash: 1000m, card: 2000m, bonuses: 250m);
        var cart = new ShoppingCart();
        var toy = new PlushToy("cat", "Cat", "Toys", "Small cat", 1000m, 3m);
        cart.Add(toy, 1m, wasWeighed: false);

        var receipt = await new CheckoutService().CheckoutAsync(
            customer,
            cart,
            new[]
            {
                new PaymentPart(PaymentMethod.Bonus, 200m),
                new PaymentPart(PaymentMethod.DebitCard, 800m)
            },
            new MemoryReceiptWriter());

        Assert.AreEqual(1000m, receipt.Total);
        Assert.AreEqual(40m, receipt.Cashback);
        Assert.AreEqual(1200m, customer.DebitCardBalance);
        Assert.AreEqual(90m, customer.BonusCard.Points);
        Assert.AreEqual(2m, toy.StockAmount);
        Assert.IsTrue(cart.IsEmpty);
    }

    /// Проверяет, что оформление заказа падает при недостаточной сумме оплат.
    [TestMethod]
    public async Task CheckoutThrowsWhenRequestedPaymentsCannotCoverTotal()
    {
        var customer = CreateCustomer(cash: 10m, card: 20m, bonuses: 5m);
        var cart = new ShoppingCart();
        var toy = new PlushToy("bear", "Bear", "Toys", "Gift bear", 100m, 1m);
        cart.Add(toy, 1m, wasWeighed: false);

        await Assert.ThrowsExactlyAsync<InsufficientFundsException>(() =>
            new CheckoutService().CheckoutAsync(
                customer,
                cart,
                new[]
                {
                    new PaymentPart(PaymentMethod.Bonus, 5m),
                    new PaymentPart(PaymentMethod.DebitCard, 20m),
                    new PaymentPart(PaymentMethod.Cash, 10m)
                },
                new MemoryReceiptWriter()));

        Assert.AreEqual(20m, customer.DebitCardBalance);
        Assert.AreEqual(5m, customer.BonusCard.Points);
        Assert.AreEqual(1m, toy.StockAmount);
        Assert.IsFalse(cart.IsEmpty);
    }

    /// Создаёт тестового покупателя с заданными балансами наличных, карты и бонусов.
    private static Customer CreateCustomer(decimal cash, decimal card, decimal bonuses)
    {
        return new Customer("Tester", cash, card, new BonusCard("TEST-1", bonuses), Array.Empty<string>());
    }

    private sealed class MemoryReceiptWriter : IReceiptWriter
    {
        /// Имитирует запись чека и сохраняет переданный чек для проверки тестом.
        public Task<string> WriteAsync(Receipt receipt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"memory://{receipt.Id}");
        }
    }
}
