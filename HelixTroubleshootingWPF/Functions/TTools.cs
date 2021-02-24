using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
//namespace Helix_Troubleshooting_CS
namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        //Working directory folder for saving the results of data gathering functions
        private static string dataGatherFolder = @$"{Directory.GetCurrentDirectory()}\DataGather\";
        //Array of strings to be added to function list
        public static readonly string[] functionList = new string[] {"ALS Point Removal","Fix Algorithm Errors","Illuminated Sphere Summary",
            "Solo Laser Line Analysis","Staring Dot Removal","Temperature Adjust", "DACMEMS Data Gather", "UFF Data Gather", "LPF Data Gather",
            "Pitch Data Gather","Evo Data Gather"};

        //Private variables for config file loading
        private static string configName = "HTconfig.xml";
        private static string tcompBackupDir = "";
        private static string tcompDir = "";
        private static string rectDataDir = "";
        private static string resultsDir = "";
        private static int lineThresholdPercent = 20;
        private static int alsSensitivity = 3;


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
