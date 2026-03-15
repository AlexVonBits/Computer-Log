using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Entertainment
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Save on window open
            Loaded += MainWindow_Loaded;
            // Save on window close
            Closing += MainWindow_Closing;
            
            // Wire up Sign Out button
            SignOut.Click += SignOut_Click;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            App.SaveAll();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            App.SaveAll();
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            // Close this window and show login
            this.Close();
            
            App.CurrentUser = null;
            var login = new LoginPage();
            login.Show();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser != null)
            {
                var profileWindow = new ProfileWindow(App.CurrentUser);
                profileWindow.Owner = this;
                profileWindow.ShowDialog();
            }
        }
    }
}
