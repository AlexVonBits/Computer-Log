using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BarcodeStandard;
using Entertainment.Inventory;
using Entertainment.Users;

namespace Entertainment;

public partial class InventoryVerification : Window, IComponentConnector
{
	private InventoryStore _inventoryStore;

	private List<InventoryItemViewModel> _currentItems;

	private string _currentUser;

	private string _sessionId;

	private List<string> _allUPCs = new List<string>();

	private int _currentQRIndex;

	private DispatcherTimer? _autoAdvanceTimer;

	internal Button ProfileButton;

	internal Button SignOutButton;

	internal TextBlock UserDisplayText;

	internal System.Windows.Controls.Image SessionQRCode;

	internal TextBlock SessionInfoText;

	internal Button PrevQRButton;

	internal CheckBox AutoAdvanceCheckBox;

	internal Button NextQRButton;

	internal TextBlock QRCounterText;

	internal TextBox BarcodeInputTextBox;

	internal CheckBox CrossReferenceCheckBox;

	internal StackPanel CrossReferencePanel;

	internal TextBox SerialNumberTextBox;

	internal TextBox UPCTextBox;

	internal ItemsControl ItemsListBox;

	internal TextBlock NoItemsMessage;

	internal Button ConfirmButton;

	internal Button RFIDIssueButton;

	internal Button WorkOnItButton;

	internal Button SkipButton;

	internal TextBlock StatusText;

	private bool _contentLoaded;

	public InventoryVerification(string userName = "Unknown")
	{
		InitializeComponent();
		_currentUser = userName;
		_sessionId = Guid.NewGuid().ToString("N").Substring(0, 8)
			.ToUpper();
		_inventoryStore = InventoryStore.Load();
		_currentItems = new List<InventoryItemViewModel>();
		base.Resources["StatusToColorConverter"] = new StatusToColorConverter();
		base.Resources["NullToVisibilityConverter"] = new NullToVisibilityConverter();
		ProfileButton.Click += ProfileButton_Click;
		SignOutButton.Click += SignOut_Click;
		UserDisplayText.Text = "Logged in as: " + userName;
		LoadAllUPCsForSlideshow();
		UpdateUI();
		BarcodeInputTextBox.Focus();
		base.Loaded += InventoryVerification_Loaded;
		base.Closing += InventoryVerification_Closing;
	}

	private void InventoryVerification_Loaded(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Maximized;
		App.SaveAll();
	}

	private void InventoryVerification_Closing(object? sender, CancelEventArgs e)
	{
		if (_autoAdvanceTimer != null)
		{
			_autoAdvanceTimer.Stop();
			_autoAdvanceTimer = null;
		}
		App.SaveAll();
	}

