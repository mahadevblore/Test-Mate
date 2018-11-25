using System.Windows;

namespace AutomationModernUI.UserControlWindows
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
            var bc = new LoadingControl();
            LoadingPanel.Children.Add(bc);
        }
    }
}
