using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DavuxLib2.Extensions;

namespace DavuxLib2.Windows
{
    public partial class ProductKeyWindow : Window
    {
        
        LicensingMode Mode = LicensingMode.Free;

        bool closing = false;


        public string Key
        {
            get { return (string)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Key.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register("Key", typeof(string), typeof(ProductKeyWindow), new UIPropertyMetadata(""));


        

        public ProductKeyWindow(bool expired, LicensingMode mode)
        {
            this.Mode = mode;
            InitializeComponent();
            Closed += (sender, e) =>
            {
                if (closing)
                    Dispatcher.InvokeShutdown();
                else
                    Environment.Exit(0);
            };
            lblWelcome.Text = "Welcome to " + App.Name;

            if (mode == LicensingMode.Pay || expired)
            {
                rTrial.Visibility = System.Windows.Visibility.Collapsed;
                
            }
            rKey.IsChecked = true;
            DataContext = this;
        }

        private void btnActivate_Click(object sender, RoutedEventArgs e)
        {
            if (rKey.IsChecked == true && string.IsNullOrEmpty(Key))
            {
                MessageBox.Show("You are required to enter a product key to continue.", "Activation", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (rTrial.IsChecked == true) Key = "";
            closing = true;
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(App.SupportURL);
        }
    }
}
