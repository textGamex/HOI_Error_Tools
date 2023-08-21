using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers;
using HOI_Error_Tools.Logic.Analyzers.CountryDefine;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.State;
using NLog;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using HOI_Error_Tools.Services;
using Jot;
using Microsoft.Extensions.DependencyInjection;
using MessageBox = HandyControl.Controls.MessageBox;
using Microsoft.Toolkit.Uwp.Notifications;

namespace HOI_Error_Tools;

public partial class MainWindowModel : ObservableObject
{
    [ObservableProperty]
    private string _gameRootPath = string.Empty;

    [ObservableProperty]
    private string _modRootPath = string.Empty;

    [ObservableProperty]
    private string _startParseButtonText = "开始分析";

    [ObservableProperty]
    private bool _loadingCircleIsRunning = false;

    [ObservableProperty]
    private string _modName = string.Empty;

    [ObservableProperty]
    private string _modTags = string.Empty;
    [ObservableProperty]
    private BitmapImage? _modImage;
    
    private Descriptor? _descriptor;
    private int _fileSum;
    private IErrorMessageService _errorMessageService;

    private readonly ILogger _log;

    public MainWindowModel(ILogger logger, Tracker tracker, IErrorMessageService errorMessageService)
    {
        _log = logger;
        _errorMessageService = errorMessageService;
        PropertyChanged += MainWindowModel_OnPropertyChanged;

        tracker.Configure<MainWindowModel>()
            .Id(w => "MainUI")
            .Properties(w => new { w.GameRootPath, w.ModRootPath })
            .PersistOn(nameof(PropertyChanged));
        tracker.Track(this);

#if DEBUG
        //GameRootPath = @"D:\STEAM\steamapps\common\Hearts of Iron IV";
        //ModRootPath = @"D:\STEAM\steamapps\workshop\content\394360\2171092591"; // 碧蓝航线
        //ModRootPath = @"D:\STEAM\steamapps\workshop\content\394360\2820469328"; // 明日方舟
#endif
    }

    private void MainWindowModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ModRootPath))
        {
            var descriptor = new Descriptor(ModRootPath);
            ModName = descriptor.Name;
            ModTags = string.Join(", ", descriptor.Tags);
            ModImage = descriptor.Picture;

            _descriptor = descriptor;
        }
        _log.Debug(CultureInfo.InvariantCulture,
            "Property changed: {PropertyName}", e.PropertyName);
    }

    [RelayCommand]
    private void ClickSelectGameRootPathButton()
    {
        FolderBrowserDialog dialog = new()
        {
            Description = "选择游戏文件夹"
        };
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }
        GameRootPath = dialog.SelectedPath;
    }

    [RelayCommand]
    private void ClickSelectModRootPathButton()
    {
        FolderBrowserDialog dialog = new()
        {
            Description = "选择Mod文件夹"
        };
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }
        ModRootPath = dialog.SelectedPath;
    }

    [RelayCommand]
    private async Task StartParse()
    {
        if (string.IsNullOrEmpty(GameRootPath) || string.IsNullOrEmpty(ModRootPath))
        {
            MessageBox.Error("未选择资源路径", "错误");
            return;
        }

        StartParseButtonText = "分析中, 请稍等...";
        LoadingCircleIsRunning = true;

        var oTime = new Stopwatch();
        _log.Info("开始分析");
        oTime.Start();
        await StartAnalyzersAsync();
        oTime.Stop();
        var elapsedTime = oTime.Elapsed;

        new ToastContentBuilder()
            .AddText("解析完成")
            .AddText($"共解析 {_fileSum} 文件, 用时 {elapsedTime.TotalSeconds:F1} 秒")
            .Show();

        _log.Info("分析完成, 用时: {Second:F1} s, {Millisecond:F0} ms",
            elapsedTime.TotalSeconds, elapsedTime.TotalMilliseconds);
    }

    private async Task StartAnalyzersAsync()
    {
        Debug.Assert(_descriptor != null, nameof(_descriptor) + " != null");

        var gameResourcesPath = new GameResourcesPath(GameRootPath, ModRootPath, _descriptor);
        var gameResources = new GameResources(gameResourcesPath);
        _fileSum = gameResourcesPath.FileSum;

        var analyzers = 
            new List<AnalyzerBase>(gameResourcesPath.StatesFilePathList.Count + gameResourcesPath.CountriesDefineFilePath.Count);
        analyzers.AddRange(gameResourcesPath.StatesFilePathList.Select(path => new StateFileAnalyzer(path, gameResources)));
        analyzers.AddRange(gameResourcesPath.CountriesDefineFilePath.Select(path => new CountryDefineFileAnalyzer(path, gameResources)));

        var errorsTask = analyzers.Select(analyzer => Task.Run(analyzer.GetErrorMessages)).ToList();
        await Task.WhenAll(errorsTask);

        var errorList = ImmutableList.CreateBuilder<ErrorMessage>();
        foreach (var error in errorsTask.Select(x => x.Result))
        {
            errorList.AddRange(error);
        }
        errorList.AddRange(GameResources.ErrorMessages);
        var result = errorList.ToImmutable();
        _errorMessageService.SetErrorMessages(result);
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<IReadOnlyList<ErrorMessage>>(result));
    }

    [RelayCommand]
    private static void ClickAboutButton()
    {
        MessageBox.Info($".NET: {Environment.Version}\n 作者: textGamex");
    }

    [RelayCommand]
    private static void ClickProjectLinkButton()
    {
        var info = new ProcessStartInfo()
        {
            FileName = "https://github.com/textGamex/HOI_Error_Tools",
            UseShellExecute = true,
        };
        _ = Process.Start(info);
    }

    [RelayCommand]
    private static void ClickSettingsButton()
    {
        WeakReferenceMessenger.Default.Send(App.Current.Services.GetRequiredService<GlobalSettings>());
    }

    [RelayCommand]
    private void WindowClosing()
    {
        ToastNotificationManagerCompat.Uninstall();
        _log.Info("Main Window Closing");
    }
}