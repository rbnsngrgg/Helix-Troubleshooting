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
        public SensorTest(string sensorIp)
        {
            SensorIp = sensorIp;
            InitializeComponent();
            PopulateTestList();
        }

        private void PopulateTestList()
        {
            TreeViewItem onboardFilesItem = new TreeViewItem { Header = "On-board Files" };
            string[] files = new string[] { "FPGAInit.dat", "LaserPowerTable.dat", "OPAInit.dat", "Sensor.xml" };
            foreach (string file in files)
            {
                onboardFilesItem.Items.Add(new TreeViewItem { Header = file });
            }
            SensorTestTree.Items.Add(onboardFilesItem);

            TreeViewItem telnetItem = new TreeViewItem { Header = "Telnet" };
            telnetItem.Items.Add(new TreeViewItem { Header = "Connection" });
            telnetItem.Items.Add(new TreeViewItem { Header = "Temptest" });
            telnetItem.Items.Add(new TreeViewItem { Header = "Acceltest" });
            SensorTestTree.Items.Add(telnetItem);

            TreeViewItem cameraItem = new TreeViewItem { Header = "Camera" };
            cameraItem.Items.Add(new TreeViewItem { Header = "Gig-E" });
            SensorTestTree.Items.Add(cameraItem);
        }
        private async void SensorTelnet()
        {
            PrimS.Telnet.Client client = new PrimS.Telnet.Client(SensorIp, 23, new System.Threading.CancellationToken());
            string response = await client.ReadAsync();

            await client.WriteLine("temptest");
            System.Threading.Thread.Sleep(1000);
            response = await client.ReadAsync();
            Debug.WriteLine(response);
        }
        public void TestCamera()
        {
            Camera cam = new Camera(0);
            cam.IO.Gpio.SetConfiguration(uEye.Defines.IO.GPIO.One, uEye.Defines.IO.GPIOConfiguration.Output, uEye.Defines.IO.State.Low);
            System.Threading.Thread.Sleep(50);
            cam.IO.Gpio.SetConfiguration(uEye.Defines.IO.GPIO.One, uEye.Defines.IO.GPIOConfiguration.Input);
            cam.Exit();

        }
    }
}
