using Automation.Library;
using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for Feedback.xaml
    /// </summary>
    public partial class Feedback : UserControl
    {
        FeedBack feedbackclass = new FeedBack();
        public Feedback()
        {
            InitializeComponent();
            List<string> opinion = new List<string>();
            try
            {
                if (feedbackclass.CheckForFeedback())
                {
                    feedbackComboBox.Visibility = Visibility.Hidden;
                    submitButton.Visibility = Visibility.Hidden;
                    opinionTextBlock.Content = "Thank You For Your Feedback";
                }
                else
                {
                    opinion.Add("Happy");
                    opinion.Add("Unhappy");
                    foreach (string str in opinion)
                    {
                        feedbackComboBox.Items.Add(str);
                    }
                }
            }
            catch (Exception exe)
            {
                ModernDialog.ShowMessage("Please Try Again Later Thank You", "Error", MessageBoxButton.OK, null);
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                feedbackclass.InsertTheFeedback(feedbackComboBox.SelectedValue.ToString() == "Happy" ? true : false);
                feedbackComboBox.Visibility = Visibility.Hidden;
                submitButton.Visibility = Visibility.Hidden;
                opinionTextBlock.Content = "Thank You For Your Feedback";
            }
            catch (Exception exe)
            {
                ModernDialog.ShowMessage("Please Try Again Later Thank You", "Error", MessageBoxButton.OK, null);
            }
        }
    }
}
