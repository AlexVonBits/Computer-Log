using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Entertainment.Inventory;
using BarcodeLib;

namespace Entertainment
{
    public partial class InventoryVerification : Window
    {
        private InventoryStore _inventoryStore;
        private List<InventoryItemViewModel> _currentItems;
        private string _currentUser;
        private string _sessionId;
        
        // QR Slideshow functionality
        private List<string> _allUPCs = new();
        private int _currentQRIndex = 0;
        private DispatcherTimer? _autoAdvanceTimer;

        public InventoryVerification(string userName = "Unknown")
        {
            InitializeComponent();
            
            _currentUser = userName;
            _sessionId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            _inventoryStore = InventoryStore.Load();
            _currentItems = new List<InventoryItemViewModel>();
            
            // Set up converters
            Resources["StatusToColorConverter"] = new StatusToColorConverter();
            Resources["NullToVisibilityConverter"] = new NullToVisibilityConverter();
            
            // Wire up header buttons
            ProfileButton.Click += ProfileButton_Click;
            SignOutButton.Click += SignOut_Click;
            
            UserDisplayText.Text = $"Logged in as: {userName}";
            
            // Load all UPCs for QR slideshow
            LoadAllUPCsForSlideshow();
            
            UpdateUI();
            
            BarcodeInputTextBox.Focus();
            
            // Save on window open
            Loaded += InventoryVerification_Loaded;
            // Save on window close
            Closing += InventoryVerification_Closing;
        }

        private void InventoryVerification_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            App.SaveAll();
        }

