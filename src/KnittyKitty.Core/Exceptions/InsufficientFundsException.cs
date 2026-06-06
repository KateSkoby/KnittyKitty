namespace KnittyKitty.Core.Exceptions;

public sealed class InsufficientFundsException : StoreException
{
    /// Создаёт исключение о недостатке средств и указывает недостающую сумму.
    public InsufficientFundsException(decimal shortage)
        : base($"Not enough money or bonuses. Shortage: {shortage:0.00}.")
    {
        Shortage = shortage;
    }

    public decimal Shortage { get; }
}
