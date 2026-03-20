using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Entertainment.Inventory;
using Entertainment.Users;

namespace Entertainment
{
    public partial class MainWindow : Window
    {
        private string? _selectedUPC;
        private string? _selectedUPCMake;
        private string? _selectedUPCType;
        private Dictionary<string, (string? Make, string? Type)> _newUPCs = new();
        private List<InventoryItem> _currentSerials = new();
        private InventoryItem? _selectedSerialItem;

        public bool CanDelete { get; private set; }

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

            // Initialize Device Type dropdown for new UPC
            NewUPCTypeBox.ItemsSource = new[]
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

            // Initialize Status dropdown for new serial
            NewSerialStatusBox.ItemsSource = InventoryStatus.AllStatuses;
            NewSerialStatusBox.SelectedIndex = 0;

            // Initialize Change Status dropdown
            ChangeStatusComboBox.ItemsSource = InventoryStatus.AllStatuses;
            ChangeStatusComboBox.SelectedIndex = 0;

            // Initialize Device Type dropdown for new serial
            // InitializeDeviceTypes();

            // Subscribe to serial grid selection
            SerialNumbersGrid.SelectionChanged += SerialNumbersGrid_SelectionChanged;

            // Save on window open
            Loaded += MainWindow_Loaded;
            // Save on window close
            Closing += MainWindow_Closing;
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
                
                // Set delete permission based on role
                CanDelete = App.CurrentUser.IsAdmin || App.CurrentUser.IsTeamLead;
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

            // Create objects with UPC, Make, Type for the DataGrid
            var upcItems = allUPCs.Select(upc => {
                string? make = null;
                string? type = null;
                
                // Check _newUPCs first
                if (_newUPCs.TryGetValue(upc, out var newUpcInfo))
                {
                    make = newUpcInfo.Make;
                    type = newUpcInfo.Type;
                }
                
                // If not found, check inventory
                if (make == null || type == null)
                {
                    var inventoryItem = store.InventoryItems.FirstOrDefault(i => i.UPC?.Contains(upc) == true);
                    if (inventoryItem != null)
                    {
                        make ??= inventoryItem.Make;
                        type ??= inventoryItem.Type;
                    }
                }
                
                return new { UPC = upc, Make = make ?? "", Type = type ?? "" };
            }).ToList();

            UPCListBox.ItemsSource = upcItems;
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

            // Create objects with UPC, Make, Type for the DataGrid
            var upcItems = allMatchingUPCs.Select(upc => {
                string? make = null;
                string? type = null;
                
                // Check _newUPCs first
                if (_newUPCs.TryGetValue(upc, out var newUpcInfo))
                {
                    make = newUpcInfo.Make;
                    type = newUpcInfo.Type;
                }
                
                // If not found, check inventory
                if (make == null || type == null)
                {
                    var inventoryItem = store.InventoryItems.FirstOrDefault(i => i.UPC?.Contains(upc) == true);
                    if (inventoryItem != null)
                    {
                        make ??= inventoryItem.Make;
                        type ??= inventoryItem.Type;
                    }
                }
                
                return new { UPC = upc, Make = make ?? "", Type = type ?? "" };
            }).ToList();

            UPCListBox.ItemsSource = upcItems;
        }

