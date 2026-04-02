using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Entertainment
{
    public partial class CrossReferenceWindow : Window
    {
        private List<string> _allUPCs = new List<string>();
        private List<string> _selectedUPCs = new List<string>();

        public string SelectedUPC { get; set; } = "";
        public List<string> LinkedUPCs { get; private set; } = new List<string>();

        public CrossReferenceWindow()
        {
            InitializeComponent();
            Loaded += CrossReferenceWindow_Loaded;
        }

        private void CrossReferenceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SelectedUPCText.Text = $"Selected UPC: {SelectedUPC}";
            LoadUPCs();
        }

        private void LoadUPCs()
        {
            // Load UPCs from the main inventory
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.InventoryItems != null)
            {
                _allUPCs = mainWindow.InventoryItems
                    .Where(i => i.UPC != null && !i.UPC.Contains(SelectedUPC))
                    .SelectMany(i => i.UPC ?? new List<string>())
                    .OrderBy(u => u)
                    .ToList();
            }

            UPCsListBox.ItemsSource = _allUPCs;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text?.ToLower() ?? "";
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                UPCsListBox.ItemsSource = _allUPCs;
            }
            else
            {
                UPCsListBox.ItemsSource = _allUPCs
                    .Where(u => u.ToLower().Contains(searchText))
                    .ToList();
            }
        }

        private void UPCCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is string upc)
            {
                if (!_selectedUPCs.Contains(upc))
                {
                    _selectedUPCs.Add(upc);
                }
            }
        }

        private void UPCCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is string upc)
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