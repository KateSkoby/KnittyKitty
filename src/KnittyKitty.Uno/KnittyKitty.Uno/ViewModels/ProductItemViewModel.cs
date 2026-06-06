using System.Collections.ObjectModel;
using KnittyKitty.Core.Models;

namespace KnittyKitty.Uno.ViewModels;

public sealed class ProductItemViewModel : ObservableObject
{
    private decimal? _weighedAmount;
    private string? _image;
    private string? _selectedColor;

    /// Создаёт отображаемую модель товара и подготавливает выбор цвета.
    public ProductItemViewModel(ProductBase product)
    {
        Product = product;
        Image = CreateImageSource(product.ImagePath);

        foreach (var color in product.AvailableColors)
        {
            ColorOptions.Add(new ProductColorOptionViewModel(color, SelectColor));
        }

        SelectedColor = ColorOptions.FirstOrDefault()?.Name;
    }

    public ProductBase Product { get; }

    public string? Image
    {
        get => _image;
        private set => SetProperty(ref _image, value);
    }

    public string Id => Product.Id;

    public string Name => Product.Name;

    public string Category => Product.Category;

    public string Description => Product.Description;

    public bool RequiresWeighing => Product.RequiresWeighing;

    public IReadOnlyList<string> AvailableColors => Product.AvailableColors;

    public ObservableCollection<ProductColorOptionViewModel> ColorOptions { get; } = new();

    public bool HasColorOptions => AvailableColors.Count > 0;

    public string? SelectedColor
    {
        get => _selectedColor;
        set
        {
            var nextColor = ResolveColor(value);
            if (SetProperty(ref _selectedColor, nextColor))
            {
                RefreshColorSelection();
                RefreshImage();
            }
        }
    }

    public decimal? WeighedAmount
    {
        get => _weighedAmount;
        private set
        {
            if (SetProperty(ref _weighedAmount, value))
            {
                OnPropertyChanged(nameof(WeighingText));
            }
        }
    }

    public string PriceText => Product.RequiresWeighing
        ? $"{Product.UnitPrice:0.00} руб./г"
        : $"{Product.UnitPrice:0.00} руб./шт.";

    public string StockText => $"Остаток: {Product.StockAmount:0.##} {Product.UnitName}";

    public string WeighingText
    {
        get
        {
            if (!Product.RequiresWeighing)
            {
                return "Поштучно";
            }

            return WeighedAmount is null
                ? "Нужно взвесить"
                : $"Взвешено: {WeighedAmount:0.##} {Product.UnitName}";
        }
    }

    /// Отмечает товар как взвешенный и сохраняет измеренное количество.
    public void MarkWeighed(decimal amount)
    {
        WeighedAmount = amount;
    }

    /// Сбрасывает сохранённый вес товара.
    public void ResetWeighing()
    {
        WeighedAmount = null;
    }

    /// Обновляет текст остатка товара после изменения склада.
    public void RefreshStock()
    {
        OnPropertyChanged(nameof(StockText));
    }

    /// Выбирает цвет товара и обновляет состояние вариантов цвета.
    private void SelectColor(ProductColorOptionViewModel option)
    {
        SelectedColor = option.Name;
    }

    /// Подбирает допустимый цвет товара или возвращает первый доступный вариант.
    private string? ResolveColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        return AvailableColors.FirstOrDefault(availableColor =>
            string.Equals(availableColor, color.Trim(), StringComparison.CurrentCultureIgnoreCase));
    }

    /// Синхронизирует визуальное состояние всех вариантов цвета.
    private void RefreshColorSelection()
    {
        foreach (var option in ColorOptions)
        {
            option.SetSelected(string.Equals(option.Name, SelectedColor, StringComparison.CurrentCultureIgnoreCase));
        }
    }

    /// Обновляет изображение товара после выбора другого цветового варианта.
    private void RefreshImage()
    {
        Image = CreateImageSource(ResolveImagePath());
    }

    /// Подбирает путь к изображению товара с учётом выбранного цвета.
    private string ResolveImagePath()
    {
        if (string.IsNullOrWhiteSpace(SelectedColor))
        {
            return Product.ImagePath;
        }

        return Product.Id switch
        {
            "cat-scarf" => ResolveColorImagePath(
                ("Зеленый", Product.ImagePath),
                ("Голубой", "Assets/Products/cat-scarf-blue.png")),
            "bunny-dream" => ResolveColorImagePath(
                ("Розовый", Product.ImagePath),
                ("Зеленый", "Assets/Products/bunny-dream-green.png"),
                ("Голубой", "Assets/Products/bunny-dream-blue.png")),
            "plush-yarn" => ResolveColorImagePath(
                ("Розовый", Product.ImagePath),
                ("Зеленый", "Assets/Products/plush-yarn-green.png"),
                ("Голубой", "Assets/Products/plush-yarn-blue.png")),
            "marshmallow-scarf" => ResolveColorImagePath(
                ("Розовый", Product.ImagePath),
                ("Зеленый", "Assets/Products/marshmallow-scarf-green.png"),
                ("Голубой", "Assets/Products/marshmallow-scarf-blue.png")),
            "ear-hat" => ResolveColorImagePath(
                ("Розовый", Product.ImagePath),
                ("Зеленый", "Assets/Products/ear-hat-green.png"),
                ("Голубой", "Assets/Products/ear-hat-blue.png")),
            _ => Product.ImagePath
        };
    }

    /// Возвращает путь к картинке, которая соответствует выбранному цвету.
    private string ResolveColorImagePath(params (string Color, string ImagePath)[] variants)
    {
        var variant = variants.FirstOrDefault(variant =>
            string.Equals(variant.Color, SelectedColor, StringComparison.CurrentCultureIgnoreCase));

        return string.IsNullOrWhiteSpace(variant.ImagePath)
            ? Product.ImagePath
            : variant.ImagePath;
    }

    /// Создаёт путь к изображению товара в формате, понятном Uno.
    private static string? CreateImageSource(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        return imagePath.Replace('\\', '/').TrimStart('/');
    }
}
