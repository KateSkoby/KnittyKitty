namespace KnittyKitty.Core.Models;

public sealed class WeightedPlushMaterial : ProductBase
{
    /// Создаёт экземпляр WeightedPlushMaterial и подготавливает его начальное состояние.
    public WeightedPlushMaterial(
        string id,
        string name,
        string category,
        string description,
        decimal pricePerGram,
        decimal stockGrams,
        string unitName = "g",
        string imagePath = "",
        IEnumerable<string>? availableColors = null)
        : base(id, name, category, description, pricePerGram, stockGrams, unitName, imagePath, availableColors)
    {
    }

    public override bool RequiresWeighing => true;

    /// Рассчитывает стоимость позиции по количеству товара.
    public override decimal CalculatePrice(decimal amount)
    {
        EnsurePositiveAmount(amount);
        return decimal.Round(UnitPrice * amount, 2, MidpointRounding.AwayFromZero);
    }
}
