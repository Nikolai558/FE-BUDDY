using System;
using System.Globalization;
using System.Windows.Data;

// This namespace contains the converters related to the FeBuddyWPF application.
namespace FeBuddyWPF.Converters
{
    // This class converts an enum value to a boolean value and vice versa.
    public class EnumToBooleanConverter : IValueConverter
    {
        // Gets or sets the type of the enum.
        public Type EnumType { get; set; }

        // Converts an enum value to a boolean value.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string enumString)
            {
                if (Enum.IsDefined(EnumType, value))
                {
                    var enumValue = Enum.Parse(EnumType, enumString);

                    return enumValue.Equals(value);
                }
            }

            return false;
        }

        // Converts a boolean value back to the corresponding enum value.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string enumString)
            {
                return Enum.Parse(EnumType, enumString);
            }

            return null;
        }
    }
}
