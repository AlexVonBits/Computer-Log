using System.Windows;
using Entertainment.Inventory;

namespace Entertainment
{
    public partial class InventoryVerificationWindow : Window
    {
        public InventoryVerificationWindow()
        {
            InitializeComponent();
            
            // Load verification data when window opens
            Loaded += InventoryVerificationWindow_Loaded;
        }

        private void InventoryVerificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadVerificationData();
        }

        private void LoadVerificationData()
        {
            var sessionStore = VerificationSessionStore.Load();
            
            // Get last verifier name
            string? lastVerifier = sessionStore.GetLastVerifier();
            WhoVerifiedLastText.Text = lastVerifier ?? "N/A";
            
            // Get last verification date/time
            var lastDate = sessionStore.GetLastVerificationDate();
            if (lastDate.HasValue)
            {
                LastDateVerifiedText.Text = lastDate.Value.ToString("MM/dd/yyyy HH:mm");
            }
            else
            {
                LastDateVerifiedText.Text = "N/A";
            }
            
            // Load all notes from verification sessions
            var allNotes = sessionStore.GetAllNotes();
            NotesDataGrid.ItemsSource = allNotes;
        }

        private void StartVerificationButton_Click(object sender, RoutedEventArgs e)
        {
            var verificationSession = new VerificationSessionWindow();
            verificationSession.Show();
        }
    }
}