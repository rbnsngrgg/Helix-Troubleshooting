using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TTools;
namespace HelixTroubleshootingWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Init class containing main functions
        TToolsFunctions tools = new TToolsFunctions();

        public MainWindow()
        {
            InitializeComponent();
            tools.LoadConfig();
            //Add functions to the list widget
            foreach (string function in tools.functionList)
            {
                ListViewItem newItem = new ListViewItem();
                newItem.Content = function;
                FunctionList.Items.Add(newItem);
            }
            FunctionList.SelectedItem = FunctionList.Items.GetItemAt(0);
        }

        //Updates Details Groupbox based on the item that was selected
        private void UpdateDetails(string item)
        {
            if(item.Contains("ALS Point Removal"))
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
                    "specified in the config.\n\nEnter a directory for rectification images and click \"Start\".\n\nAlternatively, generate data for all Solo sensors in the config" +
                    " directories by clicking \"Analyze All\"";
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

            if(function.Contains("ALS Point Removal"))
                {
                tools.ALSPointRemoval(DetailsTextBox1.Text);
                }
            else if(function.Contains("Fix Algorithm Errors"))
                {
                tools.AlgorithmErrors(DetailsTextBox1.Text);
                }
            else if (function.Contains("Illuminated Sphere Summary"))
                {
                tools.SphereSummary(DetailsTextBox1.Text);
                }
            else if (function.Contains("Solo Laser Line Analysis"))
                {
                }
            else if (function.Contains("Staring Dot Removal"))
                {
                tools.StaringDotRemoval(DetailsTextBox1.Text);
                }
            else if (function.Contains("Temperature Adjust"))
                {
                tools.TempAdjust(DetailsTextBox1.Text,DetailsTextBox2.Text);
                }
        }

        private void DetailsButton2_Click(object sender, RoutedEventArgs e)
        {
            if (FunctionList.SelectedItems.Count == 0) { return; }

            string function = FunctionList.SelectedItem.ToString();

            if (function.Contains("Fix Algorithm Errors"))
            {
                tools.RestoreTcomp(DetailsTextBox1.Text);
            }
        }
    }
}
