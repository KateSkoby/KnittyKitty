using KnittyKitty.Core.Exceptions;
using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Services;

public sealed class ShoppingCart
{
    private readonly List<CartItem> _items = new();

    public IReadOnlyList<CartItem> Items => _items;

    public decimal Total => _items.Sum(item => item.Total);

    public bool IsEmpty => _items.Count == 0;

    /// Добавляет товар в корзину после проверки веса, цвета и остатка.
    public void Add(ProductBase product, decimal amount, bool wasWeighed, string? selectedColor = null)
    {
        if (product.RequiresWeighing && !wasWeighed)
        {
            throw new ProductMustBeWeightedException(product.Name);
        }

        var color = ResolveColor(product, selectedColor);
        var alreadyReserved = _items.Where(item => item.Product.Id == product.Id).Sum(item => item.Amount);
        product.EnsureCanReserve(alreadyReserved + amount);

        var existingItem = _items.FirstOrDefault(item =>
            item.Product.Id == product.Id &&
            string.Equals(item.SelectedColor, color, StringComparison.CurrentCultureIgnoreCase));
        if (existingItem is null)
        {
            _items.Add(new CartItem(product, amount, color));
            return;
        }

        existingItem.Increase(amount);
    }

    /// Удаляет указанную позицию из корзины и сообщает, была ли она найдена.
    public bool Remove(CartItem item)
    {
        return _items.Remove(item);
    }

    /// Удаляет все позиции из корзины и сбрасывает итоговую сумму.
    public void Clear()
    {
        _items.Clear();
    }

    /// Подбирает допустимый цвет товара или возвращает первый доступный вариант.
    private static string? ResolveColor(ProductBase product, string? selectedColor)
    {
        if (product.AvailableColors.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(selectedColor))
        {
            throw new InvalidCartOperationException($"Choose a color for '{product.Name}'.");
        }

        var color = product.AvailableColors.FirstOrDefault(color =>
            string.Equals(color, selectedColor.Trim(), StringComparison.CurrentCultureIgnoreCase));

        return color ?? throw new InvalidCartOperationException($"Color '{selectedColor}' is not available for '{product.Name}'.");
    }
}
