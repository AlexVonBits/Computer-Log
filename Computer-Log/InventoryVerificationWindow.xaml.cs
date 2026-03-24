using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Entertainment.Inventory;

namespace Entertainment
{
    public partial class InventoryVerificationWindow : Window
    {
        public InventoryVerificationWindow()
        {
            InitializeComponent();
            
            // Load verification data when window opens
            Loaded += InventoryVerificationWindow_Loaded;
        }

        private void InventoryVerificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Check if current user is TeamLead or Admin
            bool isTeamLeadOrAdmin = App.CurrentUser != null && 
                (App.CurrentUser.IsTeamLead || App.CurrentUser.IsAdmin);
            
            // Show/hide the purge button based on user role
            PurgeButtonContainer.Visibility = isTeamLeadOrAdmin ? Visibility.Visible : Visibility.Collapsed;
            
            LoadVerificationData();
        }

        private void LoadVerificationData()
        {
            var sessionStore = VerificationSessionStore.Load();
            
            // Get last verifier name - show current user's full name if available
            string displayName = "N/A";
            if (App.CurrentUser != null)
            {
                // Combine FirstName and LastName for full name
                string? fullName = null;
                if (!string.IsNullOrEmpty(App.CurrentUser.FirstName) || !string.IsNullOrEmpty(App.CurrentUser.LastName))
                {
                    fullName = $"{App.CurrentUser.FirstName} {App.CurrentUser.LastName}".Trim();
                }
                
                if (!string.IsNullOrEmpty(fullName))
                {
                    displayName = fullName;
                }
                else
                {
                    // Fall back to last verifier from session store
                    var lastVerifier = sessionStore.GetLastVerifier();
                    if (!string.IsNullOrEmpty(lastVerifier))
                    {
                        displayName = lastVerifier;
                    }
                }
            }
            WhoVerifiedLastText.Text = displayName;
            
            // Get last verification date/time
            var lastDate = sessionStore.GetLastVerificationDate();
            if (lastDate.HasValue)
            {
                LastDateVerifiedText.Text = lastDate.Value.ToString("MM/dd/yyyy HH:mm");
            }
            else
            {
                LastDateVerifiedText.Text = "N/A";
            }
            
            // Load verification summary from last session
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
            
            // Load all notes from verification sessions
            var allNotes = sessionStore.GetAllNotes();
            NotesDataGrid.ItemsSource = allNotes;
        }

        private void StartVerificationButton_Click(object sender, RoutedEventArgs e)
        {
            var verificationSession = new VerificationSessionWindow();
            verificationSession.Closed += (s, args) =>
            {
                // Auto-refresh when verification session completes
                LoadVerificationData();
            };
            verificationSession.Show();
        }

        private void PurgeButton_Click(object sender, RoutedEventArgs e)
        {
            var inventoryStore = InventoryStore.Load();
            
            // Find items with "Sold" or "Claimed Out" status
            var itemsToDelete = inventoryStore.InventoryItems
                .Where(i => i.Status == InventoryStatus.Sold || i.Status == InventoryStatus.ClaimedOut)
                .ToList();
            
            if (itemsToDelete.Count == 0)
            {
                MessageBox.Show("No items with 'Sold' or 'Claimed Out' status to purge.", 
                    "Nothing to Purge", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Show confirmation dialog
            string message = $"This will permanently delete {itemsToDelete.Count} item(s) with status 'Sold' or 'Claimed Out'.\n\n" +
                "A log file will be exported containing all deleted items.\n\n" +
                "Are you sure you want to continue?";
            
            var result = MessageBox.Show(message, "Confirm Purge", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes)
                return;
            
            // Export log file before deleting
            string logFilePath = ExportPurgeLog(itemsToDelete);
            
            // Delete the items from inventory
            foreach (var item in itemsToDelete)
            {
                inventoryStore.InventoryItems.Remove(item);
            }
            inventoryStore.Save();
            
            MessageBox.Show($"Successfully purged {itemsToDelete.Count} item(s).\n\nLog file saved to:\n{logFilePath}", 
                "Purge Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string ExportPurgeLog(List<InventoryItem> deletedItems)
        {
            // Create purge log directory
            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Entertainment",
                "purge_logs"
            );
            Directory.CreateDirectory(logDirectory);
            
            // Generate unique filename with timestamp (format: purge_log_2026-03-23_20-58-00.json)
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string logFilePath = Path.Combine(logDirectory, $"purge_log_{timestamp}.json");
            
            // Create log entry objects
            var logEntries = deletedItems.Select(item => new
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
            
            // Serialize and save to JSON
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(logEntries, options);
            File.WriteAllText(logFilePath, json);
            
            return logFilePath;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}