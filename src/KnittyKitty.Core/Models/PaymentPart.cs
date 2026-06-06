namespace KnittyKitty.Core.Models;

/// Создаёт экземпляр PaymentPart и подготавливает его начальное состояние.
public sealed record PaymentPart(PaymentMethod Method, decimal Amount);
