using System.Collections.Generic;
using System.Windows;

namespace Entertainment
{
    public partial class CrossReferenceWindow : Window
    {
        public List<string> LinkedUPCs { get; private set; } = new();
        
        private readonly HashSet<string> _selectedUPCs = new();

        public CrossReferenceWindow(string selectedUPC, List<string> availableUPCs)
        {
            InitializeComponent();
            
            SelectedUPCText.Text = $"Selected UPC: {selectedUPC}";
            
            // Populate the list with available UPCs
            UPCsListBox.ItemsSource = availableUPCs;
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
