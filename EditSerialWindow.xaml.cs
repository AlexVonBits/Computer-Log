using System.Windows;
using Entertainment.Inventory;
using System.Linq;

namespace Entertainment
{
    public partial class EditSerialWindow : Window
    {
        public string? NewSerialNumber { get; private set; }
        public string? NewType { get; private set; }
        public InventoryItem? ItemToEdit { get; set; }

        public EditSerialWindow(InventoryItem item)
        {
            InitializeComponent();

            // Store the item being edited
            ItemToEdit = item;

            // Initialize Device Type dropdown
            TypeBox.ItemsSource = new[]
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

            // Initialize Status dropdown
            StatusBox.ItemsSource = InventoryStatus.AllStatuses;

            // Set current values
            SerialNumberBox.Text = item.SerialNumber;
            TypeBox.Text = item.Type;
            StatusBox.SelectedItem = item.Status;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SerialNumberBox.Text))
            {
                MessageBox.Show("Serial number cannot be empty.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewSerialNumber = SerialNumberBox.Text.Trim();
            NewType = TypeBox.Text?.Trim();
            
            // Update the inventory item with new values
            if (ItemToEdit != null)
            {
                ItemToEdit.SerialNumber = NewSerialNumber;
                if (!string.IsNullOrEmpty(NewType))
                {
                    ItemToEdit.Type = NewType;
                }
                if (StatusBox.SelectedItem is string newStatus)
                {
                    ItemToEdit.Status = newStatus;
                }
                
                // Save the changes
                var store = InventoryStore.Load();
                var item = store.InventoryItems.FirstOrDefault(i => i.Id == ItemToEdit.Id);
                if (item != null)
                {
                    item.SerialNumber = ItemToEdit.SerialNumber;
                    item.Type = ItemToEdit.Type;
                    item.Status = ItemToEdit.Status;
                    store.Save();
                }
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