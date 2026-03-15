using Entertainment.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Entertainment
{
    /// <summary>
    /// Interaction logic for ComputerLog.xaml
    /// </summary>
    public partial class ComputerLog : Window
    {
        private List<string> _serialNumbersToLogIn = new();
        private string? _selectedUPC;
        private Dictionary<string, (string? Make, string? Type)> _newUPCs = new();

        public ComputerLog()
        {
            InitializeComponent();

            LogOutReason.ItemsSource = new[]
            {
                "Sold",
                "In Claims",
                "Deleted",
                "Other"
            };

            LogOutSerial.Click += LogOutSerial_Click;
            
            // Save on window open
            Loaded += ComputerLog_Loaded;
            // Save on window close
            Closing += ComputerLog_Closing;
            
            // Wire up header buttons
            ProfileButton.Click += ProfileButton_Click;
            SignOutButton.Click += SignOut_Click;
        }

        private void ComputerLog_Loaded(object sender, RoutedEventArgs e)
        {
            App.SaveAll();
        }

        private void ComputerLog_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            App.SaveAll();
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void UPCSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = UPCSearchBox.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(searchText))
            {
                UPCListBox.ItemsSource = null;
                return;
            }

            var store = InventoryStore.Load();
            
            // Find all unique UPCs from existing inventory that match the search
            var existingUPCs = store.InventoryItems
                .SelectMany(item => item.UPC ?? new List<string>())
                .Where(upc => Matches(searchText, upc))
                .Distinct()
                .OrderBy(upc => upc)
                .ToList();

            // Also include any newly created UPCs that match
            var matchingNewUPCs = _newUPCs.Keys
                .Where(upc => Matches(searchText, upc))
                .ToList();

            // Combine and deduplicate
            var allMatchingUPCs = existingUPCs
                .Union(matchingNewUPCs)
                .OrderBy(upc => upc)
                .ToList();

            UPCListBox.ItemsSource = allMatchingUPCs;
        }

        private void UPCListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UPCListBox.SelectedItem is string selectedUPC)
            {
                _selectedUPC = selectedUPC;
                SelectedUPC.Text = selectedUPC;
            }
        }

        private void AddSerialButton_Click(object sender, RoutedEventArgs e)
        {
            string serialNumber = LogInSerialNumber.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(serialNumber))
            {
                MessageBox.Show("Serial Number is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                LogInSerialNumber.Focus();
                return;
            }

            // Check for duplicate in the list
            if (_serialNumbersToLogIn.Any(s => s.Equals(serialNumber, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This serial number is already in the list.", "Duplicate Serial", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for duplicate in inventory
            var store = InventoryStore.Load();
            if (store.SerialNumberExists(serialNumber))
            {
                MessageBox.Show("A item with this serial number already exists in inventory.", "Duplicate Serial Number", MessageBoxButton.OK, MessageBoxImage.Warning);
                LogInSerialNumber.Focus();
                return;
            }

            _serialNumbersToLogIn.Add(serialNumber.ToUpperInvariant());
            SerialNumbersListBox.ItemsSource = null;
            SerialNumbersListBox.ItemsSource = _serialNumbersToLogIn;
            
            LogInSerialNumber.Clear();
            LogInSerialNumber.Focus();
            
            App.SaveAll();
        }

        private void RemoveSerialButton_Click(object sender, RoutedEventArgs e)
        {
            if (SerialNumbersListBox.SelectedItem is string selectedSerial)
            {
                _serialNumbersToLogIn.Remove(selectedSerial);
                SerialNumbersListBox.ItemsSource = null;
                SerialNumbersListBox.ItemsSource = _serialNumbersToLogIn;
                
                App.SaveAll();
            }
            else
            {
                MessageBox.Show("Please select a serial number to remove.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LogInSerial_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrEmpty(_selectedUPC))
            {
                MessageBox.Show("Please select a UPC from the matching list.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                UPCSearchBox.Focus();
                return;
            }

            if (_serialNumbersToLogIn.Count == 0)
            {
                MessageBox.Show("Please add at least one serial number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                LogInSerialNumber.Focus();
                return;
            }

            if (App.CurrentUser == null)
            {
                MessageBox.Show("No user is logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var store = InventoryStore.Load();
            int successCount = 0;
            List<string> failedSerials = new();

            // Create inventory entries for each serial number
            foreach (string serialNumber in _serialNumbersToLogIn)
            {
                var newItem = new InventoryItem
                {
                    SerialNumber = serialNumber.ToUpperInvariant(),
                    UPC = new List<string> { _selectedUPC },
                    Make = GetMakeFromUPC(_selectedUPC, store),
                    Type = GetTypeFromUPC(_selectedUPC, store),
                    CreatedBy = App.CurrentUser.Initials,
                    CreatedAt = DateTime.Now,
                    Status = "Available"
                };

                store.InventoryItems.Add(newItem);
                successCount++;
            }

            store.Save();

            MessageBox.Show($"Successfully logged in {successCount} serial number(s) for UPC {_selectedUPC} by {App.CurrentUser.Initials}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Clear the form
            ClearLogInForm();
            
            App.SaveAll();
        }

        private string? GetMakeFromUPC(string upc, InventoryStore store)
        {
            // First check if it's a newly created UPC
            if (_newUPCs.TryGetValue(upc, out var newUpcInfo) && newUpcInfo.Make != null)
            {
                return newUpcInfo.Make;
            }

            // Otherwise check existing inventory
            return store.InventoryItems
                .FirstOrDefault(i => i.UPC?.Contains(upc) == true)?
                .Make;
        }

        private string? GetTypeFromUPC(string upc, InventoryStore store)
        {
            // First check if it's a newly created UPC
            if (_newUPCs.TryGetValue(upc, out var newUpcInfo) && newUpcInfo.Type != null)
            {
                return newUpcInfo.Type;
            }

            // Otherwise check existing inventory
            return store.InventoryItems
                .FirstOrDefault(i => i.UPC?.Contains(upc) == true)?
                .Type;
        }

        private void ClearLogInForm()
        {
            UPCSearchBox.Clear();
            SelectedUPC.Text = string.Empty;
            _selectedUPC = null;
            _serialNumbersToLogIn.Clear();
            SerialNumbersListBox.ItemsSource = null;
            LogInSerialNumber.Clear();
            UPCListBox.ItemsSource = null;
        }

        private void LogOutSerial_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsListBox.SelectedItem is not InventoryItem selectedItem)
            {
                MessageBox.Show("Please select an item from the search results first.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (App.CurrentUser == null)
            {
                MessageBox.Show("No user is logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string? reason = LogOutReason.SelectedItem as string;
            if (string.IsNullOrEmpty(reason))
            {
                MessageBox.Show("Please select a reason for logging out.", "Reason Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update the item with the current user's initials
            selectedItem.SoldBy = App.CurrentUser.Initials;
            selectedItem.SoldAt = DateTime.Now;
            selectedItem.Status = reason;

            // Save the inventory
            var store = InventoryStore.Load();
            var itemIndex = store.InventoryItems.FindIndex(i => i.Id == selectedItem.Id);
            if (itemIndex >= 0)
            {
                store.InventoryItems[itemIndex] = selectedItem;
                store.Save();
            }

            MessageBox.Show($"Serial number logged out successfully by {App.CurrentUser.Initials}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Clear the form
            SerialTextBox.Text = "Serial #:";
            UPCBox.Text = "UPC:";
            ResultsListBox.ItemsSource = null;
            LogOutReason.SelectedIndex = -1;
            
            App.SaveAll();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string serialInput = (SerialGetInput.Text ?? string.Empty).Trim().ToUpperInvariant();
            string upcInput = (UPCSearchInput.Text ?? string.Empty).Trim().ToUpperInvariant();

            if (serialInput.Length == 0 && upcInput.Length == 0)
            {
                MessageBox.Show("Please enter a serial number or UPC to search.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var items = Entertainment.Inventory.InventoryStore.Load().InventoryItems;

            var matches = items.Where(item =>
                Matches(serialInput, item.SerialNumber) ||
                (item.UPC != null && item.UPC.Any(upc => Matches(upcInput, upc)))
                ).ToList();

            if (matches.Count == 0)
            {
                MessageBox.Show("No matches found.", "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ResultsListBox.ItemsSource = matches;
            
            App.SaveAll();
        }
        private static bool Matches(string input, string? fieldValue)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(fieldValue))
            {
                return false;
            }
            string field = fieldValue.Trim().ToUpperInvariant();
            if (input.Length <= 4)
                return field.EndsWith(input);

            return field == input;
        }

        private void ShowItem(InventoryItem item)
        {
            SerialTextBox.Text = $"Serial #: {item.SerialNumber}";
            UPCBox.Text = $"UPC: {(item.UPC != null ? string.Join(", ", item.UPC) : string.Empty)}";
            
            // Show additional details as a message when item is selected
            string createdAtStr = item.CreatedAt.HasValue ? item.CreatedAt.Value.ToString("g") : "N/A";
            string soldAtStr = item.SoldAt.HasValue ? item.SoldAt.Value.ToString("g") : "N/A";
            
            string details = $"Make: {item.Make ?? "N/A"} | Type: {item.Type ?? "N/A"} | Status: {item.Status}\n" +
                           $"CreatedBy: {item.CreatedBy ?? "N/A"} | CreatedAt: {createdAtStr}\n" +
                           $"SoldBy: {item.SoldBy ?? "N/A"} | SoldAt: {soldAtStr}";
            
            MessageBox.Show(details, "Item Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsListBox.SelectedItem is InventoryItem selectedItem)
            {
                ShowItem(selectedItem);
            }
        }

        private void AddNewUPCButton_Click(object sender, RoutedEventArgs e)
        {
            // Show the new UPC panel
            NewUPCPanel.Visibility = Visibility.Visible;
            NewUPCBox.Text = UPCSearchBox.Text; // Pre-fill with search text
            NewUPCBox.Focus();
            
            // Select all text in the UPC box for easy replacement
            NewUPCBox.SelectAll();
        }

        private void SaveNewUPCButton_Click(object sender, RoutedEventArgs e)
        {
            string newUPC = NewUPCBox.Text?.Trim() ?? string.Empty;
            string make = NewUPCMakeBox.Text?.Trim() ?? string.Empty;
            string type = NewUPCTypeBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(newUPC))
            {
                MessageBox.Show("UPC is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewUPCBox.Focus();
                return;
            }

            var store = InventoryStore.Load();

            // Check if UPC already exists in inventory
            bool upcExists = store.InventoryItems
                .Any(item => item.UPC?.Any(u => u.Equals(newUPC, StringComparison.OrdinalIgnoreCase)) == true);

            if (upcExists)
            {
                MessageBox.Show("This UPC already exists in inventory. Please select it from the list.", "UPC Exists", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Store the new UPC info for later use when logging in serials
            _newUPCs[newUPC] = (string.IsNullOrEmpty(make) ? null : make, string.IsNullOrEmpty(type) ? null : type);

            // Hide the panel and clear fields
            NewUPCPanel.Visibility = Visibility.Collapsed;
            NewUPCBox.Clear();
            NewUPCMakeBox.Clear();
            NewUPCTypeBox.Clear();

            // Select the new UPC
            _selectedUPC = newUPC;
            SelectedUPC.Text = newUPC;

            // Refresh the UPC list to show the new UPC
            UPCSearchBox_TextChanged(this, null);

            MessageBox.Show($"New UPC '{newUPC}' created successfully. You can now log in serial numbers.", "UPC Created", MessageBoxButton.OK, MessageBoxImage.Information);
            
            App.SaveAll();
        }

        private void CancelNewUPCButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the panel and clear fields
            NewUPCPanel.Visibility = Visibility.Collapsed;
            NewUPCBox.Clear();
            NewUPCMakeBox.Clear();
            NewUPCTypeBox.Clear();
            
            App.SaveAll();
        }
    }
}
