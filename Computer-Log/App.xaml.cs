using System;
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
            try
            {
                Store.Save();
                var inventory = InventoryStore.Load();
                if (inventory != null)
                {
                    inventory.Save();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveAll error: {ex.Message}");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Set up global exception handlers
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                DispatcherUnhandledException += App_DispatcherUnhandledException;

                Store.Load();

                base.OnStartup(e);

                if (Store.TeamLeadNeedsSetup())
                {
                    var setup = new TeamLeadSetupWindow(Store);
                    if (setup.ShowDialog() == true)
                    {
                        // TeamLead setup successful - show MainWindow
                        // Setup window is already closed by ShowDialog returning true
                        var mainWindow = new MainWindow();
                        Application.Current.MainWindow = mainWindow;
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
                Application.Current.MainWindow = login;
                login.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Application Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"Unhandled error: {ex?.Message}\n\nStack trace:\n{ex?.StackTrace}", 
                "Critical Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"UI error: {e.Exception.Message}\n\nStack trace:\n{e.Exception.StackTrace}", 
                "Application Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
