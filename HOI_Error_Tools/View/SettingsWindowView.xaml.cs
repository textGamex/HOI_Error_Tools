using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using HOI_Error_Tools.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace HOI_Error_Tools.View;

public partial class SettingsWindowView : Window
{
    private readonly ILogger _log;

    public SettingsWindowView(
        SettingsWindowViewModel viewModel,
        ILogger log)
    {
        InitializeComponent();
        _log = log;
        DataContext = viewModel;

        var errorSettingsRoot = new SideMenuItem("错误过滤", typeof(CommonErrorMessageSettingsControlView));
        var commonSettingsRoot = new SideMenuItem("通用", typeof(CommonSettingsControlView));

        errorSettingsRoot.Items = new List<SideMenuItem>
        {
            new ("普通", typeof(CommonErrorMessageSettingsControlView)),
            new ("错误类型", typeof(ErrorMessageSettingsControlView))
        };

        SideTreeView.Items.Add(commonSettingsRoot);
        SideTreeView.Items.Add(errorSettingsRoot);
    }

    private void SideTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not SideMenuItem item)
        {
            _log.Warn("Selected New Value is not SideMenuItem Type");
            return;
        }

        if (item.ScreenType is null)
        {
            _log.Warn("Selected SideMenuItem ScreenType is null");
            return;
        }

        if (ContentControlMain.Content?.GetType() == item.ScreenType)
        {
            _log.Debug(CultureInfo.InvariantCulture,
                "切换未成功, 已经在 {Name} 界面", item.ScreenType.Name);
            return;
        }

        ContentControlMain.Content = App.Current.Services.GetRequiredService(item.ScreenType);
        _log.Debug(CultureInfo.InvariantCulture,
            "成功切换到 {Name}", item.Title);
    }
}