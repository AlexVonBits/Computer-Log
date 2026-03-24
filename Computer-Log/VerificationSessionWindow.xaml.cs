using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using Entertainment.Inventory;
using BarcodeLib;

namespace Entertainment
{
    public partial class VerificationSessionWindow : Window
    {
        private InventoryStore _inventoryStore;
        private List<string> _allUPCs;
        private int _currentIndex;
        private string _currentUser;
        private Dictionary<string, string> _verificationNotes = new();
        private Dictionary<string, string> _verificationStatuses = new();
        private bool _isClosing = false;
        
        // Serial status options
        private readonly string[] SerialStatuses = 
            { InventoryStatus.InStock, InventoryStatus.Display, InventoryStatus.InClaims, 
              InventoryStatus.ClaimedOut, InventoryStatus.Sold, InventoryStatus.Missing };

        public VerificationSessionWindow(string userName = "Unknown")
        {
            InitializeComponent();
            
            _currentUser = userName;
            _inventoryStore = InventoryStore.Load();
            _allUPCs = new List<string>();
            _currentIndex = 0;
            
            Loaded += VerificationSessionWindow_Loaded;
            Closing += VerificationSessionWindow_Closing;
        }

        private void VerificationSessionWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Skip popup if we're already closing programmatically (e.g., after successful completion)
            if (_isClosing) return;
            
            // Show "Are you sure?" confirmation when user tries to close via X button
            var result = MessageBox.Show(
                "Are you sure you want to close this verification session?\n\n" +
                "Any unsaved progress will be lost.",
                "Confirm Close",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; // Cancel the close
            }
        }

