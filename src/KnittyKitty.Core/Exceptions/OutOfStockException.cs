namespace KnittyKitty.Core.Exceptions;

public sealed class OutOfStockException : StoreException
{
    /// Создаёт исключение о нехватке товара на складе.
    public OutOfStockException(string productName, decimal requested, decimal available, string unitName)
        : base($"Product '{productName}' has only {available:0.##} {unitName}; requested {requested:0.##} {unitName}.")
    {
        ProductName = productName;
        Requested = requested;
        Available = available;
        UnitName = unitName;
    }

    public string ProductName { get; }

    public decimal Requested { get; }

    public decimal Available { get; }

    public string UnitName { get; }
}
