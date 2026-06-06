using System.Collections.ObjectModel;
using KnittyKitty.Core.Models;
using KnittyKitty.Core.Repositories;
using Microsoft.UI.Xaml.Controls;

namespace KnittyKitty.Uno.ViewModels;

public sealed class AdminViewModel : ObservableObject
{
    private const string AllCategories = "Все категории";
    private const string SortDefault = "По умолчанию";
    private const string SortName = "По названию";
    private const string SortPriceAsc = "Цена: по возрастанию";
    private const string SortPriceDesc = "Цена: по убыванию";
    private const string SortStockDesc = "Остаток: больше";
    private const string SortStockAsc = "Остаток: меньше";
    private const string SortWeightedFirst = "Сначала товары на вес";

    private readonly IProductRepository _productRepository;
    private readonly List<ProductBase> _inventory = new();
    private readonly List<ProductItemViewModel> _allProducts = new();
    private ProductItemViewModel? _selectedProduct;
    private string _searchText = string.Empty;
    private string _selectedCategory = AllCategories;
    private string _selectedSortOption = SortDefault;
    private double _restockAmount = 1;
    private string _statusTitle = "Администрирование";
    private string _statusMessage = string.Empty;
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;
    private bool _isInitialized;

    /// Создаёт модель административного экрана и команды управления складом.
    public AdminViewModel(IProductRepository productRepository)
    {
        _productRepository = productRepository;
        ReloadProductsCommand = new AsyncRelayCommand(ReloadProductsAsync);
        RestockCommand = new AsyncRelayCommand(RestockAsync, () => CanRestockSelectedProduct);
        ClearAdminFiltersCommand = new RelayCommand(ClearAdminFilters, () => HasAdminFilters);

        Categories.Add(AllCategories);
        SortOptions.Add(SortDefault);
        SortOptions.Add(SortName);
        SortOptions.Add(SortPriceAsc);
        SortOptions.Add(SortPriceDesc);
        SortOptions.Add(SortStockDesc);
        SortOptions.Add(SortStockAsc);
        SortOptions.Add(SortWeightedFirst);
    }

    public ObservableCollection<ProductItemViewModel> Products { get; } = new();

    public ObservableCollection<string> Categories { get; } = new();

    public ObservableCollection<string> SortOptions { get; } = new();

    public AsyncRelayCommand ReloadProductsCommand { get; }

    public AsyncRelayCommand RestockCommand { get; }

    public RelayCommand ClearAdminFiltersCommand { get; }

    public bool HasAdminFilters =>
        !string.IsNullOrWhiteSpace(SearchText)
        || SelectedCategory != AllCategories
        || SelectedSortOption != SortDefault;

    public string AdminResultText => Products.Count == _allProducts.Count
        ? $"{Products.Count} товаров"
        : $"{Products.Count} из {_allProducts.Count} товаров";

