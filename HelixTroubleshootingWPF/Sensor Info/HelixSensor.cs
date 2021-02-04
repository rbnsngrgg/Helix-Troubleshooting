using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
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
        public EvoPitchData PitchData { get; set; }
        public EvoUffData UffData { get; set; }
        public HelixEvoSensor() :base() { }
        public HelixEvoSensor(string sensorXmlFolder) : base(sensorXmlFolder) { }

    }
    class HelixSoloSensor : HelixSensor
    {
        public HelixSoloSensor() : base() { }
        public HelixSoloSensor(string sensorXmlFolder) : base(sensorXmlFolder) { }
    }

    class AccuracyResult
    {
        public DateTime Timestamp = new DateTime();
        public float ZeroDegree2Rms;
        public float ZeroDegreeMaxDev;

        public override string ToString()
        {
            return $"{Timestamp}\t{ZeroDegree2Rms}\t{ZeroDegreeMaxDev}";
        }
    }

    struct EvoDacMemsData
    {
        public double NLRange;
        public double NLCoverage;
        public double NRRange;
        public double NRCoverage;
        public double FLRange;
        public double FLCoverage;
        public double FRRange;
        public double FRCoverage;

        public EvoDacMemsData(string dataLine)
        {
            string[] split = dataLine.Split("\t");
            if(split.Length < 97) 
            {
                NLRange = 0;
                NLCoverage = 0;
                NRRange = 0;
                NRCoverage = 0;
                FLRange = 0;
                FLCoverage = 0;
                FRRange = 0;
                FRCoverage = 0;
                return; 
            }
            double.TryParse(split[80], out NLRange);
            double.TryParse(split[81], out NLCoverage);
            double.TryParse(split[85], out NRRange);
            double.TryParse(split[86], out NRCoverage);
            double.TryParse(split[90], out FLRange);
            double.TryParse(split[91], out FLCoverage);
            double.TryParse(split[95], out FRRange);
            double.TryParse(split[96], out FRCoverage);
        }

        public override string ToString()
        {
            return $"{NLRange}\t{NLCoverage}\t{NRRange}\t{NRCoverage}\t{FLRange}\t{FLCoverage}\t{FRRange}\t{FRCoverage}";
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
        public override string ToString()
        {
            return $"{Operator}\t{CameraTempC}\t{MinTempOverridden}\t{AlignTargetRan}\t" +
                $"{TargetMonitorAngle}\t{FocusTestRan}\t{FocusExposureGood}\t{FocusRow}\t{FocusScore}";
        }
    }
}
