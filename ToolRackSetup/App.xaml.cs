using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

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
            if (AlreadyProcessedOnThisInstance)
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
                app.ShowToolWindow();
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



        private void ShowToolWindow()
        {
            // Currently the main window..
            Window w = Application.Current.MainWindow;
            w.Activate();
            // force it up...the batch file opening it doesn't always work due to it closing
            w.Topmost = true;
            w.Topmost = false;
        }

        private FetchToolPopup? _fetchToolPopup;
        private void ShowToolFetchWindow()
        {
            // TODO: keep the pipe and tool controller somewhere else (IE: globals or here)
            MainWindow mw = (MainWindow)this.MainWindow;
            if (_fetchToolPopup == null)
            {
                _fetchToolPopup = new FetchToolPopup(mw._pipe, mw._ToolController);
            }

            _fetchToolPopup.Popup();
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            MakeSingleInstance();

            // TODO: commonize this code a bit more..
            string[] args = Environment.GetCommandLineArgs();
            for (int index = 1; index < args.Length; index += 1)
            {
                if (String.Equals(args[index], ArgShowToolWindow, StringComparison.OrdinalIgnoreCase))
                {
                    // ShowToolFetchWindow // currently the default
                }
                if (String.Equals(args[index], ArgShowFetchToolWindow, StringComparison.OrdinalIgnoreCase))
                {
                    ShowToolFetchWindow();
                }
            }

            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(HandleException);
            base.OnStartup(e);
        }

        void HandleException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Unhandled exception! App is going to crash...bye.");
            // e.Handled = true;

        }

    }

}
