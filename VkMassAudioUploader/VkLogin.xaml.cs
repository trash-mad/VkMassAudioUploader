using CefSharp;
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
using Microsoft.VisualBasic;
using System.Web;
using Newtonsoft.Json;
using System.IO;

namespace VkMassAudioUploader
{
    public class UserInfo
    {
        public string Name { get; set; }
        public string Token { get; set; }
        public string Id { get; set; }
    }

    public partial class VkLogin : Window
    {
        UserInfo returnvalue = null;

        private async void SetReturnValue(string token)
        {
            UserInfo tmp = new UserInfo();

            dynamic dynObj = JsonConvert.DeserializeObject(await MainWindow.AccessVk("users.get?", token));
            foreach (var item in dynObj.response)
            {
                tmp.Name =  item.first_name + " " + item.last_name;
                tmp.Id = item.id;
            }

            tmp.Token = token;
            returnvalue = tmp;
            Close();
        }

        public UserInfo GetReturn()
        {
            return returnvalue;
        }

        internal string CurrentUrl => Dispatcher.Invoke(new Func<String>(() => { return WebView.Address; }));

        internal string SetReturn
        {
            set { Dispatcher.Invoke(new Action(() => { SetReturnValue(value); })); }
        }


        public VkLogin()
        {
            InitializeComponent();
    
            WebView.LoadingStateChanged += WebView_LoadingStateChanged;

            WebView.Address = "https://oauth.vk.com/authorize?client_id=" + App.AppId + "&display=mobile&scope=offline,audio,groups&redirect_uri=https://oauth.vk.com/blank.html&response_type=token&v=5.68";
            CefSharp.ICookieManager m = Cef.GetGlobalCookieManager();
            if (!m.DeleteCookies("", "")) Interaction.MsgBox("Не стирается");
        }

        private void WebView_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            string tmp = CurrentUrl;
            if (tmp.Contains("access_token"))
            {
                string startsubstring = "blank.html#";
                tmp = tmp.Substring(tmp.IndexOf(startsubstring) + startsubstring.Length);
                SetReturn = HttpUtility.ParseQueryString(tmp)["access_token"];
            }
        }


    }
}
