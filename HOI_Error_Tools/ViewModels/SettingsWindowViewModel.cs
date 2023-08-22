using System.Collections.Generic;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.View;
using Microsoft.Extensions.DependencyInjection;
using NLog;


namespace HOI_Error_Tools.ViewModels;

public partial class SettingsWindowViewModel : ObservableObject
{

    //private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    public SettingsWindowViewModel(ErrorMessageSettingsControlView item1)
    {
    }
}