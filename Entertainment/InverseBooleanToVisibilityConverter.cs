using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Entertainment;

public partial class InverseBooleanToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int)
		{
			return ((int)value != 0) ? Visibility.Collapsed : Visibility.Visible;
		}
		if (value is bool)
		{
			return ((bool)value) ? Visibility.Collapsed : Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
