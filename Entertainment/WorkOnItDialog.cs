using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Entertainment;

public partial class WorkOnItDialog : Window
{
	private ComboBox _statusComboBox;

	private TextBox _noteTextBox;

	private string _currentUser;

	public string SelectedStatus { get; private set; } = "";

	public string Note { get; private set; } = "";

	public WorkOnItDialog(string userName)
	{
		_currentUser = userName;
		base.Title = "Work On It - Change Status";
		base.Width = 400.0;
		base.Height = 280.0;
		base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		base.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
		BuildUI();
	}

	private void BuildUI()
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(20.0)
		};
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		TextBlock element = new TextBlock
		{
			Text = "New Status:",
			Foreground = Brushes.White,
			FontSize = 14.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
		};
		Grid.SetRow(element, 0);
		grid.Children.Add(element);
		_statusComboBox = new ComboBox
		{
			Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
			Padding = new Thickness(8.0, 6.0, 8.0, 6.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
		};
		_statusComboBox.Items.Add("In Stock");
		_statusComboBox.Items.Add("Display");
		_statusComboBox.Items.Add("In Claims");
		_statusComboBox.Items.Add("Claimed Out");
		_statusComboBox.Items.Add("Sold");
		_statusComboBox.Items.Add("Missing");
		_statusComboBox.SelectedIndex = 0;
		Grid.SetRow(_statusComboBox, 1);
		grid.Children.Add(_statusComboBox);
		TextBlock element2 = new TextBlock
		{
			Text = "Note:",
			Foreground = Brushes.White,
			FontSize = 14.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
		};
		Grid.SetRow(element2, 2);
		grid.Children.Add(element2);
		_noteTextBox = new TextBox
		{
			Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
			Padding = new Thickness(8.0, 6.0, 8.0, 6.0),
			Height = 60.0,
			TextWrapping = TextWrapping.Wrap,
			AcceptsReturn = true,
			CaretBrush = Brushes.White
		};
		Grid.SetRow(_noteTextBox, 3);
		grid.Children.Add(_noteTextBox);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 15.0, 0.0, 0.0)
		};
		Button button = new Button
		{
			Content = "Cancel",
			Width = 80.0,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0),
			Padding = new Thickness(10.0, 5.0, 10.0, 5.0),
			Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
		};
		button.Click += delegate
		{
			base.DialogResult = false;
		};
		Button button2 = new Button
		{
			Content = "Save",
			Width = 80.0,
			Padding = new Thickness(10.0, 5.0, 10.0, 5.0),
			Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50))
		};
		button2.Click += SaveButton_Click;
		stackPanel.Children.Add(button);
		stackPanel.Children.Add(button2);
		Grid.SetRow(stackPanel, 4);
		grid.Children.Add(stackPanel);
		base.Content = grid;
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		SelectedStatus = _statusComboBox.SelectedItem?.ToString() ?? "";
		Note = _noteTextBox.Text.Trim();
		if (string.IsNullOrEmpty(Note))
		{
			MessageBox.Show("Please enter a note explaining the status change.", "Note Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
		else
		{
			base.DialogResult = true;
		}
	}
}
