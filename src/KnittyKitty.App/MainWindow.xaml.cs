using KnittyKitty.App.ViewModels;
using KnittyKitty.Core.Models;
using KnittyKitty.Core.Repositories;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace KnittyKitty.App;

public sealed partial class MainWindow : Window
{
    private const string AdminEmail = "kate@yandex.ru";
    private const int MinimumWindowWidth = 1280;
    private const int MinimumWindowHeight = 1200;
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint WindowSubclassId = 1;

    private readonly string _dataPath;
    private readonly SqliteUserRepository _userRepository;
    private readonly AuthViewModel _authViewModel;
    private readonly SubclassProc _windowSubclassProc;

    /// Создаёт главное окно, подключает модели представления и готовит начальный экран авторизации.
    public MainWindow()
    {
        _windowSubclassProc = WindowSubclassProc;

        InitializeComponent();
        Root.SizeChanged += Root_SizeChanged;
        Root.Loaded += (_, _) => ApplyAdaptiveLayout(Root.ActualWidth);

        _dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "knittykitty.db");
        SqliteSeedData.Ensure(_dataPath);
        _userRepository = new SqliteUserRepository(_dataPath);
        _authViewModel = new AuthViewModel(_userRepository);
        _authViewModel.Authenticated += AuthViewModel_Authenticated;
        AuthRoot.DataContext = _authViewModel;
    }

    public MainViewModel? ViewModel { get; private set; }

    public AdminViewModel? AdminViewModel { get; private set; }

    /// Обрабатывает успешную авторизацию и открывает экран покупателя или администратора.
    private async void AuthViewModel_Authenticated(object? sender, UserRecord user)
    {
        AuthRoot.Visibility = Visibility.Collapsed;

        if (IsAdmin(user))
        {
            AdminViewModel = new AdminViewModel(new SqliteProductRepository(_dataPath));
            AdminRoot.DataContext = AdminViewModel;
            AdminRoot.Visibility = Visibility.Visible;
            ShopRoot.Visibility = Visibility.Collapsed;

            await AdminViewModel.InitializeAsync();
            return;
        }

        ViewModel = new MainViewModel(user, _userRepository);
        ShopRoot.DataContext = ViewModel;
        AdminRoot.Visibility = Visibility.Collapsed;
        ShopRoot.Visibility = Visibility.Visible;

        await ViewModel.InitializeAsync();
    }

    /// Передаёт изменённый пароль входа в модель авторизации.
    private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _authViewModel.LoginPassword = ((PasswordBox)sender).Password;
    }

    /// Запускает проверку email входа после ухода фокуса из поля.
    private void LoginEmailBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _authViewModel.TouchLoginEmail();
    }

    /// Запускает проверку пароля входа после ухода фокуса из поля.
    private void LoginPasswordBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _authViewModel.TouchLoginPassword();
    }

    /// Передаёт изменённый пароль регистрации в модель авторизации.
    private void RegisterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _authViewModel.RegisterPassword = ((PasswordBox)sender).Password;
    }

    /// Передаёт изменённое подтверждение пароля в модель авторизации.
    private void RegisterPasswordConfirmationBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _authViewModel.RegisterPasswordConfirmation = ((PasswordBox)sender).Password;
    }

    /// Запускает проверку имени после ухода фокуса из поля.
    private void RegisterNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _authViewModel.TouchRegisterName();
    }

    /// Запускает проверку email регистрации после ухода фокуса из поля.
    private void RegisterEmailBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _authViewModel.TouchRegisterEmail();
    }

    /// Запускает проверку пароля регистрации после ухода фокуса из поля.
    private void RegisterPasswordBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _authViewModel.TouchRegisterPassword();
    }

    /// Запускает проверку подтверждения пароля после ухода фокуса из поля.
    private void RegisterPasswordConfirmationBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _authViewModel.TouchRegisterPasswordConfirmation();
    }

    /// Обрабатывает выход покупателя и возвращает экран авторизации.
    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        ReturnToAuth();
    }

    /// Обрабатывает выход администратора и возвращает экран авторизации.
    private void AdminLogoutButton_Click(object sender, RoutedEventArgs e)
    {
        ReturnToAuth();
    }

    /// Скрывает рабочие экраны, очищает формы и показывает авторизацию.
    private void ReturnToAuth()
    {
        ViewModel = null;
        AdminViewModel = null;
        ShopRoot.DataContext = null;
        AdminRoot.DataContext = null;
        ShopRoot.Visibility = Visibility.Collapsed;
        AdminRoot.Visibility = Visibility.Collapsed;

        _authViewModel.ResetForLogout();
        LoginPasswordBox.Password = string.Empty;
        RegisterPasswordBox.Password = string.Empty;
        RegisterPasswordConfirmationBox.Password = string.Empty;

        AuthRoot.Visibility = Visibility.Visible;
    }

    /// Пересчитывает адаптивную раскладку при изменении размера окна.
    private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyAdaptiveLayout(e.NewSize.Width);
    }

    /// Выбирает подходящие настройки интерфейса для текущей ширины окна.
    private void ApplyAdaptiveLayout(double width)
    {
        if (width <= 0)
        {
            return;
        }

        ApplyAuthLayout(width);
        ApplyAdminLayout(width);
        ApplyAdminControlsLayout(width);
        ApplyShopHeaderLayout(width);
        ApplyShopContentLayout(width);
        ApplyCatalogControlsLayout(width);
    }

    /// Настраивает раскладку формы авторизации для широкой или узкой области.
    private void ApplyAuthLayout(double width)
    {
        var isCompact = width < 560;
        AuthRoot.Padding = isCompact ? new Thickness(12) : new Thickness(28);
        AuthPanel.Padding = isCompact ? new Thickness(16) : new Thickness(24);
        AuthContent.RowSpacing = isCompact ? 12 : 18;

        var logoWidth = Math.Clamp(width - (isCompact ? 48 : 140), 240, 390);
        AuthLogo.Width = logoWidth;
        AuthLogo.Height = logoWidth * 0.85;
    }

    /// Настраивает административный экран для текущей ширины окна.
    private void ApplyAdminLayout(double width)
    {
        var isCompact = width < 900;
        var isTight = width < 560;
        var stackHeader = width < 760;

        AdminHeaderBorder.Padding = isTight ? new Thickness(14, 12, 14, 12) : new Thickness(22, 18, 22, 18);
        AdminHeaderLogo.Width = stackHeader ? 70 : 92;
        AdminHeaderLogo.Height = stackHeader ? 70 : 92;

        AdminHeaderActionRow.Height = stackHeader ? GridLength.Auto : new GridLength(0);
        AdminHeaderMainColumn.Width = new GridLength(1, GridUnitType.Star);
        AdminHeaderActionColumn.Width = stackHeader ? new GridLength(0) : GridLength.Auto;
        AdminHeaderGrid.ColumnSpacing = stackHeader ? 0 : 18;
        AdminHeaderGrid.RowSpacing = stackHeader ? 10 : 0;
        AdminLogoutButton.HorizontalAlignment = stackHeader ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        Grid.SetRow(AdminLogoutButton, stackHeader ? 1 : 0);
        Grid.SetColumn(AdminLogoutButton, stackHeader ? 0 : 1);

        AdminStatusBar.Margin = isTight ? new Thickness(12, 10, 12, 0) : new Thickness(18, 12, 18, 0);
        AdminContentGrid.Padding = isTight ? new Thickness(12) : new Thickness(18);

        if (isCompact)
        {
            AdminInventoryColumn.Width = new GridLength(1, GridUnitType.Star);
            AdminActionColumn.Width = new GridLength(0);
            AdminContentFirstRow.Height = new GridLength(1.35, GridUnitType.Star);
            AdminContentSecondRow.Height = new GridLength(1, GridUnitType.Star);
            AdminContentGrid.ColumnSpacing = 0;
            AdminContentGrid.RowSpacing = 14;
            Grid.SetRow(AdminActionPanel, 1);
            Grid.SetColumn(AdminActionPanel, 0);
        }
        else
        {
            AdminInventoryColumn.Width = new GridLength(2, GridUnitType.Star);
            AdminActionColumn.Width = new GridLength(1, GridUnitType.Star);
            AdminContentFirstRow.Height = new GridLength(1, GridUnitType.Star);
            AdminContentSecondRow.Height = new GridLength(0);
            AdminContentGrid.ColumnSpacing = 14;
            AdminContentGrid.RowSpacing = 0;
            Grid.SetRow(AdminActionPanel, 0);
            Grid.SetColumn(AdminActionPanel, 1);
        }
    }

    /// Настраивает сетку управляющих кнопок администратора.
    private void ApplyAdminControlsLayout(double width)
    {
        ApplyAdminInventoryHeaderLayout(width);
        ApplyAdminFiltersLayout(width);
    }

    /// Настраивает заголовок списка склада под доступную ширину.
    private void ApplyAdminInventoryHeaderLayout(double width)
    {
        var isCompact = width < 560;

        AdminInventoryHeaderActionRow.Height = isCompact ? GridLength.Auto : new GridLength(0);
        AdminInventoryActionColumn.Width = isCompact ? new GridLength(0) : GridLength.Auto;
        AdminInventoryHeaderGrid.ColumnSpacing = isCompact ? 8 : 12;
        AdminInventoryHeaderGrid.RowSpacing = isCompact ? 10 : 0;
        AdminInventorySubheadingPanel.Orientation = isCompact ? Orientation.Vertical : Orientation.Horizontal;
        AdminInventorySubtitleSeparator.Visibility = isCompact ? Visibility.Collapsed : Visibility.Visible;
        AdminReloadButton.HorizontalAlignment = isCompact ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        Grid.SetRow(AdminReloadButton, isCompact ? 1 : 0);
        Grid.SetColumn(AdminReloadButton, isCompact ? 1 : 2);
    }

    /// Настраивает блок фильтров администратора под доступную ширину.
    private void ApplyAdminFiltersLayout(double width)
    {
        if (width < 560)
        {
            AdminSearchColumn.Width = new GridLength(1, GridUnitType.Star);
            AdminCategoryColumn.Width = new GridLength(0);
            AdminSortColumn.Width = new GridLength(0);
            AdminClearColumn.Width = new GridLength(0);
            AdminFiltersSecondRow.Height = GridLength.Auto;
            AdminFiltersThirdRow.Height = GridLength.Auto;
            AdminFiltersFourthRow.Height = GridLength.Auto;
            AdminFiltersGrid.ColumnSpacing = 0;
            AdminFiltersGrid.RowSpacing = 10;
            AdminSortBox.MinWidth = 0;
            AdminClearFiltersButton.HorizontalAlignment = HorizontalAlignment.Right;

            MoveToGrid(AdminSearchBox, 0, 0);
            Grid.SetColumnSpan(AdminSearchBox, 1);
            MoveToGrid(AdminCategoryBox, 1, 0);
            Grid.SetColumnSpan(AdminCategoryBox, 1);
            MoveToGrid(AdminSortBox, 2, 0);
            Grid.SetColumnSpan(AdminSortBox, 1);
            MoveToGrid(AdminClearFiltersButton, 3, 0);
            Grid.SetColumnSpan(AdminClearFiltersButton, 1);
        }
        else if (width < 900)
        {
            AdminSearchColumn.Width = new GridLength(1, GridUnitType.Star);
            AdminCategoryColumn.Width = new GridLength(1, GridUnitType.Star);
            AdminSortColumn.Width = GridLength.Auto;
            AdminClearColumn.Width = new GridLength(0);
            AdminFiltersSecondRow.Height = GridLength.Auto;
            AdminFiltersThirdRow.Height = new GridLength(0);
            AdminFiltersFourthRow.Height = new GridLength(0);
            AdminFiltersGrid.ColumnSpacing = 10;
            AdminFiltersGrid.RowSpacing = 10;
            AdminSortBox.MinWidth = 0;
            AdminClearFiltersButton.HorizontalAlignment = HorizontalAlignment.Stretch;

            MoveToGrid(AdminSearchBox, 0, 0);
            Grid.SetColumnSpan(AdminSearchBox, 2);
            MoveToGrid(AdminClearFiltersButton, 0, 2);
            Grid.SetColumnSpan(AdminClearFiltersButton, 1);
            MoveToGrid(AdminCategoryBox, 1, 0);
            Grid.SetColumnSpan(AdminCategoryBox, 1);
            MoveToGrid(AdminSortBox, 1, 1);
            Grid.SetColumnSpan(AdminSortBox, 2);
        }
        else
        {
            AdminSearchColumn.Width = new GridLength(2, GridUnitType.Star);
            AdminCategoryColumn.Width = new GridLength(1.1, GridUnitType.Star);
            AdminSortColumn.Width = new GridLength(1.65, GridUnitType.Star);
            AdminClearColumn.Width = GridLength.Auto;
            AdminFiltersSecondRow.Height = new GridLength(0);
            AdminFiltersThirdRow.Height = new GridLength(0);
            AdminFiltersFourthRow.Height = new GridLength(0);
            AdminFiltersGrid.ColumnSpacing = 10;
            AdminFiltersGrid.RowSpacing = 0;
            AdminSortBox.MinWidth = 220;
            AdminClearFiltersButton.HorizontalAlignment = HorizontalAlignment.Stretch;

            MoveToGrid(AdminSearchBox, 0, 0);
            Grid.SetColumnSpan(AdminSearchBox, 1);
            MoveToGrid(AdminCategoryBox, 0, 1);
            Grid.SetColumnSpan(AdminCategoryBox, 1);
            MoveToGrid(AdminSortBox, 0, 2);
            Grid.SetColumnSpan(AdminSortBox, 1);
            MoveToGrid(AdminClearFiltersButton, 0, 3);
            Grid.SetColumnSpan(AdminClearFiltersButton, 1);
        }
    }

    /// Настраивает верхнюю панель магазина под доступную ширину.
    private void ApplyShopHeaderLayout(double width)
    {
        var isStacked = width < 1100;
        var isCompact = width < 620;
        var isTight = width < 560;

        ShopHeaderBorder.Padding = isTight ? new Thickness(8, 12, 14, 12) : new Thickness(12, 18, 22, 18);
        var logoWidth = Math.Clamp(width - 56, isTight ? 180 : 220, isStacked ? 250 : 300);
        ShopHeaderLogo.Width = logoWidth;
        ShopHeaderLogo.Height = logoWidth * 134 / 300;

        ShopHeaderBottomRow.Height = isStacked ? GridLength.Auto : new GridLength(0);
        ShopHeaderLogoColumn.Width = isStacked ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        ShopHeaderActionsColumn.Width = isStacked ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        ShopHeaderGrid.ColumnSpacing = isStacked ? 0 : 20;
        ShopHeaderGrid.RowSpacing = isStacked ? 12 : 0;

        Grid.SetRow(ShopHeaderActionsPanel, isStacked ? 1 : 0);
        Grid.SetColumn(ShopHeaderActionsPanel, isStacked ? 0 : 1);
        ShopHeaderActionsPanel.HorizontalAlignment = isStacked ? HorizontalAlignment.Stretch : HorizontalAlignment.Right;
        GreetingTextBlock.HorizontalAlignment = isStacked ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        GreetingTextBlock.TextAlignment = isStacked ? TextAlignment.Left : TextAlignment.Right;
        BalanceGrid.HorizontalAlignment = isStacked ? HorizontalAlignment.Stretch : HorizontalAlignment.Right;

        if (isCompact)
        {
            BalanceBottomRow.Height = GridLength.Auto;
            BalanceCashColumn.Width = new GridLength(1, GridUnitType.Star);
            BalanceCardColumn.Width = new GridLength(1, GridUnitType.Star);
            BalanceBonusColumn.Width = new GridLength(0);
            BalanceActionColumn.Width = new GridLength(0);
            BalanceGrid.ColumnSpacing = 8;
            BalanceGrid.RowSpacing = 8;

            MoveToGrid(CashBalanceCard, 0, 0);
            MoveToGrid(CardBalanceCard, 0, 1);
            MoveToGrid(BonusBalanceCard, 1, 0);
            MoveToGrid(LogoutButton, 1, 1);
            LogoutButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            BalanceBottomRow.Height = new GridLength(0);
            var balanceWidth = isStacked ? new GridLength(1, GridUnitType.Star) : new GridLength(132);
            BalanceCashColumn.Width = balanceWidth;
            BalanceCardColumn.Width = balanceWidth;
            BalanceBonusColumn.Width = balanceWidth;
            BalanceActionColumn.Width = GridLength.Auto;
            BalanceGrid.ColumnSpacing = 10;
            BalanceGrid.RowSpacing = 0;

            MoveToGrid(CashBalanceCard, 0, 0);
            MoveToGrid(CardBalanceCard, 0, 1);
            MoveToGrid(BonusBalanceCard, 0, 2);
            MoveToGrid(LogoutButton, 0, 3);
            LogoutButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
    }

    /// Переключает компоновку каталога и корзины между широкой и узкой версией.
    private void ApplyShopContentLayout(double width)
    {
        var isNarrow = width < 900;
        var isMedium = width >= 900 && width < 1280;
        var isTight = width < 560;

        ShopStatusBar.Margin = isTight ? new Thickness(12, 10, 12, 0) : new Thickness(18, 12, 18, 0);
        ShopContentGrid.Padding = isTight ? new Thickness(12) : new Thickness(18);

        if (isNarrow)
        {
            ShopCatalogColumn.Width = new GridLength(1, GridUnitType.Star);
            ShopCartColumn.Width = new GridLength(0);
            ShopPaymentColumn.Width = new GridLength(0);
            ShopContentFirstRow.Height = new GridLength(1.35, GridUnitType.Star);
            ShopContentSecondRow.Height = new GridLength(0.85, GridUnitType.Star);
            ShopContentThirdRow.Height = new GridLength(1, GridUnitType.Star);
            ShopContentGrid.ColumnSpacing = 0;
            ShopContentGrid.RowSpacing = 14;

            Grid.SetRowSpan(CatalogPanel, 1);
            MoveToGrid(CatalogPanel, 0, 0);
            MoveToGrid(CartPanel, 1, 0);
            MoveToGrid(PaymentPanel, 2, 0);
        }
        else if (isMedium)
        {
            ShopCatalogColumn.Width = new GridLength(1.65, GridUnitType.Star);
            ShopCartColumn.Width = new GridLength(1, GridUnitType.Star);
            ShopPaymentColumn.Width = new GridLength(0);
            ShopContentFirstRow.Height = new GridLength(1, GridUnitType.Star);
            ShopContentSecondRow.Height = new GridLength(1, GridUnitType.Star);
            ShopContentThirdRow.Height = new GridLength(0);
            ShopContentGrid.ColumnSpacing = 14;
            ShopContentGrid.RowSpacing = 14;

            Grid.SetRowSpan(CatalogPanel, 2);
            MoveToGrid(CatalogPanel, 0, 0);
            MoveToGrid(CartPanel, 0, 1);
            MoveToGrid(PaymentPanel, 1, 1);
        }
        else
        {
            ShopCatalogColumn.Width = new GridLength(2.1, GridUnitType.Star);
            ShopCartColumn.Width = new GridLength(1.25, GridUnitType.Star);
            ShopPaymentColumn.Width = new GridLength(1.25, GridUnitType.Star);
            ShopContentFirstRow.Height = new GridLength(1, GridUnitType.Star);
            ShopContentSecondRow.Height = new GridLength(0);
            ShopContentThirdRow.Height = new GridLength(0);
            ShopContentGrid.ColumnSpacing = 14;
            ShopContentGrid.RowSpacing = 0;

            Grid.SetRowSpan(CatalogPanel, 1);
            MoveToGrid(CatalogPanel, 0, 0);
            MoveToGrid(CartPanel, 0, 1);
            MoveToGrid(PaymentPanel, 0, 2);
        }
    }

    /// Настраивает управляющую панель каталога.
    private void ApplyCatalogControlsLayout(double width)
    {
        ApplyCatalogHeaderLayout(width);
        ApplyCatalogFiltersLayout(width);
        ApplyCatalogActionLayout(width);
    }

    /// Настраивает заголовок каталога под доступную ширину.
    private void ApplyCatalogHeaderLayout(double width)
    {
        var isCompact = width < 560;

        CatalogHeaderActionRow.Height = isCompact ? GridLength.Auto : new GridLength(0);
        CatalogActionColumn.Width = isCompact ? new GridLength(0) : GridLength.Auto;
        CatalogHeaderGrid.ColumnSpacing = isCompact ? 8 : 12;
        CatalogHeaderGrid.RowSpacing = isCompact ? 10 : 0;
        CatalogSubheadingPanel.Orientation = isCompact ? Orientation.Vertical : Orientation.Horizontal;
        CatalogSubtitleSeparator.Visibility = isCompact ? Visibility.Collapsed : Visibility.Visible;
        CatalogReloadButton.HorizontalAlignment = isCompact ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        Grid.SetRow(CatalogReloadButton, isCompact ? 1 : 0);
        Grid.SetColumn(CatalogReloadButton, isCompact ? 1 : 2);
    }

    /// Настраивает блок фильтров каталога под доступную ширину.
    private void ApplyCatalogFiltersLayout(double width)
    {
        if (width < 560)
        {
            CatalogSearchColumn.Width = new GridLength(1, GridUnitType.Star);
            CatalogCategoryColumn.Width = new GridLength(0);
            CatalogSortColumn.Width = new GridLength(0);
            CatalogClearColumn.Width = new GridLength(0);
            CatalogFiltersSecondRow.Height = GridLength.Auto;
            CatalogFiltersThirdRow.Height = GridLength.Auto;
            CatalogFiltersFourthRow.Height = GridLength.Auto;
            CatalogFiltersGrid.ColumnSpacing = 0;
            CatalogFiltersGrid.RowSpacing = 10;
            CatalogSortBox.MinWidth = 0;
            CatalogClearFiltersButton.HorizontalAlignment = HorizontalAlignment.Right;

            MoveToGrid(CatalogSearchBox, 0, 0);
            Grid.SetColumnSpan(CatalogSearchBox, 1);
            MoveToGrid(CatalogCategoryBox, 1, 0);
            Grid.SetColumnSpan(CatalogCategoryBox, 1);
            MoveToGrid(CatalogSortBox, 2, 0);
            Grid.SetColumnSpan(CatalogSortBox, 1);
            MoveToGrid(CatalogClearFiltersButton, 3, 0);
            Grid.SetColumnSpan(CatalogClearFiltersButton, 1);
        }
        else if (width < 900)
        {
            CatalogSearchColumn.Width = new GridLength(1, GridUnitType.Star);
            CatalogCategoryColumn.Width = new GridLength(1, GridUnitType.Star);
            CatalogSortColumn.Width = GridLength.Auto;
            CatalogClearColumn.Width = new GridLength(0);
            CatalogFiltersSecondRow.Height = GridLength.Auto;
            CatalogFiltersThirdRow.Height = new GridLength(0);
            CatalogFiltersFourthRow.Height = new GridLength(0);
            CatalogFiltersGrid.ColumnSpacing = 10;
            CatalogFiltersGrid.RowSpacing = 10;
            CatalogSortBox.MinWidth = 0;
            CatalogClearFiltersButton.HorizontalAlignment = HorizontalAlignment.Stretch;

            MoveToGrid(CatalogSearchBox, 0, 0);
            Grid.SetColumnSpan(CatalogSearchBox, 2);
            MoveToGrid(CatalogClearFiltersButton, 0, 2);
            Grid.SetColumnSpan(CatalogClearFiltersButton, 1);
            MoveToGrid(CatalogCategoryBox, 1, 0);
            Grid.SetColumnSpan(CatalogCategoryBox, 1);
            MoveToGrid(CatalogSortBox, 1, 1);
            Grid.SetColumnSpan(CatalogSortBox, 2);
        }
        else
        {
            CatalogSearchColumn.Width = new GridLength(2, GridUnitType.Star);
            CatalogCategoryColumn.Width = new GridLength(1.1, GridUnitType.Star);
            CatalogSortColumn.Width = new GridLength(1.65, GridUnitType.Star);
            CatalogClearColumn.Width = GridLength.Auto;
            CatalogFiltersSecondRow.Height = new GridLength(0);
            CatalogFiltersThirdRow.Height = new GridLength(0);
            CatalogFiltersFourthRow.Height = new GridLength(0);
            CatalogFiltersGrid.ColumnSpacing = 10;
            CatalogFiltersGrid.RowSpacing = 0;
            CatalogSortBox.MinWidth = 220;
            CatalogClearFiltersButton.HorizontalAlignment = HorizontalAlignment.Stretch;

            MoveToGrid(CatalogSearchBox, 0, 0);
            Grid.SetColumnSpan(CatalogSearchBox, 1);
            MoveToGrid(CatalogCategoryBox, 0, 1);
            Grid.SetColumnSpan(CatalogCategoryBox, 1);
            MoveToGrid(CatalogSortBox, 0, 2);
            Grid.SetColumnSpan(CatalogSortBox, 1);
            MoveToGrid(CatalogClearFiltersButton, 0, 3);
            Grid.SetColumnSpan(CatalogClearFiltersButton, 1);
        }
    }

    /// Настраивает панель выбора количества, веса и добавления товара.
    private void ApplyCatalogActionLayout(double width)
    {
        if (width < 560)
        {
            CatalogQuantityColumn.Width = new GridLength(1, GridUnitType.Star);
            CatalogWeightColumn.Width = new GridLength(0);
            CatalogWeighColumn.Width = new GridLength(0);
            CatalogAddColumn.Width = new GridLength(0);
            CatalogActionSecondRow.Height = GridLength.Auto;
            CatalogActionThirdRow.Height = GridLength.Auto;
            CatalogActionFourthRow.Height = GridLength.Auto;
            CatalogActionGrid.ColumnSpacing = 0;
            CatalogActionGrid.RowSpacing = 10;

            MoveToGrid(QuantityBox, 0, 0);
            MoveToGrid(WeightBox, 1, 0);
            MoveToGrid(WeighButton, 2, 0);
            MoveToGrid(AddToCartButton, 3, 0);
        }
        else if (width < 900)
        {
            CatalogQuantityColumn.Width = new GridLength(1, GridUnitType.Star);
            CatalogWeightColumn.Width = new GridLength(1, GridUnitType.Star);
            CatalogWeighColumn.Width = new GridLength(0);
            CatalogAddColumn.Width = new GridLength(0);
            CatalogActionSecondRow.Height = GridLength.Auto;
            CatalogActionThirdRow.Height = new GridLength(0);
            CatalogActionFourthRow.Height = new GridLength(0);
            CatalogActionGrid.ColumnSpacing = 10;
            CatalogActionGrid.RowSpacing = 10;

            MoveToGrid(QuantityBox, 0, 0);
            MoveToGrid(WeightBox, 0, 1);
            MoveToGrid(WeighButton, 1, 0);
            MoveToGrid(AddToCartButton, 1, 1);
        }
        else
        {
            CatalogQuantityColumn.Width = new GridLength(1, GridUnitType.Star);
            CatalogWeightColumn.Width = new GridLength(1, GridUnitType.Star);
            CatalogWeighColumn.Width = GridLength.Auto;
            CatalogAddColumn.Width = GridLength.Auto;
            CatalogActionSecondRow.Height = new GridLength(0);
            CatalogActionThirdRow.Height = new GridLength(0);
            CatalogActionFourthRow.Height = new GridLength(0);
            CatalogActionGrid.ColumnSpacing = 10;
            CatalogActionGrid.RowSpacing = 0;

            MoveToGrid(QuantityBox, 0, 0);
            MoveToGrid(WeightBox, 0, 1);
            MoveToGrid(WeighButton, 0, 2);
            MoveToGrid(AddToCartButton, 0, 3);
        }

        Grid.SetColumnSpan(QuantityBox, 1);
        Grid.SetColumnSpan(WeightBox, 1);
        Grid.SetColumnSpan(WeighButton, 1);
        Grid.SetColumnSpan(AddToCartButton, 1);
        WeighButton.HorizontalAlignment = width < 900 ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
        AddToCartButton.HorizontalAlignment = width < 900 ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
    }

    /// Перемещает элемент в указанную строку и колонку Grid.
    private static void MoveToGrid(FrameworkElement element, int row, int column)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
    }

    /// Проверяет, относится ли авторизованный пользователь к роли администратора.
    private static bool IsAdmin(UserRecord user)
    {
        return string.Equals(user.Email, AdminEmail, StringComparison.OrdinalIgnoreCase);
    }

    /// Задаёт иконку окна Windows-приложения из файла ресурсов.
    private void SetWindowIcon()
    {
        var windowHandle = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "WindowIcon.ico");

        if (File.Exists(iconPath))
        {
            appWindow.SetIcon(iconPath);
        }
    }

    /// Ограничивает минимальный размер окна так, чтобы основные панели оставались доступными.
    private void SetWindowMinimumSize()
    {
        var windowHandle = WindowNative.GetWindowHandle(this);
        SetWindowSubclass(windowHandle, _windowSubclassProc, new UIntPtr(WindowSubclassId), UIntPtr.Zero);

        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var currentSize = appWindow.Size;
        if (currentSize.Width < MinimumWindowWidth || currentSize.Height < MinimumWindowHeight)
        {
            appWindow.Resize(new SizeInt32
            {
                Width = Math.Max(currentSize.Width, MinimumWindowWidth),
                Height = Math.Max(currentSize.Height, MinimumWindowHeight)
            });
        }
    }

    private IntPtr WindowSubclassProc(
        IntPtr hWnd,
        uint uMsg,
        UIntPtr wParam,
        IntPtr lParam,
        UIntPtr uIdSubclass,
        UIntPtr dwRefData)
    {
        if (uMsg == WmGetMinMaxInfo && lParam != IntPtr.Zero)
        {
            var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            minMaxInfo.MinTrackSize.X = MinimumWindowWidth;
            minMaxInfo.MinTrackSize.Y = MinimumWindowHeight;
            Marshal.StructureToPtr(minMaxInfo, lParam, false);
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    [DllImport("Comctl32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(
        IntPtr hWnd,
        SubclassProc pfnSubclass,
        UIntPtr uIdSubclass,
        UIntPtr dwRefData);

    [DllImport("Comctl32.dll", ExactSpelling = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, UIntPtr wParam, IntPtr lParam);

    private delegate IntPtr SubclassProc(
        IntPtr hWnd,
        uint uMsg,
        UIntPtr wParam,
        IntPtr lParam,
        UIntPtr uIdSubclass,
        UIntPtr dwRefData);

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point Reserved;
        public Point MaxSize;
        public Point MaxPosition;
        public Point MinTrackSize;
        public Point MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }
}
