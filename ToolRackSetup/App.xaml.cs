using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Transactions;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using ToolRackSetup.Properties;

namespace ToolRackSetup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public partial class App : Application
    {

        // This let's the app be in any location and still find the Centroid DLL's
        // Which is essential for me to debug.
        private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            // Extract the assembly name
            string? assemblyName = new AssemblyName(args.Name).Name;

            // Define the path to the assembly
            string assemblyPath = Path.Combine("C:\\CNCM", $"{assemblyName}.dll");

            // Load and return the assembly
            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }

            // Return null if the assembly cannot be found
            return null;
        }



        public App() : base()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }


        private static bool AlreadyProcessedOnThisInstance;

        static string ArgShowToolWindow = "-showToolSetupWindow";
        static string ArgShowFetchToolWindow = "-showFetchToolWindow";
        private void MakeSingleInstance()
        {
            if (AlreadyProcessedOnThisInstance) // Probably not needed; copied from some example
            {
                return;
            }
            AlreadyProcessedOnThisInstance = true;

            Application app = Application.Current;

            string showToolWindowEventName = "ToolRacksetup" + ArgShowToolWindow;
            string showFetchToolWindowEventName = "ToolRackSetup" + ArgShowFetchToolWindow;

            bool isSecondaryInstance = true;

            EventWaitHandle? showToolWindowEventHandle = null;
            EventWaitHandle? showFetchToolWindowEventHandle = null;
            try
            {
                showToolWindowEventHandle = EventWaitHandle.OpenExisting(showToolWindowEventName);
                showFetchToolWindowEventHandle = EventWaitHandle.OpenExisting(showFetchToolWindowEventName);
            }
            catch
            {
                // This code only runs on the first instance.
                isSecondaryInstance = false;
            }

            if (isSecondaryInstance)
            {
                // See what parameters we are passsing

                string[] args = Environment.GetCommandLineArgs();
                for (int index = 1; index < args.Length; index += 1)
                {
                    if (String.Equals(args[index], ArgShowToolWindow, StringComparison.OrdinalIgnoreCase))
                    {
                        showToolWindowEventHandle!.Set();
                        Environment.Exit(0);
                    }
                    if (String.Equals(args[index], ArgShowFetchToolWindow, StringComparison.OrdinalIgnoreCase))
                    {
                        showFetchToolWindowEventHandle!.Set();
                        Environment.Exit(0);
                    }
                }


                showToolWindowEventHandle!.Set();

                Environment.Exit(0);
            }

            // Main instance!!
            RegisterForCallback(showToolWindowEventName, ShowToolWindowCallback);
            RegisterForCallback(showFetchToolWindowEventName, ShowToolFetcWindowCallback);
        }


        private void RegisterForCallback(string eventName, WaitOrTimerCallback callBack)
        {
            EventWaitHandle eventWaitHandle = new EventWaitHandle(
                false,
                EventResetMode.AutoReset,
                eventName);

            ThreadPool.RegisterWaitForSingleObject(eventWaitHandle, callBack, this, Timeout.Infinite, false);

            eventWaitHandle.Close();
        }


        private static void ShowToolWindowCallback(object state, bool timedOut)
        {
            App app = (App)state;
            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                app.ShowToolManagerWindow();
            }));
        }

        private static void ShowToolFetcWindowCallback(object state, bool timedOut)
        {
            App app = (App)state;
            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                app.ShowToolFetchWindow();
            }));
        }

        RuntimeWindow? _runtimeWindow = null;
        public void ShowRuntimeWindow()
        {
            if (_runtimeWindow == null)
            {
                _runtimeWindow = new RuntimeWindow();
            }
            _runtimeWindow.Show();
            // TODO: flip topmost on/off based on the centroid window being active or not..
            _runtimeWindow.Topmost = true;
            _runtimeWindow.Closed += _runtimeWindow_Closed;
            Settings.Default.RuntimeWindowVisible = true;
            Settings.Default.Save();

        }

        private void _runtimeWindow_Closed(object? sender, EventArgs e)
        {
            _runtimeWindow = null;
            Settings.Default.RuntimeWindowVisible = false;
            Settings.Default.Save();
        }

        public void ShowToolManagerWindow()
        {
            // If it is still around, then bring it forwards, otherwise create a new one
            ToolManagerWindow? window = ToolManagerWindow.Instance;
            if (window == null)
            {
                window = new ToolManagerWindow();
            }

            window.Show();
            window.Activate();
            // force it up...the batch file opening it doesn't always work due to it closing
            window.Topmost = true; // I think we'll leave it top most
            window.Topmost = false;
        }

        private FetchToolPopup? _fetchToolPopup;
        private void ShowToolFetchWindow()
        {
            if (_fetchToolPopup == null)
            {
                _fetchToolPopup = new FetchToolPopup();
            }

            _fetchToolPopup.Popup();
        }

        public void DropFetchToolPopupInstance()
        {
            _fetchToolPopup = null;
        }

        public void CloseAllWindows()
        {
            _fetchToolPopup?.Close();
            _fetchToolPopup = null;
            ToolManagerWindow.Instance?.Close();
            if (_runtimeWindow != null)
            {
                // unhook the event so we don't save the close state
                _runtimeWindow.Closed -= _runtimeWindow_Closed;
                _runtimeWindow?.Close();
                _runtimeWindow = null;
            }
        }


        protected override void OnStartup(StartupEventArgs e)
        {

            MakeSingleInstance();
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(HandleException);

            _ = ConnectionManager.Instance; // start it up

            // TODO: commonize this code a bit more..
            string[] args = Environment.GetCommandLineArgs();
            bool minimizeApp = false;
            Action? action = null;
            for (int index = 1; index < args.Length; index += 1)
            {
                if (String.Equals(args[index], ArgShowToolWindow, StringComparison.OrdinalIgnoreCase))
                {
                    action = new Action(() => ShowToolManagerWindow());
                    minimizeApp = true;
                }
                if (String.Equals(args[index], ArgShowFetchToolWindow, StringComparison.OrdinalIgnoreCase))
                {
                    action = new Action(() => ShowToolFetchWindow());

                    minimizeApp = true;
                }
            }

            if (minimizeApp)
            {
                Task.Delay(5).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        MainWindow.WindowState = WindowState.Minimized;
                        action!(); // works around issues with ordering..
                    }));
                }
                );
            }


            // If runtime window is to be shown, show it now..
            if (Settings.Default.RuntimeWindowVisible)
            {
                ShowRuntimeWindow();
            }

            base.OnStartup(e);
        }

        void HandleException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Unhandled exception! App is going to crash...bye.");
            // e.Handled = true;

        }

        protected override void OnExit(ExitEventArgs e)
        {
            ConnectionManager.Instance.OnExit();

        }
    }   

}