        private void InventoryVerification_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Stop auto-advance timer if running
            if (_autoAdvanceTimer != null)
            {
                _autoAdvanceTimer.Stop();
                _autoAdvanceTimer = null;
            }
            App.SaveAll();
        }

        private void BarcodeInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string barcode = BarcodeInputTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(barcode))
                {
                    SearchByBarcode(barcode);
                }
            }
        }

        private void CrossReferenceCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (CrossReferenceCheckBox.IsChecked == true)
            {
                CrossReferencePanel.Visibility = Visibility.Visible;
                BarcodeInputTextBox.IsEnabled = false;
            }
            else
            {
                CrossReferencePanel.Visibility = Visibility.Collapsed;
                BarcodeInputTextBox.IsEnabled = true;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string serial = SerialNumberTextBox.Text.Trim();
            string upc = UPCTextBox.Text.Trim();
            
            if (!string.IsNullOrEmpty(serial) || !string.IsNullOrEmpty(upc))
            {
                SearchBySerialAndUPC(serial, upc);
            }
            
            App.SaveAll();
        }

        private void SearchByBarcode(string barcode)
        {
            var items = _inventoryStore.FindByBarcode(barcode);
            DisplayItems(items);
            
            BarcodeInputTextBox.Clear();
            BarcodeInputTextBox.Focus();
        }

        private void SearchBySerialAndUPC(string serial, string upc)
        {
            var items = _inventoryStore.FindBySerialOrUPC(serial, upc);
            DisplayItems(items);
            
            SerialNumberTextBox.Clear();
            UPCTextBox.Clear();
            BarcodeInputTextBox.Focus();
        }

        private void DisplayItems(List<InventoryItem> items)
        {
            _currentItems = items.Select(i => new InventoryItemViewModel(i)).ToList();
            ItemsListBox.ItemsSource = _currentItems;
            
            bool hasItems = _currentItems.Count > 0;
            NoItemsMessage.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
            ConfirmButton.IsEnabled = hasItems;
            RFIDIssueButton.IsEnabled = hasItems;
            WorkOnItButton.IsEnabled = hasItems;
            SkipButton.IsEnabled = hasItems;
            
            if (hasItems)
            {
                StatusText.Text = $"Found {_currentItems.Count} item(s) matching the search";
            }
            else
            {
                StatusText.Text = "No items found with on-hand status (Display, In Stock, In Claims)";
            }
            
            UpdateUI();
            
            // Generate QR code with UPC of the item(s) being verified
            GenerateUPCQRCode(_currentItems);
        }

        private void GenerateUPCQRCode(List<InventoryItemViewModel> items)
        {
            if (items.Count == 0)
            {
                SessionQRCode.Source = null;
                SessionInfoText.Text = "No item selected";
                return;
            }
            
            // Get all UPCs from the items
            var allUPCs = items
                .SelectMany(i => i.UPCList)
                .Distinct()
                .ToList();
            
            if (allUPCs.Count == 0)
            {
                SessionQRCode.Source = null;
                SessionInfoText.Text = "No UPC";
                return;
            }
            
            // Use the first UPC for the barcode
            string upc = allUPCs[0];
            
            // Generate UPC-A barcode
            try
            {
                using (BarcodeLib.Barcode bar = new BarcodeLib.Barcode())
                {
                    bar.IncludeLabel = true;
                    System.Drawing.Image barcodeImage = bar.Encode(BarcodeLib.TYPE.UPCA, upc, System.Drawing.Color.Black, System.Drawing.Color.White, 300, 100);
                    
                    // Convert Image to Bitmap
                    var bitmap = new System.Drawing.Bitmap(barcodeImage);
                    
                    BitmapImage barcodeBitmap = new BitmapImage();
                    using (var memory = new System.IO.MemoryStream())
                    {
                        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                        memory.Position = 0;
                        barcodeBitmap.BeginInit();
                        barcodeBitmap.StreamSource = memory;
                        barcodeBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        barcodeBitmap.EndInit();
                        barcodeBitmap.Freeze();
                        SessionQRCode.Source = barcodeBitmap;
                    }
                }
            }
            catch
            {
                // If encoding fails, show error
                SessionQRCode.Source = null;
                SessionInfoText.Text = $"Invalid UPC: {upc}";
                return;
            }
            
            // Display the UPC(s) in the text
            string upcDisplay = allUPCs.Count == 1 ? allUPCs[0] : $"{allUPCs[0]} (+{allUPCs.Count - 1} more)";
            SessionInfoText.Text = upcDisplay;
        }

        private void UpdateUI()
        {
            // Additional UI updates can go here
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentItems.Count == 0) return;
            
            foreach (var item in _currentItems)
            {
                _inventoryStore.MarkVerified(item.Id, _currentUser);
            }
            
            StatusText.Text = $"Confirmed {_currentItems.Count} item(s) as verified";
            ClearCurrentItems();
            
            App.SaveAll();
        }

        private void RFIDIssueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentItems.Count == 0) return;
            
            var result = MessageBox.Show(
                "Mark these items as having RFID issues?\n\nThe items will remain in their current status but the issue will be logged.",
                "RFID Issue",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                foreach (var item in _currentItems)
                {
                    _inventoryStore.UpdateItemStatus(item.Id, item.Status, _currentUser, "RFID Issue");
                }
                
                StatusText.Text = $"Marked {_currentItems.Count} item(s) with RFID issue";
                ClearCurrentItems();
                
                App.SaveAll();
            }
        }

        private void WorkOnItButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentItems.Count == 0) return;
            
            // Show status change dialog
            var dialog = new WorkOnItDialog(_currentUser);
            dialog.Owner = this;
            
            if (dialog.ShowDialog() == true)
            {
                string newStatus = dialog.SelectedStatus;
                string note = dialog.Note;
                
                foreach (var item in _currentItems)
                {
                    _inventoryStore.UpdateItemStatus(item.Id, newStatus, _currentUser, note);
                }
                
                StatusText.Text = $"Changed {_currentItems.Count} item(s) to '{newStatus}' - {note}";
                ClearCurrentItems();
                
                App.SaveAll();
            }
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentItems.Count == 0) return;
            
            // Show note dialog
            var dialog = new SkipNoteDialog();
            dialog.Owner = this;
            
            if (dialog.ShowDialog() == true)
            {
                string note = dialog.Note;
                
                foreach (var item in _currentItems)
                {
                    _inventoryStore.UpdateItemStatus(item.Id, item.Status, _currentUser, $"Skipped: {note}");
                }
                
                StatusText.Text = $"Skipped {_currentItems.Count} item(s) - Reason: {note}";
                ClearCurrentItems();
                
                App.SaveAll();
            }
        }

        private void ClearCurrentItems()
        {
            _currentItems.Clear();
            ItemsListBox.ItemsSource = null;
            NoItemsMessage.Visibility = Visibility.Visible;
            ConfirmButton.IsEnabled = false;
            RFIDIssueButton.IsEnabled = false;
            WorkOnItButton.IsEnabled = false;
            SkipButton.IsEnabled = false;
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

        private void LoadAllUPCsForSlideshow()
        {
            // Get all unique UPCs from inventory
            _allUPCs = _inventoryStore.InventoryItems
                .SelectMany(i => i.UPC ?? new List<string>())
                .Distinct()
                .OrderBy(u => u)
                .ToList();
            
            // Show first QR code if available
            if (_allUPCs.Count > 0)
            {
                _currentQRIndex = 0;
                GenerateQRCodeForUPC(_allUPCs[_currentQRIndex]);
                UpdateQRCounter();
            }
            else
            {
                SessionQRCode.Source = null;
                SessionInfoText.Text = "No UPCs in inventory";
                QRCounterText.Text = "";
                PrevQRButton.IsEnabled = false;
                NextQRButton.IsEnabled = false;
            }
        }

        private void GenerateQRCodeForUPC(string upc)
        {
            if (string.IsNullOrEmpty(upc))
            {
                SessionQRCode.Source = null;
                SessionInfoText.Text = "No UPC";
                return;
            }
            
            // Generate UPC-A barcode
            try
            {
                using (BarcodeLib.Barcode bar = new BarcodeLib.Barcode())
                {
                    bar.IncludeLabel = true;
                    System.Drawing.Image barcodeImage = bar.Encode(BarcodeLib.TYPE.UPCA, upc, System.Drawing.Color.Black, System.Drawing.Color.White, 300, 100);
                    
                    // Convert Image to Bitmap
                    var bitmap = new System.Drawing.Bitmap(barcodeImage);
                    
                    BitmapImage barcodeBitmap = new BitmapImage();
                    using (var memory = new System.IO.MemoryStream())
                    {
                        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                        memory.Position = 0;
                        barcodeBitmap.BeginInit();
                        barcodeBitmap.StreamSource = memory;
                        barcodeBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        barcodeBitmap.EndInit();
                        barcodeBitmap.Freeze();
                        SessionQRCode.Source = barcodeBitmap;
                    }
                }
            }
            catch
            {
                // If encoding fails, show error
                SessionQRCode.Source = null;
                SessionInfoText.Text = $"Invalid UPC: {upc}";
                return;
            }
            
            SessionInfoText.Text = upc;
        }

        private void UpdateQRCounter()
        {
            if (_allUPCs.Count > 0)
            {
                QRCounterText.Text = $"{_currentQRIndex + 1} / {_allUPCs.Count}";
                PrevQRButton.IsEnabled = _currentQRIndex > 0;
                NextQRButton.IsEnabled = _currentQRIndex < _allUPCs.Count - 1;
            }
            else
            {
                QRCounterText.Text = "";
                PrevQRButton.IsEnabled = false;
                NextQRButton.IsEnabled = false;
            }
        }

        private void PrevQR_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQRIndex > 0)
            {
                _currentQRIndex--;
                GenerateQRCodeForUPC(_allUPCs[_currentQRIndex]);
                UpdateQRCounter();
            }
        }

        private void NextQR_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQRIndex < _allUPCs.Count - 1)
            {
                _currentQRIndex++;
                GenerateQRCodeForUPC(_allUPCs[_currentQRIndex]);
                UpdateQRCounter();
            }
        }

        private void AutoAdvanceCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (AutoAdvanceCheckBox.IsChecked == true)
            {
                // Start auto-advance timer (5 seconds)
                _autoAdvanceTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };
                _autoAdvanceTimer.Tick += AutoAdvanceTimer_Tick;
                _autoAdvanceTimer.Start();
            }
            else
            {
                // Stop auto-advance timer
                if (_autoAdvanceTimer != null)
                {
                    _autoAdvanceTimer.Stop();
                    _autoAdvanceTimer = null;
                }
            }
        }

        private void AutoAdvanceTimer_Tick(object? sender, EventArgs e)
        {
            if (_allUPCs.Count > 0)
            {
                _currentQRIndex = (_currentQRIndex + 1) % _allUPCs.Count;
                GenerateQRCodeForUPC(_allUPCs[_currentQRIndex]);
                UpdateQRCounter();
            }
        }
    }

    // ViewModel for displaying inventory items
    public class InventoryItemViewModel
    {
        private readonly InventoryItem _item;
        
        public InventoryItemViewModel(InventoryItem item)
        {
            _item = item;
        }
        
        public string Id => _item.Id;
        public string SerialNumber => _item.SerialNumber;
        public string Status => _item.Status;
        public string? Make => _item.Make;
        public string? Type => _item.Type;
        public DateTime? LastVerifiedAt => _item.LastVerifiedAt;
        public string? LastVerifiedBy => _item.LastVerifiedBy;
        
        public string UPCDisplay => _item.UPC.Count > 0 ? string.Join(", ", _item.UPC) : "None";
        
        public List<string> UPCList => _item.UPC.ToList();
        
        public string LastVerifiedDisplay
        {
            get
            {
                if (LastVerifiedAt.HasValue)
                {
                    return $"{LastVerifiedAt.Value:g} by {LastVerifiedBy ?? "Unknown"}";
                }
                return "";
            }
        }
    }

    // Dialog for Work On It - status change with note
    public class WorkOnItDialog : Window
    {
        private ComboBox _statusComboBox;
        private TextBox _noteTextBox;
        private string _currentUser;
        
        public string SelectedStatus { get; private set; } = "";
        public string Note { get; private set; } = "";
        
        public WorkOnItDialog(string userName)
        {
            _currentUser = userName;
            Title = "Work On It - Change Status";
            Width = 400;
            Height = 280;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            
            BuildUI();
        }
        
        private void BuildUI()
        {
            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Status label
            var statusLabel = new TextBlock 
            { 
                Text = "New Status:", 
                Foreground = Brushes.White, 
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(statusLabel, 0);
            grid.Children.Add(statusLabel);
            
            // Status combo box - includes all status options
            _statusComboBox = new ComboBox 
            { 
                Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            // Add all status options including the new ones
            _statusComboBox.Items.Add(InventoryStatus.InStock);
            _statusComboBox.Items.Add(InventoryStatus.Display);
            _statusComboBox.Items.Add(InventoryStatus.InClaims);
            _statusComboBox.Items.Add(InventoryStatus.ClaimedOut);
            _statusComboBox.Items.Add(InventoryStatus.Sold);
            _statusComboBox.Items.Add(InventoryStatus.Missing);
            _statusComboBox.SelectedIndex = 0;
            
            Grid.SetRow(_statusComboBox, 1);
            grid.Children.Add(_statusComboBox);
            
            // Note label
            var noteLabel = new TextBlock 
            { 
                Text = "Note:", 
                Foreground = Brushes.White, 
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(noteLabel, 2);
            grid.Children.Add(noteLabel);
            
            // Note text box
            _noteTextBox = new TextBox 
            { 
                Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Padding = new Thickness(8, 6, 8, 6),
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                CaretBrush = Brushes.White
            };
            Grid.SetRow(_noteTextBox, 3);
            grid.Children.Add(_noteTextBox);
            
            // Buttons
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };
            
            var cancelButton = new Button 
            { 
                Content = "Cancel", 
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            cancelButton.Click += (s, e) => DialogResult = false;
            
            var saveButton = new Button 
            { 
                Content = "Save", 
                Width = 80,
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50))
            };
            saveButton.Click += SaveButton_Click;
            
            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(saveButton);
            
            Grid.SetRow(buttonPanel, 4);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedStatus = _statusComboBox.SelectedItem?.ToString() ?? "";
            Note = _noteTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(Note))
            {
                MessageBox.Show("Please enter a note explaining the status change.", "Note Required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
        }
    }

    // Dialog for Skip - add note for why skipped
    public class SkipNoteDialog : Window
    {
        private TextBox _noteTextBox;
        
        public string Note { get; private set; } = "";
        
        public SkipNoteDialog()
        {
            Title = "Skip Item - Add Note";
            Width = 400;
            Height = 220;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            
            BuildUI();
        }
        
        private void BuildUI()
        {
            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Explanation
            var infoLabel = new TextBlock 
            { 
                Text = "Why are you skipping this item?", 
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)), 
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(infoLabel, 0);
            grid.Children.Add(infoLabel);
            
            // Note text box
            _noteTextBox = new TextBox 
            { 
                Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Padding = new Thickness(8, 6, 8, 6),
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                CaretBrush = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(_noteTextBox, 1);
            grid.Children.Add(_noteTextBox);
            
            // Buttons
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            var cancelButton = new Button 
            { 
                Content = "Cancel", 
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            cancelButton.Click += (s, e) => DialogResult = false;
            
            var skipButton = new Button 
            { 
                Content = "Skip Item", 
                Width = 100,
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(198, 40, 40))
            };
            skipButton.Click += SkipButton_Click;
            
            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(skipButton);
            
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }
        
        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            Note = _noteTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(Note))
            {
                MessageBox.Show("Please enter a note explaining why you're skipping this item.", "Note Required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
        }
    }

    // Status color converter
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string ?? "";
            return status switch
            {
                "In Stock" => new SolidColorBrush(Color.FromRgb(0, 113, 206)),  // Walmart Blue #0071CE
                "Display" => new SolidColorBrush(Color.FromRgb(0, 128, 128)),   // Teal
                "In Claims" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                "Claimed Out" => new SolidColorBrush(Color.FromRgb(121, 85, 72)), // Brown - distinct
                "Sold" => new SolidColorBrush(Color.FromRgb(46, 125, 50)),      // Green
                "Missing" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),  // Purple - distinct
                "Deleted" => new SolidColorBrush(Color.FromRgb(198, 40, 40)),   // Red
                "Clearance" => new SolidColorBrush(Color.FromRgb(218, 165, 32)), // Goldenrod
                _ => new SolidColorBrush(Color.FromRgb(97, 97, 97))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Null to visibility converter
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}