using System;
using System.Globalization;
using System.Windows.Data;

namespace LibraryAccounting.Converters   // 👈 ВАЖНО: отдельный namespace
{
    public class OverdueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return false;

            DateTime? dueDate = values[0] as DateTime?;
            DateTime? returnDate = values[1] as DateTime?;

            if (dueDate == null)
                return false;

            return returnDate == null && dueDate < DateTime.Now;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}