    public ProductItemViewModel? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                RefreshRestockState();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyAdminFilters();
                RefreshFilterState();
            }
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            var nextValue = string.IsNullOrWhiteSpace(value) ? AllCategories : value;
            if (SetProperty(ref _selectedCategory, nextValue))
            {
                ApplyAdminFilters();
                RefreshFilterState();
            }
        }
    }

    public string SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            var nextValue = string.IsNullOrWhiteSpace(value) ? SortDefault : value;
            if (SetProperty(ref _selectedSortOption, nextValue))
            {
                ApplyAdminFilters();
                RefreshFilterState();
            }
        }
    }

    public double RestockAmount
    {
        get => _restockAmount;
        set
        {
            if (SetProperty(ref _restockAmount, CoerceNumericInput(value)))
            {
                RefreshRestockState();
            }
        }
    }

    public bool HasSelectedProduct => SelectedProduct is not null;

    public bool CanRestockSelectedProduct
    {
        get
        {
            if (SelectedProduct is null)
            {
                return false;
            }

            var amount = ToDecimalOrZero(RestockAmount);
            if (amount <= 0)
            {
                return false;
            }

            return SelectedProduct.Product.RequiresWeighing
                || decimal.Truncate(amount) == amount;
        }
    }

    public string RestockHint => CreateRestockHint();

    public bool HasRestockHint => !string.IsNullOrWhiteSpace(RestockHint);

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string StatusTitle
    {
        get => _statusTitle;
        private set => SetProperty(ref _statusTitle, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public InfoBarSeverity StatusSeverity
    {
        get => _statusSeverity;
        private set => SetProperty(ref _statusSeverity, value);
    }

    /// Загружает начальные данные экрана и переводит модель в рабочее состояние.
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await ReloadProductsAsync();
    }

    /// Перечитывает товары из репозитория и обновляет отображаемые списки.
    private async Task ReloadProductsAsync()
    {
        try
        {
            _inventory.Clear();
            _inventory.AddRange(await _productRepository.LoadAsync());

            _allProducts.Clear();
            foreach (var product in _inventory)
            {
                _allProducts.Add(new ProductItemViewModel(product));
            }

            RefreshCategories();
            ApplyAdminFilters();
            SelectedProduct = Products.FirstOrDefault();
            SetStatus("Склад загружен", "Можно пополнять остатки товаров.", InfoBarSeverity.Success);
        }
        catch (Exception exception)
        {
            SetStatus("Склад не загружен", exception.Message, InfoBarSeverity.Error);
        }
    }

    /// Пополняет остаток выбранного товара и сохраняет склад в репозитории.
    private async Task RestockAsync()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        var amount = ToDecimalOrZero(RestockAmount);
        if (!CanRestockSelectedProduct)
        {
            SetStatus("Пополнение не выполнено", RestockHint, InfoBarSeverity.Warning);
            return;
        }

        try
        {
            SelectedProduct.Product.Replenish(amount);
            await _productRepository.SaveAsync(_inventory);
            SelectedProduct.RefreshStock();
            ApplyAdminFilters();
            SetStatus(
                "Остаток пополнен",
                $"{SelectedProduct.Name}: +{amount:0.##} {SelectedProduct.Product.UnitName}.",
                InfoBarSeverity.Success);
            RefreshRestockState();
        }
        catch (Exception exception)
        {
            SetStatus("Пополнение не выполнено", exception.Message, InfoBarSeverity.Error);
        }
    }

    /// Обновляет доступность кнопки пополнения и подсказку формы склада.
    private void RefreshRestockState()
    {
        RestockCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasSelectedProduct));
        OnPropertyChanged(nameof(CanRestockSelectedProduct));
        OnPropertyChanged(nameof(RestockHint));
        OnPropertyChanged(nameof(HasRestockHint));
    }

    /// Обновляет состояние фильтров и доступность кнопки сброса.
    private void RefreshFilterState()
    {
        ClearAdminFiltersCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasAdminFilters));
    }

    /// Применяет поиск, категорию и сортировку к административному списку товаров.
    private void ApplyAdminFilters()
    {
        var previousProductId = SelectedProduct?.Id;
        var query = SearchText.Trim();
        IEnumerable<ProductItemViewModel> filteredProducts = _allProducts;

        if (!string.IsNullOrWhiteSpace(query))
        {
            filteredProducts = filteredProducts.Where(product => MatchesSearch(product, query));
        }

        if (SelectedCategory != AllCategories)
        {
            filteredProducts = filteredProducts.Where(product => product.Category == SelectedCategory);
        }

        filteredProducts = SelectedSortOption switch
        {
            SortName => filteredProducts.OrderBy(product => product.Name),
            SortPriceAsc => filteredProducts.OrderBy(product => product.Product.UnitPrice).ThenBy(product => product.Name),
            SortPriceDesc => filteredProducts.OrderByDescending(product => product.Product.UnitPrice).ThenBy(product => product.Name),
            SortStockDesc => filteredProducts.OrderByDescending(product => product.Product.StockAmount).ThenBy(product => product.Name),
            SortStockAsc => filteredProducts.OrderBy(product => product.Product.StockAmount).ThenBy(product => product.Name),
            SortWeightedFirst => filteredProducts.OrderByDescending(product => product.RequiresWeighing).ThenBy(product => product.Name),
            _ => filteredProducts
        };

        Products.Clear();
        foreach (var product in filteredProducts)
        {
            Products.Add(product);
        }

        SelectedProduct = Products.FirstOrDefault(product => product.Id == previousProductId) ?? Products.FirstOrDefault();
        OnPropertyChanged(nameof(AdminResultText));
    }

    /// Пересобирает список категорий на основе загруженных товаров.
    private void RefreshCategories()
    {
        var currentCategory = SelectedCategory;

        Categories.Clear();
        Categories.Add(AllCategories);

        foreach (var category in _allProducts.Select(product => product.Category).Distinct().OrderBy(category => category))
        {
            Categories.Add(category);
        }

        SelectedCategory = Categories.Contains(currentCategory) ? currentCategory : AllCategories;
    }

    /// Сбрасывает фильтры административного списка товаров.
    private void ClearAdminFilters()
    {
        SearchText = string.Empty;
        SelectedCategory = AllCategories;
        SelectedSortOption = SortDefault;
        ApplyAdminFilters();
    }

    /// Формирует пояснение к форме пополнения выбранного товара.
    private string CreateRestockHint()
    {
        if (SelectedProduct is null)
        {
            return Products.Count == 0 && _allProducts.Count > 0
                ? "По текущим фильтрам товаров нет."
                : "Выберите товар.";
        }

        var amount = ToDecimalOrZero(RestockAmount);
        if (amount <= 0)
        {
            return "Введите количество для пополнения.";
        }

        if (!SelectedProduct.Product.RequiresWeighing && decimal.Truncate(amount) != amount)
        {
            return "Поштучный товар пополняется только целым количеством.";
        }

        return string.Empty;
    }

    /// Записывает текст и тип уведомления для InfoBar.
    private void SetStatus(string title, string message, InfoBarSeverity severity)
    {
        StatusTitle = title;
        StatusMessage = message;
        StatusSeverity = severity;
    }

    /// Проверяет соответствие товара поисковому запросу.
    private static bool MatchesSearch(ProductItemViewModel product, string query)
    {
        return product.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || product.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || product.Category.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    /// Преобразует числовой ввод в decimal и заменяет некорректные значения нулём.
    private static decimal ToDecimalOrZero(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            return 0;
        }

        return decimal.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
    }

    /// Нормализует числовой ввод и не допускает значения ниже минимума.
    private static double CoerceNumericInput(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            return 0;
        }

        return value;
    }
}
