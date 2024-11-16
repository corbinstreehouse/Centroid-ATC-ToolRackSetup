using System.Collections.ObjectModel;
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
        HasEnhancedATC = 160, // Currently needs to be 0
        MaxToolBins = 161, // Read/written

        // Custom parameters by Avid & Corbin
        ShouldCheckAir = 724, // Avid setting that I read and write
        HasVirtualDrawbarButton = 777, // virtual drawbar button support; prefer to be 0. (corbin)
        SpindleWaitTime = 778, // spindle wait time, in seconds (corbin)
    }

    public static class CNCPipeExtensions
    {

        // Same as the base name, but throws an exception instead of a return code.
        public static void SetMachineParameterEx(this Parameter parameter, int parameter_num, double value)
        {
            CNCPipe.ReturnCode rc = parameter.SetMachineParameter(parameter_num, value);
            if (rc != CNCPipe.ReturnCode.SUCCESS)
            {
                string reason = rc.ToString();
                string eMsg = String.Format("Failed to set machine parameter {0} to {1}.\nEnsure you are not running a job!\nError: {2}", parameter_num, value, reason);
                throw new Exception(eMsg);
            }
        }

        public static void SetMachineParameterEx(this Parameter parameter, ParameterKey parameterKey, double value)
        {
            parameter.SetMachineParameterEx((int)parameterKey, value);
        }

        public static void SetMachineParameterEx(this Parameter parameter, ParameterKey parameterKey, bool value)
        {
            parameter.SetMachineParameterEx((int)parameterKey, Convert.ToDouble(value));
        }

        public static double GetParameterValue(this CNCPipe.Parameter p, int parameterNum)
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
        public static double GetParameterValue(this CNCPipe.Parameter p, ParameterKey parameterKey)
        {
            return p.GetParameterValue((int)parameterKey);
        }

        public static bool GetBoolParameterValue(this CNCPipe.Parameter p, ParameterKey parameterKey)
        {
            double d = p.GetParameterValue(parameterKey);
            return System.Convert.ToBoolean(d);
        }

        public static int GetToolBinCount(this Parameter p) //can't be a property uet
        {
            return (int)p.GetParameterValue(ParameterKey.MaxToolBins);
        }

        public static void SetToolBinCount(this Parameter p, double value)
        {
            p.SetMachineParameterEx(ParameterKey.MaxToolBins, value);
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
                    _pipe.tool.SetBinNumber(this.Number, _pocket);
                }
            }
        }        

        //public int h_number;

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
                    _pipe.tool.SetToolDescription(this.Number, _description);
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

    public class ToolChangeSettings : NotifyingObject
    {
        private bool enableTestingMode = true;
        private double zBump = 0.005; // May need to be larger for CNC depot
        private double testingFeed = 200;
        private double zClearance = 0; //unused right now
        private double spindleWaitTime = 12.0; // seconds
        private bool shouldCheckAirPressure = true;
        private double slideDistance = 1.4; // Default value
        private double rackAdjustment = 5.5;
        private bool hasVirtualDrawbarButton = false; // If true, we do more stuff

        public bool EnableTestingMode { get => enableTestingMode; 
            set { 
                enableTestingMode = value; NotifyPropertyChanged(); 
            } 
        }
        public double ZBump { get => zBump; set { zBump = value; NotifyPropertyChanged();  } }
        public double ZClearance { get => zClearance; set { zClearance = value; NotifyPropertyChanged(); } }
        public double SpindleWaitTime { get => spindleWaitTime; set => 
                spindleWaitTime = value; }

        public double TestingFeed { get => testingFeed; set { testingFeed = value; NotifyPropertyChanged(); } }
        public bool ShouldCheckAirPressure { get => shouldCheckAirPressure; set { shouldCheckAirPressure = value; NotifyPropertyChanged(); } }

        public bool HasVirtualDrawbarButton
        { 
            get => hasVirtualDrawbarButton;
            set {
                if (hasVirtualDrawbarButton != value)
                {
                    hasVirtualDrawbarButton = value;
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

        public ToolChangeSettings()
        {
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
        // TODO: maybe don't hardcode the VCP..I could loook it up in vcp\settings.xml
        // but then it might not have the button...not sure what is best.
        private const string vcpSkinPath = "C:\\cncm\\resources\\vcp\\skins\\avid_router.vcp";
        

        public ToolChangeSettings Settings = new ToolChangeSettings();
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

            Settings.ShouldCheckAirPressure = _pipe.parameter.GetBoolParameterValue(ParameterKey.ShouldCheckAir);

            Settings.SpindleWaitTime = _pipe.parameter.GetParameterValue(ParameterKey.SpindleWaitTime);
            // if not set, default it to 12
            if (Settings.SpindleWaitTime <= 0)
            {
                Settings.SpindleWaitTime = 12;
            }
            Settings.HasVirtualDrawbarButton = _pipe.parameter.GetBoolParameterValue(ParameterKey.HasVirtualDrawbarButton);

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
                _pipe.parameter.SetMachineParameterEx(ParameterKey.HasATC, 0); // needs to be zero!
                _pipe.parameter.SetMachineParameterEx(ParameterKey.HasEnhancedATC, 0); // needs to be zero!
                _pipe.parameter.SetMachineParameterEx(ParameterKey.ShouldCheckAir, Settings.ShouldCheckAirPressure);
                _pipe.parameter.SetMachineParameterEx(ParameterKey.SpindleWaitTime, Settings.SpindleWaitTime);
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
            MessageBox.Show("Only set this if you do not have a physical drawbar release button.\n\nThis will add a virtual drawbar button to the Virtual Control Panel (VCP) when you restart CNC12.\n\nIt will also cause all manual tool change requests to prompt when the software is opening and closing the drawbar.");
        }

        private void WriteVCPSettings()
        {
            try
            {
                if (!File.Exists(vcpSkinPath)) throw new Exception(String.Format("No VCP file at {0}", vcpSkinPath));
                XDocument doc = XDocument.Load(vcpSkinPath);
                XElement? element = doc.XPathSelectElement("/vcp_skin/hide_group/group[text()='atc_drawbar']");
                if (element != null)
                {
                    // If it exists, and we are are showing the button, remove it
                    if (Settings.HasVirtualDrawbarButton)
                    {
                        element.Parent.Remove();
                        // element.Remove();
                        doc.Save(vcpSkinPath);

                    }

                } else 
                {
                    if (!Settings.HasVirtualDrawbarButton)
                    {
                        // If it doesn't exist, and we are hiding it, create it and add it
                        XElement hideGroupElement = new XElement("hide_group");
                        XElement groupElement = new XElement("group", "atc_drawbar");
                        hideGroupElement.Add(groupElement);
                        doc.Root.Add(hideGroupElement);
                        doc.Save(vcpSkinPath);


                    }
                } 




            }
            catch (Exception e) {
                MessageBox.Show(e.Message, "Failed to write VCP settings");
            }
        }

        private void SettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {   
            if (_loading) return;
            try
            {
                if (e.PropertyName == nameof(Settings.HasVirtualDrawbarButton))
                {
                    // This doesn't need to dirty the state, but does need to write the machine parameter for it.
                    // And I warn the user about it.
                    if (Settings.HasVirtualDrawbarButton)
                    {
                        ShowVirtualDrawbarMessageIfNeeded();
                    }
                    _pipe.parameter.SetMachineParameterEx(ParameterKey.HasVirtualDrawbarButton, Settings.HasVirtualDrawbarButton);
                    WriteVCPSettings();

                }
                else if (e.PropertyName == nameof(Settings.ShouldCheckAirPressure))
                {
                    _pipe.parameter.SetMachineParameterEx(ParameterKey.ShouldCheckAir, Settings.ShouldCheckAirPressure);
                }
                else if (e.PropertyName == nameof(Settings.SpindleWaitTime))
                {
                    _pipe.parameter.SetMachineParameterEx(ParameterKey.SpindleWaitTime, Settings.SpindleWaitTime);
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
    }
}