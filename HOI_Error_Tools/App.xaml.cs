using Jot.Storage;
using Jot;
using System;
using System.Windows;
using HOI_Error_Tools.Logic;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using HOI_Error_Tools.View;
using HOI_Error_Tools.ViewModels;

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
        services.AddSingleton<MainWindow>(sp => 
            new MainWindow() { DataContext = sp.GetRequiredService<MainWindowModel>() });
        services.AddSingleton<MainWindowModel>();
        services.AddTransient<GlobalSettings>(_ => GlobalSettings.Load());
        services.AddTransient<SettingsWindowView>(sp => 
            new SettingsWindowView() { DataContext = sp.GetRequiredService<SettingsWindowViewModel>() });
        services.AddTransient<SettingsWindowViewModel>();
        services.AddSingleton<Tracker>(_ => new Tracker(new JsonFileStore(GlobalSettings.SettingsFolderPath)));

        return services.BuildServiceProvider();
    }

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }
}