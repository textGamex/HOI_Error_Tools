using Jot.Storage;
using Jot;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AppUpdate;
using AppUpdate.Services;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using HOI_Error_Tools.View;
using HOI_Error_Tools.ViewModels;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using ByteSizeLib;
using LogLevel = Microsoft.AppCenter.LogLevel;
using MessageBox = HOI_Error_Tools.Services.MessageBox;
namespace HOI_Error_Tools;

public partial class App : Application
{
    public const string AppVersion = "v0.2.2-alpha";
    public static string LogsFolderPath { get; } = Path.Combine(Environment.CurrentDirectory, "Logs");
    public new static App Current => (App)Application.Current;
    public IServiceProvider Services { get; } = ConfigureServices();

    private static readonly ILogger Log = Current.Services.GetRequiredService<ILogger>();
    private static readonly IMessageBox MessageBox = Current.Services.GetRequiredService<IMessageBox>();

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddTransient<ILogger, Logger>(_ => LogManager.GetCurrentClassLogger());
        services.AddSingleton<IErrorMessageService, ErrorMessageService>();
        services.AddSingleton<IErrorFileInfoService, ErrorFileInfoService>();
        services.AddSingleton<Tracker>(_ => new Tracker(new JsonFileStore(GlobalSettings.SettingsFolderPath)));
        services.AddSingleton<GlobalSettings>(_ => GlobalSettings.Load());
        services.AddSingleton<IMessageBox, MessageBox>();
        services.AddSingleton<AppVersion>(_ => new AppVersion(AppVersion));
        services.AddTransient<UpdateServiceBase, GitHubApi>(sp =>
            new GitHubApi("textGamex", "HOI_Error_Tools", sp.GetRequiredService<AppVersion>()));

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
        SetAppCenter();
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    private static void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        foreach (var exception in e.Exception.InnerExceptions)
        {
            Log.Error(exception);
        }
        ErrorTip();
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception);
        ErrorTip();
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Error(e.ExceptionObject);
        ErrorTip();
    }

    private static void ErrorTip()
    {
        MessageBox.ErrorTip("未知错误, 请联系作者并提供程序目录下 Logs 目录下的日志文件");
    }

    private static void SetAppCenter()
    {
#if DEBUG
        AppCenter.SetEnabledAsync(false);
#elif RELEASE
        AppCenter.SetEnabledAsync(Current.Services.GetRequiredService<GlobalSettings>().EnableAppCenter);
#endif
        AppCenter.SetMaxStorageSizeAsync((long)ByteSize.FromMebiBytes(25).Bytes);
        AppCenter.LogLevel = LogLevel.Info;
        AppCenter.Start(PrivateData.AppSecret, typeof(Analytics), typeof(Crashes));
        var countryCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;
        AppCenter.SetCountryCode(countryCode);
    }
}