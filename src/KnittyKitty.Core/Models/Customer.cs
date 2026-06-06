using KnittyKitty.Core.Exceptions;

namespace KnittyKitty.Core.Models;

public sealed class Customer
{
    /// Создаёт покупателя с денежными балансами и бонусной картой.
    public Customer(
        string name,
        decimal cashBalance,
        decimal debitCardBalance,
        BonusCard bonusCard,
        IEnumerable<string> shoppingList)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        if (cashBalance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cashBalance), "Cash balance cannot be negative.");
        }

        if (debitCardBalance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(debitCardBalance), "Card balance cannot be negative.");
        }

        Name = name;
        CashBalance = cashBalance;
        DebitCardBalance = debitCardBalance;
        BonusCard = bonusCard;
        ShoppingList = shoppingList.ToArray();
    }

    public string Name { get; }

    public decimal CashBalance { get; private set; }

    public decimal DebitCardBalance { get; private set; }

    public BonusCard BonusCard { get; }

    public IReadOnlyList<string> ShoppingList { get; }

    /// Списывает сумму из наличного баланса покупателя.
    public void SpendCash(decimal amount)
    {
        EnsurePayment(CashBalance, amount);
        CashBalance -= amount;
    }

    /// Списывает сумму с баланса дебетовой карты покупателя.
    public void SpendDebitCard(decimal amount)
    {
        EnsurePayment(DebitCardBalance, amount);
        DebitCardBalance -= amount;
    }

    /// Проверяет положительность платежа и достаточность выбранного баланса.
    private static void EnsurePayment(decimal balance, decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        if (amount > balance)
        {
            throw new InsufficientFundsException(amount - balance);
        }
    }
}
