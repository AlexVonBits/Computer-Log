using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Entertainment.Inventory;

namespace Entertainment;

public partial class InventoryVerificationWindow : Window
{
    // Note: XAML x:Name attributes create fields automatically - no manual property definitions needed

    public InventoryVerificationWindow()
    {
        InitializeComponent();
        Loaded += InventoryVerificationWindow_Loaded;
    }

    private void InventoryVerificationWindow_Loaded(object sender, RoutedEventArgs e)
    {
        bool isTeamLeadOrAdmin = App.CurrentUser != null && (App.CurrentUser.IsTeamLead || App.CurrentUser.IsAdmin);
        PurgeButtonContainer.Visibility = isTeamLeadOrAdmin ? Visibility.Visible : Visibility.Collapsed;
        LoadVerificationData();
    }

    private void LoadVerificationData()
    {
        var sessionStore = VerificationSessionStore.Load();
        string lastVerifier = "N/A";
        
        if (App.CurrentUser != null)
        {
            string fullName = null;
            if (!string.IsNullOrEmpty(App.CurrentUser.FirstName) || !string.IsNullOrEmpty(App.CurrentUser.LastName))
            {
                fullName = (App.CurrentUser.FirstName + " " + App.CurrentUser.LastName).Trim();
            }
            if (!string.IsNullOrEmpty(fullName))
            {
                lastVerifier = fullName;
            }
            else
            {
                string storedVerifier = sessionStore.GetLastVerifier();
                if (!string.IsNullOrEmpty(storedVerifier))
                    lastVerifier = storedVerifier;
            }
        }
        
        WhoVerifiedLastText.Text = lastVerifier;
        
        var lastDate = sessionStore.GetLastVerificationDate();
        LastDateVerifiedText.Text = lastDate.HasValue 
            ? lastDate.Value.ToString("MM/dd/yyyy HH:mm") 
            : "N/A";

        var lastSession = sessionStore.GetLastSession();
        if (lastSession != null)
        {
            IssuesFixedCount.Text = lastSession.IssuesFixedCount.ToString();
            IssuesNotFixedCount.Text = lastSession.IssuesNotFixedCount.ToString();
            NoIssuesCount.Text = lastSession.NoIssuesCount.ToString();
        }
        else
        {
            IssuesFixedCount.Text = "0";
            IssuesNotFixedCount.Text = "0";
            NoIssuesCount.Text = "0";
        }

        NotesDataGrid.ItemsSource = sessionStore.GetAllNotes();
    }

    private void StartVerificationButton_Click(object sender, RoutedEventArgs e)
    {
        var verificationWindow = new VerificationSessionWindow();
        verificationWindow.Closed += delegate { LoadVerificationData(); };
        verificationWindow.Show();
    }

    private void PurgeButton_Click(object sender, RoutedEventArgs e)
    {
        var store = InventoryStore.Load();
        var itemsToPurge = store.InventoryItems
            .Where(i => i.Status == "Sold" || i.Status == "Claimed Out")
            .ToList();

        if (itemsToPurge.Count == 0)
        {
            MessageBox.Show("No items with 'Sold' or 'Claimed Out' status to purge.", 
                "Nothing to Purge", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            return;
        }

        var result = MessageBox.Show(
            $"This will permanently delete {itemsToPurge.Count} item(s) with status 'Sold' or 'Claimed Out'.\n\n" +
            "A log file will be exported containing all deleted items.\n\n" +
            "Are you sure you want to continue?",
            "Confirm Purge", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

        if (result != MessageBoxResult.Yes) return;

        string logPath = ExportPurgeLog(itemsToPurge);
        
        foreach (var item in itemsToPurge)
            store.InventoryItems.Remove(item);
        
        store.Save();
        
        MessageBox.Show($"Successfully purged {itemsToPurge.Count} item(s).\n\nLog file saved to:\n{logPath}", 
            "Purge Complete", MessageBoxButton.OK, MessageBoxImage.Asterisk);
    }

    private string ExportPurgeLog(List<InventoryItem> deletedItems)
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Entertainment", "purge_logs");
        Directory.CreateDirectory(logDir);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string logFile = Path.Combine(logDir, $"purge_log_{timestamp}.json");

        var logData = deletedItems.Select(item => new
        {
            item.UPC,
            item.SerialNumber,
            item.Status,
            item.Make,
            item.Type,
            item.CreatedBy,
            item.CreatedAt,
            item.SoldBy,
            item.SoldAt,
            item.OnlinePick,
            DeletedAt = DateTime.Now,
            DeletedBy = App.CurrentUser?.Initials ?? "UNKNOWN"
        }).ToList();

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(logData, options);
        File.WriteAllText(logFile, json);
        
        return logFile;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}