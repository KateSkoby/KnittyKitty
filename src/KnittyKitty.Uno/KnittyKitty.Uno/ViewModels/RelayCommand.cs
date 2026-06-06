using System.Windows.Input;

namespace KnittyKitty.Uno.ViewModels;

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// Создаёт синхронную команду с действием и необязательной проверкой доступности.
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    /// Проверяет, разрешено ли выполнение команды в текущем состоянии.
    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    /// Выполняет действие команды, если проверка доступности прошла успешно.
    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute();
        }
    }

    /// Уведомляет интерфейс о возможном изменении доступности команды.
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
