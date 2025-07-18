using System.Globalization;
namespace ControleDeContas.Converters;

public class IsRecorrenteConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value?.ToString() == "Recorrente";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}