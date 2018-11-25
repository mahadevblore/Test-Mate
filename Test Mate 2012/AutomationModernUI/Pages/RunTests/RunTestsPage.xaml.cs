using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Automation.Library;
using AutomationModernUI.Pages.Settings;
using FirstFloor.ModernUI.Windows.Controls;
using System.IO;
using FirstFloor.ModernUI.Presentation;
using System.Windows.Navigation;
using Automation.Library;
using Microsoft.TeamFoundation;
using System.ComponentModel;
using System.Diagnostics;
using System.Data.SqlClient;
using Microsoft.Win32;
using System.Net;

namespace AutomationModernUI.Pages.RunTests
{
    /// <summary>
    /// Interaction logic for RunTestsPage.xaml
    /// </summary>
    public partial class RunTestsPage
    {
        public static bool sqlConnection { set; get; }
        public static string BuildDefinition { set; get; }
        public static string TestDlls { set; get; }
        public static string SelectedProduct { set; get; }
        private System.ComponentModel.BackgroundWorker backgroundWorker = new System.ComponentModel.BackgroundWorker();
        public static DateTime starttime = new DateTime();
        public static DateTime endtime = new DateTime();

        struct RunParameters
        {
            public static string moduleToRun;
            public static string dllName;
            public static string category;
            public static List<string> testcase;
            public static string product;
        }

        //Create a Delegate that matches the Signature of the ProgressBar's SetValue method
        private delegate void UpdateProgressBarDelegate(System.Windows.DependencyProperty dp, Object value);

        /// <summary>
        /// Displays progress bar for downloading binaries
        /// </summary>
        private void ProgressBarDisplay()
        {
            ProgressBar1.Visibility = System.Windows.Visibility.Visible;
            //Configure the ProgressBar
            ProgressBar1.Foreground = new SolidColorBrush(AppearanceManager.Current.AccentColor);
            ProgressBar1.Minimum = 0;
            ProgressBar1.Maximum = 150;
            ProgressBar1.Value = 0;
            waitText.Visibility = System.Windows.Visibility.Visible;
            //Stores the value of the ProgressBar
            double value = 0;

            //Create a new instance of our ProgressBar Delegate that points
            //  to the ProgressBar's SetValue method.
            UpdateProgressBarDelegate updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar1.SetValue);

            //Tight Loop:  Loop until the ProgressBar.Value reaches the max
            do
            {
                value += 1;

                /*Update the Value of the ProgressBar:
                  1)  Pass the "updatePbDelegate" delegate that points to the ProgressBar1.SetValue method
                  2)  Set the DispatcherPriority to "Background"
                  3)  Pass an Object() Array containing the property to update (ProgressBar.ValueProperty) and the new value */
                Dispatcher.Invoke(updatePbDelegate,
                    System.Windows.Threading.DispatcherPriority.Background,
                    new object[] { ProgressBar.ValueProperty, value });
            }
            while (ProgressBar1.Value != ProgressBar1.Maximum);
        }

        /// <summary>
        /// Cancels the run by killing MsTest.exe 
        /// </summary>
        /// <returns></returns>
        private string CancelRun()
        {
            MsTestRunner.KillMsTest();
            //Process pKill = Process.GetProcesses().Select(x=>x.);
            return "Cancelled";
        }

        /// <summary> 
        /// Used to simulate a long running function such as database call 
        /// or the iteration of many rows. 
        /// </summary> 
        /// <returns></returns> 
        /// <remarks></remarks> 
        private string SomeLongRunningMethodWPF()
        {
            // don't continue if cancel button clicked 
            if (this.backgroundWorker.CancellationPending)
            {
                return CancelRun();
            }
            return "";
        }

        /// <summary> 
        /// Handles click event for wpfAsynchronousStart button. 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        /// <remarks></remarks> 
        private void WPFAsynchronousStart_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.wpfCount.Text = "";
            this.wpfAsynchronousStart.IsEnabled = false;
            this.wpfAsynchronousCancel.IsEnabled = true;

            // Calls DoWork on secondary thread 
            this.backgroundWorker.RunWorkerAsync();

