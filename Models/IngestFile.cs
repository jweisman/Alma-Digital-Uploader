using AlmaDUploader.Utils;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlmaDUploader.Models
{

    public enum IngestFileStatus { New, Waiting, Uploading, Uploaded, Error };
    public class IngestFile : INotifyPropertyChanged
    {

        public IngestFile()
        {
            Progress = new Progress();
        }

        #region Properties

        public Int64 Id { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }
        public string FullPath { get; set; }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }

        private IngestFileStatus _status;
        public IngestFileStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
                if (value == IngestFileStatus.Uploaded)
                { Progress.Percent = 100; Progress.BytesTransferred = FileSize; }
                else if (value == IngestFileStatus.New) // File upload cancelled
                { Progress.Percent = 0; Progress.BytesTransferred = 0; }
            }
        }

        private byte[] _thumbnail;
        [NotMapped]
        public byte[] Thumbnail
        {
            get
            {
                if (_thumbnail == null)
                {
                    // best effort
                    try { _thumbnail = Utilities.GetThumbnail(FullPath); }
                    catch { }
                }
                return _thumbnail;
            }
        }

        public virtual Ingest Ingest { get; set; }
        
        private Progress _progress;
        [NotMapped]
        public Progress Progress
        {
            get { return _progress; }
            set { 
                _progress = value; 
                OnPropertyChanged("Progress");
            }
        }

        public override string ToString()
        {
            return FullPath;
        }

        #endregion

        #region Action methods

        public async Task DeleteFromStorage()
        {
            // In case file is being uploaded, cancel
            CancelUpload();

            if (Status != IngestFileStatus.Uploaded)
                return;

            await S3Utilities.DeleteFile(String.Format("{0}/upload/{1}/{2}/{3}",
                    Properties.Settings.Default.InstitutionCode,
                    Ingest.MDImportProfile,
                    Ingest.Directory,
                    this.FileName.Replace("\\", "/")));
        }

        public void CancelUpload()
        {
            if (Status == IngestFileStatus.Uploading || Status == IngestFileStatus.Waiting)
                App.UploadManager.CancelUpload(Id);
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
}
