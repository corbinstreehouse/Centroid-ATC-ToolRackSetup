using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using WpfSingleInstanceByEventWaitHandle;

namespace ToolRackSetup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        
        protected override void OnStartup(StartupEventArgs e)
        {
            WpfSingleInstance.Make("ToolRackSetup");
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(HandleException);
            base.OnStartup(e);
        }

        void HandleException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Unhandled exception! App may be unstable...restart it.");
            e.Handled = true;

        }
    }

}
