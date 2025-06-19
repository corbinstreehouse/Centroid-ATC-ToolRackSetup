using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CentroidAPI;

namespace ToolRackSetup
{
    class ConnectionManager
    {
        private CNCPipe _pipe;
        public CNCPipe Pipe { get => _pipe; }

        private ParameterSettings _parameterSettings;
        public ParameterSettings Parameters { get => _parameterSettings; }

        private ToolController? _toolController;

        public ToolController ToolController
        {
            get
            {
                if (_toolController == null)
                {
                    // Suppose the controller could just access these properties now...but isolation is kind of nice.
                    _toolController = new ToolController(_pipe, _parameterSettings);
                }
                return _toolController;
            }

        }

        

        public ConnectionManager()
        {
            _pipe = new CNCPipe();

            // Wait for cnc12_pipe to be constructed before continuing.
            while (!_pipe.IsConstructed())
            {
                const string messageBoxText = "Can't connect to CNC12. Make sure it is running! Would you like to retry the connection?";
                const string messageBoxTitle = "Error Connecting to CNC12!";
                var messageBoxType = MessageBoxButton.YesNo;

                MessageBoxResult selection = MessageBox.Show(messageBoxText, messageBoxTitle, messageBoxType);

                switch (selection)
                {
                    case MessageBoxResult.Yes:
                        _pipe = new CNCPipe();
                        break;

                    case MessageBoxResult.No:
                        Environment.Exit(0);
                        break;
                }
            }

            _pipe.message_window.AddMessage("Tool Manager Connected");
            _parameterSettings = new ParameterSettings(_pipe);

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = GetPollingTimeInterval(); // 1 second polling
            dispatcherTimer.Start();

        }

        private static TimeSpan GetPollingTimeInterval()
        {
            return new TimeSpan(0, 0, 0, 1);
        }

        private void dispatcherTimer_Tick(object? sender, EventArgs e)
        {
            CNCPipe.Plc.IOState plcState;
            var rc = Pipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_JOB_IN_PROGRESS, out plcState);
            if (rc == CNCPipe.ReturnCode.ERROR_PIPE_IS_BROKEN)
            {
                // Hard quit; nothing we can do..
                Environment.Exit(0);
            }
            _toolController?.RefreshActiveTool();
        }

        public void OnExit()
        {
            _pipe.message_window.AddMessage("Tool Manager Disconnected");
        }

        ~ConnectionManager()
        {
        }

        private static ConnectionManager? _instance;
        public static ConnectionManager Instance
        {
            get
            {
                if (_instance== null)
                {
                    _instance = new ConnectionManager();
                }
                return _instance;
            }
        }


    }
}
