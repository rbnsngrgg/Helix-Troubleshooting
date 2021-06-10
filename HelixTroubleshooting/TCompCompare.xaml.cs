using System.Collections.Generic;
using System.Windows;

namespace HelixTroubleshootingWPF
{
    /// <summary>
    /// Interaction logic for TCompCompare.xaml
    /// </summary>
    public partial class TCompCompare : Window
    {
        public TCompCompare()
        {
            InitializeComponent();
            BeforePlot.plt.AxisBounds(double.NegativeInfinity, double.PositiveInfinity, -1.5, 1.5);
            BeforePlot.plt.Title("Before");
            BeforePlot.plt.XLabel("Time");
            BeforePlot.plt.YLabel("Delta Z");
            AfterPlot.plt.AxisBounds(double.NegativeInfinity, double.PositiveInfinity, -1.5, 1.5);
            AfterPlot.plt.XLabel("Time");
            AfterPlot.plt.YLabel("Delta Z");
            AfterPlot.plt.Title("After");
        }

        public void PlotTComp(List<string> lines, string plot)
        {
            List<List<string>> rotatedData = RotateData(lines);
            int dataLength = rotatedData[0].Count - 1;
            foreach(List<string> col in rotatedData)
            {
                if(col[0].Contains("[Z]"))
                {
                    List<double> xs = new List<double>();
                    List<double> ys = new List<double>();
                    for(int i = 0; i < dataLength; i++)
                    { xs.Add(i); }
                    foreach(string value in col.GetRange(1, dataLength))
                    {
                        ys.Add(double.Parse(value) - double.Parse(col[1]));
                    }
                    if (plot == "before") { BeforePlot.plt.PlotScatter(xs.ToArray(), ys.ToArray()); }
                    else if (plot == "after") { AfterPlot.plt.PlotScatter(xs.ToArray(), ys.ToArray()); };
                }
            }
            BeforePlot.Render();
            AfterPlot.Render();
        }

        private List<List<string>> RotateData(List<string> lines)
        {
            //"Rotate" the structure of the tcomp data for easier use.
            List<List<string>> vertical = new List<List<string>>(); //List of lists. Sub lists: index 0 = header, all others are data.
            foreach(string header in lines[0].Split("\t"))
            { vertical.Add(new List<string>() { header }); }
            foreach(string line in lines)
            {
                if(lines.IndexOf(line) == 0) { continue; }
                List<string> split = new List<string>();
                split.AddRange(line.Split("\t"));
                for (int i = 0; i < split.Count; i++)
                {
                    vertical[i].Add(split[i]);
                }
            }
            return vertical;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;

        private void AdjustButton_Click(object sender, RoutedEventArgs e) => this.DialogResult = true;
    }
}
