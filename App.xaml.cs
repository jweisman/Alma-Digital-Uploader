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

        protected static MDImportProfiles _mdImportProfiles;
        public static MDImportProfiles MDImportProfiles
        {
            get { return _mdImportProfiles; }
        }

        private async void App_Startup(object sender, StartupEventArgs e)
        {
            _uploadManager = new UploadManager();
            _mdImportProfiles = new MDImportProfiles();
            await _mdImportProfiles.Load();

            // Ensure the current culture passed into bindings is the OS culture.
            // By default, WPF uses en-US as the culture, regardless of the system settings.
            FrameworkElement.LanguageProperty.OverrideMetadata(
                  typeof(FrameworkElement),
                  new FrameworkPropertyMetadata(
                      XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(AppDispatcherUnhandledException);
        }

        void AppDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)   // In debug mode do not custom-handle the exception, let Visual Studio handle it
                e.Handled = false;
            else
                ShowUnhandeledException(e);
        }

        void ShowUnhandeledException(System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            string errorMessage = string.Format("An application error occurred.\n",
                e.Exception.Message + (e.Exception.InnerException != null ? "\n" +
                e.Exception.InnerException.Message : null));

            MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }
}
