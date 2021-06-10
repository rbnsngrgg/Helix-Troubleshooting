using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelixTroubleshootingWPF.Objects
{
    class EvoDataframe
    {
        //Dataframe for sorted and filtered data. All data in the EvoDataPoint.Data list will be used in KNN
        public bool Scaled { get; private set; } = false;
        public bool Sorted { get; private set; } = false;
        public List<string> Headers { get; set; } = new List<string>();
        public List<EvoDataPoint> OriginalDataPoints { get; set; } = new List<EvoDataPoint>();
        public List<EvoDataPoint> DataPoints { get; set; } = new List<EvoDataPoint>();
        public List<int> Columns { get; set; }
        public DateTime Timestamp { get; private set; }
        public int NumNeighbors { get; set; } = 10;
        public double P { get; set; } = 1.0;

        public EvoDataframe(List<HelixEvoSensor> filteredData, List<string> headerList, List<int> columns) //First col is ID col, 2nd col is label col, rest are data
        {
            Columns = columns;
            //FilteredData is list of all Evo sensors that are data-complete
            foreach(int col in Columns)
            {
                Headers.Add(headerList[col]);
            }
            foreach(HelixEvoSensor sensor in filteredData)
            {
                List<string> data = sensor.GetDataList();
                List<double> doubleData = new List<double>();
                for(int i = 2; i < Columns.Count; i++)
                {
                    if(double.TryParse(data[Columns[i]], out double dataPoint))
                    {
                        doubleData.Add(dataPoint);
                    }
                }
                if(double.TryParse(data[Columns[1]], out double label))
                { 
                    DataPoints.Add(new EvoDataPoint(data[Columns[0]], label, doubleData));
                }
            }
            foreach (HelixEvoSensor sensor in filteredData)
            {
                List<string> data = sensor.GetDataList();
                List<double> doubleData = new List<double>();
                for (int i = 2; i < Columns.Count; i++)
                {
                    if (double.TryParse(data[Columns[i]], out double dataPoint))
                    {
                        doubleData.Add(dataPoint);
                    }
                }
                if (double.TryParse(data[Columns[1]], out double label))
                {
                    OriginalDataPoints.Add(new EvoDataPoint(data[Columns[0]], label, doubleData));
                }
            }
            Timestamp = DateTime.UtcNow;
            NumNeighbors = (int)Math.Sqrt((double)DataPoints.Count);
            //NumNeighbors = 7;
        }
        public void ExportData(string resultDir)
        {
            if(Directory.Exists(resultDir))
            {
                string folderName = $"{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}_KnnDataFrame";
                string folderPath = Path.Join(resultDir, folderName);
                Directory.CreateDirectory(folderPath);
                List<string> lines = new List<string>();
                string header = "";
                foreach(string col in Headers) { header += $"{col}\t"; }
                lines.Add(header);
                foreach(EvoDataPoint dataPoint in DataPoints)
                {
                    lines.Add(dataPoint.ToString());
                }
                string fileName = $"{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}_KnnDataFrame_Scaled{Scaled}.txt";
                File.WriteAllLines(Path.Join(folderPath, fileName), lines);
            }
        }
        public EvoDataPoint? ConvertNewData(HelixEvoSensor sensor)
        {
            //Return null if the new data point does not contain all needed values
            List<string> data = sensor.GetDataList();
            List<double> doubleData = new List<double>();
            for (int i = 2; i < Columns.Count; i++)
            {
                if(data.Count < Columns[i]) { return null; }
                if (double.TryParse(data[Columns[i]], out double dataPoint))
                {
                    doubleData.Add(dataPoint);
                }
            }
            if (double.TryParse(data[Columns[1]], out double label))
            { 
                return new EvoDataPoint(data[Columns[0]], label, doubleData);
            }
            return null;
        }
        public void DimensionalityReduction(List<List<int>> groups, string resultDir)
        {
            if (Directory.Exists(resultDir))
            {
                string folderName = $"{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}_KnnDataFrame";
                string folderPath = Path.Join(resultDir, folderName);
                Directory.CreateDirectory(folderPath);
                List<string> lines = new List<string>();
                string header = $"{Headers[0]}\t{Headers[1]}";
                foreach(List<int> group in groups)
                {
                    string groupHeader = "";
                    foreach(int col in group)
                    {
                        groupHeader += $"{Headers[col+2]}_";
                    }
                    header += "\t" + groupHeader;
                }
                lines.Add(header);
                foreach(EvoDataPoint dataPoint in DataPoints)
                {
                    string line = $"{dataPoint.Id}\t{dataPoint.Label}";
                    foreach(List<int> group in groups)
                    {
                        double colValue = 0.0;
                        foreach(int col in group)
                        {
                            colValue += dataPoint.Data[col];
                        }
                        line += $"\t{colValue}";
                    }
                    lines.Add(line);
                }
                string fileName = $"{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}_KnnDataFrame_DReduction.txt";
                File.WriteAllLines(Path.Join(folderPath, fileName), lines);
            }
        }
        public double GetMinkowski(EvoDataPoint data1, EvoDataPoint data2, List<int> colOverride = null)
        {
            double sumOfDiffs = 0.0;
            if (colOverride == null)
            {
                for (int i = 0; i < data1.Data.Count; i++)
                {
                    sumOfDiffs += Math.Pow(Math.Abs(data1.Data[i] - data2.Data[i]), P);
                }
            }
            else
            {
                foreach(int col in colOverride)
                {
                    sumOfDiffs += Math.Pow(Math.Abs(data1.Data[Columns.IndexOf(col)-2] - data2.Data[Columns.IndexOf(col)-2]), P);
                }
            }
            return Math.Pow(sumOfDiffs, (1.0 / P));
        }
        public Tuple<EvoDataPoint,List<Tuple<EvoDataPoint, double>>> GetNeighbors(HelixEvoSensor sensor, List<int> colOverride = null)
        {
            List<Tuple<EvoDataPoint, double>> distanceList = new List<Tuple<EvoDataPoint, double>>();
            EvoDataPoint? newData = ConvertNewData(sensor);
            if (newData == null)
            { return new Tuple<EvoDataPoint, List<Tuple<EvoDataPoint, double>>>(new EvoDataPoint(), distanceList); }
            if (Scaled) { newData = ScaleNewData((EvoDataPoint)newData); }
            
            foreach(EvoDataPoint data in DataPoints) //Get minkowski for all data points
            {
                if (colOverride == null) { distanceList.Add(new Tuple<EvoDataPoint, double>(data, GetMinkowski((EvoDataPoint)newData, data))); }
                else { distanceList.Add(new Tuple<EvoDataPoint, double>(data, GetMinkowski((EvoDataPoint)newData, data, colOverride))); }
            }
            distanceList = SortDistanceList(distanceList);
            if(distanceList[0].Item1.Id == sensor.SerialNumber) { distanceList.RemoveAt(0); }
            return new Tuple<EvoDataPoint, List<Tuple<EvoDataPoint, double>>>((EvoDataPoint)newData,distanceList);
        }
        public void ScaleData()
        {
            if(!Scaled)
            {
                for(int i = 0; i < DataPoints[0].Data.Count; i++) //For col in data
                {
                    double max = double.MinValue;
                    double min = double.MaxValue;
                    int maxE;
                    int minE;
                    foreach(EvoDataPoint evoData in DataPoints)
                    {
                        double value = evoData.Data[i];
                        if(value > max) { max = value; }
                        if(value < min) { min = value; }
                    }
                    maxE = GetScientificExponent(max);
                    minE = GetScientificExponent(min);
                    foreach(EvoDataPoint evoData in DataPoints)
                    {
                        if(minE < -2) { evoData.Data[i] = ScaleDataUp(evoData.Data[i], minE, maxE); }
                        else if(max > 1.0 | min < -1.0) { evoData.Data[i] = ScaleDataDown(evoData.Data[i], min, max); }
                    }
                }
            }
            Scaled = true;
        }
        public EvoDataPoint ScaleNewData(EvoDataPoint data)
        {
            if(Scaled)
            {
                for (int i = 0; i < OriginalDataPoints[0].Data.Count; i++) //For col in data
                {
                    double max = double.MinValue;
                    double min = double.MaxValue;
                    int maxE;
                    int minE;
                    foreach (EvoDataPoint evoData in OriginalDataPoints)
                    {
                        double value = evoData.Data[i];
                        if (value > max) { max = value; }
                        if (value < min) { min = value; }
                    }
                    maxE = GetScientificExponent(max);
                    minE = GetScientificExponent(min);
                    if (minE < -2) { data.Data[i] = ScaleDataUp(data.Data[i], minE, maxE); }
                    else if (max > 1.0 | min < -1.0) { data.Data[i] = ScaleDataDown(data.Data[i], min, max); }
                }
            }
            return data;
        }
        public void SortDataPoints()
        {
            bool sorted = false;
            while(!sorted)
            {
                sorted = true;
                for (int i = 0; i < DataPoints.Count; i++)
                {
                    if(i < DataPoints.Count - 1 && DataPoints[i].Label > DataPoints[i+1].Label)
                    {
                        EvoDataPoint previous = DataPoints[i];
                        DataPoints[i] = DataPoints[i + 1];
                        DataPoints[i + 1] = previous;
                        sorted = false;
                    }
                }
            }
            Sorted = true;
        }
        public List<Tuple<EvoDataPoint, double>> SortDistanceList(List<Tuple<EvoDataPoint, double>> distanceList)
        {
            bool sorted = false;
            while(!sorted)
            {
                sorted = true;
                for (int i = 0; i < distanceList.Count; i++)
                {
                    if(i < distanceList.Count-1 && distanceList[i].Item2 > distanceList[i+1].Item2)
                    {
                        Tuple<EvoDataPoint, double> holder = distanceList[i];
                        distanceList[i] = distanceList[i + 1];
                        distanceList[i + 1] = holder;
                        sorted = false;
                    }
                }
            }
            return distanceList;
        }
        private int GetScientificExponent(double num)
        {
            string scientific = num.ToString("E");
            int exponent = int.Parse(scientific.Split("E")[1]);
            return exponent;
        }
        private double ScaleDataDown(double num, double min, double max)
        {
            return (num - min) / (max - min);
        }
        private double ScaleDataUp(double num, int minE, int maxE)
        {
            int newE = ((maxE + minE) / 2) * -1;
            return double.Parse($"1.0E+{newE}") * num;
        }
    }
    struct EvoDataPoint
    {
        public string Id;
        public double Label;
        public List<double> Data;

        public EvoDataPoint(string id, double label, List<double> data)
        {
            try
            {
                Id = id;
                Label = label;
                Data = data;
            }
            catch
            {
                Id = "";
                Label = 0;
                Data = new List<double>();
            }
        }
        public override string ToString()
        {
            string line = "";
            line += $"{Id}\t{Label}\t";
            for(int i = 0; i < Data.Count; i++)
            {
                if (i == Data.Count - 1) { line += $"{Data[i]}"; }
                else { line += $"{Data[i]}\t"; }
            }
            return line;
        }
    }
}
