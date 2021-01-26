using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace HelixTroubleshootingWPF
{
    //Represents an instance of a sensor as it is represented in a particular sensor.xml file
    class HelixSensor
    {
        //Properties
        public string SerialNumber { get; set; } //Should include "SN" prefix
        public string CameraSerial { get; set; }
        public string PartNumber { get; set; }
        public string SensorRev { get; set; }
        public string RectRev { get; set; }
        public string RectPosRev { get; set; }
        public string AccPosRev { get; set; }
        public DateTime Date { get; set; }
        public string Color { get; set; }
        public string LaserClass { get; set; }

        //Constructors
        public HelixSensor()
        {
            
        }
        public HelixSensor(string sensorXmlFolder)
        {
            GetSensorData(sensorXmlFolder);
        }
        
        //Methods
        //Option to set parameters, or re-set if xml folder was not provided to constructor.
        public bool GetSensorData(string containingFolder)
        {
            if (!Directory.Exists(containingFolder)) { return false; }

            string xmlPath = "";
            foreach(string file in Directory.GetFiles(containingFolder))
            {
                if (System.IO.Path.GetFileName(file).Contains(".xml") & System.IO.Path.GetFileName(file).Contains("SN") & System.IO.Path.GetFileName(file).Length==12) { xmlPath = file;}
            }

            if (xmlPath == "") { return false; }
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlPath);
            string format = "ddd MMM dd HH:mm:ss yyyy";
            //Date = DateTime.ParseExact("Wed Jul 22 08:24:53 2020\n", format, CultureInfo.InvariantCulture);
            Date = DateTime.ParseExact(xml.ChildNodes[0].Attributes[1].Value.Replace("\n","").Replace("\r",""), format, CultureInfo.InvariantCulture); //RECT_OUTPUT > Date
            SerialNumber = xml.ChildNodes[0].ChildNodes[0].ChildNodes[0].Attributes[0].Value;
            PartNumber = xml.ChildNodes[0].ChildNodes[0].ChildNodes[0].Attributes[2].Value; //RECT_OUTPUT > SENSORS > SENSOR > Part_Number
            SensorRev = xml.ChildNodes[0].ChildNodes[0].ChildNodes[0].Attributes[3].Value;
            RectRev = xml.ChildNodes[0].ChildNodes[0].ChildNodes[0].Attributes[4].Value;
            RectPosRev = xml.ChildNodes[0].ChildNodes[0].ChildNodes[0].Attributes[5].Value;
            AccPosRev = xml.ChildNodes[0].ChildNodes[0].ChildNodes[0].Attributes[6].Value;
            Color = xml.ChildNodes[0].ChildNodes[0].ChildNodes[0].Attributes[16].Value;
            LaserClass = xml.ChildNodes[0].ChildNodes[0].ChildNodes[0].Attributes[15].Value;

            CameraSerial = xml.ChildNodes[0].ChildNodes[1].ChildNodes[0].Attributes[0].Value; //RECT_OUTPUT > IMAGERS > IMAGER > Imager_UID

            return true;
        }
    }
}
