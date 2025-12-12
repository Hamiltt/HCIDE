using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HCIDE.Utils.Converters
{
    // Конвертер проверяет: если строка не пустая -> Visibility.Visible, иначе -> Visibility.Collapsed
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return !string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}