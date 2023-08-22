using CommunityToolkit.Mvvm.Messaging;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers.State;
using HOI_Error_Tools.View;
using System.Windows;
using HOI_Error_Tools.Services;
using Microsoft.Extensions.DependencyInjection;
using HOI_Error_Tools.Logic.Messages;

namespace HOI_Error_Tools;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<MainWindow, AnalysisCompleteMessage>(this, (_, _) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                StartButton.Content = "完成";
                this.LoadingCircle.IsIndeterminate = false;

                var win = App.Current.Services.GetRequiredService<ErrorMessageWindowView>();
                win.Show();
                App.Current.Services.GetRequiredService<IErrorMessageService>().Clear();

                StateFileAnalyzer.Clear();
                GameResources.ClearErrorMessagesCache();
                StartButton.Content = "开始分析";
                //TODO: 用户可以自行设置是否关闭
#if RELEASE
                this.Close();
#endif
            });
        });

        WeakReferenceMessenger.Default.Register<MainWindow, GlobalSettings>(this, (_, _)=>
        {
            var settingsWindow = App.Current.Services.GetRequiredService<SettingsWindowView>(); 
            settingsWindow.ShowDialog();
        });
    }
}