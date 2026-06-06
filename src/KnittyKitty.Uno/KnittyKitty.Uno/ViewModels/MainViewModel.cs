using System.Collections.ObjectModel;
using KnittyKitty.Core.Exceptions;
using KnittyKitty.Core.Models;
using KnittyKitty.Core.Receipts;
using KnittyKitty.Core.Repositories;
using KnittyKitty.Core.Services;
using Microsoft.UI.Xaml.Controls;

using KnittyKitty.Uno.Storage;

namespace KnittyKitty.Uno.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private const string AllCategories = "Все категории";
    private const string SortDefault = "По умолчанию";
    private const string SortName = "По названию";
    private const string SortPriceAsc = "Цена: по возрастанию";
    private const string SortPriceDesc = "Цена: по убыванию";
    private const string SortStockDesc = "Остаток: больше";
    private const string SortStockAsc = "Остаток: меньше";
    private const string SortWeightedFirst = "Сначала товары на вес";

    private readonly ShoppingCart _cart = new();
    private readonly CheckoutService _checkoutService = new();
    private readonly Customer _customer;
    private readonly UserRecord _user;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly FileReceiptWriter _receiptWriter;
    private readonly List<ProductBase> _inventory = new();
    private readonly List<ProductItemViewModel> _allProducts = new();
    private ProductItemViewModel? _selectedProduct;
    private CartLineViewModel? _selectedCartLine;
    private string _searchText = string.Empty;
    private string _selectedCategory = AllCategories;
    private string _selectedSortOption = SortDefault;
    private double _quantityToAdd = 1;
    private double _weightToAddGrams = 250;
    private double _cashPayment;
    private double _cardPayment;
    private double _bonusPayment;
    private string _statusTitle = "Готово";
    private string _statusMessage = string.Empty;
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;
    private bool _isInitialized;

    /// Создаёт модель витрины магазина для выбранного пользователя.
    public MainViewModel(UserRecord user, IUserRepository userRepository)
    {
        _user = user;
        _userRepository = userRepository;

        var dataPath = ApplicationStorage.DatabasePath;
        var receiptsPath = ApplicationStorage.ReceiptsDirectory;

        _productRepository = new SqliteProductRepository(dataPath);
        _receiptWriter = new FileReceiptWriter(receiptsPath);
        _customer = new Customer(
            user.Name,
            user.CashBalance,
            user.CardBalance,
            new BonusCard($"KK-{user.Id}", user.BonusPoints),
            new[]
            {
                "Котёнок в шарфе",
                "Плюшевая шапка с ушками",
                "Плюшевая сумка-тоут Kitty",
                "Плюшевая пряжа"
            });

        AddToCartCommand = new RelayCommand(AddSelectedProductToCart, () => CanAddSelectedProduct);
        WeighCommand = new RelayCommand(WeighSelectedProduct, () => CanWeighSelectedProduct);
        RemoveFromCartCommand = new RelayCommand(RemoveSelectedCartLine, () => HasSelectedCartLine);
        ClearCartCommand = new RelayCommand(ClearCart, () => HasCartItems);
        FillCardPaymentCommand = new RelayCommand(FillCardPayment, () => HasCartItems);
        FillMixedPaymentCommand = new RelayCommand(FillMixedPayment, () => HasCartItems);
        CheckoutCommand = new AsyncRelayCommand(CheckoutAsync, () => HasCartItems);
        ReloadProductsCommand = new AsyncRelayCommand(ReloadProductsAsync);
        ClearCatalogFiltersCommand = new RelayCommand(ClearCatalogFilters, () => HasCatalogFilters);

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

    public ObservableCollection<CartLineViewModel> CartLines { get; } = new();

    public ObservableCollection<ReceiptViewModel> PurchaseHistory { get; } = new();

    public RelayCommand AddToCartCommand { get; }

    public RelayCommand WeighCommand { get; }

    public RelayCommand RemoveFromCartCommand { get; }

    public RelayCommand ClearCartCommand { get; }

    public RelayCommand FillCardPaymentCommand { get; }

    public RelayCommand FillMixedPaymentCommand { get; }

    public AsyncRelayCommand CheckoutCommand { get; }

    public AsyncRelayCommand ReloadProductsCommand { get; }

    public RelayCommand ClearCatalogFiltersCommand { get; }

    public bool HasCartItems => !_cart.IsEmpty;

    public bool HasSelectedCartLine => SelectedCartLine is not null;

    public bool HasCatalogFilters =>
        !string.IsNullOrWhiteSpace(SearchText)
        || SelectedCategory != AllCategories
        || SelectedSortOption != SortDefault;

    public bool CanEditQuantity => SelectedProduct is { RequiresWeighing: false } product
        && GetRemainingStock(product.Product) > 0;

    public bool CanEditWeight => SelectedProduct is { RequiresWeighing: true } product
        && GetRemainingStock(product.Product) > 0;

    public bool CanWeighSelectedProduct
    {
        get
        {
            if (SelectedProduct is not { RequiresWeighing: true } product)
            {
                return false;
            }

            var amount = ToDecimalOrZero(WeightToAddGrams);
            return amount > 0 && amount <= GetRemainingStock(product.Product);
        }
    }

    public bool CanAddSelectedProduct
    {
        get
        {
            if (SelectedProduct is null)
            {
                return false;
            }

            var remainingStock = GetRemainingStock(SelectedProduct.Product);
            if (remainingStock <= 0)
            {
                return false;
            }

            if (SelectedProduct.RequiresWeighing)
            {
                return SelectedProduct.WeighedAmount is { } weighedAmount
                    && weighedAmount > 0
                    && weighedAmount <= remainingStock;
            }

            var amount = ToDecimalOrZero(QuantityToAdd);
            return amount > 0
                && decimal.Truncate(amount) == amount
                && amount <= remainingStock;
        }
    }

    public string ProductActionHint => CreateProductActionHint();

    public bool HasProductActionHint => !string.IsNullOrWhiteSpace(ProductActionHint);

    public string CartActionHint
    {
        get
        {
            if (!HasCartItems)
            {
                return "Корзина пуста";
            }

            return HasSelectedCartLine ? string.Empty : "Позиция в корзине не выбрана";
        }
    }

    public bool HasCartActionHint => !string.IsNullOrWhiteSpace(CartActionHint);

    public string PaymentActionHint => HasCartItems ? string.Empty : "Оплата недоступна, пока корзина пуста";

    public bool HasPaymentActionHint => !string.IsNullOrWhiteSpace(PaymentActionHint);

    public string UserNameText => _customer.Name;

    public string UserEmailText => _user.Email;

    public string GreetingText => $"{CreateGreeting(DateTime.Now.Hour)}, {_customer.Name}!";

    public ProductItemViewModel? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                RefreshSelectedProductActionState();
            }
        }
    }

    public CartLineViewModel? SelectedCartLine
    {
        get => _selectedCartLine;
        set
        {
            if (SetProperty(ref _selectedCartLine, value))
            {
                RemoveFromCartCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(HasSelectedCartLine));
                RefreshCartActionHint();
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
                ApplyCatalogFilters();
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
                ApplyCatalogFilters();
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
                ApplyCatalogFilters();
                RefreshFilterState();
            }
        }
    }

    public double QuantityToAdd
    {
        get => _quantityToAdd;
        set
        {
            if (SetProperty(ref _quantityToAdd, CoerceNumericInput(value, 1)))
            {
                RefreshSelectedProductActionState();
            }
        }
    }

    public double WeightToAddGrams
    {
        get => _weightToAddGrams;
        set
        {
            if (SetProperty(ref _weightToAddGrams, CoerceNumericInput(value, 1)))
            {
                RefreshSelectedProductActionState();
            }
        }
    }

    public double CashPayment
    {
        get => _cashPayment;
        set => SetProperty(ref _cashPayment, CoerceNumericInput(value, 0));
    }

    public double CardPayment
    {
        get => _cardPayment;
        set => SetProperty(ref _cardPayment, CoerceNumericInput(value, 0));
    }

    public double BonusPayment
    {
        get => _bonusPayment;
        set => SetProperty(ref _bonusPayment, CoerceNumericInput(value, 0));
    }

    public string CashBalanceText => $"{_customer.CashBalance:0.00} руб.";

    public string CardBalanceText => $"{_customer.DebitCardBalance:0.00} руб.";

    public string BonusBalanceText => $"{_customer.BonusCard.Points:0.00}";

    public string CartTotalText => $"Итого: {_cart.Total:0.00} руб.";

    public string CatalogResultText => Products.Count == _allProducts.Count
        ? $"{Products.Count} товаров"
        : $"{Products.Count} из {_allProducts.Count} товаров";

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
            _cart.Clear();
            ResetPayments();
            RefreshCart();
            ApplyCatalogFilters();
            SelectedProduct = Products.FirstOrDefault();
            SetStatus("Каталог загружен", "и готов к работе.", InfoBarSeverity.Success);
        }
        catch (Exception exception)
        {
            SetStatus("Ошибка загрузки", exception.Message, InfoBarSeverity.Error);
        }
    }

    /// Добавляет выбранный товар в корзину с учётом количества, веса и цвета.
    private void AddSelectedProductToCart()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        try
        {
            var amount = SelectedProduct.RequiresWeighing
                ? SelectedProduct.WeighedAmount ?? 0
                : ToDecimal(QuantityToAdd);

            _cart.Add(
                SelectedProduct.Product,
                amount,
                !SelectedProduct.RequiresWeighing || SelectedProduct.WeighedAmount is not null,
                SelectedProduct.SelectedColor);
            SelectedProduct.ResetWeighing();
            RefreshSelectedProductActionState();
            RefreshCart();
            SetStatus("Корзина обновлена", $"{SelectedProduct.Name} добавлен в корзину.", InfoBarSeverity.Success);
        }
        catch (ProductMustBeWeightedException exception)
        {
            SetStatus("Нужно взвесить", $"Перед добавлением взвесьте товар: {exception.ProductName}.", InfoBarSeverity.Warning);
        }
        catch (Exception exception)
        {
            SetStatus("Не удалось добавить", exception.Message, InfoBarSeverity.Error);
        }
    }

    /// Фиксирует вес выбранного весового товара перед добавлением в корзину.
    private void WeighSelectedProduct()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        if (!SelectedProduct.RequiresWeighing)
        {
            SetStatus("Взвешивание не требуется", "Этот товар продаётся поштучно.", InfoBarSeverity.Informational);
            return;
        }

        try
        {
            var amount = ToDecimal(WeightToAddGrams);
            SelectedProduct.Product.EnsureCanReserve(GetReservedAmount(SelectedProduct.Product) + amount);
            SelectedProduct.MarkWeighed(amount);
            RefreshSelectedProductActionState();
            SetStatus("Товар взвешен", $"{SelectedProduct.Name}: {amount:0.##} г.", InfoBarSeverity.Success);
        }
        catch (Exception exception)
        {
            SetStatus("Не удалось взвесить", exception.Message, InfoBarSeverity.Error);
        }
    }

    /// Удаляет выбранную строку из корзины.
    private void RemoveSelectedCartLine()
    {
        if (SelectedCartLine is null)
        {
            return;
        }

        var removedName = SelectedCartLine.Name;
        _cart.Remove(SelectedCartLine.Item);
        RefreshCart();
        SetStatus("Товар выложен", $"{removedName} удалён из корзины.", InfoBarSeverity.Informational);
    }

    /// Полностью очищает корзину и связанные суммы оплаты.
    private void ClearCart()
    {
        if (_cart.IsEmpty)
        {
            return;
        }

        _cart.Clear();
        ResetPayments();
        RefreshCart();
        SetStatus("Корзина очищена", "Все товары удалены из корзины.", InfoBarSeverity.Informational);
    }

    /// Заполняет оплату картой на сумму текущей корзины.
    private void FillCardPayment()
    {
        BonusPayment = 0;
        CashPayment = 0;
        CardPayment = (double)_cart.Total;
    }

    /// Распределяет оплату между бонусами, картой и наличными.
    private void FillMixedPayment()
    {
        var total = _cart.Total;
        var bonus = Math.Min(_customer.BonusCard.Points, total);
        var afterBonus = total - bonus;
        var card = Math.Min(_customer.DebitCardBalance, afterBonus);
        var cash = Math.Max(0, afterBonus - card);

        BonusPayment = (double)bonus;
        CardPayment = (double)card;
        CashPayment = (double)cash;
    }

    /// Оформляет покупку, списывает средства, сохраняет остатки и формирует чек.
    private async Task CheckoutAsync()
    {
        try
        {
            if (CashPayment <= 0 && CardPayment <= 0 && BonusPayment <= 0)
            {
                FillCardPayment();
            }

            var receipt = await _checkoutService.CheckoutAsync(
                _customer,
                _cart,
                new[]
                {
                    new PaymentPart(PaymentMethod.Bonus, ToDecimal(BonusPayment)),
                    new PaymentPart(PaymentMethod.DebitCard, ToDecimal(CardPayment)),
                    new PaymentPart(PaymentMethod.Cash, ToDecimal(CashPayment))
                },
                _receiptWriter);

            await _productRepository.SaveAsync(_inventory);
            SyncUserBalances();
            await _userRepository.UpdateAsync(_user);
            PurchaseHistory.Insert(0, new ReceiptViewModel(receipt));
            ResetPayments();
            RefreshCart();
            RefreshBalances();
            RefreshProductStocks();
            ApplyCatalogFilters();
            SetStatus("Покупка совершена", "Чек сохранён. Его можно открыть в истории покупок.", InfoBarSeverity.Success);
        }
        catch (InsufficientFundsException exception)
        {
            SetStatus("Недостаточно средств", $"Не хватает {exception.Shortage:0.00} руб. Выложите часть товаров или измените оплату.", InfoBarSeverity.Warning);
        }
        catch (Exception exception)
        {
            SetStatus("Покупка не завершена", exception.Message, InfoBarSeverity.Error);
        }
    }

    /// Перестраивает отображаемые строки корзины и итоговую сумму.
    private void RefreshCart()
    {
        CartLines.Clear();
        foreach (var item in _cart.Items)
        {
            CartLines.Add(new CartLineViewModel(item));
        }

        SelectedCartLine = CartLines.FirstOrDefault();
        OnPropertyChanged(nameof(HasCartItems));
        OnPropertyChanged(nameof(CartTotalText));
        RefreshCartActionHint();
        RefreshPaymentActionHint();
        RefreshSelectedProductActionState();
        RefreshCommandStates();
    }

    /// Обновляет отображаемые балансы пользователя.
    private void RefreshBalances()
    {
        OnPropertyChanged(nameof(CashBalanceText));
        OnPropertyChanged(nameof(CardBalanceText));
        OnPropertyChanged(nameof(BonusBalanceText));
    }

    /// Переносит актуальные балансы доменной модели в запись пользователя.
    private void SyncUserBalances()
    {
        _user.CashBalance = _customer.CashBalance;
        _user.CardBalance = _customer.DebitCardBalance;
        _user.BonusPoints = _customer.BonusCard.Points;
    }

    /// Обновляет отображаемые остатки товаров после изменения склада.
    private void RefreshProductStocks()
    {
        foreach (var product in _allProducts)
        {
            product.RefreshStock();
        }
    }

    /// Обновляет доступность команд, зависящих от корзины.
    private void RefreshCommandStates()
    {
        RemoveFromCartCommand.NotifyCanExecuteChanged();
        ClearCartCommand.NotifyCanExecuteChanged();
        FillCardPaymentCommand.NotifyCanExecuteChanged();
        FillMixedPaymentCommand.NotifyCanExecuteChanged();
        CheckoutCommand.NotifyCanExecuteChanged();
    }

    /// Сбрасывает суммы оплаты бонусами, картой и наличными.
    private void ResetPayments()
    {
        BonusPayment = 0;
        CardPayment = 0;
        CashPayment = 0;
    }

    /// Обновляет доступность действий для выбранного товара.
    private void RefreshSelectedProductActionState()
    {
        AddToCartCommand.NotifyCanExecuteChanged();
        WeighCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanAddSelectedProduct));
        OnPropertyChanged(nameof(CanWeighSelectedProduct));
        OnPropertyChanged(nameof(CanEditQuantity));
        OnPropertyChanged(nameof(CanEditWeight));
        OnPropertyChanged(nameof(ProductActionHint));
        OnPropertyChanged(nameof(HasProductActionHint));
    }

    /// Обновляет подсказку к действиям над корзиной.
    private void RefreshCartActionHint()
    {
        OnPropertyChanged(nameof(CartActionHint));
        OnPropertyChanged(nameof(HasCartActionHint));
    }

    /// Обновляет подсказку к блоку оплаты.
    private void RefreshPaymentActionHint()
    {
        OnPropertyChanged(nameof(PaymentActionHint));
        OnPropertyChanged(nameof(HasPaymentActionHint));
    }

    /// Обновляет состояние фильтров и доступность кнопки сброса.
    private void RefreshFilterState()
    {
        ClearCatalogFiltersCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasCatalogFilters));
    }

    /// Применяет поиск, категорию и сортировку к каталогу покупателя.
    private void ApplyCatalogFilters()
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
        OnPropertyChanged(nameof(CatalogResultText));
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

    /// Сбрасывает фильтры каталога покупателя.
    private void ClearCatalogFilters()
    {
        SearchText = string.Empty;
        SelectedCategory = AllCategories;
        SelectedSortOption = SortDefault;
        ApplyCatalogFilters();
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

    /// Формирует пояснение, почему выбранный товар можно или нельзя добавить.
    private string CreateProductActionHint()
    {
        if (SelectedProduct is null)
        {
            return Products.Count == 0 && _allProducts.Count > 0
                ? "По текущим фильтрам товаров нет"
                : "Товар не выбран";
        }

        var remainingStock = GetRemainingStock(SelectedProduct.Product);
        if (remainingStock <= 0)
        {
            return "Товар закончился";
        }

        if (SelectedProduct.RequiresWeighing)
        {
            var weight = ToDecimalOrZero(WeightToAddGrams);
            if (weight > remainingStock)
            {
                return $"На складе осталось {remainingStock:0.##} {SelectedProduct.Product.UnitName}";
            }

            return SelectedProduct.WeighedAmount is null
                ? "Материал нужно взвесить перед добавлением"
                : string.Empty;
        }

        var quantity = ToDecimalOrZero(QuantityToAdd);
        if (decimal.Truncate(quantity) != quantity)
        {
            return "Количество должно быть целым";
        }

        return quantity > remainingStock
            ? $"На складе осталось {remainingStock:0.##} {SelectedProduct.Product.UnitName}"
            : string.Empty;
    }

    /// Вычисляет доступный остаток товара с учётом уже зарезервированного количества.
    private decimal GetRemainingStock(ProductBase product)
    {
        return Math.Max(0, product.StockAmount - GetReservedAmount(product));
    }

    /// Суммирует количество выбранного товара, уже находящееся в корзине.
    private decimal GetReservedAmount(ProductBase product)
    {
        return _cart.Items
            .Where(item => item.Product.Id == product.Id)
            .Sum(item => item.Amount);
    }

    /// Преобразует числовой ввод NumberBox в decimal.
    private static decimal ToDecimal(double value)
    {
        return decimal.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
    }

    /// Преобразует числовой ввод в decimal и заменяет некорректные значения нулём.
    private static decimal ToDecimalOrZero(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            return 0;
        }

        return ToDecimal(value);
    }

    /// Нормализует числовой ввод и не допускает значения ниже минимума.
    private static double CoerceNumericInput(double value, double minimum)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum)
        {
            return minimum;
        }

        return value;
    }

    /// Создаёт приветствие пользователя по текущему часу суток.
    private static string CreateGreeting(int hour)
    {
        return hour switch
        {
            >= 5 and < 12 => "Доброе утро",
            >= 12 and < 18 => "Добрый день",
            >= 18 and < 23 => "Добрый вечер",
            _ => "Доброй ночи"
        };
    }
}
