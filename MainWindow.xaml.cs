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
using FirstFloor.ModernUI.Windows.Controls;
using AlmaDUploader.Models;

namespace AlmaDUploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Height = Properties.Settings.Default.WindowHeight;
            this.Width = Properties.Settings.Default.WindowWidth;

            if (String.IsNullOrEmpty(Properties.Settings.Default.StorageAccessKey))
                this.ContentSource = new Uri("/Pages/Settings.xaml", UriKind.Relative);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.WindowHeight = this.Height;
            Properties.Settings.Default.WindowWidth = this.Width;
            Properties.Settings.Default.Save();
        }
    }
}
