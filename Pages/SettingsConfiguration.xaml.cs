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
using System.Windows.Shapes;
using Amazon.S3.Transfer;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Navigation;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

namespace AlmaDUploader
{
    /// <summary>
    /// Interaction logic for SettingsConfiguration.xaml
    /// </summary>
    public partial class SettingsConfiguration : UserControl, IContent
    {
        private bool IsValid = false;
        const string CRLF = "&#13;&#10;";

        public SettingsConfiguration()
        {
            InitializeComponent();
            if (!String.IsNullOrEmpty(Properties.Settings.Default.StorageAccessKey))
                IsValid = true;
        }

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            await TestSettings();

            if (IsValid)
            { 
                Properties.Settings.Default.Save();
                Amazon.Util.ProfileManager.RegisterProfile("AlmaDUploader",
                    Properties.Settings.Default.StorageAccessKey,
                    Properties.Settings.Default.StorageAccessSecret);
            }
        }

        private void Settings_Changed(object sender, TextChangedEventArgs e)
        {
            IsValid = false;
        }

        private async Task TestSettings()
        {
            bbTestResults.Visibility = Visibility.Visible;
            bbTestResults.BBCode = "Testing configuration..." + Environment.NewLine;
            bbTestResults.BBCode += "Writing a file to storage...   ";
            // try to write a file
            IAmazonS3 client = new AmazonS3Client(txtAccessKey.Text, txtAccessSecret.Text, Amazon.RegionEndpoint.USEast1);
            try { 
                PutObjectRequest request = new PutObjectRequest()
                    {
                        BucketName = cmbBucket.SelectedValue.ToString(),
                        Key = String.Format("{0}/upload/.test",
                            txtInst.Text)
                    };
                PutObjectResponse response = await client.PutObjectAsync(request);
                bbTestResults.BBCode += "   [color=green]Success![/color]";
                IsValid = true;
            }
            catch (Exception e)
            {
                bbTestResults.BBCode += @"[color=red]Failed: " + Environment.NewLine +
                    e.Message + "[/color]";
                IsValid = false;
            }
        }

        #region IContent implementation

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
        }

        public void OnNavigatedTo(NavigationEventArgs e)
        {

        }

        public void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (!IsValid)
                TestSettings();

            if (!IsValid)
                e.Cancel = true;

        }

        public void OnFragmentNavigation(FragmentNavigationEventArgs e)
        {

        }
        #endregion

    }
}
