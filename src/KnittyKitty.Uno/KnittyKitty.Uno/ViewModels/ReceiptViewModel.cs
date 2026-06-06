using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using KnittyKitty.Core.Models;
#if __WASM__
using Uno.Foundation;
#endif

namespace KnittyKitty.Uno.ViewModels;

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
#if __WASM__
            var fileNameLiteral = ToJavaScriptStringLiteral(Path.GetFileName(FilePath));
            var receiptTextLiteral = ToJavaScriptStringLiteral(File.ReadAllText(FilePath));

            WebAssemblyRuntime.InvokeJS($$"""
                (function(fileName, receiptText) {
                    const blob = new Blob([receiptText], { type: 'text/plain;charset=utf-8' });
                    const url = URL.createObjectURL(blob);
                    const opened = window.open(url, '_blank');

                    if (!opened) {
                        const link = document.createElement('a');
                        link.href = url;
                        link.download = fileName;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                    }

                    setTimeout(function() {
                        URL.revokeObjectURL(url);
                    }, 10000);
                })({{fileNameLiteral}}, {{receiptTextLiteral}});
                """);
#else
            Process.Start(new ProcessStartInfo(FilePath)
            {
                UseShellExecute = true
            });
#endif
        }
        catch
        {
        }

        return Task.CompletedTask;
    }

#if __WASM__
    /// Экранирует строку для безопасной передачи в JavaScript.
    private static string ToJavaScriptStringLiteral(string value)
    {
        return $"\"{JavaScriptEncoder.Default.Encode(value)}\"";
    }
#endif

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
