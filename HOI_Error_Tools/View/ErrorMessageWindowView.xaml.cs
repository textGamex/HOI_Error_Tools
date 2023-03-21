using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HOI_Error_Tools.Logic.Analyzers.Error;
using NLog;
using static HOI_Error_Tools.ViewModels.ErrorMessageWindowViewModel;

namespace HOI_Error_Tools.View;

/// <summary>
/// ErrorMessageWindowView.xaml 的交互逻辑
/// </summary>
public partial class ErrorMessageWindowView
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    public ErrorMessageWindowView(IImmutableList<ErrorMessage> errors)
    {
        InitializeComponent();

        this.DataContext = new ViewModels.ErrorMessageWindowViewModel(errors);
    }
}

public class StringCollectionConverter : IValueConverter
{
    public static StringCollectionConverter Instance { get; } = new ();
    private const string Separator = ", ";

    private StringCollectionConverter()
    { }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<string> collection)
        {
            return string.Join(Separator, collection);
        }

        throw new ArgumentException($"转换失败, 类型不应该是 {value.GetType().FullName}", nameof(value));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str.Split(Separator);
        }

        throw new ArgumentException($"转换失败, 类型不应该是 {value.GetType().FullName}", nameof(value));
    }
}