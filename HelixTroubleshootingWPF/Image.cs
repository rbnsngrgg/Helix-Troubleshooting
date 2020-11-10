using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImageMagick;

namespace TTools
{
    class HelixImage
    {
        private MagickReadSettings settings = new MagickReadSettings();
        private int rows;
        private int columns;
        private string name;
        private string path;
        private MagickImage magick;

        //Constructors
        public HelixImage()
        {
            settings.SetDefine("tiff:ignore-tags", "37373");
        }
        public HelixImage(string path)
        {
            settings.SetDefine("tiff:ignore-tags", "37373");
            if (File.Exists(path))
            {
                this.path = path;
                name = System.IO.Path.GetFileName(path);
                magick = new MagickImage(path, settings);
                rows = magick.Height;
                columns = magick.Width;
            }
        }
        public HelixImage(string path, MagickImage magick)
        {
            settings.SetDefine("tiff:ignore-tags", "37373");
            this.path = path;
            name = System.IO.Path.GetFileName(path);
            this.magick = magick;
            rows = magick.Height;
            columns = magick.Width;
        }

        //Methods
        public bool SaveImage(bool overwrite, string path = "")
        {
            if (overwrite)
            {
                magick.Write(this.path);
                return true;
            }
            else
            {
                try
                {
                    magick.Write(path);
                    return true;
                }
                catch (System.Exception)
                {
                    return false;
                }
            }
        }
    }

    class SoloLaserImage : HelixImage
    {
        
    }

    class EvoLaserImage : HelixImage
    { 

    }
}
