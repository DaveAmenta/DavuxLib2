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
using System.IO;
using System.Reflection;

namespace DavuxLib2.Windows
{
    /// <summary>
    /// Interaction logic for EULAWindow.xaml
    /// </summary>
    public partial class EULAWindow : Window
    {
        public bool Accepted = false;
        public EULAWindow(LicensingMode mode)
        {
            InitializeComponent();

            Closed += (sender, e) => Dispatcher.InvokeShutdown();

            Assembly assembly = Assembly.GetEntryAssembly();

            AssemblyTitleAttribute assemblyTitle = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0] as AssemblyTitleAttribute;

            lblWelcome.Text = assemblyTitle.Title + " " + lblWelcome.Text;

            switch (mode)
            {
                case LicensingMode.Free:
                    txtEULA.Text = GetEULA("Free");
                    break;
                case LicensingMode.PayWithTrial:
                case LicensingMode.Pay:
                    txtEULA.Text = GetEULA("Pay");
                    break;
            }
        }

        private string GetEULA(string eula)
        {
            Stream resStream = Application.GetResourceStream(new Uri("pack://application:,,,/DavuxLib2;component/EULAs/" + eula + ".txt")).Stream;
            StreamReader sr = new StreamReader(resStream);
            string ret = sr.ReadToEnd();

            Assembly assembly = Assembly.GetEntryAssembly();
            
            AssemblyTitleAttribute assemblyTitle = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0] as AssemblyTitleAttribute;
            AssemblyDescriptionAttribute assemblyDescription = assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0] as AssemblyDescriptionAttribute;
            AssemblyCompanyAttribute assemblyCompany = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0] as AssemblyCompanyAttribute;
            AssemblyCopyrightAttribute assemblyCopyright = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0] as AssemblyCopyrightAttribute;
            AssemblyProductAttribute assemblyProduct = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0] as AssemblyProductAttribute;

            string company = assemblyCompany.Company;


            if (company == "Dave Amenta") company = "Dave Amenta Software, LLC";
            //ret = ret.Replace("[SOFTWARE TITLE]", assemblyTitle.Title);
            ret = ret.Replace("[COMPANY_NAME]", company);
            //ret = ret.Replace("[YOUR COUNTRY]", "USA");


            return ret;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            this.Try(() => System.Diagnostics.Process.Start(App.SupportURL));
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            Close();
        }

        private void btnDecline_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            Close();
        }
    }
}
