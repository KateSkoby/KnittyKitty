using KnittyKitty.Core.Models;
using KnittyKitty.Core.Repositories;
using KnittyKitty.Core.Security;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml.Controls;

namespace KnittyKitty.Uno.ViewModels;

public sealed class AuthViewModel : ObservableObject
{
    private const decimal StartingTotalMoneyBalance = 10000m;
    private const decimal StartingBonusPoints = 100m;
    private const int BalanceStep = 100;
    private const int MinimumPasswordLength = 8;
    private const string PasswordSpecialCharacters = "!@#$%^&*?_-";

    private readonly IUserRepository _userRepository;
    private bool _isRegisterMode;
    private bool _loginEmailTouched;
    private bool _loginPasswordTouched;
    private bool _registerNameTouched;
    private bool _registerEmailTouched;
    private bool _registerPasswordTouched;
    private bool _registerPasswordConfirmationTouched;
    private string _loginEmail = string.Empty;
    private string _loginPassword = string.Empty;
    private string _registerName = string.Empty;
    private string _registerEmail = string.Empty;
    private string _registerPassword = string.Empty;
    private string _registerPasswordConfirmation = string.Empty;
    private string _statusTitle = string.Empty;
    private string _statusMessage = string.Empty;
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

    /// Создаёт модель авторизации и подключает команды входа и регистрации.
    public AuthViewModel(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        ShowLoginCommand = new RelayCommand(ShowLogin);
        ShowRegisterCommand = new RelayCommand(ShowRegister);
        LoginCommand = new AsyncRelayCommand(LoginAsync, AreLoginFieldsFilled);
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, AreRegisterFieldsFilled);
    }

    public event EventHandler<UserRecord>? Authenticated;

    public RelayCommand ShowLoginCommand { get; }

    public RelayCommand ShowRegisterCommand { get; }

    public AsyncRelayCommand LoginCommand { get; }

    public AsyncRelayCommand RegisterCommand { get; }

    public bool IsLoginMode => !IsRegisterMode;

    public bool IsRegisterMode
    {
        get => _isRegisterMode;
        private set
        {
            if (SetProperty(ref _isRegisterMode, value))
            {
                OnPropertyChanged(nameof(IsLoginMode));
                ClearStatus();
            }
        }
    }

    public string LoginEmail
    {
        get => _loginEmail;
        set
        {
            if (SetProperty(ref _loginEmail, value))
            {
                ClearStatusIfShown();
                RefreshLoginValidation();
                LoginCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string LoginPassword
    {
        get => _loginPassword;
        set
        {
            if (SetProperty(ref _loginPassword, value))
            {
                ClearStatusIfShown();
                RefreshLoginValidation();
                LoginCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string RegisterName
    {
        get => _registerName;
        set
        {
            if (SetProperty(ref _registerName, value))
            {
                ClearStatusIfShown();
                RefreshRegisterValidation();
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string RegisterEmail
    {
        get => _registerEmail;
        set
        {
            if (SetProperty(ref _registerEmail, value))
            {
                ClearStatusIfShown();
                RefreshRegisterValidation();
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string RegisterPassword
    {
        get => _registerPassword;
        set
        {
            if (SetProperty(ref _registerPassword, value))
            {
                ClearStatusIfShown();
                RefreshRegisterValidation();
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string RegisterPasswordConfirmation
    {
        get => _registerPasswordConfirmation;
        set
        {
            if (SetProperty(ref _registerPasswordConfirmation, value))
            {
                ClearStatusIfShown();
                RefreshRegisterValidation();
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLoginEmailInvalid => _loginEmailTouched && GetLoginEmailHint().Length > 0;

    public string LoginEmailHint => GetLoginEmailHint();

    public bool IsLoginPasswordInvalid => _loginPasswordTouched && GetLoginPasswordHint().Length > 0;

    public string LoginPasswordHint => GetLoginPasswordHint();

    public bool IsRegisterNameInvalid => _registerNameTouched && GetRegisterNameHint().Length > 0;

    public string RegisterNameHint => GetRegisterNameHint();

    public bool IsRegisterEmailInvalid => _registerEmailTouched && GetRegisterEmailHint().Length > 0;

    public string RegisterEmailHint => GetRegisterEmailHint();

    public bool IsRegisterPasswordInvalid => _registerPasswordTouched && GetRegisterPasswordHint().Length > 0;

    public string RegisterPasswordHint => GetRegisterPasswordHint();

    public bool IsRegisterPasswordConfirmationInvalid =>
        _registerPasswordConfirmationTouched && GetRegisterPasswordConfirmationHint().Length > 0;

    public string RegisterPasswordConfirmationHint => GetRegisterPasswordConfirmationHint();

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

    /// Очищает данные авторизации и возвращает форму к начальному состоянию.
    public void ResetForLogout()
    {
        LoginEmail = string.Empty;
        LoginPassword = string.Empty;
        RegisterName = string.Empty;
        RegisterEmail = string.Empty;
        RegisterPassword = string.Empty;
        RegisterPasswordConfirmation = string.Empty;
        IsRegisterMode = false;
        ResetValidationState();
        LoginCommand.NotifyCanExecuteChanged();
        RegisterCommand.NotifyCanExecuteChanged();
        ClearStatus();
    }

    /// Помечает email входа как проверенный и обновляет подсказку валидации.
    public void TouchLoginEmail()
    {
        _loginEmailTouched = true;
        RefreshLoginValidation();
    }

    /// Помечает пароль входа как проверенный и обновляет подсказку валидации.
    public void TouchLoginPassword()
    {
        _loginPasswordTouched = true;
        RefreshLoginValidation();
    }

    /// Помечает имя регистрации как проверенное и обновляет подсказку валидации.
    public void TouchRegisterName()
    {
        _registerNameTouched = true;
        RefreshRegisterValidation();
    }

    /// Помечает email регистрации как проверенный и обновляет подсказку валидации.
    public void TouchRegisterEmail()
    {
        _registerEmailTouched = true;
        RefreshRegisterValidation();
    }

    /// Помечает пароль регистрации как проверенный и обновляет подсказку валидации.
    public void TouchRegisterPassword()
    {
        _registerPasswordTouched = true;
        RefreshRegisterValidation();
    }

    /// Помечает подтверждение пароля как проверенное и обновляет подсказку валидации.
    public void TouchRegisterPasswordConfirmation()
    {
        _registerPasswordConfirmationTouched = true;
        RefreshRegisterValidation();
    }

    /// Проверяет введённые данные входа и открывает приложение для найденного пользователя.
    private async Task LoginAsync()
    {
        var email = LoginEmail.Trim();
        TouchAllLoginFields();

        if (IsLoginEmailInvalid || IsLoginPasswordInvalid)
        {
            SetStatus("Проверьте поля", "Заполните подсвеченные поля.", InfoBarSeverity.Warning);
            return;
        }

        try
        {
            var user = await _userRepository.FindByEmailAsync(email);
            if (user is null || !PasswordHasher.Verify(LoginPassword, user.PasswordHash))
            {
                SetStatus("Вход не выполнен", "Проверьте почту и пароль.", InfoBarSeverity.Error);
                return;
            }

            Authenticated?.Invoke(this, user);
        }
        catch (Exception exception)
        {
            SetStatus("Вход не выполнен", exception.Message, InfoBarSeverity.Error);
        }
    }

    /// Проверяет, заполнены ли обязательные поля входа.
    private bool AreLoginFieldsFilled()
    {
        return !string.IsNullOrWhiteSpace(LoginEmail)
            && !string.IsNullOrWhiteSpace(LoginPassword);
    }

    /// Проверяет, заполнены ли обязательные поля регистрации.
    private bool AreRegisterFieldsFilled()
    {
        return !string.IsNullOrWhiteSpace(RegisterName)
            && !string.IsNullOrWhiteSpace(RegisterEmail)
            && !string.IsNullOrWhiteSpace(RegisterPassword)
            && !string.IsNullOrWhiteSpace(RegisterPasswordConfirmation);
    }

    /// Создаёт нового пользователя после проверки формы регистрации.
    private async Task RegisterAsync()
    {
        var name = RegisterName.Trim();
        var email = RegisterEmail.Trim();
        TouchAllRegisterFields();

        if (IsRegisterNameInvalid
            || IsRegisterEmailInvalid
            || IsRegisterPasswordInvalid
            || IsRegisterPasswordConfirmationInvalid)
        {
            SetStatus("Проверьте поля", "Заполните подсвеченные поля и исправьте пароль.", InfoBarSeverity.Warning);
            return;
        }

        try
        {
            if (await _userRepository.FindByEmailAsync(email) is not null)
            {
                SetStatus("Почта уже занята", "Войдите с этим адресом или используйте другую почту.", InfoBarSeverity.Warning);
                return;
            }

            var (cardBalance, cashBalance) = CreateStartingMoneyBalances();
            var user = new UserRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Email = email,
                PasswordHash = PasswordHasher.Hash(RegisterPassword),
                CardBalance = cardBalance,
                CashBalance = cashBalance,
                BonusPoints = StartingBonusPoints
            };

            await _userRepository.AddAsync(user);
            Authenticated?.Invoke(this, user);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            SetStatus("Почта уже занята", "Войдите с этим адресом или используйте другую почту.", InfoBarSeverity.Warning);
        }
        catch (Exception exception)
        {
            SetStatus("Регистрация не выполнена", exception.Message, InfoBarSeverity.Error);
        }
    }

    /// Переключает экран авторизации в режим входа.
    private void ShowLogin()
    {
        IsRegisterMode = false;
    }

    /// Переключает экран авторизации в режим регистрации.
    private void ShowRegister()
    {
        IsRegisterMode = true;
    }

    /// Записывает текст и тип уведомления для InfoBar.
    private void SetStatus(string title, string message, InfoBarSeverity severity)
    {
        StatusTitle = title;
        StatusMessage = message;
        StatusSeverity = severity;
    }

    /// Очищает текущее уведомление формы.
    private void ClearStatus()
    {
        StatusTitle = string.Empty;
        StatusMessage = string.Empty;
        StatusSeverity = InfoBarSeverity.Informational;
    }

    /// Очищает уведомление только при наличии отображаемого сообщения.
    private void ClearStatusIfShown()
    {
        if (HasStatusMessage)
        {
            ClearStatus();
        }
    }

    /// Помечает все поля входа как проверенные перед попыткой авторизации.
    private void TouchAllLoginFields()
    {
        _loginEmailTouched = true;
        _loginPasswordTouched = true;
        RefreshLoginValidation();
    }

    /// Помечает все поля регистрации как проверенные перед созданием профиля.
    private void TouchAllRegisterFields()
    {
        _registerNameTouched = true;
        _registerEmailTouched = true;
        _registerPasswordTouched = true;
        _registerPasswordConfirmationTouched = true;
        RefreshRegisterValidation();
    }

    /// Сбрасывает признаки проверки и сообщения валидации формы.
    private void ResetValidationState()
    {
        _loginEmailTouched = false;
        _loginPasswordTouched = false;
        _registerNameTouched = false;
        _registerEmailTouched = false;
        _registerPasswordTouched = false;
        _registerPasswordConfirmationTouched = false;
        RefreshLoginValidation();
        RefreshRegisterValidation();
    }

    /// Обновляет признаки ошибок и подсказки для формы входа.
    private void RefreshLoginValidation()
    {
        OnPropertyChanged(nameof(IsLoginEmailInvalid));
        OnPropertyChanged(nameof(LoginEmailHint));
        OnPropertyChanged(nameof(IsLoginPasswordInvalid));
        OnPropertyChanged(nameof(LoginPasswordHint));
    }

    /// Обновляет признаки ошибок и подсказки для формы регистрации.
    private void RefreshRegisterValidation()
    {
        OnPropertyChanged(nameof(IsRegisterNameInvalid));
        OnPropertyChanged(nameof(RegisterNameHint));
        OnPropertyChanged(nameof(IsRegisterEmailInvalid));
        OnPropertyChanged(nameof(RegisterEmailHint));
        OnPropertyChanged(nameof(IsRegisterPasswordInvalid));
        OnPropertyChanged(nameof(RegisterPasswordHint));
        OnPropertyChanged(nameof(IsRegisterPasswordConfirmationInvalid));
        OnPropertyChanged(nameof(RegisterPasswordConfirmationHint));
    }

    /// Возвращает подсказку для email на форме входа.
    private string GetLoginEmailHint()
    {
        if (string.IsNullOrWhiteSpace(LoginEmail))
        {
            return "Введите почту.";
        }

        return IsValidEmail(LoginEmail.Trim()) ? string.Empty : "Введите корректный адрес почты.";
    }

    /// Возвращает подсказку для пароля на форме входа.
    private string GetLoginPasswordHint()
    {
        return string.IsNullOrWhiteSpace(LoginPassword) ? "Введите пароль." : string.Empty;
    }

    /// Возвращает подсказку для имени на форме регистрации.
    private string GetRegisterNameHint()
    {
        return string.IsNullOrWhiteSpace(RegisterName) ? "Введите имя." : string.Empty;
    }

    /// Возвращает подсказку для email на форме регистрации.
    private string GetRegisterEmailHint()
    {
        if (string.IsNullOrWhiteSpace(RegisterEmail))
        {
            return "Введите почту.";
        }

        return IsValidEmail(RegisterEmail.Trim()) ? string.Empty : "Введите корректный адрес почты.";
    }

    /// Возвращает подсказку для пароля на форме регистрации.
    private string GetRegisterPasswordHint()
    {
        if (string.IsNullOrWhiteSpace(RegisterPassword))
        {
            return "Введите пароль.";
        }

        return IsStrongPassword(RegisterPassword)
            ? string.Empty
            : $"Минимум {MinimumPasswordLength} символов: латинская буква, цифра и один из {PasswordSpecialCharacters}.";
    }

    /// Возвращает подсказку для подтверждения пароля.
    private string GetRegisterPasswordConfirmationHint()
    {
        if (string.IsNullOrWhiteSpace(RegisterPasswordConfirmation))
        {
            return "Повторите пароль.";
        }

        return RegisterPassword == RegisterPasswordConfirmation ? string.Empty : "Пароли должны совпадать.";
    }

    /// Проверяет формат email регулярным выражением.
    private static bool IsValidEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex > 0
            && atIndex < email.Length - 1
            && email.IndexOf('.', atIndex) > atIndex + 1;
    }

    /// Проверяет минимальные требования к сложности пароля.
    private static bool IsStrongPassword(string password)
    {
        return password.Length >= MinimumPasswordLength
            && password.Any(IsLatinLetter)
            && password.Any(char.IsDigit)
            && password.Any(character => PasswordSpecialCharacters.Contains(character));
    }

    /// Проверяет, является ли символ латинской буквой.
    private static bool IsLatinLetter(char character)
    {
        return character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    /// Создаёт стартовые денежные балансы для нового пользователя.
    private static (decimal CardBalance, decimal CashBalance) CreateStartingMoneyBalances()
    {
        var totalUnits = (int)(StartingTotalMoneyBalance / BalanceStep);
        var cardUnits = Random.Shared.Next(1, totalUnits);
        var cardBalance = cardUnits * BalanceStep;
        var cashBalance = StartingTotalMoneyBalance - cardBalance;

        return (cardBalance, cashBalance);
    }
}
