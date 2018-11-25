using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Automation.Library;
using AutomationModernUI.Pages.RunTests;
using FirstFloor.ModernUI.Windows.Controls;
using System.Data.SqlClient;

namespace AutomationModernUI.Pages.ViewResults
{
    /// <summary>
    /// Interaction logic for LastRunPage.xaml
    /// </summary>
    public partial class TestRequirementsPage : UserControl
    {
        public TestRequirementsPage()
        {
            InitializeComponent();
            Loaded += MyWindow_Loaded;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Compatibility.ItemsSource = Query.GetCompatibilityMatrix().DefaultView;
        }
    }
}
