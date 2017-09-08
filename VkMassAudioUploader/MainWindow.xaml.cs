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
using Microsoft.VisualBasic;
using System.Net;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Net.Http;
using SeasideResearch.LibCurlNet;
using System.Collections.Specialized;

namespace VkMassAudioUploader
{
    public class GroupParams
    {
        public GroupParams(string name,string id)
        {
            this.Name = name;
            this.Id = id;
        }
        public string Name { get; set; }
        public string Id { get; set; }
    }

    public class Bot : INotifyPropertyChanged
    {
        public Bot(UserInfo ui,int Count)
        {
            this.ui = ui;
            this.Count = Count;
        }
        public UserInfo ui=null;
        int count;
        public string Name { get { return ui.Name; } set { ui.Name = value; OnPropertyChanged("Name"); } }
        public string Token { get { return ui.Token; } private set {} }
        public int Count { get { return count; } set { count = value; OnPropertyChanged("Count"); } }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class LogItem : INotifyPropertyChanged
    {
        Color color;
        string file;
        string state;
        public string File { get { return file; } set { file = value; OnPropertyChanged("File"); } }
        public string State { get { return state; } set { state = value; OnPropertyChanged("State"); } }
        public Color Color { get { return color; } set { color = value; OnPropertyChanged("Color"); } }
        
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class MainWindow : Window
    {
        UserInfo admininfo=null;

        ObservableCollection<GroupParams> groups;
        ObservableCollection<Bot> bots;
        ObservableCollection<LogItem> log;
        List<String> musicpath;

        public static Task<string> AccessVk(string parameters,string token)
        {
            return Task.Run(() =>
            {
                string url = "https://api.vk.com/method/" + parameters + "&access_token=" + token + "&v=5.68";
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback((object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) => { return true; });
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                string responseText;
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    responseText = reader.ReadToEnd();
                }
                Interaction.MsgBox(responseText);
                return responseText;
            });
        }

        public MainWindow()
        {
            InitializeComponent();
            groups = new ObservableCollection<GroupParams>();
            bots = new ObservableCollection<Bot>();
            log = new ObservableCollection<LogItem>();
            musicpath = new List<String>();
            ChooseGroupComboBox.ItemsSource = groups;
            BotListView.ItemsSource = bots;
            LogListView.ItemsSource = log;
        }

        private async void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            VkLogin vk = new VkLogin();
            vk.ShowDialog();
            admininfo = vk.GetReturn();

            //Вывести имя
            AdminNameLabel.Content = admininfo.Name;

            //Заполнить список групп
            {
                groups.Clear();
                dynamic dynObj = JsonConvert.DeserializeObject(await AccessVk("groups.get?extended=1&offset=0&count=100&filter=admin",admininfo.Token));
                foreach (var item in dynObj.response.items)
                {

                    //Interaction.MsgBox(item.name+" "+item.id);
                    groups.Add(new GroupParams(item.name.ToString(), item.id.ToString()));

                }
            }


        }

        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            bots.Add(new Bot(admininfo, 0));
        }

        private void RemoveButtonClick(object sender, RoutedEventArgs e)
        {
            var item = BotListView.SelectedItem as Bot;
            if (item != null) bots.Remove(item);
        }

        private void ChooseFolderButtonClick(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string[] files = Directory.GetFiles(fbd.SelectedPath);

                    int count = 0;
                    foreach(string item in files)
                    {
                        if (item.Substring(item.Length - 4).ToLower() == ".mp3")
                        {
                            musicpath.Add(item);
                            count++;
                        }
                    }

                    Interaction.MsgBox("В директории есть " + count + " .mp3");
                }
            }
        }

        private async void StartUploadButtonClick(object sender, RoutedEventArgs e)
        {
            string groupid = (ChooseGroupComboBox.SelectedItem as GroupParams).Id;

            //Работаем с ботами по порядку
            for (int i = 0; i < bots.Count; i++)
            {
                String uploadserver = null;

                dynamic urlObj = JsonConvert.DeserializeObject(await AccessVk("audio.getUploadServer?", bots[i].Token));
                uploadserver = urlObj.response.upload_url;


                String server = null;
                String audio = null;
                String hash = null;
                WebClient wc = new WebClient();
                byte[] responseArray = wc.UploadFile(uploadserver, "post", @"C:\2.mp3");

                dynamic paramsObj = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(responseArray));
                server = paramsObj.server;
                audio = paramsObj.audio;
                hash = paramsObj.hash;

                

                String id = null;
                NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
                queryString["server"] = server;
                queryString["audio"] = audio;
                queryString["hash"] = hash;
                Interaction.MsgBox(queryString.ToString());
                dynamic saveObj = JsonConvert.DeserializeObject(await AccessVk("audio.save?"+ queryString.ToString(), bots[i].Token));
                id = saveObj.response.id;


                dynamic addObj = JsonConvert.DeserializeObject(await AccessVk("audio.add?audio_id=" + id + "&owner_id=" + bots[i].ui.Id + "&group_id=" + groupid, bots[i].Token));


                //Interaction.MsgBox(uploadserver);
            }
 
        }

    }
}
