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


namespace ToolRackSetup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

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
        private double spindleWaitTime = 10.0; // seconds
        private bool shouldCheckAirPressure = true;

        public bool EnableTestingMode { get => enableTestingMode; 
            set { 
                enableTestingMode = value; NotifyPropertyChanged(); 
            } 
        }
        public double ZBump { get => zBump; set { zBump = value; NotifyPropertyChanged();  } }
        public double ZClearance { get => zClearance; set { zClearance = value; NotifyPropertyChanged(); } }
        public double SpindleWaitTime { get => spindleWaitTime; set => spindleWaitTime = value; }

        public double TestingFeed { get => testingFeed; set { testingFeed = value; NotifyPropertyChanged(); } }
        public bool ShouldCheckAirPressure { get => shouldCheckAirPressure; set { shouldCheckAirPressure = value; NotifyPropertyChanged(); } }

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

        public int Pocket { get; }

        public double X { get => x; set { object old = x;  x = value; NotifyPropertyChanged(old); } }
        public double Y { get => y; set { object old = y; y = value; NotifyPropertyChanged(old); } }
        public double Z { get => z; set { object old = z; z = value; NotifyPropertyChanged(old); } }

        private double _slideDistance = 1.4; // somewhere from 1.2 to 1.4 is a good value
        public double SlideDistance
        {
            get { return _slideDistance; }
            set { _slideDistance = value; NotifyPropertyChanged(); }
        }

        private int _slideAxis = 1; // 0 = x, 1=y CNCPipe.Axes.AXIS_2; // Y, most likley
        public int SlideAxis
        {
            get { return _slideAxis; }
            set { _slideAxis = value; NotifyPropertyChanged(); }
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
                }

            }
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

        const int Param_MaxToolBins = 161;
        const int Param_HasATC = 6;
        const int Param_HasEnhancedATC = 160;
        const int Param_ShouldCheckAir = 724;

        ObservableCollection<ToolInfo> _toolInfoList;
        ObservableCollection<ToolPocketItem> _toolPocketItems;


        public ToolChangeSettings Settings = new ToolChangeSettings();
        bool _loading = true;
        public MainWindow()
        {

            InitializeComponent();

            // Variables for the API Connection MessageBox
            var messageBoxText = "API is not connected. Would you like to retry connection?";
            var messageBoxTitle = "API Connection Error!";
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

            List<Info> toolLibrary;
            _pipe.tool.GetToolLibrary(out toolLibrary);
            double toolBinCount = 0;
            _pipe.parameter.GetMachineParameterValue(Param_MaxToolBins, out toolBinCount);
            double shouldCheckAirPressure = 0;
            _pipe.parameter.GetMachineParameterValue(Param_ShouldCheckAir, out shouldCheckAirPressure);
            Settings.ShouldCheckAirPressure = Convert.ToBoolean(shouldCheckAirPressure);


            // Convert toolInfoList into our own datastructure
            // fixup tools to be in one bin at a time (mainly because of how I messed it up when testing)
            _toolInfoList = new ObservableCollection<ToolInfo>();
            HashSet<int> usedPockets = new HashSet<int>();

            for (int i = 0; i < toolLibrary.Count; i++)
            {
                ToolInfo item = new ToolInfo(_pipe, toolLibrary[i]);
                _toolInfoList.Add(item);
                System.Diagnostics.Debug.WriteLine("Tool: {0} bin: {1}, {2}", toolLibrary[i].number, toolLibrary[i].bin, toolLibrary[i].description);
                if (item.Pocket > toolBinCount)
                {
                    item.Pocket = -1;
                } else if (item.Pocket > 0)
                {
                    if (usedPockets.Contains(item.Pocket)) {
                        // Can't have two tools in one pocket!
                        item.Pocket = -1;
                    } else
                    {
                        usedPockets.Add(item.Pocket);
                    }
                }
            }

            _toolPocketItems = new ObservableCollection<ToolPocketItem>();

            InitializeToolPocketItems((int)toolBinCount);
            LoadSavedValues();
            InitializeUI();
            Settings.PropertyChanged += SettingsPropertyChanged;

            _dirty = false;
            _loading = false;

        }

        private const String settingsPath = "C:\\cncm\\CorbinsWorkshop\\ToolPocketPositions.xml";
        private const String systemSettingsPath = "C:\\cncm\\RackMountBin.xml";

        /*
         * 
         * <Table>
  <Speed>50</Speed>
        */
        private void LoadSavedValues()
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
                String? v = doc.XPathSelectElement(String.Format("/Table/ZBump"))?.Value;
                if (v != null && Double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out double zBumpValue))
                {
                    Settings.ZBump = zBumpValue;
                }
                v = doc.XPathSelectElement(String.Format("/Table/SpindleWaitTime"))?.Value;
                if (v != null && Double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out double SpindleWaitTime))
                {
                    Settings.SpindleWaitTime = SpindleWaitTime;
                }
                v = doc.XPathSelectElement(String.Format("/Table/ZClearance"))?.Value;
                if (v != null && Double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out double ZClearance))
                {
                    Settings.ZClearance = ZClearance;
                }
                v = doc.XPathSelectElement(String.Format("/Table/EnableTestingMode"))?.Value;
                if (v != null && bool.TryParse(v, out bool EnableTestingMode))
                {
                    Settings.EnableTestingMode = EnableTestingMode;
                }
                v = doc.XPathSelectElement(String.Format("/Table/TestingFeed"))?.Value;
                if (v != null && Double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out double TestingFeed))
                {
                    Settings.TestingFeed = TestingFeed;
                }


                // probably not the fastest way to do this.
                foreach (ToolPocketItem item in _toolPocketItems)
                {

                    XElement? element = doc.XPathSelectElement(String.Format("/Table/Bin/BinNumber[text()='{0}']", item.Pocket));
                    XElement? parent = element?.Parent;
                    if (parent == null) { continue; }


                    v = parent.XPathSelectElement("XPosition")?.Value;
                    if (v != null && Double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out double x))
                    {
                        item.X = x;
                    }

                    v = parent.XPathSelectElement("YPosition")?.Value;
                    if (v != null && Double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out double y))
                    {
                        item.Y = y;
                    }
                    v = parent.XPathSelectElement("ZHeight")?.Value; // The variable names are copied from the Centroid base script generation
                    if (v != null && Double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out double z))
                    {
                        item.Z = z;
                    }

                    v = parent.XPathSelectElement("DistancetoClear")?.Value;
                    if (v != null && Double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out double c))
                    {
                        item.SlideDistance = c;
                    }

                    v = parent.XPathSelectElement("ClearingAxis")?.Value;
                    if (v != null && int.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out int clearingAxis))
                    {
                        item.SlideAxis = clearingAxis;
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
            // done in other spots...but here too
            _pipe.parameter.SetMachineParameter(Param_MaxToolBins, _toolPocketItems.Count);
            _pipe.parameter.SetMachineParameter(Param_HasATC, 0); // needs to be zero!
            _pipe.parameter.SetMachineParameter(Param_HasEnhancedATC, 0); // needs to be zero!
            _pipe.parameter.SetMachineParameter(Param_ShouldCheckAir, Convert.ToDouble(Settings.ShouldCheckAirPressure));
        }

        private void WriteSettings()
        {

            // I hate string based programmming. so easy to make mistakes.

            XDocument doc = new XDocument();
            XElement topLevel = new XElement("Table");
            // Write settings here..
            topLevel.Add(new XElement("ZBump", Settings.ZBump.ToString(CultureInfo.InvariantCulture)));
            topLevel.Add(new XElement("ZClearance", Settings.ZClearance.ToString(CultureInfo.InvariantCulture)));
            topLevel.Add(new XElement("SpindleWaitTime", Settings.SpindleWaitTime.ToString(CultureInfo.InvariantCulture)));
            topLevel.Add(new XElement("EnableTestingMode", Settings.EnableTestingMode.ToString(CultureInfo.InvariantCulture)));
            topLevel.Add(new XElement("TestingFeed", Settings.TestingFeed.ToString(CultureInfo.InvariantCulture)));
            foreach (ToolPocketItem item in _toolPocketItems)
            {
                XElement child = new XElement("Bin");
                child.Add(new XElement("BinNumber", item.Pocket.ToString(CultureInfo.InvariantCulture)));
                child.Add(new XElement("XPosition", item.X.ToString(CultureInfo.InvariantCulture)));
                child.Add(new XElement("YPosition", item.Y.ToString(CultureInfo.InvariantCulture)));
                child.Add(new XElement("ZHeight", item.Z.ToString(CultureInfo.InvariantCulture)));
                child.Add(new XElement("DistancetoClear", item.SlideDistance.ToString(CultureInfo.InvariantCulture)));
                child.Add(new XElement("ClearingAxis", item.SlideAxis.ToString(CultureInfo.InvariantCulture)));
                topLevel.Add(child);
            }

            doc.Add(topLevel);
            doc.Save(settingsPath);
        }

        private void WriteMacros()
        {
            string filePath = "c:\\cncm\\CorbinsWorkshop\\pocket_position_template.cnc";
            string fileContents = File.ReadAllText(filePath);
            string targetDir = "C:\\cncm\\CorbinsWorkshop\\Generated\\";
            Directory.CreateDirectory(targetDir);

            string testingSpeed = ""; // rapid speed
            if (Settings.EnableTestingMode)
            {
                testingSpeed = String.Format("L{0}", Settings.TestingFeed);
            }
            fileContents = fileContents.Replace("<SPEED>", testingSpeed);

            // the real math is here..
            double adjustmentAmount = 5.5; // Looks about right, hardcoded inches.

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
                CNCPipe.Axes axis = (CNCPipe.Axes)item.SlideAxis;
                if (axis == CNCPipe.Axes.AXIS_1) // x
                {
                    // X axis slide means we are facing to the left or the right, depending on the slide direction.
                    // I could check if we are sliding in from the middle, or what not, but
                    // It doesn't really matter if we have a little extra movement that could be avoided, as it is minor
                    if (xPos > xMiddle)
                    {
                        // On the right side of the table; offset to the left
                        xPosFront -= adjustmentAmount;
                    } else
                    {
                        // on the left side of the table; offset to the right
                        xPosFront += adjustmentAmount;
                    }
                    xPosClear -= item.SlideDistance;
                } else if (axis == CNCPipe.Axes.AXIS_2) // y
                {
                    // Y axis slide means we are facing forwards or backwards from the table y axis
                    if (yPos > yMiddle)
                    {
                        // Back of table
                        yPosFront -= adjustmentAmount;
                    } else
                    {
                        yPosFront += adjustmentAmount;
                    }
                    yPosClear -= item.SlideDistance;

                } else
                {
                    // not allowed should error out/exception
                    throw new Exception("Slide axis can only be 0 or 1 (X axis or Y axis)");
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


            lstviewTools.UnselectAll();
        }


        //CNCPipe.Job job = new CNCPipe.Job(_pipe);
        //String command = String.Format("G10 P{0} R{1}", CMaxToolBins, 50);
        //job.RunCommand(command, "c:\\cncm", false);

        public ObservableCollection<ToolPocketItem> ToolPocketItems { get { return _toolPocketItems; } }

        private void InitializeToolPocketItems(int toolBinCount)
        {
            for (int i = 1; i <= toolBinCount; i++)
            {
                ToolInfo? toolInfo = _toolInfoList.FindToolInfoForPocket(i);
                ToolPocketItem tpi = new ToolPocketItem(i, toolInfo, _toolInfoList);
                _toolPocketItems.Add(tpi);
                tpi.PropertyChanged += Tpi_PropertyChanged;
            }

        }

        private bool _dirty = false; // only for changes that are not immediate, which require new files to be written.

        private void SettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {   
            if (_loading) return;

            if (e.PropertyName == nameof(Settings.ShouldCheckAirPressure))
            {
                // Update the parameter for this immedietly 
                _pipe.parameter.SetMachineParameter(Param_ShouldCheckAir, Convert.ToDouble(Settings.ShouldCheckAirPressure));
            }
            else
            {
                WriteSettings();
                _dirty = true;
            }            

        }
        private void Tpi_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_loading) return;
            if (e.PropertyName != nameof(ToolPocketItem.ToolInfo) && e.PropertyName != nameof(ToolPocketItem.ToolNumber)) // ignore changing the tool number..we set that dynamically all the time.
            {
                _dirty = true; // only dirty when something is changed that needs to cause us to write the macros..                   
            }
//            ToolPocketItem toolPocketItem = (ToolPocketItem)sender;
//            MyPropertyChangedEventArgs args = (MyPropertyChangedEventArgs)e;
////            System.Diagnostics.Debug.WriteLine(e.PropertyName);
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void txtBoxToolNumber_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            // your event handler here
            e.Handled = true;
            TextBox txtBoxToolNumber = (TextBox)sender;
            BindingExpression binding = txtBoxToolNumber.GetBindingExpression(TextBox.TextProperty);
            binding.UpdateSource();
            txtBoxToolNumber.SelectAll();
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
                tpi.SlideAxis = last.SlideAxis;
                tpi.SlideDistance = last.SlideDistance;
                tpi.X = last.X;
                tpi.Y = last.Y;
                tpi.Z = last.Z;
            }

            _toolPocketItems.Add(tpi);
            tpi.PropertyChanged += Tpi_PropertyChanged;
            lstviewTools.SelectedItem = tpi;

            // Update the parameter right away
            _pipe.parameter.SetMachineParameter(Param_MaxToolBins, _toolPocketItems.Count);

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