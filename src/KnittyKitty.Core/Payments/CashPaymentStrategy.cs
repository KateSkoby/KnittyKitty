using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Payments;

public sealed class CashPaymentStrategy : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.Cash;

    /// Возвращает доступную сумму для выбранной платёжной стратегии.
    public decimal GetAvailableAmount(Customer customer)
    {
        return customer.CashBalance;
    }

    /// Выполняет списание суммы через выбранную платёжную стратегию.
    public void Pay(Customer customer, decimal amount)
    {
        customer.SpendCash(amount);
    }
}
