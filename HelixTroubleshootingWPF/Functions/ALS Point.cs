using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        public static void ALSPointRemoval(string directory)
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
                foreach (string line in lines)
                {
                    if (line.Contains('R'))
                    { continue; }

                    string entry = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries)[5];
                    entry = entry.Split(".")[0];

                    //check if number is already in the dict and increment count by 1 if it is, add it if not
                    if (occurrences.ContainsKey(entry))
                    { occurrences[entry] += 1; }
                    else
                    { occurrences.Add(entry, 1); }
                }
                //Iterate again now that we have the count of each number. Remove those that are > 3 occurrences
                foreach (string line in lines)
                {
                    if (line.Contains('R'))
                    {
                        newLines.Add(line);
                        continue;
                    }
                    string entry = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries)[5];
                    entry = entry.Split(".")[0];

                    if (occurrences.ContainsKey(entry))
                    {
                        if (occurrences[entry] <= 3)
                        { newLines.Add(line); }
                    }
                }
                //Re-write file with the new lines
                File.WriteAllLines(file, newLines);
            }
            MessageBox.Show($"Erroneous ALS points removed from {files.Count} file(s).", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
