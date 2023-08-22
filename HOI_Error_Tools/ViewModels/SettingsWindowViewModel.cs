using System.Collections.Generic;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using HOI_Error_Tools.View;
using Microsoft.Extensions.DependencyInjection;
using NLog;


namespace HOI_Error_Tools.ViewModels;

public partial class SettingsWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private List<TabItem> _data;

    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    public SettingsWindowViewModel(ErrorMessageSettingsControlView item1)
    {
        _data = new List<TabItem>(1)
        {
            new()
            {
                Header = "Error Messages",
                Content = item1
            },
            new()
            {
                Header = "错误显示"
            }
        };
    }
}