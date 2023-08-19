using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.ViewModels;

namespace HOI_Error_Tools.View;

/// <summary>
/// SettingsWindowView.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindowView : Window
{
    public SettingsWindowView(GlobalSettings settings)
    {
        InitializeComponent();

        this.DataContext = new SettingsWindowViewModel(settings);
    }
}