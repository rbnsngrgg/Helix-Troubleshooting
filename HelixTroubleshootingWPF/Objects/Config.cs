using System;
using System.IO;
using System.Windows;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;

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
        public string EvoTuningFixtureLog { get; private set; } = "";
        public string EvoLpfLog { get; private set; } = "";
        public string EvoPitchLog { get; private set; } = "";
        public string EvoUffLog { get; private set; } = "";
        public string SoloLaserAlignLog { get; private set; } = "";
        public string SoloFocusLog { get; private set; } = "";
        public string HelixRectResultsLog { get; private set; } = "";
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
                        "\t<Directories tcompBackupDir = \"\" tcompDir = \"\" rectDataDir = \"\" resultsDir = \"\">",
                        "\t\t<FixtureResults mirrorcleDataDir = \"\" evoTuningFixtureLog = \"\" evoLpfLog = \"\" " +
                        "evoPitchLog = \"\" evoUffLog = \"\" soloLaserAlignLog = \"\" soloFocusLog = \"\" helixRectResultsLog = \"\"/>",
                        "\t</Directories>",
                        "\t<LineAnalysis lineThresholdPercent = \"\"/>",
                        "\t<ALSPointRemover alsSensitivity = \"\"/>",
                        "\t<KNN dataCols = \"6,12,13,21,23,24,25,28\" " +
                        "allCols = \"6,9,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34," +
                        "37,39,40,41,42,45,46,51,54,57,60,63,66,69,72,75,78,81,84,87,90,93,94\" " +
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
                //Helix_Troubleshooting_Config > Directories > FixtureResults
                config.FirstChild.ChildNodes[0].ChildNodes[0].Attributes[0].Value = @"\\MOBIUS\ftpgen\LocalUser\Mirrorcle\PRCPYieldTable.csv";
                config.FirstChild.ChildNodes[0].ChildNodes[0].Attributes[1].Value = @"\\castor\Production\Manufacturing\MfgSoftware\DACMEMSTuningFixture\200-0530\Results\MEMSDACTuningFixtureResults.txt";
                config.FirstChild.ChildNodes[0].ChildNodes[0].Attributes[2].Value = @"\\castor\Production\Manufacturing\MfgSoftware\HelixEvoLaserPowerFixture\200-0638\Results\HelixEvoLaserPowerFixtureResultsLog.txt";
                config.FirstChild.ChildNodes[0].ChildNodes[0].Attributes[3].Value = @"\\castor\Production\Manufacturing\MfgSoftware\HelixEvoCameraPitchFixture\200-0632\Results\HelixEvoCameraPitchFixtureMasterLog.txt";
                config.FirstChild.ChildNodes[0].ChildNodes[0].Attributes[4].Value = @"\\castor\Production\Manufacturing\MfgSoftware\UniversalFocusFixture\200-0539\Results\UniversalFocusFixtureMasterLog.txt";
                config.FirstChild.ChildNodes[0].ChildNodes[0].Attributes[5].Value = @"\\castor\Production\Manufacturing\MfgSoftware\HelixSoloLaserAlignFixture\200-0654\Results\HelixSoloLaserAlignFixtureMasterLog.txt";
                config.FirstChild.ChildNodes[0].ChildNodes[0].Attributes[6].Value = @"\\castor\Production\Manufacturing\MfgSoftware\HelixSoloFocusFixture\200-0655\Results\HelixSoloFocusFixtureMasterLog.txt";
                config.FirstChild.ChildNodes[0].ChildNodes[0].Attributes[7].Value = @"\\castor\Ftproot\RectData\HelixRectResults.log";


                //config.FirstChild.ChildNodes[0].ChildNodes[0].Attributes[0]
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
            EvoTuningFixtureLog = "";
            EvoLpfLog = "";
            EvoPitchLog = "";
            EvoUffLog = "";
            SoloLaserAlignLog = "";
            SoloFocusLog = "";
            HelixRectResultsLog = "";
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

            //Directories
            var elements = config.GetElementsByTagName("Directories");
            if (elements != null)
            {
                TcompBackupDir = elements[0].Attributes.GetNamedItem("tcompBackupDir").Value;
                TcompDir = elements[0].Attributes.GetNamedItem("tcompDir").Value;
                RectDataDir = elements[0].Attributes.GetNamedItem("rectDataDir").Value;
                ResultsDir = elements[0].Attributes.GetNamedItem("resultsDir").Value;
            }
            elements = config.GetElementsByTagName("FixtureResults");
            if (elements != null)
            {
                MirrorcleDataPath = elements[0].Attributes.GetNamedItem("mirrorcleDataDir").Value;
                EvoTuningFixtureLog = elements[0].Attributes.GetNamedItem("evoTuningFixtureLog").Value;
                EvoLpfLog = elements[0].Attributes.GetNamedItem("evoLpfLog").Value;
                EvoPitchLog = elements[0].Attributes.GetNamedItem("evoPitchLog").Value;
                EvoUffLog = elements[0].Attributes.GetNamedItem("evoUffLog").Value;
                SoloLaserAlignLog = elements[0].Attributes.GetNamedItem("soloLaserAlignLog").Value;
                SoloFocusLog = elements[0].Attributes.GetNamedItem("soloFocusLog").Value;
                HelixRectResultsLog = elements[0].Attributes.GetNamedItem("helixRectResultsLog").Value;
            }
            elements = config.GetElementsByTagName("LineAnalysis");
            if (elements != null)
            {
                if (Int32.TryParse(elements[0].Attributes.GetNamedItem("lineThresholdPercent").Value, out int lineThresholdPercent))
                { LineThresholdPercent = lineThresholdPercent; }
                else
                { MessageBox.Show("Cannot parse config lineThresholdPercent value to int.", "Config Parse Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            elements = config.GetElementsByTagName("ALSPointRemover");
            if (elements != null)
            {
                if (Int32.TryParse(elements[0].Attributes.GetNamedItem("alsSensitivity").Value, out int alsSensitivity))
                { AlsSensitivity = alsSensitivity; }
                else
                { MessageBox.Show("Cannot parse config alsSensitivity value to int.", "Config Parse Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            elements = config.GetElementsByTagName("KNN");
            if (elements != null)
            {
                foreach (string col in elements[0].Attributes.GetNamedItem("dataCols").Value.Split(","))
                {
                    if (int.TryParse(col, out int intCol)) { KnnDataColumns.Add(intCol); }
                }
                foreach (string col in elements[0].Attributes.GetNamedItem("allCols").Value.Split(","))
                {
                    if (int.TryParse(col, out int intCol)) { AllKnnCols.Add(intCol); }
                }
                string groupString = elements[0].Attributes.GetNamedItem("dataGrouping").Value;
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
            }
            elements = config.GetElementsByTagName("SensorTest");
            if (elements != null)
            { SensorIp = elements[0].Attributes.GetNamedItem("sensorIp").Value; }
        }
    }
}
