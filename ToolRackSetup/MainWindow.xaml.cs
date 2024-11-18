﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CentroidAPI;
using System.Xml.Linq;
using System.IO;
using static CentroidAPI.CNCPipe.Tool;
using System.Xml;
using System.Xml.XPath;
using System.Globalization;
using System.Runtime;
using System.Linq.Expressions;
using static CentroidAPI.CNCPipe;
using static CentroidAPI.CNCPipe.State;
using Microsoft.VisualBasic;

namespace ToolRackSetup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public enum ParameterKey
    {
        // System parameters
        HasATC = 6, // Currentlly needs to be 0
        HasEnhancedATC = 160, // Currently needs to be 0, can have variuous bits set
        MaxToolBins = 161, // Read/written

        // Custom parameters by Avid
        ShouldCheckAir = 724, // Avid setting that I read and write

        // Custom parameters by Corbin
        ATCToolOptions = 776, // bitset, see ATCToolOptions enum

        //  EnableVirtualDrawbar = 777, // virtual drawbar button support; prefer to be 0. (corbin)
        SpindleWaitTime = 778, // spindle wait time, in seconds (corbin)
    }

    // Bitset values
    public enum ATCToolOptions
    {
        EnableATC = 1,  // 1 if it is enabled; checked in some code for clearing the tool table
        TurnOffRelay2WithSpindle = 2, // Turn off relay 2 with the spindle instead of the end of the job
        EnableVirtualDrawbar = 4, // Enable vitual drawbar button
    }

    public static class CNCPipeExtensions
    {

        public static bool GetToolOptionValue(this CNCPipe.Parameter parameter, ATCToolOptions option)
        {
            return parameter.GetBitValue((int)ParameterKey.ATCToolOptions, (int)option);
        }
        public static void SetToolOptionValue(this CNCPipe.Parameter parameter, ATCToolOptions option, bool value)
        {
            parameter.SetBitValue((int)ParameterKey.ATCToolOptions, (int)option, value);
        }

        public static void SetBitValue(this CNCPipe.Parameter parameter,
            int parameter_num, int bit, bool onOrOff)
        {
            // Get the existing value and turn on/off the appropriate bit
            int value = (int)parameter.GetValue(parameter_num);
            if (onOrOff)
            {
                value = value | bit;
            } else
            {
                value = value & ~bit;
            }

            parameter.SetValue(parameter_num, value);
        }


        public static bool GetBitValue(this CNCPipe.Parameter parameter, int parameterNum, int bit)
        {
            int value = (int)parameter.GetValue(parameterNum);
            return (value & bit) == bit;
        }


        // Throws an exception on error!
        public static void SetValue(this Parameter parameter, int parameter_num, double value)
        {
            CNCPipe.ReturnCode rc = parameter.SetMachineParameter(parameter_num, value);
            if (rc != CNCPipe.ReturnCode.SUCCESS)
            {
                string reason = rc.ToString();
                string eMsg = String.Format("Failed to set machine parameter {0} to {1}.\nEnsure you are not running a job!\nError: {2}", parameter_num, value, reason);
                throw new Exception(eMsg);
            }
        }

        public static void SetValue(this Parameter parameter, ParameterKey parameterKey, double value)
        {
            parameter.SetValue((int)parameterKey, value);
        }

        public static void SetValue(this Parameter parameter, ParameterKey parameterKey, bool value)
        {
            parameter.SetValue((int)parameterKey, Convert.ToDouble(value));
        }

        public static double GetValue(this CNCPipe.Parameter p, int parameterNum)
        {
            double value = 0;
            CNCPipe.ReturnCode rc = p.GetMachineParameterValue(parameterNum, out value);
            if (rc != CNCPipe.ReturnCode.SUCCESS)
            {
                string reason = rc.ToString();
                string eMsg = String.Format("Failed to get machine parameter {0}.\nEnsure that CNC12 is running!\nError: {2}", parameterNum, reason);
                throw new Exception(eMsg);
            }
            return value;
        }
        public static double GetValue(this CNCPipe.Parameter p, ParameterKey parameterKey)
        {
            return p.GetValue((int)parameterKey);
        }

        public static bool GetBoolValue(this CNCPipe.Parameter p, ParameterKey parameterKey)
        {
            double d = p.GetValue(parameterKey);
            return System.Convert.ToBoolean(d);
        }

        public static int GetToolBinCount(this Parameter p) //can't be a property uet
        {
            return (int)p.GetValue(ParameterKey.MaxToolBins);
        }

        public static void SetToolBinCount(this Parameter p, double value)
        {
            p.SetValue(ParameterKey.MaxToolBins, value);
        }
    }

    public class ClickSelectTextBox : TextBox
    {
        public ClickSelectTextBox()
        {
            AddHandler(PreviewMouseLeftButtonDownEvent,
              new MouseButtonEventHandler(SelectivelyIgnoreMouseButton), true);
            AddHandler(GotKeyboardFocusEvent,
              new RoutedEventHandler(SelectAllText), true);
            AddHandler(MouseDoubleClickEvent,
              new RoutedEventHandler(SelectAllText), true);
        }

        private static void SelectivelyIgnoreMouseButton(object sender,
                                                         MouseButtonEventArgs e)
        {
            // Find the TextBox
            DependencyObject parent = e.OriginalSource as UIElement;
            while (parent != null && !(parent is TextBox))
                parent = VisualTreeHelper.GetParent(parent);

            if (parent != null)
            {
                var textBox = (TextBox)parent;
                if (!textBox.IsKeyboardFocusWithin)
                {
                    // If the text box is not yet focussed, give it the focus and
                    // stop further processing of this click event.
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private static void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null)
                textBox.SelectAll();
        }
    }
    public class MyPropertyChangedEventArgs: PropertyChangedEventArgs
    {
        public Object? oldValue { get; }
        public MyPropertyChangedEventArgs(object? oldValue, string propertyName) : base(propertyName)
        {
            this.oldValue = oldValue;
        }
    }

    public class ToolInfo
    {
        private CNCPipe _pipe;

        public ToolInfo(CNCPipe pipe, Info info)
        {
            _pipe = pipe;
            this.Number = info.number;
            _pocket = info.bin;
            this.HeightOffset = info.height_offset;
            _description = info.description;
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
                    _pocket = value;
                    CheckForeError(_pipe.tool.SetBinNumber(this.Number, _pocket), "Setting tool pocket");
                }
            }
        }

        private void CheckForeError(ReturnCode code, string op)
        {
            if ( code != ReturnCode.SUCCESS)
            {

                string reason = code.ToString();
                string eMsg = String.Format("Error: {0}.\nEnsure you are not running a job!\nError: {2}", op, reason);
                throw new Exception(eMsg);              
            }
        }

        int _heightNumber;
        public int HeightNumber
        {
            get { return _heightNumber;  }
            set
            {
                if (_heightNumber != value)
                {
                    _heightNumber = value;
                    CheckForeError(_pipe.tool.SetToolHNumber(this.Number, _heightNumber), "Setting tool height");
                }
            }
        }

        public double HeightOffset { get; }

        //public int d_number;

        //public double diameter_offset;

        //public Coolant coolant;

        //public SpindleDirection spindle_direction;

        //public int spindle_speed;

        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    CheckForeError(_pipe.tool.SetToolDescription(this.Number, _description), "Setting tool description");
                }
            }
        }

    }

    public static class Extensions
    {

        public static ToolInfo? FindToolInfoForPocket(this ObservableCollection<ToolInfo> list, int pocketIndex)
        {
            // this is not the right way to do this; I should sort _toolLibrary once into a new variable by bin, but this should be fast enough for finding stuff.
            foreach (ToolInfo toolInfo in list)
            {
                if (toolInfo.Pocket == pocketIndex)
                {
                    return toolInfo;
                }

            }
            return null;
        }
    }

    public enum PocketStyle
    {
        XMinus = 0,
        XPlus = 1,
        YMinus = 2,
        YPlus = 3,
        //Hole = 4, // not implemented
    }

    public class PocketStyleConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PocketStyle)
            {
                PocketStyle ps = (PocketStyle)value;
                return ((int)ps);
            }
            PocketStyle thisStyle = (PocketStyle)Enum.Parse(typeof(PocketStyle), (string)value, true);
            
            return thisStyle;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
            {
                int v = (int)value;
                PocketStyle ps  = (PocketStyle)v;
                return ps;
            }
            return PocketStyle.XPlus;
        }
    }


    public  class NotifyingObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(object? oldValue, [CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new MyPropertyChangedEventArgs(oldValue, propertyName));
        }
        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new MyPropertyChangedEventArgs(null, propertyName));
        }
    }

    // TODO: Seperate to ParameterSettings and PocketSettings
    public class ToolChangeSettings : NotifyingObject
    {
        CNCPipe _pipe;

        private bool enableTestingMode = true;
        private double zBump = 0.005; // May need to be larger for CNC depot
        private double testingFeed = 200;
        private double zClearance = 0; //unused right now
        private double _spindleWaitTime = 12.0; // seconds
        private bool _shouldCheckAirPressure = true;
        private double slideDistance = 1.4; // Default value
        private double rackAdjustment = 5.5;
        private bool _enableVirtualDrawbar = false; // If true, we do more stuff

        public bool EnableTestingMode { get => enableTestingMode; 
            set { 
                enableTestingMode = value; NotifyPropertyChanged(); 
            } 
        }
        public double ZBump { get => zBump; set { zBump = value; NotifyPropertyChanged();  } }
        public double ZClearance { get => zClearance; set { zClearance = value; NotifyPropertyChanged(); } }
        public double SpindleWaitTime { get => _spindleWaitTime; 
            set
            {
                if (_spindleWaitTime != value)
                {
                    _spindleWaitTime = value;
                    _pipe.parameter.SetValue(ParameterKey.SpindleWaitTime, _spindleWaitTime);
                    NotifyPropertyChanged();
                }
            }
        }

        public double TestingFeed { get => testingFeed; set { testingFeed = value; NotifyPropertyChanged(); } }

        public bool ShouldCheckAirPressure { 
            get => _shouldCheckAirPressure; 
            set {
                if (_shouldCheckAirPressure != value) {     
                  _shouldCheckAirPressure = value;

                    _pipe.parameter.SetValue(ParameterKey.ShouldCheckAir,_shouldCheckAirPressure);
                    NotifyPropertyChanged(); 
                   
                }
            
            } 
        }

        bool _enableATC = false;
        public bool EnableATC { get => _enableATC; set
            {
                if (_enableATC != value) { _enableATC = value;

                    _pipe.parameter.SetToolOptionValue(ATCToolOptions.EnableATC, _enableATC);
                    NotifyPropertyChanged(); 
                }

            }
        }

        public bool EnableVirtualDrawbar
        { 
            get => _enableVirtualDrawbar;
            set {
                if (_enableVirtualDrawbar != value)
                {
                    _enableVirtualDrawbar = value;
                    _pipe.parameter.SetToolOptionValue(ATCToolOptions.EnableVirtualDrawbar, _enableVirtualDrawbar);
                    NotifyPropertyChanged();
                }
            } 
        }


        public double SlideDistance
        {
            get => slideDistance;
            set
            {
                if (slideDistance != value)
                {
                    slideDistance = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double RackOffset { get => rackAdjustment;
            set { rackAdjustment = value; NotifyPropertyChanged(); }
        }

        public ToolChangeSettings(CNCPipe pipe)
        {
            _pipe = pipe;
            _enableATC = _pipe.parameter.GetToolOptionValue(ATCToolOptions.EnableATC);
            _shouldCheckAirPressure = _pipe.parameter.GetBoolValue(ParameterKey.ShouldCheckAir);
            _spindleWaitTime = _pipe.parameter.GetValue(ParameterKey.SpindleWaitTime);
            // if not set, default it to 12
            if (_spindleWaitTime <= 0)
            {
                _spindleWaitTime = 12;
            }
            _enableVirtualDrawbar = _pipe.parameter.GetToolOptionValue(ATCToolOptions.EnableVirtualDrawbar);
        }
    }

    // I suppose this is the model object
    public class ToolPocketItem : NotifyingObject
    {
        private double x;
        private double y;
        private double z;
        private PocketStyle style = PocketStyle.YPlus;

        public int Pocket { get; }

        public double X { get => x; set { object old = x;  x = value; NotifyPropertyChanged(old); } }
        public double Y { get => y; set { object old = y; y = value; NotifyPropertyChanged(old); } }
        public double Z { get => z; set { object old = z; z = value; NotifyPropertyChanged(old); } }
        public PocketStyle Style
        {
            get { return style;  }
            set
            {
                if (value != style)
                {
                    object old = value;
                    style = value;
                    NotifyPropertyChanged(old);
                }
            }
        }

        private ToolInfo? _toolInfo = null;
        public ToolInfo? ToolInfo {
            get
            {
                return _toolInfo;
            }

            set
            {
                if (_toolInfo != value)
                {
                    object ?old = _toolInfo;
                    _toolInfo = value;
                    
                    NotifyPropertyChanged(old);
                    NotifyPropertyChanged(nameof(IsToolEnabled));
                }

            }
        }

        public bool IsToolEnabled
        {
            get { return _toolInfo != null;  }
        }

        private ObservableCollection<ToolInfo> _allTools;

        public ToolPocketItem(int pocket, ToolInfo? toolInfo, ObservableCollection<ToolInfo> allTools)
        {
            Pocket = pocket;
            ToolInfo = toolInfo;
            _allTools = allTools;
        }

        public int ToolNumber
        {
            get
            {
                if (ToolInfo != null)
                {
                    return ToolInfo.Number;
                } else
                {
                    return 0;
                }
            }
            set
            {
                int oldToolNumber = this.ToolNumber;
                if (value != oldToolNumber)
                {
                    // Do some quick validation
                    if (value > 0)
                    {
                        // If we have a valid new tool index (should check max 200 too)
                        ToolInfo tInfo = _allTools[value - 1];
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
                        ToolInfo = _allTools[value - 1];
                        ToolInfo.Pocket = Pocket;
                    }
                    NotifyPropertyChanged(oldToolNumber);
                }
            }
        }

  
    }



    public partial class MainWindow : Window
    {

        private CNCPipe _pipe;

        ObservableCollection<ToolInfo> _toolInfoList;
        ObservableCollection<ToolPocketItem> _toolPocketItems;

        private const string settingsPath = "C:\\cncm\\CorbinsWorkshop\\ToolPocketPositions.xml";
        private const string systemSettingsPath = "C:\\cncm\\RackMountBin.xml"; // If the file above isn't found we can attempt to load values from here, in case the user used the CNC script for ATC stuff.
        private const string pocketTemplatePath = "c:\\cncm\\CorbinsWorkshop\\pocket_position_template.cnc";
        private const string generatedMacroPath = "C:\\cncm\\CorbinsWorkshop\\Generated\\";

        private const string vcpOptionsPath = "C:\\cncm\\resources\\vcp\\options.xml";
        private const string vcpSkinPathFormat = "C:\\cncm\\resources\\vcp\\skins\\{0}.vcp";


        public ToolChangeSettings Settings;
        bool _loading = true;
        public MainWindow()
        {

            InitializeComponent();

            // Variables for the API Connection MessageBox
            var messageBoxText = "Can't connect to CNC12. Make sure it is running! Would you like to retry the connection?";
            var messageBoxTitle = "Error Connecting to CNC12!";
            var messageBoxType = MessageBoxButton.YesNo;

            _pipe = new CNCPipe();

            // Wait for cnc12_pipe to be constructed before continuing.
            while (!_pipe.IsConstructed())
            {
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




           _pipe.message_window.AddMessage("ToolRackSetup Connected");

            Settings = new ToolChangeSettings(_pipe);

            // Don't use the property, which has side effects
            _vcpHasVirtualDrawbarButton = GetIfVCPHasVirtualdrawbarButton();
            UpdateAddRemoveVCPButtonTitle();

            // Convert toolInfoList into our own datastructure
            // fixup tools to be in one bin at a time (mainly because of how I messed it up when testing)
            _toolInfoList = new ObservableCollection<ToolInfo>();
            _toolPocketItems = new ObservableCollection<ToolPocketItem>();

            RefreshTools();

            InitializeToolPocketItems();
            ReadSettings();
            InitializeUI();
            Settings.PropertyChanged += SettingsPropertyChanged;

            _dirty = false;
            _loading = false;

        }

        private void RefreshTools()
        {

            List<Info> toolLibrary;
            _pipe.tool.GetToolLibrary(out toolLibrary);
            double toolBinCount = _pipe.parameter.GetToolBinCount();

            HashSet<int> usedPockets = new HashSet<int>();
            _toolInfoList.Clear();

            for (int i = 0; i < toolLibrary.Count; i++)
            {
                ToolInfo item = new ToolInfo(_pipe, toolLibrary[i]);
                _toolInfoList.Add(item);
                System.Diagnostics.Debug.WriteLine("Tool: {0} bin: {1}, {2}", toolLibrary[i].number, toolLibrary[i].bin, toolLibrary[i].description);
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
            }

        }

        private void ReadSettings()
        {
            XDocument? doc = null;
            if (File.Exists(settingsPath))
            {
                doc = XDocument.Load(settingsPath);
            } else if (File.Exists(systemSettingsPath)) {
                doc = XDocument.Load(systemSettingsPath);
            }
            if (doc != null)
            {
                double ReadDouble(string name, double defaultValue)
                {
                    string expr = String.Format("/Table/{0}", name);
                    string? v = doc.XPathSelectElement(expr)?.Value;
                    if (v != null && Double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out double result)) return result;
                    return defaultValue;                   
                }

                bool ReadBool(string name, bool defaultValue)
                {
                    string expr = String.Format("/Table/{0}", name);
                    string? v = doc.XPathSelectElement(expr)?.Value;
                    if (v != null && Boolean.TryParse(v, out bool result)) return result;
                    return defaultValue;
                }

                Settings.ZBump = ReadDouble(nameof(Settings.ZBump), Settings.ZBump);
                Settings.ZClearance = ReadDouble(nameof(Settings.ZClearance), Settings.ZClearance);
                Settings.EnableTestingMode = ReadBool(nameof(Settings.EnableTestingMode), Settings.EnableTestingMode);
                Settings.TestingFeed = ReadDouble(nameof(Settings.TestingFeed), Settings.TestingFeed);
                Settings.SlideDistance = ReadDouble(nameof(Settings.SlideDistance), Settings.SlideDistance);
                Settings.RackOffset = ReadDouble(nameof(Settings.RackOffset), Settings.RackOffset);

                // probably not the fastest way to do this.
                foreach (ToolPocketItem item in _toolPocketItems)
                {

                    XElement? element = doc.XPathSelectElement(String.Format("/Table/Bin/BinNumber[text()='{0}']", item.Pocket));
                    XElement? parent = element?.Parent;
                    if (parent == null) { continue; }

                    double ReadItemDouble(string name, double defaultValue)
                    {
                        string? v = parent.XPathSelectElement(name)?.Value;
                        if (v != null && Double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out double result)) return result;
                        return defaultValue;
                    }
                    item.X = ReadItemDouble("XPosition", item.X);
                    item.Y = ReadItemDouble("YPosition", item.Y);
                    item.Z = ReadItemDouble("ZHeight", item.Z);

                    string? v = parent.XPathSelectElement("PocketStyle")?.Value;
                    if (v != null) {
                        item.Style =(PocketStyle)Enum.Parse(typeof(PocketStyle), v);
                    }

                }
            }
        }

        /*
         *   <Bin>
    <BinNumber>1</BinNumber>
    <XPosition>1.91887</XPosition>
    <YPosition>12</YPosition>
    <DistancetoClear>1.5</DistancetoClear>
    <ClearingAxis>1</ClearingAxis>
    <ZHeight>-3</ZHeight>
  </Bin>
        */

        private void WriteParameters()
        {
            try
            {  
                // done in other spots...but here too
                _pipe.parameter.SetToolBinCount(_toolPocketItems.Count);
                _pipe.parameter.SetValue(ParameterKey.HasATC, 0); // needs to be zero!
                _pipe.parameter.SetValue(ParameterKey.HasEnhancedATC, 0); // needs to be zero!
                _pipe.parameter.SetValue(ParameterKey.ShouldCheckAir, Settings.ShouldCheckAirPressure);
                _pipe.parameter.SetValue(ParameterKey.SpindleWaitTime, Settings.SpindleWaitTime);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to write parameters");
            }
        }

        private void WriteSettings()
        {

            // I hate string based programmming. so easy to make mistakes.

            XDocument doc = new XDocument();
            XElement topLevel = new XElement("Table");
            // Write settings here..
            topLevel.Add(new XElement(nameof(Settings.ZBump), Settings.ZBump.ToString(CultureInfo.InvariantCulture)));
            topLevel.Add(new XElement(nameof(Settings.ZClearance), Settings.ZClearance.ToString(CultureInfo.InvariantCulture)));
            topLevel.Add(new XElement(nameof(Settings.EnableTestingMode), Settings.EnableTestingMode.ToString(CultureInfo.InvariantCulture)));
            topLevel.Add(new XElement(nameof(Settings.TestingFeed), Settings.TestingFeed.ToString(CultureInfo.InvariantCulture)));
            topLevel.Add(new XElement(nameof(Settings.SlideDistance), Settings.SlideDistance.ToString(CultureInfo.InvariantCulture)));
            topLevel.Add(new XElement(nameof(Settings.RackOffset), Settings.RackOffset.ToString(CultureInfo.InvariantCulture)));

            foreach (ToolPocketItem item in _toolPocketItems)
            {
                // The names are copied from what the initial stuff did.
                XElement child = new XElement("Bin");
                child.Add(new XElement("BinNumber", item.Pocket.ToString(CultureInfo.InvariantCulture)));
                child.Add(new XElement("XPosition", item.X.ToString(CultureInfo.InvariantCulture)));
                child.Add(new XElement("YPosition", item.Y.ToString(CultureInfo.InvariantCulture)));
                child.Add(new XElement("ZHeight", item.Z.ToString(CultureInfo.InvariantCulture)));
                child.Add(new XElement("PocketStyle", item.Style.ToString()));
                topLevel.Add(child);
            }

            doc.Add(topLevel);
            doc.Save(settingsPath);
        }

        private void WriteMacros()
        {

            const string filePath = pocketTemplatePath;
            string fileContents = File.ReadAllText(filePath);
            const string targetDir = generatedMacroPath;
            Directory.CreateDirectory(targetDir);

            string testingSpeed = ""; // rapid speed
            if (Settings.EnableTestingMode)
            {
                testingSpeed = String.Format("L{0}", Settings.TestingFeed);
            }
            fileContents = fileContents.Replace("<SPEED>", testingSpeed);

            // the real math is here..
            double adjustmentAmount = Settings.RackOffset; 

            double xMin = 0;
            double xMax = 0;
            double yMin = 0;
            double yMax = 0;
            _pipe.axis.GetTravelLimit(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Direction.MINUS, out xMin);
            _pipe.axis.GetTravelLimit(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Direction.PLUS, out xMax);
            _pipe.axis.GetTravelLimit(CNCPipe.Axes.AXIS_2, CNCPipe.Axis.Direction.MINUS, out yMin);
            _pipe.axis.GetTravelLimit(CNCPipe.Axes.AXIS_2, CNCPipe.Axis.Direction.PLUS, out yMax);
            double yMiddle = yMin + ((yMax - yMin) / 2.0);
            double xMiddle = xMin + ((xMax - xMin) / 2.0);

            foreach (ToolPocketItem item in _toolPocketItems)
            {
                StringBuilder stringBuilder = new StringBuilder(fileContents);

                double xPos = item.X;
                double yPos = item.Y;

                // Figure out a spot in front of the rack to avoid collisions
                double xPosFront = xPos;
                double yPosFront = yPos;
                // Figure out where to start the slide in at (or where to slide out to after clamping) in front of the fork
                double xPosClear = xPos;
                double yPosClear = yPos;
                double slideDistance = Settings.SlideDistance;

                if (item.Style == PocketStyle.XMinus || item.Style == PocketStyle.XPlus)
                {
                    // X axis slide means we are facing to the left or the right, depending on the slide direction.
                    if (xPos > xMiddle)
                    {
                        // On the right side of the table; offset to the left
                        xPosFront -= adjustmentAmount;
                    } 
                    else
                    {
                        // on the left side of the table; offset to the right
                        xPosFront += adjustmentAmount;
                    }
                    if (item.Style == PocketStyle.XMinus)
                    {
                        xPosClear -= slideDistance;
                    }
                    else
                    {
                        xPosClear += slideDistance;
                    }
                } 
                else if (item.Style == PocketStyle.YMinus || item.Style == PocketStyle.YPlus)
                {
                    if (yPos > yMiddle)
                    {
                        // Back of table
                        yPosFront -= adjustmentAmount;
                    }
                    else
                    {
                        yPosFront += adjustmentAmount;
                    }
                    if (item.Style == PocketStyle.YMinus)
                    {
                        yPosClear -= slideDistance;
                    }
                    else
                    {
                        yPosClear += slideDistance;
                    }

                } else
                {
                    throw new Exception("Unhandled pocket type");
                }
                
                
                double zPos = item.Z;
                double zPosBump = item.Z + Settings.ZBump;

                string xPosString = xPos.ToString("F4", CultureInfo.InvariantCulture);
                stringBuilder.Replace("<XPOS>", xPosString);

                string yPosString = yPos.ToString("F4", CultureInfo.InvariantCulture);
                stringBuilder.Replace("<YPOS>", yPosString);

                string xPosFrontString = xPosFront.ToString("F4", CultureInfo.InvariantCulture);
                stringBuilder.Replace("<XPOS_FRONT>", xPosFrontString);

                string yPosFrontString = yPosFront.ToString("F4", CultureInfo.InvariantCulture);
                stringBuilder.Replace("<YPOS_FRONT>", yPosFrontString);

                string xPosClearString = xPosClear.ToString("F4", CultureInfo.InvariantCulture);
                stringBuilder.Replace("<XPOS_CLEAR>", xPosClearString);

                string yPosClearString = yPosClear.ToString("F4", CultureInfo.InvariantCulture);
                stringBuilder.Replace("<YPOS_CLEAR>", yPosClearString);

                string zPosString = zPos.ToString("F4", CultureInfo.InvariantCulture);
                stringBuilder.Replace("<ZPOS>", zPosString);

                string zPosBumpString = zPosBump.ToString("F4", CultureInfo.InvariantCulture);
                stringBuilder.Replace("<ZPOS_BUMP>", zPosBumpString);

                string filename = String.Format("pocket_{0:D}_position.cnc", item.Pocket);
                string targetPath = System.IO.Path.Combine(targetDir, filename);
                File.WriteAllText(targetPath, stringBuilder.ToString());


            }
        }
        private void InitializeUI()
        {
            lstviewTools.ItemsSource = _toolPocketItems;
            lstviewTools.DataContext = Settings;
            grdATCSettings.DataContext = Settings;
            // I can't figure out how to set bindings up in the xaml

            // txtBxZClearance.DataContext = Settings;
            txtBoxWaitTime.DataContext = Settings;
            txtBoxZBump.DataContext = Settings;
            chkbxTestingMode.DataContext = Settings;
            txtBoxTestingFeed.DataContext = Settings;
            chkbxCheckAirPressure.DataContext = Settings;
            txtBxSlideDistance.DataContext = Settings;
            txtBoxRackOffset.DataContext = Settings;
            chkbxVirtualDrawbarButton.DataContext = Settings;
            btnAddRemoveVCPButton.DataContext = Settings;
            chkbxEnableATC.DataContext = Settings;

            lstviewTools.UnselectAll();
        }


        // how to run gcode? not tested yet..
        //CNCPipe.Job job = new CNCPipe.Job(_pipe);
        //String command = String.Format("G10 P{0} R{1}", CMaxToolBins, 50);
        //job.RunCommand(command, "c:\\cncm", false);

        public ObservableCollection<ToolPocketItem> ToolPocketItems { get { return _toolPocketItems; } }

        private void InitializeToolPocketItems()
        {
            int toolBinCount = _pipe.parameter.GetToolBinCount();
            for (int i = 1; i <= toolBinCount; i++)
            {
                ToolInfo? toolInfo = _toolInfoList.FindToolInfoForPocket(i);
                ToolPocketItem tpi = new ToolPocketItem(i, toolInfo, _toolInfoList);
                _toolPocketItems.Add(tpi);
                tpi.PropertyChanged += Tpi_PropertyChanged;
            }

        }

        private bool _dirty = false; // only for changes that are not immediate, which require new files to be written.

        private bool _virtualDrawbarMessageBoxShown = false;

        private void ShowVirtualDrawbarMessageIfNeeded()
        {
            if (_virtualDrawbarMessageBoxShown) return;

            _virtualDrawbarMessageBoxShown = true;
            MessageBox.Show("Please restart CNC12 to see the change.");
        }


        private string GetCurrentSkinPath()
        {
            if (!File.Exists(vcpOptionsPath)) throw new Exception(String.Format("No VCP options file at {0}", vcpOptionsPath));
            // Read the skin being used..
            XDocument doc = XDocument.Load(vcpOptionsPath);
            XElement element = doc.XPathSelectElement("/ArrayOfVcpOption/VcpOption/Name[text()='Skin']") ?? throw new Exception("Failed to parse VCP options for the current Skin");

            XElement value = element.Parent!.XPathSelectElement("Value")!;
            return String.Format(vcpSkinPathFormat, value.Value!);
        }

        private bool GetIfVCPHasVirtualdrawbarButton()
        {
            try
            {
                string vcpSkinPath = GetCurrentSkinPath();
                XDocument doc = XDocument.Load(vcpSkinPath);
                XElement? element = doc.XPathSelectElement("/vcp_skin/button[text()='drawbar']");
                return element != null;
            } catch
            {
                return false;
            }
        }

        private bool _vcpHasVirtualDrawbarButton = false;
        // for bindings
        private bool VCPHasVirtualDrawbarButton
        {
            get
            {
                return _vcpHasVirtualDrawbarButton;
            }
             set
            {
                if (_vcpHasVirtualDrawbarButton != value)
                {
                    _vcpHasVirtualDrawbarButton = value;
                    UpdateVCPDrawbarButton(_vcpHasVirtualDrawbarButton);
                    UpdateAddRemoveVCPButtonTitle();
                    
                }
            }
        }

        private void UpdateAddRemoveVCPButtonTitle()
        {
            String s = VCPHasVirtualDrawbarButton ? "Remove Drawbar Button from VCP" : "Add Drawbar Button to VCP";
            btnAddRemoveVCPButton.Content = s;
        }

        private void UpdateVCPDrawbarButton(bool shouldAdd)
        {
            try
            {
                //   <button row="12" column="1" group="atc">drawbar</button>
                string vcpSkinPath = GetCurrentSkinPath();
                XDocument doc = XDocument.Load(vcpSkinPath);
                XElement? element = doc.XPathSelectElement("/vcp_skin/button[text()='drawbar']");

                if (shouldAdd)
                {
                    // add it, if not found
                    if (element == null)
                    {
                        element = new XElement("button", "drawbar");
                        // Set the position...hardcoded for now
                        element.Add(new XAttribute("row", "12"));
                        element.Add(new XAttribute("column", "1"));
                        element.Add(new XAttribute("group", "atc"));
                        doc.Root!.Add(element);
                        doc.Save(vcpSkinPath);
                        ShowVirtualDrawbarMessageIfNeeded();
                    }
                } else
                {
                    if (element != null)
                    {
                        element!.Remove();
                        doc.Save(vcpSkinPath);
                        ShowVirtualDrawbarMessageIfNeeded();
                    }
                }
 
            }
            catch (Exception e) {
                MessageBox.Show(e.Message, "Failed to write");
            }
        }



        private void SettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {   
            if (_loading) return;
            try
            {
                // I should simplify this by seperating XML saved settings and parmaeter saved settings
                if (e.PropertyName == nameof(Settings.EnableVirtualDrawbar))
                {
      
                }
                else if (e.PropertyName == nameof(Settings.ShouldCheckAirPressure))
                {
              
                }
                else if (e.PropertyName == nameof(Settings.SpindleWaitTime))
                {
                   
                }
                else if (e.PropertyName == nameof(Settings.EnableATC))
                {
                    // nop
                }
                else
                {
                    WriteSettings();
                    _dirty = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        private void Tpi_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_loading) return;
            if (e.PropertyName != nameof(ToolPocketItem.ToolInfo) && e.PropertyName != nameof(ToolPocketItem.ToolNumber)) // ignore changing the tool number..we set that dynamically all the time.
            {
                _dirty = true; // only dirty when something is changed that needs to cause us to write the macros..                   
            }
            WriteSettings(); // save the xml files
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void txtBoxToolNumber_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;
          
            e.Handled = true;
            TextBox txtBox = (TextBox)sender;
            BindingExpression binding = txtBox.GetBindingExpression(TextBox.TextProperty);
            binding.UpdateSource();

            txtBox.SelectAll(); // maybe move the focus away?
            
        }

        private void txtBoxToolNumber_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void SaveAll()
        {
            WriteParameters();
            WriteSettings();
            WriteMacros();
            _dirty = false;

        }

        private void btnWriteChanges_Click(object sender, RoutedEventArgs e)
        {
            SaveAll();
            MessageBoxResult r = MessageBox.Show("Macros generated and saved to C:\\cncm\\CorbinsWorkshop\\Generated");
        }

        private void btnRemoveLastPocket_Click(object sender, RoutedEventArgs e)
        {
            if (_toolPocketItems.Count > 0)
            {
                ToolPocketItem? item = _toolPocketItems.Last();
                item.ToolNumber = 0; /// make sure it doesn't have a tool..
                _toolPocketItems.RemoveAt(_toolPocketItems.Count - 1);
            }
            _pipe.parameter.SetToolBinCount(_toolPocketItems.Count);

            _dirty = true;
        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            if (_dirty)
            {
                MessageBoxResult r = MessageBox.Show("Changes were made that require the macros to be updated. Do you want to write out the macros now?", "Whoa there", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Yes);
                if (r == MessageBoxResult.Yes)
                {
                    SaveAll();
                } else if (r == MessageBoxResult.Cancel) { 
                    e.Cancel = true;
                }
            }
            Properties.Settings.Default.Save();

        }



        private void btnAddPocket_Click(object sender, RoutedEventArgs e)
        {
            _dirty = true;
            ToolPocketItem tpi = new ToolPocketItem(_toolPocketItems.Count + 1, null, _toolInfoList);
            if (_toolPocketItems.Count > 0)
            {
                ToolPocketItem last = _toolPocketItems.Last()!;

                tpi.X = last.X;
                tpi.Y = last.Y;
                tpi.Z = last.Z;
                tpi.Style = last.Style;
            }

            _toolPocketItems.Add(tpi);
            tpi.PropertyChanged += Tpi_PropertyChanged;
            lstviewTools.SelectedItem = tpi;

            // Update the parameter right away
            try
            {
                _pipe.parameter.SetToolBinCount( _toolPocketItems.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to add a pocket.");
            }
        }

        private void btnAssignMachineCoordX_Click(object sender, RoutedEventArgs e)
        {
            double[] machine_pos = new double[4];
            _pipe.state.GetCurrentMachinePosition(out machine_pos);

            foreach (ToolPocketItem tpi in lstviewTools.SelectedItems)
            {
                tpi.X = machine_pos[0];
            }
        }

        private void btnAssignMachineCoordY_Click(object sender, RoutedEventArgs e)
        {
            double[] machine_pos = new double[4];
            _pipe.state.GetCurrentMachinePosition(out machine_pos);

            foreach (ToolPocketItem tpi in lstviewTools.SelectedItems)
            {
                tpi.Y = machine_pos[1];
            }
        }

        private void btnAssignMachineCoordZ_Click(object sender, RoutedEventArgs e)
        {
            double[] machine_pos = new double[4];
            _pipe.state.GetCurrentMachinePosition(out machine_pos);

            foreach (ToolPocketItem tpi in lstviewTools.SelectedItems)
            {
                tpi.Z = machine_pos[2];
            }
        }

        private void btnAddRemoveVCPButton_Click(object sender, RoutedEventArgs e)
        {
            VCPHasVirtualDrawbarButton = !VCPHasVirtualDrawbarButton;

        }

        private void mainWindow_Closed(object sender, EventArgs e)
        {
            // ignore errors on this call..
            _pipe.message_window.AddMessage("ToolRackSetup Disconnected");
        }
    }
}