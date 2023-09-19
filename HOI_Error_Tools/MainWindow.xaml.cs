using System;
using CommunityToolkit.Mvvm.Messaging;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers.State;
using HOI_Error_Tools.View;
using System.Windows;
using HOI_Error_Tools.Services;
using Microsoft.Extensions.DependencyInjection;
using HOI_Error_Tools.Logic.Messages;
using Microsoft.Toolkit.Uwp.Notifications;
using NLog;
using MessageBox = System.Windows.MessageBox;

namespace HOI_Error_Tools;

public partial class MainWindow : Window
{
    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();
    public MainWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<MainWindow, AnalysisCompleteMessage>(this, (_, message) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                StartButton.Content = "完成";
                this.LoadingCircle.IsIndeterminate = false;

                var win = App.Current.Services.GetRequiredService<ErrorMessageWindowView>();
                win.Show();
                
                Reset();
                var settings = App.Current.Services.GetRequiredService<GlobalSettings>();
                if (settings.EnableParseCompletionPrompt)
                {
                    ParseCompletionToast(message.FileSum, message.ElapsedTime);
                }
            });
        });

        WeakReferenceMessenger.Default.Register<MainWindow, GlobalSettings>(this, (_, _)=>
        {
            var settingsWindow = App.Current.Services.GetRequiredService<SettingsWindowView>(); 
            settingsWindow.ShowDialog();
        });
        
        WeakReferenceMessenger.Default.Register<MainWindow, AppUpdateMessage>(this, (_, updateInfo)=>
        {
            if (updateInfo.HasNewVersion)
            {
                if (MessageBox.Show("检测到有新版本更新, 是否前往下载?", "有新版本", MessageBoxButton.YesNo) ==
                    MessageBoxResult.Yes)
                {
                    var info = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = updateInfo.NewVersionAppUrl.ToString(),
                        UseShellExecute = true,
                    };
                    System.Diagnostics.Process.Start(info);
                }
            }
            else
            {
                MessageBox.Show("您的版本为最新, 无需更新", "提示");
            }
        });
    }

    private void Reset()
    {
        App.Current.Services.GetRequiredService<IErrorMessageService>().Clear();
        StateFileAnalyzer.Clear();
        GameResources.ClearErrorMessagesCache();
        StartButton.Content = "开始分析";
    }

    private static void ParseCompletionToast(int fileSum, TimeSpan elapsedTime)
    {
        new ToastContentBuilder()
            .AddText("解析完成")
            .AddText($"共解析 {fileSum} 文件, 用时 {elapsedTime.TotalSeconds:F1} 秒")
            .Show();
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        ToastNotificationManagerCompat.Uninstall();
        Log.Info("MainWindow Closed");
    }
}