        private void UPCListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle the anonymous object format with UPC, Make, Type properties
            if (UPCListBox.SelectedItem != null)
            {
                // Use reflection to get the properties since it's an anonymous type
                var selectedItem = UPCListBox.SelectedItem;
                var upcProperty = selectedItem.GetType().GetProperty("UPC");
                var makeProperty = selectedItem.GetType().GetProperty("Make");
                var typeProperty = selectedItem.GetType().GetProperty("Type");
                
                string? selectedUPC = upcProperty?.GetValue(selectedItem) as string;
                string? selectedMake = makeProperty?.GetValue(selectedItem) as string;
                string? selectedType = typeProperty?.GetValue(selectedItem) as string;
                
                if (!string.IsNullOrEmpty(selectedUPC))
                {
                    _selectedUPC = selectedUPC;
                    _selectedUPCMake = selectedMake;
                    _selectedUPCType = selectedType;
                    SelectedUPC.Text = selectedUPC;
                    
                    // Display the make for this UPC
                    SelectedUPCMake.Text = string.IsNullOrEmpty(_selectedUPCMake) ? "" : $"({_selectedUPCMake})";
                    
                    // Load all serial numbers for the selected UPC
                    LoadSerialsForUPC(selectedUPC);
                    
                    // Clear serial search when UPC changes
                    SerialSearchBox.Clear();
                    
                    // Hide panels when UPC changes
                    ChangeStatusPanel.Visibility = Visibility.Collapsed;
                    _selectedSerialItem = null;
                }
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

            // Device Type is inherited from the selected UPC's type
            string? deviceType = _selectedUPCType;

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

            // Get Device Type value
            string? typeValue = null;
            if (NewUPCTypeBox.SelectedItem != null)
            {
                typeValue = NewUPCTypeBox.SelectedItem.ToString();
            }
            else if (!string.IsNullOrEmpty(NewUPCTypeBox.Text))
            {
                typeValue = NewUPCTypeBox.Text.Trim();
            }

            // Store both Make and Type in _newUPCs
            _newUPCs[newUPC] = (makeValue, typeValue);

            // Hide the panel
            NewUPCPanel.Visibility = Visibility.Collapsed;
            
            // Clear fields
            NewUPCBox.Text = string.Empty;
            NewUPCMakeBox.SelectedIndex = -1;
            NewUPCMakeBox.Text = string.Empty;
            NewUPCTypeBox.SelectedIndex = -1;
            NewUPCTypeBox.Text = string.Empty;

            // Select the new UPC
            _selectedUPC = newUPC;
            _selectedUPCMake = makeValue;
            _selectedUPCType = typeValue;
            SelectedUPC.Text = newUPC;
            SelectedUPCMake.Text = string.IsNullOrEmpty(makeValue) ? "" : $"({makeValue})";

            // Refresh the UPC list
            UPCSearchBox_TextChanged(this, null);

            // Select the new UPC in the list - find the matching anonymous object
            foreach (var item in UPCListBox.ItemsSource)
            {
                var upcProp = item.GetType().GetProperty("UPC");
                if (upcProp?.GetValue(item) as string == newUPC)
                {
                    UPCListBox.SelectedItem = item;
                    break;
                }
            }

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
            NewUPCTypeBox.SelectedIndex = -1;
            NewUPCTypeBox.Text = string.Empty;
        }

        private string? GetMakeFromUPC(string upc)
        {
            return GetMakeFromUPC(upc, InventoryStore.Load());
        }

