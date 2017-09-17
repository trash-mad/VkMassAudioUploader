using Newtonsoft.Json;
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
    /// Interaction logic for VkCaptcha.xaml
    /// </summary>
    public partial class VkCaptcha : Window
    {
        string captcha_sid = "";
        string captcha_key = "";

        public string GetCaptchaCode()
        {
            return "&captcha_sid=" + captcha_sid + "&captcha_key=" + captcha_key;
        }

        public VkCaptcha(string json)
        {
            InitializeComponent();

            Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                    Console.Beep(700, 250);
            });

            dynamic paramsObj = JsonConvert.DeserializeObject(json);
            captcha_sid = paramsObj.error.captcha_sid.ToString();
            CaptchaImage.Source = new BitmapImage(new Uri(paramsObj.error.captcha_img.ToString()));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            captcha_key = CaptchaText.Text;
            Close();
        }
    }
}
