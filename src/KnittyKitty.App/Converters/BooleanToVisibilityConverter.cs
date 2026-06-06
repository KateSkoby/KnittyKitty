using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace KnittyKitty.App.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    /// Преобразует логическое значение в состояние видимости элемента.
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    /// Запрещает обратное преобразование, так как привязка используется только для вывода.
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Visible;
    }
}
