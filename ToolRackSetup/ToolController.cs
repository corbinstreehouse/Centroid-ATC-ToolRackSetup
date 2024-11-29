using System;
using CentroidAPI;
using static CentroidAPI.CNCPipe.Parameter;
using static CentroidAPI.CNCPipe;
using static CentroidAPI.CNCPipe.Tool;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Xml.Linq;
using System.Configuration;

namespace ToolRackSetup {

    public class ToolInfo : ObservableObject
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
            _diameter = info.diameter_offset;
            _heightNumber = info.h_number;
            _diameterNumber = info.d_number;

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
                    CheckForError(_pipe.tool.SetBinNumber(this.Number, value), "Setting tool pocket");
                    SetProperty(ref _pocket, value);
                }
            }
        }

        public bool IsInSpindle
        {
            get => _isInSpindle;
            set => SetProperty(ref _isInSpindle, value);

        }


        private void CheckForError(ReturnCode code, string op)
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
                    CheckForError(_pipe.tool.SetToolHNumber(this.Number, value), "Setting tool height number");
                    SetProperty(ref _heightNumber, value);
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
                    CheckForError(_pipe.tool.SetToolHeightOffsetAmout(this.Number, value), "Setting tool height");
                    SetProperty(ref _heightOffset, value);
                }
            }

        }

        int _diameterNumber;
        public int DiameterNumber
        {
            get => _diameterNumber;
            set
            {
                if (value != _diameterNumber)
                {
                    CheckForError(_pipe.tool.SetToolDNumber(this.Number, value), "Setting tool diameter number");
                    SetProperty(ref _diameterNumber, value);

                }
            }
        }

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
                    CheckForError(_pipe.tool.SetToolDiameterOffsetAmout(this.Number, value), "Setting tool diameter");
                    SetProperty(ref _diameter, value);  
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
                    CheckForError(_pipe.tool.SetToolDescription(this.Number, value), "Setting tool description");
                    SetProperty(ref _description, value);   
                }
            }
        }
    }

    public class ToolController : ObservableObject
	{
        private CNCPipe _pipe;
        ObservableCollection<ToolInfo> _toolInfoLibrary;
        private ToolInfo ?_activeTool;

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

        private const string macroPathTemplate = "C:\\cncm\\CorbinsWorkshop\\{0}";


        // seperate setter so we can change the active tool based on setting it, instead of changing the numbrer of the tool
        public int ActiveToolNumber
        {
            get
            {
                if (_activeTool != null) return _activeTool!.Number;

                return 0;
            } 
            set
            {


                if (value != this.ActiveToolNumber)
                {

                    // Just do a set and measure if needed
                    CNCPipe.Job job = new CNCPipe.Job(_pipe);

                    string filename1 = string.Format(macroPathTemplate, "tool_set.cnc");
                    string gcode = String.Format("G65 \"{0}\" T{1}\n", filename1, value);
                    string filename2 = string.Format(macroPathTemplate, "tool_measure_if_needed.cnc");
                    string gcode2 = String.Format("G65 \"{0}\" T{1}", filename2, value);

                    job.RunCommand(String.Format("{0}\n{1}", gcode, gcode2), "c:\\cncm", false);

                    Task.Delay(1000).ContinueWith(_ =>
                    {
                        // Update the active tool numbers...maybe on a delay to let the work get done?
                        RefreshActiveTool();                        
                    } );
                }
            }
        }
 

        public ToolInfo? ActiveTool
        {
            get
            {
                return _activeTool;
            }
            set
            {
                if (_activeTool != value)
                {
                    if (_activeTool != null)
                    {
                        _activeTool.IsInSpindle = false;
                    }
                    SetProperty(ref _activeTool, value);
                    OnPropertyChanged(nameof(ActiveToolNumber));
                    if (_activeTool != null)
                    {
                        _activeTool.IsInSpindle = true;
                    }
                }
            }
        }

        private void RefreshActiveTool()
        {
            int toolInSpindle = (int)_pipe.parameter.GetValue(ParameterKey.CurrentToolNumber);
            ActiveTool = toolInSpindle > 0 ? _toolInfoLibrary[toolInSpindle - 1] : null;
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
            ToolInfo? activeTool = null;

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
                if (item.IsInSpindle)
                {
                    activeTool = item;
                }

            }
            this.ActiveTool = activeTool;


        }

    }

}