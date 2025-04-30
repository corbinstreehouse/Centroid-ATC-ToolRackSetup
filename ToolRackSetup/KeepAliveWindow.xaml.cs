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
using System.Windows.Shapes;

namespace ToolRackSetup
{
    /// <summary>
    /// Interaction logic for KeepAliveWindow.xaml
    /// </summary>
    public partial class KeepAliveWindow : Window
    {
        public KeepAliveWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((App)App.Current).ShowToolManagerWindow();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            /// message is never shown...I need to move it somewhere else...
//            ConnectionManager.Instance.Pipe.message_window.AddMessage("Tool Manager Disconnected");
            ((App)App.Current).CloseAllWindows();
        }

        private void Window_Activated(object sender, EventArgs e)
        {

        }
    }
}
