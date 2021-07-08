using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using HelixTroubleshootingML.Model;
using HelixTroubleshootingWPF.Objects;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        private static readonly string BaseDataHeader = "SN\tModel\tAccuracyTimestamp\t_2RMS\tMaxDev";
        private static readonly string VDEDataHeader = "VDETimestamp\tSphereSpacingError\tSphereProbingErrorSize\tSphereProbingErrorForm\tPlaneProbingError";
        private static readonly string DacMemsDataHeader = "MemsSN\tNLRange\tNLCoverage\tNRRange\tNRCoverage\tFLRange\tFLCoverage\tFRRange\tFRCoverage";
        private static readonly string LpfDataHeader = "LPFOperator\tMeterZeroed\tMeterBackGroundBiasuW\t" +
                                                "MeterAligned\tXAlignMM\tYAlignMM\tStabilityMinPowermW\tStabilityMaxPowermW\tPowerStabilityPassed\t" +
                                                "RangeTestPassed\tMinPowermW\tMaxPowermW\tLinearityPassed\tCalibratePassed";
        private static readonly string PitchDataHeader = "PitchOperator\tPitchTestRan\tXPixelsDelta\tYPixelsDelta";
        private static readonly string MirrorcleHeader = "ThetaX_110V\tThetaY_110V\tFnX\tFnY\tPRCPMaxDiff\tFitC0\tFitC1\tFitC2\tFitC3\tFitC4\tFitC5\tLinError";
        private static readonly string UffDataHeader = "UFFOperator\tCameraTempC\tMinTempOverridden\tAlignTargetRan\tMonitorAngle\tFocusTestRan\tExposureGood\tFocusRow\tFocusScore";
        private static readonly List<string> ComboHeaderList = new Func<List<string>>(() =>
       {
           string header = $"{BaseDataHeader}\t{VDEDataHeader}\t{UffDataHeader}\t{DacMemsDataHeader}\t{MirrorcleHeader}\t{LpfDataHeader}";
           for (int i = 1; i <= 14; i++) { header += $"\tTableIndex{i}\tPoweruW{i}\tPercentNominal{i}"; }
           header += $"\t{PitchDataHeader}";
           List<string> headerList = new();
           headerList.AddRange(header.Split("\t"));
           return headerList;
       })();
        private static readonly string ComboHeader = new Func<string>(() =>
       {
           string header = $"{BaseDataHeader}\t{VDEDataHeader}\t{UffDataHeader}\t{DacMemsDataHeader}\t{MirrorcleHeader}\t{LpfDataHeader}";
           for (int i = 1; i <= 14; i++) { header += $"\tTableIndex{i}\tPoweruW{i}\tPercentNominal{i}"; }
           header += $"\t{PitchDataHeader}";
           return header;
       })();


        public static string GetSensorPn(string sn)
        {
            List<string[]> resultsLogSplit = GetResultsLogSplit();
            string pn = "";
            foreach (string[] line in resultsLogSplit)
            {
                if (line[0] == sn)
                {
                    pn = line[2];
                }
            }
            return pn;
        }
        public static void GetMirrorcleData(ref List<HelixEvoSensor> sensors)
        {
            if (File.Exists(Config.MirrorcleDataPath))
            {
                string[] mirrorcleDataLines = File.ReadAllLines(Config.MirrorcleDataPath);
                foreach (HelixEvoSensor sensor in sensors)
                {
                    foreach (string line in mirrorcleDataLines)
                    {
                        if (sensor.DacMemsData.CheckComplete())
                        {
                            if (line.StartsWith($"MTI{sensor.DacMemsData.MemsSerialNumber.Replace("MTI", "")}"))
                            {
                                sensor.Mirrorcle = new MirrorcleData(line);
                                break;
                            }
                        }
                    }
                }
            }
        }
        public static void GatherEvoData(string pathFolder = "")
        {
            List<HelixEvoSensor> sensorList = GetEvoData();
            List<string> logLines = new() { ComboHeader };
            int i = 0;
            foreach (HelixEvoSensor s in sensorList)
            {
                if (s.CheckComplete())
                { logLines.Add(s.GetDataString()); }
                i++;
            }
            string filePath;
            if (pathFolder == "")
            {
                filePath = @$"{dataGatherFolder}\{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_EvoFixtureDataGather.txt";
                WriteFile(dataGatherFolder, filePath, logLines);
            }
            else
            {
                filePath = @$"{dataGatherFolder}\{pathFolder}\{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_EvoFixtureDataGather.txt";
                WriteFile(@$"{dataGatherFolder}\{pathFolder}", filePath, logLines);
            }

        }
        public static List<HelixEvoSensor> GetEvoData(string filter = "")
        {
            //Return list of Evo sensor data
            List<HelixEvoSensor> sensorList = EvoSensorsFromPitch(
                EvoSensorsFromLpf(
                    EvoSensorsFromDacMems(
                        EvoSensorsFromUff())));
            GetMirrorcleData(ref sensorList);
            GetAccuracyResultsFromLog(ref sensorList);
            //GetAfterThermalAccuracy(ref sensorList);
            if (filter != "")
            {
                sensorList.RemoveAll(s => !s.PartNumber.Contains(filter));
            }
            return sensorList;
        }
        public static void DacMemsDataGather()
        {
            List<HelixEvoSensor> sensorList = EvoSensorsFromDacMems();
            List<string> logLines = new() { $"{BaseDataHeader}\t{DacMemsDataHeader}" };
            GetAccuracyResultsFromLog(ref sensorList);
            foreach (HelixEvoSensor sensor in sensorList)
            {
                logLines.Add($"{sensor.SerialNumber}\t{sensor.PartNumber}\t{sensor.AccuracyResult}\t{sensor.DacMemsData}");
            }
            string filePath = @$"{dataGatherFolder}\{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_DACMEMSDataGather.txt";
            WriteFile(dataGatherFolder, filePath, logLines);
        }
        public static void UffDataGather()
        {
            List<HelixEvoSensor> sensorList = EvoSensorsFromUff();
            List<string> logLines = new() { $"{BaseDataHeader}\t{UffDataHeader}" };
            GetAccuracyResultsFromLog(ref sensorList);
            foreach (HelixEvoSensor s in sensorList)
            {
                logLines.Add($"{s.SerialNumber}\t{s.PartNumber}\t{s.AccuracyResult}\t{s.UffData}");
            }
            string filePath = $@"{dataGatherFolder}\{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_UFFDataGather.txt";
            WriteFile(dataGatherFolder, filePath, logLines);
        }
        public static void LpfDataGather()
        {
            List<HelixEvoSensor> sensorList = EvoSensorsFromLpf();
            List<string> logLines = new() { LpfDataHeader };
            for (int i = 1; i <= 14; i++) { logLines[0] = logLines[0] + $"\tTableIndex{i}\tPoweruW{i}\tPercentNominal{i}"; }
            GetAccuracyResultsFromLog(ref sensorList);
            foreach (HelixEvoSensor s in sensorList)
            {
                logLines.Add($"{s.SerialNumber}\t{s.PartNumber}\t{s.AccuracyResult}\t{s.LpfData}");
            }
            string filePath = $@"{dataGatherFolder}\{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_LPFDataGather.txt";
            WriteFile(dataGatherFolder, filePath, logLines);
        }
        public static void PitchDataGather()
        {
            List<HelixEvoSensor> sensorList = EvoSensorsFromPitch();
            List<string> logLines = new() { $"{BaseDataHeader}\t{PitchDataHeader}" };
            GetAccuracyResultsFromLog(ref sensorList);
            foreach (HelixEvoSensor s in sensorList)
            {
                logLines.Add($"{s.SerialNumber}\t{s.PartNumber}\t{s.AccuracyResult}\t{s.PitchData}");
            }
            string filePath = $@"{dataGatherFolder}\{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_PitchDataGather.txt";
            WriteFile(dataGatherFolder, filePath, logLines);
        }
        public static List<HelixEvoSensor> EvoSensorsFromDacMems(List<HelixEvoSensor> preSensors = null)
        {
            List<HelixEvoSensor> sensors = new();
            if (preSensors != null) { sensors = preSensors; }
            foreach (string line in File.ReadAllLines(Config.EvoTuningFixtureLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 65 | split[4].Length < 6) { continue; }
                bool sensorFound = false;
                foreach (HelixEvoSensor sensor in sensors)
                {
                    if (sensor.SerialNumber == split[4])
                    {
                        sensor.PartNumber = split[9];
                        sensor.DacMemsData = new EvoDacMemsData(line);
                        sensorFound = true;
                        break;
                    }
                }
                if (!sensorFound)
                {
                    HelixEvoSensor sensor = new();
                    sensor.SerialNumber = split[4];
                    sensor.PartNumber = split[9];
                    sensor.DacMemsData = new EvoDacMemsData(line);
                    sensors.Add(sensor);
                }
            }
            return sensors;
        }
        public static List<HelixEvoSensor> EvoSensorsFromLpf(List<HelixEvoSensor> preSensors = null)
        {
            List<HelixEvoSensor> sensors = new();
            if (preSensors != null) { sensors = preSensors; }
            foreach (string line in File.ReadAllLines(Config.EvoLpfLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 65 | split[4].Length < 6) { continue; }
                bool sensorFound = false;
                foreach (HelixEvoSensor sensor in sensors)
                {
                    if (sensor.SerialNumber == split[4])
                    {
                        sensor.PartNumber = split[5];
                        sensor.LpfData = new EvoLpfData(line);
                        sensorFound = true;
                        break;
                    }
                }
                if (!sensorFound)
                {
                    HelixEvoSensor sensor = new();
                    sensor.SerialNumber = split[4];
                    sensor.PartNumber = split[5];
                    sensor.LpfData = new EvoLpfData(line);
                    sensors.Add(sensor);
                }
            }
            return sensors;
        }
        public static List<HelixEvoSensor> EvoSensorsFromPitch(List<HelixEvoSensor> preSensors = null)
        {
            List<HelixEvoSensor> sensors = new();
            if (preSensors != null) { sensors = preSensors; }
            foreach (string line in File.ReadAllLines(Config.EvoPitchLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 9 | split[4].Length < 6) { continue; }
                bool sensorFound = false;
                foreach (HelixEvoSensor sensor in sensors)
                {
                    if (sensor.SerialNumber == split[4])
                    {
                        sensor.PartNumber = split[5];
                        sensor.PitchData = new EvoPitchData(line);
                        sensorFound = true;
                        break;
                    }
                }
                if (!sensorFound)
                {
                    HelixEvoSensor sensor = new();
                    sensor.SerialNumber = split[4];
                    sensor.PartNumber = split[5];
                    sensor.PitchData = new EvoPitchData(line);
                    sensors.Add(sensor);
                }
            }
            return sensors;
        }
        public static List<HelixEvoSensor> EvoSensorsFromUff(List<HelixEvoSensor> preSensors = null)
        {
            List<HelixEvoSensor> sensors = new();
            if (preSensors != null) { sensors = preSensors; }
            foreach (string line in File.ReadAllLines(Config.EvoUffLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 15) { continue; }
                if (split[11] != "Yes" | split[4].Length < 6) { continue; }
                bool sensorFound = false;
                foreach (HelixEvoSensor sensor in sensors)
                {
                    if (sensor.SerialNumber == split[4])
                    {
                        sensor.PartNumber = split[6];
                        sensor.UffData = new EvoUffData(line);
                        sensorFound = true;
                        break;
                    }
                }
                if (!sensorFound)
                {
                    HelixEvoSensor sensor = new();
                    sensor.SerialNumber = split[4];
                    sensor.PartNumber = split[6];
                    sensor.UffData = new EvoUffData(line);
                    sensors.Add(sensor);
                }
            }
            return sensors;
        }
        public static HelixEvoSensor AllEvoDataSingle(HelixEvoSensor sensor)
        {
            if (sensor.SerialNumber != "")
            {
                return SingleEvoAccuracyFromLog(
                        SingleSensorUff(
                            SingleSensorPitch(
                                SingleSensorLpf(
                                    SingleSensorMirrorcle(
                                        SingleSensorDacMems(sensor
                                        ))))));
            }
            else { return sensor; }
        }
        public static HelixEvoSensor SingleSensorDacMems(HelixEvoSensor sensor)
        {
            foreach (string line in File.ReadAllLines(Config.EvoTuningFixtureLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 65 | split[4].Length < 6) { continue; }
                if (sensor.SerialNumber == split[4])
                {
                    sensor.PartNumber = split[9];
                    sensor.DacMemsData = new EvoDacMemsData(line);
                }
            }
            return sensor;
        }
        public static HelixEvoSensor SingleSensorLpf(HelixEvoSensor sensor)
        {
            foreach (string line in File.ReadAllLines(Config.EvoLpfLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 65 | split[4].Length < 6) { continue; }
                if (sensor.SerialNumber == split[4])
                {
                    sensor.PartNumber = split[5];
                    sensor.LpfData = new EvoLpfData(line);
                }
            }
            return sensor;
        }
        public static HelixEvoSensor SingleSensorMirrorcle(HelixEvoSensor sensor)
        {
            if (File.Exists(Config.MirrorcleDataPath))
            {
                string[] mirrorcleDataLines = File.ReadAllLines(Config.MirrorcleDataPath);
                foreach (string line in mirrorcleDataLines)
                {
                    if (sensor.DacMemsData.MemsSerialNumber != "")
                    {
                        if (line.StartsWith($"MTI{sensor.DacMemsData.MemsSerialNumber.Replace("MTI", "")}"))
                        {
                            sensor.Mirrorcle = new MirrorcleData(line);
                            break;
                        }
                    }
                }
            }
            return sensor;
        }
        public static HelixEvoSensor SingleSensorPitch(HelixEvoSensor sensor)
        {
            foreach (string line in File.ReadAllLines(Config.EvoPitchLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 9 | split[4].Length < 6) { continue; }
                if (split[4] == sensor.SerialNumber)
                {
                    sensor.PartNumber = split[5];
                    sensor.PitchData = new EvoPitchData(line);
                }
            }
            return sensor;
        }
        public static HelixEvoSensor SingleSensorUff(HelixEvoSensor sensor)
        {
            foreach (string line in File.ReadAllLines(Config.EvoUffLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 15) { continue; }
                if (split[11] != "Yes" | split[4].Length < 6) { continue; }
                if (split[4] == sensor.SerialNumber)
                {
                    sensor.PartNumber = split[6];
                    sensor.UffData = new EvoUffData(line);
                }
            }
            return sensor;
        }
        public static HelixEvoSensor SingleSensorAccuracy(HelixEvoSensor sensor)
        {
            string snRectDataFolder = Path.Join(Config.RectDataDir, $"SN{sensor.SerialNumber.Substring(0, 3)}XXX");
            if (!Directory.Exists(snRectDataFolder)) { return sensor; }
            foreach (string folder in Directory.EnumerateDirectories(snRectDataFolder))
            {
                if (folder.Contains(sensor.SerialNumber) && Directory.Exists(folder))
                {
                    foreach (string file in Directory.GetFiles(folder))
                    {
                        if (Path.GetFileName(file) == "AccDataAcq.log")
                        {
                            sensor.AccuracyResult.Update(file);
                            break;
                        }
                        else if (Path.GetFileName(file) == "AccuracyTest_Vde2634.log")
                        {
                            sensor.VDE.Update(file);
                        }
                    }
                }
            }
            return sensor;
        }
        public static HelixEvoSensor SingleEvoAccuracyFromLog(HelixEvoSensor sensor, bool first = false)
        {
            List<string> resultsLog = new();
            resultsLog.AddRange(File.ReadAllLines(Config.HelixRectResultsLog));

            foreach (string line in resultsLog)
            {
                string[] lineSplit = line.Split("\t");
                if (lineSplit[0] == sensor.SerialNumber)
                {
                    if (lineSplit.Length < 190) { continue; }
                    //Check "Test Accuracy Status" and "0 Deg. 2 RMS error"
                    if ((lineSplit[187] == "Finished") & float.TryParse(lineSplit[45], out float rms2))
                    {
                        if (rms2! < 0) { continue; }
                        if (sensor.AccuracyResult.UpdateFromLog(lineSplit) & sensor.VDE.UpdateFromLog(lineSplit)) { if (first) { break; } }
                    }
                }
            }
            return sensor;
        }
        public static void TestML()
        {
            string[] headers = ComboHeader.Split("\t");
            //string[] data = AllSensorDataSingle(sn).GetDataList().ToArray();
            List<HelixEvoSensor> testSensors = GetEvoData();
            for (int i = testSensors.Count - 1; i >= 0; i--)
            {
                if (int.TryParse(testSensors[i].SerialNumber, out int snInt))
                {
                    if (snInt < 138700 | !testSensors[i].CheckComplete())
                    {
                        testSensors.RemoveAt(i);
                    }
                }
                else
                {
                    testSensors.RemoveAt(i);
                }
            }
            int sensorsTested = 0;
            float averageAbsError = 0.0f;
            foreach (HelixEvoSensor sensor in testSensors)
            {
                var input = new ModelInput();
                Type inputType = input.GetType();
                string[] data = sensor.GetDataList().ToArray();
                for (int i = 0; i < headers.Length; i++)
                {
                    System.Reflection.PropertyInfo propertyInfo = inputType.GetProperty(headers[i]);
                    if (propertyInfo == null) { continue; }
                    Type propType = propertyInfo.PropertyType;
                    if (propType == typeof(Single))
                    {
                        propertyInfo.SetValue(input, Single.Parse(data[i]));
                    }
                    else if (propType == typeof(System.String))
                    {
                        propertyInfo.SetValue(input, data[i]);
                    }
                    else if (propType == typeof(bool))
                    {
                        propertyInfo.SetValue(input, bool.Parse(data[i]));
                    }
                }
                ModelOutput result = ConsumeModel.Predict(input);
                float absDiff = Math.Abs(input.MaxDev - result.Score);
                averageAbsError += absDiff;
                sensorsTested++;
                Debug.WriteLine($"Sensor {sensor.SerialNumber}:\t2RMS(Actual|Predicted) {input.MaxDev}|{result.Score}\tAbs Difference: {absDiff}");
            }
            Debug.WriteLine($"\nSensors tested:\t{sensorsTested}\nAverage Abs Diff:\t{averageAbsError / sensorsTested}");
        }
        public static void GetAccuracyResults(ref List<HelixEvoSensor> sensors)
        {
            List<string> rectDataFolders = new();
            foreach (HelixEvoSensor sensor in sensors)
            {
                string rectDataFolder = $"SN{sensor.SerialNumber.Substring(0, 3)}XXX";
                string snRectdataFolder = Path.Join(Config.RectDataDir, rectDataFolder);
                if (!Directory.Exists(snRectdataFolder)) { continue; }
                if (!rectDataFolders.Contains(rectDataFolder)) { rectDataFolders.Add(rectDataFolder); }
            }
            foreach (string rectDataFolder in rectDataFolders)
            {
                foreach (string folder in Directory.EnumerateDirectories(Path.Join(Config.RectDataDir, rectDataFolder)))
                {
                    foreach (HelixEvoSensor sensor in sensors)
                    {
                        if (folder.Contains(sensor.SerialNumber) && Directory.Exists(folder))
                        {
                            foreach (string file in Directory.GetFiles(folder))
                            {
                                if (Path.GetFileName(file) == "AccDataAcq.log")
                                {
                                    sensor.AccuracyResult.Update(file);
                                    break;
                                }
                                else if (Path.GetFileName(file) == "AccuracyTest_Vde2634.log")
                                {
                                    sensor.VDE.Update(file);
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void GetAccuracyResultsFromLog(ref List<HelixEvoSensor> sensors, bool first = false)
        {
            List<string[]> resultsLogSplit = GetResultsLogSplit();
            foreach (HelixEvoSensor sensor in sensors)
            {
                foreach (string[] line in resultsLogSplit)
                {
                    if (line[0] == sensor.SerialNumber)
                    {
                        if (line.Length < 190) { continue; }
                        if (line[187] == "Finished" & float.TryParse(line[45], out float rms2))
                        {
                            if (rms2! < 0) { continue; }
                            if (sensor.AccuracyResult.UpdateFromLog(line) & sensor.VDE.UpdateFromLog(line)) { if (first) { break; } }
                        }
                    }
                }
            }
        }
        public static void GetAfterThermalAccuracy(ref List<HelixEvoSensor> sensors)
        {
            List<string[]> resultsLogSplit = GetResultsLogSplit();
            foreach (HelixEvoSensor sensor in sensors)
            {
                int resultNum = 0;
                for (int i = resultsLogSplit.Count - 1; i > 0; i--)
                {
                    if (resultsLogSplit[i][0] == sensor.SerialNumber && resultNum == 0) { resultNum++; }
                    else if (resultsLogSplit[i][0] == sensor.SerialNumber && resultNum == 1)
                    {
                        if (resultsLogSplit[i].Length < 190) { continue; }
                        if (resultsLogSplit[i][187] == "Finished" & float.TryParse(resultsLogSplit[i][45], out float rms2))
                        {
                            if (rms2! < 0) { continue; }
                            if (sensor.AccuracyResult.UpdateFromLog(resultsLogSplit[i]) & sensor.VDE.UpdateFromLog(resultsLogSplit[i])) { break; }
                        }
                    }
                }
            }
        }
        public static void GetTcompData(ref List<HelixEvoSensor> sensors)
        {
            foreach (HelixEvoSensor sensor in sensors)
            {
                string tcompDataFolder = $"SN{sensor.SerialNumber.Substring(0, 3)}XXX";
                string snTcompDataFolder = Path.Join(Config.TcompDir, tcompDataFolder);
                string file = "";
                string day2 = Path.Join(snTcompDataFolder, $"SN{sensor.SerialNumber}.day2");
                string complete = Path.Join(snTcompDataFolder, $"SN{sensor.SerialNumber}.txt");
                if (File.Exists(day2)) { file = day2; }
                if (File.Exists(complete)) { file = complete; }

                if (file == "") { continue; }
                sensor.TComp.ParseTCompData(file);

            }
        }
        public static void GetRectificationTemps(ref List<HelixEvoSensor> sensors, string model)
        {
            List<Tuple<string, double, double, double>> temps = new();
            List<string> lines = new() { "SerialNumber\tFirstTemp\tLastTemp\tDifference\tRefCycleAvg" };
            GetTcompData(ref sensors);
            //Tuples are <sn, first temp, last temp, difference>
            foreach (HelixEvoSensor sensor in sensors)
            {
                if (!sensor.PartNumber.Contains(model)) { continue; }
                string dataFolder = Path.Join(Config.RectDataDir, $"\\SN{sensor.SerialNumber.Substring(0, 3)}XXX\\SN{sensor.SerialNumber}");
                if (Directory.Exists(dataFolder))
                {
                    string logFile = Path.Join(dataFolder, $"SN{sensor.SerialNumber}.log");
                    if (File.Exists(logFile))
                    {
                        Tuple<string, double, double, double> temp = GetTempsFromFile(sensor.SerialNumber, logFile);
                        if(temp.Item2 != 0.0 && temp.Item3 != 0.0)
                        {
                            temps.Add(temp);
                            lines.Add($"{sensor.SerialNumber}\t{temp.Item2}\t{temp.Item3}\t{Math.Round(temp.Item4, 3)}\t{Math.Round(sensor.TComp.RefCycleAvg, 3)}");
                        }
                    }
                }
            }
            string folder = Path.Join(Config.ResultsDir, "\\RectificationTemps");
            string filePath = Path.Join(folder, $"\\{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_RectificationTemps.txt");
            WriteFile(folder, filePath, lines);
        }
        public static Tuple<string, double, double, double> GetTempsFromFile(string sn, string file)
        {
            List<string> lines = new();
            for(int i = 0; i < 3; i++)
            {
                try
                {
                    lines.AddRange(File.ReadAllLines(file));
                    break;
                }
                catch (System.IO.IOException)
                {
                    if (i == 2) { return new Tuple<string, double, double, double>(sn, 0.0, 0.0, 0.0); }
                }
            }
            double temp1 = 0.0;
            double temp2 = 0.0;
            //If these are the same at the end, the two temps are from the same run. Invalidate
            int temp1Index1 = 0;
            int temp1Index2 = 0;
            //Get first from top (first rectification)
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("Sensor Temp-1"))
                {
                    if (double.TryParse(lines[i].Replace("Sensor Temp-1:", ""), out double tempParse))
                    {
                        temp1 = tempParse;
                        temp1Index1 = i;
                    }
                }
                else if (lines[i].Contains("Sensor Temp-2"))
                {
                    if (double.TryParse(lines[i].Replace("Sensor Temp-2:", ""), out double tempParse))
                    {
                        temp1 += tempParse;
                        temp1 /= 2;
                    }
                    break;
                }
            }
            //Get first from bottom (last rectification)
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Contains("Sensor Temp-2"))
                {
                    if (double.TryParse(lines[i].Replace("Sensor Temp-2:", ""), out double tempParse))
                    {
                        temp2 = tempParse;
                    }
                }
                else if (lines[i].Contains("Sensor Temp-1"))
                {
                    if (double.TryParse(lines[i].Replace("Sensor Temp-1:", ""), out double tempParse))
                    {
                        temp2 += tempParse;
                        temp1Index2 = i;
                        temp2 /= 2;
                    }
                    break;
                }
            }
            return temp1Index1 == temp1Index2
                ? new Tuple<string, double, double, double>(sn, 0.0, 0.0, 0.0)
                : new Tuple<string, double, double, double>(sn, temp1, temp2, temp2 - temp1);
        }
        public static List<string[]> GetResultsLogSplit()
        {
            List<string> resultsLog = new();
            resultsLog.AddRange(File.ReadAllLines(Config.HelixRectResultsLog));
            List<string[]> resultsLogSplit = new();
            foreach (string line in resultsLog)
            {
                resultsLogSplit.Add(line.Split("\t"));
            }
            return resultsLogSplit;
        }
        public static Dictionary<string, List<string>> GetEvoRectDataFolders(ref List<HelixEvoSensor> sensors)
        {
            Dictionary<string, List<string>> folders = new();
            foreach(HelixSensor sensor in sensors)
            {
                if (!folders.ContainsKey(sensor.RectDataFolder))
                {
                    folders.Add(sensor.RectDataFolder, new());
                }
            }
            foreach(string folder in folders.Keys)
            {
                string rectDataFolder = Path.Join(Config.RectDataDir, folder);
                if(Directory.Exists(rectDataFolder))
                {
                    folders[folder].AddRange(Directory.GetDirectories(rectDataFolder));
                }
                else
                {
                    folders.Remove(folder);
                }
            }
            return folders;
        }
        public static void WriteFile(string folderPath, string filePath, List<string> lines, bool open = true)
        {
            Directory.CreateDirectory(folderPath);
            File.WriteAllLines(filePath, lines);
            if (open) { Process.Start("explorer.exe", folderPath); }
        }
    }
}
