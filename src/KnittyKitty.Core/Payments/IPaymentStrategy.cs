using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Payments;

public interface IPaymentStrategy
{
    PaymentMethod Method { get; }

    /// Возвращает доступную сумму для выбранной платёжной стратегии.
    decimal GetAvailableAmount(Customer customer);

    /// Выполняет списание суммы через выбранную платёжную стратегию.
    void Pay(Customer customer, decimal amount);
}
