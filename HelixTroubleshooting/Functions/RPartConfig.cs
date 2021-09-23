using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelixTroubleshootingWPF.Functions;
using System.Text.RegularExpressions;
using System.Windows;

namespace HelixTroubleshootingWPF.Functions
{
    static class RPartConfig
    {
        public static void CreateRPartConfigs(string cFolder)
        {
            try
            {
                //For each folder with evo/solo PN (regex: ^\d{3}-\d{4}-\w{4}$)
                string pnPattern = @"^\d{3}-\d{4}-\w{4}$";
                foreach (string origFolder in Directory.GetDirectories(cFolder))
                {
                    string pn = Path.GetFileNameWithoutExtension(origFolder);
                    if (Regex.IsMatch(pn, pnPattern))
                    {
                        //append "R" for part number
                        string rPn = $"{pn}R";
                        bool found = false;
                        //check if configs for that PN exist
                        foreach (string folder2 in Directory.GetDirectories(cFolder))
                        {
                            string folderName2 = Path.GetFileNameWithoutExtension(folder2);
                            //if exists: break
                            if (folderName2 == rPn)
                            {
                                found = true;
                                break;
                            }
                        }
                        //else:
                        if (!found)
                        {
                            //copy original PN folder, with R-PN
                            string newDir = Path.Join(cFolder, rPn);
                            Directory.CreateDirectory(newDir);
                            foreach (string file in Directory.GetFiles(origFolder))
                            {
                                //rename files to include R-PN
                                string rPnFile = file.Replace(pn, rPn);
                                File.Copy(file, rPnFile);
                                if (rPnFile.ToLower().Contains(".xml"))
                                {
                                    //edit config xml to update sensor PN
                                    string fileText = File.ReadAllText(rPnFile);
                                    fileText = fileText.Replace(pn, rPn);
                                    File.WriteAllText(rPnFile, fileText);
                                }
                            }
                        }
                    }
                }
                MessageBox.Show("R-Configs created.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Generating R-Configs", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
