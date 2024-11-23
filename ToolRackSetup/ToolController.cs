using System;
using CentroidAPI;
using static CentroidAPI.CNCPipe.Parameter;
using static CentroidAPI.CNCPipe;
using static CentroidAPI.CNCPipe.Tool;
using System.Collections.ObjectModel;

namespace ToolRackSetup {

    public class ToolInfo : NotifyingObject
    {
        private CNCPipe _pipe;

        public ToolInfo(CNCPipe pipe, Info info)
        {
            _pipe = pipe;
            this.Number = info.number;
            _pocket = info.bin;
            _heightOffset = info.height_offset;
            _description = info.description;
            _isInSpindle = false;
        }
        public int Number { get; }

        private int _pocket = -1;
        public int Pocket
        {
            get { return _pocket; }
            set
            {
                if (_pocket != value)
                {
                    CheckForeError(_pipe.tool.SetBinNumber(this.Number, value), "Setting tool pocket");
                    _pocket = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsInSpindle
        {
            get
            {
                return _isInSpindle;
            }
            set
            {
                if (_isInSpindle != value)
                {
                    _isInSpindle = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private void CheckForeError(ReturnCode code, string op)
        {
            if (code != ReturnCode.SUCCESS)
            {

                string reason = code.ToString();
                string eMsg = String.Format("Error: {0}.\nEnsure you are not running a job!\nError: {2}", op, reason);
                throw new Exception(eMsg);
            }
        }

        int _heightNumber;
        public int HeightNumber
        {
            get { return _heightNumber; }
            set
            {
                if (_heightNumber != value)
                {
                    CheckForeError(_pipe.tool.SetToolHNumber(this.Number, value), "Setting tool height number");
                    _heightNumber = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double HeightOffset
        {
            get
            {
                return _heightOffset;
            }
            set
            {
                if (_heightOffset != value)
                {
                    CheckForeError(_pipe.tool.SetToolHeightOffsetAmout(this.Number, value), "Setting tool height");
                    _heightOffset = value;
                    NotifyPropertyChanged();
                }
            }

        }

        //public int d_number;

        public double Diameter
        {
            get
            {
                return _diameter;
            }
            set
            {
                if (value != _diameter)
                {
                    CheckForeError(_pipe.tool.SetToolDiameterOffsetAmout(this.Number, value), "Setting tool diameter");
                    _diameter = value;
                    NotifyPropertyChanged();
                }
            }

        }

        //public Coolant coolant;

        //public SpindleDirection spindle_direction;

        //public int spindle_speed;

        private string _description;
        private bool _isInSpindle;
        private double _heightOffset;
        private double _diameter;

        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    CheckForeError(_pipe.tool.SetToolDescription(this.Number, value), "Setting tool description");
                    _description = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }

    public class ToolController
	{
        private CNCPipe _pipe;
        ObservableCollection<ToolInfo> _toolInfoLibrary;

        public ToolController(CNCPipe pipe)
		{
            _pipe = pipe;
            _toolInfoLibrary = new ObservableCollection<ToolInfo>();

        }

        public ObservableCollection<ToolInfo> Tools
        {
            get { return _toolInfoLibrary; }
        }

        public ToolInfo? FindToolInfoForPocket(int pocketIndex)
        {

            // this is not the right way to do this; I should sort _toolInfoLibrary once into a new variable by bin, but this should be fast enough for finding stuff.
            foreach (ToolInfo toolInfo in _toolInfoLibrary)
            {
                if (toolInfo.Pocket == pocketIndex)
                {
                    return toolInfo;
                }

            }
            return null;

        }

        public void RefreshTools()
        {

            List<Info> toolLibrary;
            _pipe.tool.GetToolLibrary(out toolLibrary);
            int toolBinCount = (int)_pipe.parameter.GetValue(ParameterKey.MaxToolBins);

            HashSet<int> usedPockets = new HashSet<int>();

            // Pockets are pointing to tools; it would be better to just update the existing ones if they are there..
            // going to assume the tool count won't ever change..
            bool useExisting = _toolInfoLibrary.Count > 0;

            int toolInSpindle = (int)_pipe.parameter.GetValue(ParameterKey.CurrentToolNumber);

            for (int i = 0; i < toolLibrary.Count; i++)
            {
                ToolInfo item = useExisting ? _toolInfoLibrary[i] : new ToolInfo(_pipe, toolLibrary[i]);


                if (!useExisting) _toolInfoLibrary.Add(item);
                //  System.Diagnostics.Debug.WriteLine("Tool: {0} bin: {1}, {2}", toolLibrary[i].number, toolLibrary[i].bin, toolLibrary[i].description);
                if (item.Pocket > toolBinCount)
                {
                    item.Pocket = -1;
                }
                else if (item.Pocket > 0)
                {
                    if (usedPockets.Contains(item.Pocket))
                    {
                        // Can't have two tools in one pocket!
                        item.Pocket = -1;
                    }
                    else
                    {
                        usedPockets.Add(item.Pocket);
                    }
                }
                item.IsInSpindle = item.Number == toolInSpindle;

            }

        }

    }

}