using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.State;
using HOI_Error_Tools.View;
using NLog;

namespace HOI_Error_Tools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
}
