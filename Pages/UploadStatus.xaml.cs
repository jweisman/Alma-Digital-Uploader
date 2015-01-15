using FirstFloor.ModernUI.Windows.Controls;
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
    /// Interaction logic for UploadStatus.xaml
    /// </summary>
    public partial class UploadStatus : UserControl
    {
        public UploadStatus()
        {
            InitializeComponent();
        }

        private void UploadStatus_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var form = new SettingsUploadManager();

            var dlg = new ModernDialog
            {
                Title = "",
                Content = form
            };
            dlg.Buttons = new Button[] { dlg.OkButton };
            dlg.ShowDialog();
        }
    }
}
