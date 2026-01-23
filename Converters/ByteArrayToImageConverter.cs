using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LibraryAccounting.Converters
{
    public class ByteArrayToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                byte[] bytes = value as byte[];

                if (bytes != null && bytes.Length > 0)
                {
                    BitmapImage image = new BitmapImage();
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze();
                    }
                    return image;
                }

                // 🔥 ЗАГЛУШКА
                return new BitmapImage(
                    new Uri("pack://application:,,,/Images/nofoto.png")
                );
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
