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

        //Constructors
        public HelixSensor()
        {
            
        }
        public HelixSensor(string sensorXml, bool rawXml = false)
        {
            if (!rawXml) { GetSensorData(sensorXml); }
            else { GetSensorDataFromString(sensorXml); }
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
        public bool GetSensorDataFromString(string xmlString)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlString);
            //string format = "ddd MMM  d HH:mm:ss yyyy";
            //Date = DateTime.ParseExact("Wed Jul 22 08:24:53 2020\n", format, CultureInfo.InvariantCulture);
            Date = xml.LastChild.Attributes[1].Value.Replace("\n", "").Replace("\r", ""); //RECT_OUTPUT > Date
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
        public VDEResult VDE { get; set; } = new VDEResult();
        public HelixEvoSensor() :base() { }
        public HelixEvoSensor(string sensorXmlFolder, bool rawXml = false) : base(sensorXmlFolder, rawXml) { }
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
        public SoloFocusData FocusData { get; set; }
        public SoloLaserAlign LaserAlign { get; set; }
        public HelixSoloSensor() : base() { }
        public HelixSoloSensor(string sensorXmlFolder, bool rawXml = false) : base(sensorXmlFolder, rawXml) { }
    }

    class AccuracyResult
    {
        private DateTime timestamp = new DateTime();
        private float zeroDegree2Rms = 0f;
        private float zeroDegreeMaxDev = 0f;
        public DateTime Timestamp { get => timestamp; }
        public float ZeroDegree2Rms { get => zeroDegree2Rms; }
        public float ZeroDegreeMaxDev { get => zeroDegreeMaxDev; }

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
                            timestamp = newTimestamp;
                        }
                        else if (line.Contains("Line_Zero Test Type"))
                        { zeroDegree = true; }
                        else if (line.Contains("Maximum deviation") & zeroDegree)
                        { float.TryParse(line.Split("=")[1].Split(",")[0], out zeroDegreeMaxDev); }
                        else if (line.Contains("2RMS deviation") & zeroDegree)
                        { float.TryParse(line.Split("=")[1].Split(",")[0], out zeroDegree2Rms); return true; }
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
            zeroDegree2Rms = zero2Rms;
            zeroDegreeMaxDev = zeroMaxDev;
            timestamp = newTimestamp;
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

    //Evo data
    class VDEResult
    {
        private DateTime timestamp = new DateTime();
        private float sphereSpacingError = 0f;
        private float sphereProbingErrorSize = 0f;
        private float sphereProbingErrorForm = 0f;
        private float planeProbingError = 0f;

        public DateTime Timestamp { get => timestamp; }
        public float SphereSpacingError { get => sphereSpacingError; }
        public float SphereProbingErrorSize { get => sphereProbingErrorSize; }
        public float SphereProbingErrorForm { get => sphereProbingErrorForm; }
        public float PlaneProbingError { get => planeProbingError; }

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
                            timestamp = newTimestamp;
                        }
                        else if (line.Contains("VDE-2634 Accuracy Test: Inspection Results"))
                        { results = true; }
                        else if (line.Contains("Sphere Spacing Error (SD)") & results)
                        { float.TryParse(line.Split(" ")[4], out sphereSpacingError); }
                        else if (line.Contains("Sphere Probing Error - Size (Ps)") & results)
                        { float.TryParse(line.Split("=")[6], out sphereProbingErrorSize); }
                        else if (line.Contains("Sphere Probing Error - Form (Pf)") & results)
                        { float.TryParse(line.Split("=")[6], out sphereProbingErrorForm); }
                        else if (line.Contains("Plane Probing Error  - Form (F)") & results)
                        { float.TryParse(line.Split("=")[6], out planeProbingError); return true; }
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
            float.TryParse(line[176], out sphereSpacingError);
            float.TryParse(line[179], out sphereProbingErrorForm);
            float.TryParse(line[182], out sphereProbingErrorSize);
            float.TryParse(line[185], out planeProbingError);
            if (sphereSpacingError == 0f || sphereProbingErrorForm == 0f) { return false; }
            timestamp = newTimestamp;
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
        public string MemsSerialNumber { get => memsSerialNumber; }
        private string memsSerialNumber;
        public double NLRange { get => nlRange; }
        private double nlRange;
        public double NLCoverage { get => nlCoverage; }
        private double nlCoverage;
        public double NRRange { get => nrRange; }
        private double nrRange;
        public double NRCoverage { get => nrCoverage; }
        private double nrCoverage;
        public double FLRange { get => flRange; }
        private double flRange;
        public double FLCoverage { get => flCoverage; }
        private double flCoverage;
        public double FRRange { get => frRange; }
        private double frRange;
        public double FRCoverage { get => frCoverage; }
        private double frCoverage;
        public bool IsAssigned;

        public EvoDacMemsData(string dataLine)
        {
            string[] split = dataLine.Split("\t");
            if(split.Length < 97) 
            {
                memsSerialNumber = "";
                nlRange = 0d;
                nlCoverage = 0d;
                nrRange = 0d;
                nrCoverage = 0d;
                flRange = 0d;
                flCoverage = 0d;
                frRange = 0d;
                frCoverage = 0d;
                IsAssigned = false;
                return; 
            }
            memsSerialNumber = split[5];
            double.TryParse(split[80], out nlRange);
            double.TryParse(split[81], out nlCoverage);
            double.TryParse(split[85], out nrRange);
            double.TryParse(split[86], out nrCoverage);
            double.TryParse(split[90], out flRange);
            double.TryParse(split[91], out flCoverage);
            double.TryParse(split[95], out frRange);
            double.TryParse(split[96], out frCoverage);
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
        private DateTime date;
        private string operatorInitials;
        private bool meterZeroed;
        private int meterBackgroundBiasuW;
        private bool meterAligned;
        private float xAlignMm;
        private float yAlignMm;
        private bool powerStabilityRan;
        private float stabilityMinPowermW;
        private float stabilityMaxPowermW;
        private bool powerStabilityPassed;
        private bool rangeTestRan;
        private bool rangeTestPassed;
        private float minPowermW;
        private float maxPowermW;
        private bool linearityRan;
        private bool linearityPassed;
        private bool calibrateRan;
        private bool calibratePassed;
        private bool amSamplingRan;
        private List<LpfTestIndex> tableSamples;

        public DateTime Date { get => date; }
        public string Operator { get => operatorInitials; }
        public bool MeterZeroed { get => meterZeroed; }
        public int MeterBackgroundBiasuW { get => meterBackgroundBiasuW; }
        public bool MeterAligned { get => meterAligned; }
        public float XAlignMm { get => xAlignMm; }
        public float YAlignMm { get => yAlignMm; }
        public bool PowerStabilityRan { get => powerStabilityRan; }
        public float StabilityMinPowermW { get => stabilityMinPowermW; }
        public float StabilityMaxPowermW { get => stabilityMaxPowermW; }
        public bool PowerStabilityPassed { get => powerStabilityPassed; }
        public bool RangeTestRan { get => rangeTestRan; }
        public bool RangeTestPassed { get => rangeTestPassed; }
        public float MinPowermW { get => minPowermW; }
        public float MaxPowermW { get => maxPowermW; }
        public bool LinearityRan { get => linearityRan; }
        public bool LinearityPassed { get => linearityPassed; }
        public bool CalibrateRan { get => calibrateRan; }
        public bool CalibratePassed { get => calibratePassed; }
        public bool AmSamplingRan { get => amSamplingRan; }
        public List<LpfTestIndex> TableSamples { get => tableSamples; }

        public EvoLpfData(string line)
        {
            string[] split = line.Split("\t");
            if(split.Length < 65)
            {
                date = new DateTime();
                operatorInitials = "";
                meterZeroed = false;
                meterBackgroundBiasuW = 0;
                meterAligned = false;
                xAlignMm = 0f;
                yAlignMm = 0f;
                powerStabilityRan = false;
                stabilityMinPowermW = 0f;
                stabilityMaxPowermW = 0f;
                powerStabilityPassed = false;
                rangeTestRan = false;
                rangeTestPassed = false;
                minPowermW = 0f;
                maxPowermW = 0f;
                linearityRan = false;
                linearityPassed = false;
                calibrateRan = false;
                calibratePassed = false;
                amSamplingRan = false;
                tableSamples = new List<LpfTestIndex>();
            }
            else
            {
                if (!DateTime.TryParseExact($"{split[0]} {split[1]}", "MM/dd/yy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date)) { date = new DateTime(); }
                operatorInitials = split[2];
                meterZeroed = split[6] == "Y";
                if (!int.TryParse(split[7], out meterBackgroundBiasuW)) { meterBackgroundBiasuW = 0; }
                meterAligned = split[8] == "Y";
                if (!float.TryParse(split[9], out xAlignMm)) { xAlignMm = 0f; }
                if (!float.TryParse(split[10], out yAlignMm)) { yAlignMm = 0f; }
                powerStabilityRan = split[11] == "Y";
                if (!float.TryParse(split[12], out stabilityMinPowermW)) { stabilityMinPowermW = 0f; }
                if (!float.TryParse(split[13], out stabilityMaxPowermW)) { stabilityMaxPowermW = 0f; }
                powerStabilityPassed = split[14] == "Y";
                rangeTestRan = split[15] == "Y";
                rangeTestPassed = split[16] == "Y";
                if (!float.TryParse(split[17], out minPowermW)) { minPowermW = 0f; }
                if (!float.TryParse(split[18], out maxPowermW)) { maxPowermW = 0f; }
                linearityRan = split[19] == "Y";
                linearityPassed = split[20] == "Y";
                calibrateRan = split[21] == "Y";
                calibratePassed = split[22] == "Y";
                amSamplingRan = split[23] == "Y";
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
                tableSamples = newSamples;
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
        private int index;
        private int poweruW;
        private int percentNominal;

        public int Index { get => index; set => index = value; }
        public int PoweruW { get => poweruW; set => poweruW = value; }
        public int PercentNominal { get => percentNominal; set => percentNominal = value; }

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
        private DateTime date;
        private string operatorInitials;
        private bool pitchTestRan;
        private float xPixelsDelta;
        private float yPixelsDelta;

        public DateTime Date { get => date; }
        public string Operator { get => operatorInitials; }
        public bool PitchTestRan { get => pitchTestRan; }
        public float XPixelsDelta { get => xPixelsDelta; }
        public float YPixelsDelta { get => yPixelsDelta; }

        public EvoPitchData(string line)
        {
            string[] split = line.Split("\t");
            if (split.Length < 9)
            {
                date = new DateTime();
                operatorInitials = "";
                pitchTestRan = false;
                xPixelsDelta = 0f;
                yPixelsDelta = 0f;
            }
            else
            {
                if (!DateTime.TryParseExact($"{split[0]} {split[1]}", "MM/dd/yy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date)) { date = new DateTime(); }
                operatorInitials = split[2];
                pitchTestRan = split[6] == "Y";
                if(!float.TryParse(split[7], out xPixelsDelta)) { xPixelsDelta = 0f; }
                if(!float.TryParse(split[8], out yPixelsDelta)) { yPixelsDelta = 0f; }
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
        public DateTime Date { get => date; }
        private DateTime date;
        public string Operator { get => operatorInitials; }
        private string operatorInitials;
        public float CameraTempC { get => cameraTempC; }
        private float cameraTempC;
        public bool MinTempOverridden { get => minTempOverridden; }
        private bool minTempOverridden;
        public bool AlignTargetRan { get => alignTargetRan; }
        private bool alignTargetRan;
        public float TargetMonitorAngle { get => targetMonitorAngle; }
        private float targetMonitorAngle;
        public bool FocusTestRan { get => focusTestRan; }
        private bool focusTestRan;
        public bool FocusExposureGood { get => focusExposureGood; }
        private bool focusExposureGood;
        public float FocusRow { get => focusRow; }
        private float focusRow;
        public float FocusScore { get => focusScore;}
        private float focusScore;

        public EvoUffData(string line)
        {
            string[] split = line.Split("\t");
            if(split.Length < 15)
            {
                date = new DateTime();
                operatorInitials = "";
                cameraTempC = 0f;
                minTempOverridden = false;
                alignTargetRan = false;
                targetMonitorAngle = 0f;
                focusTestRan = false;
                focusExposureGood = false;
                focusRow = 0;
                focusScore = 0;
            }
            else
            {
                if (!DateTime.TryParseExact($"{split[0]} {split[1]}", "MM/dd/yy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date)) { date = new DateTime(); }
                operatorInitials = split[2];
                if (!float.TryParse(split[7], out cameraTempC)) { cameraTempC = 0f; }
                minTempOverridden = split[8] == "Yes";
                alignTargetRan = split[9] == "Yes";
                if (!float.TryParse(split[10], out targetMonitorAngle)) { targetMonitorAngle = 0f; }
                focusTestRan = split[11] == "Yes";
                focusExposureGood = split[12] == "Good";
                if (!float.TryParse(split[13], out focusRow)) { focusRow = 0; }
                if (!float.TryParse(split[14], out focusScore)) { focusScore = 0; }
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
        private string serialNumber;
        private float thetaX110;
        private float thetaY110;
        private int fnX;
        private int fnY;
        private float prcpMaxDiff;
        private decimal fitC0;
        private decimal fitC1;
        private decimal fitC2;
        private decimal fitC3;
        private decimal fitC4;
        private decimal fitC5;
        private float linError;

        public string SerialNumber { get => serialNumber; }
        public float ThetaX110 { get => thetaX110; }
        public float ThetaY110 { get => thetaY110; }
        public int FnX { get => fnX; }
        public int FnY { get => fnY; }
        public float PrcpMaxDiff { get => prcpMaxDiff; }
        public decimal FitC0 { get => fitC0; }
        public decimal FitC1 { get => fitC1; }
        public decimal FitC2 { get => fitC2; }
        public decimal FitC3 { get => fitC3; }
        public decimal FitC4 { get => fitC4; }
        public decimal FitC5 { get => fitC5; }
        public float LinError { get => linError; }
        public bool IsAssigned;

        public MirrorcleData(string line)
        {
            string[] split = line.Split(",");
            if(split.Length < 31)
            {
                serialNumber = default;
                thetaX110 = default;
                thetaY110 = default;
                fnX = default;
                fnY = default;
                prcpMaxDiff = default;
                fitC0 = default;
                fitC1 = default;
                fitC2 = default;
                fitC3 = default;
                fitC4 = default;
                fitC5 = default;
                linError = default;
                IsAssigned = false;
            }
            else
            {
                serialNumber = split[0];
                if(!float.TryParse(split[17], out thetaX110))   { thetaX110 = default; }
                if(!float.TryParse(split[18], out thetaY110))   { thetaY110 = default; }
                if(!int.TryParse(split[19], out fnX))           { fnX = default; }
                if(!int.TryParse(split[20], out fnY))           { fnY = default; }
                if(!float.TryParse(split[21], out prcpMaxDiff)) { prcpMaxDiff = default; }
                if (!decimal.TryParse(split[22], NumberStyles.Float, CultureInfo.InvariantCulture, out fitC0)) { fitC0 = default; }
                if (!decimal.TryParse(split[23], NumberStyles.Float, CultureInfo.InvariantCulture, out fitC1)) { fitC1 = default; }
                if (!decimal.TryParse(split[24], NumberStyles.Float, CultureInfo.InvariantCulture, out fitC2)) { fitC2 = default; }
                if (!decimal.TryParse(split[25], NumberStyles.Float, CultureInfo.InvariantCulture, out fitC3)) { fitC3 = default; }
                if (!decimal.TryParse(split[26], NumberStyles.Float, CultureInfo.InvariantCulture, out fitC4)) { fitC4 = default; }
                if (!decimal.TryParse(split[27], NumberStyles.Float, CultureInfo.InvariantCulture, out fitC5)) { fitC5 = default; }
                if (!float.TryParse(split[28], out linError)) { linError = default; }
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

    //Solo data
    struct SoloFocusData
    {
        public DateTime Date { get => date; }
        private DateTime date;
        public string Operator { get => operatorInitials; }
        private string operatorInitials;
        public float CameraTempC { get => cameraTempC; }
        private float cameraTempC;
        public float XOffsetPixels { get => xOffsetPixels; }
        private float xOffsetPixels;
        public float YOffsetPixels { get => yOffsetPixels; }
        private float yOffsetPixels;
        public float ZRotationDeg { get => zRotationDeg; }
        private float zRotationDeg;
        public int FocusExposure { get => focusExposure; }
        private int focusExposure;
        public float FocusScore { get => focusScore; }
        private float focusScore;
        public float FocusRow { get => focusRow; }
        private float focusRow;

        public SoloFocusData(string line)
        {
            string[] split = line.Split("\t");
            if (split.Length < 21)
            {
                date = new DateTime();
                operatorInitials = "";
                cameraTempC = 0f;
                xOffsetPixels = 0f;
                yOffsetPixels = 0f;
                zRotationDeg = 0f;
                focusExposure = 0;
                focusRow = 0f;
                focusScore = 0f;
            }
            else
            {
                if (!DateTime.TryParseExact($"{split[0]}", "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date)) { date = new DateTime(); }
                operatorInitials = split[2];
                if (!float.TryParse(split[8], out cameraTempC)) { cameraTempC = 0f; }
                if (!float.TryParse(split[14], out xOffsetPixels)) { xOffsetPixels = 0f; }
                if (!float.TryParse(split[15], out yOffsetPixels)) { yOffsetPixels = 0f; }
                if (!float.TryParse(split[16], out zRotationDeg)) { zRotationDeg = 0f; }
                if (!int.TryParse(split[18], out focusExposure)) { focusExposure = 0; }
                if (!float.TryParse(split[20], out focusScore)) { focusScore = 0f; }
                if (!split[6].Contains("917") || !float.TryParse(split[21], out focusRow)) { focusRow = 0f; }
            }
        }
        public bool CheckComplete()
        {
            if (Date == new DateTime() || Operator == "" || CameraTempC == 0f || XOffsetPixels == 0f || YOffsetPixels == 0f
                || ZRotationDeg == 0f || FocusExposure == 0 || FocusScore == 0f)
            { return false; }
            else { return true; }
        }
        public override string ToString()
        {
            return $"{Operator}\t{CameraTempC}\t{XOffsetPixels}\t{YOffsetPixels}\t" +
                $"{ZRotationDeg}\t{FocusExposure}\t{FocusScore}\t{FocusRow}";
        }
    }
    struct SoloLaserAlign
    {
        public DateTime Date { get => date; }
        private DateTime date;
        public string Operator { get => operatorInitials; }
        private string operatorInitials;
        public float LeftCameraTempC { get => leftCameraTempC; }
        private float leftCameraTempC;
        public float RightCameraTempC { get => rightCameraTempC; }
        private float rightCameraTempC;
        public float LaserPowermW { get => laserPowermW; }
        private float laserPowermW;
        public float LaserPowerPercent { get => laserPowerPercent; }
        private float laserPowerPercent;
        public float LaserPitchDeg { get => laserPitchDeg; }
        private float laserPitchDeg;
        public float LaserRollDeg { get => laserRollDeg; }
        private float laserRollDeg;
        public float LaserOffsetmm { get => laserOffsetmm; }
        private float laserOffsetmm;
        public float AvgTpWidthLeft { get => avgTpWidthLeft; }
        private float avgTpWidthLeft;
        public float AvgTpWidthRight { get => avgTpWidthRight; }
        private float avgTpWidthRight;
        public float LinearityLeft { get => linearityLeft; }
        private float linearityLeft;
        public float LinearityRight { get => linearityRight; }
        private float linearityRight;
        public SoloLaserAlign(string line)
        {
            string[] split = line.Split("\t");
            if (split.Length < 21)
            {
                date = new DateTime();
                operatorInitials = "";
                leftCameraTempC = 0f;
                rightCameraTempC = 0f;
                laserPowermW = 0f;
                laserPowerPercent = 0f;
                laserPitchDeg = 0f;
                laserRollDeg = 0f;
                laserOffsetmm = 0f;
                avgTpWidthLeft = 0f;
                avgTpWidthRight = 0f;
                linearityLeft = 0f;
                linearityRight = 0f;
            }
            else
            {
                if (!DateTime.TryParseExact($"{split[0]}", "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date)) { date = new DateTime(); }
                operatorInitials = split[2];
                if (!float.TryParse(split[7], out leftCameraTempC)) { leftCameraTempC = 0f; }
                if (!float.TryParse(split[8], out rightCameraTempC)) { rightCameraTempC = 0f; }
                if (!float.TryParse(split[14], out laserPowermW)) { laserPowermW = 0f; }
                if (!float.TryParse(split[15], out laserPowerPercent)) { laserPowerPercent = 0f; }
                if (!float.TryParse(split[17], out laserPitchDeg)) { laserPitchDeg = 0f; }
                if (!float.TryParse(split[18], out laserRollDeg)) { laserRollDeg = 0f; }
                if (!float.TryParse(split[19], out laserOffsetmm)) { laserOffsetmm = 0f; }
                if (!float.TryParse(split[22], out avgTpWidthLeft)) { avgTpWidthLeft = 0f; }
                if (!float.TryParse(split[26], out avgTpWidthRight)) { avgTpWidthRight = 0f; }
                if (!float.TryParse(split[28], out linearityLeft)) { linearityLeft = 0f; }
                if (!float.TryParse(split[29], out linearityRight)) { linearityRight = 0f; }
            }
        }
        public bool CheckComplete()
        {
            if (Date == new DateTime() || Operator == "" || leftCameraTempC == 0f || rightCameraTempC == 0f || laserPowermW == 0f
                || laserPowerPercent == 0f || laserPitchDeg == 0f || laserRollDeg == 0f || laserOffsetmm == 0f || avgTpWidthLeft == 0f
                || avgTpWidthRight == 0f || linearityLeft == 0f || linearityRight == 0f)
            { return false; }
            else { return true; }
        }
        public override string ToString()
        {
            return $"{Operator}\t{LeftCameraTempC}\t{RightCameraTempC}\t{LaserPowermW}\t{LaserPowerPercent}\t{LaserPitchDeg}" +
                $"{LaserRollDeg}\t{LaserOffsetmm}\t{AvgTpWidthLeft}\t{AvgTpWidthRight}\t{LinearityLeft}\t{LinearityRight}";
        }
    }
}
