using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace ImageExplorer
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            cbQuality.SelectedItem = Quality.Fast;
        }

        private void btDumpStats_Click(object sender, RoutedEventArgs e)
        {
            string data = this.explorer.getStats();

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "*.txt|*.txt";


            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, data);
            }
        }

        private void cbQualityChanged(object sender, SelectionChangedEventArgs e)
        {
            explorer.quality = (Quality)cbQuality.SelectedItem;
        }
    }
}
