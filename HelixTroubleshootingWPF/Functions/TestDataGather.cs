using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
//using System.Windows.Shapes;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        private static readonly string BaseDataHeader = "SN\tModel\tAccuracyTimestamp\t2RMS\tMaxDev";
        private static readonly string DacMemsDataHeader = "MemsSN\tNLRange\tNLCoverage\tNRRange\tNRCoverage\tFLRange\tFLCoverage\tFRRange\tFRCoverage";
        private static readonly string LpfDataHeader = "LPFOperator\tMeterZeroed\tMeterBackGroundBiasuW\t" +
                                                "MeterAligned\tXAlignMM\tYAlignMM\tStabilityMinPowermW\tStabilityMaxPowermW\tPowerStabilityPassed\t" +
                                                "RangeTestPassed\tMinPowermW\tMaxPowermW\tLinearityPassed\tCalibratePassed";
        private static readonly string PitchDataHeader = "PitchOperator\tPitchTestRan\tXPixelsDelta\tYPixelsDelta";
        private static readonly string MirrorcleHeader = "ThetaX@110V\tThetaY@110V\tFnX\tFnY\tPRCPMaxDiff\tFitC0\tFitC1\tFitC2\tFitC3\tFitC4\tFitC5\tLinError";
        private static readonly string UffDataHeader = "UFFOperator\tCameraTempC\tMinTempOverridden\tAlignTargetRan\tMonitorAngle\tFocusTestRan\tExposureGood\tFocusRow\tFocusScore";
        private static List<string> ComboHeaderList = new Func<List<string>>(() =>
       {
           string header = $"{BaseDataHeader}\t{UffDataHeader}\t{DacMemsDataHeader}\t{MirrorcleHeader}\t{LpfDataHeader}";
           for (int i = 1; i <= 14; i++) { header = header + $"\tTableIndex{i}\tPoweruW{i}\tPercentNominal{i}"; }
           header = header + $"\t{PitchDataHeader}";
           List<string> headerList = new List<string>();
           headerList.AddRange(header.Split("\t"));
           return headerList;
       })();
        private static string ComboHeader = new Func<string>( () => 
        {
            string header = $"{BaseDataHeader}\t{UffDataHeader}\t{DacMemsDataHeader}\t{MirrorcleHeader}\t{LpfDataHeader}";
            for (int i = 1; i <= 14; i++) { header = header + $"\tTableIndex{i}\tPoweruW{i}\tPercentNominal{i}"; }
            header = header + $"\t{PitchDataHeader}";
            return header;
        })();

        public static void GetMirrorcleData(ref List<HelixEvoSensor> sensors)
        {
            if (File.Exists(Config.MirrorcleDataPath))
            {
                string[] mirrorcleDataLines = File.ReadAllLines(Config.MirrorcleDataPath);
                foreach(HelixEvoSensor sensor in sensors)
                {
                    foreach (string line in mirrorcleDataLines)
                    {
                        if (sensor.DacMemsData.CheckComplete())
                        {
                            if (line.StartsWith($"MTI{sensor.DacMemsData.MemsSerialNumber.Replace("MTI", "")}"))
                            {
                                sensor.Mirrorcle =  new MirrorcleData(line);
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
            List<string> logLines = new List<string>() { ComboHeader };

            foreach(HelixEvoSensor s in sensorList)
            {
                if (s.CheckComplete())
                { logLines.Add(s.GetDataString()); }
            }
            string filePath;
            if (pathFolder == "")
            { 
                filePath = @$"{dataGatherFolder}\{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_EvoFixtureDataGather.txt";
                WriteAndOpen(dataGatherFolder, filePath, logLines);
            }
            else
            { 
                filePath = @$"{dataGatherFolder}\{pathFolder}\{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_EvoFixtureDataGather.txt";
                WriteAndOpen(@$"{dataGatherFolder}\{pathFolder}", filePath, logLines);
            }
            
        }
        public static List<HelixEvoSensor> GetEvoData()
        {
            //Return list of Evo sensor data
            List<HelixEvoSensor> sensorList = EvoSensorsFromPitch(
                EvoSensorsFromLpf(
                    EvoSensorsFromDacMems(
                        EvoSensorsFromUff())));
            GetMirrorcleData(ref sensorList);
            GetAccuracyResults(ref sensorList);
            return sensorList;
        }
        public static void DacMemsDataGather()
        {
            List<HelixEvoSensor> sensorList = EvoSensorsFromDacMems();
            List<string> logLines = new List<string>() {$"{BaseDataHeader}\t{DacMemsDataHeader}"};
            GetAccuracyResults(ref sensorList);
            foreach (HelixEvoSensor sensor in sensorList)
            {
                logLines.Add($"{sensor.SerialNumber}\t{sensor.PartNumber}\t{sensor.AccuracyResult}\t{sensor.DacMemsData}");
            }
            string filePath = @$"{dataGatherFolder}\{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss")}_DACMEMSDataGather.txt";
            WriteAndOpen(dataGatherFolder, filePath, logLines);
        }
        public static void UffDataGather()
        {
            List<HelixEvoSensor> sensorList = EvoSensorsFromUff();
            List<string> logLines = new List<string>() {$"{BaseDataHeader}\t{UffDataHeader}"};
            GetAccuracyResults(ref sensorList);
            foreach (HelixEvoSensor s in sensorList)
            {
                logLines.Add($"{s.SerialNumber}\t{s.PartNumber}\t{s.AccuracyResult}\t{s.UffData}");
            }
            string filePath = $@"{dataGatherFolder}\{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss")}_UFFDataGather.txt";
            WriteAndOpen(dataGatherFolder, filePath, logLines);
        }
        public static void LpfDataGather()
        {
            List<HelixEvoSensor> sensorList = EvoSensorsFromLpf();
            List<string> logLines = new List<string>() { LpfDataHeader };
            for(int i = 1; i <= 14; i++) { logLines[0] = logLines[0] + $"\tTableIndex{i}\tPoweruW{i}\tPercentNominal{i}"; }
            GetAccuracyResults(ref sensorList);
            foreach (HelixEvoSensor s in sensorList)
            {
                logLines.Add($"{s.SerialNumber}\t{s.PartNumber}\t{s.AccuracyResult}\t{s.LpfData}");
            }
            string filePath = $@"{dataGatherFolder}\{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss")}_LPFDataGather.txt";
            WriteAndOpen(dataGatherFolder, filePath, logLines);
        }
        public static void PitchDataGather()
        {
            List<HelixEvoSensor> sensorList = EvoSensorsFromPitch();
            List<string> logLines = new List<string>() {$"{BaseDataHeader}\t{PitchDataHeader}"};
            GetAccuracyResults(ref sensorList);
            foreach (HelixEvoSensor s in sensorList)
            {
                logLines.Add($"{s.SerialNumber}\t{s.PartNumber}\t{s.AccuracyResult}\t{s.PitchData}");
            }
            string filePath = $@"{dataGatherFolder}\{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss")}_PitchDataGather.txt";
            WriteAndOpen(dataGatherFolder, filePath, logLines);
        }
        static public List<HelixEvoSensor> EvoSensorsFromDacMems(List<HelixEvoSensor> preSensors = null)
        {
            List<HelixEvoSensor> sensors = new List<HelixEvoSensor>();
            if(preSensors != null) { sensors = preSensors; }
            string tuningFixtureLog = @"\\castor\Production\Manufacturing\MfgSoftware\DACMEMSTuningFixture\200-0530\Results\MEMSDACTuningFixtureResults.txt";
            foreach (string line in File.ReadAllLines(tuningFixtureLog))
            {
                string[] split = line.Split("\t");
                if(split.Length < 65 | split[4].Length < 6) { continue; }
                bool sensorFound = false;
                foreach(HelixEvoSensor sensor in sensors)
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
                    HelixEvoSensor sensor = new HelixEvoSensor();
                    sensor.SerialNumber = split[4];
                    sensor.PartNumber = split[9];
                    sensor.DacMemsData = new EvoDacMemsData(line);
                    sensors.Add(sensor);
                }
            }
            return sensors;
        }
        static public List<HelixEvoSensor> EvoSensorsFromLpf(List<HelixEvoSensor> preSensors = null)
        {
            List<HelixEvoSensor> sensors = new List<HelixEvoSensor>();
            if(preSensors != null) { sensors = preSensors; }
            string lpfLog = @"\\castor\Production\Manufacturing\MfgSoftware\HelixEvoLaserPowerFixture\200-0638\Results\HelixEvoLaserPowerFixtureResultsLog.txt";
            foreach (string line in File.ReadAllLines(lpfLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 65 | split[4].Length < 6) { continue; }
                bool sensorFound = false;
                foreach(HelixEvoSensor sensor in sensors)
                {
                    if(sensor.SerialNumber == split[4])
                    {
                        sensor.PartNumber = split[5];
                        sensor.LpfData = new EvoLpfData(line);
                        sensorFound = true;
                        break;
                    }
                }
                if (!sensorFound)
                {
                    HelixEvoSensor sensor = new HelixEvoSensor();
                    sensor.SerialNumber = split[4];
                    sensor.PartNumber = split[5];
                    sensor.LpfData = new EvoLpfData(line);
                    sensors.Add(sensor);
                }
            }
            return sensors;
        }
        static public List<HelixEvoSensor> EvoSensorsFromPitch(List<HelixEvoSensor> preSensors = null)
        {
            List<HelixEvoSensor> sensors = new List<HelixEvoSensor>();
            if(preSensors != null) { sensors = preSensors; }
            string lpfLog = @"\\castor\Production\Manufacturing\MfgSoftware\HelixEvoCameraPitchFixture\200-0632\Results\HelixEvoCameraPitchFixtureMasterLog.txt";
            foreach (string line in File.ReadAllLines(lpfLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 9 | split[4].Length < 6) { continue; }
                bool sensorFound = false;
                foreach(HelixEvoSensor sensor in sensors)
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
                    HelixEvoSensor sensor = new HelixEvoSensor();
                    sensor.SerialNumber = split[4];
                    sensor.PartNumber = split[5];
                    sensor.PitchData = new EvoPitchData(line);
                    sensors.Add(sensor);
                }
            }
            return sensors;
        }
        static public List<HelixEvoSensor> EvoSensorsFromUff(List<HelixEvoSensor> preSensors = null)
        {
            List<HelixEvoSensor> sensors = new List<HelixEvoSensor>();
            if(preSensors != null) { sensors = preSensors; }
            string uffLog = @"\\castor\Production\Manufacturing\MfgSoftware\UniversalFocusFixture\200-0539\Results\UniversalFocusFixtureMasterLog.txt";
            foreach (string line in File.ReadAllLines(uffLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 15) { continue; }
                if (split[11] != "Yes" | split[4].Length < 6) { continue; }
                bool sensorFound = false;
                foreach (HelixEvoSensor sensor in sensors)
                {
                    if(sensor.SerialNumber == split[4])
                    {
                        sensor.PartNumber = split[6];
                        sensor.UffData = new EvoUffData(line);
                        sensorFound = true;
                        break;
                    }
                }
                if (!sensorFound)
                {
                    HelixEvoSensor sensor = new HelixEvoSensor();
                    sensor.SerialNumber = split[4];
                    sensor.PartNumber = split[6];
                    sensor.UffData = new EvoUffData(line);
                    sensors.Add(sensor);
                }
            }
            return sensors;
        }
        static public HelixEvoSensor AllSensorDataSingle(string sn)
        {
            return SingleSensorAccuracy(
                    SingleSensorUff(
                        SingleSensorPitch(
                            SingleSensorLpf(
                                SingleSensorMirrorcle(
                                    SingleSensorDacMems(new HelixEvoSensor() { SerialNumber = sn }
                                    ))))));
        }
        static public HelixEvoSensor SingleSensorDacMems(HelixEvoSensor sensor)
        {
            string tuningFixtureLog = @"\\castor\Production\Manufacturing\MfgSoftware\DACMEMSTuningFixture\200-0530\Results\MEMSDACTuningFixtureResults.txt";
            foreach (string line in File.ReadAllLines(tuningFixtureLog))
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
        static public HelixEvoSensor SingleSensorLpf(HelixEvoSensor sensor)
        {
            string lpfLog = @"\\castor\Production\Manufacturing\MfgSoftware\HelixEvoLaserPowerFixture\200-0638\Results\HelixEvoLaserPowerFixtureResultsLog.txt";
            foreach (string line in File.ReadAllLines(lpfLog))
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
        static public HelixEvoSensor SingleSensorMirrorcle(HelixEvoSensor sensor)
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
        static public HelixEvoSensor SingleSensorPitch(HelixEvoSensor sensor)
        {
            string lpfLog = @"\\castor\Production\Manufacturing\MfgSoftware\HelixEvoCameraPitchFixture\200-0632\Results\HelixEvoCameraPitchFixtureMasterLog.txt";
            foreach (string line in File.ReadAllLines(lpfLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 9 | split[4].Length < 6) { continue; }
                if(split[4] == sensor.SerialNumber)
                {
                    sensor.PartNumber = split[5];
                    sensor.PitchData = new EvoPitchData(line);
                }
            }
            return sensor;
        }
        static public HelixEvoSensor SingleSensorUff(HelixEvoSensor sensor)
        {
            string uffLog = @"\\castor\Production\Manufacturing\MfgSoftware\UniversalFocusFixture\200-0539\Results\UniversalFocusFixtureMasterLog.txt";
            foreach (string line in File.ReadAllLines(uffLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 15) { continue; }
                if (split[11] != "Yes" | split[4].Length < 6) { continue; }
                if(split[4] == sensor.SerialNumber)
                    {
                        sensor.PartNumber = split[6];
                        sensor.UffData = new EvoUffData(line);
                    }
            }
            return sensor;
        }
        static public HelixEvoSensor SingleSensorAccuracy(HelixEvoSensor sensor)
        {
            string rectDataFolder = $"SN{sensor.SerialNumber.Substring(0, 3)}XXX";
            if (!Directory.Exists(Path.Join(@"\\castor\Ftproot\RectData", rectDataFolder))) { return sensor; }
            foreach (string folder in Directory.EnumerateDirectories(Path.Join(@"\\castor\Ftproot\RectData", rectDataFolder)))
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
                    }
                }
            }
            return sensor;
        }
        static public void GetAccuracyResults(ref List<HelixEvoSensor> sensors)
        {
            List<string> rectDataFolders = new List<string>();
            foreach(HelixEvoSensor sensor in sensors)
            {
                string rectDataFolder = $"SN{sensor.SerialNumber.Substring(0, 3)}XXX";
                if (!Directory.Exists(Path.Join(@"\\castor\Ftproot\RectData", rectDataFolder))) { continue; }
                if (!rectDataFolders.Contains(rectDataFolder)) { rectDataFolders.Add(rectDataFolder); }
            }
            foreach (string rectDataFolder in rectDataFolders)
            {
                foreach (string folder in Directory.EnumerateDirectories(Path.Join(@"\\castor\Ftproot\RectData", rectDataFolder)))
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
                            }
                        }
                    }
                }
            }
        }
        static public void WriteAndOpen(string folderPath, string filePath, List<string> lines)
        {
            Directory.CreateDirectory(folderPath);
            File.WriteAllLines(filePath, lines);
            Process.Start("explorer.exe", folderPath);
        }
    }
}
