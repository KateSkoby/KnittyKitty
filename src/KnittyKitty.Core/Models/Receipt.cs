namespace KnittyKitty.Core.Models;

/// Создаёт экземпляр Receipt и подготавливает его начальное состояние.
public sealed record Receipt(
    Guid Id,
    DateTimeOffset PurchasedAt,
    string CustomerName,
    IReadOnlyList<ReceiptLine> Lines,
    IReadOnlyList<PaymentAllocation> Payments,
    decimal Total,
    decimal Cashback,
    decimal CashBalanceAfter,
    decimal CardBalanceAfter,
    decimal BonusBalanceAfter,
    string? FilePath = null);
