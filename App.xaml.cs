using System.Configuration;
using System.Data;
using System.Windows;
using Entertainment.Security;
using Entertainment.Users;
using Entertainment.Inventory;

namespace Entertainment
{
    public partial class App : Application
    {
        //variable that stores the data loaded from users.json when the application starts
        public static User Store { get; private set; } = new User();

        //currently logged-in user
        public static Associate? CurrentUser { get; set; }

        /// <summary>
        /// Central Save method that persists all data to JSON files.
        /// Saves both the User store and Inventory store.
        /// </summary>
        public static void SaveAll()
        {
            Store.Save();
            InventoryStore.Load().Save();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Store.Load();

            base.OnStartup(e);

            if (Store.TeamLeadNeedsSetup())
            {
                var setup = new TeamLeadSetupWindow(Store);
                if (setup.ShowDialog() == true)
                {
                    // TeamLead setup successful - show MainWindow
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    return;
                }
                else
                {
                    // Setup was cancelled - exit the app
                    Shutdown();
                    return;
                }
            }
            
            // Show login and keep it open until logged in
            var login = new LoginPage();
            login.Show();
        }
    }
}
