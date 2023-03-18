using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.State;
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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var gamePaths = new GameResourcesPath(
                @"D:\STEAM\steamapps\common\Hearts of Iron IV",
                @"C:\Users\Programmer\Documents\Paradox Interactive\Hearts of Iron IV\mod\ASB-MIN");
            var list = new List<Task<StateFileAnalyzer>>();
            var stateResource = new StateResources(gamePaths);

            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();   //定义一个计时对象  
            oTime.Start();                         //开始计时 

            foreach (var path in gamePaths.StatesPathList)
            {
                list.Add(Task.Run(() => new StateFileAnalyzer(path, stateResource)));
            }

            Task.WhenAll(list);
            oTime.Stop();                          //结束计时
            //输出运行时间。  
            _logger.Info("程序的运行时间：{Value}毫秒", oTime.Elapsed.TotalMilliseconds);
            _logger.Info("Size: {Size}", list.Count);

            var size = list[0].Result._registeredProvince.Count;
            foreach (var task in list)
            {
                if (task.Result._registeredProvince.Count != size)
                {
                    _logger.Error("线程冲突");
                }
            }
        }
    }
}
