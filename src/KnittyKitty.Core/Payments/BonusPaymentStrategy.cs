using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Payments;

public sealed class BonusPaymentStrategy : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.Bonus;

    /// Возвращает доступную сумму для выбранной платёжной стратегии.
    public decimal GetAvailableAmount(Customer customer)
    {
        return customer.BonusCard.Points;
    }

    /// Выполняет списание суммы через выбранную платёжную стратегию.
    public void Pay(Customer customer, decimal amount)
    {
        customer.BonusCard.Spend(amount);
    }
}
