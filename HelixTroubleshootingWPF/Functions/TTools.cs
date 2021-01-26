using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Text.RegularExpressions;
using System.Xml;
using ImageMagick;
using System.Windows.Controls;
using HelixTroubleshootingWPF;
using System.Windows.Threading;
using PrimS;
using System.Threading.Tasks;
//namespace Helix_Troubleshooting_CS
namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        //Array of strings to be added to function list
        public static readonly string[] functionList = new string[] {"ALS Point Removal","Fix Algorithm Errors","Illuminated Sphere Summary","Solo Laser Line Analysis","Staring Dot Removal","Temperature Adjust"};

        //Private variables for config file loading
        private static string configName = "HTconfig.xml";
        private static string tcompBackupDir = "";
        private static string tcompDir = "";
        private static string rectDataDir = "";
        private static string resultsDir = "";
        private static int lineThresholdPercent = 20;
        private static int alsSensitivity = 3;


        public static void LineAnalysis()
        {
            Debug.WriteLine("Laser line analysis\n");
        }

        public static void StaringDotRemoval(string imagesFolder)
        {
            if (!Directory.Exists(imagesFolder)) { return; }
            Debug.WriteLine("Staring dot removal----------------------------------------\n");
            HelixSensor sensor = new HelixSensor(imagesFolder);
            Debug.WriteLine($"{sensor.Date}\n\tSensor information for {sensor.SerialNumber}:\n\tPN: {sensor.PartNumber}\n\tSensor Rev: {sensor.SensorRev}\n\tRect Rev: {sensor.RectRev}\n\t" +
                $"Rect Pos Rev: {sensor.RectPosRev}\n\tAcc Pos Rev: {sensor.AccPosRev}\n\t" +
                $"Laser Color: {sensor.Color}\n\tLaser Class: {sensor.LaserClass}\n");
        }


        //Private Helper Functions----------------------------------------------------------------------------------------

        //Find all files in directory that contain a certain string. Return empty list if none found.
        private static List<string> GetFilesWith(string directory, string find)
        {
            find = find.Insert(0, "*");
            find += "*";
            List<string> files = new List<string>();
            try
            {
                files.AddRange(Directory.GetFiles(directory, find));
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.ArgumentException)
            {
                return files;
            }
            return files;
        }

        //Get folder that corresponds to a SN group (e.g. "SN137XXX"). Return empty string if none found.
        private static string GetGroupFolder(string directory, string sn)
        {
            string folder = "";
            if(sn.Length == 8)
            {
                sn = sn.Remove(5);
                sn += "XXX";
                Debug.WriteLine($@"{directory}\{sn}");
                if (Directory.Exists($@"{directory}\{sn}"))
                {
                    folder = $@"{directory}\{sn}";
                }
            }
            return folder;
        }


    }
}
