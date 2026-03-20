using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Entertainment.Inventory;
using Entertainment.Users;

namespace Entertainment
{
    public partial class MainWindow : Window
    {
        private string? _selectedUPC;
        private string? _selectedUPCMake;
        private Dictionary<string, string?> _newUPCs = new();
        private List<InventoryItem> _currentSerials = new();
        private InventoryItem? _selectedSerialItem;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize Make dropdown for new UPC
            NewUPCMakeBox.ItemsSource = new[]
            {
                "Lenovo",
                "HP",
                "Asus",
                "Samsung",
                "Apple",
                "MSI",
                "CyberPower"
            };

            // Initialize Edit Make dropdown
            EditMakeComboBox.ItemsSource = new[]
            {
                "Lenovo",
                "HP",
                "Asus",
                "Samsung",
                "Apple",
                "MSI",
                "CyberPower"
            };

            // Initialize Status dropdown for new serial
            NewSerialStatusBox.ItemsSource = InventoryStatus.AllStatuses;
            NewSerialStatusBox.SelectedIndex = 0;

            // Initialize Change Status dropdown
            ChangeStatusComboBox.ItemsSource = InventoryStatus.AllStatuses;
            ChangeStatusComboBox.SelectedIndex = 0;

            // Initialize Device Type dropdown for new serial
            InitializeDeviceTypes();

            // Subscribe to serial grid selection
            SerialNumbersGrid.SelectionChanged += SerialNumbersGrid_SelectionChanged;

            // Save on window open
            Loaded += MainWindow_Loaded;
            // Save on window close
            Closing += MainWindow_Closing;
        }

