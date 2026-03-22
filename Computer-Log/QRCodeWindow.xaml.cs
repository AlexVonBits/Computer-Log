using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Entertainment
{
    public partial class QRCodeWindow : Window
    {
        public QRCodeWindow(string upc, Bitmap qrBitmap)
        {
            InitializeComponent();

            UPCLabel.Text = $"UPC: {upc}";
            
            // Convert System.Drawing.Bitmap to BitmapImage for WPF
            QRCodeImage.Source = ConvertBitmapToBitmapImage(qrBitmap);
        }

        private static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using var memory = new System.IO.MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
