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
        }

        private void window_Closed(object sender, EventArgs e)
        {
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
