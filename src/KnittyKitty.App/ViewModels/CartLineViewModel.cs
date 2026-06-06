using KnittyKitty.Core.Models;

namespace KnittyKitty.App.ViewModels;

public sealed class CartLineViewModel
{
    /// Создаёт строку корзины на основе доменного элемента покупки.
    public CartLineViewModel(CartItem item)
    {
        Item = item;
    }

    public CartItem Item { get; }

    public string ProductId => Item.Product.Id;

    public string Name => Item.Product.Name;

    public string ColorText => string.IsNullOrWhiteSpace(Item.SelectedColor)
        ? string.Empty
        : $"Цвет: {Item.SelectedColor}";

    public bool HasColor => !string.IsNullOrWhiteSpace(Item.SelectedColor);

    public string AmountText => $"{Item.Amount:0.##} {Item.Product.UnitName}";

    public string TotalText => $"{Item.Total:0.00} руб.";
}
