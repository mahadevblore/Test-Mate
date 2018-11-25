using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Automation.Library;
using AutomationModernUI.Pages.RunTests;
using FirstFloor.ModernUI.Windows.Controls;

namespace AutomationModernUI.Pages.ViewResults
{
    /// <summary>
    /// Interaction logic for LastRunPage.xaml
    /// </summary>
    public partial class LastRunPage : UserControl
    {
        public LastRunPage()
        {
            InitializeComponent();
            Loaded += MyWindow_Loaded;
            //ShowChart();
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ShowChart();
        }

        private void ShowChart()
        {
            if (RunTestsPage.SelectedProduct != null)
            {
                Hashtable lastRundata = MsTestRunner.GetMyLastRunResults(Environment.MachineName);
                TotalTcTextBlock.Text = lastRundata["TotalTC"].ToString();
                PassedTcTextBlock.Text = lastRundata["Passed"].ToString();
                FailedTcTextBlock.Text = lastRundata["Failed"].ToString();
                RoiTextBlock.Text = lastRundata["Roi"].ToString() + " hrs";

                int passed = Convert.ToInt32(lastRundata["Passed"]);
                int failed = Convert.ToInt32(lastRundata["Failed"]);
                var myValue = new List<KeyValuePair<string, int>>
                {
                    new KeyValuePair<string, int>("Passed", passed),
                    new KeyValuePair<string, int>("Failed", failed)
                };
                MyPieChart.DataContext = myValue;
            }
            else
            {
                ModernDialog.ShowMessage("Select Product Before proceeding", "Message", MessageBoxButton.OK, null);
            }
        }
    }
}
