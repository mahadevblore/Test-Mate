using Automation.Library;
using FirstFloor.ModernUI.Windows.Controls;
using System.Windows.Controls;

namespace AutomationModernUI
{
    /// <summary>
    /// Interaction logic for ModernMessageBox.xaml
    /// </summary>
    public partial class ModernMessageBox : ModernDialog
    {
        public ModernMessageBox()
        {
            InitializeComponent();
            // define the dialog buttons
            this.Buttons = new Button[] { this.OkButton};
            //OkButton.Click += OkButtonOnClick;
            
        }

        //private void OkButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
        //{
        //    //OkButton.Content = "ji";
            
        //}

        
        
    }
}
