using System;
using System.Windows;
using System.Windows.Controls;
using Entertainment.Security;
using Entertainment.Users;

namespace Entertainment
{
    public partial class LoginPage : Window
    {
        public LoginPage()
        {
            InitializeComponent();
            SignIn.Click += SignIn_Click;
            Loaded += LoginPage_Loaded;
        }

        private void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim().ToUpperInvariant();
            string password = PasswordBox.Password.ToUpperInvariant();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check TeamLead credentials
            var teamLead = App.Store.TeamLead;
            if (teamLead.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                PasswordSecurity.VerifyPassword(password, teamLead.PasswordHash))
            {
                App.CurrentUser = teamLead;
                OpenMainWindow();
                return;
            }

            // Check Associate credentials
            foreach (var associate in App.Store.Associates)
            {
                if (associate.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                    PasswordSecurity.VerifyPassword(password, associate.PasswordHash))
                {
                    App.CurrentUser = associate;
                    OpenMainWindow();
                    return;
                }
            }

            MessageBox.Show("Invalid username or password.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OpenMainWindow()
        {
            try
            {
                // Create main window
                var mainWindow = new MainWindow();
                
                // Close login page first to prevent race conditions
                this.Close();
                
                // Set and show main window after login is closed
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open main window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
