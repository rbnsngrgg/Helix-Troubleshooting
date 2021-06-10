using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelixTroubleshootingWPF.Functions
{
    static partial class TToolsFunctions
    {
        public static void RunSensorTest()
        {
            SensorTest test = new SensorTest(Config.SensorIp);
            test.ShowDialog();
        }
    }
}
