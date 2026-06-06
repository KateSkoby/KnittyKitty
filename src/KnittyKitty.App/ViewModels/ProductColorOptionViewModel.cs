namespace KnittyKitty.App.ViewModels;

public sealed class ProductColorOptionViewModel : ObservableObject
{
    private bool _isSelected;
    private readonly Action<ProductColorOptionViewModel> _select;

    /// Создаёт вариант цвета товара и команду выбора этого цвета.
    public ProductColorOptionViewModel(string name, Action<ProductColorOptionViewModel> select)
    {
        Name = name;
        _select = select;
        SelectCommand = new RelayCommand(() => _select(this));
    }

    public string Name { get; }

    public RelayCommand SelectCommand { get; }

    public bool IsSelected
    {
        get => _isSelected;
        private set => SetProperty(ref _isSelected, value);
    }

    /// Устанавливает признак выбранного цвета и обновляет привязку.
    public void SetSelected(bool isSelected)
    {
        IsSelected = isSelected;
    }
}
