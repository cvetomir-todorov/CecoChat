using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CecoChat.Client.Wpf.Infrastructure
{
    [ValueConversion(sourceType: typeof(bool), targetType: typeof(SolidColorBrush))]
    public class BooleanToColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color colorIfTrue = Colors.LimeGreen;
            Color colorIfFalse = Colors.Transparent;

            if (parameter != null)
            {
                // format is ColorNameIfTrue;ColorNameIfFalse
                string parameterString = parameter.ToString();
                if (!string.IsNullOrEmpty(parameterString))
                {
                    string[] parameters = parameterString.Split(';');
                    if (parameters.Length > 0 && !string.IsNullOrWhiteSpace(parameters[0]))
                    {
                        colorIfTrue = ColorFromName(parameters[0]);
                    }
                    if (parameters.Length > 1 && !string.IsNullOrWhiteSpace(parameters[1]))
                    {
                        colorIfFalse = ColorFromName(parameters[1]);
                    }
                }
            }

            if (value != null && (bool)value)
            {
                return new SolidColorBrush(colorIfTrue);
            }
            else
            {
                return new SolidColorBrush(colorIfFalse);
            }
        }

        private static Color ColorFromName(string colorName)
        {
            System.Drawing.Color systemColor = System.Drawing.Color.FromName(colorName);
            return Color.FromArgb(systemColor.A, systemColor.R, systemColor.G, systemColor.B);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
