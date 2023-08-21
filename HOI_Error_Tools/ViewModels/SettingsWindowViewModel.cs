using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.View;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using WinRT;


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

    [RelayCommand]
    private void ControlSelectionChanged(SelectionChangedEventArgs args)
    {
        Log.Debug("SettingsWindowView selection changed: {Header}", args.AddedItems[0].As<TabItem>().Header);
    }
}