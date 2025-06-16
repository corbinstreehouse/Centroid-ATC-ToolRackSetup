using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.XPath;

namespace ToolRackSetup
{
    class CentroidThemeManager
    {
        private const string sColoPickerFilePath= "c:\\cncm\\ColorPicker.txt";
        private const string sThemeXMLFileTemplatePath = "C:\\cncm\\resources\\colors\\{0}";

        private string GetThemeFilename()
        {
            string fileContents = File.ReadAllText(sColoPickerFilePath);
            fileContents = fileContents.Trim();
            if (!File.Exists(fileContents))
            {
                // add the resource directory
                return String.Format(sThemeXMLFileTemplatePath, fileContents);
            }
            return fileContents;
        }
        public Color ColorFromString(String hex)
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
        public void LoadTheme()
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
    }
}
