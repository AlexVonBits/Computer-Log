using System.Windows;

namespace Entertainment
{
    public partial class EditUPCWindow : Window
    {
        public string? NewMake { get; private set; }
        public string? NewType { get; private set; }

        public EditUPCWindow(string upc, string? currentMake, string? currentType)
        {
            InitializeComponent();

            UPCDisplay.Text = upc;

            // Initialize Make dropdown
            MakeBox.ItemsSource = new[]
            {
                "Lenovo",
                "HP",
                "Asus",
                "Samsung",
                "Apple",
                "MSI",
                "CyberPower",
                "Dell",
                "Acer",
                "Toshiba",
                "Microsoft",
                "Other"
            };

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

            // Set current values
            if (!string.IsNullOrEmpty(currentMake))
            {
                MakeBox.Text = currentMake;
            }
            if (!string.IsNullOrEmpty(currentType))
            {
                TypeBox.Text = currentType;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            NewMake = MakeBox.Text?.Trim();
            NewType = TypeBox.Text?.Trim();
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
