namespace KnittyKitty.Core.Models;

public sealed class CartItem
{
    /// Создаёт позицию корзины и фиксирует товар, количество и выбранный цвет.
    public CartItem(ProductBase product, decimal amount, string? selectedColor)
    {
        Product = product;
        Amount = amount;
        SelectedColor = selectedColor;
    }

    public ProductBase Product { get; }

    public string? SelectedColor { get; }

    public decimal Amount { get; private set; }

    public decimal Total => Product.CalculatePrice(Amount);

    /// Увеличивает количество товара в строке корзины.
    public void Increase(decimal amount)
    {
        Product.EnsureCanReserve(Amount + amount);
        Amount += amount;
    }
}
