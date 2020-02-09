using AvengersDKPTool.Models;
using System;
using System.Collections.Generic;
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
        private HashSet<string> _mains;
        private Dictionary<string, string> _alts;
        public MainWindow()
        {
            InitializeComponent();
            if(File.Exists(Path.Combine(_appRoot, "gamepath.txt")))
            {
                GamePathBox.Text = File.ReadAllText(Path.Combine(_appRoot, "gamepath.txt"));
            }
            RefreshLists();
        }
        private void RefreshLists()
        {
            if (File.Exists(Path.Combine(_appRoot, "mains.txt")))
            {
                var list = File.ReadAllLines(Path.Combine(_appRoot, "mains.txt"));
                _mains = list.OrderBy(x=>x.ToUpper()).ToHashSet();
            }
            else
            {
                _mains = new HashSet<string>();
            }
            MainsList.ItemsSource = _mains;

            _alts = new Dictionary<string, string>();
            if (File.Exists(Path.Combine(_appRoot, "alts.txt")))
            {
                var list = File.ReadAllLines(Path.Combine(_appRoot, "alts.txt"));
                foreach (var alt in list)
                {
                    var s = alt.Split("::", StringSplitOptions.RemoveEmptyEntries);
                    _alts.Add(s[1], s[0]);
                }
            }
            AltsList.ItemsSource = _alts;
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
                
                DumpFilesList.ItemsSource = raidDumpFiles.Select(x=>x.Replace(GamePathBox.Text, ""));
                if (Directory.Exists(Path.Combine(GamePathBox.Text, "Logs")))
                {
                    var chatlogFiles = Directory.GetFiles(Path.Combine(GamePathBox.Text, "Logs"), "eqlog*.txt");
                    ChatLogList.ItemsSource = chatlogFiles.Select(x=>x.Replace(GamePathBox.Text, ""));
                }
            }
        }

        private void DumpFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var content = File.ReadAllLines(GamePathBox.Text + (string)DumpFilesList.SelectedItem);
            var list = new List<string>();
            foreach(var s in content)
            {
                var s1 = s.Split("\t", StringSplitOptions.RemoveEmptyEntries);
                var charname = s1[1];
                if (_mains.Contains(charname))
                {
                    if (!list.Contains(charname))
                    {
                        list.Add(charname);
                    }
                }
                else if (_alts.ContainsKey(charname))
                {
                    var mainName = _alts[charname];
                    if (!list.Contains(mainName))
                    {
                        list.Add(mainName);
                    }
                }
                else
                {
                    var newCharWnd = new AddCharWindow(charname, _mains);

                    newCharWnd.Left = this.Left + 150;
                    newCharWnd.Top = this.Top + 50;
                    newCharWnd.ShowDialog();
                    RefreshLists();
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
    }
}