        private void InitializeDeviceTypes()
        {
            // Common device types - can be customized
            var deviceTypes = new[]
            {
                "Laptop",
                "Desktop",
                "Monitor",
                "Tablet",
                "All-in-One",
                "Server",
                "Workstation",
                "Gaming PC",
                "Chromebook",
                "Other"
            };
            NewSerialDeviceTypeBox.ItemsSource = deviceTypes;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            
            // Set welcome message
            if (App.CurrentUser != null)
            {
                WelcomeText.Text = $"Welcome, {App.CurrentUser.FirstName}!";
                
                // Update created by text for new serial
                NewSerialCreatedBy.Text = $"Created by: {App.CurrentUser.Initials} | {DateTime.Now:MM/dd/yyyy HH:mm}";
            }
            
            // Load all UPCs initially
            LoadAllUPCs();
            
            App.SaveAll();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            App.SaveAll();
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            // Close main window first
            this.Close();
            
            // Clear current user
            App.CurrentUser = null;
            
            // Show login page
            var login = new LoginPage();
            Application.Current.MainWindow = login;
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

        private void LoadAllUPCs()
        {
            var store = InventoryStore.Load();
            
            // Get all unique UPCs from existing inventory
            var existingUPCs = store.InventoryItems
                .SelectMany(item => item.UPC ?? new List<string>())
                .Where(upc => !string.IsNullOrWhiteSpace(upc))
                .Distinct()
                .OrderBy(upc => upc)
                .ToList();

            // Also include any newly created UPCs
            var allUPCs = existingUPCs
                .Union(_newUPCs.Keys)
                .Distinct()
                .OrderBy(upc => upc)
                .ToList();

            UPCListBox.ItemsSource = allUPCs;
        }

        private void UPCSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = UPCSearchBox.Text?.Trim() ?? string.Empty;
            
            var store = InventoryStore.Load();
            
            if (string.IsNullOrEmpty(searchText))
            {
                // Show all UPCs when search is empty
                LoadAllUPCs();
                return;
            }

            // Find all unique UPCs that match the search
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
                
                // Get the make for this UPC
                _selectedUPCMake = GetMakeFromUPC(selectedUPC);
                SelectedUPCMake.Text = string.IsNullOrEmpty(_selectedUPCMake) ? "" : $"({_selectedUPCMake})";
                
                // Load all serial numbers for the selected UPC
                LoadSerialsForUPC(selectedUPC);
                
                // Clear serial search when UPC changes
                SerialSearchBox.Clear();
                
                // Hide edit panels when UPC changes
                EditMakePanel.Visibility = Visibility.Collapsed;
                ChangeStatusPanel.Visibility = Visibility.Collapsed;
                _selectedSerialItem = null;
            }
        }

        private void UPCListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Double-click to edit or view details could be added here
        }

        private void LoadSerialsForUPC(string upc)
        {
            var store = InventoryStore.Load();
            _currentSerials = store.InventoryItems
                .Where(i => i.UPC?.Contains(upc) == true)
                .OrderBy(i => i.SerialNumber)
                .ToList();
            
            SerialNumbersGrid.ItemsSource = _currentSerials;
        }

        private void SerialSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SerialSearchBox.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(searchText) && _selectedUPC != null)
            {
                // Show all serials for selected UPC
                LoadSerialsForUPC(_selectedUPC);
                return;
            }

            if (string.IsNullOrEmpty(_selectedUPC))
            {
                // Search across all serials if no UPC selected
                var store = InventoryStore.Load();
                var allSerials = store.InventoryItems
                    .Where(i => Matches(searchText, i.SerialNumber))
                    .OrderBy(i => i.SerialNumber)
                    .ToList();
                SerialNumbersGrid.ItemsSource = allSerials;
                return;
            }

            // Filter current serials by search text
            var filteredSerials = _currentSerials
                .Where(i => Matches(searchText, i.SerialNumber))
                .ToList();
            SerialNumbersGrid.ItemsSource = filteredSerials;
        }

        private void AddNewSerialButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedUPC))
            {
                MessageBox.Show("Please select a UPC first.", "No UPC Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Hide change status panel when adding new serial
            ChangeStatusPanel.Visibility = Visibility.Collapsed;
            _selectedSerialItem = null;

            // Show the new serial panel
            NewSerialPanel.Visibility = Visibility.Visible;
            // Only autofill Serial Number if something was typed in search box
            string searchText = SerialSearchBox.Text?.Trim() ?? string.Empty;
            NewSerialBox.Text = string.IsNullOrEmpty(searchText) ? string.Empty : searchText;
            NewSerialDeviceTypeBox.SelectedIndex = -1;
            NewSerialDeviceTypeBox.Text = string.Empty;
            NewSerialStatusBox.SelectedIndex = 0;
            
            // Update created by info
            if (App.CurrentUser != null)
            {
                NewSerialCreatedBy.Text = $"Created by: {App.CurrentUser.Initials} | {DateTime.Now:MM/dd/yyyy HH:mm}";
            }
            
            NewSerialBox.Focus();
            NewSerialBox.SelectAll();
        }

        private void SaveNewSerialButton_Click(object sender, RoutedEventArgs e)
        {
            string serialNumber = NewSerialBox.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(serialNumber))
            {
                MessageBox.Show("Serial Number is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewSerialBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(_selectedUPC))
            {
                MessageBox.Show("Please select a UPC first.", "No UPC Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var store = InventoryStore.Load();

            // Check for duplicate in inventory
            if (store.SerialNumberExists(serialNumber))
            {
                MessageBox.Show("A item with this serial number already exists in inventory.", 
                    "Duplicate Serial Number", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewSerialBox.Focus();
                return;
            }

            // Get Device Type value
            string? deviceType = null;
            if (NewSerialDeviceTypeBox.SelectedItem != null)
            {
                deviceType = NewSerialDeviceTypeBox.SelectedItem.ToString();
            }
            else if (!string.IsNullOrEmpty(NewSerialDeviceTypeBox.Text))
            {
                deviceType = NewSerialDeviceTypeBox.Text.Trim();
            }

            // Get Status value
            string? status = NewSerialStatusBox.SelectedItem as string;
            if (string.IsNullOrEmpty(status))
            {
                status = InventoryStatus.InStock;
            }

            // Get current user initials
            string? createdBy = App.CurrentUser?.Initials ?? "UNK";
            DateTime createdOn = DateTime.Now;

            // Create new inventory item
            // NOTE: Make is NOT autofilled - user must enter manually
            var newItem = new InventoryItem
            {
                SerialNumber = serialNumber.ToUpperInvariant(),
                UPC = new List<string> { _selectedUPC },
                Make = null,  // Don't autofill Make - user enters manually
                Type = deviceType,
                CreatedBy = createdBy,
                CreatedAt = createdOn,
                Status = status
            };

            store.InventoryItems.Add(newItem);
            store.Save();

            // Hide the panel
            NewSerialPanel.Visibility = Visibility.Collapsed;

            // Refresh the serial numbers list
            LoadSerialsForUPC(_selectedUPC);

            MessageBox.Show($"Serial number '{newItem.SerialNumber}' created successfully by {createdBy}.", 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            App.SaveAll();
        }

        private void CancelNewSerialButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the panel and clear fields
            NewSerialPanel.Visibility = Visibility.Collapsed;
            NewSerialBox.Text = string.Empty;
            NewSerialDeviceTypeBox.SelectedIndex = -1;
            NewSerialDeviceTypeBox.Text = string.Empty;
            NewSerialStatusBox.SelectedIndex = 0;
        }

        private void AddNewUPCButton_Click(object sender, RoutedEventArgs e)
        {
            // Show the new UPC panel
            NewUPCPanel.Visibility = Visibility.Visible;
            NewUPCBox.Text = UPCSearchBox.Text; // Pre-fill with search text
            
            // Force layout update
            UpdateLayout();
            
            NewUPCBox.Focus();
            NewUPCBox.SelectAll();
        }

        private void SaveNewUPCButton_Click(object sender, RoutedEventArgs e)
        {
            string newUPC = NewUPCBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(newUPC))
            {
                MessageBox.Show("UPC is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewUPCBox.Focus();
                return;
            }

            var store = InventoryStore.Load();

            // Check if UPC already exists
            bool upcExists = store.InventoryItems
                .Any(item => item.UPC?.Any(u => string.Equals(newUPC, u?.Trim(), StringComparison.OrdinalIgnoreCase)) == true);

            if (upcExists)
            {
                MessageBox.Show("This UPC already exists in inventory. Please select it from the list.", 
                    "UPC Exists", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get Make value
            string? makeValue = null;
            if (NewUPCMakeBox.SelectedItem != null)
            {
                makeValue = NewUPCMakeBox.SelectedItem.ToString();
            }
            else if (!string.IsNullOrEmpty(NewUPCMakeBox.Text))
            {
                makeValue = NewUPCMakeBox.Text.Trim();
            }

            // NOTE: Don't store Make in _newUPCs - Make should not autofill for serial numbers
            // Store null so GetMakeFromUPC won't return a cached Make value
            _newUPCs[newUPC] = null;

            // Hide the panel
            NewUPCPanel.Visibility = Visibility.Collapsed;
            
            // Clear fields
            NewUPCBox.Text = string.Empty;
            NewUPCMakeBox.SelectedIndex = -1;
            NewUPCMakeBox.Text = string.Empty;

            // Select the new UPC
            _selectedUPC = newUPC;
            _selectedUPCMake = makeValue;
            SelectedUPC.Text = newUPC;
            SelectedUPCMake.Text = string.IsNullOrEmpty(makeValue) ? "" : $"({makeValue})";

            // Refresh the UPC list
            UPCSearchBox_TextChanged(this, null);

            // Select the new UPC in the list
            UPCListBox.SelectedItem = newUPC;

            MessageBox.Show($"New UPC '{newUPC}' created successfully. You can now add serial numbers.", 
                "UPC Created", MessageBoxButton.OK, MessageBoxImage.Information);
            
            App.SaveAll();
        }

        private void CancelNewUPCButton_Click(object sender, RoutedEventArgs e)
        {
            NewUPCPanel.Visibility = Visibility.Collapsed;
            NewUPCBox.Text = string.Empty;
            NewUPCMakeBox.SelectedIndex = -1;
            NewUPCMakeBox.Text = string.Empty;
        }

        private string? GetMakeFromUPC(string upc)
        {
            return GetMakeFromUPC(upc, InventoryStore.Load());
        }

        private string? GetMakeFromUPC(string upc, InventoryStore store)
        {
            // First check if it's a newly created UPC
            if (_newUPCs.TryGetValue(upc, out var makeValue) && makeValue != null)
            {
                return makeValue;
            }

            // Otherwise check existing inventory
            return store.InventoryItems
                .FirstOrDefault(i => i.UPC?.Contains(upc) == true)?
                .Make;
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

        private void SerialNumbersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SerialNumbersGrid.SelectedItem is InventoryItem selectedItem)
            {
                _selectedSerialItem = selectedItem;
                // Show the change status panel when a serial is selected
                // But only if we're not already adding a new serial
                if (NewSerialPanel.Visibility != Visibility.Visible)
                {
                    ChangeStatusPanel.Visibility = Visibility.Visible;
                    SelectedSerialText.Text = selectedItem.SerialNumber;
                    ChangeStatusComboBox.SelectedItem = selectedItem.Status;
                }
            }
        }

        private void EditMakeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedUPC))
            {
                MessageBox.Show("Please select a UPC first.", "No UPC Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Show the edit make panel
            EditMakePanel.Visibility = Visibility.Visible;
            
            // Pre-fill with current make
            if (!string.IsNullOrEmpty(_selectedUPCMake))
            {
                EditMakeComboBox.SelectedItem = _selectedUPCMake;
                EditMakeComboBox.Text = _selectedUPCMake;
            }
            else
            {
                EditMakeComboBox.SelectedIndex = -1;
                EditMakeComboBox.Text = string.Empty;
            }
            
            EditMakeComboBox.Focus();
        }

        private void SaveMakeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedUPC))
            {
                MessageBox.Show("Please select a UPC first.", "No UPC Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get new make value
            string? newMake = null;
            if (EditMakeComboBox.SelectedItem != null)
            {
                newMake = EditMakeComboBox.SelectedItem.ToString();
            }
            else if (!string.IsNullOrEmpty(EditMakeComboBox.Text))
            {
                newMake = EditMakeComboBox.Text.Trim();
            }

            var store = InventoryStore.Load();

            // Update make for all inventory items with this UPC
            var itemsToUpdate = store.InventoryItems
                .Where(i => i.UPC?.Contains(_selectedUPC) == true)
                .ToList();

            if (itemsToUpdate.Count > 0)
            {
                foreach (var item in itemsToUpdate)
                {
                    item.Make = newMake;
                }
                store.Save();

                MessageBox.Show($"Make updated for {itemsToUpdate.Count} item(s) with UPC '{_selectedUPC}'.", 
                    "Make Updated", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh the serial numbers list to show updated make
                if (_selectedUPC != null)
                {
                    LoadSerialsForUPC(_selectedUPC);
                }
            }
            else
            {
                // Update in memory for newly created UPCs
                _newUPCs[_selectedUPC] = newMake;
            }

            // Update the display
            _selectedUPCMake = newMake;
            SelectedUPCMake.Text = string.IsNullOrEmpty(newMake) ? "" : $"({newMake})";

            // Hide the panel
            EditMakePanel.Visibility = Visibility.Collapsed;
            
            App.SaveAll();
        }

        private void CancelMakeButton_Click(object sender, RoutedEventArgs e)
        {
            EditMakePanel.Visibility = Visibility.Collapsed;
            EditMakeComboBox.SelectedIndex = -1;
            EditMakeComboBox.Text = string.Empty;
        }

        private void UpdateStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSerialItem == null)
            {
                MessageBox.Show("Please select a serial number first.", "No Serial Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string? newStatus = ChangeStatusComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(newStatus))
            {
                MessageBox.Show("Please select a status.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var store = InventoryStore.Load();
            var item = store.InventoryItems.FirstOrDefault(i => i.Id == _selectedSerialItem.Id);
            
            if (item != null)
            {
                string oldStatus = item.Status;
                item.Status = newStatus;
                store.Save();

                MessageBox.Show($"Status for '{item.SerialNumber}' changed from '{oldStatus}' to '{newStatus}'.", 
                    "Status Updated", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh the list
                if (_selectedUPC != null)
                {
                    LoadSerialsForUPC(_selectedUPC);
                }

                // Hide the panel
                ChangeStatusPanel.Visibility = Visibility.Collapsed;
                _selectedSerialItem = null;
                
                App.SaveAll();
            }
        }

        private void CancelStatusButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeStatusPanel.Visibility = Visibility.Collapsed;
            _selectedSerialItem = null;
            SerialNumbersGrid.SelectedItem = null;
        }
    }

    // Converter for inverse boolean to visibility
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
