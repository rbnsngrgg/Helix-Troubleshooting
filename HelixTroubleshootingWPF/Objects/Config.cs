using System;
using System.IO;
using System.Windows;
using System.Xml;
using System.Collections.Generic;

namespace HelixTroubleshootingWPF.Functions
{
    class TToolsConfig
    {
        public List<int> AllKnnCols { get; private set; } = new List<int>();
        public int AlsSensitivity { get; private set; } = 3;
        public readonly string ConfigName = "HTconfig.xml";
        public List<int> KnnDataColumns { get; private set; } = new List<int>();
        public List<List<int>> KnnDataGroups { get; private set; } = new List<List<int>>();
        public int LineThresholdPercent { get; private set; } = 20;
        public string MirrorcleDataPath { get; private set; } = "";
        public string RectDataDir { get; private set; } = "";
        public string ResultsDir { get; private set; } = "";
        public string TcompBackupDir { get; private set; } = "";
        public string TcompDir { get; private set; } = "";
        public string SensorIp { get; private set; } = "";
        private bool CreateConfig(string configPath)
        {
            ///Create new config with default values, return true if success
            try
            {
                //Generate file and write first lines
                string[] lines = { "<Helix_Troubleshooting_Config>",
                        "\t<Directories tcompBackupDir = \"\" tcompDir = \"\" rectDataDir = \"\" resultsDir = \"\" mirrorcleDataDir = \"\"/>",
                        "\t<LineAnalysis lineThresholdPercent = \"\"/>",
                        "\t<ALSPointRemover alsSensitivity = \"\"/>",
                        "\t<KNN dataCols = \"6,12,13,21,23,24,25,28\" " +
                        "allCols = \"6,9,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,37,39,40,41,42,45,46,51,54,57,60,63,66,69,72,75,78,81,84,87,90,93,94\" " +
                        "dataGrouping = \"\"/>",
                        "\t<SensorTest sensorIp = \"10.0.4.96\"/>",
                    "</Helix_Troubleshooting_Config>" };
                File.WriteAllLines(configPath, lines);
                XmlDocument config = new XmlDocument();
                config.Load(configPath);
                //Set tcompBackupDir, tcompDir, rectDataDir, and resultsDir to default values
                config.FirstChild.ChildNodes[0].Attributes[0].Value = @"\\castor\Production\Manufacturing\Evo\Tcomp Templates\Originals";
                config.FirstChild.ChildNodes[0].Attributes[1].Value = @"\\castor\Production\Manufacturing\MfgSoftware\ThermalTest\200-0526\Results";
                config.FirstChild.ChildNodes[0].Attributes[2].Value = @"\\castor\Ftproot\RectData";
                config.FirstChild.ChildNodes[0].Attributes[3].Value = @"\\castor\Production\Manufacturing\MFGENG SW Tools\Helix Troubleshooting\Results";
                config.FirstChild.ChildNodes[0].Attributes[4].Value = @"\\MOBIUS\ftpgen\LocalUser\Mirrorcle\PRCPYieldTable.csv";
                config.FirstChild.ChildNodes[1].Attributes[0].Value = "20";
                config.FirstChild.ChildNodes[2].Attributes[0].Value = "3";
                config.Save(ConfigName);
                return true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error creating config file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        //Config Loader
        private void ClearConfigValues()
        {
            TcompBackupDir = "";
            TcompDir = "";
            RectDataDir = "";
            ResultsDir = "";
            MirrorcleDataPath = "";
            AllKnnCols = new List<int>();
            KnnDataColumns = new List<int>();
            KnnDataGroups = new List<List<int>>();
            LineThresholdPercent = 20;
            AlsSensitivity = 3;
            SensorIp = "";
        }
        public void LoadConfig()
        {
            ClearConfigValues();
            string configPath = $"{Directory.GetCurrentDirectory()}\\{ConfigName}";
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

            TcompBackupDir = config.ChildNodes[node].ChildNodes[0].Attributes[0].Value;
            TcompDir = config.ChildNodes[node].ChildNodes[0].Attributes[1].Value;
            RectDataDir = config.ChildNodes[node].ChildNodes[0].Attributes[2].Value;
            ResultsDir = config.ChildNodes[node].ChildNodes[0].Attributes[3].Value;
            MirrorcleDataPath = config.ChildNodes[node].ChildNodes[0].Attributes[4].Value;
            if (Int32.TryParse(config.ChildNodes[node].ChildNodes[1].Attributes[0].Value, out int lineThresholdPercent))
            { LineThresholdPercent = lineThresholdPercent; }
            else
            { MessageBox.Show("Cannot parse lineThresholdPercent value to int in config.", "Config Parse Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            if (Int32.TryParse(config.ChildNodes[node].ChildNodes[2].Attributes[0].Value, out int alsSensitivity))
            { AlsSensitivity = alsSensitivity; }
            else
            { MessageBox.Show("Cannot parse alsSensitivity value to int in config.", "Config Parse Error", MessageBoxButton.OK, MessageBoxImage.Warning); }

            foreach(string col in config.ChildNodes[node].ChildNodes[3].Attributes[0].Value.Split(","))
            {
                if (int.TryParse(col, out int intCol)) { KnnDataColumns.Add(intCol); }
            }
            foreach (string col in config.ChildNodes[node].ChildNodes[3].Attributes[1].Value.Split(","))
            {
                if (int.TryParse(col, out int intCol)) { AllKnnCols.Add(intCol); }
            }
            string groupString = config.ChildNodes[node].ChildNodes[3].Attributes[2].Value;
            if (groupString.Length > 1)
            {
                foreach (string group in groupString.Split("_"))
                {
                    List<int> groupList = new List<int>();
                    foreach (string col in group.Split(","))
                    {
                        groupList.Add(int.Parse(col));
                    }
                    KnnDataGroups.Add(groupList);
                }
            }
            SensorIp = config.ChildNodes[node].ChildNodes[4].Attributes[0].Value;

        }
    }
}
