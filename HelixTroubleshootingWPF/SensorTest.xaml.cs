using HelixTroubleshootingWPF.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using uEye;
using PrimS;
using System.Diagnostics;

namespace HelixTroubleshootingWPF
{
    /// <summary>
    /// Interaction logic for SensorTest.xaml
    /// </summary>
    public partial class SensorTest : Window
    {
        private string SensorIp;
        private PrimS.Telnet.Client Client;
        string FPGAInit = "";
        string LaserPowerTable = "";
        string OPAInit = "";
        string SensorXml = "";
        HelixSensor Sensor = new();
        HelixEvoSensor EvoSensor = new();
        HelixSoloSensor SoloSensor = new();
        public SensorTest(string sensorIp)
        {
            SensorIp = sensorIp;
            InitializeComponent();
            PopulateTestList();
        }

        private void PopulateTestList()
        {
            TreeViewItem onboardFilesItem = new TreeViewItem { Header = "On-board Files", IsExpanded = true };
            string[] files = new string[] { "FPGAInit.dat", "LaserPowerTable.dat", "OPAInit.dat", "sensor.xml" };
            foreach (string file in files)
            {
                onboardFilesItem.Items.Add(new TreeViewItem { Header = file });
            }
            SensorTestTree.Items.Add(onboardFilesItem);

            TreeViewItem telnetItem = new() { Header = "Telnet", IsExpanded = true };
            telnetItem.Items.Add(new TreeViewItem { Header = "Connection" });
            telnetItem.Items.Add(new TreeViewItem { Header = "Temptest" });
            telnetItem.Items.Add(new TreeViewItem { Header = "Acceltest" });
            SensorTestTree.Items.Add(telnetItem);

            TreeViewItem cameraItem = new TreeViewItem { Header = "Camera", IsExpanded = true };
            cameraItem.Items.Add(new TreeViewItem { Header = "Gig-E" });
            SensorTestTree.Items.Add(cameraItem);
        }
        private bool ConnectTelnet()
        {
            try
            {
                Log($"Opening Telnet connection to { SensorIp} on port 23.");
                Client = new PrimS.Telnet.Client(SensorIp, 23, new System.Threading.CancellationToken());
                return true;
            }
            catch(Exception e)
            {
                Highlight("Connection", false);
                Log(e.Message);
                Client.Dispose();
                return false;
            }
        }
        private string GetResponse(string command, int wait = 1000)
        {
            string res = Task.Run(async () => {
                string response = await Client.ReadAsync();
                await Client.WriteLine(command);
                Thread.Sleep(wait);
                response = await Client.ReadAsync();
                return response;
            }).Result;
            return res;
        }
        private void CheckFiles()
        {
            Log($"Checking onboard files.");
            try
            {
                string response = "";
                response = GetResponse("cd storage/");
                response = GetResponse("ls");

                string[] split = response.Split("\r\n");
                Highlight("sensor.xml", response.Contains("sensor.xml"));
                Highlight("OPAInit.dat", response.Contains("OPAInit.dat"));
                Highlight("FPGAInit.dat", response.Contains("FPGAInit.dat"));
                Highlight("LaserPowerTable.dat", response.Contains("LaserPowerTable.dat"));

                List<string> res = new();
                res.AddRange(GetResponse("cat sensor.xml").Split("\r\n"));
                res.RemoveAt(0);
                res.RemoveAt(res.Count - 1);
                SensorXml = string.Join("", res);
                SensorXml = SensorXml.Replace(Convert.ToChar(0x0).ToString(), "");

                res.Clear();
                res.AddRange(GetResponse("cat LaserPowerTable.dat").Split("\r\n"));
                res.RemoveAt(0);
                res.RemoveAt(res.Count-1);
                LaserPowerTable = string.Join("", res);

                res.Clear();
                res.AddRange(GetResponse("cat OPAInit.dat").Split("\r\n"));
                res.RemoveAt(0);
                res.RemoveAt(res.Count - 1);
                OPAInit = string.Join("", res);

                res.Clear();
                res.AddRange(GetResponse("cat FPGAInit.dat").Split("\r\n"));
                res.RemoveAt(0);
                res.RemoveAt(res.Count - 1);
                FPGAInit = string.Join("", res);

                Sensor.GetSensorDataFromString(SensorXml);
                if (!Sensor.PartNumber.Contains("920"))
                {
                    HighlightGray("FPGAInit.dat");
                    HighlightGray("OPAInit.dat");
                    HighlightGray("LaserPowerTable.dat");
                }
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
            finally
            {
                Client.Dispose();
            }
        }
        private async Task TempAccel()
        {
            Log("Checking Temptest and Acceltest.");
            try
            {
                string response = await Client.ReadAsync();
                //Get temptest response
                await Client.WriteLine("temptest");
                System.Threading.Thread.Sleep(1000);
                response = await Client.ReadAsync();

                string[] split = response.Split("\r\n");
                string temp0 = "";
                string temp1 = "";
                foreach (string res in split)
                {
                    if (res.Contains("Sensor0")) { temp0 = res.Split(" ")[1]; }
                    else if (res.Contains("Sensor1")) { temp1 = res.Split(" ")[1]; }
                }
                Log($"Temptest:\n\tSensor0: {temp0}C\n\tSensor1: {temp1}C");
                if (temp0 != "" & temp1 != "")
                {
                    Highlight("Connection", true);
                    Highlight("Temptest", true);
                }
                else
                {
                    Highlight("Connection", true);
                    Log("Temptest failed");
                    Highlight("Temptest", false);
                }
                //Get acceltest response
                await Client.WriteLine("acceltest");
                System.Threading.Thread.Sleep(1000);
                response = await Client.ReadAsync();

                split = response.Split("\r\n");
                string accel = "";
                foreach (string res in split)
                {
                    if (res.Contains("x/y/z")) { accel = res; }
                }
                if (accel != "")
                {
                    Log($"Acceltest: {accel}");
                    Highlight("Acceltest", true);
                }
                else { Log("Acceltest failed"); Highlight("Acceltest", false); }
            }
            catch(Exception e)
            {
                Highlight("Connection", false);
                Log(e.Message);
            }
        }
        public bool TestCamera(bool retry = true)
        {
            Log("Testing camera connection.");
            Camera cam = new();
            try
            {
                cam.Init(0);
                //cam.IO.Gpio.SetConfiguration(uEye.Defines.IO.GPIO.One, uEye.Defines.IO.GPIOConfiguration.Output, uEye.Defines.IO.State.Low);
                System.Threading.Thread.Sleep(50);
                //cam.IO.Gpio.SetConfiguration(uEye.Defines.IO.GPIO.One, uEye.Defines.IO.GPIOConfiguration.Input);

                uEye.Types.DeviceInformation info;
                cam.Information.GetDeviceInfo(out info);
                int linkspeed = info.DeviceInfoHeartbeat.LinkSpeed_Mb;
                if (linkspeed == 1000) { Highlight("Gig-E", true); }
                else { Highlight("Gig-E", false); }
                Log($"Camera link speed: {linkspeed}");
                cam.Exit();
                
                return true;
            }
            catch(Exception e)
            {
                cam.Exit();
                Log(e.Message);
                bool result = false;
                if (retry) { Log("Retrying"); result = TestCamera(false); }
                Highlight("Gig-E", result);
                return result;
            }
        }

        private void ClearHighlights()
        {
            foreach (TreeViewItem item in SensorTestTree.Items)
            {
                item.Background = SystemColors.WindowBrush;
                foreach (TreeViewItem subItem in item.Items)
                {
                    subItem.Background = SystemColors.WindowBrush;
                }
            }
        }
        private void FillSensorInfo()
        {
            InfoDate.Text = Sensor.Date;
            InfoSerialNumber.Text = Sensor.SerialNumber;
            InfoPartNumber.Text = Sensor.PartNumber;
            InfoRev.Text = Sensor.SensorRev;
            InfoImagerID.Text = Sensor.CameraSerial;
            InfoLaserClass.Text = Sensor.LaserClass;
            InfoLaserColor.Text = Sensor.Color;
            InfoRectRev.Text = Sensor.RectRev;
            InfoRectPosRev.Text = Sensor.RectPosRev;
            InfoAccPosRev.Text = Sensor.AccPosRev;
        }
        private void FillFixtureInfo(string sn = "")
        {
            if (sn != "") { Sensor = new HelixSensor { SerialNumber = sn, PartNumber = TToolsFunctions.GetSensorPn(sn)}; }
            //TODO: Handler method for finding whether a sn belongs to an evo or solo sensor. Can search HelixRectResults.log
            if (Sensor.PartNumber.Contains("920-"))
            {
                SoloFixtureDataPanel.Visibility = Visibility.Collapsed;
                EvoFixtureDataPanel.Visibility = Visibility.Visible;
                if (sn == "") { EvoSensor = TToolsFunctions.AllEvoDataSingle(new HelixEvoSensor(SensorXml, true)); }
                else { EvoSensor = TToolsFunctions.AllEvoDataSingle(new HelixEvoSensor() { SerialNumber = sn }); }
                FixtureResultsSnEntry.Text = EvoSensor.SerialNumber;
                UFFDataGrid.ItemsSource = new List<EvoUffData> { EvoSensor.UffData };
                TuningDataGrid.ItemsSource = new List<EvoDacMemsData> { EvoSensor.DacMemsData };
                MirrorcleDataGrid.ItemsSource = new List<MirrorcleData> { EvoSensor.Mirrorcle };
                LPFDataGrid.ItemsSource = new List<EvoLpfData> { EvoSensor.LpfData };
                SamplingDataGrid.ItemsSource =  EvoSensor.LpfData.TableSamples;
                PitchDataGrid.ItemsSource = new List<EvoPitchData> { EvoSensor.PitchData };
                AccuracyDataGrid.ItemsSource = new List<AccuracyResult> { EvoSensor.AccuracyResult };
                VDEDataGrid.ItemsSource = new List<VDEResult> { EvoSensor.VDE };
            }
        }
        private void Highlight(string header, bool passed)
        {
            foreach(TreeViewItem item in SensorTestTree.Items)
            {
                if (header == (string)item.Header && passed) { item.Background = Brushes.LightGreen; return; }
                else if (header == (string)item.Header && !passed) { item.Background = Brushes.IndianRed; return; }
                foreach (TreeViewItem subItem in item.Items)
                {
                    if (header == (string)subItem.Header && passed) { subItem.Background = Brushes.LightGreen; return; }
                    else if (header == (string)subItem.Header && !passed) { subItem.Background = Brushes.IndianRed; return; }
                }
            }
        }
        private void HighlightGray(string header)
        {
            foreach (TreeViewItem item in SensorTestTree.Items)
            {
                if (header == (string)item.Header) { item.Background = Brushes.LightGray; return; }
                foreach (TreeViewItem subItem in item.Items)
                {
                    if (header == (string)subItem.Header) { subItem.Background = Brushes.LightGray; return; }
                }
            }
        }
        private void Log(string message, bool clear = false)
        {
            if (clear) { SensorTestLogBox.Text = ""; }
            SensorTestLogBox.Text += $"\n{DateTime.Now:HH:mm:ss:ff}: {message}";
            SensorTestLogBox.ScrollToEnd();
        }
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            ClearHighlights();
            Log("Starting Tests...", true);
            ConnectTelnet();
            await TempAccel();
            CheckFiles();
            Client.Dispose();
            TestCamera();
            if (SensorXml != "")
            {
                try
                {
                    Sensor = new HelixSensor(SensorXml, true);
                    FillSensorInfo();
                    FillFixtureInfo();
                }
                catch(System.Xml.XmlException exception)
                {
                    Log(exception.Message);
                }
            }
        }
        private void SensorTestTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem selectedItem = (TreeViewItem)SensorTestTree.SelectedItem;
            string header = (string)selectedItem.Header;
            if(header == "sensor.xml") { SensorFileBox.Text = SensorXml; }
            else if (header == "LaserPowerTable.dat") { SensorFileBox.Text = LaserPowerTable; }
            else if (header == "OPAInit.dat") { SensorFileBox.Text = OPAInit; }
            else if (header == "FPGAInit.dat") { SensorFileBox.Text = FPGAInit; }
        }

        private void FixtureResultsGetDataBtn_Click(object sender, RoutedEventArgs e)
        {
            FillFixtureInfo(FixtureResultsSnEntry.Text);
        }
    }
}
