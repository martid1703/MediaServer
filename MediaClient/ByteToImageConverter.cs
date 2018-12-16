using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MediaClient
{
    public class ByteToImageConverter : IValueConverter
    {
        public BitmapImage ConvertByteArrayToBitmap(byte[] imageByteArray)
        {
            using (var ms = new System.IO.MemoryStream(imageByteArray))
            {
                var image = new BitmapImage();
                image.BeginInit();
                //must also set BitmapCacheOptions.OnLoad to achieve that the image is loaded immediately, 
                //otherwise the stream needs to be kept open
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BitmapImage img =null;
            if (value != null)
            {
                img = this.ConvertByteArrayToBitmap(value as byte[]);
            }
            return img;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
