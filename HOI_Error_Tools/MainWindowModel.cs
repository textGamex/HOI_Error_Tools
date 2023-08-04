﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.State;
using NLog;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HOI_Error_Tools.Logic.Analyzers.CountryDefine;
using MessageBox = HandyControl.Controls.MessageBox;

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

    private Descriptor? _descriptor;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public MainWindowModel()
    {
        PropertyChanged += MainWindowModel_OnPropertyChanged;
#if DEBUG
        GameRootPath = @"D:\STEAM\steamapps\common\Hearts of Iron IV";
        ModRootPath = @"D:\STEAM\steamapps\workshop\content\394360\2171092591";
#endif
    }

    private void MainWindowModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ModRootPath))
        {
            var descriptor = new Descriptor(ModRootPath);
            ModName = descriptor.Name;
            ModTags = string.Join(", ", descriptor.Tags);

            _descriptor = descriptor;
        }
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
    private async Task ClickStartButton()
    {
        if (string.IsNullOrEmpty(GameRootPath) || string.IsNullOrEmpty(ModRootPath))
        {
            MessageBox.Error("未选择资源路径", "错误");
            return;
        }

        StartParseButtonText = "分析中, 请稍等...";
        LoadingCircleIsRunning = true;
        
        await StartAnalyzersAsync();
    }

    private async Task StartAnalyzersAsync()
    {
        Debug.Assert(_descriptor != null, nameof(_descriptor) + " != null");

        var gameResourcesPath = new GameResourcesPath(GameRootPath, ModRootPath, _descriptor);
        var gameResources = new GameResources(gameResourcesPath);
        var errorsTask = new List<Task<IEnumerable<ErrorMessage>>>();
        var analyzers = new List<AnalyzerBase>(1024);
        analyzers.AddRange(gameResourcesPath.StatesFilePathList.Select(path => new StateFileAnalyzer(path, gameResources)));
        analyzers.AddRange(gameResourcesPath.CountriesDefineFilePath.Select(path => new CountryDefineFileAnalyzer(path, gameResources)));
        foreach (var analyzer in analyzers)
        {
            var errorMessage = Task.Run(() => analyzer.GetErrorMessages());
            errorsTask.Add(errorMessage);
        }
        await Task.WhenAll(errorsTask);
        var errorList = ImmutableList.CreateBuilder<ErrorMessage>();
        foreach (var error in errorsTask.Select(x => x.Result))
        {
            errorList.AddRange(error);
        }
        errorList.AddRange(GameResources.ErrorMessages);
        
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<IImmutableList<ErrorMessage>>(errorList.ToImmutable()));
    }
}