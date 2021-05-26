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
        static public HelixSoloSensor AllSoloDataSingle(HelixSoloSensor sensor)
        {
            if (sensor.SerialNumber != "")
            {
                return SingleSoloAccuracyFromLog(
                        SingleSensorSoloFocus(
                            SingleSensorSoloLaser(sensor
                            )));
            }
            else { return sensor; }
        }
        static public HelixSoloSensor SingleSensorSoloLaser(HelixSoloSensor sensor)
        {
            string lpfLog = @"\\castor\Production\Manufacturing\MfgSoftware\HelixSoloLaserAlignFixture\200-0654\Results\HelixSoloLaserAlignFixtureMasterLog.txt";
            foreach (string line in File.ReadAllLines(lpfLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 30) { continue; }
                if (sensor.SerialNumber == split[4])
                {
                    sensor.PartNumber = split[5];
                    sensor.LaserAlign = new SoloLaserAlign(line);
                }
            }
            return sensor;
        }
        static public HelixSoloSensor SingleSensorSoloFocus(HelixSoloSensor sensor)
        {
            string uffLog = @"\\castor\Production\Manufacturing\MfgSoftware\HelixSoloFocusFixture\200-0655\Results\HelixSoloFocusFixtureMasterLog.txt";
            foreach (string line in File.ReadAllLines(uffLog))
            {
                string[] split = line.Split("\t");
                if (split.Length < 20) { continue; }
                if (split[4] == sensor.SerialNumber)
                {
                    sensor.FocusData = new SoloFocusData(line);
                }
            }
            return sensor;
        }
        static public HelixSoloSensor SingleSoloAccuracyFromLog(HelixSoloSensor sensor, bool first = false)
        {
            List<string> resultsLog = new List<string>();
            resultsLog.AddRange(File.ReadAllLines($@"{Config.RectDataDir}\HelixRectResults.log"));

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
                        if (sensor.AccuracyResult.UpdateFromLog(lineSplit)) { if (first) { break; } }
                    }
                }
            }
            return sensor;
        }
    }
}
