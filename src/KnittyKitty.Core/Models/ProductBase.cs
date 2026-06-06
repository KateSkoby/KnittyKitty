using KnittyKitty.Core.Exceptions;

namespace KnittyKitty.Core.Models;

public abstract class ProductBase
{
    /// Создаёт экземпляр ProductBase и подготавливает его начальное состояние.
    protected ProductBase(
        string id,
        string name,
        string category,
        string description,
        decimal unitPrice,
        decimal stockAmount,
        string unitName,
        string imagePath = "",
        IEnumerable<string>? availableColors = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Product id is required.", nameof(id));
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must be positive.");
        }

        if (stockAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stockAmount), "Stock amount cannot be negative.");
        }

        Id = id;
        Name = name;
        Category = category;
        Description = description;
        UnitPrice = unitPrice;
        StockAmount = stockAmount;
        UnitName = unitName;
        ImagePath = imagePath;
        AvailableColors = (availableColors ?? Enumerable.Empty<string>())
            .Select(color => color.Trim())
            .Where(color => !string.IsNullOrWhiteSpace(color))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public string Id { get; }

    public string Name { get; }

    public string Category { get; }

    public string Description { get; }

    public decimal UnitPrice { get; }

    public decimal StockAmount { get; private set; }

    public string UnitName { get; }

    public string ImagePath { get; }

    public IReadOnlyList<string> AvailableColors { get; }

    public abstract bool RequiresWeighing { get; }

    /// Рассчитывает стоимость позиции по количеству товара.
    public abstract decimal CalculatePrice(decimal amount);

    /// Проверяет обязательное условие и сообщает об ошибке при нарушении.
    public virtual void EnsureCanReserve(decimal amount)
    {
        EnsurePositiveAmount(amount);

        if (amount > StockAmount)
        {
            throw new OutOfStockException(Name, amount, StockAmount, UnitName);
        }
    }

    /// Списывает товар со склада после проверки доступного остатка.
    public void Withdraw(decimal amount)
    {
        EnsureCanReserve(amount);
        StockAmount -= amount;
    }

    /// Увеличивает складской остаток товара после проверки количества.
    public virtual void Replenish(decimal amount)
    {
        EnsurePositiveAmount(amount);
        StockAmount += amount;
    }

    /// Форматирует значение для отображения или сохранения.
    public virtual string FormatAmount(decimal amount)
    {
        return $"{amount:0.##} {UnitName}";
    }

    /// Проверяет, что количество товара больше нуля.
    protected static void EnsurePositiveAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }
    }
}
