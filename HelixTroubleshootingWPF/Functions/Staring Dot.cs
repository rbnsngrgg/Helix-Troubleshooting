using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
//using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

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
            ProgressBar progressBar = new ProgressBar
            { Visibility = Visibility.Visible };
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
                { "tl", new int[2]{ 0,0} },
                { "tr", new int[2]{ 0,1200} },
                { "bl", new int[2]{ 1600,0} },
                { "br", new int[2]{ 0,0} }
            };
            EvoLaserImage image1 = null;
            EvoLaserImage image2 = null;
            foreach(string img in images) //Get 2 different images of same z value
            {
                string zValue = GetImageZValue(img);
                if (zValue == z)
                {
                    if (image1 == null) { image1 = new EvoLaserImage(img); }
                    else if (Path.GetFileNameWithoutExtension(img) != image1.Name & image2 == null) { image2 = new EvoLaserImage(img); break; }
                }
            }
            IPixelCollection<byte> img1Pixels = image1.Magick.GetPixels();
            IPixelCollection<byte> img2Pixels = image2.Magick.GetPixels();
            //Create coords for box corners
            for (int x = image1.Magick.Width / 4; x < (image1.Magick.Width / 4) * 3; x++)
            {
                for (int y = image1.Magick.Height / 3; y < (image1.Magick.Height / 3) * 2; y++)
                {
                    int img1Value = img1Pixels.GetPixel(x, y).GetChannel(0);
                    int img2Value = img2Pixels.GetPixel(x, y).GetChannel(0);
                    if (img1Value > 51 & img2Value > 51)
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
                    EvoLaserImage image = new EvoLaserImage(laserImage);
                    IPixelCollection<byte> pixels = image.Magick.GetPixels();
                    for (int x = box["bl"][0]; x <= box["tr"][0]; x++)
                    {
                        for (int y = box["tr"][1]; y <= box["bl"][1]; y++)
                        {
                            pixels.SetPixel(x, y, pixelValue);
                        }
                    }
                    image.SaveImage(true);
                }
            }
        }
    }
}
