using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Entertainment
{
    public partial class CrossReferenceWindow : Window
    {
        public List<string> LinkedUPCs { get; private set; } = new();
        
        private readonly HashSet<string> _selectedUPCs = new();
        private readonly List<string> _allAvailableUPCs;

        public CrossReferenceWindow(string selectedUPC, List<string> availableUPCs)
        {
            InitializeComponent();
            
            SelectedUPCText.Text = $"Selected UPC: {selectedUPC}";
            
            // Store all available UPCs for filtering
            _allAvailableUPCs = availableUPCs;
            
            // Populate the list with available UPCs
            UPCsListBox.ItemsSource = _allAvailableUPCs;
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(searchText))
            {
                // Show all UPCs when search is empty
                UPCsListBox.ItemsSource = _allAvailableUPCs;
            }
            else
            {
                // Filter by full UPC or last 4 digits
                var filteredUPCs = _allAvailableUPCs.Where(upc => 
                    upc.Contains(searchText) || 
                    (upc.Length >= 4 && upc.Substring(upc.Length - 4).Contains(searchText))
                ).ToList();
                
                UPCsListBox.ItemsSource = filteredUPCs;
            }
        }

        private void UPCCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is string upc)
            {
                _selectedUPCs.Add(upc);
            }
        }

        private void UPCCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is string upc)
            {
                _selectedUPCs.Remove(upc);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            LinkedUPCs = new List<string>(_selectedUPCs);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
