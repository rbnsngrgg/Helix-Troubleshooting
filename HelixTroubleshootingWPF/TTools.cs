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
namespace TTools
{
    class TToolsFunctions
    {
        //Array of strings to be added to function list
        public readonly string[] functionList = new string[] {"ALS Point Removal","Fix Algorithm Errors","Illuminated Sphere Summary","Solo Laser Line Analysis","Staring Dot Removal","Temperature Adjust"};

        //Private variables for config file loading
        private string configName = "HTconfig.xml";
        private string tcompBackupDir = "";
        private string tcompDir = "";
        private string rectDataDir = "";
        private string resultsDir = "";
        private int lineThresholdPercent = 20;
        private int alsSensitivity = 3;
        //Create new config with default values, return true if success
        public bool CreateConfig(string configPath)
        {
            try
            {
                //Generate file and write first lines
                string[] lines = { "<Helix_Troubleshooting_Config>",
                        "\t<Directories tcompBackupDir = \"testestest\" tcompDir = \"\" rectDataDir = \"\" resultsDir = \"\"/>", 
                        "\t<LineAnalysis lineThresholdPercent = \"\"/>",
                        "\t<ALSPointRemover alsSensitivity = \"\"/>",
                    "</Helix_Troubleshooting_Config>" };
                File.WriteAllLines(configPath, lines);
                XmlDocument config = new XmlDocument();
                config.Load(configPath);
                //Set tcompBackupDir, tcompDir, rectDataDir, and resultsDir to default values
                config.FirstChild.ChildNodes[0].Attributes[0].Value = @"\\castor\Production\Manufacturing\Evo\Tcomp Templates\Originals";
                config.FirstChild.ChildNodes[0].Attributes[1].Value = @"\\castor\Production\Manufacturing\MfgSoftware\ThermalTest\200-0526\Results";
                config.FirstChild.ChildNodes[0].Attributes[2].Value = @"\\castor\Ftproot\RectData";
                config.FirstChild.ChildNodes[0].Attributes[3].Value = @"\\castor\Production\Manufacturing\MFGENG SW Tools\Helix Troubleshooting\Results";
                config.FirstChild.ChildNodes[1].Attributes[0].Value = "20";
                config.FirstChild.ChildNodes[2].Attributes[0].Value = "3";
                config.Save(configName);
                return true;
            }
            catch(System.Exception ex)
            {
                MessageBox.Show($"Error creating config file: {ex.Message}","Error",MessageBoxButton.OK,MessageBoxImage.Error);
                return false;
            }
        }
        
