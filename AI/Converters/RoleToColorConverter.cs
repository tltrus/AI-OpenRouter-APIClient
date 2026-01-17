using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AI.Converters
{
    public class RoleToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string role)
            {
                return role.ToLower() switch
                {
                    "user" => new SolidColorBrush(Color.FromArgb(255, 232, 245, 233)), // Светло-зеленый
                    "assistant" => new SolidColorBrush(Color.FromArgb(255, 227, 242, 253)), // Светло-синий
                    _ => Brushes.WhiteSmoke
                };
            }
            return Brushes.WhiteSmoke;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}