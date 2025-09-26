using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ToolRackSetup.Properties;

namespace ToolRackSetup
{
    /// <summary>
    /// Interaction logic for RuntimeWindow.xaml
    /// </summary>
    public partial class RuntimeWindow : Window
    {
        // public for bindings
        public RuntimeController _runtimeController;
        public RuntimeController RuntimeController
        {
            get => _runtimeController;
        }

        public RuntimeWindow()
        {
            // _runtimeController due to bindings
            _runtimeController = new RuntimeController(ConnectionManager.Instance.Pipe);
            InitializeComponent();

            _dispatchTimer = new System.Windows.Threading.DispatcherTimer();
            _dispatchTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            _dispatchTimer.Interval = GetPollingTimeInterval(); // 1 second polling
            _dispatchTimer.Start();
        }
        private static TimeSpan GetPollingTimeInterval()
        {
            return new TimeSpan(0, 0, 0, 0, 500); // every 1 second...
        }

        DispatcherTimer _dispatchTimer;

        #region "WinAPI"

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr lpdwProcessId);

        #endregion


        private string getCurrentAppName()
        {
            IntPtr activeAppHandle = GetForegroundWindow();

            IntPtr activeAppProcessId;
            GetWindowThreadProcessId(activeAppHandle, out activeAppProcessId);

            Process currentAppProcess = Process.GetProcessById((int)activeAppProcessId);
            string currentAppName = FileVersionInfo.GetVersionInfo(currentAppProcess.MainModule.FileName).FileName;

            return currentAppName;
        }

        private void dispatcherTimer_Tick(object? sender, EventArgs e)
        {
            // if cnc12 isn't top most...then take away our top most bit, and vice-versa
            String appName = getCurrentAppName();
            //Debug.Print(appName);
            if (appName != null && (appName.EndsWith("VirtualControlPanel.exe") || appName.EndsWith("cncr.exe")))
            {
                if (this.Topmost == false)
                {

                    // Make sure we are topmost ..stupid ness
                    this.Topmost = true;
                    this.Topmost = false;
                    this.Topmost = true;
                }
            }
            else
            {
                if (this.Topmost == true)
                {
                    this.Topmost = false;
                }
            }
        }

        private void window_Closed(object sender, EventArgs e)
        {
            _dispatchTimer.Stop();

            _runtimeController.Dispose();
        }

        private void window_LocationChanged(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is Button) //  || e.Source is TextBlock
            {
                // Don't drag the window if the user clicked on a button or text block
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();

        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Settings.Default.Save();

        }
    }
}
