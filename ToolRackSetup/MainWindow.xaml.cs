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
using static CentroidAPI.CNCPipe.State;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ToolRackSetup
{

    /// <summary>
    /// This converter targets a column header,
    /// in order to take its width to zero when
    /// it should be hidden
    /// </summary>
    public class ColumnWidthConverter
        : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            var isVisible = (bool)value;
            var width = double.Parse(parameter as string);
            return isVisible ? width : 0.0;
        }


        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

 
 
    public enum PocketStyle
    {
        XMinus = 0,
        XPlus = 1,
        YMinus = 2,
        YPlus = 3,
        Hole = 4,
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


    public  class MyObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Settings that write parameter values
    public class ParameterSettings : ObservableObject
    {
        CNCPipe _pipe;
        private double _spindleWaitTime = 12.0; // seconds
        private bool _shouldCheckAirPressure = true;
        private bool _enableVirtualDrawbar = false; // If true, we do more stuff
        private int _pocketCount = 0;
        public ParameterSettings(CNCPipe pipe)
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
            _pocketCount = (int)_pipe.parameter.GetValue(ParameterKey.MaxToolBins);
            _promptWhenGoingToTouchPlate = _pipe.parameter.GetBoolValue(ParameterKey.PromptToGoToTouchPlate);
        }

        public bool PromptWhenGoingToTouchPlate
        {
            get
            {
                return _promptWhenGoingToTouchPlate;
            }
            set
            {
                if (_promptWhenGoingToTouchPlate != value)
                {
                    _pipe.parameter.SetValue(ParameterKey.PromptToGoToTouchPlate, value);
                    SetProperty(ref _promptWhenGoingToTouchPlate, value);
                }
            }
        }

        public double SpindleWaitTime
        {
            get => _spindleWaitTime;
            set
            {
                if (_spindleWaitTime != value)
                {
                    _pipe.parameter.SetValue(ParameterKey.SpindleWaitTime, value);
                    SetProperty(ref _spindleWaitTime, value);
                }
            }
        }


        public bool ShouldCheckAirPressure
        {
            get => _shouldCheckAirPressure;
            set
            {
                if (_shouldCheckAirPressure != value)
                {
                    _pipe.parameter.SetValue(ParameterKey.ShouldCheckAir, value);
                    SetProperty(ref _shouldCheckAirPressure, value);
                }

            }
        }

        bool _enableATC = false;
        private bool _promptWhenGoingToTouchPlate;

        public bool EnableATC
        {
            get => _enableATC; set
            {
                if (_enableATC != value)
                {

                    _pipe.parameter.SetToolOptionValue(ATCToolOptions.EnableATC, value);
                    // We have other bits that *must* be zero for now.
                    // I'm now settings these in mfunc6_corbin.mac instead
                    // of doing it here, as one person had ran the wizard and it
                    // re-wrote these values and caused problems.
                    //_pipe.parameter.SetValue(ParameterKey.CentroidHasATC, 0); // needs to be zero!
                    //_pipe.parameter.SetValue(ParameterKey.CentroidHasEnhancedATC, 0); // needs to be zero!

                    SetProperty(ref _enableATC, value);
                }

            }
        }

        public bool EnableVirtualDrawbar
        {
            get => _enableVirtualDrawbar;
            set
            {
                if (_enableVirtualDrawbar != value)
                {
                    _pipe.parameter.SetToolOptionValue(ATCToolOptions.EnableVirtualDrawbar, value); // throws on error
                    SetProperty(ref _enableVirtualDrawbar, value);
                }
            }
        }

        public int PocketCount
        {
            get => _pocketCount;
            set
            {
                if (_pocketCount != value)
                {
                    _pipe.parameter.SetValue(ParameterKey.MaxToolBins, value); // throws on error
                    SetProperty(ref _pocketCount, value);
                }
            }
        }

    }

    public class ToolChangeSettings : ObservableObject
    {

        private bool _enableTestingMode = true;
        private double _zBump = 0.005; // May need to be larger for CNC depot
        private double _testingFeed = 200;
        private double _zClearance = 0; //unused right now

        private double _slideDistance = 1.4; // Default value
        private double _rackAdjustment = 5.5;

        public bool EnableTestingMode { get => _enableTestingMode; 
            set {
                SetProperty(ref _enableTestingMode, value);
            } 
        }
        public double ZBump { get => _zBump; set { 
                SetProperty(ref _zBump, value); 
            } }
        public double ZClearance { get => _zClearance; set { 
                SetProperty(ref _zClearance, value);
            } }

        public double TestingFeed { get => _testingFeed; set {
                SetProperty(ref _testingFeed, value);
            } }

        public double SlideDistance
        {
            get => _slideDistance;
            set
            {
                SetProperty(ref _slideDistance, value);
            }
        }

        public double RackOffset { get => _rackAdjustment;
            set {
                SetProperty(ref _rackAdjustment, value);
            }
        }

        public ToolChangeSettings(bool isMetric)
        {
            // TODO: on first call, reset parameter defaults to metric if needed
            if (isMetric)
            {

            }
        
        }
    }

    // I suppose this is the model object
    public class ToolPocketItem : MyObservableObject
    {
        private double x;
        private double y;
        private double z;
        private PocketStyle style = PocketStyle.YPlus;

        public int Pocket { get; }

        public double X { get => x; set {   x = value; NotifyPropertyChanged(); } }
        public double Y { get => y; set {  y = value; NotifyPropertyChanged(); } }
        public double Z { get => z; set {  z = value; NotifyPropertyChanged(); } }
        public PocketStyle Style
        {
            get { return style;  }
            set
            {
                if (value != style)
                {
                    style = value;
                    NotifyPropertyChanged();
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
                    _toolInfo = value;
                    
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsToolEnabled));
                }

            }
        }

        public bool IsToolEnabled
        {
            get { return _toolInfo != null;  }
        }

        private ToolController _toolController;

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
                    NotifyPropertyChanged();
                }
            }
        }

  
    }



    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // I should use a 3d party framework to do this silly boiler plate code that should be built in.
        public event PropertyChangedEventHandler? PropertyChanged;
 
        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private CNCPipe _pipe;

        ToolController _toolController;
        ObservableCollection<ToolPocketItem> _toolPocketItems;

        private const string settingsPath = "C:\\cncm\\CorbinsWorkshop\\ToolPocketPositions.xml";
        private const string systemSettingsPath = "C:\\cncm\\RackMountBin.xml"; // If the file above isn't found we can attempt to load values from here, in case the user used the CNC script for ATC stuff.
        private const string pocketTemplatePath = "c:\\cncm\\CorbinsWorkshop\\pocket_position_template.cnc";
        private const string generatedMacroPath = "C:\\cncm\\CorbinsWorkshop\\Generated\\";

        private const string vcpOptionsPath = "C:\\cncm\\resources\\vcp\\options.xml";
        private const string vcpSkinPathFormat = "C:\\cncm\\resources\\vcp\\skins\\{0}.vcp";


        public ToolChangeSettings Settings { get; }
        public ParameterSettings _parameterSettings { get; }

        public bool Dirty
        {
            get
            {
                return _dirty;

            }
            set
            {
                if (_dirty != value)
                {
                    _dirty = value;
                    NotifyPropertyChanged();
                }
            }
        }

        bool _loading = true;

        private string GetThemeFilename()
        {
            string fileContents = File.ReadAllText("c:\\cncm\\ColorPicker.txt");
            fileContents = fileContents.Trim();
            if (!File.Exists(fileContents))
            {
                // add the resource directory
                return String.Format("C:\\cncm\\resources\\colors\\{0}", fileContents);
            }
            return fileContents;
        }
        public System.Windows.Media.Color ColorFromString(String hex)
        {
            byte a = 255;
            byte r = 255;
            byte g = 255;
            byte b = 255;
            int start = 0;
            //handle ARGB strings (8 characters long)
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                start = 2;
            }
            //convert RGB characters to bytes
            r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);
            return System.Windows.Media.Color.FromArgb(a, r, g, b);
        }
        private void LoadTheme()
        {
            string filename = GetThemeFilename();
            XDocument doc = XDocument.Load(filename);
            Color GetThemeColor(string name)
            {
                string key = String.Format("/Table/Colors/{0}", name);
                string value = doc.XPathSelectElement(key)!.Value;
                return ColorFromString(value);
            }

         //   Color bgColor = GetThemeColor("backgroundColor");
         //   this.Background = new SolidColorBrush(bgColor);
            // TODO: load/set other colors, but use the resources way..
            //if (Application.Current.Resources.Contains("txtColor"))
            //{
            //    Application.Current.Resources["txtColor"] = new SolidColorBrush(Colors.Blue);
            //}


        }
        public MainWindow()
        {

            InitializeComponent();

            LoadTheme();
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

           _pipe.message_window.AddMessage("ATC Tools Connected");

            // TODO: Check if we are metric and flip default values if we are
            bool isMetric = false;
            Settings = new ToolChangeSettings(isMetric);
            NotifyPropertyChanged(nameof(Settings));
            _parameterSettings = new ParameterSettings(_pipe);
            NotifyPropertyChanged(nameof(_parameterSettings));

            // Don't use the property, which has side effects
            _vcpHasVirtualDrawbarButton = GetIfVCPHasVirtualdrawbarButton();
            UpdateAddRemoveVCPButtonTitle();

            // Convert toolInfoList into our own datastructure
            // fixup tools to be in one bin at a time (mainly because of how I messed it up when testing)
            _toolController = new ToolController(_pipe);
            _toolPocketItems = new ObservableCollection<ToolPocketItem>();

            RefreshTools();

            InitializeToolPocketItems();
            ReadSettings();

            lstviewPockets.ItemsSource = _toolPocketItems;
            lstviewPockets.UnselectAll();

            lstvwTools.ItemsSource = _toolController.Tools;

            Settings.PropertyChanged += SettingsPropertyChanged;

            _dirty = false;
            _loading = false;



        }
        private void RefreshTools()
        {
            _toolController.RefreshTools();
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

            // TODO: verify that the user can reach this position and it isn't outside of the limits
            // Chad was hitting a case where the slide was too large and put it outside where his machine could go. I can check for that..


            for (int i = 0; i < _toolPocketItems.Count; i++)
            {

                ToolPocketItem item = _toolPocketItems[i];
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
                double zPos = item.Z;
                double zPosBump = item.Z + Settings.ZBump;

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
                    // Hole style....just go straight to it! 
                    zPosBump = item.Z; // Maybe we need it? not sure..
                    // Well, see if the front pos can be figured out by looking at the prior/next and figuring otu the alignment... would maybe be nice to do..
                    //ToolPocketItem? adjacentItem = null;
                    //if (i > 0)
                    //{
                    //    // Was the last one also a pocket? was it close?


                    //} else
                    //{

                    //}


                }
                
                
   

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


        // how to run gcode? not tested yet..
        //CNCPipe.Job job = new CNCPipe.Job(_pipe);
        //String command = String.Format("G10 P{0} R{1}", CMaxToolBins, 50);
        //job.RunCommand(command, "c:\\cncm", false);

        public ObservableCollection<ToolPocketItem> ToolPocketItems { get { return _toolPocketItems; } }

        private void InitializeToolPocketItems()
        {
            int toolBinCount = _parameterSettings.PocketCount;
            for (int i = 1; i <= toolBinCount; i++)
            {
                ToolInfo? toolInfo = _toolController.FindToolInfoForPocket(i);
                ToolPocketItem tpi = new ToolPocketItem(i, toolInfo, _toolController);
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
               WriteSettings();
               Dirty = true;
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
                Dirty = true; // only dirty when something is changed that needs to cause us to write the macros..                   
            }
            WriteSettings(); // save the xml files
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void txtBox_KeyUp(object sender, KeyEventArgs e)
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
            Dirty = false;

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
                // Attempt the API call first; if it fails an exception will be thrown and our UI won't update and have a bad state
                int lastIndex = _toolPocketItems.Count - 1;
                _parameterSettings.PocketCount = lastIndex;

                ToolPocketItem? item = _toolPocketItems.Last();
                item.ToolNumber = 0; /// make sure it doesn't have a tool..
                _toolPocketItems.RemoveAt(lastIndex);
                Dirty = true;
            }
        }

        private void RunGCode(string code)
        {
            CNCPipe.Job job = new CNCPipe.Job(_pipe);
            job.RunCommand(code, "c:\\cncm", false);
        }

        private void BtnFetchClick(object sender, RoutedEventArgs e)
        {
            ToolInfo? toolInfo = ToolInfoFromSender(sender);
            if (toolInfo != null)
            {
                RunGCode(String.Format("T{0} M6\nG43 H{0}", toolInfo.Number));
            }
        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            if (this.Dirty)
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
            // Update the parameter right away
            try
            {
                // Make sure we aren't running first!! otherwise we get to a strange state. Attempting to set the pocket count will do it.
                int newCount = _toolPocketItems.Count + 1;
                _parameterSettings.PocketCount = newCount;

                Dirty = true;
                ToolPocketItem tpi = new ToolPocketItem(newCount, null, _toolController);
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
                lstviewPockets.SelectedItem = tpi;

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

            foreach (ToolPocketItem tpi in lstviewPockets.SelectedItems)
            {
                tpi.X = machine_pos[0];
            }
        }

        private void btnAssignMachineCoordY_Click(object sender, RoutedEventArgs e)
        {
            double[] machine_pos = new double[4];
            _pipe.state.GetCurrentMachinePosition(out machine_pos);

            foreach (ToolPocketItem tpi in lstviewPockets.SelectedItems)
            {
                tpi.Y = machine_pos[1];
            }
        }

        private void btnAssignMachineCoordZ_Click(object sender, RoutedEventArgs e)
        {
            double[] machine_pos = new double[4];
            _pipe.state.GetCurrentMachinePosition(out machine_pos);

            foreach (ToolPocketItem tpi in lstviewPockets.SelectedItems)
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
            _pipe.message_window.AddMessage("ATC Tools Disconnected");
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

        private void btnResetHeight_Click(object sender, RoutedEventArgs e)
        {
      
            ToolInfo? toolInfo = ToolInfoFromSender(sender);
            if (toolInfo != null) toolInfo.HeightOffset = 0;
        }

        private void BtnResetDiameter_click(object sender, RoutedEventArgs e)
        {
            ToolInfo? toolInfo = ToolInfoFromSender(sender);
            if (toolInfo != null) toolInfo.Diameter = 0;
        }

        private void mainWindow_Activated(object sender, EventArgs e)
        {
            // Make sure the pipe is good
            double value = 0;
            CNCPipe.ReturnCode rc = _pipe.parameter.GetMachineParameterValue((int)ParameterKey.CurrentToolNumber, out value);
            if (rc == CNCPipe.ReturnCode.ERROR_PIPE_IS_BROKEN) { 
                MessageBox.Show("The connection to CNC12 has been lost! Exiting now...");
                Environment.Exit(0);
            }
            RefreshTools();
        }
    }
}