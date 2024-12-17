using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CentroidAPI;
using Point = System.Windows.Point;

namespace ToolRackSetup
{

    /// <summary>
    /// Interaction logic for FetchToolWindow.xaml
    /// </summary>
    public partial class FetchToolPopup : Window
    {
        public FetchToolPopup()
        {
            InitializeComponent();
            lstvwPockets.ItemsSource = ConnectionManager.Instance.ToolController.ToolPocketItems;
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Hide();
            }
        }

        private bool _closing = false;

        public void Popup()
        {

            _closing = false;
     
            Point p = MouseUtilities.GetMousePos();

            System.Diagnostics.Debug.WriteLine("{0}", p);
            Left = p.X - Width / 2.0;
            Top = p.Y;
            Topmost = true;
            Show();
            //SizeToContent();
            Activate();
        }

        

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!_closing) 
            {
                Hide();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private ToolInfo? ToolInfoFromSender(object sender)
        {
            Button? b = sender as Button;
            ToolInfo? toolInfo = null;
            if (b?.DataContext is ToolPocketItem)
            {
                ToolPocketItem item = (ToolPocketItem)b.DataContext;
                toolInfo = item.ToolInfo;
            }
            else if (b?.DataContext is ToolInfo)
            {
                toolInfo = (ToolInfo)b.DataContext;
            }
            return toolInfo;
        }

        private void FetchButtonClick(object sender, RoutedEventArgs e)
        {
            ToolInfo? toolInfo = ToolInfoFromSender(sender);
            if (toolInfo != null)
            {
                // Do a m6 on it..
                CNCPipe.Job job = new CNCPipe.Job(ConnectionManager.Instance.Pipe);
                String command = String.Format("M6 T{0}\nG43 H{0}", toolInfo.Number);
                job.RunCommand(command, "c:\\cncm", false);
            }
            Hide();
        }
    }
}
