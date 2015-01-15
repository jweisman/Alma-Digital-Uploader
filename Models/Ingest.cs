using AlmaDUploader.Utils;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FirstFloor.ModernUI.Presentation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace AlmaDUploader.Models
{
    public enum IngestStatus { New, Pending, Waiting, Uploading, Uploaded, Submitted, Error };
    public class Ingest : INotifyPropertyChanged
    {
        public Ingest()
        {
            Progress = new Progress() { Percent = 0, Speed = 0 };
            Directory = Guid.NewGuid().ToString();
            Status = IngestStatus.New;
            //MDImportProfile = 1;
            DateAdded = DateTime.Now;
            Metadata = new Metadata();
            Progress = new Progress();

            Files.CollectionChanged +=Files_CollectionChanged;
        }

        #region Properties

        public Int64 Id { get; set; }
        public string Name { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateSubmitted { get; set; }
        public string Directory { get; set; }
        public long MDImportProfile { get; set; }

        private string _errorMessage;
        public string ErrorMessage {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }

        private ObservableHashSet<IngestFile> _files;
        public virtual ObservableHashSet<IngestFile> Files
        {
            get { return _files ?? (_files = new ObservableHashSet<IngestFile>()); }
            set { _files = value; }
        }

        [NotMapped]
        public long FilesSize
        {
            get
            {
                if (Files == null || Files.Count == 0)
                    return 0;
                else
                    return Files.Sum(f => f.FileSize);
            }
        }

        private IngestStatus _status;
        public IngestStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        [NotMapped]
        public Progress Progress { get; set; }

        public virtual Metadata Metadata { get; set; }

        [NotMapped]
        public bool ReadyForSubmit
        {
            get {
                if (Status != IngestStatus.Uploaded)
                    return false;

                string metadata =
                    Metadata.ToMetadataFormat(App.MDImportProfiles[this.MDImportProfile].MDFormat);
                if (metadata != null)
                    return true;

                string mdFileName = App.MDImportProfiles[this.MDImportProfile].MDFileName;
                if (this.Files.Any(f => f.FileName == mdFileName))
                    return true;

                return false;
            }
        }

        #endregion

        #region Action methods

        public async Task Lock()
        {
            await S3Utilities.CreateFile(String.Format("{0}/upload/{1}/{2}/.lock",
                    Properties.Settings.Default.InstitutionCode,
                    this.MDImportProfile,
                    this.Directory));
        }

        public async Task Unlock()
        {
            await S3Utilities.DeleteFile(String.Format("{0}/upload/{1}/{2}/.lock",
                    Properties.Settings.Default.InstitutionCode,
                    this.MDImportProfile,
                    this.Directory));
        }

        public async Task Submit()
        {
            if (Status != IngestStatus.Uploaded)
                return;

            string metadata =
                Metadata.ToMetadataFormat(App.MDImportProfiles[this.MDImportProfile].MDFormat);

            if (metadata != null)
            {
                await S3Utilities.CreateFile(String.Format("{0}/upload/{1}/{2}/{3}",
                        Properties.Settings.Default.InstitutionCode,
                        this.MDImportProfile,
                        this.Directory,
                        App.MDImportProfiles[this.MDImportProfile].MDFileName),
                        metadata);
            }
            else // confirm there is a file with the MD name
            {
                string mdFileName = App.MDImportProfiles[this.MDImportProfile].MDFileName;
                if (!this.Files.Any(f => f.FileName == mdFileName))
                {
                    this.ErrorMessage = String.Format("No file with the name {0} exists. Ingest cannot be submitted for processing.",
                        mdFileName);
                    this.Status = IngestStatus.Error;
                    return;
                }
            }
            await Unlock();
            this.ErrorMessage = null;
            this.Status = IngestStatus.Submitted;
            this.DateSubmitted = DateTime.Now;
        }

        public async Task DeleteFilesFromStorage()
        {
            IAmazonS3 client = new AmazonS3Client(App.GetAWSCredentials(), S3Utilities.GetEndPoint());
            DeleteObjectsRequest request = new DeleteObjectsRequest();
            request.BucketName = Properties.Settings.Default.StorageBucket;
            if (Files.Any(f => f.Status == IngestFileStatus.Uploaded))
            {
                foreach (IngestFile file in Files)
                {
                    if (file.Status == IngestFileStatus.Uploaded)
                        request.AddKey(String.Format("{0}/upload/{1}/{2}/{3}",
                            Properties.Settings.Default.InstitutionCode,
                            this.MDImportProfile,
                            this.Directory,
                            file.FileName.Replace("\\", "/")));
                }
                DeleteObjectsResponse response = await client.DeleteObjectsAsync(request);
            }
        }

        public void CancelUpload()
        {
            if (Status == IngestStatus.Uploading || Status == IngestStatus.Pending || Status == IngestStatus.Waiting)
                foreach (IngestFile file in Files)
                    file.CancelUpload();
        }

        #endregion

        #region Private methods

        private void Files_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (IngestFile item in e.NewItems)
                    item.PropertyChanged += Files_PropertyChanged;

            if (e.OldItems != null)
                foreach (IngestFile item in e.OldItems)
                    item.PropertyChanged -= Files_PropertyChanged;

            UpdateFileProperties();
        }

        private void Files_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Status" || e.PropertyName == "Progress")
            {
                UpdateFileProperties();
            }
        }

        private void UpdateFileProperties()
        {
            SetStatus();
            SetProgress();

            OnPropertyChanged("FilesSize");
            OnPropertyChanged("Status");
            OnPropertyChanged("Progress");
        }

        private void SetStatus()
        {
            if (Status == IngestStatus.Submitted)
                return;

            if (Files.Count == 0 || Files.All(f => f.Status == IngestFileStatus.New))
                Status = IngestStatus.New;
            else if (Files.Any(f => f.Status == IngestFileStatus.Uploading))
                Status = IngestStatus.Uploading;
            else if (Files.All(f => f.Status == IngestFileStatus.Uploaded))
                Status = IngestStatus.Uploaded;
            else if (Files.All(f => f.Status == IngestFileStatus.Waiting))
                Status = IngestStatus.Waiting;
            else if (Files.Any(f => f.Status == IngestFileStatus.New))
                Status = IngestStatus.Pending;
        }

        private void SetProgress()
        {
            long totalBytesTransferred = Files.Sum(f => f.Progress.BytesTransferred);
            long totalBytes = Files.Sum(f => f.FileSize);
            if (totalBytesTransferred == 0)
                Progress.Percent = 0;
            else
                Progress.Percent = (int)(((double)totalBytesTransferred / totalBytes) * 100);
        }


        #endregion

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


    public class Progress
    {
        public int Percent { get; set; }
        public double Speed { get; set; }
        public long BytesTransferred { get; set; }
    }

    [XmlRoot("md_import_profile")]
    public class MDImportProfile
    {
        [XmlElement("id")]
        public long Id { get; set; }
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("collection_name")]
        public string Collection { get; set; }
        [XmlElement("md_file_name")]
        public string MDFileName { get; set; }
        [XmlElement("md_format")]
        public string MDFormat { get; set; }
        public string DisplayName
        {
            get { return String.Format("{0} ({1})", Collection, Name); }
        }
    }

    [XmlRoot("md_import_profiles")]
    public class MDImportProfiles
    {
        public MDImportProfiles()
        {
            Profiles = new List<MDImportProfile>();
        }

        [XmlElement("md_import_profile")]
        public List<MDImportProfile> Profiles { get; set; }
    }
}
