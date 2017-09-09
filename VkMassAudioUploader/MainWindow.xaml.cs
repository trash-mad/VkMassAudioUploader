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
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;

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

    [Serializable]
    public class BotSaver
    {
        List<SerializableBot> list;

        public BotSaver()
        {
            list = new List<SerializableBot>();
        }

        [Serializable]
        public class SerializableBot
        {
            public UserInfo ui = null;
            public int count = 0;
        }

        public void AddBots(Bot[] arr)
        {
            foreach (var item in arr)
            {
                SerializableBot tmp = new SerializableBot();
                tmp.ui = item.ui;
                tmp.count = item.Count;
                list.Add(tmp);
            }
        }

        public Bot[] GetBots()
        {
            List<Bot> ret = new List<Bot>();
            foreach (var item in list)
            {
                ret.Add(new Bot(item.ui, item.count));
            }
            return ret.ToArray();
        }

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
        public LogItem(string file,string state)
        {
            this.file = file;
            this.state = state;
        }


        string file;
        string state;
        public string File { get { return file; } set { file = value; OnPropertyChanged("File"); } }
        public string State { get { return state; } set { state = value; OnPropertyChanged("State"); } }
 
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class MainWindow : Window
    {
        UserInfo admininfo = null;

        static bool isDebug = false;

        ObservableCollection<GroupParams> groups;
        ObservableCollection<Bot> bots;
        ObservableCollection<LogItem> log;
        List<String> musicpath;

        public static Task<string> AccessVk(string parameters, string token)
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
                if (responseText.IndexOf("error") > 0 && isDebug) Interaction.MsgBox(responseText);
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

            if (admininfo == null)
            {
                Interaction.MsgBox("Нужно войти для продолжения...");
                return;
            }

            //Вывести имя
            AdminNameLabel.Content = admininfo.Name;

            //Заполнить список групп
            {
                groups.Clear();
                dynamic dynObj = JsonConvert.DeserializeObject(await AccessVk("groups.get?extended=1&offset=0&count=100&filter=admin", admininfo.Token));
                foreach (var item in dynObj.response.items)
                {

                    //Interaction.MsgBox(item.name+" "+item.id);
                    groups.Add(new GroupParams(item.name.ToString(), item.id.ToString()));

                }
            }

            StepB.Opacity = 1;


        }

        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            VkLogin vk = new VkLogin();
            vk.ShowDialog();
            UserInfo tmp = vk.GetReturn();
            if (tmp == null)
            {
                Interaction.MsgBox("Нужно войти для продолжения...");
                return;
            }
            bots.Add(new Bot(tmp, 0));
            StepE.Opacity = 1;
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
                    foreach (string item in files)
                    {
                        if (item.Substring(item.Length - 4).ToLower() == ".mp3")
                        {
                            musicpath.Add(item);
                            count++;
                        }
                    }

                    Interaction.MsgBox("В директории есть " + count + " .mp3");
                    StepF.Opacity = 1;
                }
            }
            
        }

        private async void StartUploadButtonClick(object sender, RoutedEventArgs e)
        {
                if (ChooseGroupComboBox.SelectedItem == null)
                {
                    Interaction.MsgBox("Нужно выбрать группу! См. шаг 2");
                    return;
                }

                if (bots.Count == 0)
                {
                    Interaction.MsgBox("Нет ботов. Старт невозможен. См. пункт 4.");
                    return;
                }
                if (musicpath.Count == 0)
                {
                    Interaction.MsgBox("Выберите директорию с музыкой");
                    return;
                 }

            string groupid = (ChooseGroupComboBox.SelectedItem as GroupParams).Id;
                
                

                bool needmorebots = false;
                int botnum = 0;

                //Работаем с ботами по порядку
                for (int i = musicpath.Count() - 1; i >= 0; i--)
                {
                    if (bots[botnum].Count >= 20)
                    {
                        botnum++;
                    }
                    if (botnum > bots.Count - 1)
                    {
                        needmorebots = true;
                        break;
                    }

                    String uploadserver = null;
                    try
                    {
                        dynamic urlObj = JsonConvert.DeserializeObject(await AccessVk("audio.getUploadServer?", bots[botnum].Token));
                        uploadserver = urlObj.response.upload_url;
                    }
                    catch (Exception ex)
                    {
                        if (isDebug) Interaction.MsgBox("В процессе получения ip сервера произошла ошибка. Код: " + ex.Message);
                        log.Add(new LogItem(System.IO.Path.GetFileName(musicpath[i]), "Не выгружен"));
                        continue;
                    }

                    String server = null;
                    String audio = null;
                    String hash = null;
                    try
                    {
                        byte[] responseArray=null;
                        await Task.Run(() =>
                        {
                            WebClient wc = new WebClient();
                            responseArray = wc.UploadFile(uploadserver, "post", musicpath[i]);
                        });

                        dynamic paramsObj = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(responseArray));
                        server = paramsObj.server;
                        audio = paramsObj.audio;
                        hash = paramsObj.hash;
                    }
                    catch (Exception ex)
                    {
                        if(isDebug) Interaction.MsgBox("В процессе выгрузки mp3 на сервер произошла ошибка. Код: " + ex.Message);
                        log.Add(new LogItem(System.IO.Path.GetFileName(musicpath[i]), "Не выгружен"));
                        continue;
                    }

                    String id = null;
                    try
                    {

                        NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
                        queryString["server"] = server;
                        queryString["audio"] = audio;
                        queryString["hash"] = hash;
                        dynamic saveObj = JsonConvert.DeserializeObject(await AccessVk("audio.save?" + queryString.ToString(), bots[botnum].Token));
                        id = saveObj.response.id;
                    }
                    catch (Exception ex)
                    {
                        if (isDebug) Interaction.MsgBox("В процессе добавления в базу данных! Код: " + ex.Message);
                        log.Add(new LogItem(System.IO.Path.GetFileName(musicpath[i]), "Не выгружен"));
                        continue;
                    }

                    string ret = await AccessVk("audio.add?audio_id=" + id + "&owner_id=" + bots[botnum].ui.Id + "&group_id=" + groupid, bots[botnum].Token);
                    if (!(ret.IndexOf("response") > 0))
                    {
                        if (isDebug) Interaction.MsgBox("Файл не выгружен: " + System.IO.Path.GetFileName(musicpath[i]));
                        log.Add(new LogItem(System.IO.Path.GetFileName(musicpath[i]), "Не выгружен"));
                    }
                    else
                    {
                        log.Add(new LogItem(System.IO.Path.GetFileName(musicpath[i]), "Выгружен"));
                        bots[botnum].Count++;
                        musicpath.RemoveAt(i);
                    }

                }

                if (needmorebots == true)
                {
                    Interaction.MsgBox("Вам не хватает ботов. Для выгрузки большего колличества требуется добавить!");
                }

                if (musicpath.Count > 0)
                {
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Не все аудио выложены. Скопировать их в отдельную папку?", "Не все аудио выложены.", System.Windows.MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        string path = new FileInfo(musicpath[0]).Directory.FullName;
                        int fcount = 1;
                        while (true)
                        {
                            if (!Directory.Exists(path + @"\" + "tempdir" + fcount))
                            {
                                System.IO.Directory.CreateDirectory(path +@"\"+ "tempdir" + fcount);
                                path = path + @"\" + "tempdir" + fcount;
                                break;
                            }
                            fcount++;
                        }
                        foreach (var item in musicpath)
                        {
                            try
                            {
                            File.Copy(item, path + @"\" + System.IO.Path.GetFileName(item));
                            }
                            catch(Exception ex)
                            {
                            continue;
                            }
                        }
                    }
                }
        }

        private void DebugChecked(object sender, RoutedEventArgs e)
        {
            if((sender as System.Windows.Controls.CheckBox).IsChecked == true)
            {
                isDebug = true;
            }
            else
            {
                isDebug = false;
            }
        }

 
        private void ChooseGroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StepC.Opacity = 1;
        }

        private void DeveloperMenuItemClick(object sender, RoutedEventArgs e)
        {
            AboutDeveloper ad = new AboutDeveloper();
            ad.ShowDialog();
        }

        private void SaveMenuItemClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "binary files (*.bin)|*.bin";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.ShowDialog();

            string filename = saveFileDialog1.FileName;
            if (!String.IsNullOrEmpty(filename))
            {
                try
                {
                    BotSaver bs = new BotSaver();
                    bs.AddBots(bots.ToArray());

                    BinaryFormatter formatter = new BinaryFormatter();
                    using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
                    {
                        formatter.Serialize(fs, bs);
                        fs.Close();
                    }
                    Interaction.MsgBox("Сохранено в " + filename);
                }
                catch (Exception ex)
                {
                    Interaction.MsgBox("Ошибка: " + ex.Message);
                }
            }
        }

        private void LoadMenuItemClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "binary files (*.bin)|*.bin";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.ShowDialog();

            string filename = openFileDialog1.FileName;
            if (!String.IsNullOrEmpty(filename))
            {
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);

                    BotSaver saver = (BotSaver)formatter.Deserialize(fs);
                    Bot[] arr = saver.GetBots();
                    bots.Clear();
                    foreach (var item in arr)
                    {
                        bots.Add(item);
                    }

                    fs.Close();

                    Interaction.MsgBox("Восстановлено из " + filename);
                }
                catch (Exception ex)
                {
                    Interaction.MsgBox("Произошла ошибка: " + ex.Message);
                }
                StepB.Opacity = 1;
                StepC.Opacity = 1;
                StepD.Opacity = 1;
                StepE.Opacity = 1;
                StepF.Opacity = 1;
            }
        }

        private void ClearCountButtonClick(object sender, RoutedEventArgs e)
        {
            var item = BotListView.SelectedItem as Bot;
            if (item == null)
            {
                Interaction.MsgBox("Кликни на бота!");
                return;
            }
            item.Count = 0;
        }
    }
}
