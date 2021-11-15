using System;
using System.Globalization;
using System.Windows.Data;

namespace CecoChat.Client.Wpf.Infrastructure
{
    [ValueConversion(sourceType: typeof(string), targetType: typeof(string), ParameterType = typeof(string))]
    public class FormattingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                if (parameter == null)
                {
                    return string.Empty;
                }
                else
                {
                    return parameter.ToString();
                }
            }
            else
            {
                if (parameter == null)
                {
                    return value.ToString();
                }
                else
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    return string.Format(parameter.ToString(), value);
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
