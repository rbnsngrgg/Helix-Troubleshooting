using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using HelixTroubleshootingML.Model;
//using System.Windows.Shapes;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        public static void LinearityFlatnessResults()
        {
            List<HelixEvoSensor> sensors = GetEvoData();
            Dictionary<string, List<string>> folders = GetEvoRectDataFolders(ref sensors);
            int sensorNum = 0;
            foreach(HelixEvoSensor sensor in sensors)
            {
                sensorNum++;
                Debug.WriteLine($"Processing data for sensor {sensorNum} of {sensors.Count}");
                if(sensor.RectDataFolder != "" && folders.ContainsKey(sensor.RectDataFolder))
                {
                    foreach(string folder in folders[sensor.RectDataFolder])
                    {
                        if (folder.Contains(sensor.SerialNumber))
                        {
                            foreach(string file in Directory.GetFiles(folder))
                            {
                                if(file.Contains($"SN{sensor.SerialNumber}.log"))
                                {
                                    sensor.RectData.Update(file);
                                }
                            }
                        }
                    }
                }
            }
            SaveFlatnessResults(ref sensors);
        }

        public static void SaveFlatnessResults(ref List<HelixEvoSensor> sensors)
        {
            Debug.WriteLine("Writing results file");
            List<string> lines = new()
            {
                "SN\tPN\t-45MaxLinearity\t0MaxLinearity\t45MaxLinearity\t-45MaxFlatness\t0MaxFlatness\t45MaxFlatness"
            };
            foreach (HelixEvoSensor sensor in sensors)
            {
                if (sensor.RectData.CheckComplete())
                {
                    lines.Add($"{sensor.SerialNumber}\t{sensor.PartNumber}\t" +
                        $"{sensor.RectData.Minus45DegreeMaxLinearity}\t{sensor.RectData.ZeroDegreeMaxLinearity}\t{sensor.RectData.Plus45DegreeMaxLinearity}\t" +
                        $"{sensor.RectData.Minus45DegreeMaxFlatness}\t{sensor.RectData.ZeroDegreeMaxFlatness}\t{sensor.RectData.Plus45DegreeMaxFlatness}");
                }
            }
            string folder = Path.Join(Config.ResultsDir, "EvoLinearityFlatness");
            string file = Path.Join(folder, $"{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_LinearityFlatness.txt");
            WriteAndOpen(folder, file, lines);
        }
    }
}
