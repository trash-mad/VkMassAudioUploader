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
        ObservableCollection<LogItem> log;
        List<String> musicpath;

        private readonly object botslocker;
        ObservableCollection<Bot> bots;
        public ObservableCollection<Bot> BotAccessor
        {
            get { return bots; }
            set
            {
                bots = value;
                BindingOperations.EnableCollectionSynchronization(bots, botslocker);
            }
        }

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
                if (responseText.IndexOf("error") > 0 && isDebug) Task.Run(() => Interaction.MsgBox(responseText));
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

        private async void UpdateGroups()
        {
            groups.Clear();
            dynamic dynObj = JsonConvert.DeserializeObject(await AccessVk("groups.get?extended=1&offset=0&count=100&filter=admin", admininfo.Token));
            foreach (var item in dynObj.response.items)
            {

                //Interaction.MsgBox(item.name+" "+item.id);
                groups.Add(new GroupParams(item.name.ToString(), item.id.ToString()));

            }
        }

        private void LoginButtonClick(object sender, RoutedEventArgs e)
        {

            var file = System.IO.Path.GetTempPath() + "vklogin.bin";

            try
            {
                if (File.Exists(file))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream fs = new FileStream(file, FileMode.OpenOrCreate);

                    UserInfo tmp = (UserInfo)formatter.Deserialize(fs);
                    fs.Close();
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Сохранен старый токен администратора...", "Есть старый токен администратора. Войти автоматически?", MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        admininfo = tmp;
                        UpdateGroups();
                        AdminNameLabel.Content = admininfo.Name;
                        StepB.Opacity = 1;
                        return;
                    }
                    else
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Interaction.MsgBox("Подгрузка логина невозможна: " + ex.Message);
            }

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

            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream fs = new FileStream(file, FileMode.OpenOrCreate))
                {
                    formatter.Serialize(fs, admininfo);
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                Interaction.MsgBox("Не удалось запомнить токен администратора: " + ex.Message);
            }


            UpdateGroups();
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

                    musicpath.Clear();
                
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

        private string UploadAudioAndReturnIdOrNull(string token, string filename)
        {
            String uploadserver = null;
            try
            {
                dynamic urlObj = JsonConvert.DeserializeObject(AccessVk("audio.getUploadServer?", token).Result);
                uploadserver = urlObj.response.upload_url;
            }
            catch (Exception ex)
            {
                if (isDebug) Task.Run(()=>Interaction.MsgBox("В процессе получения ip сервера произошла ошибка. Код: " + ex.Message));
                logAccess = (new LogItem(System.IO.Path.GetFileName(filename), "Не выгружен"));
                return null;
            }

            String server = null;
            String audio = null;
            String hash = null;
            try
            {
                byte[] responseArray = null;
                WebClient wc = new WebClient();
                responseArray = wc.UploadFile(uploadserver, "post", filename);


                dynamic paramsObj = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(responseArray));
                server = paramsObj.server;
                audio = paramsObj.audio;
                hash = paramsObj.hash;
            }
            catch (Exception ex)
            {
                if (isDebug) Task.Run(() => Interaction.MsgBox("В процессе выгрузки mp3 на сервер произошла ошибка. Код: " + ex.Message));
                logAccess = (new LogItem(System.IO.Path.GetFileName(filename), "Не выгружен"));
                return null;
            }

            String retid = null;
            try
            {

                NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
                queryString["server"] = server;
                queryString["audio"] = audio;
                queryString["hash"] = hash;
                dynamic saveObj = JsonConvert.DeserializeObject(AccessVk("audio.save?" + queryString.ToString(), token).Result);
                retid = saveObj.response.id;
            }
            catch (Exception ex)
            {
                if (isDebug) Task.Run(() => Interaction.MsgBox("В процессе добавления в базу данных! Код: " + ex.Message));
                logAccess=(new LogItem(System.IO.Path.GetFileName(filename), "Не выгружен"));
                return null;
            }
            return retid;
        }


    //Доступ к свойствам из дрогого потока

        internal GroupParams ChooseGroupComboBoxSelectedItem
        {
            set { Dispatcher.Invoke(new Action(() => { ChooseGroupComboBox.SelectedItem = value; })); }
            get
            {
               return Dispatcher.Invoke(new Func<GroupParams>(() => { return ChooseGroupComboBox.SelectedItem as GroupParams; }));
            }
        }

        internal int botsCount
        {
           get
           {
               return bots.Count;
           }
        }

        internal List<string> musicpathAccess
        {
            set { Dispatcher.Invoke(new Action(() => { musicpath = value; })); }
            get
            {
                return Dispatcher.Invoke(new Func<List<string>>(() => { return musicpath; }));
            }
        }

        internal ObservableCollection<Bot> BotAccess
        {
            set { Dispatcher.Invoke(new Action(() => { bots = value; })); }
            get
            {
                return Dispatcher.Invoke(new Func<ObservableCollection<Bot>>(() => { return bots; }));
            }
        }

        internal LogItem logAccess
        {
            set { Dispatcher.Invoke(new Action(() => { log.Add(value); })); }
        }

        internal string OpenDialog
        {
            get
            {
                return Dispatcher.Invoke(new Func<string>(() => {
                    using (var fbd = new FolderBrowserDialog())
                    {
                        DialogResult result = fbd.ShowDialog();
                        return fbd.SelectedPath;
                    }
                }));
            }
        }

        private async void StartUploadButtonClick(object sender, RoutedEventArgs e)
        {

            await Task.Run(async () =>
            {

                List<int> notuploadedindex = new List<int>();

                if (ChooseGroupComboBoxSelectedItem == null)
                {
                    Interaction.MsgBox("Нужно выбрать группу! См. шаг 2");
                    return;
                }

                if (botsCount == 0)
                {
                    Interaction.MsgBox("Нет ботов. Старт невозможен. См. пункт 4.");
                    return;
                }
                if (musicpathAccess.Count == 0)
                {
                    Interaction.MsgBox("Выберите директорию с музыкой");
                    return;
                }

                string groupid = ChooseGroupComboBoxSelectedItem.Id;



                bool needmorebots = false;
                int botnum = 0;

                //Работаем с ботами по порядку
                for (int i = musicpathAccess.Count-1; i >= 0; i--)
                {

                    if (BotAccess[botnum].Count >= 20)
                    {
                        botnum++;
                        i++;
                        continue;
                    }

                    if (botnum > BotAccess.Count - 1)
                    {
                        needmorebots = true;
                        for (int j = i; j >= 0; j--)
                            notuploadedindex.Add(i);
                        break;
                    }



                    String id = UploadAudioAndReturnIdOrNull(BotAccess[botnum].Token, musicpathAccess[i]);
                    if (id == null) continue;

                    string ret = await AccessVk("audio.add?audio_id=" + id + "&owner_id=" + BotAccess[botnum].ui.Id + "&group_id=" + groupid, admininfo.Token);
                    if (!(ret.IndexOf("response") > 0))
                    {
                        if (isDebug) Task.Run(() => Interaction.MsgBox("Файл не выгружен: " + System.IO.Path.GetFileName(musicpathAccess[i])));
                        logAccess=(new LogItem(System.IO.Path.GetFileName(musicpathAccess[i]), "Не выгружен"));
                    }
                    else
                    {
                        logAccess=(new LogItem(System.IO.Path.GetFileName(musicpathAccess[i]), "Выгружен"));
                        BotAccess[botnum].Count++;
                        notuploadedindex.Add(i);
                    }


                }

                if (needmorebots == true)
                {
                    Interaction.MsgBox("Вам не хватает ботов. Для выгрузки большего колличества требуется добавить!");
                }

                if (notuploadedindex.Count > 0)
                {
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Переместить выгруженные в новую папку?", "Аудио выгружены", System.Windows.MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {

                            string result = OpenDialog;
                            if (!string.IsNullOrWhiteSpace(result))
                            {
                                foreach (var item in notuploadedindex)
                                {
                                    try
                                    {
                                    
                                        File.Move(musicpathAccess[item], result + @"\" + System.IO.Path.GetFileName(musicpathAccess[item]));
                                    }
                                    catch (Exception ex)
                                    {
                                        Task.Run(() => Interaction.MsgBox("Файл не выгружен: " + ex.Message));
                                        continue;
                                    }
                                }

                            }
                            else
                            {
                            Interaction.MsgBox("Папка не выбрана, выгруженные будут удалены");
                            foreach (var item in notuploadedindex)
                            {
                                try
                                {
                                    File.Delete(musicpathAccess[item]);
                                }
                                catch (Exception ex)
                                {
                                    Task.Run(() => Interaction.MsgBox("Файл не удален: " + ex.Message));
                                    continue;
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in notuploadedindex)
                        {
                            try
                            {
                                File.Delete(musicpathAccess[item]);
                            }
                            catch (Exception ex)
                            {
                                Task.Run(() => Interaction.MsgBox("Файл не удален: " + ex.Message));
                                continue;
                            }
                        }
                    }
                }

            });
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
