using KnittyKitty.Core.Exceptions;

namespace KnittyKitty.Core.Models;

public sealed class PlushToy : ProductBase
{
    /// Создаёт экземпляр PlushToy и подготавливает его начальное состояние.
    public PlushToy(
        string id,
        string name,
        string category,
        string description,
        decimal price,
        decimal stockAmount,
        string unitName = "pcs",
        string imagePath = "",
        IEnumerable<string>? availableColors = null)
        : base(id, name, category, description, price, stockAmount, unitName, imagePath, availableColors)
    {
    }

    public override bool RequiresWeighing => false;

    /// Рассчитывает стоимость позиции по количеству товара.
    public override decimal CalculatePrice(decimal amount)
    {
        EnsureWholePieces(amount);
        return decimal.Round(UnitPrice * amount, 2, MidpointRounding.AwayFromZero);
    }

    /// Проверяет обязательное условие и сообщает об ошибке при нарушении.
    public override void EnsureCanReserve(decimal amount)
    {
        EnsureWholePieces(amount);
        base.EnsureCanReserve(amount);
    }

    /// Увеличивает складской остаток товара после проверки количества.
    public override void Replenish(decimal amount)
    {
        EnsureWholePieces(amount);
        base.Replenish(amount);
    }

    /// Проверяет, что штучный товар покупается целым количеством.
    private void EnsureWholePieces(decimal amount)
    {
        EnsurePositiveAmount(amount);

        if (decimal.Truncate(amount) != amount)
        {
            throw new InvalidCartOperationException($"{Name} can be bought only as whole pieces.");
        }
    }
}
