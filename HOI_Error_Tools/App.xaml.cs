﻿using Jot.Storage;
using Jot;
using System;
using System.Windows;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using HOI_Error_Tools.View;
using HOI_Error_Tools.ViewModels;
using MessageBox = HOI_Error_Tools.Services.MessageBox;

namespace HOI_Error_Tools;

public partial class App : Application
{
    public new static App Current => (App)Application.Current;
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddTransient<ILogger, Logger>(_ => LogManager.GetCurrentClassLogger());
        services.AddSingleton<IErrorMessageService, ErrorMessageService>();
        services.AddSingleton<IErrorFileInfoService, ErrorFileInfoService>();
        services.AddSingleton<Tracker>(_ => new Tracker(new JsonFileStore(GlobalSettings.SettingsFolderPath)));
        services.AddSingleton<GlobalSettings>(_ => GlobalSettings.Load());
        services.AddSingleton<IMessageBox, MessageBox>();

        services.AddSingleton<MainWindow>(sp => 
            new MainWindow() { DataContext = sp.GetRequiredService<MainWindowModel>() });
        services.AddSingleton<MainWindowModel>();

        services.AddTransient<SettingsWindowView>();
        services.AddTransient<SettingsWindowViewModel>();

        services.AddTransient<ErrorMessageWindowView>(sp => 
                       new ErrorMessageWindowView() { DataContext = sp.GetRequiredService<ErrorMessageWindowViewModel>() });
        services.AddTransient<ErrorMessageWindowViewModel>();

        services.AddTransient<ErrorFileInfoView>(sp => 
            new ErrorFileInfoView() { DataContext = sp.GetRequiredService<ErrorFileInfoViewModel>() });
        services.AddTransient<ErrorFileInfoViewModel>();

        services.AddTransient<ErrorMessageSettingsControlView>(sp => 
            new ErrorMessageSettingsControlView() { DataContext = sp.GetRequiredService<ErrorMessageSettingsControlViewModel>() });
        services.AddTransient<ErrorMessageSettingsControlViewModel>();

        services.AddTransient<CommonSettingsControlView>(sp => 
            new CommonSettingsControlView() { DataContext = sp.GetRequiredService<CommonSettingsControlViewModel>()});
        services.AddTransient<CommonSettingsControlViewModel>();

        services.AddTransient<CommonErrorMessageSettingsControlView>(sp => 
                       new CommonErrorMessageSettingsControlView() { DataContext = sp.GetRequiredService<CommonErrorMessageSettingsControlViewModel>() });
        services.AddTransient<CommonErrorMessageSettingsControlViewModel>();

        return services.BuildServiceProvider();
    }

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }
}