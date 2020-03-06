using AvengersDKPTool.Api;
using AvengersDKPTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace AvengersDKPTool
{
    /// <summary>
    /// Interaction logic for AddCharWindow.xaml
    /// </summary>
    public partial class AddCharWindow : Window
    {
        private string _charname;
        private ApiMethods _api;
        public AddCharWindow(string charname, HashSet<EqDkpPlayer> mains, ApiMethods api)
        {
            InitializeComponent();
            _charname = charname;
            MainCharSelect.ItemsSource = mains;
            CharnameLabel.Content = _charname;
            _api = api;
        }

        private void AddAsMainBtn_Click(object sender, RoutedEventArgs e)
        {
              
            this.Close();
        }

        private void AddAltBtn_Click(object sender, RoutedEventArgs e)
        {
            if(MainCharSelect.SelectedIndex != -1)
            {
                var mainChar = (EqDkpPlayer)MainCharSelect.SelectedItem;

                //File.AppendAllLines(Path.Combine(Directory.GetCurrentDirectory(), "alts.txt"), new string[] { mainName + "::" + _charname });
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a main");
            }
        }
    }
}
