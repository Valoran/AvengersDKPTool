using AvengersDKPTool.Api;
using AvengersDKPTool.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Path = System.IO.Path;

namespace AvengersDKPTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _appRoot = Directory.GetCurrentDirectory();
        private HashSet<EqDkpPlayer> _mains;
        private HashSet<EqDkpPlayer> _allChars;
        private ApiMethods _api;
        public MainWindow()
        {
            InitializeComponent();
            if(File.Exists(Path.Combine(_appRoot, "gamepath.txt")))
            {
                GamePathBox.Text = File.ReadAllText(Path.Combine(_appRoot, "gamepath.txt"));
            }
            if (File.Exists(Path.Combine(_appRoot, "token.txt")))
            {
                ApiToken.Text = File.ReadAllText(Path.Combine(_appRoot, "token.txt"));
                
            }
            _api = new ApiMethods(ApiToken.Text);
            RefreshLists();
        }
        private async void RefreshLists()
        {
            var players = await _api.GetRosterAsync();
            _allChars = players.Select(x=>x.Value).ToHashSet();
            _mains = new HashSet<EqDkpPlayer>();
            foreach (var p in _allChars)
            {
                if(p.Active && p.Id == p.MainId)
                {
                    _mains.Add(p);
                }
            }
            MainsList.ItemsSource = _mains;
            if (await _api.ValidateTokenAsync())
            {
                File.WriteAllText(Path.Combine(_appRoot, "token.txt"), ApiToken.Text);
                ApiKeyValid.Visibility = Visibility.Visible;
                ApiKeyInvalid.Visibility = Visibility.Hidden;
            }
            else
            {
                ApiKeyValid.Visibility = Visibility.Hidden;
                ApiKeyInvalid.Visibility = Visibility.Visible;
            }
        }
        private void GamePathBrowse_Click(object sender, RoutedEventArgs e)
        {
            var ookiiDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (ookiiDialog.ShowDialog() == true)
            {
                
                if (Directory.Exists(ookiiDialog.SelectedPath))
                {
                    File.WriteAllText(Path.Combine(_appRoot, "gamepath.txt"), ookiiDialog.SelectedPath);
                    GamePathBox.Text = ookiiDialog.SelectedPath;
                }
            }
        }

        private void GamePathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(Directory.Exists(GamePathBox.Text))
            {
                if(File.ReadAllText(Path.Combine(_appRoot, "gamepath.txt")) != GamePathBox.Text){
                    File.WriteAllText(Path.Combine(_appRoot, "gamepath.txt"), GamePathBox.Text);
                }
                var raidDumpFiles = Directory.GetFiles(GamePathBox.Text, "RaidRoster*.txt");
                var dateRegex = new Regex(@"\d{8}-\d{6}");
                var logs = new HashSet<RaidLogFileModel>();
                foreach(var file in raidDumpFiles)
                {
                    
                    var date = DateTime.ParseExact(dateRegex.Match(file).Value, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                    logs.Add(new RaidLogFileModel()
                    {
                        Date = date,
                        File = file
                    });
                }
                DumpFilesList.ItemsSource = logs;
                if (Directory.Exists(Path.Combine(GamePathBox.Text, "Logs")))
                {
                    var chatlogFiles = Directory.GetFiles(Path.Combine(GamePathBox.Text, "Logs"), "eqlog*.txt");
                    ChatLogList.ItemsSource = chatlogFiles.Select(x=>x.Replace(GamePathBox.Text, ""));
                }
            }
        }

        private void DumpFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var log = (RaidLogFileModel)DumpFilesList.SelectedItem;
            var content = File.ReadAllLines(log.File);
            var list = new List<EqDkpPlayer>();
            foreach(var s in content)
            {
                var s1 = s.Split("\t", StringSplitOptions.RemoveEmptyEntries);
                var charname = s1[1];

                var player = _allChars.FirstOrDefault(x => x.Name.ToLower() == charname.ToLower());
                if(player == null)
                {
                    player = new EqDkpPlayer()
                    {
                        Name = charname
                    };
                }
                else if(player.Id != player.MainId)
                {
                    player = _allChars.FirstOrDefault(x => x.Id == player.MainId);
                }

                if (!list.Contains(player))
                {
                    list.Add(player);
                }
            }
            AttendeeGrid.ItemsSource = list;
        }

        private void ChatLogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(ChatLogList.SelectedIndex != -1)
            {
                var path = GamePathBox.Text+(string)ChatLogList.SelectedItem;
                if (File.Exists(path))
                {
                    var mainRegex = new Regex("auction category.+?grat", RegexOptions.IgnoreCase);
                    var data = new HashSet<string>();
                    var lines = File.ReadAllLines(path);
                    foreach(var line in lines)
                    {
                        if (mainRegex.IsMatch(line))
                        {
                            data.Add(line);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    LootList.ItemsSource = data;
                }
            }
        }

      

        private async void ApiToken_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(_api != null && !string.IsNullOrEmpty(ApiToken.Text))
            {
                _api.UpdateApiToken(ApiToken.Text);
                if(await _api.ValidateTokenAsync())
                {
                    File.WriteAllText(Path.Combine(_appRoot, "token.txt"), ApiToken.Text);
                    ApiKeyValid.Visibility = Visibility.Visible;
                    ApiKeyInvalid.Visibility = Visibility.Hidden;
                }
                else
                {
                    ApiKeyValid.Visibility = Visibility.Hidden;
                    ApiKeyInvalid.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ApiKeyValid.Visibility = Visibility.Hidden;
                ApiKeyInvalid.Visibility = Visibility.Hidden;
            }
        }

        private void MainsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(MainsList.SelectedIndex != -1)
            {
                var selected = (EqDkpPlayer)MainsList.SelectedItem;

                var alts = _allChars.Where(x => x.MainId == selected.Id && x.Id != selected.Id);
                AltsList.ItemsSource = alts;
            }
        }

        private async void UploadLogBtn_Click(object sender, RoutedEventArgs e)
        {
            var players = (ICollection<EqDkpPlayer>)AttendeeGrid.ItemsSource;
            var selectedLog = (RaidLogFileModel)DumpFilesList.SelectedItem;

            await _api.UploadRaidLog(selectedLog.Date, RaidLogNote.Text, players.Where(x => x.Id > 0).ToHashSet());
        }
    }
}