        private void VerificationSessionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            LoadAllUPCs();
            ShowCurrentUPC();
            UpdateNavigationButtons();
            App.SaveAll();
        }

        private void LoadAllUPCs()
        {
            // Get all unique UPCs from inventory
            var upcSet = new HashSet<string>();
            
            // From inventory items
            foreach (var item in _inventoryStore.InventoryItems)
            {
                if (item.UPC != null)
                {
                    foreach (var upc in item.UPC)
                    {
                        if (!string.IsNullOrWhiteSpace(upc))
                        {
                            upcSet.Add(upc);
                        }
                    }
                }
            }
            
            // Also from UPCMetadata
            foreach (var upc in _inventoryStore.UPCMetadata.Keys)
            {
                if (!string.IsNullOrWhiteSpace(upc))
                {
                    upcSet.Add(upc);
                }
            }
            
            _allUPCs = upcSet.OrderBy(u => u).ToList();
            
            if (_allUPCs.Count == 0)
            {
                MessageBox.Show("No UPCs found in inventory to verify.", "No UPCs", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
                return;
            }
        }

        private List<string> GetLinkedUPCs(string upc)
        {
            // Check CrossReferences for linked UPCs
            if (_inventoryStore.CrossReferences.TryGetValue(upc, out var linked))
            {
                return linked.ToList();
            }
            
            // Also check reverse - see if other UPCs reference this one
            var reverseLinked = new List<string>();
            foreach (var kvp in _inventoryStore.CrossReferences)
            {
                if (kvp.Value.Contains(upc))
                {
                    reverseLinked.Add(kvp.Key);
                }
            }
            
            return reverseLinked;
        }

        private void ShowCurrentUPC()
        {
            if (_currentIndex >= _allUPCs.Count)
            {
                ShowCompletionMessage();
                return;
            }

            string currentUPC = _allUPCs[_currentIndex];
            
            // Get linked UPCs
            var linkedUPCs = GetLinkedUPCs(currentUPC);
            bool hasCrossRefs = linkedUPCs.Count > 0;
            
            // Update progress text
            ProgressText.Text = $"UPC {_currentIndex + 1} of {_allUPCs.Count}";
            
            // Display UPC
            UPCDisplayText.Text = currentUPC;
            
            // Get Make and Type (check metadata first, then inventory)
            string? make = null;
            string? type = null;
            
            if (_inventoryStore.UPCMetadata.TryGetValue(currentUPC, out var metadata))
            {
                make = metadata.Make;
                type = metadata.Type;
            }
            
            if (string.IsNullOrEmpty(make) || string.IsNullOrEmpty(type))
            {
                var item = _inventoryStore.InventoryItems
                    .FirstOrDefault(i => i.UPC?.Contains(currentUPC) == true);
                if (item != null)
                {
                    make ??= item.Make;
                    type ??= item.Type;
                }
            }
            
            MakeText.Text = make ?? "Unknown";
            TypeText.Text = type ?? "Unknown";
            
            // Calculate On Hand count: count of serials with status "claims" or "in stock"
            // For this UPC and all linked UPCs
            var upcsToCheck = new List<string> { currentUPC };
            if (hasCrossRefs)
            {
                upcsToCheck.AddRange(linkedUPCs);
            }
            
            int onHandCount = _inventoryStore.InventoryItems
                .Where(i => i.UPC != null && i.UPC.Any(u => upcsToCheck.Contains(u)))
                .Count(i => InventoryStatus.OnHandStatuses.Contains(i.Status));
            
            OnHandCountText.Text = onHandCount.ToString();
            
            // Display cross-reference info
            if (hasCrossRefs)
            {
                CrossRefText.Text = $"Cross-References: {string.Join(", ", linkedUPCs)}";
                CrossRefText.Visibility = Visibility.Visible;
            }
            else
            {
                CrossRefText.Visibility = Visibility.Collapsed;
            }
            
            // Generate barcode
            GenerateBarcode(currentUPC);
            
            // Load notes for this UPC (from previous entries if any)
            if (_verificationNotes.TryGetValue(currentUPC, out var savedNote))
            {
                UPCNotesTextBox.Text = savedNote;
            }
            else
            {
                UPCNotesTextBox.Text = "";
            }
            
            // Load serial numbers
            LoadSerialNumbers(upcsToCheck);
            
            // Restore verification status
            RestoreVerificationStatus(currentUPC);
        }

        private void GenerateBarcode(string upc)
        {
            try
            {
                using (BarcodeLib.Barcode bar = new BarcodeLib.Barcode())
                {
                    bar.IncludeLabel = true;
                    System.Drawing.Image barcodeImage = bar.Encode(BarcodeLib.TYPE.UPCA, upc, 
                        System.Drawing.Color.Black, System.Drawing.Color.White, 300, 100);
                    
                    var bitmap = new System.Drawing.Bitmap(barcodeImage);
                    
                    BitmapImage barcodeBitmap = new BitmapImage();
                    using (var memory = new MemoryStream())
                    {
                        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                        memory.Position = 0;
                        barcodeBitmap.BeginInit();
                        barcodeBitmap.StreamSource = memory;
                        barcodeBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        barcodeBitmap.EndInit();
                        barcodeBitmap.Freeze();
                        BarcodeImage.Source = barcodeBitmap;
                    }
                }
                BarcodeLabelText.Text = upc;
            }
            catch (Exception ex)
            {
                BarcodeImage.Source = null;
                BarcodeLabelText.Text = $"Invalid UPC: {ex.Message}";
            }
        }

        private void LoadSerialNumbers(List<string> upcsToCheck)
        {
            var serials = _inventoryStore.InventoryItems
                .Where(i => i.UPC != null && i.UPC.Any(u => upcsToCheck.Contains(u)))
                .OrderBy(i => i.SerialNumber)
                .Select(i => new SerialViewModel(i))
                .ToList();
            
            SerialNumbersItemsControl.ItemsSource = serials;
            
            NoSerialsMessage.Visibility = serials.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RestoreVerificationStatus(string upc)
        {
            // Clear previous selections
            NoIssuesRadio.IsChecked = false;
            IssueFixedRadio.IsChecked = false;
            IssueNotFixedRadio.IsChecked = false;
            ReasonTextBox.Text = "";
            ReasonPanel.Visibility = Visibility.Collapsed;
            
            // Restore saved status if any
            if (_verificationStatuses.TryGetValue(upc, out var status))
            {
                switch (status)
                {
                    case "No Issues":
                        NoIssuesRadio.IsChecked = true;
                        break;
                    case "Found Issue-Fixed":
                        IssueFixedRadio.IsChecked = true;
                        break;
                    case "Found Issue-Not Fixed":
                        IssueNotFixedRadio.IsChecked = true;
                        ReasonPanel.Visibility = Visibility.Visible;
                        if (_verificationNotes.TryGetValue(upc + "_reason", out var reason))
                        {
                            ReasonTextBox.Text = reason;
                        }
                        break;
                }
            }
            
            UpdateNextButton();
        }

        private void UpdateNavigationButtons()
        {
            PreviousButton.IsEnabled = _currentIndex > 0;
        }

        private void UpdateNextButton()
        {
            // Next is enabled only when a radio button is checked
            // and if "Found Issue-Not Fixed" is checked, reason must be filled
            bool hasRadioSelection = NoIssuesRadio.IsChecked == true || 
                                     IssueFixedRadio.IsChecked == true || 
                                     IssueNotFixedRadio.IsChecked == true;
            
            bool hasReason = true;
            if (IssueNotFixedRadio.IsChecked == true)
            {
                hasReason = !string.IsNullOrWhiteSpace(ReasonTextBox.Text);
            }
            
            NextButton.IsEnabled = hasRadioSelection && hasReason;
        }

        private void VerificationRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Show/hide reason panel based on selection
            if (IssueNotFixedRadio.IsChecked == true)
            {
                ReasonPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ReasonPanel.Visibility = Visibility.Collapsed;
            }
            
            // Save verification status
            string currentUPC = _allUPCs[_currentIndex];
            string status = "";
            
            if (NoIssuesRadio.IsChecked == true) status = "No Issues";
            else if (IssueFixedRadio.IsChecked == true) status = "Found Issue-Fixed";
            else if (IssueNotFixedRadio.IsChecked == true) status = "Found Issue-Not Fixed";
            
            if (!string.IsNullOrEmpty(status))
            {
                _verificationStatuses[currentUPC] = status;
            }
            
            UpdateNextButton();
        }

        private void ReasonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Save reason when typed
            string currentUPC = _allUPCs[_currentIndex];
            _verificationNotes[currentUPC + "_reason"] = ReasonTextBox.Text;
            
            UpdateNextButton();
        }

        private void UPCNotesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Save notes when typed
            string currentUPC = _allUPCs[_currentIndex];
            _verificationNotes[currentUPC] = UPCNotesTextBox.Text;
        }

        private void SerialStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
            {
                string itemId = comboBox.Tag?.ToString() ?? "";
                string newStatus = comboBox.SelectedItem.ToString() ?? "";
                
                if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(newStatus))
                {
                    // Update the item's status
                    var item = _inventoryStore.InventoryItems.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        item.Status = newStatus;
                        _inventoryStore.Save();
                        
                        // Refresh the on-hand count
                        RefreshOnHandCount();
                    }
                }
            }
        }

        private void RefreshOnHandCount()
        {
            string currentUPC = _allUPCs[_currentIndex];
            var linkedUPCs = GetLinkedUPCs(currentUPC);
            
            var upcsToCheck = new List<string> { currentUPC };
            if (linkedUPCs.Count > 0)
            {
                upcsToCheck.AddRange(linkedUPCs);
            }
            
            int onHandCount = _inventoryStore.InventoryItems
                .Where(i => i.UPC != null && i.UPC.Any(u => upcsToCheck.Contains(u)))
                .Count(i => InventoryStatus.OnHandStatuses.Contains(i.Status));
            
            OnHandCountText.Text = onHandCount.ToString();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                // Save current state
                SaveCurrentVerification();
                
                _currentIndex--;
                ShowCurrentUPC();
                UpdateNavigationButtons();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Save current verification
            SaveCurrentVerification();
            
            _currentIndex++;
            
            if (_currentIndex >= _allUPCs.Count)
            {
                ShowCompletionMessage();
            }
            else
            {
                ShowCurrentUPC();
                UpdateNavigationButtons();
            }
        }

        private void SaveCurrentVerification()
        {
            string currentUPC = _allUPCs[_currentIndex];
            
            // Save verification status
            if (NoIssuesRadio.IsChecked == true)
            {
                _verificationStatuses[currentUPC] = "No Issues";
            }
            else if (IssueFixedRadio.IsChecked == true)
            {
                _verificationStatuses[currentUPC] = "Found Issue-Fixed";
            }
            else if (IssueNotFixedRadio.IsChecked == true)
            {
                _verificationStatuses[currentUPC] = "Found Issue-Not Fixed";
                _verificationNotes[currentUPC + "_reason"] = ReasonTextBox.Text;
            }
            
            // Save notes
            _verificationNotes[currentUPC] = UPCNotesTextBox.Text;
            
            // Apply verification data to inventory items
            ApplyVerificationToItems(currentUPC);
            
            _inventoryStore.Save();
        }

        private void ApplyVerificationToItems(string upc)
        {
            // Get all items for this UPC
            var items = _inventoryStore.InventoryItems
                .Where(i => i.UPC?.Contains(upc) == true)
                .ToList();
            
            if (items.Count == 0) return;
            
            string status = _verificationStatuses.GetValueOrDefault(upc, "");
            string note = _verificationNotes.GetValueOrDefault(upc, "");
            string reason = _verificationNotes.GetValueOrDefault(upc + "_reason", "");
            
            string combinedNote = note;
            if (status == "Found Issue-Not Fixed" && !string.IsNullOrEmpty(reason))
            {
                combinedNote = string.IsNullOrEmpty(note) 
                    ? $"Issue Not Fixed: {reason}" 
                    : $"{note}\nIssue Not Fixed: {reason}";
            }
            else if (status == "Found Issue-Fixed")
            {
                combinedNote = string.IsNullOrEmpty(note) 
                    ? "Issue Found and Fixed" 
                    : $"{note}\nIssue Found and Fixed";
            }
            
            // Update all items for this UPC
            foreach (var item in items)
            {
                item.LastVerifiedAt = DateTime.Now;
                item.LastVerifiedBy = _currentUser;
                
                if (!string.IsNullOrEmpty(combinedNote))
                {
                    item.VerificationNote = combinedNote;
                }
            }
        }

        private void ShowCompletionMessage()
        {
            // Calculate summary
            int totalUPCs = _allUPCs.Count;
            int noIssues = _verificationStatuses.Count(s => s.Value == "No Issues");
            int fixedCount = _verificationStatuses.Count(s => s.Value == "Found Issue-Fixed");
            int notFixed = _verificationStatuses.Count(s => s.Value == "Found Issue-Not Fixed");
            
            string summary = $"Verification Session Complete!\n\n" +
                $"Total UPCs Verified: {totalUPCs}\n" +
                $"No Issues: {noIssues}\n" +
                $"Issues Fixed: {fixedCount}\n" +
                $"Issues Not Fixed: {notFixed}";
            
            MessageBox.Show(summary, "Verification Complete", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Save the completed verification session to persistent store
            SaveCompletedSession();
            
            // Mark as closing to prevent duplicate popup
            _isClosing = true;
            this.Close();
        }

        private void SaveCompletedSession()
        {
            try
            {
                var sessionStore = VerificationSessionStore.Load();
                
                // Get user initials from App.CurrentUser if available
                string userInitials = "UNK";
                if (App.CurrentUser != null)
                {
                    userInitials = App.CurrentUser.Initials ?? "UNK";
                }
                
                // Calculate summary counts from the verification session
                int issuesFixedCount = _verificationStatuses.Count(s => s.Value == "Found Issue-Fixed");
                int issuesNotFixedCount = _verificationStatuses.Count(s => s.Value == "Found Issue-Not Fixed");
                int noIssuesCount = _verificationStatuses.Count(s => s.Value == "No Issues");
                
                var session = new VerificationSession
                {
                    VerifierName = _currentUser,
                    VerifierInitials = userInitials,
                    CompletedAt = DateTime.Now,
                    Notes = new List<VerificationNote>(),
                    IssuesFixedCount = issuesFixedCount,
                    IssuesNotFixedCount = issuesNotFixedCount,
                    NoIssuesCount = noIssuesCount
                };
                
                // Add all notes from the verification session
                foreach (var kvp in _verificationNotes)
                {
                    string upc = kvp.Key;
                    
                    // Skip reason notes (they're combined with the main note)
                    if (upc.EndsWith("_reason")) continue;
                    
                    string note = kvp.Value;
                    if (!string.IsNullOrWhiteSpace(note))
                    {
                        session.Notes.Add(new VerificationNote
                        {
                            UPC = upc,
                            Note = note,
                            UserInitials = userInitials,
                            Timestamp = DateTime.Now
                        });
                    }
                }
                
                sessionStore.AddSession(session);
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt the user
                System.Diagnostics.Debug.WriteLine($"Error saving verification session: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel this verification session?\n\n" +
                "Progress will not be saved.",
                "Cancel Session",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
    }

    // ViewModel for serial number display
    public class SerialViewModel
    {
        public string ItemId { get; }
        public string SerialNumber { get; }
        public string Status { get; }
        public string[] StatusOptions { get; }
        public string SelectedStatus { get; set; }

        public SerialViewModel(InventoryItem item)
        {
            ItemId = item.Id;
            SerialNumber = item.SerialNumber;
            Status = item.Status;
            
            // All status options
            StatusOptions = new[] 
            { 
                InventoryStatus.InStock, 
                InventoryStatus.Display, 
                InventoryStatus.InClaims, 
                InventoryStatus.ClaimedOut, 
                InventoryStatus.Sold, 
                InventoryStatus.Missing 
            };
            
            // Set current status as selected
            SelectedStatus = item.Status;
        }
    }
}