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
            foreach (string line in File.ReadAllLines(Config.SoloLaserAlignLog))
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
            foreach (string line in File.ReadAllLines(Config.SoloFocusLog))
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
            List<string[]> resultSplit = GetResultsLogSplit();
            foreach (string[] lineSplit in resultSplit)
            {
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
