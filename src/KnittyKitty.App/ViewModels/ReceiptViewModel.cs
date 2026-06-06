using System.Diagnostics;
using System.IO;
using KnittyKitty.Core.Models;

namespace KnittyKitty.App.ViewModels;

public sealed class ReceiptViewModel
{
    /// Создаёт отображаемую модель чека и команду открытия файла.
    public ReceiptViewModel(Receipt receipt)
    {
        Receipt = receipt;
        OpenReceiptCommand = new AsyncRelayCommand(OpenReceiptAsync, () => HasReceiptFile);
    }

    public Receipt Receipt { get; }

    public string Summary => $"{Receipt.PurchasedAt:HH:mm} - {Receipt.Total:0.00} руб., кешбэк {Receipt.Cashback:0.00}";

    public string PaymentsText => string.Join(", ", Receipt.Payments.Select(payment =>
        $"{FormatMethod(payment.Method)} {payment.Amount:0.00}"));

    public string FilePath => Receipt.FilePath ?? string.Empty;

    public bool HasReceiptFile => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);

    public string ReceiptLinkText => HasReceiptFile ? "Открыть чек (.txt)" : "Чек недоступен";

    public AsyncRelayCommand OpenReceiptCommand { get; }

    /// Открывает сохранённый чек средствами текущей платформы.
    private Task OpenReceiptAsync()
    {
        if (!HasReceiptFile)
        {
            return Task.CompletedTask;
        }

        try
        {
            Process.Start(new ProcessStartInfo(FilePath)
            {
                UseShellExecute = true
            });
        }
        catch
        {
        }

        return Task.CompletedTask;
    }

    /// Преобразует способ оплаты в русскоязычную подпись.
    private static string FormatMethod(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Cash => "наличные",
            PaymentMethod.DebitCard => "карта",
            PaymentMethod.Bonus => "бонусы",
            _ => method.ToString()
        };
    }
}
