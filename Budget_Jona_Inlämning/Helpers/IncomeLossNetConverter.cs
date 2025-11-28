#nullable enable
using System;
using System.Globalization;
using System.Windows.Data;

namespace Budget_Jona_Inlämning.Helpers;

public sealed class IncomeLossNetConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2)
        {
            return 0m;
        }

        try
        {
            Decimal amountLost = System.Convert.ToDecimal(values[0], culture);
            Decimal refund = System.Convert.ToDecimal(values[1], culture);
            return amountLost - refund;
        }
        catch
        {
            return 0m;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}