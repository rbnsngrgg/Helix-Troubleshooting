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

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        public static void SphereSummary(string directory)
        {
            //Get all illuminated sphere images
            List<string> images = GetFilesWith(directory, "RZ*.tif");
            if (images.Count == 0)
            { MessageBox.Show($"No illuminted sphere images found in {directory}.", "No Images Found", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            //Get z-values from file names
            List<string> zValues = new List<string>();
            foreach (string image in images)
            {
                string zValue = System.IO.Path.GetFileName(image).Split("Y")[0];
                if (!zValues.Contains(zValue))
                { zValues.Add(zValue); }
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
                foreach (string image in images)
                {
                    if (image.Contains(zValue))
                    {
                        //Loop to add each image to summary via lighten operation
                        summaryImage = new MagickImage(image, settings);
                        foreach (string image2 in images)
                        {
                            //if (image2.Contains(zValue))
                            if(System.IO.Path.GetFileName(image2).Split("Y")[0] == zValue)
                            {
                                summaryImage.Composite(new MagickImage(image2, settings), CompositeOperator.Lighten);
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
            MessageBox.Show("Summary image processing complete", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            pBar.Visibility = Visibility.Hidden;
            pBar.Close();
        }
    }
}