        //Config Loader
        public void LoadConfig()
        {
            string configPath = $"{Directory.GetCurrentDirectory()}\\{configName}";
            //Create if it doesn't exist
            if (!File.Exists(configPath))
            {
                if (!CreateConfig(configPath))
                {
                    return;
                }
            }
            //Load xml, assign specified values to the class properties, for use in the public functions.
            XmlDocument config = new XmlDocument();
            config.Load(configPath);
            int node = 0;
            //Check if auto-formatting from xml-notepad occurred, in which case the node with config info will be 1 (instead of 0).
            if (config.ChildNodes[0].ChildNodes.Count < 3) { node = 1; }
            
            tcompBackupDir = config.ChildNodes[node].ChildNodes[0].Attributes[0].Value;
            tcompDir = config.ChildNodes[node].ChildNodes[0].Attributes[1].Value;
            rectDataDir = config.ChildNodes[node].ChildNodes[0].Attributes[2].Value;
            resultsDir = config.ChildNodes[node].ChildNodes[0].Attributes[3].Value;
            if (!Int32.TryParse(config.ChildNodes[node].ChildNodes[1].Attributes[0].Value, out lineThresholdPercent))
                { MessageBox.Show("Cannot parse lineThresholdPercent value to int in config.", "Config Parse Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            if (!Int32.TryParse(config.ChildNodes[node].ChildNodes[2].Attributes[0].Value, out alsSensitivity))
                {MessageBox.Show("Cannot parse alsSensitivity value to int in config.", "Config Parse Error",MessageBoxButton.OK,MessageBoxImage.Warning);}
        }

        //Functions that are in the listview
        public void ALSPointRemoval(string directory)
        {
            //Find all CG text files in given directory
            List<string> files = GetFilesWith(directory, "*CGs.txt");
            if (files.Count() == 0)
            {
                MessageBox.Show("No CG text files were found.", "None Found", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            //Find # of occurrences for each whole number in R column for each file, then mark lines for removal if R col # of occurrences is > 3
            foreach (string file in files)
            {
                List<string> lines = new List<string>(File.ReadAllLines(file));
                List<string> newLines = new List<string>();
                Dictionary<string, int> occurrences = new Dictionary<string, int>();
                foreach(string line in lines)
                {
                    if(line.Contains('R'))
                        {continue;}

                    string entry = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries)[5];
                    entry = entry.Split(".")[0];

                    //check if number is already in the dict and increment count by 1 if it is, add it if not
                    if(occurrences.ContainsKey(entry))
                        { occurrences[entry] += 1; }
                    else
                        { occurrences.Add(entry, 1); }
                }
                //Iterate again now that we have the count of each number. Remove those that are > 3 occurrences
                foreach(string line in lines)
                {
                    if (line.Contains('R'))
                    {
                        newLines.Add(line);
                        continue;
                    }
                    string entry = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries)[5];
                    entry = entry.Split(".")[0];

                    if(occurrences.ContainsKey(entry))
                    {
                        if (occurrences[entry] <= 3)
                            {newLines.Add(line);}
                    }
                }
                //Re-write file with the new lines
                File.WriteAllLines(file,newLines);
            }
            MessageBox.Show($"Erroneous ALS points removed from {files.Count} file(s).", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void AlgorithmErrors(string sn)
        {
            if(sn == ""){ return; }

            //Get t-comp results folder that matches the entered SN (e.g. SN138XXX for SN138265)
            string snGroupFolder = GetGroupFolder(tcompDir, sn);
            if(snGroupFolder == "") {MessageBox.Show($"Could not find the t-comp file for {sn}.","File Not Found",MessageBoxButton.OK,MessageBoxImage.Warning); return; }

            //Add wildcards for search
            string searchSN = sn;
            searchSN.Insert(0, "*");
            searchSN += "*";
            //Get the t-comp file
            List<string> files = GetFilesWith(snGroupFolder, sn);
            if(files.Count == 0)
            {
                MessageBox.Show($"No t-comp file found matching {sn} in folder: {snGroupFolder}.","None found",MessageBoxButton.OK,MessageBoxImage.Warning);
                return;
            }
            string file = files[0];
            List<string> lines = new List<string>(File.ReadAllLines(file));

            //Find indexes of lines with algorithm errors
            List<int> linesWithAlgoError = new List<int>();
            foreach (string line in lines)
            {
                if(line == lines[0])
                    {continue;}
                List<string> entries = new List<string>(line.Split('\t'));
                foreach(string entry in entries)
                {
                    if(entries.IndexOf(entry) == 0)
                    { continue; }
                    double number = default;
                    if (Double.TryParse(entry, out number))
                    {
                        if (number > 500)
                        {
                            linesWithAlgoError.Add(lines.IndexOf(line));
                            break;
                        }
                    }
                }
            }
            //Correct algo errors
            foreach(int lineNum in linesWithAlgoError)
            {
                string line = lines[lineNum];
                List<string> entries = new List<string>(line.Split('\t'));
                foreach(string entry in entries)
                {
                    if (entries.IndexOf(entry) == 0)
                    { continue; }
                    //Split current line and get index of the current entry
                    List<string> currentLine = new List<string>(lines[lineNum].Split('\t'));
                    int currentIndex = currentLine.IndexOf(entry);
                    double current = default;
                    Double.TryParse(entry, out current);

                    //Regex to replace only a single instance of the algorithm error entries
                    var regex = new Regex(Regex.Escape(entry));
                    //if first line, copy value from next line that isn't algo error
                    if (lineNum == 1)
                    {
                        //Get next line that doesn't have algo errors. Skip if there are no good entries (entire column is algorithm errors)
                        int nextLineNum = CheckNext(linesWithAlgoError,lineNum,lines.Count-1);
                        if(nextLineNum == lines.Count)
                        {
                            continue;
                        }
                        List<string> nextLine = new List<string>(lines[nextLineNum].Split('\t'));
                        lines[lineNum] = regex.Replace(lines[lineNum], nextLine[currentIndex], 1);
                    }
                    else
                    {
                        //Assign "previous" to the number in the same column on the previous line.
                        List<string> previousLine = new List<string>(lines[lineNum - 1].Split('\t'));
                        double previous = default;
                        Double.TryParse(previousLine[currentIndex], out previous);
                        //If not algo error
                        if(current < 500)
                        { continue; }

                        double next = default;
                        //Replace current value in line with the value from previous line if there are no valid values from checkNext()
                        int nextLineNum = CheckNext(linesWithAlgoError, lineNum, lines.Count - 1);
                        if (nextLineNum == lines.Count)
                        {
                            lines[lineNum] = regex.Replace(lines[lineNum], previous.ToString("#.######"), 1);
                            continue;
                        }
                        else
                        {
                            List<string> nextLine = new List<string>(lines[nextLineNum].Split('\t'));
                            Double.TryParse(nextLine[currentIndex], out next);
                            double newValue = Math.Round((previous + next) / 2,6);

                            lines[lineNum] = regex.Replace(lines[lineNum],newValue.ToString("#.######"),1);
                        }
                    }
                }
            }
            //Backup the original file, write and save the new lines with ".txt" file extension
            if(file.ToLower().Contains(".day2"))
            {
                string backupFile = $"{tcompBackupDir}\\{System.IO.Path.GetFileName(file)}";
                if(File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                }
                File.Move(file, backupFile);
                File.WriteAllLines($"{file.Replace(System.IO.Path.GetFileName(file),"")}{System.IO.Path.GetFileNameWithoutExtension(file)}.txt",lines);
            }
            else
            {
                File.WriteAllLines(file, lines);
            }
            MessageBox.Show($"Algorithm errors fixed for {sn}","Done",MessageBoxButton.OK,MessageBoxImage.Information);
        }

        public void SphereSummary(string directory)
        {
            //Get all illuminated sphere images
            List<string> images = GetFilesWith(directory, "RZ*.tif");
            if(images.Count == 0)
                { MessageBox.Show($"No illuminted sphere images found in {directory}.", "No Images Found", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            //Get z-values from file names
            List<string> zValues = new List<string>();
            foreach(string image in images)
            {
                string zValue = System.IO.Path.GetFileName(image).Split("Y")[0];
                if (!zValues.Contains(zValue))
                    {zValues.Add(zValue);}
            }

            //MagickReadSettings to ignore null tag 37373 in tif images generated during rectification.
            var settings = new MagickReadSettings();
            settings.SetDefine("tiff:ignore-tags", "37373");

            //Create summary folder
            string summaryFolder = $@"{System.IO.Path.GetDirectoryName(images[0])}\Summary";
            Directory.CreateDirectory(summaryFolder);

            //Init progress bar
            var pBar = new HelixTroubleshootingWPF.ProgressBar();
            pBar.Visibility = Visibility.Visible;
            pBar.Show();
            pBar.mainProgressBar.Visibility = Visibility.Visible;
            float progressTick = 100f / (float)zValues.Count;
            //Loop through zValues and combine all images that match
            int imgCount = 1;
            foreach (string zValue in zValues)
            {
                pBar.label.Dispatcher.Invoke(() => pBar.label.Text = $"Creating image ({imgCount}/{zValues.Count}): {zValue}.tif", DispatcherPriority.Background);
                pBar.label.Text = $"Creating image ({imgCount}/{zValues.Count}): {zValue}.tif";
                MagickImage summaryImage;
                //Pick first img to initialize summaryImg
                foreach(string image in images)
                {
                    if(image.Contains(zValue))
                    {
                        //Loop to add each image to summary via lighten operation
                        summaryImage = new MagickImage(image, settings);
                        foreach (string image2 in images)
                        {
                            if (image2.Contains(zValue))
                            {
                                summaryImage.Composite(new MagickImage(image2, settings),CompositeOperator.Lighten);
                            }
                        }
                        summaryImage.Write($@"{summaryFolder}\{zValue}.tif");
                        break;
                    }
                }
                //Tick progress
                pBar.mainProgressBar.Dispatcher.Invoke(() => pBar.mainProgressBar.Value += progressTick, DispatcherPriority.Background);
                imgCount++;
            }
            pBar.mainProgressBar.Value = 100;
            MessageBox.Show("Summary image processing complete","Done", MessageBoxButton.OK,MessageBoxImage.Information);
            pBar.Visibility = Visibility.Hidden;
            pBar.Close();
        }

        public void LineAnalysis()
        {
            Debug.WriteLine("Laser line analysis\n");
        }

        public void StaringDotRemoval(string imagesFolder)
        {
            if (!Directory.Exists(imagesFolder)) { return; }
            Debug.WriteLine("Staring dot removal----------------------------------------\n");
            HelixSensor sensor = new HelixSensor(imagesFolder);
            Debug.WriteLine($"{sensor.Date}\n\tSensor information for {sensor.SerialNumber}:\n\tPN: {sensor.PartNumber}\n\tSensor Rev: {sensor.SensorRev}\n\tRect Rev: {sensor.RectRev}\n\t" +
                $"Rect Pos Rev: {sensor.RectPosRev}\n\tAcc Pos Rev: {sensor.AccPosRev}\n\t" +
                $"Laser Color: {sensor.Color}\n\tLaser Class: {sensor.LaserClass}\n");
        }

        public void TempAdjust(string sn, string tempString)
        {
            float setTemp;
            List<string> tcompFiles = GetFilesWith(GetGroupFolder(tcompDir, sn), sn);
            if (tcompFiles.Count == 0)
            {
                MessageBox.Show($"No tcomp file found for \"{sn}\".", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (tempString == "" || !float.TryParse(tempString, out setTemp))
            {
                MessageBox.Show($"Invalid temperature entered.", "Invalid Temperature", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string file = tcompFiles[0];
            List<string> lines = new List<string>(File.ReadAllLines(file));
            if (lines.Count < 85)
            { MessageBox.Show($"The t-comp file at: \"{file}\" is incomplete.", "Incomplete T-Comp File", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            //Get current reference average. Temperatures are in column 1, excluding row 0.
            float tempOffset = 0.0f;
            float referenceAvg;
            if(!GetReferenceAvg(lines, out referenceAvg))
            { return; }
            tempOffset = setTemp - referenceAvg;

            List<string> newLines = new List<string>();
            //Change col 1 values so that reference average matches the desired temperature
            foreach(string line in lines)
            {
                if (lines.IndexOf(line) == 0)
                { newLines.Add(line); continue; }
                String[] separatedLine = line.Split("\t");
                float lineTemp = float.Parse(separatedLine[1]);
                float newTemp = lineTemp + tempOffset;
                var regex = new Regex(Regex.Escape(separatedLine[1]));
                newLines.Add(regex.Replace(lines[lines.IndexOf(line)], newTemp.ToString("#.###"), 1));
            }

            float newReferenceAvg;
            GetReferenceAvg(newLines, out newReferenceAvg);

            MessageBox.Show($"Reference cycle temperature for {sn} adjusted from {referenceAvg}C to {newReferenceAvg}C.","Temperature Adjust",MessageBoxButton.OK, MessageBoxImage.Information);

            //Write and save new file.
            File.WriteAllLines(file, newLines);
        }
        
        public void RestoreTcomp(string sn)
        {
            if (sn == "") { return; }
            string backupFile = "";
            string restoreTo = "";

            //Find tcomp file for sn
            List<string> files = GetFilesWith(tcompBackupDir, sn);
            if(files.Count == 0) { MessageBox.Show($"No backup found for {sn} in {tcompBackupDir}.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            else
            {backupFile = files[0];}

            //Find sn group folder to copy the file into
            string groupFolder = GetGroupFolder(tcompDir, sn);
            if(groupFolder == "") { MessageBox.Show($"Could not find results folder for {sn}.","Folder Not Found",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
            restoreTo = $@"{groupFolder}\{System.IO.Path.GetFileName(backupFile)}";

            try 
            { 
                File.Move(backupFile, restoreTo);
                MessageBox.Show($"T-Comp file for {sn} restored to {restoreTo}.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"{ex}","Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        //Private Helper Functions----------------------------------------------------------------------------------------

        //Get reference cycle average from a t-comp file. Return 0.0f if there is an error.
        private bool GetReferenceAvg(List<string> lines, out float referenceAvg)
        {
            referenceAvg = 0.0f;
            foreach (string line in lines)
            {
                if (lines.IndexOf(line) == 0)
                { continue; }
                else if (lines.IndexOf(line) == 6)
                { break; }
                String[] separatedLine = line.Split("\t");
                float lineTemp;
                if (!float.TryParse(separatedLine[1], out lineTemp))
                { MessageBox.Show($"Error parsing temperature value on line {lines.IndexOf(line)}.", "Float Parse Error", MessageBoxButton.OK, MessageBoxImage.Error); return false; }

                referenceAvg += lineTemp;
            }
            referenceAvg /= 5.0f;
            return true;
        }
        //Find all files in directory that contain a certain string. Return empty list if none found.
        private List<string> GetFilesWith(string directory, string find)
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
        private string GetGroupFolder(string directory, string sn)
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

        //Recursively check lines for algo errors. Return line number + 1 (will equal the number of lines in the original list) if all next lines contain algo errors.
        private int CheckNext(List<int> linesWithAlgoError,int ln, int lastLineNum)
        {
            if(ln == lastLineNum)
            {
                if (linesWithAlgoError.Contains(ln))
                { return ln + 1; }
                else
                {
                    return ln;
                }
            }
            return linesWithAlgoError.Contains(ln + 1) ? CheckNext(linesWithAlgoError,ln + 1,lastLineNum) : ln + 1;
        }

    }
}
