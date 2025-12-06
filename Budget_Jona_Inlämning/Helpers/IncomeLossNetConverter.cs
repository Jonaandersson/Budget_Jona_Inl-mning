#nullable enable
using System;
using System.Globalization;
using System.Windows.Data;

namespace Budget_Jona_Inlämning.Helpers;

/// <summary>
/// Provides a value converter that calculates the net income loss by subtracting a refund amount from a lost amount.
/// Intended for use in data binding scenarios where multiple input values are combined into a single output value.
/// </summary>
/// <remarks>This converter implements the IMultiValueConverter interface for use in WPF or similar frameworks.
/// The Convert method expects two input values: the first representing the amount lost and the second representing the
/// refund. If either value is missing or cannot be converted to a decimal, the converter returns 0. The ConvertBack
/// method is not supported and will throw a NotSupportedException if called.</remarks>
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