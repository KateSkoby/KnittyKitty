namespace KnittyKitty.Core.Exceptions;

public sealed class ProductMustBeWeightedException : StoreException
{
    /// Создаёт исключение для весового товара, который не был взвешен.
    public ProductMustBeWeightedException(string productName)
        : base($"Product '{productName}' must be weighed before adding it to the cart.")
    {
        ProductName = productName;
    }

    public string ProductName { get; }
}
