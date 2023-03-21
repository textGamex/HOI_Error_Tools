using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.State;
using MessageBox = HandyControl.Controls.MessageBox;
using NLog;

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

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

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
#if DEBUG
        GameRootPath = @"D:\STEAM\steamapps\common\Hearts of Iron IV";
        ModRootPath = @"D:\STEAM\steamapps\workshop\content\394360\2131096629";
#endif
        if (string.IsNullOrEmpty(GameRootPath) || string.IsNullOrEmpty(ModRootPath))
        {
            MessageBox.Error("未选择资源路径", "错误");
            return;
        }

        StartParseButtonText = "分析中, 请稍等...";
        LoadingCircleIsRunning = true;

        await Task.Run(async () =>
        {
            var gameResourcesPath = new GameResourcesPath(GameRootPath, ModRootPath);
            var gameResources = new GameResources(gameResourcesPath);
            var errorsTask = new List<Task<IEnumerable<ErrorMessage>>>();
            var stateList = new List<AnalyzerBase>(gameResourcesPath.StatesPathList.Count);
            stateList.AddRange(gameResourcesPath.StatesPathList.Select(path => new StateFileAnalyzer(path, gameResources)));

            foreach (var stateFileAnalyzer in stateList)
            {
                var errorMessage = Task.Run(() => stateFileAnalyzer.GetErrorMessages());
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
        }).ConfigureAwait(false);
    }
}