using System.Windows;

namespace Entertainment
{
    public partial class InventoryVerificationWindow : Window
    {
        public InventoryVerificationWindow()
        {
            InitializeComponent();
        }

        private void StartVerificationButton_Click(object sender, RoutedEventArgs e)
        {
            var verificationSession = new VerificationSessionWindow();
            verificationSession.Show();
        }
    }
}