using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Text.RegularExpressions;
using System.Xml;
using ImageMagick;
using System.Windows.Controls;
using HelixTroubleshootingWPF;
using System.Windows.Threading;
using PrimS;
using System.Threading.Tasks;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        //Create new config with default values, return true if success
        public static bool CreateConfig(string configPath)
        {
            try
            {
                //Generate file and write first lines
                string[] lines = { "<Helix_Troubleshooting_Config>",
                        "\t<Directories tcompBackupDir = \"testestest\" tcompDir = \"\" rectDataDir = \"\" resultsDir = \"\"/>",
                        "\t<LineAnalysis lineThresholdPercent = \"\"/>",
                        "\t<ALSPointRemover alsSensitivity = \"\"/>",
                    "</Helix_Troubleshooting_Config>" };
                File.WriteAllLines(configPath, lines);
                XmlDocument config = new XmlDocument();
                config.Load(configPath);
                //Set tcompBackupDir, tcompDir, rectDataDir, and resultsDir to default values
                config.FirstChild.ChildNodes[0].Attributes[0].Value = @"\\castor\Production\Manufacturing\Evo\Tcomp Templates\Originals";
                config.FirstChild.ChildNodes[0].Attributes[1].Value = @"\\castor\Production\Manufacturing\MfgSoftware\ThermalTest\200-0526\Results";
                config.FirstChild.ChildNodes[0].Attributes[2].Value = @"\\castor\Ftproot\RectData";
                config.FirstChild.ChildNodes[0].Attributes[3].Value = @"\\castor\Production\Manufacturing\MFGENG SW Tools\Helix Troubleshooting\Results";
                config.FirstChild.ChildNodes[1].Attributes[0].Value = "20";
                config.FirstChild.ChildNodes[2].Attributes[0].Value = "3";
                config.Save(configName);
                return true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error creating config file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        //Config Loader
        public static void LoadConfig()
        {
            string configPath = $"{Directory.GetCurrentDirectory()}\\{configName}";
            //Create if it doesn't exist
            if (!File.Exists(configPath))
            {
                if (!CreateConfig(configPath))
                {
                    return;
                }
            }
            //Load xml, assign specified values to the class properties, for use in the public functions.
            XmlDocument config = new XmlDocument();
            config.Load(configPath);
            int node = 0;
            //Check if auto-formatting from xml-notepad occurred, in which case the node with config info will be 1 (instead of 0).
            if (config.ChildNodes[0].ChildNodes.Count < 3) { node = 1; }

            tcompBackupDir = config.ChildNodes[node].ChildNodes[0].Attributes[0].Value;
            tcompDir = config.ChildNodes[node].ChildNodes[0].Attributes[1].Value;
            rectDataDir = config.ChildNodes[node].ChildNodes[0].Attributes[2].Value;
            resultsDir = config.ChildNodes[node].ChildNodes[0].Attributes[3].Value;
            if (!Int32.TryParse(config.ChildNodes[node].ChildNodes[1].Attributes[0].Value, out lineThresholdPercent))
            { MessageBox.Show("Cannot parse lineThresholdPercent value to int in config.", "Config Parse Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            if (!Int32.TryParse(config.ChildNodes[node].ChildNodes[2].Attributes[0].Value, out alsSensitivity))
            { MessageBox.Show("Cannot parse alsSensitivity value to int in config.", "Config Parse Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
    }
}
