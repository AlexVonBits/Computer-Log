using System;
using System.Windows;
using System.Windows.Controls;
using Entertainment.Security;
using Entertainment.Users;

namespace Entertainment;

public partial class TeamLeadSetupWindow : Window
{
    private readonly User _store;

    public TeamLeadSetupWindow(User store)
    {
        InitializeComponent();
        _store = store;
        Loaded += TeamLeadSetupWindow_Loaded;
        Closing += TeamLeadSetupWindow_Closing;
    }

    private void TeamLeadSetupWindow_Loaded(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Maximized;
        App.SaveAll();
    }

    private void TeamLeadSetupWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        App.SaveAll();
    }

    private void TeamLeadSetupSubmit_Click(object sender, RoutedEventArgs e)
    {
        string username = UserNameIdTeamLeadBox.Text.Trim().ToUpperInvariant();
        string password = PasswordTeamLeadBox.Text.ToUpperInvariant();
        string firstName = FirstNameTeamLeadBox.Text;
        string lastName = LastNameTeamLeadBox.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || 
            string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
        {
            MessageBox.Show("All fields are required.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            return;
        }

        Associate teamLead = App.Store.TeamLead;
        teamLead.Username = username;
        teamLead.FirstName = firstName;
        teamLead.LastName = lastName;
        teamLead.Initials = $"{firstName[0]}{lastName[0]}".ToUpperInvariant();
        teamLead.PasswordHash = PasswordSecurity.HashPassword(password);
        
        App.Store.Save();
        App.SaveAll();
        DialogResult = true;
        Close();
    }
}