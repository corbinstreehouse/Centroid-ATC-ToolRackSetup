using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using WpfSingleInstanceByEventWaitHandle;

namespace ToolRackSetup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 



    public partial class App : Application
    {

        private static Assembly CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            // Extract the assembly name
            string assemblyName = new AssemblyName(args.Name).Name;

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


        protected override void OnStartup(StartupEventArgs e)
        {
            WpfSingleInstance.Make("ToolRackSetup");
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
