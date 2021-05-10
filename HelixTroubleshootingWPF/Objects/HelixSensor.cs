using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace HelixTroubleshootingWPF
{
    //Represents an instance of a sensor as it is represented in a particular sensor.xml file
    class HelixSensor
    {
        
        //Properties
        public string SerialNumber { get; set; } //Should include "SN" prefix
        public string CameraSerial { get; set; }
        public string PartNumber { get; set; }
        public string SensorRev { get; set; }
        public string RectRev { get; set; }
        public string RectPosRev { get; set; }
        public string AccPosRev { get; set; }
        public string Date { get; set; }
        public string Color { get; set; }
        public string LaserClass { get; set; }
        public AccuracyResult AccuracyResult { get; set; } = new AccuracyResult();
        public VDEResult VDE { get; set; } = new VDEResult();

        //Constructors
        public HelixSensor()
        {
            
        }
        public HelixSensor(string sensorXmlFolder)
        {
            GetSensorData(sensorXmlFolder);
        }
        
        //Methods
        //Option to set parameters, or re-set if xml folder was not provided to constructor.
        public bool GetSensorData(string containingFolder)
        {
            if (!Directory.Exists(containingFolder)) { return false; }

            string xmlPath = "";
            foreach(string file in Directory.GetFiles(containingFolder))
            {
                if (System.IO.Path.GetFileName(file).Contains(".xml") & System.IO.Path.GetFileName(file).Contains("SN") & System.IO.Path.GetFileName(file).Length==12) { xmlPath = file;}
            }

            if (xmlPath == "") { return false; }
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlPath);
            //string format = "ddd MMM  d HH:mm:ss yyyy";
            //Date = DateTime.ParseExact("Wed Jul 22 08:24:53 2020\n", format, CultureInfo.InvariantCulture);
            Date = xml.LastChild.Attributes[1].Value.Replace("\n","").Replace("\r",""); //RECT_OUTPUT > Date
            SerialNumber = xml.LastChild.ChildNodes[0].ChildNodes[0].Attributes[0].Value;
            PartNumber = xml.LastChild.ChildNodes[0].ChildNodes[0].Attributes[2].Value; //RECT_OUTPUT > SENSORS > SENSOR > Part_Number
            SensorRev = xml.LastChild.ChildNodes[0].ChildNodes[0].Attributes[3].Value;
            RectRev = xml.LastChild.ChildNodes[0].ChildNodes[0].Attributes[4].Value;
            RectPosRev = xml.LastChild.ChildNodes[0].ChildNodes[0].Attributes[5].Value;
            AccPosRev = xml.LastChild.ChildNodes[0].ChildNodes[0].Attributes[6].Value;
            Color = xml.LastChild.ChildNodes[0].ChildNodes[0].Attributes[16].Value;
            LaserClass = xml.LastChild.ChildNodes[0].ChildNodes[0].Attributes[15].Value;

            CameraSerial = xml.LastChild.ChildNodes[1].ChildNodes[0].Attributes[0].Value; //RECT_OUTPUT > IMAGERS > IMAGER > Imager_UID

            return true;
        }
    }

    class HelixEvoSensor : HelixSensor
    {
        public EvoDacMemsData DacMemsData { get; set; }
        public EvoLpfData LpfData { get; set; }
        public MirrorcleData Mirrorcle { get; set; }
        public EvoPitchData PitchData { get; set; }
        public EvoUffData UffData { get; set; }
        public HelixEvoSensor() :base() { }
        public HelixEvoSensor(string sensorXmlFolder) : base(sensorXmlFolder) { }

        public bool CheckComplete()
        {
            if(!DacMemsData.CheckComplete() || !LpfData.CheckComplete() || 
                !PitchData.CheckComplete() || !UffData.CheckComplete() ||
                !AccuracyResult.CheckComplete() || !Mirrorcle.CheckComplete() || !VDE.CheckComplete()) { return false; }
            return true;
        }
        public string GetDataString()
        {
            return $"{SerialNumber}\t{PartNumber}\t{AccuracyResult}\t{VDE}\t{UffData}\t{DacMemsData}\t{Mirrorcle}\t{LpfData}\t{PitchData}";
        }
        public List<string> GetDataList()
        {
            List<string> data = new List<string>();
            foreach(string dataString in GetDataString().Split("\t"))
            {
                data.Add(dataString);
            }
            return data;
        }
    }

    class HelixSoloSensor : HelixSensor
    {
        public HelixSoloSensor() : base() { }
        public HelixSoloSensor(string sensorXmlFolder) : base(sensorXmlFolder) { }
    }

    class AccuracyResult
    {
        public DateTime Timestamp = new DateTime();
        public float ZeroDegree2Rms = 0f;
        public float ZeroDegreeMaxDev = 0f;

        public AccuracyResult()
        {

        }
        public AccuracyResult(string filePath)
        {
            Update(filePath);
        }
        public bool Update(string filePath)
        {
            for(int i = 0; i < 4; i++)
            {
                try
                {
                    bool zeroDegree = false;
                    foreach (string line in File.ReadAllLines(filePath))
                    {
                        DateTime newTimestamp = new DateTime();
                        if (line.Contains("Started at"))
                        {
                            newTimestamp = DateTime.ParseExact(line.Replace("Started at ", ""), "M/d/yyyy h:m:s tt", CultureInfo.InvariantCulture);
                            if (newTimestamp < Timestamp) { return false; }
                            Timestamp = newTimestamp;
                        }
                        else if (line.Contains("Line_Zero Test Type"))
                        { zeroDegree = true; }
                        else if (line.Contains("Maximum deviation") & zeroDegree)
                        { float.TryParse(line.Split("=")[1].Split(",")[0], out ZeroDegreeMaxDev); }
                        else if (line.Contains("2RMS deviation") & zeroDegree)
                        { float.TryParse(line.Split("=")[1].Split(",")[0], out ZeroDegree2Rms); return true; }
                    }
                    return false;
                }
                catch (System.IO.IOException)
                {
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }
            }
            return false;
        }
        public bool UpdateFromLog(string[] line)
        {
            DateTime newTimestamp;
            try
            {
                newTimestamp = DateTime.ParseExact(line[7], "M/d/yyyy h:m:s tt", CultureInfo.InvariantCulture);
            }
            catch { return false; }
            float.TryParse(line[45], out float zero2Rms);
            float.TryParse(line[42], out float zeroMaxDev);
            if(zero2Rms == 0f || zeroMaxDev == 0f) { return false; }
            ZeroDegree2Rms = zero2Rms;
            ZeroDegreeMaxDev = zeroMaxDev;
            Timestamp = newTimestamp;
            return true;
        }
        public bool CheckComplete()
        {
            if(Timestamp == new DateTime() || ZeroDegree2Rms == 0f || ZeroDegreeMaxDev == 0f) { return false; }
            return true;
        }
        public override string ToString()
        {
            return $"{Timestamp}\t{ZeroDegree2Rms}\t{ZeroDegreeMaxDev}";
        }
    }

    class VDEResult
    {
        public DateTime Timestamp = new DateTime();
        public float SphereSpacingError = 0f;
        public float SphereProbingErrorSize = 0f;
        public float SphereProbingErrorForm = 0f;
        public float PlaneProbingError = 0f;

        public VDEResult()
        {

        }
        public VDEResult(string filePath)
        {
            Update(filePath);
        }
        public bool Update(string filePath)
        {
            for (int i = 0; i < 4; i++) //Attempt to get the data a maximum of 4 times
            {
                try
                {
                    bool results = false;
                    foreach (string line in File.ReadAllLines(filePath))
                    {
                        DateTime newTimestamp = new DateTime();
                        if (line.Contains("Time"))
                        {
                            newTimestamp = DateTime.ParseExact(line.Replace("Time : ", ""), "M/d/yyyy h:m:s tt", CultureInfo.InvariantCulture);
                            if (newTimestamp < Timestamp) { return false; }
                            Timestamp = newTimestamp;
                        }
                        else if (line.Contains("VDE-2634 Accuracy Test: Inspection Results"))
                        { results = true; }
                        else if (line.Contains("Sphere Spacing Error (SD)") & results)
                        { float.TryParse(line.Split(" ")[4], out SphereSpacingError); }
                        else if (line.Contains("Sphere Probing Error - Size (Ps)") & results)
                        { float.TryParse(line.Split("=")[6], out SphereProbingErrorSize); }
                        else if (line.Contains("Sphere Probing Error - Form (Pf)") & results)
                        { float.TryParse(line.Split("=")[6], out SphereProbingErrorForm); }
                        else if (line.Contains("Plane Probing Error  - Form (F)") & results)
                        { float.TryParse(line.Split("=")[6], out PlaneProbingError); return true; }
                    }
                    return false;
                }
                catch (System.IO.IOException)
                {
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }
            }
            return false;
        }
        public bool UpdateFromLog(string[] line)
        {
            DateTime newTimestamp;
            try
            {
                newTimestamp = DateTime.ParseExact(line[7], "M/d/yyyy h:m:s tt", CultureInfo.InvariantCulture);
            }
            catch { return false; }
            float.TryParse(line[176], out float sphereSpacingError);
            float.TryParse(line[179], out float sphereProbingErrorForm);
            float.TryParse(line[182], out float sphereProbingErrorSize);
            float.TryParse(line[185], out float planeProbingError);
            if (sphereSpacingError == 0f || sphereProbingErrorForm == 0f) { return false; }
            SphereSpacingError = sphereSpacingError;
            SphereProbingErrorForm = sphereProbingErrorForm;
            SphereProbingErrorSize = sphereProbingErrorSize;
            PlaneProbingError = planeProbingError;
            Timestamp = newTimestamp;
            return true;
        }
        public bool CheckComplete()
        {
            return SphereSpacingError != 0f && SphereProbingErrorSize != 0f && SphereProbingErrorForm != 0f && PlaneProbingError != 0f;
        }
        public override string ToString()
        {
            return $"{Timestamp}\t{SphereSpacingError}\t{SphereProbingErrorSize}\t{SphereProbingErrorForm}\t{PlaneProbingError}";
        }
    }

    struct EvoDacMemsData
    {
        public string MemsSerialNumber;
        public double NLRange;
        public double NLCoverage;
        public double NRRange;
        public double NRCoverage;
        public double FLRange;
        public double FLCoverage;
        public double FRRange;
        public double FRCoverage;
        public bool IsAssigned;

        public EvoDacMemsData(string dataLine)
        {
            string[] split = dataLine.Split("\t");
            if(split.Length < 97) 
            {
                MemsSerialNumber = "";
                NLRange = 0d;
                NLCoverage = 0d;
                NRRange = 0d;
                NRCoverage = 0d;
                FLRange = 0d;
                FLCoverage = 0d;
                FRRange = 0d;
                FRCoverage = 0d;
                IsAssigned = false;
                return; 
            }
            MemsSerialNumber = split[5];
            double.TryParse(split[80], out NLRange);
            double.TryParse(split[81], out NLCoverage);
            double.TryParse(split[85], out NRRange);
            double.TryParse(split[86], out NRCoverage);
            double.TryParse(split[90], out FLRange);
            double.TryParse(split[91], out FLCoverage);
            double.TryParse(split[95], out FRRange);
            double.TryParse(split[96], out FRCoverage);
            IsAssigned = true;
        }
        public bool CheckComplete()
        {
            if(NLRange == 0d || NLCoverage == 0d || NRRange == 0d || NRCoverage == 0d ||
                FLRange == 0d || FLCoverage == 0d || FRRange == 0d || FRCoverage == 0d || !IsAssigned) { return false; }
            return true;
        }
        public override string ToString()
        {
            return $"{MemsSerialNumber}\t{NLRange}\t{NLCoverage}\t{NRRange}\t{NRCoverage}\t{FLRange}\t{FLCoverage}\t{FRRange}\t{FRCoverage}";
        }
    }

    struct EvoLpfData
    {
        public DateTime Date;
        public string Operator;
        public bool MeterZeroed;
        public int MeterBackgroundBiasuW;
        public bool MeterAligned;
        public float XAlignMm;
        public float YAlignMm;
        public bool PowerStabilityRan;
        public float StabilityMinPowermW;
        public float StabilityMaxPowermW;
        public bool PowerStabilityPassed;
        public bool RangeTestRan;
        public bool RangeTestPassed;
        public float MinPowermW;
        public float MaxPowermW;
        public bool LinearityRan;
        public bool LinearityPassed;
        public bool CalibrateRan;
        public bool CalibratePassed;
        public bool AmSamplingRan;
        public List<LpfTestIndex> TableSamples;

        public EvoLpfData(string line)
        {
            string[] split = line.Split("\t");
            if(split.Length < 65)
            {
                Date = new DateTime();
                Operator = "";
                MeterZeroed = false;
                MeterBackgroundBiasuW = 0;
                MeterAligned = false;
                XAlignMm = 0f;
                YAlignMm = 0f;
                PowerStabilityRan = false;
                StabilityMinPowermW = 0f;
                StabilityMaxPowermW = 0f;
                PowerStabilityPassed = false;
                RangeTestRan = false;
                RangeTestPassed = false;
                MinPowermW = 0f;
                MaxPowermW = 0f;
                LinearityRan = false;
                LinearityPassed = false;
                CalibrateRan = false;
                CalibratePassed = false;
                AmSamplingRan = false;
                TableSamples = new List<LpfTestIndex>();
            }
            else
            {
                if (!DateTime.TryParseExact($"{split[0]} {split[1]}", "MM/dd/yy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out Date)) { Date = new DateTime(); }
                Operator = split[2];
                MeterZeroed = split[6] == "Y";
                if (!int.TryParse(split[7], out MeterBackgroundBiasuW)) { MeterBackgroundBiasuW = 0; }
                MeterAligned = split[8] == "Y";
                if (!float.TryParse(split[9], out XAlignMm)) { XAlignMm = 0f; }
                if (!float.TryParse(split[10], out YAlignMm)) { YAlignMm = 0f; }
                PowerStabilityRan = split[11] == "Y";
                if (!float.TryParse(split[12], out StabilityMinPowermW)) { StabilityMinPowermW = 0f; }
                if (!float.TryParse(split[13], out StabilityMaxPowermW)) { StabilityMaxPowermW = 0f; }
                PowerStabilityPassed = split[14] == "Y";
                RangeTestRan = split[15] == "Y";
                RangeTestPassed = split[16] == "Y";
                if (!float.TryParse(split[17], out MinPowermW)) { MinPowermW = 0f; }
                if (!float.TryParse(split[18], out MaxPowermW)) { MaxPowermW = 0f; }
                LinearityRan = split[19] == "Y";
                LinearityPassed = split[20] == "Y";
                CalibrateRan = split[21] == "Y";
                CalibratePassed = split[22] == "Y";
                AmSamplingRan = split[23] == "Y";
                List<LpfTestIndex> newSamples = new List<LpfTestIndex>();
                for(int i = 24; i < split.Length; i++)
                {
                    if((i - 24) % 3 == 0 | i == 24)
                    {
                        int index;
                        int power;
                        int percent;
                        if (!int.TryParse(split[i], out index)) { index = 0; }
                        if (!int.TryParse(split[i+1], out power)) { power = 0; }
                        if (!int.TryParse(split[i+2], out percent)) { percent = 0; }
                        newSamples.Add(new LpfTestIndex() { Index = index, PoweruW = power, PercentNominal = percent});
                    }
                }
                TableSamples = newSamples;
            }
        }
        public bool CheckComplete()
        {
            if(TableSamples == null || TableSamples.Count < 14) { return false; }
            foreach(LpfTestIndex sample in TableSamples)
            {
                if (!sample.CheckComplete()) { return false; }
            }
            return true;
        }
        public override string ToString()
        {
            string line =
                $"{Operator}\t{MeterZeroed}\t{MeterBackgroundBiasuW}\t{MeterAligned}\t{XAlignMm}\t{YAlignMm}\t" +
                $"{StabilityMinPowermW}\t{StabilityMaxPowermW}\t{PowerStabilityPassed}\t" +
                $"{RangeTestPassed}\t{MinPowermW}\t{MaxPowermW}\t{LinearityPassed}\t{CalibratePassed}";
            if (TableSamples != null)
            {
                foreach (LpfTestIndex index in TableSamples)
                {
                    line = line + $"\t{index}";
                }
            }
            return line;
        }
    }

    struct LpfTestIndex
    {
        public int Index;
        public int PoweruW;
        public int PercentNominal;

        public bool CheckComplete()
        {
            if(PoweruW == default || PercentNominal == default) { return false; }
            return true;
        }

        public override string ToString()
        {
            return $"{Index}\t{PoweruW}\t{PercentNominal}";
        }
    }

    struct EvoPitchData
    {
        public DateTime Date;
        public string Operator;
        public bool PitchTestRan;
        public float XPixelsDelta;
        public float YPixelsDelta;

        public EvoPitchData(string line)
        {
            string[] split = line.Split("\t");
            if (split.Length < 9)
            {
                Date = new DateTime();
                Operator = "";
                PitchTestRan = false;
                XPixelsDelta = 0f;
                YPixelsDelta = 0f;
            }
            else
            {
                if (!DateTime.TryParseExact($"{split[0]} {split[1]}", "MM/dd/yy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out Date)) { Date = new DateTime(); }
                Operator = split[2];
                PitchTestRan = split[6] == "Y";
                if(!float.TryParse(split[7], out XPixelsDelta)) { XPixelsDelta = 0f; }
                if(!float.TryParse(split[8], out YPixelsDelta)) { YPixelsDelta = 0f; }
            }
        }
        public bool CheckComplete()
        {
            if(Date == new DateTime() || Operator == "")
            {return false;}
            return true;
        }
        public override string ToString()
        {
            return $"{Operator}\t{PitchTestRan}\t{XPixelsDelta}\t{YPixelsDelta}";
        }
    }

    struct EvoUffData
    {
        public DateTime Date;
        public string Operator;
        public float CameraTempC;
        public bool MinTempOverridden;
        public bool AlignTargetRan;
        public float TargetMonitorAngle;
        public bool FocusTestRan;
        public bool FocusExposureGood;
        public float FocusRow;
        public float FocusScore;

        public EvoUffData(string line)
        {
            string[] split = line.Split("\t");
            if(split.Length < 15)
            {
                Date = new DateTime();
                Operator = "";
                CameraTempC = 0f;
                MinTempOverridden = false;
                AlignTargetRan = false;
                TargetMonitorAngle = 0f;
                FocusTestRan = false;
                FocusExposureGood = false;
                FocusRow = 0;
                FocusScore = 0;
            }
            else
            {
                if (!DateTime.TryParseExact($"{split[0]} {split[1]}", "MM/dd/yy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out Date)) { Date = new DateTime(); }
                Operator = split[2];
                if (!float.TryParse(split[7], out CameraTempC)) { CameraTempC = 0f; }
                MinTempOverridden = split[8] == "Yes";
                AlignTargetRan = split[9] == "Yes";
                if (!float.TryParse(split[10], out TargetMonitorAngle)) { TargetMonitorAngle = 0f; }
                FocusTestRan = split[11] == "Yes";
                FocusExposureGood = split[12] == "Good";
                if (!float.TryParse(split[13], out FocusRow)) { FocusRow = 0; }
                if (!float.TryParse(split[14], out FocusScore)) { FocusScore = 0; }
            }
        }
        public bool CheckComplete()
        {
            if (Date == new DateTime() || Operator == "" || CameraTempC == 0f || TargetMonitorAngle == 0f || FocusScore == 0 )
            { return false; }
            else { return true; }
        }
        public override string ToString()
        {
            return $"{Operator}\t{CameraTempC}\t{MinTempOverridden}\t{AlignTargetRan}\t" +
                $"{TargetMonitorAngle}\t{FocusTestRan}\t{FocusExposureGood}\t{FocusRow}\t{FocusScore}";
        }
    }

    struct MirrorcleData
    {
        public string SerialNumber;
        public float ThetaX110;
        public float ThetaY110;
        public int FnX;
        public int FnY;
        public float PrcpMaxDiff;
        public decimal FitC0;
        public decimal FitC1;
        public decimal FitC2;
        public decimal FitC3;
        public decimal FitC4;
        public decimal FitC5;
        public float LinError;
        public bool IsAssigned;

        public MirrorcleData(string line)
        {
            string[] split = line.Split(",");
            if(split.Length < 31)
            {
                SerialNumber = default;
                ThetaX110 = default;
                ThetaY110 = default;
                FnX = default;
                FnY = default;
                PrcpMaxDiff = default;
                FitC0 = default;
                FitC1 = default;
                FitC2 = default;
                FitC3 = default;
                FitC4 = default;
                FitC5 = default;
                LinError = default;
                IsAssigned = false;
            }
            else
            {
                SerialNumber = split[0];
                if(!float.TryParse(split[17], out ThetaX110))   { ThetaX110 = default; }
                if(!float.TryParse(split[18], out ThetaY110))   { ThetaY110 = default; }
                if(!int.TryParse(split[19], out FnX))           { FnX = default; }
                if(!int.TryParse(split[20], out FnY))           { FnY = default; }
                if(!float.TryParse(split[21], out PrcpMaxDiff)) { PrcpMaxDiff = default; }
                if (!decimal.TryParse(split[22], NumberStyles.Float, CultureInfo.InvariantCulture, out FitC0)) { FitC0 = default; }
                if (!decimal.TryParse(split[23], NumberStyles.Float, CultureInfo.InvariantCulture, out FitC1)) { FitC1 = default; }
                if (!decimal.TryParse(split[24], NumberStyles.Float, CultureInfo.InvariantCulture, out FitC2)) { FitC2 = default; }
                if (!decimal.TryParse(split[25], NumberStyles.Float, CultureInfo.InvariantCulture, out FitC3)) { FitC3 = default; }
                if (!decimal.TryParse(split[26], NumberStyles.Float, CultureInfo.InvariantCulture, out FitC4)) { FitC4 = default; }
                if (!decimal.TryParse(split[27], NumberStyles.Float, CultureInfo.InvariantCulture, out FitC5)) { FitC5 = default; }
                if (!float.TryParse(split[28], out LinError)) { LinError = default; }
                IsAssigned = true;
            }
        }
        public bool CheckComplete()
        {
            return (
                SerialNumber != "" & ThetaX110 != default & ThetaY110 != default & FnX != default
                & FnY != default & PrcpMaxDiff != default & FitC0 != default & FitC1 != default & FitC2 != default
                & FitC3 != default & FitC4 != default & FitC5 != default & LinError != default & IsAssigned
                );
        }
        public override string ToString()
        {
            return $"{ThetaX110}\t{ThetaY110}\t{FnX}\t{FnY}\t{PrcpMaxDiff}\t{FitC0}\t{FitC1}\t{FitC2}\t{FitC3}\t{FitC4}\t{FitC5}\t{LinError}";
        }
    }
}
