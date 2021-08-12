using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
//using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using HelixTroubleshootingWPF.Objects;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        public static void StaringDotRemoval(string imagesFolder)
        {
            if (!Directory.Exists(imagesFolder)) { return; }
            HelixSensor sensor = new HelixSensor(imagesFolder);
            List<string> laserImages = GetLaserLineImages(imagesFolder);
            List<string> zValues = ListZValues(laserImages);
            ProgressBar progressBar = new(){ Visibility = Visibility.Visible };
            progressBar.Show();
            progressBar.mainProgressBar.Visibility = Visibility.Visible;
            float progressTick = 100f / (float)zValues.Count;
            int zCount = 1;
            foreach (string z in zValues)
            {
                progressBar.label.Dispatcher.Invoke(() => progressBar.label.Text = $"Z Value {z} ({zCount}/{zValues.Count})", DispatcherPriority.Background);
                progressBar.label.Text = $"Z Value {z} ({zCount}/{zValues.Count})";
                Dictionary<string, int[]> box = GetStaringDotBox(z, ref laserImages);
                RemoveDots(z, box, laserImages);
                progressBar.mainProgressBar.Dispatcher.Invoke(() => progressBar.mainProgressBar.Value += progressTick, DispatcherPriority.Background);
                zCount++;
            }
            progressBar.mainProgressBar.Value = 100;
            MessageBox.Show("Staring dot image processing complete", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            progressBar.Visibility = Visibility.Hidden;
            progressBar.Close();
        }
        private static Dictionary<string,int[]> GetStaringDotBox(string z, ref List<string> images)
        {
            Dictionary<string, int[]> box = new Dictionary<string, int[]>() 
            { 
                { "tr", new int[2]{ 0,1200} },
                { "bl", new int[2]{ 1600,0} },
            };
            HelixImage image1 = null;
            HelixImage image2 = null;
            foreach(string img in images) //Get 2 different images of same z value
            {
                string zValue = GetImageZValue(img);
                if (zValue == z && Path.GetFileNameWithoutExtension(img).Contains("A0"))
                {
                    if (image1 == null) { image1 = new HelixImage(img); }
                    else if (Path.GetFileNameWithoutExtension(img) != image1.Name && image2 == null) { image2 = new HelixImage(img); break; }
                }
            }
            IPixelCollection<byte> img1Pixels = image1.Magick.GetPixels();
            IPixelCollection<byte> img2Pixels = image2.Magick.GetPixels();
            box["tr"][1] = image1.Magick.Height;
            box["bl"][0] = image1.Magick.Width;

            int intensityThreshold = (int)(255f * Config.StaringDotSensitivityPercent);
            int xStart = (int)((Config.StaringDotExcludeXPercent * image1.Magick.Width) / 2);
            int xEnd = image1.Magick.Width - xStart;
            int yStart = (int)((Config.StaringDotExcludeYPercent * image1.Magick.Height) / 2);
            int yEnd = image1.Magick.Height - yStart;

            //Create coords for box corners
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    if(img1Pixels.GetPixel(x, y).GetChannel(0) > intensityThreshold &&
                        img2Pixels.GetPixel(x, y).GetChannel(0) > intensityThreshold)
                    {
                        if (x < box["bl"][0]) { box["bl"][0] = x; }
                        if (x > box["tr"][0]) { box["tr"][0] = x; }

                        if (y < box["tr"][1]) { box["tr"][1] = y; }
                        if (y > box["bl"][1]) { box["bl"][1] = y; }
                    }
                }
            }
            //Add padding for the box borders
            if (box["bl"][0] > 5) { box["bl"][0] -= 5; };
            box["bl"][1] += 5;
            box["tr"][0] += 5;
            if (box["tr"][1] > 5) { box["tr"][1] -= 5; }
            image1.Magick.Dispose();
            image2.Magick.Dispose();
            img1Pixels.Dispose();
            img2Pixels.Dispose();
            return box;
        }
        private static List<string> GetLaserLineImages(string folder)
        {
            List<string> lineImages = new List<string>();
            foreach(string file in Directory.GetFiles(folder))
            {
                if(Regex.IsMatch(Path.GetFileName(file), @"TZ-?\d+A-?\d+L\d+I0\.tif$"))
                {
                    lineImages.Add(file);
                }
            }
            return lineImages;
        }
        private static List<string> ListZValues(List<string> images)
        {
            List<string> zValues = new List<string>();
            
            foreach (string img in images)
            {
                string zValue = GetImageZValue(img);
                if (!zValues.Contains(zValue))
                { 
                    zValues.Add(zValue); 
                } 
            }
            return zValues;
        }
        private static string GetImageZValue(string path)
        {
            return Path.GetFileNameWithoutExtension(path).Split("A")[0].Replace("TZ", "");
        }
        private static void RemoveDots(string zValue, Dictionary<string, int[]> box, List<string> laserImages)
        {
            byte[] pixelValue = BitConverter.GetBytes(0);
            foreach (string laserImage in laserImages)
            {
                if (GetImageZValue(laserImage) == zValue)
                {
                    HelixImage image = new HelixImage(laserImage);
                    IPixelCollection<byte> pixels = image.Magick.GetPixels();
                    for (int x = box["bl"][0]; x <= box["tr"][0]; x++)
                    {
                        for (int y = box["tr"][1]; y <= box["bl"][1]; y++)
                        {
                            pixels.SetPixel(x, y, pixelValue);
                        }
                    }
                    image.SaveImage(true);
                    image.Magick.Dispose();
                    pixels.Dispose();
                }
            }
        }
    }
}
