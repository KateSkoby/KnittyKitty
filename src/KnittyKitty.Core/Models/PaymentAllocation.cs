namespace KnittyKitty.Core.Models;

/// Создаёт экземпляр PaymentAllocation и подготавливает его начальное состояние.
public sealed record PaymentAllocation(PaymentMethod Method, decimal Amount);
