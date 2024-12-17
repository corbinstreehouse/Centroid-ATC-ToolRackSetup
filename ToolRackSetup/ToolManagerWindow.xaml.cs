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

    public partial class ToolManagerWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
 
        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // TODO: Maybe refactor to just use the connection manager...
        private CNCPipe _pipe
        {
            get { return ConnectionManager.Instance.Pipe; }
        }

        public ToolController ToolController { get { return ConnectionManager.Instance.ToolController; }  }
        private const string cncmPath = "c:\\cncm\\";
        private const string vcpPath = cncmPath + "resources\\vcp\\";
        private const string corbinsWorkshopPath = cncmPath + "CorbinsWorkshop\\";
        private const string centroidWizardSettingsPath = cncmPath + "wizardSettings.xml";


        private const string settingsPath = corbinsWorkshopPath + "ToolPocketPositions.xml";

        private const string systemSettingsPath = cncmPath + "RackMountBin.xml"; // If the file above isn't found we can attempt to load values from here, in case the user used the CNC script for ATC stuff.
        private const string pocketTemplatePath = corbinsWorkshopPath + "pocket_position_template.cnc";
        private const string generatedMacroPath = corbinsWorkshopPath + "Generated\\";

        private const string vcpOptionsPath = vcpPath + "options.xml";
        private const string vcpSkinPathFormat = vcpPath + "skins\\{0}.vcp";


        public ToolChangeSettings Settings { get; }
        public ParameterSettings _parameterSettings { get { return ConnectionManager.Instance.Parameters; } }

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
        
            // TODO: load/set other colors, but use the resources way..
            //if (Application.Current.Resources.Contains("txtColor"))
            //{
            //    Application.Current.Resources["txtColor"] = new SolidColorBrush(Colors.Blue);
            //}


        }


        public static ToolManagerWindow? Instance = null;
        public ToolManagerWindow()
        {

            Instance = this;
            InitializeComponent();

            LoadTheme();

            // TODO: Check if we are metric and flip default values if we are
            bool isMetric = false;
            Settings = new ToolChangeSettings(isMetric);
            NotifyPropertyChanged(nameof(Settings));
            
            // Don't use the property, which has side effects
            _vcpHasVirtualDrawbarButton = GetIfVCPHasVirtualdrawbarButton();
            UpdateAddRemoveVCPButtonTitle();

            StartWatchingToolPocketItems();
            ReadSettings();

            lstviewPockets.ItemsSource = ToolController.ToolPocketItems;
            lstviewPockets.UnselectAll();

            lstvwTools.ItemsSource = ToolController.Tools;
            txtBxActiveToolNumber.DataContext = ToolController;
            lblActiveToolDescription.DataContext = ToolController;


            Settings.PropertyChanged += SettingsPropertyChanged;

            _dirty = false;
            _loading = false;

        }

        private void CheckForCentroidATCSetup()
        {
            // Warn the user if they have done this...as it keeps writing 160/06
            if (!File.Exists(centroidWizardSettingsPath)) return;

            try
            {
                XDocument doc = XDocument.Load(centroidWizardSettingsPath);
                XAttribute? attr = doc.XPathSelectElement("/WizardSettings/ATCWritten")?.Attribute("value");
                string? v = attr?.Value;
                if (v != null && Boolean.TryParse(v, out bool result))
                {
                    if (result)
                    {
                        // Ask if we should turn it off
                        MessageBoxResult r = MessageBox.Show("It looks like you ran the Centroid ATC Wizard.\n" +
                            "This will cause the Tool Change Macro to have issues with tools not in the rack due to Parameters 6 (Centroid ATC Enabled) and 160 (Centroid Enhanced ATC).\n\n" + 
                            "Should I automatically reset these values to 0 and reset the wizard setting?", "ATC Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);
                        if (r == MessageBoxResult.Yes)
                        {
                            attr!.Value = "False";

                            // Also do CustomToolChangeMacro
                            XAttribute? customToolMacroAttr = doc.XPathSelectElement("/WizardSettings/CustomToolChangeMacro")?.Attribute("value");
                            if (customToolMacroAttr != null) { customToolMacroAttr!.Value = "True";  }

                            doc.Save(centroidWizardSettingsPath);
                            _parameterSettings.WriteCentroidATCParamsOff();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Checking XML file failed....Centroid ATC Setup");
            }

        }

        private void RefreshTools()
        {
            ToolController.RefreshTools();
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
                foreach (ToolPocketItem item in ToolController.ToolPocketItems)
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

            foreach (ToolPocketItem item in ToolController.ToolPocketItems)
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


            for (int i = 0; i < ToolController.ToolPocketItems.Count; i++)
            {

                ToolPocketItem item = ToolController.ToolPocketItems[i];
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

        // Did I have a binding to this?
        public ObservableCollection<ToolPocketItem> ToolPocketItems { get { return ToolController.ToolPocketItems; } }

        private void StartWatchingToolPocketItems()
        {
            foreach(ToolPocketItem tpi in ToolController.ToolPocketItems)
            {
                tpi.PropertyChanged += Tpi_PropertyChanged;

            }
        }

        private void StopWatchingToolPocketItems()
        {
            foreach (ToolPocketItem tpi in ToolController.ToolPocketItems)
            {
                tpi.PropertyChanged -= Tpi_PropertyChanged;

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
            if (e.PropertyName != nameof(ToolPocketItem.ToolInfo) && e.PropertyName != nameof(ToolPocketItem.ToolNumber) && e.PropertyName != nameof(ToolPocketItem.IsToolEnabled) &&
                e.PropertyName != nameof(ToolPocketItem.FetchButtonTitle)) // ignore changing the tool number..we set that dynamically all the time.
            {
                Dirty = true; // only dirty when something is changed that needs to cause us to write the macros
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
            if (ToolController.ToolPocketItems.Count > 0)
            {
                ToolController.RemoveLastPocket();
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
                ToolPocketItem tpi = ToolController.AddToolPocket();
                Dirty = true;
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
            StopWatchingToolPocketItems();
            Instance = null;
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

        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RefreshMaximizeRestoreButton()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.maximizeButton.Visibility = Visibility.Collapsed;
                this.restoreButton.Visibility = Visibility.Visible;
            }
            else
            {
                this.maximizeButton.Visibility = Visibility.Visible;
                this.restoreButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            this.RefreshMaximizeRestoreButton();
        }

        private void Window_(object sender, EventArgs e)
        {

        }

        private void chkbxEnableATC_Checked(object sender, RoutedEventArgs e)
        {
            // corbin, test!! I'm not sure if this is right; i need to test the wizard more...with aTC stuff
            if (!_loading && _parameterSettings.EnableATC)
            {
                CheckForCentroidATCSetup();
            }
        }
    }


}