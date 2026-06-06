using System.Text;
using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Receipts;

public sealed class FileReceiptWriter : IReceiptWriter
{
    private readonly string _receiptDirectory;

    /// Создаёт файловый writer чеков и задаёт каталог сохранения.
    public FileReceiptWriter(string receiptDirectory)
    {
        _receiptDirectory = receiptDirectory;
    }

    /// Записывает чек в текстовый файл и возвращает путь к созданному файлу.
    public async Task<string> WriteAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_receiptDirectory);

        var fileName = $"чек_{receipt.PurchasedAt:yyyyMMdd_HHmmss}_{receipt.Id:N}.txt";
        var filePath = Path.Combine(_receiptDirectory, fileName);
        await File.WriteAllTextAsync(filePath, BuildReceiptText(receipt), Encoding.UTF8, cancellationToken);

        return filePath;
    }

    /// Формирует полный текст чека с позициями, оплатами и итогами.
    private static string BuildReceiptText(Receipt receipt)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Knitty Kitty");
        builder.AppendLine("Магазин вязанных товаров");
        builder.AppendLine(new string('-', 42));
        builder.AppendLine($"Чек: {receipt.Id}");
        builder.AppendLine($"Дата: {receipt.PurchasedAt:dd.MM.yyyy HH:mm:ss}");
        builder.AppendLine($"Покупатель: {receipt.CustomerName}");
        builder.AppendLine();
        builder.AppendLine("Товары:");

        foreach (var line in receipt.Lines)
        {
            var productName = string.IsNullOrWhiteSpace(line.SelectedColor)
                ? line.ProductName
                : $"{line.ProductName} ({line.SelectedColor})";

            builder.AppendLine(
                $"{productName} | {line.Amount:0.##} {line.UnitName} по {line.UnitPrice:0.00} = {line.Total:0.00}");
        }

        builder.AppendLine();
        builder.AppendLine($"Итого: {receipt.Total:0.00}");
        builder.AppendLine("Оплата:");

        foreach (var payment in receipt.Payments)
        {
            builder.AppendLine($"{FormatPaymentMethod(payment.Method)}: {payment.Amount:0.00}");
        }

        builder.AppendLine($"Кешбэк: {receipt.Cashback:0.00}");
        builder.AppendLine();
        builder.AppendLine("Остатки после покупки:");
        builder.AppendLine($"Наличные: {receipt.CashBalanceAfter:0.00}");
        builder.AppendLine($"Карта: {receipt.CardBalanceAfter:0.00}");
        builder.AppendLine($"Бонусная карта: {receipt.BonusBalanceAfter:0.00}");
        builder.AppendLine(new string('-', 42));
        builder.AppendLine("Спасибо, что выбрали Knitty Kitty!");
        return builder.ToString();
    }

    /// Преобразует способ оплаты в текст для файла чека.
    private static string FormatPaymentMethod(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Cash => "Наличные",
            PaymentMethod.DebitCard => "Карта",
            PaymentMethod.Bonus => "Бонусы",
            _ => "Неизвестный способ"
        };
    }
}
