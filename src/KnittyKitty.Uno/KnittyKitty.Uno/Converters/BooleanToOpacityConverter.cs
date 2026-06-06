using Microsoft.UI.Xaml.Data;

namespace KnittyKitty.Uno.Converters;

public sealed class BooleanToOpacityConverter : IValueConverter
{
    public double EnabledOpacity { get; set; } = 1;

    public double DisabledOpacity { get; set; } = 0.42;

    /// Преобразует логическое значение в непрозрачность элемента интерфейса.
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? EnabledOpacity : DisabledOpacity;
    }

    /// Запрещает обратное преобразование, так как привязка используется только для вывода.
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is double opacity && opacity >= (EnabledOpacity + DisabledOpacity) / 2;
    }
}
