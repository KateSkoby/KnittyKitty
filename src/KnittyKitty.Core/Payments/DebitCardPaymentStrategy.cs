using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Payments;

public sealed class DebitCardPaymentStrategy : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.DebitCard;

    /// Возвращает доступную сумму для выбранной платёжной стратегии.
    public decimal GetAvailableAmount(Customer customer)
    {
        return customer.DebitCardBalance;
    }

    /// Выполняет списание суммы через выбранную платёжную стратегию.
    public void Pay(Customer customer, decimal amount)
    {
        customer.SpendDebitCard(amount);
    }
}
