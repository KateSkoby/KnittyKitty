namespace KnittyKitty.Core.Models;

/// Создаёт экземпляр ReceiptLine и подготавливает его начальное состояние.
public sealed record ReceiptLine(
    string ProductName,
    string Category,
    string? SelectedColor,
    decimal Amount,
    string UnitName,
    decimal UnitPrice,
    decimal Total);
