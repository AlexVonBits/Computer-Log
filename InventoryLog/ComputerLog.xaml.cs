using Entertainment.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Entertainment
{
    /// <summary>
    /// Interaction logic for ComputerLog.xaml
    /// </summary>
    public partial class ComputerLog : Window
    {
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
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string serialInput = (SerialGetInput.Text ?? string.Empty).Trim().ToUpperInvariant();
            string upcInput = (UPCGetInput.Text ?? string.Empty).Trim().ToUpperInvariant();

            if (serialInput.Length == 0 && upcInput.Length == 0)
            {
                MessageBox.Show("Please enter a serial number or UPC to search.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var items = Inventory.InventoryStore.Load().InventoryItems;

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
            SerialTextBox.Text = item.SerialNumber;
            UPCBox.Text = item.UPC != null ? string.Join(", ", item.UPC) : string.Empty;
        }

        private void ResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsListBox.SelectedItem is InventoryItem selectedItem)
            {
                ShowItem(selectedItem);
            }
        }
    }
}
