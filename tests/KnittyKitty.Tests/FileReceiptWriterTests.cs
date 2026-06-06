using KnittyKitty.Core.Models;
using KnittyKitty.Core.Receipts;

namespace KnittyKitty.Tests;

[TestClass]
public sealed class FileReceiptWriterTests
{
    /// Проверяет, что файл чека содержит русскоязычный текст и основные суммы.
    [TestMethod]
    public async Task WriteAsyncUsesRussianReceiptText()
    {
        var receiptDirectory = Path.Combine(Path.GetTempPath(), "KnittyKittyTests", Guid.NewGuid().ToString("N"));
        var receipt = new Receipt(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new DateTimeOffset(2026, 6, 5, 17, 48, 0, TimeSpan.Zero),
            "Маша",
            new[]
            {
                new ReceiptLine("Котёнок в шарфе", "Игрушки", "розовый", 1m, "шт.", 1290m, 1290m)
            },
            new[]
            {
                new PaymentAllocation(PaymentMethod.DebitCard, 1290m)
            },
            1290m,
            64.5m,
            500m,
            3710m,
            164.5m);

        try
        {
            var path = await new FileReceiptWriter(receiptDirectory).WriteAsync(receipt);
            var text = await File.ReadAllTextAsync(path);

            StringAssert.StartsWith(Path.GetFileName(path), "чек_");
            StringAssert.Contains(text, "Knitty Kitty");
            StringAssert.Contains(text, "Магазин вязанных товаров");
            StringAssert.Contains(text, "Чек:");
            StringAssert.Contains(text, "Покупатель: Маша");
            StringAssert.Contains(text, "Оплата:");
            StringAssert.Contains(text, "Карта: 1290,00");
            StringAssert.Contains(text, "Спасибо, что выбрали Knitty Kitty!");

            foreach (var englishLabel in new[]
            {
                "Plush goods store",
                "Магазин плюшевых товаров",
                "Receipt:",
                "Date:",
                "Customer:",
                "Items:",
                "Total:",
                "Payments:",
                "Cashback:",
                "Balances after purchase:",
                "Debit card:",
                "Thank you"
            })
            {
                Assert.IsFalse(text.Contains(englishLabel, StringComparison.Ordinal), englishLabel);
            }
        }
        finally
        {
            if (Directory.Exists(receiptDirectory))
            {
                Directory.Delete(receiptDirectory, recursive: true);
            }
        }
    }
}
