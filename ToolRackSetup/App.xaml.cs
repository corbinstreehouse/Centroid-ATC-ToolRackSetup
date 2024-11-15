using System.Configuration;
using System.Data;
using System.Windows;
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

            base.OnStartup(e);
        }
    }

}
