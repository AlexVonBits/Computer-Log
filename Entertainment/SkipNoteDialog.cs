using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Entertainment;

public partial class SkipNoteDialog : Window
{
	private TextBox _noteTextBox;

	public string Note { get; private set; } = "";

	public SkipNoteDialog()
	{
		base.Title = "Skip Item - Add Note";
		base.Width = 400.0;
		base.Height = 220.0;
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
		TextBlock element = new TextBlock
		{
			Text = "Why are you skipping this item?",
			Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
			FontSize = 14.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
			TextWrapping = TextWrapping.Wrap
		};
		Grid.SetRow(element, 0);
		grid.Children.Add(element);
		_noteTextBox = new TextBox
		{
			Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
			Padding = new Thickness(8.0, 6.0, 8.0, 6.0),
			Height = 60.0,
			TextWrapping = TextWrapping.Wrap,
			AcceptsReturn = true,
			CaretBrush = Brushes.White,
			Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
		};
		Grid.SetRow(_noteTextBox, 1);
		grid.Children.Add(_noteTextBox);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right
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
			Content = "Skip Item",
			Width = 100.0,
			Padding = new Thickness(10.0, 5.0, 10.0, 5.0),
			Background = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(198, 40, 40))
		};
		button2.Click += SkipButton_Click;
		stackPanel.Children.Add(button);
		stackPanel.Children.Add(button2);
		Grid.SetRow(stackPanel, 2);
		grid.Children.Add(stackPanel);
		base.Content = grid;
	}

	private void SkipButton_Click(object sender, RoutedEventArgs e)
	{
		Note = _noteTextBox.Text.Trim();
		if (string.IsNullOrEmpty(Note))
		{
			MessageBox.Show("Please enter a note explaining why you're skipping this item.", "Note Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
		else
		{
			base.DialogResult = true;
		}
	}
}
