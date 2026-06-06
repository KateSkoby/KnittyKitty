using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Services;

public sealed class CashbackPolicy
{
    /// Создаёт политику кешбэка и проверяет допустимость процента начисления.
    public CashbackPolicy(decimal rate = 0.05m)
    {
        if (rate < 0 || rate > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Cashback rate must be between 0 and 1.");
        }

        Rate = rate;
    }

    public decimal Rate { get; }

    /// Рассчитывает кешбэк по сумме платежей, не оплаченных бонусами.
    public decimal Calculate(IReadOnlyCollection<PaymentAllocation> payments)
    {
        var eligibleAmount = payments
            .Where(payment => payment.Method != PaymentMethod.Bonus)
            .Sum(payment => payment.Amount);

        return decimal.Round(eligibleAmount * Rate, 2, MidpointRounding.AwayFromZero);
    }
}
