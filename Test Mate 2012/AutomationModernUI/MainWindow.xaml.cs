using Automation.Library;
using FirstFloor.ModernUI.Windows.Controls;

namespace AutomationModernUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            LoggerUtil.GetTempPath();
            LoggerUtil.LogMessageToFile("Initiated New Test Session.");
        }
    }
}
