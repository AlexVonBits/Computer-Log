using System.Windows;

namespace Entertainment
{
    public partial class QRCodeWindow : Window
    {
        public QRCodeWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}