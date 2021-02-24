using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        public static void AlgorithmErrors(string sn)
        {
            if (sn == "") { return; }

            string file = GetTcompFile(sn);
            if(file == "") { return; }
            List<string> lines = new List<string>(File.ReadAllLines(file));
            List<string> originalLines = new List<string>();
            originalLines.AddRange(lines);

            //Find indexes of lines with algorithm errors
            List<int> linesWithAlgoError = GetAlgoErrorLines(lines);

            //Correct algo errors
            foreach (int lineNum in linesWithAlgoError)
            {
                string line = lines[lineNum];
                List<string> entries = new List<string>(line.Split('\t'));
                foreach (string entry in entries)
                {
                    if (entries.IndexOf(entry) == 0) //Time column
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
                        int nextLineNum = CheckNext(linesWithAlgoError, lineNum, lines.Count - 1);
                        if (nextLineNum == lines.Count)
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
                        if (current < 500)
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
                            double newValue = Math.Round((previous + next) / 2, 6);

                            lines[lineNum] = regex.Replace(lines[lineNum], newValue.ToString("#.######"), 1);
                        }
                    }
                }
            }
            if (PlotBeforeAndAfter(originalLines, lines))
            {
                //Backup the original file, write and save the new lines with ".txt" file extension
                if (file.ToLower().Contains(".day2"))
                {
                    string backupFile = $"{tcompBackupDir}\\{System.IO.Path.GetFileName(file)}";
                    if (File.Exists(backupFile))
                    {
                        File.Delete(backupFile);
                    }
                    File.Move(file, backupFile);
                    File.WriteAllLines($"{file.Replace(System.IO.Path.GetFileName(file), "")}{System.IO.Path.GetFileNameWithoutExtension(file)}.txt", lines);
                }
                else
                {
                    File.WriteAllLines(file, lines);
                }
                MessageBox.Show($"Algorithm errors fixed for {sn}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public static void TempAdjust(string sn, string tempString)
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
            if (!GetReferenceAvg(lines, out referenceAvg))
            { return; }
            tempOffset = setTemp - referenceAvg;

            List<string> newLines = new List<string>();
            //Change col 1 values so that reference average matches the desired temperature
            foreach (string line in lines)
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

            if (PlotBeforeAndAfter(lines, newLines, referenceAvg, newReferenceAvg))
            {
                //Write and save new file.
                File.WriteAllLines(file, newLines);
                MessageBox.Show($"Reference cycle temperature for {sn} adjusted from {referenceAvg}C to {newReferenceAvg}C.", "Temperature Adjust", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public static void RestoreTcomp(string sn)
        {
            if (sn == "") { return; }
            string backupFile = "";
            string restoreTo = "";

            //Find tcomp file for sn
            List<string> files = GetFilesWith(tcompBackupDir, sn);
            if (files.Count == 0) { MessageBox.Show($"No backup found for {sn} in {tcompBackupDir}.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            else
            { backupFile = files[0]; }

            //Find sn group folder to copy the file into
            string groupFolder = GetGroupFolder(tcompDir, sn);
            if (groupFolder == "") { MessageBox.Show($"Could not find results folder for {sn}.", "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            restoreTo = $@"{groupFolder}\{System.IO.Path.GetFileName(backupFile)}";

            try
            {
                File.Move(backupFile, restoreTo);
                MessageBox.Show($"T-Comp file for {sn} restored to {restoreTo}.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static List<int> GetAlgoErrorLines(List<string> lines)
        {
            List<int> errorLines = new List<int>();
            foreach (string line in lines)
            {
                if (line == lines[0])
                { continue; }
                List<string> entries = new List<string>(line.Split('\t'));
                foreach (string entry in entries)
                {
                    if (entries.IndexOf(entry) == 0)
                    { continue; }
                    if (Double.TryParse(entry, out double number))
                    {
                        if (number > 500)
                        {
                            errorLines.Add(lines.IndexOf(line));
                            break;
                        }
                    }
                }
            }
            return errorLines;
        }
        private static string GetTcompFile(string sn)
        {
            //Get t-comp results folder that matches the entered SN (e.g. SN138XXX for SN138265)
            string snGroupFolder = GetGroupFolder(tcompDir, sn);
            if (snGroupFolder == "") 
            {
                MessageBox.Show($"Could not find the t-comp file for {sn}.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return "";
            }
            //Get the t-comp file
            List<string> files = GetFilesWith(snGroupFolder, sn);
            if (files.Count == 0)
            {
                MessageBox.Show($"No t-comp file found matching {sn} in folder: {snGroupFolder}.", "None found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return "";
            }
            return files[0];
        }
        //Get reference cycle average from a t-comp file. Return 0.0f if there is an error.
        private static bool GetReferenceAvg(List<string> lines, out float referenceAvg)
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
        //Recursively check lines for algo errors. Return line number + 1 (will equal the number of lines in the original list) if all next lines contain algo errors.
        private static int CheckNext(List<int> linesWithAlgoError, int ln, int lastLineNum)
        {
            if (ln == lastLineNum)
            {
                if (linesWithAlgoError.Contains(ln))
                { return ln + 1; }
                else
                {
                    return ln;
                }
            }
            return linesWithAlgoError.Contains(ln + 1) ? CheckNext(linesWithAlgoError, ln + 1, lastLineNum) : ln + 1;
        }

        private static bool PlotBeforeAndAfter(List<string> before, List<string> after, float refAvgBefore = 0.0f, float refAvgAfter = 0.0f)
        {
            TCompCompare compareWindow = new TCompCompare();
            if(refAvgBefore > 0.0f & refAvgAfter > 0.0f)
            {
                compareWindow.BeforePlot.plt.Title($"Before: Reference Average {refAvgBefore}");
                compareWindow.AfterPlot.plt.Title($"After: Reference Average {refAvgAfter}");
            }
            compareWindow.PlotTComp(before, "before");
            compareWindow.PlotTComp(after, "after");
            if(compareWindow.ShowDialog() == true) { return true; }
            return false;
        }
    }
}
