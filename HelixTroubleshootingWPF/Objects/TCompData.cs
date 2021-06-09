using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelixTroubleshootingWPF.Objects
{
    class TCompData
    {
        public readonly List<DateTime> TimeData = new(); //Header = "unit"
        public readonly List<double> TempData = new(); //Header = "mm"
        public readonly Dictionary<string, List<double>> MeasurementData = new();

        public TCompData() { }
        public TCompData(string filePath)
        {
            ParseTCompData(filePath);
        }

        public void ClearData()
        {
            TimeData.Clear();
            TempData.Clear();
            MeasurementData.Clear();
        }
        public bool ParseTCompData(string filePath)
        {
            ClearData();
            try
            {
                if (File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);
                    string[] headerSplit = lines[0].Split('\t');
                    //For col in file
                    for (int i = 0; i < headerSplit.Length; i++)
                    {
                        string header = "";
                        List<double> data = new();
                        //For row in file
                        for (int j = 0; j < lines.Length; j++)
                        {
                            string[] lineSplit = lines[j].Split('\t');
                            if (i == 0) //Time data
                            {
                                if (j != 0) //Skip header row
                                {
                                    if (lineSplit[i].Length == 8)
                                    {
                                        TimeData.Add(DateTime.ParseExact(lineSplit[i], "HH:mm:ss", CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        TimeData.Add(DateTime.ParseExact(lineSplit[i], "H:mm:ss", CultureInfo.InvariantCulture));
                                    }
                                }
                            }
                            else if (i == 1) //Temperature data
                            {
                                if (j != 0)
                                {
                                    TempData.Add(double.Parse(lineSplit[i]));
                                }
                            }
                            else //Measurement Data
                            {
                                if (j == 0) { header = lineSplit[i]; }
                                else
                                {
                                    data.Add(double.Parse(lineSplit[i]));
                                }
                            }
                        }
                        if (header != "")
                        {
                            if (!MeasurementData.ContainsKey(header)) { MeasurementData.Add(header, data); }
                        }
                    }
                    return true;
                }
                return false;
            }
            catch
            {
                ClearData();
                return false;
            }
        }
        public bool IsValid()
        {
            int entries = TimeData.Count;
            if(TempData.Count != entries || entries < 91 || entries > 93 || MeasurementData.Count < 33) { return false; }
            foreach(List<double> series in MeasurementData.Values)
            {
                if(series.Count != entries) { return false; }
                foreach(double value in series)
                {
                    if(value > 500) //Eliminate t-comp data with algo/acquisition errors
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public List<string> GetLines()
        {
            List<string> lines = new();
            string header = $"unit\tmm";
            foreach(string key in MeasurementData.Keys)
            {
                header += $"\t{key}";
            }
            lines.Add(header);
            for(int i = 0; i < TimeData.Count; i++)
            {
                string line = $"{TimeData[i]:HH:mm:ss}\t{TempData[i]}";
                foreach(string key in MeasurementData.Keys)
                {
                    line += $"\t{MeasurementData[key][i]}";
                }
                lines.Add(line);
            }
            return lines;
        }
    }
}
