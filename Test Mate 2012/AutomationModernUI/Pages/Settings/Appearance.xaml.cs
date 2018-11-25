using System.Windows.Controls;
using System.Windows.Media;
using AutomationModernUI.Pages.RunTests;

namespace AutomationModernUI.Pages.Settings
{
    /// <summary>
    /// Interaction logic for Appearance.xaml
    /// </summary>
    public partial class Appearance : UserControl
    {
        public Appearance()
        {
            InitializeComponent();

            // create and assign the appearance view model
            this.DataContext = new AppearanceViewModel();
            
        }
    }
}
