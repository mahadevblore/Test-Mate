using System.Windows.Controls;

namespace AutomationModernUI.Pages.ViewResults
{
    /// <summary>
    /// Interaction logic for StatisticsPage.xaml
    /// </summary>
    public partial class StatisticsPage : UserControl
    {
        public StatisticsPage()
        {
            InitializeComponent();
            SplunkWebBrowser.Navigate("<ROI_CALCULATOR_URL_SHAREPOINT>");
        }
    }
}
