using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows;
using HelixTroubleshootingWPF.Objects;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        public static void LineAnalysis(string rectFolder)
        {
            if (!Directory.Exists(rectFolder)) //If folder doesn't exist
            {MessageBox.Show("The specified directory doesn't exist.","Directory Not Found",MessageBoxButton.OK,MessageBoxImage.Error); return; }

            HelixSoloSensor sensor = new HelixSoloSensor(rectFolder);
            List<SoloLaserImage> images = GetSoloLineImages(rectFolder);
            if(images.Count == 0) //If no images
            { MessageBox.Show("No unfiltered laser images found", "No Laser Images", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            //Create results folder
            string resultFolder = Path.Join(rectFolder, @"\Line Analysis");
            Directory.CreateDirectory(resultFolder);

            if(!GetCgs(ref images)) { return; }
            ProgressBar pb = new ProgressBar()
            {
                Title = "Writing Logs",
                Visibility = Visibility.Visible
            };
            pb.SetProgressTick(images.Count + 1);
            pb.SetLabel($"Writing log (1/{images.Count+1}): Focus_Summary.txt");
            WriteSummaryLog(ref sensor, images, resultFolder);
            foreach (SoloLaserImage image in images)
            {
                if (pb.Canceled) { pb.Close(); break; }
                pb.SetLabel($"Writing log ({images.IndexOf(image)+2}/{images.Count+1}): {Path.GetFileNameWithoutExtension(image.Name)}.txt");
                pb.TickProgress();
                WriteLineLog(ref sensor, image, resultFolder);
            }
            pb.SetValueComplete();
            pb.Close();
            MessageBox.Show("Line analysis complete.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private static List<SoloLaserImage> GetSoloLineImages(string folder)
        {
            //Get unfiltered laser line images by regex match
            List<SoloLaserImage> images = new List<SoloLaserImage>();
            foreach(string file in Directory.GetFiles(folder))
            {
                if(Regex.IsMatch(Path.GetFileName(file), @"TZ-?\d+Y-?\d+X\d+\.tif$"))
                {
                    images.Add(new SoloLaserImage(file));
                }
            }
            return SortNearFar(images);
        }
        private static bool GetCgs(ref List<SoloLaserImage> images) //Get CG info for each image in list
        {
            ProgressBar pb = new ProgressBar()
            { 
                Title = "Processing Line CGs",
                Visibility = Visibility.Visible 
            };
            pb.SetProgressTick(images.Count);
            foreach(SoloLaserImage image in images)
            {
                if (pb.Canceled) { pb.Close(); return false; }
                pb.SetLabel($"Image ({images.IndexOf(image) + 1}/{images.Count}): {image.Name}");
                image.CGs = GetCgInfo(image);
                pb.TickProgress();
            }
            pb.SetValueComplete();
            pb.Visibility = Visibility.Hidden;
            pb.Close();
            return true;
        }
        private static List<CGInfo> GetCgInfo(SoloLaserImage image)
        {
            List<CGInfo> cgList = new List<CGInfo>();
            var pixels = image.Magick.GetPixels();
            List<IPixel<byte>> cgPixels = new List<IPixel<byte>>();
            for (int col = 0; col < image.Magick.Width; col++)
            {
                int peak = 0;
                for (int row = 0; row < image.Magick.Height; row++)
                {
                    var pixel = pixels.GetPixel(col, row).GetChannel(0);
                    if (pixel > peak) { peak = pixel; }
                }
                if (peak < (Config.LineThresholdPercent / 100.0) * 255.0) { continue; }
                for (int row = 0; row < image.Magick.Height; row++)
                {
                    var pixel = pixels.GetPixel(col, row);
                    if (pixel.GetChannel(0) > (Config.LineThresholdPercent / 100.0) * peak)
                    { 
                        cgPixels.Add(pixel);
                    }
                }
                float cgTotal = 0;
                float cgMoment = 0;
                foreach(var pixel in cgPixels)
                {
                    cgTotal += pixel.GetChannel(0);
                    cgMoment += pixel.GetChannel(0) * (pixel.Y - cgPixels[0].Y);
                }
                float cg = (float)(Math.Round(cgPixels[0].Y + (cgMoment / cgTotal), 1));
                Tuple<int, float> scoreIntensity = GetScoreIntensity(cgPixels);
                cgList.Add(new CGInfo(col, cg, scoreIntensity.Item2, cgPixels.Count, scoreIntensity.Item1));
                cgPixels.Clear();
            }
            return cgList;
        }
        private static Tuple<int,float> GetScoreIntensity(List<IPixel<byte>> pixels)
        {
            //Return tuple<int,float> (peak intensity, focus score)
            int peak = 0;
            int run1 = 1;
            int run2 = 1;
            foreach(var pixel in pixels)
            {
                if (pixel.GetChannel(0) > peak) { peak = pixel.GetChannel(0); }
            }
            int rise = peak;
            //First side
            for(int i = 0; i < pixels.Count; i++)
            {
                if(pixels[i].GetChannel(0) == peak & i != 0)
                {
                    run1 += pixels[i].Y - pixels[0].Y;
                    break;
                }
            }
            float score1 = rise / run1;
            //Second side
            for(int i = pixels.Count - 1; i >= 0; i--)
            {
                if(pixels[i].GetChannel(0) == peak & i != pixels.Count - 1)
                {
                    run2 += pixels[pixels.Count-1].Y - pixels[i].Y;
                    break;
                }
            }
            float score2 = rise / run2;

            float score = (float)Math.Round(score1 + score2, 2);
            return new Tuple<int, float>(peak, score);
        }
        private static List<SoloLaserImage> SortNearFar(List<SoloLaserImage> images)
        {
            bool sorted = false;
            while (!sorted)
            {
                sorted = true;
                for (int i = 0; i < images.Count; i++)
                {
                    if (i + 1 < images.Count)
                    {
                        if (int.Parse(images[i].ZValue) > int.Parse(images[i + 1].ZValue))
                        {
                            SoloLaserImage popImage = images[i + 1];
                            images.RemoveAt(i + 1);
                            images.Insert(i, popImage);
                            sorted = false;
                        }
                    }
                }
            }
            return images;
        }
        private static bool WriteLineLog(ref HelixSoloSensor sensor, SoloLaserImage image, string resultFolder)
        {
            List<string> logLines = new List<string>();
            logLines.AddRange(new string[] {$"{sensor.SerialNumber}\t{sensor.PartNumber}\t{sensor.Date}", image.Name,
            "CG_Row\tCG_Col\tWidth\tPeak_Intensity\tFocus_Score"});
            if (image.CGs.Count > 0)
            {
                foreach (CGInfo cg in image.CGs)
                {
                    logLines.Add(cg.ToString());
                }
                logLines.AddRange(new string[] {
                $"\nCG Count:\t{image.CGs.Count}\n",
                $"CG Avg:\t{image.CgAverages.Item2}",
                $"CG Avg Left:\t{image.CgAverages.Item1}",
                $"CG Avg Right:\t{image.CgAverages.Item3}\n",
                $"Width Avg:\t{image.WidthAverages.Item2}",
                $"Width Avg Left:\t{image.WidthAverages.Item1}",
                $"Width Avg Right:\t{image.WidthAverages.Item3}\n",
                $"Line Angle Degrees:\t{image.LineAngle}\n",
                $"Line Focus Score:\t{image.FocusScore}"
                });
            }
            else
            {
                logLines.Add("\nNo laser line found");
            }

            string path = Path.Join(resultFolder, $"{Path.GetFileNameWithoutExtension(image.Name)}.txt");
            File.WriteAllLines(path, logLines);
            return true;
        }
        private static bool WriteSummaryLog(ref HelixSoloSensor sensor, List<SoloLaserImage> images, string resultFolder)
        {
            List<string> logLines = new List<string>();
            logLines.AddRange(new string[] { $"{sensor.SerialNumber}\t{sensor.PartNumber}\t{sensor.Date}\n", "Z_Values\t Focus_Score" });
            foreach(SoloLaserImage image in images)
            {
                logLines.Add($"{image.ZValue}\t{image.FocusScore}");
            }
            string path = Path.Join(resultFolder, "Focus_Summary.txt");
            File.WriteAllLines(path, logLines);

            return true;
        }
    }
}
