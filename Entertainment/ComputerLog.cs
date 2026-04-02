using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Entertainment.Inventory;

namespace Entertainment;

public partial class ComputerLog : Window, IComponentConnector
{
	private List<string> _serialNumbersToLogIn = new List<string>();

	private string? _selectedUPC;

	private InventoryItem? _selectedInventoryItem;

	internal Button ProfileButton;

	internal Button SignOutButton;

	internal TextBox UPCSearchBox;

	internal ListBox UPCListBox;

	internal Button AddNewUPCButton;

	internal TextBlock SelectedUPC;

	internal TextBlock SelectedUPCInfo;

	internal Border NewUPCPanel;

	internal TextBox NewUPCBox;

	internal TextBox NewUPCMakeBox;

	internal TextBox NewUPCTypeBox;

	internal Button SaveNewUPCButton;

	internal Button CancelNewUPCButton;

	internal TextBox LogInSerialNumber;

	internal ComboBox StatusComboBox;

	internal Button AddSerialButton;

	internal ListBox SerialNumbersListBox;

	internal Button RemoveSerialButton;

	internal TextBlock SelectedSerialDisplay;

	internal ComboBox UpdateStatusComboBox;

	internal CheckBox OnlinePickCheckBox;

	internal Button UpdateStatusButton;

	internal TextBox SerialGetInput;

	internal TextBox UPCSearchInput;

	internal Button Search;

	internal TextBlock UPCBox;

	internal TextBlock SerialTextBox;

	internal ListBox ResultsListBox;

	internal ComboBox LogOutReason;

	internal Button LogOutSerial;

	internal CheckBox OGPCheck;

	private bool _contentLoaded;

	public ComputerLog()
	{
		InitializeComponent();
		LogOutReason.ItemsSource = new string[4] { "Sold", "In Claims", "Deleted", "Other" };
		StatusComboBox.ItemsSource = InventoryStatus.AllStatuses;
		StatusComboBox.SelectedIndex = 0;
		UpdateStatusComboBox.ItemsSource = InventoryStatus.AllStatuses;
		UpdateStatusComboBox.SelectedIndex = 0;
		base.Loaded += ComputerLog_Loaded;
		base.Closing += ComputerLog_Closing;
	}

	private void ComputerLog_Loaded(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Maximized;
		InventoryStore.Load().MigrateTypesFromUPC();
		App.SaveAll();
	}

