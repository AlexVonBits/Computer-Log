using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Entertainment;

public partial class SoldInfoVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((value as DateTime?).HasValue)
		{
			return Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
