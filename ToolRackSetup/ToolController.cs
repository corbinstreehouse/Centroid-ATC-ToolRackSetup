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

    public enum PocketStyle
    {
        XMinus = 0,
        XPlus = 1,
        YMinus = 2,
        YPlus = 3,
        Hole = 4,
    }

    public class ToolInfo : ObservableObject
    {
        private CNCPipe _pipe;

        public ToolInfo(CNCPipe pipe, Info info)
        {
            _pipe = pipe;
            this.Number = info.number;
            _isInSpindle = false;
            RefreshFromInfo(info);
        }

        public void RefreshFromInfo(Info info)
        {

            OnPropertyChanging(nameof(this.Diameter));
            OnPropertyChanging(nameof(this.HeightNumber));
            OnPropertyChanging(nameof(this.HeightOffset));
            OnPropertyChanging(nameof(this.DiameterNumber));
            OnPropertyChanging(nameof(this.Description));
            OnPropertyChanging(nameof(this.Pocket));

            _pocket = info.bin;
            _heightOffset = info.height_offset;
            _description = info.description;
            _diameter = info.diameter_offset;
            _heightNumber = info.h_number;
            _diameterNumber = info.d_number;

            OnPropertyChanged(nameof(this.Diameter));
            OnPropertyChanged(nameof(this.HeightNumber));
            OnPropertyChanged(nameof(this.HeightOffset));
            OnPropertyChanged(nameof(this.DiameterNumber));
            OnPropertyChanged(nameof(this.Description));
            OnPropertyChanged(nameof(this.Pocket));
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

        private string _description = "";
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

    public class ToolPocketItem : ObservableObject
    {
        private double x;
        private double y;
        private double z;
        private PocketStyle style = PocketStyle.YPlus;

        public int Pocket { get; }

        public double X
        {
            get => x; set
            {
                SetProperty(ref x, value);
            }
        }
        public double Y { get => y; set { SetProperty(ref y, value); } }
        public double Z { get => z; set { SetProperty(ref z, value); } }
        public PocketStyle Style
        {
            get { return style; }
            set
            {
                SetProperty(ref style, value);
            }
        }

        private ToolInfo? _toolInfo = null;
        public ToolInfo? ToolInfo
        {
            get
            {
                return _toolInfo;
            }

            set
            {
                if (_toolInfo != value)
                {
                    OnPropertyChanging(nameof(IsToolEnabled));
                    OnPropertyChanging(nameof(FetchButtonTitle));
                    SetProperty(ref _toolInfo, value);
                    OnPropertyChanged(nameof(IsToolEnabled));
                    OnPropertyChanged(nameof(FetchButtonTitle));

                }
            }
        }

        public bool IsToolEnabled
        {
            get { return _toolInfo != null; }
        }

        public string FetchButtonTitle
        {
            get
            {
                if (_toolInfo != null)
                {
                    return String.Format("T{0} - {1}", _toolInfo.Number, _toolInfo.Description);
                } else
                {
                    return "Empty";
                }
            }

        }


        private ToolController _toolController; // weak reference? loops and GC?

        public ToolPocketItem(int pocket, ToolInfo? toolInfo, ToolController toolController)
        {
            Pocket = pocket;
            ToolInfo = toolInfo;
            _toolController = toolController;
        }

        public int ToolNumber
        {
            get
            {
                if (ToolInfo != null)
                {
                    return ToolInfo.Number;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                int oldToolNumber = this.ToolNumber;
                if (value != oldToolNumber)
                {
                    OnPropertyChanging();
                    // Do some quick validation
                    if (value > 0)
                    {
                        // If we have a valid new tool index (should check max 200 too)
                        ToolInfo tInfo = _toolController.Tools[value - 1];
                        // If it is already in a pocket, just move it here
                        if (tInfo.Pocket > 0)
                        {
                            throw new Exception(String.Format("Remove tool from pocket {0} before assigning to another pocket", tInfo.Pocket));
                        }
                    }

                    if (ToolInfo != null)
                    {
                        // We have to unassociate the old one first
                        ToolInfo.Pocket = -1;
                        ToolInfo = null;
                    }
                    if (value > 0)
                    {
                        // If we have a valid new tool index (should check max 200 too)
                        ToolInfo = _toolController.Tools[value - 1];
                        ToolInfo.Pocket = Pocket;
                    }
                    OnPropertyChanged();
                }
            }
        }


    }

    public class ToolController : ObservableObject
	{
        private CNCPipe _pipe;
        private ObservableCollection<ToolInfo> _toolInfoLibrary;
        private ToolInfo ?_activeTool;
        private ObservableCollection<ToolPocketItem> _toolPocketItems;
        private ParameterSettings _parameterSettings;

        public ToolController(CNCPipe pipe, ParameterSettings parameterSettings)
		{
            _pipe = pipe;
            _parameterSettings = parameterSettings;
            _toolInfoLibrary = new ObservableCollection<ToolInfo>();
            _toolPocketItems = new ObservableCollection<ToolPocketItem>();
            RefreshTools();
            InitializeToolPocketItems();
        }

        public ObservableCollection<ToolPocketItem> ToolPocketItems { get { return _toolPocketItems; } }

        private void InitializeToolPocketItems()
        {
            int toolBinCount = _parameterSettings.PocketCount;
            for (int i = 1; i <= toolBinCount; i++)
            {
                ToolInfo? toolInfo = FindToolInfoForPocket(i);
                ToolPocketItem tpi = new ToolPocketItem(i, toolInfo, this);
                _toolPocketItems.Add(tpi);
            }
        }

        public void RefreshToolPocketItems()
        {
            RefreshTools();
            _toolPocketItems.Clear();
            InitializeToolPocketItems();
        }

        public void RemoveLastPocket()
        {
            if (_toolPocketItems.Count > 0)
            {
                // Attempt the API call first; if it fails an exception will be thrown and our UI won't update and have a bad state
                int lastIndex = _toolPocketItems.Count - 1;
                _parameterSettings.PocketCount = lastIndex;

                ToolPocketItem? item = _toolPocketItems.Last();
                item.ToolNumber = 0; /// make sure it doesn't have a tool..
                _toolPocketItems.RemoveAt(lastIndex);
            }
        }

        public ToolPocketItem AddToolPocket()
        {
            // Make sure we aren't running first!! otherwise we get to a strange state. Attempting to set the pocket count will do it.
            int newCount = _toolPocketItems.Count + 1;
            _parameterSettings.PocketCount = newCount;

            ToolPocketItem tpi = new ToolPocketItem(newCount, null, this);
            if (_toolPocketItems.Count > 0)
            {
                ToolPocketItem last = _toolPocketItems.Last()!;

                tpi.X = last.X;
                tpi.Y = last.Y;
                tpi.Z = last.Z;
                tpi.Style = last.Style;
            }

            _toolPocketItems.Add(tpi);
            return tpi;
        }


        public ObservableCollection<ToolInfo> Tools
        {
            get { return _toolInfoLibrary; }
        }

        private ToolInfo? FindToolInfoForPocket(int pocketIndex)
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

        public void RefreshActiveTool()
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

                if (!useExisting) _toolInfoLibrary.Add(item); else item.RefreshFromInfo(toolLibrary[i]);

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