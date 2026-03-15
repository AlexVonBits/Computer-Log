using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Entertainment.Security;
using Entertainment.Users;


namespace Entertainment
{
    /// <summary>
    /// Interaction logic for TeamLeadSetupWindow.xaml
    /// </summary>
    public partial class TeamLeadSetupWindow : Window
    {
        private readonly User _store;
        public TeamLeadSetupWindow(User store)
        {
            InitializeComponent();
            _store = store;
            
            // Save on window open
            Loaded += TeamLeadSetupWindow_Loaded;
            // Save on window close
            Closing += TeamLeadSetupWindow_Closing;
        }

        private void TeamLeadSetupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            App.SaveAll();
        }

        private void TeamLeadSetupWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            App.SaveAll();
        }

        private void TeamLeadSetupSubmit_Click(object sender, RoutedEventArgs e)
        {
            string username = UserNameIdTeamLeadBox.Text.Trim();
            string password = PasswordTeamLeadBox.Text;
            string firstName = FirstNameTeamLeadBox.Text;
            string lastName = LastNameTeamLeadBox.Text;


            username = username.ToUpperInvariant();
            password = password.ToUpperInvariant();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                MessageBox.Show("All fields are required.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var teamLead = App.Store.TeamLead;
            teamLead.Username = username;
            teamLead.FirstName = firstName;
            teamLead.LastName = lastName;


            teamLead.Initials = $"{firstName[0]}{lastName[0]}".ToUpperInvariant();

            teamLead.PasswordHash = PasswordSecurity.HashPassword(password);

            App.Store.Save();
            App.SaveAll();
            
            // Close this window and open MainWindow
            this.DialogResult = true;
            this.Close();
            
            // Open MainWindow
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
