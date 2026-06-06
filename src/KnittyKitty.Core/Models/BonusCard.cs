using KnittyKitty.Core.Exceptions;

namespace KnittyKitty.Core.Models;

public sealed class BonusCard
{
    /// Создаёт бонусную карту и проверяет корректность начального баланса.
    public BonusCard(string number, decimal points)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Bonus card number is required.", nameof(number));
        }

        if (points < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(points), "Bonus points cannot be negative.");
        }

        Number = number;
        Points = points;
    }

    public string Number { get; }

    public decimal Points { get; private set; }

    /// Списывает указанную сумму бонусов после проверки доступного баланса.
    public void Spend(decimal amount)
    {
        EnsurePositive(amount);

        if (amount > Points)
        {
            throw new InsufficientFundsException(amount - Points);
        }

        Points -= amount;
    }

    /// Начисляет положительную сумму кешбэка на бонусную карту.
    public void AddCashback(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Cashback cannot be negative.");
        }

        Points += amount;
    }

    /// Проверяет, что сумма операции строго положительная.
    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }
    }
}
