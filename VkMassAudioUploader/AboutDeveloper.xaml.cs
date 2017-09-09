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
using System.Windows.Shapes;

namespace VkMassAudioUploader
{
    /// <summary>
    /// Interaction logic for AboutDeveloper.xaml
    /// </summary>
    public partial class AboutDeveloper : Window
    {
        public AboutDeveloper()
        {
            InitializeComponent();
        }

        private void mail_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("mailto:tripolskypetr@gmail.com");
        }

        private void phone_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("tel:89999830024");
        }
    }
}
