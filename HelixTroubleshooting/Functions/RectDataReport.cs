using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Timers;
using System.Windows;
using HelixTroubleshootingML.Model;
//using System.Windows.Shapes;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        public static Timer Timer { get; set; }

        public static string LogLocation { get; set; }
        public static List<HelixEvoSensor> LinearityFlatnessResults()
        {
            List<HelixEvoSensor> sensors = GetEvoData("920-");
            Dictionary<string, List<string>> folders = GetEvoRectDataFolders(ref sensors);
            int sensorNum = 0;
            foreach(HelixEvoSensor sensor in sensors)
            {
                sensorNum++;
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
                                    sensor.GetRectData(file);
                                }
                            }
                        }
                    }
                }
            }
            return sensors;
        }
        public static HelixEvoSensor LinearityFlatnessResultsSingle(HelixEvoSensor sensor)
        {
            List<HelixEvoSensor> sensors = new() { sensor };
            Dictionary<string, List<string>> folders = GetEvoRectDataFolders(ref sensors);
            if (sensor.RectDataFolder != "" && folders.ContainsKey(sensor.RectDataFolder))
            {
                foreach (string folder in folders[sensor.RectDataFolder])
                {
                    if (folder.Contains(sensor.SerialNumber))
                    {
                        foreach (string file in Directory.GetFiles(folder))
                        {
                            if (file.Contains($"SN{sensor.SerialNumber}.log"))
                            {
                                sensor.GetRectData(file);
                            }
                        }
                    }
                }
            }
            return sensor;
        }
        public static void SaveFlatnessResults(ref List<HelixEvoSensor> sensors)
        {
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
            WriteFile(folder, file, lines);
        }
        public static void AllEvoReportsTimerElapsed(Object source = null, ElapsedEventArgs e = null)
        {
            DateTime now = DateTime.Now;
            DateTime last = GetLastReportTime();
            double minutesDifference = (now - last).TotalMinutes;
            if(now.Hour >= Config.ReportStartTime.Hour &&
                now.Minute >= Config.ReportStartTime.Minute &&
                minutesDifference >= Config.ReportIntervalMinutes)
            {
                AllEvoReports();
            }
            Timer.Start();
        }
        public static void SingleEvoReport(string sn)
        {
            HelixEvoSensor sensor = new() { SerialNumber = sn };
            sensor = AllEvoDataSingle(sensor);
            sensor = LinearityFlatnessResultsSingle(sensor);
            sensor.GetSensorData(Path.Join(Config.RectDataDir, sensor.RectDataFolder));
            GenerateEvoReport(sensor, true);
        }
        public static void AllEvoReports(bool all = false)
        {
            WriteReportLog("Starting Report Generation-----------------------------------------------------------");

            int sensorCount = 0;

            DateTime timeFilter = GetLastReportTime();
            List <HelixEvoSensor> sensors = LinearityFlatnessResults();
            if(timeFilter != default && !all)
            {
                sensors.RemoveAll(s => s.AccuracyResult.Timestamp < timeFilter);
            }

            foreach (HelixEvoSensor sensor in sensors)
            {
                sensor.GetSensorData(Path.Join(Config.RectDataDir, sensor.RectDataFolder));
                GenerateEvoReport(sensor);
                sensorCount++;
            }
            WriteReportLog($"Processed data for {sensorCount} sensors.\n");
        }

        private static void GenerateEvoReport(HelixEvoSensor sensor, bool openFolder = false)
        {
            if (sensor.VDE.CheckComplete() && sensor.RectData.CheckComplete() && sensor.AccuracyResult.CheckComplete())
            {
                List<string> reportLines = new()
                {
                    "==============================================================================",
                    "\tSENSOR PERFORMANCE RESULTS",
                    "\t--------------------------",
                    $"\tSENSOR PART NUMBER:    {sensor.PartNumber}",
                    $"\tSENSOR REVISION LEVEL: {sensor.SensorRev}",
                    $"\tLASER CLASS:           {sensor.LaserClass}\n",
                    "------------------------------------------------------------------------------",
                    $"\tSensor Serial Number:   {sensor.SerialNumber}",
                    $"\tSensor Type:            ALS",
                    $"\tPeceptron Part Number:  {sensor.PartNumber}",
                    $"\tPerceptron Revision:    {sensor.SensorRev}",
                    $"\tRectification Revision: {sensor.RectRev}",
                    "------------------------------------------------------------------------------",
                    $"\tTime:    {sensor.RectData.Timestamp:M/d/yyyy h:m:s tt}\n",
                    "VALIDATION DATA:",
                    "\tResidual Sigma                  Actual  Limit",
                    "\t--------------                  ------  -----",
                    $"\tMax. Linearity Error (microns)  {string.Format("{0,6:F2}", sensor.RectData.ZeroDegreeMaxLinearity)}   {sensor.RectData.ZeroDegreeLinearityLimit}",
                    $"\tMax. Flatness Sigma (microns)   {string.Format("{0,6:F2}", sensor.RectData.ZeroDegreeMaxFlatness)}   {sensor.RectData.ZeroDegreeFlatnessLimit}\n",
                    $"\tSensor " +
                    (sensor.RectData.ZeroDegreeMaxFlatness < sensor.RectData.ZeroDegreeFlatnessLimit
                    && sensor.RectData.ZeroDegreeMaxLinearity < sensor.RectData.ZeroDegreeLinearityLimit ? "PASSES" : "FAILS") +
                    $" Residual Specification\n",
                    "ACCURACY DATA: B89\n",
                    $"\tSensor {(sensor.AccuracyResult.ZeroDegree2Rms < sensor.AccuracyResult.ZeroDegree2RmsLimit ? "PASSES" : "FAILS")} 0 Degree De-rectification Test\n\n",
                    $"ACCURACY DATA: VDE-2634",
                    "\tVDE-2634 Accuracy Test: Inspection Results\n",
                    "\tPoint Deviation Statistic: Maximum Average Point-to-Fit Distance",
                    "\tTest                                Measured    Limit    Test",
                    "\t--------------------------------    ---------   -----    ----",
                    $"\tSphere Spacing Error (SD)           {string.Format("{0,6:F2}", (sensor.VDE.SphereSpacingError * 1000f))}" +
                    $"       {sensor.VDE.SphereSpacingErrorLimit * 1000f}" +
                    $"     {(sensor.VDE.SphereSpacingError * 1000f < sensor.VDE.SphereSpacingErrorLimit * 1000f ? "PASS" : "FAIL")}",
                    $"\tSphere Probing Error - Size (Ps)    {string.Format("{0,6:F2}", (sensor.VDE.SphereProbingErrorSize * 1000f))}" +
                    $"       {sensor.VDE.SphereProbingErrorSizeLimit * 1000f}" +
                    $"     {(sensor.VDE.SphereProbingErrorSize * 1000f < sensor.VDE.SphereProbingErrorSizeLimit * 1000f ? "PASS" : "FAIL")}",
                    $"\tSphere Probing Error - Form (Pf)    {string.Format("{0,6:F2}", (sensor.VDE.SphereProbingErrorForm * 1000f))}" +
                    $"       {sensor.VDE.SphereProbingErrorFormLimit * 1000f}" +
                    $"     {(sensor.VDE.SphereProbingErrorForm * 1000f < sensor.VDE.SphereProbingErrorFormLimit * 1000f ? "PASS" : "FAIL")}",
                    $"\tPlane Probing Error - Form (F)      {string.Format("{0,6:F2}", (sensor.VDE.PlaneProbingError * 1000f))}" +
                    $"       {sensor.VDE.PlaneProbingErrorFormLimit * 1000f}" +
                    $"     {(sensor.VDE.PlaneProbingError * 1000f < sensor.VDE.PlaneProbingErrorFormLimit * 1000f ? "PASS" : "FAIL")}"
                };
                string folder = GetReportResultsFolder(sensor.RectDataFolder);
                string file = Path.Join(folder, $"SN{sensor.SerialNumber}.txt");
                WriteFile(folder, file, reportLines, openFolder);
            }
        }
        public static string GetReportResultsFolder(string rectDataFolder)
        {
            return Path.Join(Config.RectDataDir, $@"{rectDataFolder}\SensorPerformanceResults");
        }

        public static void WriteReportLog(string message, int count = 0)
        {
            try
            {
                if(Directory.Exists(
                    Directory.GetParent(
                        Directory.GetParent(LogLocation).FullName).FullName))
                {
                    Directory.CreateDirectory(Directory.GetParent(LogLocation).FullName);
                    File.AppendAllText(LogLocation, $"{DateTime.Now:yyyy-MM-dd HH-mm-ss}: {message}\n");
                }
                else { throw new IOException(); }
            }
            catch (IOException)
            {
                if (count < 5)
                {
                    System.Threading.Thread.Sleep(100);
                    WriteReportLog(message, count + 1);
                }
                else
                {
                    MessageBox.Show($"The log file at \"{LogLocation}\" could not be reached.",
                        "Error Logging Evo Report Generation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Environment.Exit(1);
                }
            }
        }

        public static DateTime GetLastReportTime()
        {
            DateTime time = default;
            if(File.Exists(LogLocation))
            {
                var logLines = File.ReadLines(LogLocation);
                foreach (string line in logLines)
                {
                    if (line.Contains("Processed data for"))
                    {
                        DateTime.TryParseExact(
                            line.Split(":")[0],
                            "yyyy-MM-dd HH-mm-ss",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out time);
                    }
                }
            }
            return time;
        }
    }
}
