using System.Collections.Generic;
using System.Windows;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.View;

/// <summary>
/// ErrorFileInfoView.xaml 的交互逻辑
/// </summary>
public partial class ErrorFileInfoView : Window
{
    public ErrorFileInfoView(IEnumerable<(string, Position)> fileErrorInfoList)
    {
        InitializeComponent();

        this.DataContext = new ViewModels.ErrorFileInfoViewModel(fileErrorInfoList);
    }
}