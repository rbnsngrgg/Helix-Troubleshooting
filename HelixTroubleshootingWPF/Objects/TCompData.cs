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
        public bool ParseTCompData(string filePath)
        {
            TimeData.Clear();
            TempData.Clear();
            MeasurementData.Clear();
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
                                TimeData.Add(DateTime.ParseExact(lineSplit[i], "HH:mm:ss", CultureInfo.InvariantCulture));
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
                        MeasurementData.Add(header, data);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
