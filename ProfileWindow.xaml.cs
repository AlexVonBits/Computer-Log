using System.Windows;
using Entertainment.Security;
using Entertainment.Users;

namespace Entertainment
{
    public partial class ProfileWindow : Window
    {
        private readonly Associate _currentUser;
        
        public ProfileWindow(Associate currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            
            LoadUserInfo();
            SetupUserManagement();
            
            // Save on window close
            Closing += ProfileWindow_Closing;
            
            // Wire up header button
            SignOutButton.Click += SignOut_Click;
        }

        private void ProfileWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            App.SaveAll();
        }

        private void LoadUserInfo()
        {
            UsernameText.Text = _currentUser.Username;
            NameText.Text = $"{_currentUser.FirstName} {_currentUser.LastName}";
            InitialsText.Text = _currentUser.Initials;
            
            if (_currentUser.IsTeamLead)
                RoleText.Text = "TeamLead";
            else if (_currentUser.IsAdmin)
                RoleText.Text = "Admin";
            else
                RoleText.Text = "Associate";
        }

        private void SetupUserManagement()
        {
            // Show user management section only for TeamLead
            if (_currentUser.IsTeamLead)
            {
                UserManagementSection.Visibility = Visibility.Visible;
                CreateTeamLeadButton.Visibility = Visibility.Visible;
                LoadAssociates();
            }
        }

        private void LoadAssociates()
        {
            AssociatesDataGrid.ItemsSource = null;
            AssociatesDataGrid.ItemsSource = App.Store.Associates;
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string oldPassword = OldPasswordBox.Password.ToUpperInvariant();
            string newPassword = NewPasswordBox.Password.ToUpperInvariant();
            string confirmPassword = ConfirmPasswordBox.Password.ToUpperInvariant();

            // Validate old password
            if (!PasswordSecurity.VerifyPassword(oldPassword, _currentUser.PasswordHash))
            {
                PasswordMessage.Text = "Current password is incorrect.";
                return;
            }

            // Validate new password
            if (string.IsNullOrEmpty(newPassword))
            {
                PasswordMessage.Text = "New password cannot be empty.";
                return;
            }

            if (newPassword != confirmPassword)
            {
                PasswordMessage.Text = "New passwords do not match.";
                return;
            }

            if (newPassword == oldPassword)
            {
                PasswordMessage.Text = "New password must be different from old password.";
                return;
            }

            // Update password
            _currentUser.PasswordHash = PasswordSecurity.HashPassword(newPassword);
            App.Store.Save();
            
            PasswordMessage.Foreground = System.Windows.Media.Brushes.Green;
            PasswordMessage.Text = "Password changed successfully!";
            
            // Clear fields
            OldPasswordBox.Password = "";
            NewPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            string username = NewUsernameBox.Text.Trim().ToUpperInvariant();
            string password = NewPasswordBox.Password.ToUpperInvariant();
            string firstName = NewFirstNameBox.Text.Trim();
            string lastName = NewLastNameBox.Text.Trim();
            bool isAdmin = NewUserAdminCheckBox.IsChecked ?? false;

            // Validate input
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                MessageBox.Show("All fields are required.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check if username already exists
            if (App.Store.TeamLead.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Username already exists as TeamLead.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (var associate in App.Store.Associates)
            {
                if (associate.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Username already exists.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Create new associate
            var newAssociate = new Associate
            {
                Id = App.Store.Associates.Count > 0 ? App.Store.Associates.Max(a => a.Id) + 1 : 1,
                Username = username,
                PasswordHash = PasswordSecurity.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                Initials = $"{firstName[0]}{lastName[0]}".ToUpperInvariant(),
                IsAdmin = isAdmin,
                IsTeamLead = false
            };

            App.Store.Associates.Add(newAssociate);
            App.Store.Save();
            
            // Clear form
            NewUsernameBox.Text = "";
            NewPasswordBox.Password = "";
            NewFirstNameBox.Text = "";
            NewLastNameBox.Text = "";
            NewUserAdminCheckBox.IsChecked = false;
            
            // Refresh grid
            LoadAssociates();
            
            MessageBox.Show("User added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (AssociatesDataGrid.SelectedItem is Associate selectedAssociate)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete user '{selectedAssociate.Username}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    App.Store.Associates.Remove(selectedAssociate);
                    App.Store.Save();
                    LoadAssociates();
                }
            }
            else
            {
                MessageBox.Show("Please select a user to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CreateTeamLeadButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will create a new TeamLead account. You will be logged out and need to log in with the new credentials. Continue?",
                "Create New TeamLead",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var teamLeadSetup = new TeamLeadSetupWindow(App.Store);
                if (teamLeadSetup.ShowDialog() == true)
                {
                    // Close all windows and restart login
                    Application.Current.MainWindow?.Close();
                    this.Close();
                    
                    // Show new login
                    var login = new LoginPage();
                    login.Show();
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            App.CurrentUser = null;
            var login = new LoginPage();
            login.Show();
        }
    }
}
