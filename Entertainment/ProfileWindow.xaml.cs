using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Entertainment.Users;
using Entertainment.Security;

namespace Entertainment
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        public ProfileWindow(Associate user) : this()
        {
        }

        private void LoadUserInfo()
        {
            var currentUser = App.CurrentUser;
            if (currentUser != null)
            {
                UsernameText.Text = currentUser.Username;
                NameText.Text = $"{currentUser.FirstName} {currentUser.LastName}";
                InitialsText.Text = currentUser.Initials;
                RoleText.Text = currentUser.IsAdmin ? "TeamLead" : "Associate";

                UserManagementSection.Visibility = currentUser.IsAdmin ? Visibility.Visible : Visibility.Collapsed;

                if (currentUser.IsAdmin)
                {
                    LoadAssociates();
                }
            }
        }

        private void LoadAssociates()
        {
            var user = new User();
            user.Load();
            var associates = user.Associates
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FirstName,
                    u.LastName,
                    u.Initials,
                    u.IsAdmin
                })
                .ToList();

            AssociatesDataGrid.ItemsSource = associates;
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            PasswordMessage.Text = "";
            PasswordMessage.Foreground = System.Windows.Media.Brushes.Red;

            string oldPassword = OldPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(oldPassword))
            {
                PasswordMessage.Text = "Please enter current password.";
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                PasswordMessage.Text = "Please enter new password.";
                return;
            }

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                PasswordMessage.Text = "Please confirm new password.";
                return;
            }

            if (newPassword != confirmPassword)
            {
                PasswordMessage.Text = "New passwords do not match.";
                return;
            }

            if (newPassword.Length < 4)
            {
                PasswordMessage.Text = "Password must be at least 4 characters.";
                return;
            }

            var currentUser = App.CurrentUser;
            if (currentUser != null && PasswordSecurity.VerifyPassword(oldPassword, currentUser.PasswordHash))
            {
                currentUser.PasswordHash = PasswordSecurity.HashPassword(newPassword);
                var user = new User();
                user.TeamLead = currentUser;
                user.Save();
                PasswordMessage.Text = "Password changed successfully!";
                PasswordMessage.Foreground = System.Windows.Media.Brushes.Green;
                OldPasswordBox.Password = "";
                NewPasswordBox.Password = "";
                ConfirmPasswordBox.Password = "";
            }
            else
            {
                PasswordMessage.Text = "Current password is incorrect.";
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            string username = NewUsernameBox.Text.Trim();
            string password = NewUserPasswordBox.Password;
            string firstName = NewFirstNameBox.Text.Trim();
            string lastName = NewLastNameBox.Text.Trim();
            bool isAdmin = NewUserAdminCheckBox.IsChecked ?? false;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string initials = "";
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                initials = (firstName[0].ToString() + lastName[0].ToString()).ToUpper();
            }

            var user = new User();
            user.Load();
            if (user.Associates.Any(a => a.Username == username))
            {
                MessageBox.Show("Username already exists.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newAssociate = new Associate
            {
                Username = username,
                PasswordHash = PasswordSecurity.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                Initials = initials,
                IsAdmin = isAdmin,
                IsTeamLead = false
            };

            user.Associates.Add(newAssociate);
            user.Save();

            MessageBox.Show("User added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            ClearAddUserForm();
            LoadAssociates();
        }

        private void ClearAddUserForm()
        {
            NewUsernameBox.Text = "";
            NewUserPasswordBox.Password = "";
            NewFirstNameBox.Text = "";
            NewLastNameBox.Text = "";
            NewUserAdminCheckBox.IsChecked = false;
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (AssociatesDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Please select a user to delete.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic selectedUser = AssociatesDataGrid.SelectedItem;
            int userId = selectedUser.Id;

            var result = MessageBox.Show($"Are you sure you want to delete user '{selectedUser.Username}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var user = new User();
                user.Load();
                var associate = user.Associates.FirstOrDefault(a => a.Id == userId);
                if (associate != null)
                {
                    user.Associates.Remove(associate);
                    user.Save();
                    MessageBox.Show("User deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadAssociates();
                }
            }
        }

        private void CreateTeamLeadButton_Click(object sender, RoutedEventArgs e)
        {
            string username = NewUsernameBox.Text.Trim();
            string password = NewUserPasswordBox.Password;
            string firstName = NewFirstNameBox.Text.Trim();
            string lastName = NewLastNameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string initials = "";
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                initials = (firstName[0].ToString() + lastName[0].ToString()).ToUpper();
            }

            var user = new User();
            user.Load();
            if (!string.IsNullOrWhiteSpace(user.TeamLead.Username))
            {
                MessageBox.Show("TeamLead already exists.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            user.TeamLead = new Associate
            {
                Username = username,
                PasswordHash = PasswordSecurity.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                Initials = initials,
                IsAdmin = true,
                IsTeamLead = true
            };

            user.Save();
            MessageBox.Show("TeamLead created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            ClearAddUserForm();
        }
    }
}