using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;

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
        private List<CGInfo> cgs = new List<CGInfo>();
        public List<CGInfo> CGs 
        {
            get { return cgs; }
            set 
            {
                cgs = value;
                if(value.Count > 0)
                {
                    SetCgAverages();
                    SetWidthAverages();
                    SetLineAngle();
                    SetFocusScore();
                }
            }
        }
        public string ZValue {get; private set;}
        public float FocusScore { get; private set; }
        public double LineAngle { get; private set; }
        public Tuple<float, float, float> CgAverages { get; private set; }
        public Tuple<float, float, float> WidthAverages { get; private set; }
        public SoloLaserImage(string path) : base(path)
        {
            GetLaserZValue();
        }

        public void GetLaserZValue()
        {
            if(path == "") { return; }
            //Get file name, split by "A", get first section, replace TZ with blank
            if (path.Contains("TZ") & path.Contains(".tif"))
            { ZValue = Path.GetFileNameWithoutExtension(path).Split("Y")[0].Replace("TZ", ""); } 
        }

        private void SetCgAverages()
        {
            float cgAvgLeft = 0, cgAvg = 0, cgAvgRight = 0;
            int center = CGs.Count / 2;
            foreach(CGInfo cg in CGs)
            {
                cgAvg += cg.Row;
                if (cg.Col < center) { cgAvgLeft += cg.Row; }
                else { cgAvgRight += cg.Row; }
            }
            cgAvgLeft = (float)Math.Round(cgAvgLeft / center, 2);
            cgAvg = (float)Math.Round(cgAvg / CGs.Count, 2);
            cgAvgRight = (float)Math.Round(cgAvgRight / center, 2);
            CgAverages =  new Tuple<float, float, float>(cgAvgLeft, cgAvg, cgAvgRight);
        }
        private void SetFocusScore()
        {
            float focusAvg = 0;
            foreach(CGInfo cg in CGs)
            {
                focusAvg += cg.Score;
            }
            FocusScore = (float)Math.Round(focusAvg / CGs.Count, 2);
        }
        private void SetWidthAverages()
        {
            float widthAvgLeft = 0, widthAvg = 0, widthAvgRight = 0;
            int center = CGs.Count / 2;
            foreach (CGInfo cg in CGs)
            {
                widthAvg += cg.LineWidth;
                if (cg.Col < center) { widthAvgLeft += cg.LineWidth; }
                else { widthAvgRight += cg.LineWidth; }
            }
            widthAvgLeft = (float)Math.Round(widthAvgLeft / center,2);
            widthAvg = (float)Math.Round(widthAvg / CGs.Count, 2);
            widthAvgRight = (float)Math.Round(widthAvgRight / center, 2);
            WidthAverages = new Tuple<float, float, float>(widthAvgLeft, widthAvg, widthAvgRight);
        }
        private void SetLineAngle()
        {
            double lineDegrees = (180/Math.PI) * Math.Atan((CGs[^1].Row - CGs[0].Row) / Magick.Width);
            LineAngle = Math.Round(lineDegrees, 2);
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
            //Get file name, split by "A", get first section, replace TZ with blank
            if (path.Contains("TZ") & path.Contains(".tif"))
            { ZValue = Path.GetFileNameWithoutExtension(path).Split("A")[0].Replace("TZ", ""); }
        }
    }

    struct CGInfo
    {
        public float Col;
        public float Row;
        public float Score;
        public float LineWidth;
        public int PeakIntensity;
        public CGInfo(float col, float row, float score, float lineWidth, int peakIntensity)
        {
            Col = col;
            Row = row;
            Score = score;
            LineWidth = lineWidth;
            PeakIntensity = peakIntensity;
        }

        public override string ToString()
        {
            return $"{Row.ToString("F2")}\t{Col}\t{LineWidth}\t{PeakIntensity}\t{Score}";
        }
    }
}
