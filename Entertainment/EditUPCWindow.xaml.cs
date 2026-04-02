using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Entertainment
{
    public partial class EditUPCWindow : Window
    {
        public string UPC { get; set; } = "";
        public string SelectedMake { get; set; } = "";
        public string SelectedType { get; set; } = "";

        public EditUPCWindow()
        {
            InitializeComponent();
            Loaded += EditUPCWindow_Loaded;
        }

        private void EditUPCWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UPCDisplay.Text = UPC;
            LoadMakeOptions();
            LoadTypeOptions();
        }

        private void LoadMakeOptions()
        {
            // Load unique makes from inventory
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.InventoryItems != null)
            {
                var makes = mainWindow.InventoryItems
                    .Where(i => !string.IsNullOrWhiteSpace(i.Make))
                    .Select(i => i.Make)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToList();

                MakeBox.ItemsSource = makes;
            }
        }

        private void LoadTypeOptions()
        {
            // Load unique device types from inventory
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.InventoryItems != null)
            {
                var types = mainWindow.InventoryItems
                    .Where(i => !string.IsNullOrWhiteSpace(i.Type))
                    .Select(i => i.Type)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                TypeBox.ItemsSource = types;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedMake = MakeBox.Text?.Trim() ?? "";
            SelectedType = TypeBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(SelectedMake))
            {
                MessageBox.Show("Please enter a Make.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedType))
            {
                MessageBox.Show("Please enter a Device Type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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