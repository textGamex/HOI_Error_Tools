using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers;
using HOI_Error_Tools.Logic.Analyzers.CountryDefine;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.State;
using NLog;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using AppUpdate;
using AppUpdate.Services;
using HOI_Error_Tools.Services;
using Jot;
using Microsoft.Extensions.DependencyInjection;
using HOI_Error_Tools.Logic.Messages;

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

    [ObservableProperty] 
    private string _modId = string.Empty;

    private Descriptor? _descriptor;
    private int _fileSum;
    private readonly IErrorMessageService _errorMessageService;
    private readonly IMessageBox _messageBox;
    private readonly ILogger _log;
    
    private static readonly AppVersion Version = new("v0.2.1-alpha");

    public MainWindowModel(ILogger logger, Tracker tracker, IErrorMessageService errorMessageService, IMessageBox messageBox)
    {
        _log = logger;
        _errorMessageService = errorMessageService;
        _messageBox = messageBox;

        tracker.Configure<MainWindowModel>()
            .Id(_ => "MainUI")
            .Properties(w => new { w.GameRootPath, w.ModRootPath })
            .PersistOn(nameof(PropertyChanged));
        tracker.Track(this);

        if (App.Current.Services.GetRequiredService<GlobalSettings>().EnableAutoCheckUpdate)
        {
            Task.Run(async () => await CheckAppUpdateAsync());
        }
#if DEBUG
        //ModRootFolderPath = @"D:\STEAM\steamapps\workshop\content\394360\2171092591"; // 碧蓝航线
        //ModRootFolderPath = @"D:\STEAM\steamapps\workshop\content\394360\2820469328"; // 明日方舟
#endif
    }

    partial void OnModRootPathChanged(string? oldValue, string newValue)
    {
        var descriptorPath = Path.Combine(newValue, "descriptor.mod");
        if (!File.Exists(descriptorPath))
        {
            _modRootPath = oldValue!;
            _messageBox.Show("此文件夹不存在 descriptor.mod 文件", "错误");
            _log.Warn("Modification root path change failed: \n path {Path} is not exist", descriptorPath);
            return;
        }
        var descriptor = new Descriptor(descriptorPath);
        ModName = descriptor.Name;
        ModTags = string.Join(", ", descriptor.Tags);
        ModImage = descriptor.Picture;
        ModId = descriptor.RemoteFileId;

        _descriptor = descriptor;
        _log.Debug("Modification root path changed");
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
            _messageBox.Show("未选择资源路径", "错误");
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
        WeakReferenceMessenger.Default.Send(new AnalysisCompleteMessage(_fileSum, elapsedTime));

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

        var errorsTask = analyzers
            .Select(analyzer => Task.Run(analyzer.GetErrorMessages))
            .ToList();
        await Task.WhenAll(errorsTask);

        var errorList = ImmutableList.CreateBuilder<ErrorMessage>();
        foreach (var error in errorsTask.Select(x => x.Result))
        {
            errorList.AddRange(error);
        }
        errorList.AddRange(GameResources.ErrorMessages);
        var result = errorList.ToImmutable();
        _errorMessageService.SetErrorMessages(result);
    }

    [RelayCommand]
    private void ClickAboutButton()
    {
        _messageBox.Show($".NET: {Environment.Version}\n 作者: textGamex");
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
    private static async Task CheckAppUpdateAsync()
    {
        var api = new GitHubApi("textGamex", "HOI_Error_Tools", Version);
        WeakReferenceMessenger.Default.Send(new AppUpdateMessage(await api.HasLatestAsync(), 
            new Uri("https://github.com/textGamex/HOI_Error_Tools/releases")));
    }
}