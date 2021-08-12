using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using HelixTroubleshootingWPF.Objects;
//namespace Helix_Troubleshooting_CS
namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        //Working directory folder for saving the results of data gathering functions
        private static string dataGatherFolder = @$"{Directory.GetCurrentDirectory()}\DataGather\";
        //Array of strings to be added to function list
        public static readonly string[] functionList = new string[] {"ALS Point Removal","Fix Algorithm Errors","Illuminated Sphere Summary",
            "Solo Laser Line Analysis","Staring Dot Removal","Temperature Adjust", "DACMEMS Data Gather", "UFF Data Gather", "LPF Data Gather",
            "Pitch Data Gather","Evo Data Gather", "Sensor Test", "Generate TComp Template", "Evo Performance Reports", "Test"};// "Evo KNN", "Evo KNN Regression", "KNN Validation", "Test ML.net"};

        public static TToolsConfig Config = new();
        //Private Helper Functions----------------------------------------------------------------------------------------

        //Find all files in directory that contain a certain string. Return empty list if none found.
        private static List<string> GetFilesWith(string directory, string find)
        {
            find = find.Insert(0, "*");
            find += "*";
            List<string> files = new();
            try
            {
                files.AddRange(Directory.GetFiles(directory, find));
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.ArgumentException)
            {
                return files;
            }
            return files;
        }
        //Get folder that corresponds to a SN group (e.g. "SN137XXX"). Return empty string if none found.
        private static string GetGroupFolder(string directory, string sn)
        {
            string folder = "";
            if(sn.Length == 8)
            {
                sn = sn.Remove(5);
                sn += "XXX";
                if (Directory.Exists($@"{directory}\{sn}"))
                {
                    folder = $@"{directory}\{sn}";
                }
            }
            return folder;
        }

        public static void DebugFunction()
        {
            //List<HelixEvoSensor> sensorsFirstRun = GetEvoData("", true);
            //List<HelixEvoSensor> sensorsLastRun = GetEvoData();
            //List<string> lines = new() { "SN\tModel\tFirstAccuracyTime\tFirst2RMS\tFirstMaxDev\tLastAccuracyTime\tLast2RMS\tLastMaxDev" };
            //foreach (HelixEvoSensor sensor in sensorsFirstRun)
            //{
            //    string line = $"{sensor.SerialNumber}\t{sensor.PartNumber}\t{sensor.AccuracyResult.Timestamp}" +
            //        $"\t{sensor.AccuracyResult.ZeroDegree2Rms}\t{sensor.AccuracyResult.ZeroDegreeMaxDev}";
            //    foreach (HelixEvoSensor sensor2 in sensorsLastRun)
            //    {
            //        if (sensor2.SerialNumber == sensor.SerialNumber)
            //        {
            //            line += $"\t{sensor2.AccuracyResult.Timestamp}\t{sensor2.AccuracyResult.ZeroDegree2Rms}" +
            //                $"\t{sensor2.AccuracyResult.ZeroDegreeMaxDev}";
            //            lines.Add(line);
            //            break;
            //        }
            //    }
            //}
            //WriteFile(Path.Join(Config.ResultsDir, "FirstLastAccuracy"), Path.Join(Config.ResultsDir, @"FirstLastAccuracy\firstLastAccuracy.txt"), lines);

            //List<HelixEvoSensor> sensors = LinearityFlatnessResults();
            //SaveFlatnessResults(ref sensors);
            List<HelixEvoSensor> sensors = GetEvoData();
            GetRectificationTemps(ref sensors);
        }
    }
}
