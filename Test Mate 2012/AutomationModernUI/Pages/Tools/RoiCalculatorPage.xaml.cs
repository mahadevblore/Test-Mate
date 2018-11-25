namespace AutomationModernUI.Pages.Tools
{
    /// <summary>
    /// Interaction logic for RunTestsPage.xaml
    /// </summary>
    public partial class RoiCalculatorPage
    {
        public RoiCalculatorPage()
        {
            InitializeComponent();
            RoiCalculatorBrowser.Navigate("<ROI_CALCULATOR_URL_SHAREPOINT>");
        }
    }
}
