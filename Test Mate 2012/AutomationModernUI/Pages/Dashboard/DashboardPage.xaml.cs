using Automation.Library;
using AutomationModernUI.Pages.RunTests;
using System.Windows;
using System.Windows.Controls;
namespace AutomationModernUI.Pages.Tools
{
    /// <summary>
    /// Interaction logic for RunTestsPage.xaml
    /// </summary>
    public partial class DashboardPage
    {
        public DashboardPage()
        {
            InitializeComponent();
            Loaded += MyWindow_Loaded;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {            
            if (RunTestsPage.SelectedProduct != null)
            {
                WebBrowser DashboardBrowser = new WebBrowser();
                scrlVwr.Content = DashboardBrowser;
                DashboardBrowser.Navigate(ProductConfiguration.GetProductCentralUrl(RunTestsPage.SelectedProduct));
            }
            else
            {
                scrlVwr.Content = new TextBlock().Text = "Error : Please select a product from RUN TESTS Page";
                DisplayErrorMessage("Please Select the Product from Run Tests Page");
            }
        }

        private void DisplayErrorMessage(string message)
        {
            var errorBox = new ModernMessageBox { Title = "Error", Content = "Error: " + message };
            errorBox.ShowDialog();
        }
    }
}
