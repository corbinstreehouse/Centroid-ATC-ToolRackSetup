using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using static CentroidAPI.CNCPipe;
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
        

        private void AdjustSizeToFitScreen()
        {
            // Make sure it doesn't go taller than the screen
            double maxY = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
            double windowMaxY = this.Top + this.Height;

            if (windowMaxY > maxY)
            {
                double diff = windowMaxY - maxY;
                this.SizeToContent = SizeToContent.Manual;
                Height = Height - diff - 20;
            }
        }



        public void Popup()
        {
            ConnectionManager.Instance.ToolController.RefreshTools();
            Point p = MouseUtilities.GetMousePos();

            Left = p.X - Width / 2.0;
            Top = p.Y;
            Topmost = false;
            Topmost = true;
            this.SizeToContent = SizeToContent.Height;

            AdjustSizeToFitScreen();

            Show();


            Activate();
            // Sometimes it isn't active...if we do this in a delay it seems to fix that. Super hacky.
            // Original delay was 10ms, and I increased it to 100ms as I still was seeing it inactive.
            Task.Delay(100).ContinueWith(_ =>
            {
                if (Visibility == Visibility.Visible)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Topmost = false;
                        Topmost = true;
                        Activate();
                    }
                    ));
                    
                }
            });
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // NOTE: when closed we need to delete the shared instance!
            ((App)App.Current).DropFetchToolPopupInstance();

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AdjustSizeToFitScreen();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CNCPipe.Job job = new CNCPipe.Job(ConnectionManager.Instance.Pipe);
            String command = String.Format("G65 \"c:\\cncm\\CorbinsWorkshop\\tool_fetch.mac\"");
            job.RunCommand(command, "c:\\cncm", false);
            Hide();
        }

        private void Window_Activated(object sender, EventArgs e)
        {

        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // Testing..corbin
         //   Debug.WriteLine("Content Rendered");
        }
    }
}