	private void ComputerLog_Closing(object? sender, CancelEventArgs e)
	{
		App.SaveAll();
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
			ProfileWindow profileWindow = new ProfileWindow();
			profileWindow.Owner = this;
			profileWindow.ShowDialog();
		}
	}

	private void UPCSearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		string searchText = UPCSearchBox.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(searchText))
		{
			UPCListBox.ItemsSource = null;
			return;
		}
		InventoryStore store = InventoryStore.Load();
		List<string> first = (from upc in (from upc in store.InventoryItems.SelectMany((InventoryItem item) => item.UPC ?? new List<string>())
				where Matches(searchText, upc)
				select upc).Distinct()
			orderby upc
			select upc).ToList();
		List<string> second = store.UPCMetadata.Keys.Where((string upc) => Matches(searchText, upc)).ToList();
		var itemsSource = (from upc in first.Union(second)
			orderby upc
			select upc).ToList().Select(delegate(string upc)
		{
			string text = null;
			string text2 = null;
			if (store.UPCMetadata.TryGetValue(upc, out UPCMetadata value))
			{
				text = value.Make;
				text2 = value.Type;
			}
			if (text == null || text2 == null)
			{
				InventoryItem inventoryItem = store.InventoryItems.FirstOrDefault((InventoryItem i) => i.UPC?.Contains(upc) ?? false);
				if (inventoryItem != null)
				{
					if (text == null)
					{
						text = inventoryItem.Make;
					}
					if (text2 == null)
					{
						text2 = inventoryItem.Type;
					}
				}
			}
			return new
			{
				UPC = upc,
				Make = (text ?? ""),
				Type = (text2 ?? "")
			};
		}).ToList();
		UPCListBox.ItemsSource = itemsSource;
	}

	private void UPCListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (UPCListBox.SelectedItem is string text)
		{
			_selectedUPC = text;
			SelectedUPC.Text = text;
			InventoryStore store = InventoryStore.Load();
			string makeFromUPC = GetMakeFromUPC(text, store);
			string typeFromUPC = GetTypeFromUPC(text, store);
			if (!string.IsNullOrEmpty(makeFromUPC) || !string.IsNullOrEmpty(typeFromUPC))
			{
				List<string> list = new List<string>();
				if (!string.IsNullOrEmpty(makeFromUPC))
				{
					list.Add("Make: " + makeFromUPC);
				}
				if (!string.IsNullOrEmpty(typeFromUPC))
				{
					list.Add("Type: " + typeFromUPC);
				}
				SelectedUPCInfo.Text = string.Join(" | ", list);
			}
			else
			{
				SelectedUPCInfo.Text = "No Make/Type info";
			}
		}
		else
		{
			if (UPCListBox.SelectedItem == null)
			{
				return;
			}
			object selectedItem = UPCListBox.SelectedItem;
			PropertyInfo property = selectedItem.GetType().GetProperty("UPC");
			PropertyInfo property2 = selectedItem.GetType().GetProperty("Make");
			PropertyInfo property3 = selectedItem.GetType().GetProperty("Type");
			if (!(property != null))
			{
				return;
			}
			_selectedUPC = property.GetValue(selectedItem) as string;
			SelectedUPC.Text = _selectedUPC ?? string.Empty;
			string text2 = property2?.GetValue(selectedItem) as string;
			string text3 = property3?.GetValue(selectedItem) as string;
			if (!string.IsNullOrEmpty(text2) || !string.IsNullOrEmpty(text3))
			{
				List<string> list2 = new List<string>();
				if (!string.IsNullOrEmpty(text2))
				{
					list2.Add("Make: " + text2);
				}
				if (!string.IsNullOrEmpty(text3))
				{
					list2.Add("Type: " + text3);
				}
				SelectedUPCInfo.Text = string.Join(" | ", list2);
			}
			else
			{
				SelectedUPCInfo.Text = "No Make/Type info";
			}
		}
	}

	private void AddSerialButton_Click(object sender, RoutedEventArgs e)
	{
		string serialNumber = LogInSerialNumber.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(serialNumber))
		{
			MessageBox.Show("Serial Number is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			LogInSerialNumber.Focus();
			return;
		}
		if (_serialNumbersToLogIn.Any((string s) => s.Equals(serialNumber, StringComparison.OrdinalIgnoreCase)))
		{
			MessageBox.Show("This serial number is already in the list.", "Duplicate Serial", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		if (InventoryStore.Load().SerialNumberExists(serialNumber))
		{
			MessageBox.Show("A item with this serial number already exists in inventory.", "Duplicate Serial Number", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			LogInSerialNumber.Focus();
			return;
		}
		_serialNumbersToLogIn.Add(serialNumber.ToUpperInvariant());
		SerialNumbersListBox.ItemsSource = null;
		SerialNumbersListBox.ItemsSource = _serialNumbersToLogIn;
		SerialNumbersListBox.Items.Refresh();
		LogInSerialNumber.Clear();
		LogInSerialNumber.Focus();
		App.SaveAll();
	}

	private void RemoveSerialButton_Click(object sender, RoutedEventArgs e)
	{
		if (SerialNumbersListBox.SelectedItem is string item)
		{
			_serialNumbersToLogIn.Remove(item);
			SerialNumbersListBox.ItemsSource = null;
			SerialNumbersListBox.ItemsSource = _serialNumbersToLogIn;
			App.SaveAll();
		}
		else
		{
			MessageBox.Show("Please select a serial number to remove.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
	}

	private void LogInSerial_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(_selectedUPC))
		{
			MessageBox.Show("Please select a UPC from the matching list.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			UPCSearchBox.Focus();
			return;
		}
		if (_serialNumbersToLogIn.Count == 0)
		{
			MessageBox.Show("Please add at least one serial number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			LogInSerialNumber.Focus();
			return;
		}
		if (App.CurrentUser == null)
		{
			MessageBox.Show("No user is logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		string status = (StatusComboBox.SelectedItem as string) ?? "In Stock";
		InventoryStore inventoryStore = InventoryStore.Load();
		int num = 0;
		new List<string>();
		foreach (string item2 in _serialNumbersToLogIn)
		{
			InventoryItem item = new InventoryItem
			{
				SerialNumber = item2.ToUpperInvariant(),
				UPC = new List<string> { _selectedUPC },
				Make = GetMakeFromUPC(_selectedUPC, inventoryStore),
				Type = GetTypeFromUPC(_selectedUPC, inventoryStore),
				CreatedBy = App.CurrentUser.Initials,
				CreatedAt = DateTime.Now,
				Status = status
			};
			inventoryStore.InventoryItems.Add(item);
			num++;
		}
		inventoryStore.Save();
		MessageBox.Show($"Successfully logged in {num} serial number(s) for UPC {_selectedUPC} by {App.CurrentUser.Initials}.", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		ClearLogInForm();
		App.SaveAll();
	}

	private string? GetMakeFromUPC(string upc, InventoryStore store)
	{
		if (store.UPCMetadata.TryGetValue(upc, out UPCMetadata value) && value.Make != null)
		{
			return value.Make;
		}
		return store.InventoryItems.FirstOrDefault((InventoryItem i) => i.UPC?.Contains(upc) ?? false)?.Make;
	}

	private string? GetTypeFromUPC(string upc, InventoryStore store)
	{
		if (store.UPCMetadata.TryGetValue(upc, out UPCMetadata value) && value.Type != null)
		{
			return value.Type;
		}
		return store.InventoryItems.FirstOrDefault((InventoryItem i) => i.UPC?.Contains(upc) ?? false)?.Type;
	}

	private void ClearLogInForm()
	{
		UPCSearchBox.Clear();
		SelectedUPC.Text = string.Empty;
		SelectedUPCInfo.Text = string.Empty;
		_selectedUPC = null;
		_serialNumbersToLogIn.Clear();
		SerialNumbersListBox.ItemsSource = null;
		LogInSerialNumber.Clear();
		UPCListBox.ItemsSource = null;
	}

	private void LogOutSerial_Click(object sender, RoutedEventArgs e)
	{
		object selectedItem = ResultsListBox.SelectedItem;
		InventoryItem selectedItem2 = selectedItem as InventoryItem;
		if (selectedItem2 == null)
		{
			MessageBox.Show("Please select an item from the search results first.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		if (App.CurrentUser == null)
		{
			MessageBox.Show("No user is logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		string text = LogOutReason.SelectedItem as string;
		if (string.IsNullOrEmpty(text))
		{
			MessageBox.Show("Please select a reason for logging out.", "Reason Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		if (OGPCheck.IsChecked == true)
		{
			selectedItem2.SoldBy = "OGP";
		}
		else
		{
			selectedItem2.SoldBy = App.CurrentUser.Initials;
		}
		selectedItem2.SoldAt = DateTime.Now;
		selectedItem2.Status = text;
		InventoryStore inventoryStore = InventoryStore.Load();
		int num = inventoryStore.InventoryItems.FindIndex((InventoryItem i) => i.Id == selectedItem2.Id);
		if (num >= 0)
		{
			inventoryStore.InventoryItems[num] = selectedItem2;
			inventoryStore.Save();
		}
		MessageBox.Show("Serial number logged out successfully by " + App.CurrentUser.Initials + ".", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		Search_Click(sender, e);
		App.SaveAll();
	}

	private void Search_Click(object sender, RoutedEventArgs e)
	{
		string serialInput = (SerialGetInput.Text ?? string.Empty).Trim().ToUpperInvariant();
		string upcInput = (UPCSearchInput.Text ?? string.Empty).Trim().ToUpperInvariant();
		if (serialInput.Length == 0 && upcInput.Length == 0)
		{
			MessageBox.Show("Please enter a serial number or UPC to search.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		List<InventoryItem> list = (from item in InventoryStore.Load().InventoryItems
			where Matches(serialInput, item.SerialNumber) || (item.UPC != null && item.UPC.Any((string upc) => Matches(upcInput, upc)))
			orderby item.Status switch
			{
				"In Stock" => 1, 
				"Available" => 1, 
				"Display" => 2, 
				"In Claims" => 3, 
				"Claimed Out" => 4, 
				"Sold" => 5, 
				"Missing" => 6, 
				_ => 7, 
			}
			select item).ToList();
		if (list.Count == 0)
		{
			MessageBox.Show("No matches found.", "Search Results", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return;
		}
		ResultsListBox.ItemsSource = list;
		App.SaveAll();
	}

	private static bool Matches(string input, string? fieldValue)
	{
		if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(fieldValue))
		{
			return false;
		}
		string text = fieldValue.Trim().ToUpper();
		string text2 = input.ToUpper();
		if (text2.Length <= 4)
		{
			return text.EndsWith(text2);
		}
		return text == text2;
	}

	private void ShowItem(InventoryItem item)
	{
		SerialTextBox.Text = "Serial #: " + item.SerialNumber;
		UPCBox.Text = "UPC: " + ((item.UPC != null) ? string.Join(", ", item.UPC) : string.Empty);
		string value = (item.CreatedAt.HasValue ? item.CreatedAt.Value.ToString("g") : "N/A");
		string value2 = (item.SoldAt.HasValue ? item.SoldAt.Value.ToString("g") : "N/A");
		string value3 = (item.OnlinePick ? "Yes" : "No");
		MessageBox.Show($"Make: {item.Make ?? "N/A"} | Type: {item.Type ?? "N/A"} | Status: {item.Status}\nOnline Pick: {value3}\nCreatedBy: {item.CreatedBy ?? "N/A"} | CreatedAt: {value}\nSoldBy: {item.SoldBy ?? "N/A"} | SoldAt: {value2}", "Item Details", MessageBoxButton.OK, MessageBoxImage.Asterisk);
	}

	private void ResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ResultsListBox.SelectedItem is InventoryItem inventoryItem)
		{
			_selectedInventoryItem = inventoryItem;
			SelectedSerialDisplay.Text = inventoryItem.SerialNumber;
			UpdateStatusComboBox.SelectedItem = inventoryItem.Status;
			ShowItem(inventoryItem);
			OnlinePickCheckBox.IsChecked = false;
		}
	}

	private void AddNewUPCButton_Click(object sender, RoutedEventArgs e)
	{
		NewUPCPanel.Visibility = Visibility.Visible;
		NewUPCBox.Text = UPCSearchBox.Text;
		if (!string.IsNullOrEmpty(_selectedUPC))
		{
			InventoryStore store = InventoryStore.Load();
			string makeFromUPC = GetMakeFromUPC(_selectedUPC, store);
			string typeFromUPC = GetTypeFromUPC(_selectedUPC, store);
			if (!string.IsNullOrEmpty(makeFromUPC))
			{
				NewUPCMakeBox.Text = makeFromUPC;
			}
			if (!string.IsNullOrEmpty(typeFromUPC))
			{
				NewUPCTypeBox.Text = typeFromUPC;
			}
		}
		UpdateLayout();
		NewUPCBox.Focus();
		NewUPCBox.SelectAll();
	}

	private void SaveNewUPCButton_Click(object sender, RoutedEventArgs e)
	{
		string newUPC = NewUPCBox.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(newUPC))
		{
			MessageBox.Show("UPC is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			NewUPCBox.Focus();
			return;
		}
		InventoryStore inventoryStore = InventoryStore.Load();
		if (inventoryStore.InventoryItems.Any((InventoryItem item) => item.UPC?.Any((string u) => string.Equals(newUPC, u?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? false))
		{
			MessageBox.Show("This UPC already exists in inventory. Please select it from the list.", "UPC Exists", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		inventoryStore.UPCMetadata[newUPC] = new UPCMetadata
		{
			Make = (string.IsNullOrEmpty(NewUPCMakeBox.Text?.Trim()) ? null : NewUPCMakeBox.Text?.Trim()),
			Type = (string.IsNullOrEmpty(NewUPCTypeBox.Text?.Trim()) ? null : NewUPCTypeBox.Text?.Trim())
		};
		inventoryStore.Save();
		NewUPCPanel.Visibility = Visibility.Collapsed;
		NewUPCBox.Clear();
		NewUPCMakeBox.Clear();
		NewUPCTypeBox.Clear();
		_selectedUPC = newUPC;
		SelectedUPC.Text = newUPC;
		UPCSearchBox_TextChanged(this, null);
		MessageBox.Show("New UPC '" + newUPC + "' created successfully. You can now log in serial numbers.", "UPC Created", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		App.SaveAll();
	}

	private void CancelNewUPCButton_Click(object sender, RoutedEventArgs e)
	{
		NewUPCPanel.Visibility = Visibility.Collapsed;
		NewUPCBox.Clear();
		NewUPCMakeBox.Clear();
		NewUPCTypeBox.Clear();
		App.SaveAll();
	}

	private void UpdateStatusButton_Click(object sender, RoutedEventArgs e)
	{
		if (_selectedInventoryItem == null)
		{
			MessageBox.Show("Please select an item from the search results first.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		if (App.CurrentUser == null)
		{
			MessageBox.Show("No user is logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		string text = UpdateStatusComboBox.SelectedItem as string;
		if (string.IsNullOrEmpty(text))
		{
			MessageBox.Show("Please select a new status.", "Status Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		InventoryStore inventoryStore = InventoryStore.Load();
		int num = inventoryStore.InventoryItems.FindIndex((InventoryItem i) => i.Id == _selectedInventoryItem.Id);
		if (num < 0)
		{
			MessageBox.Show("Item not found in inventory.", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		InventoryItem inventoryItem = inventoryStore.InventoryItems[num];
		if (text == "Sold")
		{
			if (OnlinePickCheckBox.IsChecked == true)
			{
				inventoryItem.SoldBy = "OGP";
			}
			else
			{
				inventoryItem.SoldBy = App.CurrentUser.Initials;
			}
			inventoryItem.SoldAt = DateTime.Now;
		}
		inventoryItem.OnlinePick = OnlinePickCheckBox.IsChecked == true;
		inventoryItem.Status = text;
		inventoryStore.Save();
		OnlinePickCheckBox.IsChecked = false;
		MessageBox.Show("Status updated successfully for serial number " + inventoryItem.SerialNumber + ".", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		ResultsListBox.SelectedItem = null;
		_selectedInventoryItem = null;
		SelectedSerialDisplay.Text = string.Empty;
		UpdateStatusComboBox.SelectedIndex = 0;
		Search_Click(sender, e);
		App.SaveAll();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.25.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Entertainment;V1.3.4.0;component/inventorylog/computerlog.xaml", UriKind.Relative);
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
			UPCSearchBox = (TextBox)target;
			UPCSearchBox.TextChanged += UPCSearchBox_TextChanged;
			break;
		case 4:
			UPCListBox = (ListBox)target;
			UPCListBox.SelectionChanged += UPCListBox_SelectionChanged;
			break;
		case 5:
			AddNewUPCButton = (Button)target;
			AddNewUPCButton.Click += AddNewUPCButton_Click;
			break;
		case 6:
			SelectedUPC = (TextBlock)target;
			break;
		case 7:
			SelectedUPCInfo = (TextBlock)target;
			break;
		case 8:
			NewUPCPanel = (Border)target;
			break;
		case 9:
			NewUPCBox = (TextBox)target;
			break;
		case 10:
			NewUPCMakeBox = (TextBox)target;
			break;
		case 11:
			NewUPCTypeBox = (TextBox)target;
			break;
		case 12:
			SaveNewUPCButton = (Button)target;
			SaveNewUPCButton.Click += SaveNewUPCButton_Click;
			break;
		case 13:
			CancelNewUPCButton = (Button)target;
			CancelNewUPCButton.Click += CancelNewUPCButton_Click;
			break;
		case 14:
			LogInSerialNumber = (TextBox)target;
			break;
		case 15:
			StatusComboBox = (ComboBox)target;
			break;
		case 16:
			AddSerialButton = (Button)target;
			AddSerialButton.Click += AddSerialButton_Click;
			break;
		case 17:
			SerialNumbersListBox = (ListBox)target;
			break;
		case 18:
			RemoveSerialButton = (Button)target;
			RemoveSerialButton.Click += RemoveSerialButton_Click;
			break;
		case 19:
			SelectedSerialDisplay = (TextBlock)target;
			break;
		case 20:
			UpdateStatusComboBox = (ComboBox)target;
			break;
		case 21:
			OnlinePickCheckBox = (CheckBox)target;
			break;
		case 22:
			UpdateStatusButton = (Button)target;
			UpdateStatusButton.Click += UpdateStatusButton_Click;
			break;
		case 23:
			SerialGetInput = (TextBox)target;
			break;
		case 24:
			UPCSearchInput = (TextBox)target;
			break;
		case 25:
			Search = (Button)target;
			Search.Click += Search_Click;
			break;
		case 26:
			UPCBox = (TextBlock)target;
			break;
		case 27:
			SerialTextBox = (TextBlock)target;
			break;
		case 28:
			ResultsListBox = (ListBox)target;
			ResultsListBox.SelectionChanged += ResultsListBox_SelectionChanged;
			break;
		case 29:
			LogOutReason = (ComboBox)target;
			break;
		case 30:
			LogOutSerial = (Button)target;
			LogOutSerial.Click += LogOutSerial_Click;
			break;
		case 31:
			OGPCheck = (CheckBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
