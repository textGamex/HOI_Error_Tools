using HOI_Error_Tools.Logic.Analyzers.Error;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

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