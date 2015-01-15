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

namespace AlmaDUploader
{
    /// <summary>
    /// Interaction logic for SettingsConfiguration.xaml
    /// </summary>
    public partial class SettingsOptions : UserControl
    {
        public SettingsOptions()
        {
            InitializeComponent();
            this.DataContext = new Models.OptionsViewModel();
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}
namespace AlmaDUploader.Models
{
    public class OptionsViewModel
    {
        public bool IsUploadManagerRunning
        {
            get { return App.UploadManager.IsRunning;  }
            set
            {
                if (value)
                    App.UploadManager.Start();
                else
                    App.UploadManager.Stop();
            }
        }
    }
}
