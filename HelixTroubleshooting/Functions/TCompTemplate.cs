using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using HelixTroubleshootingWPF.Objects;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        public static void Template(string model, int months)
        {
            Tuple<TCompData, int> templateData = GenerateTemplate(model, months);
            TCompData template = templateData.Item1;
            List<string> lines = template.GetLines();
            string templateFolder = Path.Join(Config.ResultsDir, "\\TComp_Templates\\GenericTemplates");
            Directory.CreateDirectory(templateFolder);
            WriteFile(templateFolder, Path.Join(templateFolder, $"{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_{model}_Template.txt"), lines);
            MessageBox.Show($"{model} template generated using the data from {templateData.Item2} sensors from the past {months} months.",
                "Template Generated", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ApplyTemplate(string serialNumber, int months)
        {
            HelixEvoSensor sensor = AllEvoDataSingle(new() { SerialNumber = serialNumber });
            Tuple<TCompData, int> templateData = GenerateTemplate(sensor.PartNumber.Substring(0,8), months); //Only get 920-0201 portion of PN
            TCompData template = templateData.Item1;
            List<string> lines = template.GetLines();
            string snGroupFolder = GetGroupFolder(Config.TcompDir, sensor.SerialNumber);

            string tcompPath = Path.Join(snGroupFolder, $"SN{sensor.SerialNumber}.txt");

            if (File.Exists(tcompPath)) { TCompBackup(tcompPath); }
            WriteFile(snGroupFolder, tcompPath, lines, false);
            LogTemplateApplication(sensor, templateData.Item2, months);
            MessageBox.Show($"Template generated for SN{sensor.SerialNumber}, model {sensor.PartNumber}. Used data from {templateData.Item2} sensors" +
                $" from the past {months} months.", "Template Applied", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static Tuple<TCompData,int> GenerateTemplate(string model, int months)
        {
            List<HelixEvoSensor> sensors = GetEvoData();
            TCompData template = new();
            Dictionary<string, List<double>> deltas = new(); //Deltas begin after reference cycle
            int datacount = 0;
            //Only include sensors within the last x months
            for(int i = sensors.Count-1; i >=0; i--)
            {
                if (!sensors[i].PartNumber.Contains(model) || sensors[i].AccuracyResult.Timestamp < DateTime.Now.AddMonths(-months))
                { sensors.RemoveAt(i); }
            }

            GetTcompData(ref sensors);
            foreach (HelixEvoSensor sensor in sensors)
            {
                if(sensor.PartNumber.Contains(model) && sensor.TComp.IsValid())
                {
                    if(template.TimeData.Count == 0) //Import arbitrary time data
                    {
                        template.TimeData.AddRange(sensor.TComp.TimeData);
                        template.TempData.AddRange(sensor.TComp.TempData);
                        foreach(string key in sensor.TComp.MeasurementData.Keys)
                        {
                            List<double> addData = new();
                            addData.AddRange(sensor.TComp.MeasurementData[key]);
                            template.MeasurementData.Add(key, addData);
                            deltas.Add(key, new List<double>());
                        }
                        deltas.Add("temp", new List<double>());
                        TrimTemplate(ref template, true);
                        FillTemplate(ref template);
                    }
                    else
                    {
                        //Check that all keys are present in the sensor's tcomp data
                        //Skip sensor if not all keys are present, so the integrity of the final result is maintained.
                        bool allKeys = true;
                        foreach(string key in template.MeasurementData.Keys) { if (!sensor.TComp.MeasurementData.ContainsKey(key)) allKeys = false; }
                        if (!allKeys) { continue; }
                        for (int i = 0; i < 5; i++) //Apply values for  reference cycle temperature
                        {
                            template.TempData[i] += sensor.TComp.TempData[i];
                        }
                    }
                    //Temperature data deltas
                    for (int i = 5; i < sensor.TComp.TempData.Count; i++)
                    {
                        if(deltas["temp"].Count <= i-5) { deltas["temp"].Add(0.0); }
                        deltas["temp"][i-5] += sensor.TComp.TempData[i] - sensor.TComp.TempData[i - 1];
                    }
                    //Measurement data deltas
                    foreach (string key in template.MeasurementData.Keys)
                    {
                        for (int i = 5; i < sensor.TComp.TimeData.Count; i++)
                        {
                            if(deltas[key].Count <= i-5) { deltas[key].Add(0.0); } //Add new delta entry if it doesn't exist yet
                            if (sensor.TComp.MeasurementData.ContainsKey(key))
                            {
                                //Apply values for current delta
                                deltas[key][i-5] += sensor.TComp.MeasurementData[key][i] - sensor.TComp.MeasurementData[key][i - 1]; 
                            }
                        }
                    }
                    datacount++;
                }
            }
            //Average reference cycle values and deltas
            for(int i = 0; i < 86; i++) //deltas should have at least 86 values
            {
                if (i < 5) { template.TempData[i] /= datacount; }
                foreach(string key in deltas.Keys)
                {
                    deltas[key][i] /= datacount;
                }
            }
            ApplyDeltas(ref template, ref deltas);
            return new Tuple<TCompData,int> (template, datacount);
        }

        public static void ApplyDeltas(ref TCompData template, ref Dictionary<string, List<double>> deltas)
        {
            for(int i = 5; i < 91; i++)
            {
                template.TempData[i] = template.TempData[i - 1] + deltas["temp"][i - 5]; //Apply delta to previous value to get current value
                foreach(string key in template.MeasurementData.Keys)
                {
                    template.MeasurementData[key][i] = template.MeasurementData[key][i - 1] + deltas[key][i - 5];
                }
            }
        }

        public static void TrimTemplate(ref TCompData template, bool reference = false)
        {
            int trimTo = reference ? 5 : 91;
            //Trim all data columns to length of 91
            while (template.TimeData.Count > 91) //Ensure count is 91
            {
                template.TimeData.RemoveAt(template.TimeData.Count - 1);
            }
            while(template.TempData.Count > trimTo)
            {
                template.TempData.RemoveAt(template.TempData.Count - 1);
            }
            foreach(string key in template.MeasurementData.Keys)
            {
                while(template.MeasurementData[key].Count > trimTo)
                {
                    template.MeasurementData[key].RemoveAt(template.MeasurementData[key].Count - 1);
                }
            }
        }
        public static void FillTemplate(ref TCompData template)
        {
            //Fill template up to count 91 with 0.0 values
            while (template.TempData.Count < 91)
            {
                template.TempData.Add(0.0);
            }
            foreach (string key in template.MeasurementData.Keys)
            {
                while (template.MeasurementData[key].Count < 91)
                {
                    template.MeasurementData[key].Add(0.0);
                }
            }
        }

        public static void LogTemplateApplication(HelixEvoSensor sensor, int numSensors, int months)
        {
            List<string> headers = new() { "SerialNumber\tFilter\tDateGenerated\t#SensorsUsed\t#Months" };
            string templateLogPath = Path.Join(Config.ResultsDir, "\\TComp_Templates\\Template.log");
            if (!File.Exists(templateLogPath))
            {
                File.WriteAllLines(templateLogPath, headers);
            }
            File.AppendAllText(templateLogPath, 
                $"\n{sensor.SerialNumber}\t{sensor.PartNumber.Substring(0,8)}\t{DateTime.Now:yyyy-MM-dd-HH-mm-ss}\t{numSensors}\t{months}");
        }
    }
}
