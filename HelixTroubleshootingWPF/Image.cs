using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ImageMagick;

namespace HelixTroubleshootingWPF
{
    class HelixImage
    {
        protected MagickImage magick = null;

        protected static MagickReadSettings settings = new MagickReadSettings();
        public string Name { get; protected set; }
        protected string path = "";
        public MagickImage Magick
        {
            get
            {
                if (magick == null)
                { magick = new MagickImage(path, settings); }
                return magick;
            }
            protected set { magick = value; } 
        }

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
                Name = System.IO.Path.GetFileName(path);
                //Magick = new MagickImage(path, settings);
            }
        }
        public HelixImage(string path, MagickImage magick)
        {
            settings.SetDefine("tiff:ignore-tags", "37373");
            this.path = path;
            Name = System.IO.Path.GetFileName(path);
            this.Magick = magick;
        }

        //Methods
        public bool SaveImage(bool overwrite, string path = "")
        {
            if (overwrite)
            {
                Magick.Write(this.path);
                return true;
            }
            else
            {
                try
                {
                    Magick.Write(path);
                    return true;
                }
                catch (System.Exception)
                {
                    return false;
                }
            }
        }

        //Get pixel
        public int GetPixelValue(int x, int y)
        {
            return Magick.GetPixels().GetPixel(x,y).GetChannel(0);
        }
    }

    class SoloLaserImage : HelixImage
    {
        public string ZValue {get; private set;}

        public SoloLaserImage(string path) : base(path)
        {
            GetLaserZValue();
        }

        public void GetLaserZValue()
        {
            if(path == "") { return; }
            if (path.Contains("TZ") & path.Contains(".tif"))
            { ZValue = Path.GetFileNameWithoutExtension(path).Split("A")[0].Replace("TZ", ""); } //Get file name, split by "A", get first section, replace TZ with blank
        }
    }

    class EvoLaserImage : HelixImage
    {
        public string ZValue { get; private set; }

        public EvoLaserImage(string path) : base(path)
        {
            GetLaserZValue();
        }

        public void GetLaserZValue()
        {
            if (path == "") { return; }
            if (path.Contains("TZ") & path.Contains(".tif"))
            { ZValue = Path.GetFileNameWithoutExtension(path).Split("A")[0].Replace("TZ", ""); } //Get file name, split by "A", get first section, replace TZ with blank
        }
    }
}
