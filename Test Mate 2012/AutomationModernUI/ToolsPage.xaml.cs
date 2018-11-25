using Automation.Library;
namespace AutomationModernUI
{
    /// <summary>
    /// Interaction logic for RunTestsPage.xaml
    /// </summary>
    public partial class ToolsPage
    {
        public ToolsPage()
        {
            InitializeComponent();
            LoggerUtil.GetTempPath();
            LoggerUtil.LogMessageToFile("Initiated Tools Page.");
        }
    }
}
