using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Entertainment.Inventory;
using Entertainment.Users;

namespace Entertainment;

public partial class App : Application
{
	public static User Store { get; private set; } = new User();

	public static Associate? CurrentUser { get; set; }

	public static void SaveAll()
	{
		try
		{
			Store.Save();
			InventoryStore.Load()?.Save();
		}
		catch (Exception)
		{
		}
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		try
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			base.DispatcherUnhandledException += App_DispatcherUnhandledException;
			Store.Load();
			base.OnStartup(e);
			if (Store.TeamLeadNeedsSetup())
			{
				if (new TeamLeadSetupWindow(Store).ShowDialog() == true)
				{
					MainWindow mainWindow = new MainWindow();
					Application.Current.MainWindow = mainWindow;
					mainWindow.Show();
				}
				else
				{
					Shutdown();
				}
			}
			else
			{
				LoginPage loginPage = new LoginPage();
				Application.Current.MainWindow = loginPage;
				loginPage.Show();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Startup error: " + ex.Message + "\n\nStack trace:\n" + ex.StackTrace, "Application Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			Shutdown(1);
		}
	}

	private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Exception ex = e.ExceptionObject as Exception;
		MessageBox.Show("Unhandled error: " + ex?.Message + "\n\nStack trace:\n" + ex?.StackTrace, "Critical Error", MessageBoxButton.OK, MessageBoxImage.Hand);
	}

	private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		MessageBox.Show("UI error: " + e.Exception.Message + "\n\nStack trace:\n" + e.Exception.StackTrace, "Application Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		e.Handled = true;
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.25.0")]
	public static void Main()
	{
		new App().Run();
	}
}