            // RunWorkerAsync returns immediately, start progress bar 
            wpfProgressBarAndText.Visibility = Visibility.Visible;
        }

        /// <summary> 
        /// Runs on secondary thread. 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        /// <remarks></remarks> 
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // call long running process and get result 
            e.Result = this.RunLogic(ref starttime, ref  endtime);

            DateTime dt = DateTime.Now;
            Process pr = null;
            int i = 0;
            do
            {
                try
                {
                    pr = Process.GetProcesses().Single(x => x.ProcessName.Contains("MSTest"));
                    System.Threading.Thread.Sleep(1500);
                    LoggerUtil.LogMessageToFile("MsTest.exe checking ...");
                    dt = pr.StartTime;
                    LoggerUtil.LogMessageToFile("MSTest.exe start Time : " + dt.ToString());
                    i++;
                }
                catch (Exception ex)
                {
                    LoggerUtil.LogMessageToFile(ex.ToString());
                    System.Threading.Thread.Sleep(1500);
                    i++;
                }
            } while (pr == null && i != 10);

            while (!pr.HasExited)
            {
                LoggerUtil.LogMessageToFile("Exit Status of MsTest.exe is : ? " + pr.HasExited.ToString());
                System.Threading.Thread.Sleep(4000);
            }

            // Cancel if cancel button was clicked. 
            if (this.backgroundWorker.CancellationPending)
            {
                CancelRun();
                e.Cancel = true;
                return;
            }
        }

        /// <summary> 
        /// Method is called everytime backgroundWorker.ReportProgress is called which triggers ProgressChanged event. 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        /// <remarks></remarks> 
        private void backgroundWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            // Update UI with % completed. 
            this.wpfCount.Text = "Executing TestCases. Please Wait..";
        }

        /// <summary> 
        /// Called when DoWork has completed. 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        /// <remarks></remarks> 
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Back on primary thread, can access ui controls 
            wpfProgressBarAndText.Visibility = Visibility.Collapsed;

            if (e.Cancelled)
            {
                CancelRun();
                this.wpfCount.Text = "Execution Cancelled.";
            }
            else
            {
                try
                {
                    DisplayAndUpdateResults(starttime, endtime);
                    this.wpfCount.Text = "Execution completed. ";
                }
                catch (Exception ex)
                {
                    LoggerUtil.LogMessageToFile(ex.ToString());
                    throw new Exception("Could not run the test properly. Try Again.");
                }

                System.Threading.Thread.Sleep(3000);
            }

            this.myStoryboard.Stop(this.lastStackPanel);

            this.wpfAsynchronousStart.IsEnabled = true;

            this.wpfAsynchronousCancel.IsEnabled = false;
        }

        /// <summary> 
        /// Handles click event for cancel button. 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        /// <remarks></remarks> 
        private void WPFAsynchronousCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Cancel the asynchronous operation. 
            this.backgroundWorker.CancelAsync();
            CancelRun();
            // Enable the Start button. 
            this.wpfAsynchronousStart.IsEnabled = true;

            // Disable the Cancel button. 
            this.wpfAsynchronousCancel.IsEnabled = false;
        }

        public RunTestsPage()
        {
            InitializeComponent();
            LoggerUtil.GetTempPath();
            LoggerUtil.LogMessageToFile("Initiated Run Tests Page.");

            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
            this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
            this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);

            this.backgroundWorker.WorkerSupportsCancellation = true;
            this.backgroundWorker.WorkerReportsProgress = true;

            NameScope.SetNameScope(this, new NameScope());
            lastStackPanel.RegisterName("wpfProgressBar", wpfProgressBar);
            RunButton.IsEnabled = false;

            Browse.Content = "Browse" + Environment.NewLine + "Product DLL";

            try
            {
                List<string> list = ProductDetails.GetProductList();
                foreach (string s in list)
                {
                    ProductNameComboBox.Items.Add(s);
                }
            }
            catch (Exception)
            {
                DisplayErrorMessage("Loading Products Failed");
            }
        }

        private void LoadSettingsElements(string type)
        {
            switch (type.ToUpper())
            {
                case "DESKTOP":
                    //ProductExeLocationLable.Visibility = Visibility.Visible;
                    //ProductExeLocationTextBox.Visibility = Visibility.Visible;
                    break;
                case "WEB":
                    //ProductUrlLable.Visibility = Visibility.Visible;
                    //ProductUrlTextBox.Visibility = Visibility.Visible;
                    //ProductBrowserLable.Visibility = Visibility.Visible;
                    //ProductBrowserRadioButtonIe.Visibility = Visibility.Visible;
                    //ProductBrowserRadioButtonChrome.Visibility = Visibility.Visible;
                    //ProductBrowserRadioButtonFireFox.Visibility = Visibility.Visible;
                    break;
                case "API":
                    break;
                case "MOBILE":
                    break;
                case "DB":
                    break;
            }
        }

        private void ProductNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Handling sqlserver connection error
            try
            {
                SqlConnection sCon = SqlServer.GetConnection();
                sCon.Open();
                sCon.Close();
                sqlConnection = true;
            }
            catch(Exception ex)
            {
                LoggerUtil.LogMessageToFile("Application SQL DB ISSUE : " + ex.ToString() + Environment.NewLine + ex.StackTrace.ToString());
                sqlConnection = false;
            }

            if (sqlConnection)
            {
                ProgressBarDisplay();

                try
                {
                    TestDllNameComboBox.Items.Clear();
                    SelectCategoryComboBox.Items.Clear();
                    SelectModuleComboBox.Items.Clear();
                    SelectTestCaseComboBox.Items.Clear();

                    SelectedProduct = ProductNameComboBox.SelectedItem.ToString();
                    if (SelectedProduct.ToUpper() == "MADE2MANAGE")
                    {
                        Browse.Visibility = System.Windows.Visibility.Visible;
                        FileNameTextBox.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        Browse.Visibility = System.Windows.Visibility.Hidden;
                        FileNameTextBox.Visibility = System.Windows.Visibility.Hidden;
                    }
                    Hashtable ht = ProductConfiguration.GetProductConfigurationHashtable(ProductNameComboBox.SelectedItem.ToString());
                    BuildDefinition = ht["BuildDefinition"].ToString();
                    TestDlls = ht["TestDlls"].ToString();
                    string testBuildNumber = null;
                    foreach (string str in ht["Type"].ToString().Split(','))
                    {
                        LoadSettingsElements(str);
                    }
                    try
                    {
                        foreach (string str in BuildDefinition.Split(','))
                        {
                            testBuildNumber = TfsUtils.GetLastSuccededDropLocation(ProductNameComboBox.SelectedItem.ToString(), str);
                            ProductDetails.CopyBuildToLocalMachine(TfsUtils.GetLatestTestBuild(), str);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (exception.ToString().Contains("TeamFoundation"))
                        {
                            //throw exception;
                        }
                        DisplayErrorMessage("Loading Test Builds Failed - " + exception.Message);
                    }
                    if (testBuildNumber != null)
                    {
                        var a = testBuildNumber.Split('\\');
                        testBuildNumber = a[a.Length - 1];
                    }
                    TestBuildNameTextBox.Text = testBuildNumber;

                    foreach (var str in TestDlls.Split(','))
                    {
                        var temp = str.Split('\\');
                        TestDllNameComboBox.Items.Add(temp[temp.Length - 1]);
                    }

                    string cmp = Compatibility.ComparisonLogic(ProductNameComboBox.SelectedValue.ToString());

                    if (cmp != null)
                    {
                        DisplayContentMessage(cmp, "Compatibility");
                    }
                }
                catch (Exception ex)
                {
                    DisplayErrorMessage(ex.ToString());
                    // waitLabel.Visibility = System.Windows.Visibility.Hidden;
                }
                finally
                {
                    InactiveDisplayComponents();
                }
            }
            else
            {
                DisplayErrorMessage("Unable to Connect to Test Server." + Environment.NewLine + "Please Navigate to [Settings] -> [REPORT AN ISSUE] and report this issue.");
            }
        }

        private void InactiveDisplayComponents()
        {
            ProgressBar1.Visibility = System.Windows.Visibility.Hidden;
            waitText.Visibility = System.Windows.Visibility.Hidden;
            //FileNamePresenter.Visibility = System.Windows.Visibility.Hidden;
        }

        private void TestDllNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectCategoryComboBox.Items.Clear();
            SelectModuleComboBox.Items.Clear();
            SelectTestCaseComboBox.Items.Clear();
            ClearGrid();
            try
            {
                if (e.AddedItems.Count > 0)
                {
                    string[] list = ProductDetails.GetCategoryList();
                    foreach (string s in list)
                    {
                        SelectCategoryComboBox.Items.Add(s);
                    }
                }
            }
            catch (Exception exception)
            {
                DisplayErrorMessage("Loading Categories Failed - " + exception.Message);
            }
        }

        private void SelectCategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectModuleComboBox.Items.Clear();
            SelectTestCaseComboBox.Items.Clear();
            ClearGrid();
            try
            {
                Dictionary<string, List<string>> moduleTestCaseDictionary = new Dictionary<string, List<string>>();

                if (e.AddedItems.Count > 0)
                {
                    if (BuildDefinition.Contains(','))
                    {
                        foreach (string bDef in BuildDefinition.Split(','))
                        {
                            try
                            {
                                moduleTestCaseDictionary = ProductDetails.GetModuleTestCaseDictionary(bDef,
                   TestDllNameComboBox.SelectedValue.ToString(), SelectCategoryComboBox.SelectedValue.ToString());
                            }
                            catch
                            {
                                LoggerUtil.LogMessageToFile("Build Definition : " + bDef + " does not contain the Module selected by the user.");
                            }
                        }
                    }
                    else
                    {
                        moduleTestCaseDictionary = ProductDetails.GetModuleTestCaseDictionary(BuildDefinition,
                    TestDllNameComboBox.SelectedValue.ToString(), SelectCategoryComboBox.SelectedValue.ToString());
                    }
                    var modulelist = moduleTestCaseDictionary.Keys.ToList();
                    if (modulelist.Count == 0)
                    {
                        throw new Exception("Selected Dll is not a test Dll");
                    }
                    modulelist.Sort();

                    foreach (var key in modulelist)
                    {
                        SelectModuleComboBox.Items.Add(key);
                    }
                }
            }
            catch (Exception exception)
            {
                DisplayErrorMessage("Loading Modules Failed - " + exception.Message);
            }
            finally
            {
                //ProgressRing.IsActive = false;
            }
        }

        private void SelectModuleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProgressBarDisplay();
            ClearGrid();
            SelectTestCaseComboBox.Items.Clear();

            try
            {
                if (e.AddedItems.Count > 0)
                {
                    var selectedModule = SelectModuleComboBox.SelectedValue.ToString();
                    Dictionary<string, List<string>> moduleTestCaseDictionary = new Dictionary<string, List<string>>();

                    if (BuildDefinition.Contains(','))
                    {
                        foreach (string bDef in BuildDefinition.Split(','))
                        {
                            try
                            {
                                moduleTestCaseDictionary = ProductDetails.GetModuleTestCaseDictionary(bDef,
                   TestDllNameComboBox.SelectedValue.ToString(), SelectCategoryComboBox.SelectedValue.ToString());
                                TotalTestCases.Text = moduleTestCaseDictionary[SelectModuleComboBox.SelectedValue.ToString()].Count.ToString();
                            }
                            catch
                            {
                                LoggerUtil.LogMessageToFile("Build Definition : " + bDef + " does not contain the Module selected by the user.");
                            }
                        }
                    }
                    else
                    {
                        moduleTestCaseDictionary = ProductDetails.GetModuleTestCaseDictionary(BuildDefinition,
                    TestDllNameComboBox.SelectedValue.ToString(), SelectCategoryComboBox.SelectedValue.ToString());
                    }


                    var testCaselist = moduleTestCaseDictionary[selectedModule];

                    testCaselist.Sort();

                    for (int i = 0; i < SelectTestCaseComboBox.Items.Count; i++)
                    {
                        SelectTestCaseComboBox.Items.RemoveAt(i);
                        i--;
                    }

                    SelectTestCaseComboBox.Items.Add("All");

                    foreach (string testCase in testCaselist)
                    {
                        SelectTestCaseComboBox.Items.Add(testCase);
                    }
                }
            }
            catch (Exception exception)
            {
                DisplayErrorMessage("Loading TestCases Failed - " + exception.Message);
            }
            finally
            {
                InactiveDisplayComponents();
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            //ProgressBarDisplay();((System.Windows.Controls.ListBox)(SelectTestCaseComboBox)).SelectedItems

            if (SelectCategoryComboBox.SelectedValue == null || SelectModuleComboBox.SelectedValue == null
                || TestDllNameComboBox.SelectedValue == null || ProductNameComboBox.SelectedValue == null || ((System.Windows.Controls.ListBox)(SelectTestCaseComboBox)).SelectedItems.Count == 0)
            {
                DisplayContentMessage("Please select appropriate values from the comboboxes.\r\nAll comboboxes are mandatory and should contain appropriate values", "Comboboxes Not Selected Properly");
                //InactiveDisplayComponents();
            }
            else if (string.IsNullOrWhiteSpace(ProductVersionTextBox.Text) || ProductVersionTextBox.Text == "0.0.0.0")
            {
                DisplayContentMessage("Please enter the build version properly.\r\nValues cannot be null or 0.0.0.0 ", "Product Build Version");
                //InactiveDisplayComponents();
            }
            else
            {
                try
                {
                    if (ProductVersionTextBox.Text == "0.0.0.0" || string.IsNullOrWhiteSpace(ProductVersionTextBox.Text.ToString()))
                    {
                        DisplayContentMessage("Please enter the build version properly.\r\nValues cannot be null or 0.0.0.0 ", "Product Build Version");
                        //InactiveDisplayComponents();
                    }
                    else
                    {
                        ClearGrid();

                        RunParameters.moduleToRun = (string)SelectModuleComboBox.SelectedValue;
                        RunParameters.category = (string)SelectCategoryComboBox.SelectedValue;
                        RunParameters.testcase = ((System.Windows.Controls.ListBox)(SelectTestCaseComboBox)).SelectedItems.Cast<string>().ToList();
                        if (RunParameters.testcase.Contains("All"))
                        {
                            RunParameters.testcase = SelectTestCaseComboBox.Items.Cast<string>().ToList();
                            RunParameters.testcase.Remove("All");
                        }
                        RunParameters.dllName = (string)TestDllNameComboBox.SelectedValue;
                        RunParameters.product = (string)ProductNameComboBox.SelectedValue;

                        if (RunParameters.moduleToRun == null)
                        { DisplayErrorMessage("Select the module and Test Case"); }

                        this.wpfCount.Text = "";
                        this.wpfAsynchronousStart.IsEnabled = false;
                        this.wpfAsynchronousCancel.IsEnabled = true;

                        // Calls DoWork on secondary thread 
                        this.backgroundWorker.RunWorkerAsync();

                        // RunWorkerAsync returns immediately, start progress bar 
                        wpfProgressBarAndText.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    DisplayErrorMessage(ex.ToString());
                }
                finally
                {
                    //DisplayAndUpdateResults(starttime, endtime);
                    //InactiveDisplayComponents();
                }
            }
        }

        private void ClearGrid()
        {
            CurrentTestResultDataGrid.Visibility = Visibility.Hidden;
            CurrentTestResultDataGrid.ItemsSource = null;
            FileNamePresenter.Visibility = System.Windows.Visibility.Hidden;
        }

        private string RunLogic(ref DateTime starttime, ref DateTime endtime)
        {
            string dlllocation = null;

            if (RunParameters.moduleToRun != null)
            {
                if (BuildDefinition.Contains(','))
                {
                    foreach (string bDef in BuildDefinition.Split(','))
                    {
                        try
                        {
                            string[] dir = Directory.GetFiles(ConfigurationManager.AppSettings["AppDataLocation"] + "TestBuilds\\" + bDef, "*.dll", SearchOption.AllDirectories);
                            dlllocation = dir.First(x => x.Contains(RunParameters.dllName));
                            starttime = DateTime.Now;
                            if (RunParameters.product.ToUpper() == "MADE2MANAGE")
                            {
                                MsTestRunner.RunExtended(RunParameters.dllName,dlllocation, RunParameters.category, RunParameters.moduleToRun, RunParameters.testcase);
                            }
                            else if (RunParameters.product.ToUpper() == "ROSSAPPS" ||
                                RunParameters.product.ToUpper() == "ROSSPLATFORM")
                            {
                                MsTestRunner.RunDirectCall(RunParameters.dllName, dlllocation, RunParameters.category, RunParameters.moduleToRun, RunParameters.testcase);
                            }
                            else
                            {
                                MsTestRunner.Run(RunParameters.dllName,dlllocation, RunParameters.category, RunParameters.moduleToRun, RunParameters.testcase);
                            }
                            break;
                        }
                        catch (CustomException ex)
                        {
                            throw ex;
                        }
                        catch
                        {
                            LoggerUtil.LogMessageToFile("Build Definition : " + bDef + " does not have the test that was selected.");
                        }
                    }
                }
                else
                {
                    string[] dir = Directory.GetFiles(ConfigurationManager.AppSettings["AppDataLocation"] + "TestBuilds\\" + BuildDefinition, "*.dll", SearchOption.AllDirectories);
                    dlllocation = dir.First(x => x.Contains(RunParameters.dllName));
                    starttime = DateTime.Now;
                    if (RunParameters.product.ToUpper() == "MADE2MANAGE")
                    {
                        MsTestRunner.RunExtended(RunParameters.dllName, dlllocation, RunParameters.category, RunParameters.moduleToRun, RunParameters.testcase);
                    }
                    else if (RunParameters.product.ToUpper() == "ROSSAPPS" ||
                        RunParameters.product.ToUpper() == "ROSSPLATFORM")
                    {
                        MsTestRunner.RunDirectCall(RunParameters.dllName, dlllocation, RunParameters.category, RunParameters.moduleToRun, RunParameters.testcase);
                    }
                    else
                    {
                        MsTestRunner.Run(RunParameters.dllName, dlllocation, RunParameters.category, RunParameters.moduleToRun, RunParameters.testcase);
                    }
                }
                endtime = DateTime.Now;
            }
            return "Completed";
        }

        private void DisplayAndUpdateResults(DateTime starttime, DateTime endtime)
        {
            List<MsTestRunner.ShortTrxEntity> results = new List<MsTestRunner.ShortTrxEntity>();

            try
            {
                results = MsTestRunner.GetShortResults();
                CurrentTestResultDataGrid.Visibility = Visibility.Visible;
                CurrentTestResultDataGrid.ItemsSource = results;
                FileNamePresenter.Visibility = System.Windows.Visibility.Visible;
                string str = ConfigurationManager.AppSettings["TrxFileLocation"];
                FileLink.NavigateUri = new System.Uri(MsTestRunner.GetLatestResultspath(str));
                FileLink.RequestNavigate += LinkOnRequestNavigate;
                CurrentTestResultDataGrid.HeadersVisibility = DataGridHeadersVisibility.All;
                CurrentTestResultDataGrid.ColumnHeaderHeight = 30;
                CurrentTestResultDataGrid.BorderThickness = new Thickness(2);
                MsTestRunner.UpdateResults(ProductNameComboBox.SelectedValue.ToString(), ProductVersionTextBox.Text.Trim(), TestBuildNameTextBox.Text, starttime, endtime);
                System.Threading.Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogMessageToFile("Exception caught while getting trx file : " + ex.ToString());
                DisplayErrorMessage("There was some issue while getting the Test Result." +Environment.NewLine+"Either Test Run was cancelled or there was an issue while reading the results file.");
            }
        }

        private void LinkOnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var selectedTheme = new AppearanceViewModel().GetSelectedTheme();
            selectedTheme = ((FirstFloor.ModernUI.Presentation.Displayable)(selectedTheme)).DisplayName;
            if (selectedTheme.ToString() == "light")
            {
                var cb = (ComboBox)sender;
                cb.Foreground = Brushes.Black;
            }
            else
            {
                var cb = (ComboBox)sender;
                cb.Foreground = Brushes.Black;
            }
        }

        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            var selectedTheme = new AppearanceViewModel().GetSelectedTheme();
            selectedTheme = ((FirstFloor.ModernUI.Presentation.Displayable)(selectedTheme)).DisplayName;
            if (selectedTheme.ToString() == "light")
            {
                var cb = (ComboBox)sender;
                cb.Foreground = Brushes.Black;
            }
            else
            {
                var cb = (ComboBox)sender;
                cb.Foreground = Brushes.WhiteSmoke;
            }
        }

        private void ProductVersionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ProductVersionTextBox.Text == "")
            {
                ProductVersionTextBox.Text = "0.0.0.0";
            }
        }

        private void DisplayErrorMessage(string message)
        {
            var errorBox = new ModernMessageBox { Title = "Error", Content = "Error: " + message };
            errorBox.ShowDialog();
        }

        private void DisplayContentMessage(string message, string title)
        {
            var contentBox = new ModernMessageBox { Title = title, Content = message };
            contentBox.ShowDialog();
        }

        private void SelectTestCaseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RunButton.IsEnabled = true;
            CurrentTestResultDataGrid.ItemsSource = null;
            CurrentTestResultDataGrid.Visibility = Visibility.Hidden;
            FileNamePresenter.Visibility = System.Windows.Visibility.Hidden;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Browse_Click_1(object sender, RoutedEventArgs e)
        {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
                openFileDialog.Filter = "All Files(*.dll)|*.dll;";
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == true)
                {
                    FileNameTextBox.Clear();
                    foreach (var file in openFileDialog.FileNames)
                    {
                        string fileName = System.IO.Path.GetFileName(file);
                        FileNameTextBox.Text += fileName + ";" + Environment.NewLine;
                        System.IO.File.Copy(file, @"C:\Automation\AppData\TestBuilds\" + BuildDefinition + "\\" + fileName,true);
                    }
                }
        }
    }
}
