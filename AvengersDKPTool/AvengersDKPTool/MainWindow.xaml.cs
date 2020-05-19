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
        private List<EqDkpRaidListModel> _raids;
        private List<LootGridModel> _items;
        private List<LootGridModel> _currentItems;
        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists(Path.Combine(_appRoot, "gamepath.txt")))
            {
                GamePathBox.Text = File.ReadAllText(Path.Combine(_appRoot, "gamepath.txt"));
            }
            if (File.Exists(Path.Combine(_appRoot, "token.txt")))
            {
                ApiToken.Text = File.ReadAllText(Path.Combine(_appRoot, "token.txt"));

            }
            _api = new ApiMethods(ApiToken.Text);
            ItemLogDate.SelectedDate = DateTime.Now;
            ItemLogDate.SelectedDateFormat = DatePickerFormat.Short;
            _currentItems = new List<LootGridModel>();
            
            RefreshLists();
        }
        private async Task RefreshLists()
        {
            LoadingSpinner.Visibility = Visibility.Visible;
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
            var players = await _api.GetRosterAsync();
            _allChars = players.Select(x => x.Value).ToHashSet();
            _mains = new HashSet<EqDkpPlayer>();
            foreach (var p in _allChars)
            {
                if (p.Active && p.Id == p.MainId)
                {
                    _mains.Add(p);
                }
            }
            MainsList.ItemsSource = _mains;

            await UpdateRaidlogList();
            await UpdateRaidSelect();
            await ParseChatLog();

            if(ApiKeyValid.Visibility == Visibility.Visible)
            {
                LoadingSpinner.Visibility = Visibility.Hidden;
            }
        }
        private async Task UpdateRaidSelect()
        {
            _raids = await _api.GetRaidList();
            RaidSelectBox.ItemsSource = _raids.Where(x => x.Date.Date == DateTime.Now.Date);
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

        private async void GamePathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Directory.Exists(GamePathBox.Text))
            {
                if (File.ReadAllText(Path.Combine(_appRoot, "gamepath.txt")) != GamePathBox.Text)
                {
                    File.WriteAllText(Path.Combine(_appRoot, "gamepath.txt"), GamePathBox.Text);
                }
                await UpdateRaidlogList();
                if (Directory.Exists(Path.Combine(GamePathBox.Text, "Logs")))
                {
                    var chatlogFiles = Directory.GetFiles(Path.Combine(GamePathBox.Text, "Logs"), "eqlog*.txt");
                    ChatLogList.ItemsSource = chatlogFiles.Select(x => x.Replace(GamePathBox.Text, ""));
                }
            }
        }
        private async Task UpdateRaidlogList()
        {
            if (Directory.Exists(GamePathBox.Text))
            {
                var newLogFiles = Directory.GetFiles(GamePathBox.Text, "*RaidRoster*.txt");
                var dateRegex = new Regex(@"\d{8}-\d{6}");
                var newLogs = new HashSet<RaidLogFileModel>();
                foreach (var file in newLogFiles)
                {
                    var date = DateTime.ParseExact(dateRegex.Match(file).Value, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                    newLogs.Add(new RaidLogFileModel()
                    {
                        Date = date,
                        File = file,
                        Parsed = file.Contains("UploadedRaidRoster")
                    });
                }
                DumpFilesList.ItemsSource = newLogs.OrderByDescending(x => x.Date);
            }
        }
        private void DumpFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DumpFilesList.SelectedIndex != -1)
            {
                var log = (RaidLogFileModel)DumpFilesList.SelectedItem;
                if (!string.IsNullOrEmpty(RaidLogNote.Text) && !log.Parsed)
                {
                    UploadLogBtn.IsEnabled = true;
                }
                else
                {
                    UploadLogBtn.IsEnabled = false;
                }
                var content = File.ReadAllLines(log.File);
                var list = new List<EqDkpPlayer>();
                foreach (var s in content)
                {
                    var s1 = s.Split("\t", StringSplitOptions.RemoveEmptyEntries);
                    var charname = s1[1];

                    var player = _allChars.FirstOrDefault(x => x.Name.ToLower() == charname.ToLower());
                    if (player == null)
                    {
                        player = new EqDkpPlayer()
                        {
                            Name = charname
                        };
                    }
                    else if (player.Id != player.MainId)
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
            else
            {
                AttendeeGrid.ItemsSource = new List<EqDkpPlayer>();
                UploadLogBtn.IsEnabled = false;
            }
        }

        private async void ChatLogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ParseChatLog();
        }

        private async Task ParseChatLog()
        {
            if (ChatLogList.SelectedIndex != -1)
            {
                var path = GamePathBox.Text + (string)ChatLogList.SelectedItem;
                if (File.Exists(path))
                {
                    var mainRegex = new Regex("auction category.+?grat", RegexOptions.IgnoreCase);
                    var startRegex = new Regex(@"\[(.*)\].+?auction ?category[\d\, ]*", RegexOptions.IgnoreCase);
                    var charCostRegex = new Regex(@"(\w+)[, ]*(secondary|sec|box|main)*[, ]*(\d+) ?dkp", RegexOptions.IgnoreCase);
                    var cleanupRegex = new Regex(@"[,\. ]+Grat.*", RegexOptions.IgnoreCase);
                    var data = new HashSet<string>();
                    var lines = File.ReadAllLines(path);
                    var dateToShow = (DateTime)ItemLogDate.SelectedDate;
                    var items = new List<LootGridModel>();
                    foreach (var line in lines)
                    {
                        if (mainRegex.IsMatch(line))
                        {
                            try
                            {
                                var parsedLine = line;
                                var dateString = startRegex.Match(parsedLine).Groups[1].Value;
                                var date = DateTime.ParseExact(dateString, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture);
                                
                                parsedLine = startRegex.Replace(parsedLine, "");
                                var matches = charCostRegex.Matches(parsedLine);
                                parsedLine = charCostRegex.Replace(parsedLine, "");
                                foreach (Match match in matches)
                                {
                                    var charname = match.Groups[1].Value;
                                    var type = match.Groups[2].Value;
                                    int amount = 0;
                                    var dkpAmount = match.Groups[3].Value;
                                    int.TryParse(dkpAmount, out amount);
                                    var itemName = cleanupRegex.Replace(parsedLine, "");
                                    var item = new LootGridModel()
                                    {
                                        Cost = amount,
                                        Date = date,
                                        ItemName = itemName,
                                        Charname = charname,
                                        Type = string.IsNullOrEmpty(type) ? "main" : type.ToLower(),
                                        CharnameFound = _mains.Any(x => x.Name.ToLower() == charname.ToLower()),
                                        Upload = false
                                    };
                                    if(item.Type == "secondary" || item.Type == "sec")
                                    {
                                        item.Calculated = Convert.ToInt32(Math.Floor((double)item.Cost / 3));
                                    }
                                    else if (item.Type == "box")
                                    {
                                        item.Calculated = Convert.ToInt32(Math.Floor((double)item.Cost / 6));
                                    }
                                    else
                                    {
                                        item.Calculated = item.Cost;
                                    }
                                    items.Add(item);
                                }
                                data.Add(line);
                            }
                            catch (Exception ex)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    _items = items;
                    _currentItems = _items.Where(x => x.Date.Date == dateToShow.Date).ToList();
                    LootList.ItemsSource = _currentItems;
                }
            }
        }


        private async void ApiToken_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_api != null && !string.IsNullOrEmpty(ApiToken.Text))
            {
                _api.UpdateApiToken(ApiToken.Text);
                if (await _api.ValidateTokenAsync())
                {
                    File.WriteAllText(Path.Combine(_appRoot, "token.txt"), ApiToken.Text);
                    ApiKeyValid.Visibility = Visibility.Visible;
                    ApiKeyInvalid.Visibility = Visibility.Hidden;
                    await RefreshLists();
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
            if (MainsList.SelectedIndex != -1)
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
            LoadingSpinner.Visibility = Visibility.Visible;
            if (await _api.UploadRaidLog(selectedLog.Date, RaidLogNote.Text, players.Where(x => x.Id > 0).ToHashSet()))
            {
                File.Move(selectedLog.File, selectedLog.File.Replace("RaidRoster", "UploadedRaidRoster"));
                await UpdateRaidlogList();
                await UpdateRaidSelect();
                LoadingSpinner.Visibility = Visibility.Hidden;
                MessageBox.Show("Raidlog " + selectedLog.Date.ToString("yyyy-MM-dd HH:mm") + " uploaded!", "Upload Successful", MessageBoxButton.OK);
            }
            else
            {
                LoadingSpinner.Visibility = Visibility.Hidden;
                MessageBox.Show("Raidlog " + selectedLog.Date.ToString("yyyy-MM-dd HH:mm") + " failed to upload.", "Upload Failed", MessageBoxButton.OK);
            }
        }

        private void RaidLogNote_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(RaidLogNote.Text) && DumpFilesList.SelectedIndex != -1 && !((RaidLogFileModel)DumpFilesList.SelectedItem).Parsed)
            {
                UploadLogBtn.IsEnabled = true;
            }
            else
            {
                UploadLogBtn.IsEnabled = false;
            }
        }

        private async void ItemLogDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemLogDate.SelectedDate.HasValue)
            {
                if (_items != null)
                {
                    _currentItems = _items.Where(x => x.Date.Date == ItemLogDate.SelectedDate.Value.Date).ToList();
                    LootList.ItemsSource = _currentItems;
                }
                
                if(_raids != null)
                {
                    RaidSelectBox.SelectedIndex = -1;
                    RaidSelectBox.ItemsSource = _raids.Where(x => x.Date.Date == ItemLogDate.SelectedDate.Value.Date);
                }
            }
        }

        private async void ReloadBtn_Click(object sender, RoutedEventArgs e)
        {
            await RefreshLists();
        }

        private void DateBackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ItemLogDate.SelectedDate.HasValue)
            {
                ItemLogDate.SelectedDate = ItemLogDate.SelectedDate.Value.AddDays(-1);
            }
        }

        private void DateForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ItemLogDate.SelectedDate.HasValue)
            {
                ItemLogDate.SelectedDate = ItemLogDate.SelectedDate.Value.AddDays(1);
            }
        }

        private async void UploadItemsBtn_Click(object sender, RoutedEventArgs e)
        {
            if(RaidSelectBox.SelectedIndex != -1)
            {
                var raid = (EqDkpRaidListModel)RaidSelectBox.SelectedItem;
                var items = _currentItems.Where(x => x.Upload == true).ToList();
                LoadingSpinner.Visibility = Visibility.Visible;
                if (await _api.UploadItems(raid.Id, items, _mains))
                {
                    LoadingSpinner.Visibility = Visibility.Hidden;
                    MessageBox.Show(this,"Items Uploaded", "Success", MessageBoxButton.OK);
                }
                else
                {
                    LoadingSpinner.Visibility = Visibility.Hidden;
                    MessageBox.Show(this, "Upload failed.", "Error", MessageBoxButton.OK);
                }
            }

        }
    }
}
