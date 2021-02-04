using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Windows;
//using System.Windows.Shapes;
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
        public static void LineAnalysis(string rectFolder)
        {
            if (!Directory.Exists(rectFolder)) //If folder doesn't exist
            {MessageBox.Show("The specified directory doesn't exist.","Directory Not Found",MessageBoxButton.OK,MessageBoxImage.Error); return; }

            List<string> images = GetSoloLineImages(rectFolder);
            if(images.Count == 0) //If no images
            { MessageBox.Show("No unfiltered laser images found", "No Laser Images", MessageBoxButton.OK, MessageBoxImage.Error); return; }


        }

        private static List<string> GetSoloLineImages(string folder)
        {
            //Get unfiltered laser line images by regex match
            List<string> images = new List<string>();
            foreach(string file in Directory.GetFiles(folder))
            {
                if(Regex.IsMatch(Path.GetFileName(file), @"TZ-?\d+Y-?\d+X\d+\.tif$"))
                {
                    images.Add(file);
                }
            }
            return images;
        }
    }
}
