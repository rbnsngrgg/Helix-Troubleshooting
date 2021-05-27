using HelixTroubleshootingWPF.Functions;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System;

namespace HelixTroubleshootingWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly string Version = "3.0.0";
        public MainWindow()
        {
            InitializeComponent();
            Title = $"{Title} {Version}";
            TToolsFunctions.Config.LoadConfig();
            AddFunctions();
            ToggleDataGather();
        }

        
        private void UpdateDetails(string item)
        {
            /// <summary>Updates Details Groupbox based on the item that was selected</summary>
            if (item.Contains("ALS Point Removal"))
            {
                DetailsBox.Text = "Removes erroneous entries in the CG text files for Evo sensors that cause ALS Point errors during rectification." +
                    "\nEnter the directory of a rectification images folder, then click \"Start\".";
                DetailsTextBox1.Visibility = System.Windows.Visibility.Visible;
                DetailsTextBox2.Visibility = System.Windows.Visibility.Hidden;
                DetailsButton1.Content = "Start";
                DetailsButton1.Visibility = System.Windows.Visibility.Visible;
                DetailsButton2.Content = "";
                DetailsButton2.Visibility = System.Windows.Visibility.Hidden;
            }
            else if(item.Contains("Fix Algorithm Errors"))
            {
                DetailsBox.Text = "Patches algorithm errors in an Evo sensor's t-comp file.\nEnter the serial number (SNXXXXXX) and click \"Start\"." +
                    "\n\nAlternatively, an original t-comp file can be replaced by clicking \"Restore Original\"";
                DetailsTextBox1.Visibility = System.Windows.Visibility.Visible;
                DetailsTextBox2.Visibility = System.Windows.Visibility.Hidden;
                DetailsButton1.Content = "Start";
                DetailsButton1.Visibility = System.Windows.Visibility.Visible;
                DetailsButton2.Content = "Restore Original";
                DetailsButton2.Visibility = System.Windows.Visibility.Visible;

            }
            else if(item.Contains("Illuminated Sphere Summary"))
            {
                DetailsBox.Text = "Generates compilation images of illuminated spheres (\"RZ\" prefix) from the rectification images folder of Evo or Solo sensors. " +
                    "The summary images are placed in a folder named \"Summary\".\n\nEnter the directory of a rectification images folder, then click \"Start\".";
                DetailsTextBox1.Visibility = System.Windows.Visibility.Visible;
                DetailsTextBox2.Visibility = System.Windows.Visibility.Hidden;
                DetailsButton1.Content = "Start";
                DetailsButton1.Visibility = System.Windows.Visibility.Visible;
                DetailsButton2.Content = "";
                DetailsButton2.Visibility = System.Windows.Visibility.Hidden;
            }
            else if(item.Contains("Solo Laser Line Analysis"))
            {
                DetailsBox.Text = "Analyzes the laser line images from the rectification images folder of a Helix Solo sensor. The results are placed in the analysis folder, " +
                    "specified in the config.\n\nEnter a directory for rectification images and click \"Start\".";
                DetailsTextBox1.Visibility = System.Windows.Visibility.Visible;
                DetailsTextBox2.Visibility = System.Windows.Visibility.Hidden;
                DetailsButton1.Content = "Start";
                DetailsButton1.Visibility = System.Windows.Visibility.Visible;
                DetailsButton2.Content = "";
                DetailsButton2.Visibility = System.Windows.Visibility.Hidden;
            }
            else if(item.Contains("Staring Dot Removal"))
            {
                DetailsBox.Text = "Cleans the staring dots from rectification images of Evo sensors, providing a workaround for laser model errors caused by these dots." +
                    "\nEnter the directory of a rectification images folder, then click \"Start\".";
                DetailsTextBox1.Visibility = System.Windows.Visibility.Visible;
                DetailsTextBox2.Visibility = System.Windows.Visibility.Hidden;
                DetailsButton1.Content = "Start";
                DetailsButton1.Visibility = System.Windows.Visibility.Visible;
                DetailsButton2.Content = "";
                DetailsButton2.Visibility = System.Windows.Visibility.Hidden;
            }
            else if(item.Contains("Temperature Adjust"))
            {
                DetailsBox.Text = "Adjust the temperature column of a t-comp file, so that the reference cycle average is equal to the sensor's current operating temperature." +
                    "\n\nEnter the serial number (SNXXXXXX) in the first box, the desired reference average in the 2nd box, then click \"start\".";
                DetailsTextBox1.Visibility = System.Windows.Visibility.Visible;
                DetailsTextBox2.Visibility = System.Windows.Visibility.Visible;
                DetailsButton1.Content = "Start";
                DetailsButton1.Visibility = System.Windows.Visibility.Visible;
                DetailsButton2.Content = "";
                DetailsButton2.Visibility = System.Windows.Visibility.Hidden;
            }
            else if(item.Contains("DACMEMS Data Gather"))
            {
                DetailsBox.Text = "Gather data for each sensor that has run through the DACMEMS Tuning Fixture, paired with its zero degree 2RMS and Max Deviation.";
                DataGatherSettings();
            }
            else if(item.Contains("UFF Data Gather"))
            {
                DetailsBox.Text = "Gather data for each sensor that has run through the Universal Focus Fixture, paired with its zero degree 2RMS and Max Deviation.";
                DataGatherSettings();
            }
            else if(item.Contains("LPF Data Gather"))
            {
                DetailsBox.Text = "Gather data for each sensor that has run through the Laser Power Fixture, paired with its zero degree 2RMS and Max Deviation.";
                DataGatherSettings();
            }
            else if(item.Contains("Pitch Data Gather"))
            {
                DetailsBox.Text = "Gather data for each sensor that has run through the Camera Pitch Fixture, paired with its zero degree 2RMS and Max Deviation.";
                DataGatherSettings();
            }
            else if(item.Contains("Evo Data Gather"))
            {
                DetailsBox.Text = "Gather data from each fixture for each Evo sensor, paired with accuracy test 0 degree 2RMS and Max Deviation.";
                DataGatherSettings();
            }
            else if(item.Contains("Sensor Test"))
            {
                DetailsBox.Text = "Test sensor functionality and gather sensor fixture information.";
                DataGatherSettings();
            }
            else if (item.Contains("Evo KNN Regression"))
            {
                DetailsBox.Text = "K-Nearest Neighbors regression for Helix Evo Sensors. Enter a serial number, then start.";
                DataGatherSettings();
                DetailsTextBox1.Visibility = Visibility.Visible;
            }
            else if (item.Contains("Evo KNN"))
            {
                DetailsBox.Text = "K-Nearest Neighbors algorithm for Helix Evo Sensors. Enter a model number, or leave blank to include all Evo models.";
                DataGatherSettings();
                DetailsTextBox1.Visibility = Visibility.Visible;
            }
            else if (item.Contains("KNN Validation"))
            {
                DetailsBox.Text = "Find and validate the best combination of data for the KNN.";
                DataGatherSettings();
            }
            else if (item.Contains("Test ML.net"))
            {
                DetailsBox.Text = "Test the ML.net regression algorithm with data from the specified Evo sensor. Enter serial number without \"SN\" prefix.";
                DataGatherSettings();
                DetailsTextBox1.Visibility = Visibility.Visible;
            }
        }

        //Event handlers
        private void FunctionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDetails(e.AddedItems[0].ToString());
        }

        private void DetailsButton1_Click(object sender, RoutedEventArgs e)
        {
            if (FunctionList.SelectedItems.Count == 0)
            {
                return;
            }
            string function = FunctionList.SelectedItem.ToString();

            if (function.Contains("ALS Point Removal"))
            {
                TToolsFunctions.ALSPointRemoval(DetailsTextBox1.Text);
            }
            else if (function.Contains("Fix Algorithm Errors"))
            {
                TToolsFunctions.AlgorithmErrors(DetailsTextBox1.Text);
            }
            else if (function.Contains("Illuminated Sphere Summary"))
            {
                TToolsFunctions.SphereSummary(DetailsTextBox1.Text);
            }
            else if (function.Contains("Solo Laser Line Analysis"))
            {
                TToolsFunctions.LineAnalysis(DetailsTextBox1.Text);
            }
            else if (function.Contains("Staring Dot Removal"))
            {
                TToolsFunctions.StaringDotRemoval(DetailsTextBox1.Text);
            }
            else if (function.Contains("Temperature Adjust"))
            {
                TToolsFunctions.TempAdjust(DetailsTextBox1.Text, DetailsTextBox2.Text);
            }
            else if (function.Contains("DACMEMS Data Gather"))
            {
                TToolsFunctions.DacMemsDataGather();
            }
            else if (function.Contains("UFF Data Gather"))
            {
                TToolsFunctions.UffDataGather();
            }
            else if (function.Contains("LPF Data Gather"))
            {
                TToolsFunctions.LpfDataGather();
            }
            else if (function.Contains("Pitch Data Gather"))
            {
                TToolsFunctions.PitchDataGather();
            }
            else if (function.Contains("Evo Data Gather"))
            {
                TToolsFunctions.GatherEvoData();
            }
            else if (function.Contains("Sensor Test"))
            {
                TToolsFunctions.RunSensorTest();
            }
            else if (function == ("Evo KNN"))
            {
                TToolsFunctions.RunKnn("", DetailsTextBox1.Text);
            }
            else if (function.Contains("Evo KNN Regression"))
            {
                TToolsFunctions.RunKnn(DetailsTextBox1.Text,"");
            }
            else if(function.Contains("KNN Validation"))
            {
                TToolsFunctions.RunCombos();
            }
            else if(function.Contains("Test ML.net"))
            {
                TToolsFunctions.TestML();
            }
        }
        private void DetailsButton2_Click(object sender, RoutedEventArgs e)
        {
            if (FunctionList.SelectedItems.Count == 0) { return; }

            string function = FunctionList.SelectedItem.ToString();

            if (function.Contains("Fix Algorithm Errors"))
            {
                TToolsFunctions.RestoreTcomp(DetailsTextBox1.Text);
            }
        }

        private void AddFunctions()
        {
            FunctionList.Items.Clear();
            //Add functions to the list widget
            foreach (string function in TToolsFunctions.functionList)
            {
                ListViewItem newItem = new ListViewItem() { Content = function };
                FunctionList.Items.Add(newItem);
            }
            FunctionList.SelectedItem = FunctionList.Items.GetItemAt(0);
        }
        private void ToggleDataGather()
        {
            foreach (ListViewItem item in FunctionList.Items)
            {
                if (item.Content.ToString().Contains("Data Gather"))
                {
                    if (item.Visibility == Visibility.Visible)
                    { item.Visibility = Visibility.Collapsed; }
                    else
                    { item.Visibility = Visibility.Visible; }
                }
            }
        }
        private void DataGatherSettings()
        {
            DetailsTextBox1.Visibility = System.Windows.Visibility.Hidden;
            DetailsTextBox2.Visibility = System.Windows.Visibility.Hidden;
            DetailsButton1.Content = "Start";
            DetailsButton1.Visibility = System.Windows.Visibility.Visible;
            DetailsButton2.Content = "";
            DetailsButton2.Visibility = System.Windows.Visibility.Hidden;
        }
        private void FileMenuExit_Click(object sender, RoutedEventArgs e)
        {
            HelixTroubleshootingMainWindow.Close();
        }
        private void ToolsMenuToggleDataGather_Click(object sender, RoutedEventArgs e)
        {
            ToggleDataGather();
        }

        private void HelixTroubleshootingMainWindow_Closed(object sender, EventArgs e)
        {
            var processes = Process.GetProcessesByName("HelixTroubleshootingWPF");
            foreach (Process p in processes)
            {
                p.Kill();
            }
        }
    }
}
