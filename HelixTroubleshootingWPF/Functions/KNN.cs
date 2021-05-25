using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using HelixTroubleshootingWPF.Objects;
using Newtonsoft.Json;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        private static Tuple<List<int>, double, double> bestCombo = new Tuple<List<int>, double, double>(new List<int>(), double.MaxValue, 0.0);
        private static List<HelixEvoSensor> sensorList = new List<HelixEvoSensor>();
        private static List<HelixEvoSensor> testSensors = new List<HelixEvoSensor>();
        private static EvoDataframe Dataframe;
        private static long combosTried = 0;
        public static void RunKnn(string sn = "", string model = "")
        {
#nullable enable
            HelixEvoSensor? sensor = null;
#nullable disable
            if(sn != "")
            {
                sensor = AllEvoDataSingle(new HelixEvoSensor() { SerialNumber = sn});
            }
            Config.LoadConfig();
            List<HelixEvoSensor> evoData = LoadEvoData();
            if (sensor != null) { sensorList = FilterEvoData(evoData, sensor.PartNumber.Substring(0, 8)); }
            else { sensorList = FilterEvoData(evoData, model); }
            LoadDataframe(sensorList);
            Dataframe.ExportData(dataGatherFolder);
            if(sensor != null)
            {
                Tuple<EvoDataPoint, List<Tuple<EvoDataPoint, double>>> neighbors = Dataframe.GetNeighbors(sensor, Config.KnnDataColumns);
                if(neighbors.Item1.Data != null)
                {
                    Debug.WriteLine($"{Dataframe.NumNeighbors} Neighbors for {neighbors.Item1.Id}\n{neighbors.Item1}");
                    double labelRegression = 0.0;
                    for (int i = 0; i < Dataframe.NumNeighbors; i++)
                    {
                        Debug.WriteLine($"{i}: Distance = {neighbors.Item2[i].Item2}; {neighbors.Item2[i].Item1}");
                        labelRegression += neighbors.Item2[i].Item1.Label;
                    }
                    labelRegression /= Dataframe.NumNeighbors;
                    Debug.WriteLine($"\nLabel regression: {labelRegression}");
                }
            }
        }
        public static void GetTestSensors()
        {
            //Get sensors to be used as validation input for KNN
            List<HelixEvoSensor> sensors = LoadEvoData(false, false);
            for(int i = sensors.Count - 1; i >= 0; i--)
            {
                if(int.TryParse(sensors[i].SerialNumber, out int snInt))
                {
                    if(snInt < 138600) { sensors.RemoveAt(i); }
                }
            }
            testSensors = FilterEvoData(sensors, "920-0201");
        }
        public static void RunCombos()
        {
            GetTestSensors();
            sensorList = FilterEvoData(LoadEvoData(), "920-0201");
            for (int i = sensorList.Count - 1; i >= 0; i--)
            {
                if (int.TryParse(sensorList[i].SerialNumber, out int snInt))
                {
                    if (snInt > 138600) { sensorList.RemoveAt(i); }
                }
            }
            Random rand = new Random();
            LoadDataframe(sensorList, true);
            //GetCombo(2, 10);
            //for (int i = 10; i < 16; i++)
            //{
            //    //GetCombo(2, i);
            //    TestBestCombo(Config.KnnDataColumns, i);
            //}
            TestBestCombo(Config.KnnDataColumns);
            Console.WriteLine("Done");
        }
        public static void TestBestCombo(List<int> combo, int k = 0)
        {
            double avgDiff = 0.0;
            double maxDiff = 0.0;
            int sensors = 0;
            if (k > 0) { Dataframe.NumNeighbors = k; }
            Dataframe.ScaleData();
            foreach (HelixEvoSensor sensor in testSensors)
            {
                Tuple<EvoDataPoint, List<Tuple<EvoDataPoint, double>>> neighbors = Dataframe.GetNeighbors(sensor, combo);
                if(neighbors.Item1.Label != 0.0)
                {
                    double labelRegression = 0.0;
                    for (int i = 0; i < Dataframe.NumNeighbors; i++)
                    {
                        labelRegression += neighbors.Item2[i].Item1.Label;
                    }
                    labelRegression = labelRegression / Dataframe.NumNeighbors;
                    double diff = Math.Abs(labelRegression - neighbors.Item1.Label);
                    avgDiff += diff;
                    if(diff > maxDiff) { maxDiff = diff; }
                    sensors += 1;
                }
            }
            avgDiff /= sensors;
            combosTried++;
            if(avgDiff < bestCombo.Item2)
            {
                bestCombo = new Tuple<List<int>, double, double>(combo, avgDiff, maxDiff);
                Debug.WriteLine($"Best combo:\n\tColumns: {string.Join(",",bestCombo.Item1)}" +
                    $"\n\tMax Label Regression Difference: {bestCombo.Item3}" +
                    $"\n\tAvg Label Regression Difference: {bestCombo.Item2}" +
                    $"\n\tCombo # {combosTried}\n\tK = {k}");
            }
        }
        private static void GetCombo(int start, int comboLen, List<int> currentCombo = null)
        {
            if(currentCombo == null) { currentCombo = new List<int>(); }
            foreach(int col in Config.AllKnnCols)
            {
                int index = Config.AllKnnCols.IndexOf(col);
                if (index >= start)
                {
                    //List<int> newCombo = currentCombo;
                    List<int> newCombo = new List<int>();
                    newCombo.AddRange(currentCombo);
                    newCombo.Add(col);
                    if(newCombo.Count == comboLen) { TestBestCombo(newCombo); }
                    else { GetCombo(index+1, comboLen, newCombo); }
                }
            }
        }
        private static List<HelixEvoSensor> FilterEvoData(List<HelixEvoSensor> evoData, string model)
        {
            List<HelixEvoSensor> filteredData = new List<HelixEvoSensor>();
            foreach(HelixEvoSensor sensor in evoData)
            {
                if (sensor.CheckComplete() & sensor.PartNumber.Contains(model)) { filteredData.Add(sensor); }
            }
            return filteredData;
        }
        private static void LoadDataframe(List<HelixEvoSensor> filteredData, bool scaleData = true)
        {
            Dataframe = new EvoDataframe(filteredData, ComboHeaderList, Config.AllKnnCols);
            Dataframe.SortDataPoints();
            if (scaleData) { Dataframe.ScaleData(); }
        }
        private static List<HelixEvoSensor> LoadEvoData(bool refresh = false, bool saveFile = true)
        {
            List<HelixEvoSensor> evoData;
            string evoJsonPath = Path.Join(dataGatherFolder, "EvoData.json");
            if (!File.Exists(evoJsonPath) | refresh)
            {
                evoData = GetEvoData();
                if (saveFile) { File.WriteAllLines(evoJsonPath, new string[] { JsonConvert.SerializeObject(evoData, Formatting.Indented) }); }
            }
            else
            {
                evoData = (List<HelixEvoSensor>)((Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject(File.ReadAllText(evoJsonPath))).ToObject(typeof(List<HelixEvoSensor>));
            }
            return evoData;
        }
    }
}
