using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Automation.Library;
using AutomationModernUI.Pages.RunTests;
using FirstFloor.ModernUI.Windows.Controls;
using System.Linq;

namespace AutomationModernUI.Pages.ViewResults
{
    /// <summary>
    /// Interaction logic for MyLastRunPage.xaml
    /// </summary>
    public partial class MyLastRunPage : UserControl
    {
        public MyLastRunPage()
        {
            InitializeComponent();

            Loaded += MyWindow_Loaded;

            List<KeyValuePair<string, int>> dateRange = new List<KeyValuePair<string, int>>();
            dateRange.Add(new KeyValuePair<string,int>("Today",0));
            dateRange.Add(new KeyValuePair<string,int>("Last 24 Hrs",1));
            dateRange.Add(new KeyValuePair<string, int>("Last 3 Days", 3));
            dateRange.Add(new KeyValuePair<string, int>("Last 5 Days", 5));
            dateRange.Add(new KeyValuePair<string, int>("Last 10 Days", 10));
            dateRange.Add(new KeyValuePair<string, int>("Last 20 Days", 20));
            DateRangeSelection.ItemsSource = dateRange;
            DateRangeSelection.DisplayMemberPath = "Key";
            DateRangeSelection.SelectedValuePath = "Value";

            UserSelection.ItemsSource = Query.GetRunByUsers(RunTestsPage.SelectedProduct);
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MyBarChart.DataContext = null;
            DateRangeSelection.SelectedIndex = -1;
            UserSelection.SelectedIndex = -1;
            RoiData.Text = "0 hrs";
            UserSelection.ItemsSource = Query.GetRunByUsers(RunTestsPage.SelectedProduct);
        }

        private void ShowChart(int dt)
        {
            try
            {
                if (RunTestsPage.SelectedProduct != null)
                {
                    List<Dictionary<DateTime, double>> combinedList = new List<Dictionary<DateTime, double>>();

                    if (UserSelection.SelectedItem == null || UserSelection.SelectedItem.ToString().Contains("All"))
                    {
                        combinedList = Query.GetLastRunResults(RunTestsPage.SelectedProduct, ((KeyValuePair<string, int>)DateRangeSelection.SelectedItem).Value);
                    }
                    else
                    {
                        combinedList = Query.GetLastRunResults(RunTestsPage.SelectedProduct, ((KeyValuePair<string, int>)DateRangeSelection.SelectedItem).Value, UserSelection.SelectedItem.ToString());
                    }

                    List<KeyValuePair<string, double>> sumPassed = new List<KeyValuePair<string, double>>();
                    List<KeyValuePair<string, double>> sumFailed = new List<KeyValuePair<string, double>>();

                    for (int i = 0; i <= dt; i++)
                    {
                        try
                        {
                            sumPassed.Add(new KeyValuePair<string, double>(DateTime.Now.AddDays(-i).ToString("yy-MM-dd"), combinedList[0].Where(x => x.Key.Date.ToString("yy-MM-dd") == DateTime.Now.AddDays(-i).Date.ToString("yy-MM-dd")).Sum(x => x.Value)));
                        }
                        catch
                        {
                            sumPassed.Add(new KeyValuePair<string, double>(DateTime.Now.AddDays(-i).ToString("yy-MM-dd"), 0));
                        }
                    }

                    for (int j = 0; j <= dt; j++)
                    {
                        try
                        {
                            sumFailed.Add(new KeyValuePair<string, double>(DateTime.Now.AddDays(-j).ToString("yy-MM-dd"), combinedList[1].Where(x => x.Key.Date.ToString("yy-MM-dd") == DateTime.Now.AddDays(-j).Date.ToString("yy-MM-dd")).Sum(x => x.Value)));
                        }
                        catch
                        {
                            sumFailed.Add(new KeyValuePair<string, double>(DateTime.Now.AddDays(-j).ToString("yy-MM-dd"), 0));
                        }
                    }

                    var dataSourceList = new List<List<KeyValuePair<string, double>>>();
                    dataSourceList.Add(sumPassed);
                    dataSourceList.Add(sumFailed);
                    double roiSum = combinedList[2].Sum(x => x.Value);
                    RoiData.Text = roiSum.ToString() + " hrs";
                    MyBarChart.DataContext = dataSourceList;

                    //MyColumnChart.DataContext = myValue;
                }
                else
                {
                    ModernDialog.ShowMessage("Select Product Before proceeding", "Message", MessageBoxButton.OK, null);
                }
            }
            catch (NullReferenceException ex)
            {
                LoggerUtil.LogMessageToFile("Exception caught at My Last Run page is : " + ex.ToString());
            }
            catch (Exception ex)
            {
                LoggerUtil.LogMessageToFile("Exception caught at My Last Run page is : " + ex.ToString());
                ModernDialog.ShowMessage("Error Caught While Retriving the Data." + Environment.NewLine + " Please try again later.", "Message", MessageBoxButton.OK, null);
            }
        }

        private void DateRangeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowChart(Convert.ToInt32(DateRangeSelection.SelectedValue));
        }

        private void UserSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DateRangeSelection.SelectedValue != null)
            {
                ShowChart(Convert.ToInt32(DateRangeSelection.SelectedValue));
            }
            else
            {
                ShowChart(0);
            }
        }

        //private void ScrollViewer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        //{
        //    DateRangeSelection.Items.Clear();
        //    RoiData.Text = "";
        //    MyBarChart.DataContext = null;
        //}

        //private void ScrollViewer_Initialized(object sender, EventArgs e)
        //{
        //    DateRangeSelection.Items.Clear();
        //    RoiData.Text = "";
        //    MyBarChart.DataContext = null;
        //}
    }
}
