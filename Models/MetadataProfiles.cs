using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace AlmaDUploader.Models
{

    public class MDImportProfiles : INotifyPropertyChanged
    {
        public Dictionary<long, MDImportProfile> Profiles;

        public MDImportProfiles() 
        {
            Profiles = new Dictionary<long, MDImportProfile>();
        }

        public async Task Load()
        {
            // Check for API Key
            if (String.IsNullOrEmpty(AlmaDUploader.Properties.Settings.Default.AlmaAPIKey))
                return;

            // Get collections from Alma
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("apikey",
                        AlmaDUploader.Properties.Settings.Default.AlmaAPIKey);

                try
                {
                    var xml = await client.GetStringAsync(String.Format("{0}/conf/md-import-profiles?type=REPOSITORY&ie_type=DIGITAL",
                        System.Configuration.ConfigurationManager.AppSettings["AlmaApiUrl"]));

                    this.Profiles.Clear();

                    ImportProfiles mdImportProfiles;

                    using (TextReader reader = new StringReader(xml))
                    {

                        XmlSerializer deserializer = new XmlSerializer(typeof(ImportProfiles));
                        mdImportProfiles = (ImportProfiles)deserializer.Deserialize(reader);
                    }

                    mdImportProfiles.Profiles.ForEach(p => this.Profiles.Add(p.Id, p));
                    OnPropertyChanged("Profiles");
                }
                catch (HttpRequestException)
                {
                    // do nothing and just deal with an empty collection list
                }
            }
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion
    }

    [XmlRoot("import_profile")]
    public class MDImportProfile
    {
        [XmlElement("id")]
        public long Id { get; set; }
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("digital_details")]
        public DigitalDetails Digital { get; set; }
        [XmlElement("file_name_patterns")]
        public string MDFileName { get; set; }
        [XmlElement("source_format")]
        public string MDFormat { get; set; }
        public string DisplayName
        {
            get { return String.Format("{0} ({1})", Digital.CollectionName, Name); }
        }
    }

    [XmlRoot("import_profiles")]
    public class ImportProfiles
    {
        public ImportProfiles()
        {
            Profiles = new List<MDImportProfile>();
        }

        [XmlElement("import_profile")]
        public List<MDImportProfile> Profiles { get; set; }
    }

    public class DigitalDetails
    {
        [XmlElement("collection_assignment")]
        public string CollectionName { get; set; }
    }
}
