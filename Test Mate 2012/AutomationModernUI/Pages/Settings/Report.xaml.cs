using Automation.Library;
using AutomationModernUI.Pages.RunTests;
using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

namespace AutomationModernUI.Pages.Settings
{
    /// <summary>
    /// Interaction logic for Report.xaml
    /// </summary>
    public partial class Report : UserControl
    {
        public Report()
        {
            InitializeComponent();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Comments.Text) || string.IsNullOrWhiteSpace(EmailId.Text) || EmailId.Text.Contains('@') || EmailId.Text.Contains("com") || !EmailId.Text.Contains('.'))
            {
                DisplayContentMessage("Comments and Id are mandatory."+ Environment.NewLine+"Please Enter the values properly."+Environment.NewLine+"ID example : John.Black", "Error");
            }
            else
            {
                try
                {
                    SqlConnection sCon = new SqlConnection();

                    try
                    {
                        sCon = SqlServer.GetConnection();
                        sCon.Open();
                        var command = sCon.CreateCommand();
                        if (RunTestsPage.SelectedProduct == null)
                        {
                            RunTestsPage.SelectedProduct = "None";
                        }
                        command.CommandText =
                            "INSERT INTO issueList (userName, machineName, reportingTime ,operatingSystem,productName,logFilePath,comments)" +
                            "VALUES ('" +
                            Environment.UserDomainName + "\\" + Environment.UserName + "','" +
                            Environment.MachineName + "','" +
                            DateTime.Now.ToString() + "','" +
                            Environment.OSVersion + "','" +
                            RunTestsPage.SelectedProduct + "','" +
                            LoggerUtil.GetTempPath() + "TestMateLogFile.txt" + "','" +
                            Comments.Text.ToString() + "');";
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        LoggerUtil.LogMessageToFile("Exception Caught at Reporting module is : " + ex.ToString());
                    }
                    finally
                    {
                        EmailMethod.SendEmailTemplate(Comments.Text.ToString(),EmailId.Text);
                        DisplayContentMessage("Reported Succefully!", "Report");
                    }
                }
                catch (Exception exe)
                {
                    ModernDialog.ShowMessage("Please Contact Automation Team.", "Error", MessageBoxButton.OK, null);
                }
            }
        }
        private void DisplayContentMessage(string message, string title)
        {
            var contentBox = new ModernMessageBox { Title = title, Content = message };
            contentBox.ShowDialog();
        }
    }
}
