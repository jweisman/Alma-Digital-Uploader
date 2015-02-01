using AlmaDUploader.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Serialization;

namespace AlmaDUploader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected static IngestsContext _db = new IngestsContext();
        public static IngestsContext DBContext
        {
            get
            {
                try
                {
                    return _db;
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        MessageBox.Show(ex.InnerException.Message);
                    else
                        MessageBox.Show(ex.Message);
                    throw ex;
                }
            }
        }

        protected static Amazon.Runtime.AWSCredentials awsCredentials;
        public static Amazon.Runtime.AWSCredentials GetAWSCredentials()
        {
            if (!String.IsNullOrEmpty(AlmaDUploader.Properties.Settings.Default.StorageAccessKey))
                return awsCredentials ??
                    (awsCredentials = new Amazon.Runtime.StoredProfileAWSCredentials("AlmaDUploader"));
            else return null;
        }

        protected static UploadManager _uploadManager;
        public static UploadManager UploadManager
        {
            get { return _uploadManager; }
        }

        protected static Dictionary<long, MDImportProfile> _mdImportProfiles = new Dictionary<long, MDImportProfile>();
        public static Dictionary<long, MDImportProfile> MDImportProfiles
        {
            get { return _mdImportProfiles; }
        }

        public static void LoadCollections()
        {
            // TOTO: Make load asynchornous when using REST
            _mdImportProfiles.Clear();

            MDImportProfiles mdImportProfiles;

            using (FileStream stream = new FileStream(Utils.Utilities.GetDataDirectory() + @"\MDImportProfiles.xml", FileMode.Open))
            {
                XmlReader reader = new XmlTextReader(stream);

                XmlSerializer deserializer = new XmlSerializer(typeof(MDImportProfiles));
                mdImportProfiles = (MDImportProfiles)deserializer.Deserialize(reader);
            }

            mdImportProfiles.Profiles.ForEach(p => _mdImportProfiles.Add(p.Id, p));
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            _uploadManager = new UploadManager();

            // Ensure the current culture passed into bindings is the OS culture.
            // By default, WPF uses en-US as the culture, regardless of the system settings.
            FrameworkElement.LanguageProperty.OverrideMetadata(
                  typeof(FrameworkElement),
                  new FrameworkPropertyMetadata(
                      XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }
    }
}
