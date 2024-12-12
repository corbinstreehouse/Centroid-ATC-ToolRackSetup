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
        private CNCPipe pipe;
        private ToolController toolController;
        public FetchToolPopup(CNCPipe pipe, ToolController toolController)
        {
            this.pipe = pipe;
            this.toolController = toolController;
            InitializeComponent();
            lstvwPockets.ItemsSource = toolController.ToolPocketItems;
        }

        private bool _closing = false;


        public void Popup()
        {

            _closing = false;
            // Figure out our location..
            //int x, y = 0;
            //int width, height = 0;
            //pipe.state.GetScreenPosition(out x, out y);
            //pipe.state.GetScreenSize(out width, out height);
            //System.Diagnostics.Debug.WriteLine("{0}", x);            
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
    }
}