	private void BarcodeInputTextBox_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			string text = BarcodeInputTextBox.Text.Trim();
			if (!string.IsNullOrEmpty(text))
			{
				SearchByBarcode(text);
			}
		}
	}

	private void CrossReferenceCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (CrossReferenceCheckBox.IsChecked == true)
		{
			CrossReferencePanel.Visibility = Visibility.Visible;
			BarcodeInputTextBox.IsEnabled = false;
		}
		else
		{
			CrossReferencePanel.Visibility = Visibility.Collapsed;
			BarcodeInputTextBox.IsEnabled = true;
		}
	}

	private void SearchButton_Click(object sender, RoutedEventArgs e)
	{
		string text = SerialNumberTextBox.Text.Trim();
		string text2 = UPCTextBox.Text.Trim();
		if (!string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(text2))
		{
			SearchBySerialAndUPC(text, text2);
		}
		App.SaveAll();
	}

	private void SearchByBarcode(string barcode)
	{
		List<InventoryItem> items = _inventoryStore.FindByBarcode(barcode);
		DisplayItems(items);
		BarcodeInputTextBox.Clear();
		BarcodeInputTextBox.Focus();
	}

	private void SearchBySerialAndUPC(string serial, string upc)
	{
		List<InventoryItem> items = _inventoryStore.FindBySerialOrUPC(serial, upc);
		DisplayItems(items);
		SerialNumberTextBox.Clear();
		UPCTextBox.Clear();
		BarcodeInputTextBox.Focus();
	}

	private void DisplayItems(List<InventoryItem> items)
	{
		_currentItems = items.Select((InventoryItem i) => new InventoryItemViewModel(i)).ToList();
		ItemsListBox.ItemsSource = _currentItems;
		bool flag = _currentItems.Count > 0;
		NoItemsMessage.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		ConfirmButton.IsEnabled = flag;
		RFIDIssueButton.IsEnabled = flag;
		WorkOnItButton.IsEnabled = flag;
		SkipButton.IsEnabled = flag;
		if (flag)
		{
			StatusText.Text = $"Found {_currentItems.Count} item(s) matching the search";
		}
		else
		{
			StatusText.Text = "No items found with on-hand status (Display, In Stock, In Claims)";
		}
		UpdateUI();
		GenerateUPCQRCode(_currentItems);
	}

	private void GenerateUPCQRCode(List<InventoryItemViewModel> items)
	{
		if (items.Count == 0)
		{
			SessionQRCode.Source = null;
			SessionInfoText.Text = "No item selected";
			return;
		}
		List<string> list = items.SelectMany((InventoryItemViewModel i) => i.UPCList).Distinct().ToList();
		if (list.Count == 0)
		{
			SessionQRCode.Source = null;
			SessionInfoText.Text = "No UPC";
			return;
		}
		string text = list[0];
		try
		{
			using Barcode barcode = new Barcode();
			barcode.IncludeLabel = true;
			Bitmap bitmap = new Bitmap(barcode.Encode(TYPE.UPCA, text, Color.Black, Color.White, 300, 100));
			BitmapImage bitmapImage = new BitmapImage();
			using MemoryStream memoryStream = new MemoryStream();
			bitmap.Save(memoryStream, ImageFormat.Png);
			memoryStream.Position = 0L;
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memoryStream;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			SessionQRCode.Source = bitmapImage;
		}
		catch
		{
			SessionQRCode.Source = null;
			SessionInfoText.Text = "Invalid UPC: " + text;
			return;
		}
		string text2 = ((list.Count == 1) ? list[0] : $"{list[0]} (+{list.Count - 1} more)");
		SessionInfoText.Text = text2;
	}

	private void UpdateUI()
	{
	}

	private void ConfirmButton_Click(object sender, RoutedEventArgs e)
	{
		if (_currentItems.Count == 0)
		{
			return;
		}
		foreach (InventoryItemViewModel currentItem in _currentItems)
		{
			_inventoryStore.MarkVerified(currentItem.Id, _currentUser);
		}
		StatusText.Text = $"Confirmed {_currentItems.Count} item(s) as verified";
		ClearCurrentItems();
		App.SaveAll();
	}

	private void RFIDIssueButton_Click(object sender, RoutedEventArgs e)
	{
		if (_currentItems.Count == 0 || MessageBox.Show("Mark these items as having RFID issues?\n\nThe items will remain in their current status but the issue will be logged.", "RFID Issue", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
		{
			return;
		}
		foreach (InventoryItemViewModel currentItem in _currentItems)
		{
			_inventoryStore.UpdateItemStatus(currentItem.Id, currentItem.Status, _currentUser, "RFID Issue");
		}
		StatusText.Text = $"Marked {_currentItems.Count} item(s) with RFID issue";
		ClearCurrentItems();
		App.SaveAll();
	}

	private void WorkOnItButton_Click(object sender, RoutedEventArgs e)
	{
		if (_currentItems.Count == 0)
		{
			return;
		}
		WorkOnItDialog workOnItDialog = new WorkOnItDialog(_currentUser);
		workOnItDialog.Owner = this;
		if (workOnItDialog.ShowDialog() != true)
		{
			return;
		}
		string selectedStatus = workOnItDialog.SelectedStatus;
		string note = workOnItDialog.Note;
		foreach (InventoryItemViewModel currentItem in _currentItems)
		{
			_inventoryStore.UpdateItemStatus(currentItem.Id, selectedStatus, _currentUser, note);
		}
		StatusText.Text = $"Changed {_currentItems.Count} item(s) to '{selectedStatus}' - {note}";
		ClearCurrentItems();
		App.SaveAll();
	}

	private void SkipButton_Click(object sender, RoutedEventArgs e)
	{
		if (_currentItems.Count == 0)
		{
			return;
		}
		SkipNoteDialog skipNoteDialog = new SkipNoteDialog();
		skipNoteDialog.Owner = this;
		if (skipNoteDialog.ShowDialog() != true)
		{
			return;
		}
		string note = skipNoteDialog.Note;
		foreach (InventoryItemViewModel currentItem in _currentItems)
		{
			_inventoryStore.UpdateItemStatus(currentItem.Id, currentItem.Status, _currentUser, "Skipped: " + note);
		}
		StatusText.Text = $"Skipped {_currentItems.Count} item(s) - Reason: {note}";
		ClearCurrentItems();
		App.SaveAll();
	}

	private void ClearCurrentItems()
	{
		_currentItems.Clear();
		ItemsListBox.ItemsSource = null;
		NoItemsMessage.Visibility = Visibility.Visible;
		ConfirmButton.IsEnabled = false;
		RFIDIssueButton.IsEnabled = false;
		WorkOnItButton.IsEnabled = false;
		SkipButton.IsEnabled = false;
	}

	private void SignOut_Click(object sender, RoutedEventArgs e)
	{
		Close();
		App.CurrentUser = null;
		new LoginPage().Show();
	}

	private void ProfileButton_Click(object sender, RoutedEventArgs e)
	{
		if (App.CurrentUser != null)
		{
			ProfileWindow profileWindow = new ProfileWindow(App.CurrentUser);
			profileWindow.Owner = this;
			profileWindow.ShowDialog();
		}
	}

	private void LoadAllUPCsForSlideshow()
	{
		_allUPCs = (from u in _inventoryStore.InventoryItems.SelectMany((InventoryItem i) => i.UPC ?? new List<string>()).Distinct()
			orderby u
			select u).ToList();
		if (_allUPCs.Count > 0)
		{
			_currentQRIndex = 0;
			GenerateQRCodeForUPC(_allUPCs[_currentQRIndex]);
			UpdateQRCounter();
		}
		else
		{
			SessionQRCode.Source = null;
			SessionInfoText.Text = "No UPCs in inventory";
			QRCounterText.Text = "";
			PrevQRButton.IsEnabled = false;
			NextQRButton.IsEnabled = false;
		}
	}

	private void GenerateQRCodeForUPC(string upc)
	{
		if (string.IsNullOrEmpty(upc))
		{
			SessionQRCode.Source = null;
			SessionInfoText.Text = "No UPC";
			return;
		}
		try
		{
			using Barcode barcode = new Barcode();
			barcode.IncludeLabel = true;
			Bitmap bitmap = new Bitmap(barcode.Encode(TYPE.UPCA, upc, Color.Black, Color.White, 300, 100));
			BitmapImage bitmapImage = new BitmapImage();
			using MemoryStream memoryStream = new MemoryStream();
			bitmap.Save(memoryStream, ImageFormat.Png);
			memoryStream.Position = 0L;
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memoryStream;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			SessionQRCode.Source = bitmapImage;
		}
		catch
		{
			SessionQRCode.Source = null;
			SessionInfoText.Text = "Invalid UPC: " + upc;
			return;
		}
		SessionInfoText.Text = upc;
	}

	private void UpdateQRCounter()
	{
		if (_allUPCs.Count > 0)
		{
			QRCounterText.Text = $"{_currentQRIndex + 1} / {_allUPCs.Count}";
			PrevQRButton.IsEnabled = _currentQRIndex > 0;
			NextQRButton.IsEnabled = _currentQRIndex < _allUPCs.Count - 1;
		}
		else
		{
			QRCounterText.Text = "";
			PrevQRButton.IsEnabled = false;
			NextQRButton.IsEnabled = false;
		}
	}

	private void PrevQR_Click(object sender, RoutedEventArgs e)
	{
		if (_currentQRIndex > 0)
		{
			_currentQRIndex--;
			GenerateQRCodeForUPC(_allUPCs[_currentQRIndex]);
			UpdateQRCounter();
		}
	}

	private void NextQR_Click(object sender, RoutedEventArgs e)
	{
		if (_currentQRIndex < _allUPCs.Count - 1)
		{
			_currentQRIndex++;
			GenerateQRCodeForUPC(_allUPCs[_currentQRIndex]);
			UpdateQRCounter();
		}
	}

	private void AutoAdvanceCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (AutoAdvanceCheckBox.IsChecked == true)
		{
			_autoAdvanceTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(5.0)
			};
			_autoAdvanceTimer.Tick += AutoAdvanceTimer_Tick;
			_autoAdvanceTimer.Start();
		}
		else if (_autoAdvanceTimer != null)
		{
			_autoAdvanceTimer.Stop();
			_autoAdvanceTimer = null;
		}
	}

	private void AutoAdvanceTimer_Tick(object? sender, EventArgs e)
	{
		if (_allUPCs.Count > 0)
		{
			_currentQRIndex = (_currentQRIndex + 1) % _allUPCs.Count;
			GenerateQRCodeForUPC(_allUPCs[_currentQRIndex]);
			UpdateQRCounter();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.25.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Entertainment;V1.3.4.0;component/inventorylog/inventoryverification.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.25.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			ProfileButton = (Button)target;
			ProfileButton.Click += ProfileButton_Click;
			break;
		case 2:
			SignOutButton = (Button)target;
			SignOutButton.Click += SignOut_Click;
			break;
		case 3:
			UserDisplayText = (TextBlock)target;
			break;
		case 4:
			SessionQRCode = (System.Windows.Controls.Image)target;
			break;
		case 5:
			SessionInfoText = (TextBlock)target;
			break;
		case 6:
			PrevQRButton = (Button)target;
			PrevQRButton.Click += PrevQR_Click;
			break;
		case 7:
			AutoAdvanceCheckBox = (CheckBox)target;
			AutoAdvanceCheckBox.Checked += AutoAdvanceCheckBox_Changed;
			AutoAdvanceCheckBox.Unchecked += AutoAdvanceCheckBox_Changed;
			break;
		case 8:
			NextQRButton = (Button)target;
			NextQRButton.Click += NextQR_Click;
			break;
		case 9:
			QRCounterText = (TextBlock)target;
			break;
		case 10:
			BarcodeInputTextBox = (TextBox)target;
			BarcodeInputTextBox.KeyDown += BarcodeInputTextBox_KeyDown;
			break;
		case 11:
			CrossReferenceCheckBox = (CheckBox)target;
			CrossReferenceCheckBox.Checked += CrossReferenceCheckBox_Changed;
			CrossReferenceCheckBox.Unchecked += CrossReferenceCheckBox_Changed;
			break;
		case 12:
			CrossReferencePanel = (StackPanel)target;
			break;
		case 13:
			SerialNumberTextBox = (TextBox)target;
			break;
		case 14:
			UPCTextBox = (TextBox)target;
			break;
		case 15:
			((Button)target).Click += SearchButton_Click;
			break;
		case 16:
			ItemsListBox = (ItemsControl)target;
			break;
		case 17:
			NoItemsMessage = (TextBlock)target;
			break;
		case 18:
			ConfirmButton = (Button)target;
			ConfirmButton.Click += ConfirmButton_Click;
			break;
		case 19:
			RFIDIssueButton = (Button)target;
			RFIDIssueButton.Click += RFIDIssueButton_Click;
			break;
		case 20:
			WorkOnItButton = (Button)target;
			WorkOnItButton.Click += WorkOnItButton_Click;
			break;
		case 21:
			SkipButton = (Button)target;
			SkipButton.Click += SkipButton_Click;
			break;
		case 22:
			StatusText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
