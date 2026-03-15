using System.Configuration;
using System.Data;
using System.Windows;
using Entertainment.Security;
using Entertainment.Users;

namespace Entertainment
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //variable that stores the data loaded from users.json when the application starts
        public static User Store { get; private set; } = new User();

        protected override void OnStartup(StartupEventArgs e)
        {
            Store.Load();

            base.OnStartup(e);

            if (Store.TeamLeadNeedsSetup())
            {
                var setup = new TeamLeadSetupWindow(Store);
                setup.ShowDialog();
            }
            var login = new LoginPage();
            login.Show();
        }
    }

}