        private string? GetMakeFromUPC(string upc, InventoryStore store)
        {
            // First check if it's a newly created UPC
            if (_newUPCs.TryGetValue(upc, out var upcInfo) && upcInfo.Make != null)
            {
                return upcInfo.Make;
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
                
                // Always start with Online Pick unchecked when selecting a new item
                OnlinePickBox.IsChecked = false;
            }
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
                
                // FIX ISSUE 1: Set SoldBy and SoldAt when status is changed to Sold
                if (newStatus == InventoryStatus.Sold)
                {
                    // Check if this is an online pick
                    if (OnlinePickBox.IsChecked == true)
                    {
                        item.SoldBy = "OGP";
                    }
                    else
                    {
                        item.SoldBy = App.CurrentUser?.Initials ?? "UNK";
                    }
                    item.SoldAt = DateTime.Now;
                }
                
                item.Status = newStatus;
                store.Save();

                // FIX ISSUE 2: Uncheck the OnlinePickBox after status update
                OnlinePickBox.IsChecked = false;

                MessageBox.Show($"Status for '{item.SerialNumber}' changed from '{oldStatus}' to '{newStatus}'.", 
                    "Status Updated", MessageBoxButton.OK, MessageBoxImage.Information);

                // FIX ISSUE 1 (real-time update): Clear selection first, then refresh to ensure display updates
                SerialNumbersGrid.SelectedItem = null;
                _selectedSerialItem = null;
                
                // Refresh the list
                if (_selectedUPC != null)
                {
                    LoadSerialsForUPC(_selectedUPC);
                }

                // Hide the panel
                ChangeStatusPanel.Visibility = Visibility.Collapsed;
                
                App.SaveAll();
            }
        }

        private void CancelStatusButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeStatusPanel.Visibility = Visibility.Collapsed;
            _selectedSerialItem = null;
            SerialNumbersGrid.SelectedItem = null;
        }

        private void DeleteUPCButton_Click(object sender, RoutedEventArgs e)
        {
            // Check role permission
            if (!CanDelete)
            {
                MessageBox.Show("You do not have permission to delete UPCs. Only admins and team leads can delete.", 
                    "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            object? selectedItem = null;
            string? upcToDelete = null;

            // Check if triggered from context menu (MenuItem) or button
            if (sender is System.Windows.Controls.MenuItem)
            {
                // Get from selected item in the grid
                selectedItem = UPCListBox.SelectedItem;
                if (selectedItem != null)
                {
                    var upcProperty = selectedItem.GetType().GetProperty("UPC");
                    upcToDelete = upcProperty?.GetValue(selectedItem) as string;
                }
            }
            else
            {
                // Get from button's data context
                var button = sender as System.Windows.Controls.Button;
                selectedItem = button?.DataContext;
                if (selectedItem != null)
                {
                    var upcProperty = selectedItem.GetType().GetProperty("UPC");
                    upcToDelete = upcProperty?.GetValue(selectedItem) as string;
                }
            }

            if (string.IsNullOrEmpty(upcToDelete)) return;

            // Confirm deletion
            var result = MessageBox.Show(
                $"Are you sure you want to delete UPC '{upcToDelete}'? This will also delete all serial numbers associated with this UPC.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var store = InventoryStore.Load();

            // Find all inventory items with this UPC and remove them
            var itemsToRemove = store.InventoryItems
                .Where(i => i.UPC?.Contains(upcToDelete) == true)
                .ToList();

            foreach (var item in itemsToRemove)
            {
                store.InventoryItems.Remove(item);
            }

            // Also remove from _newUPCs if it exists there
            _newUPCs.Remove(upcToDelete);

            store.Save();

            // Refresh the list
            LoadAllUPCs();

            // Clear selection if the deleted UPC was selected
            if (_selectedUPC == upcToDelete)
            {
                _selectedUPC = null;
                _selectedUPCType = null;
                SelectedUPC.Text = "";
                SelectedUPCMake.Text = "";
                SerialNumbersGrid.ItemsSource = null;
            }

            MessageBox.Show($"UPC '{upcToDelete}' and {itemsToRemove.Count} associated serial number(s) deleted.", 
                "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);

            App.SaveAll();
        }

        private void DeleteSerialButton_Click(object sender, RoutedEventArgs e)
        {
            // Check role permission
            if (!CanDelete)
            {
                MessageBox.Show("You do not have permission to delete serial numbers. Only admins and team leads can delete.", 
                    "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InventoryItem? itemToDelete = null;

            // Check if clicked from button or context menu
            if (sender is System.Windows.Controls.Button button)
            {
                itemToDelete = button.DataContext as InventoryItem;
            }
            else if (sender is System.Windows.Controls.MenuItem)
            {
                // Context menu - get from selected item
                itemToDelete = SerialNumbersGrid.SelectedItem as InventoryItem;
            }

            if (itemToDelete == null) return;

            // Confirm deletion
            var result = MessageBox.Show(
                $"Are you sure you want to delete serial number '{itemToDelete.SerialNumber}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var store = InventoryStore.Load();

            // Find and remove the item
            var item = store.InventoryItems.FirstOrDefault(i => i.Id == itemToDelete.Id);
            if (item != null)
            {
                store.InventoryItems.Remove(item);
                store.Save();

                // Refresh the list
                if (_selectedUPC != null)
                {
                    LoadSerialsForUPC(_selectedUPC);
                }

                MessageBox.Show($"Serial number '{itemToDelete.SerialNumber}' deleted.", 
                    "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);

                App.SaveAll();
            }
        }

        private void DeleteUPCContextMenu_Click(object sender, RoutedEventArgs e)
        {
            // Check role permission
            if (!CanDelete)
            {
                MessageBox.Show("You do not have permission to delete UPCs. Only admins and team leads can delete.", 
                    "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get UPC from selected item (context menu)
            var selectedItem = UPCListBox.SelectedItem;
            if (selectedItem == null) return;

            var upcProperty = selectedItem.GetType().GetProperty("UPC");
            string? upcToDelete = upcProperty?.GetValue(selectedItem) as string;

            if (string.IsNullOrEmpty(upcToDelete)) return;

            // Confirm deletion
            var result = MessageBox.Show(
                $"Are you sure you want to delete UPC '{upcToDelete}'? This will also delete all serial numbers associated with this UPC.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var store = InventoryStore.Load();

            // Find all inventory items with this UPC and remove them
            var itemsToRemove = store.InventoryItems
                .Where(i => i.UPC?.Contains(upcToDelete) == true)
                .ToList();

            foreach (var item in itemsToRemove)
            {
                store.InventoryItems.Remove(item);
            }

            // Also remove from _newUPCs if it exists there
            _newUPCs.Remove(upcToDelete);

            store.Save();

            // Refresh the list
            LoadAllUPCs();

            // Clear selection if the deleted UPC was selected
            if (_selectedUPC == upcToDelete)
            {
                _selectedUPC = null;
                _selectedUPCType = null;
                SelectedUPC.Text = "";
                SelectedUPCMake.Text = "";
                SerialNumbersGrid.ItemsSource = null;
            }

            MessageBox.Show($"UPC '{upcToDelete}' and {itemsToRemove.Count} associated serial number(s) deleted.", 
                "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);

            App.SaveAll();
        }

        private void LinkCrossReferences_Click(object sender, RoutedEventArgs e)
        {
            // Check role permission
            if (!CanDelete)
            {
                MessageBox.Show("You do not have permission to link cross-references. Only admins and team leads can perform this action.", 
                    "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get selected UPC
            var selectedItem = UPCListBox.SelectedItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select a UPC first.", "No UPC Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var upcProperty = selectedItem.GetType().GetProperty("UPC");
            string? selectedUPC = upcProperty?.GetValue(selectedItem) as string;

            if (string.IsNullOrEmpty(selectedUPC)) return;

            // Get all available UPCs for linking
            var store = InventoryStore.Load();
            var allUPCs = store.InventoryItems
                .SelectMany(item => item.UPC ?? new List<string>())
                .Where(upc => !string.IsNullOrWhiteSpace(upc))
                .Distinct()
                .OrderBy(upc => upc)
                .ToList();

            // Also include newly created UPCs
            allUPCs = allUPCs
                .Union(_newUPCs.Keys)
                .Distinct()
                .OrderBy(upc => upc)
                .ToList();

            // Remove the selected UPC from the list of available UPCs
            var availableUPCs = allUPCs.Where(u => u != selectedUPC).ToList();

            if (availableUPCs.Count == 0)
            {
                MessageBox.Show("No other UPCs available to link.", "No Options", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show a simple input dialog for cross-reference linking
            var crossRefWindow = new CrossReferenceWindow(selectedUPC, availableUPCs);
            crossRefWindow.Owner = this;
            
            if (crossRefWindow.ShowDialog() == true)
            {
                var linkedUPCs = crossRefWindow.LinkedUPCs;
                
                if (linkedUPCs.Count > 0)
                {
                    // Get all inventory items with the selected UPC
                    var itemsToUpdate = store.InventoryItems
                        .Where(i => i.UPC?.Contains(selectedUPC) == true)
                        .ToList();

                    // Add cross-reference UPCs to each item
                    foreach (var item in itemsToUpdate)
                    {
                        foreach (var linkedUpc in linkedUPCs)
                        {
                            if (item.UPC == null)
                            {
                                item.UPC = new List<string>();
                            }
                            if (!item.UPC.Contains(linkedUpc))
                            {
                                item.UPC.Add(linkedUpc);
                            }
                        }
                    }

                    store.Save();
                    
                    MessageBox.Show($"Successfully linked {linkedUPCs.Count} cross-reference(s) to UPC '{selectedUPC}'.", 
                        "Cross-References Linked", MessageBoxButton.OK, MessageBoxImage.Information);

                    App.SaveAll();
                }
            }
        }

        private List<string> GetLinkedUPCs(string upc)
        {
            var store = InventoryStore.Load();
            if (store.CrossReferences.TryGetValue(upc, out var linked))
            {
                return linked.ToList();
            }
            return new List<string>();
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
