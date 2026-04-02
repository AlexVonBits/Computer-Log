using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Entertainment;

public partial class StatusToColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return ((value as string) ?? "") switch
		{
			"In Stock" => new SolidColorBrush(Color.FromRgb(0, 113, 206)), 
			"Display" => new SolidColorBrush(Color.FromRgb(0, 128, 128)), 
			"In Claims" => new SolidColorBrush(Color.FromRgb(byte.MaxValue, 152, 0)), 
			"Claimed Out" => new SolidColorBrush(Color.FromRgb(121, 85, 72)), 
			"Sold" => new SolidColorBrush(Color.FromRgb(46, 125, 50)), 
			"Missing" => new SolidColorBrush(Color.FromRgb(156, 39, 176)), 
			"Deleted" => new SolidColorBrush(Color.FromRgb(198, 40, 40)), 
			"Clearance" => new SolidColorBrush(Color.FromRgb(218, 165, 32)), 
			_ => new SolidColorBrush(Color.FromRgb(97, 97, 97)), 
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
