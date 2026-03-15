using System.Windows;
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
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim().ToUpperInvariant();
            string password = PasswordBox.Text.ToUpperInvariant();

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
            this.Close();
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
