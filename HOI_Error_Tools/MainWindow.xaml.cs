using System;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.State;
using HOI_Error_Tools.View;
using NLog;
using System.Collections.Immutable;
using System.Windows;
using System.Windows.Threading;

namespace HOI_Error_Tools;

public partial class MainWindow : Window
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public MainWindow()
    {
        InitializeComponent();

        this.DataContext = new MainWindowModel();

        WeakReferenceMessenger.Default.Register<MainWindow, ValueChangedMessage<IImmutableList<ErrorMessage>>>(this, (_, list) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                StartButton.Content = "完成";
                this.LoadingCircle.IsRunning = false;
                var win = new ErrorMessageWindowView(list.Value);
                win.Show();

                StateFileAnalyzer.Clear();
                GameResources.ClearErrorMessagesCache();
#if RELEASE
                    this.Close();
#endif  
            });
        });
    }
}