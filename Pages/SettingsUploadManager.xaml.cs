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

namespace AlmaDUploader.Pages
{
    /// <summary>
    /// Interaction logic for SettingsUploadManager.xaml
    /// </summary>
    public partial class SettingsUploadManager : UserControl
    {
        public SettingsUploadManager()
        {
            InitializeComponent();
        }

        private void cmbWorkers_Changed(object sender, SelectionChangedEventArgs e)
        {
            App.UploadManager.WorkerThreads = (int)cmbWorkers.SelectedValue;
            Properties.Settings.Default.Save();

        }

        private void UploadManager_Click(object sender, RoutedEventArgs e)
        {
            if (App.UploadManager.IsRunning)
                App.UploadManager.Stop();
            else
                App.UploadManager.Start();
        }
    }
}
