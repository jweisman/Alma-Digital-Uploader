using Amazon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Amazon.S3;
using Amazon.S3.Model;
using System.Threading;
using System.ComponentModel;
using Amazon.Runtime;
using System.Collections.Concurrent;
using Amazon.S3.Transfer;
using System.IO;

namespace AlmaDUploader.Models
{
    public class UploadTask
    {
        public Task Task { get; set; }
        public double Speed { get; set; }
    }

    public class UploadManager : INotifyPropertyChanged
    {
        IngestsContext db = App.DBContext;

        private readonly Queue<Task> WaitingTasks = new Queue<Task>();
        private readonly Dictionary<Int64, UploadTask> RunningTasks = new Dictionary<Int64, UploadTask>();
        private ConcurrentDictionary<long, CancellationTokenSource> workerTokens = new ConcurrentDictionary<long, CancellationTokenSource>();
        private CancellationTokenSource primaryTokenSource;
        private int ErrorCounter = 0;
        const int MaxErrors = 5;

        public UploadManager()
        {
            // Set up
            Worker.Done = new Worker.DoneDelegate(WorkerDone);
            Worker.Error = new Worker.ErrorDelegate(WorkerError);
            Worker.ReportProgress = new Worker.ReportProgressDelegate(WorkerReportProgress);
            db.FileChanged += File_Changed;

            Start();
            Init();
        }

        #region Properties

        private int MaxRunningTasks = Properties.Settings.Default.UploadWorkerThreads;
        public int WorkerThreads
        {
            set {
                if (value >= 0 && value <= 10)
                    MaxRunningTasks = value;
            }
        }

        public bool IsRunning { get; set; }

        public int FilesUploading
        {
            get { return RunningTasks.Count; }
        }
        public int FilesWaiting
        {
            get { return WaitingTasks.Count + RunningTasks.Count; }
        }
        public double UploadSpeed
        {
            get { lock (RunningTasks) return RunningTasks.Values.Select(s => s.Speed).Sum(); }
        }

        private void UpdateCounters()
        {
            OnPropertyChanged("FilesUploading");
            OnPropertyChanged("FilesWaiting");
            OnPropertyChanged("UploadSpeed");
        }

        #endregion

        #region Methods

        public void Start()
        {
            if (IsRunning) return;

            ErrorCounter = 0;

            primaryTokenSource = new CancellationTokenSource();
            CancellationToken primaryToken = primaryTokenSource.Token;

            var task = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    while ((WaitingTasks.Count > 0) || (RunningTasks.Count > 0))
                    {
                        if (primaryToken.IsCancellationRequested) return;
                        if (ErrorCounter >= MaxErrors) Stop();

                        // launch tasks when there's room
                        while ((WaitingTasks.Count > 0) && (RunningTasks.Count < MaxRunningTasks))
                        {
                            if (primaryToken.IsCancellationRequested) return;

                            Task uploadTask = WaitingTasks.Dequeue();
                            if (uploadTask.IsCanceled)
                                WorkerCancelled((long)uploadTask.AsyncState);
                            else 
                            {
                                lock (RunningTasks) RunningTasks.Add((Int64)uploadTask.AsyncState, new UploadTask() { Task = uploadTask });
                                UpdateCounters();
                                uploadTask.Start();
                            }
                        }
                    }
                    await Task.Delay(1000);
                }
            }, primaryToken);

            IsRunning = true;
            OnPropertyChanged("IsRunning");
        }

        public void Stop()
        {
            if (!IsRunning) return;

            primaryTokenSource.Cancel();
            IsRunning = false;
            OnPropertyChanged("IsRunning");
        }

        public void CancelUploads(List<long> FileIds)
        {
            FileIds.ForEach(f => CancelUpload(f));
        }

        public void CancelUpload(long FileId)
        {
            CancellationTokenSource source = null;
            if (workerTokens.TryRemove(FileId, out source))
                source.Cancel();
        }

        public async void Init()
        {
            // give everything a chance to start up before loading existing files
            await Task.Delay(30000);

            // Call once to queue up any waiting uploads
            db.IngestFiles.Where(f => f.Status == IngestFileStatus.Waiting
                || f.Status == IngestFileStatus.Uploading).ToList().ForEach(f => EnqueueUpload(f));
        }

        private void EnqueueUpload(IngestFile file)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            workerTokens.GetOrAdd(file.Id, tokenSource);

            WaitingTasks.Enqueue(new Task(id => new Worker().UploadFile(file, token), file.Id, token));
            UpdateCounters();
        }

        private void File_Changed(IngestFile file, EventArgs e)
        {
            if (file.Status == IngestFileStatus.Waiting)
                EnqueueUpload(file);
        }

        #endregion

        #region Callbacks

        void WorkerDone(IngestFile file)
        {
            App.Current.Dispatcher.Invoke(async() =>  
            {
                file.Status = IngestFileStatus.Uploaded;
                // If submit when ready
                if (Properties.Settings.Default.SubmitWhenReady &&
                    file.Ingest.Status == IngestStatus.Uploaded &&
                    file.Ingest.ReadyForSubmit)
                {
                    await file.Ingest.Submit();
                }
                db.SaveChanges();
            });

            CancellationTokenSource source = null;
            workerTokens.TryRemove(file.Id, out source);

            lock (RunningTasks) RunningTasks.Remove(file.Id);
            UpdateCounters();
        }

        private void WorkerReportProgress(IngestFile file)
        {
            RunningTasks[file.Id].Speed = file.Progress.Speed;
            OnPropertyChanged("UploadSpeed");
        }

        void WorkerCancelled(long FileId)
        {
            WorkerError(db.IngestFiles.Find(FileId), new OperationCanceledException());
        }

        void WorkerError(IngestFile file, Exception e)
        {
            // Clean up queues
            CancellationTokenSource source = null;
            workerTokens.TryRemove(file.Id, out source);

            lock (RunningTasks) RunningTasks.Remove(file.Id);

            // File is not found- bail out
            if (e is FileNotFoundException)
            {
                App.Current.Dispatcher.Invoke((Action)delegate()
                {
                    file.Status = IngestFileStatus.Error;
                    file.ErrorMessage = e.Message;
                    db.SaveChanges();
                });
            }

            // Cancelled
            if (e is OperationCanceledException)
            {
                App.Current.Dispatcher.Invoke((Action)delegate()
                {
                    file.Status = IngestFileStatus.New;
                    db.SaveChanges();
                });
            }

            // Something temporary, hopefully. Try again.
            if (e is AmazonServiceException)
            {
                EnqueueUpload(file);
                ErrorCounter++;
            }

            UpdateCounters();
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

    internal class Worker
    {
        public delegate void DoneDelegate(IngestFile file);
        public static DoneDelegate Done { private get; set; }
        public delegate void ErrorDelegate(IngestFile file, Exception e);
        public static ErrorDelegate Error { private get; set; }
        public delegate void ReportProgressDelegate(IngestFile file);
        public static ReportProgressDelegate ReportProgress { private get; set; }
        private static readonly Random Rnd = new Random();
        private IngestFile _file;
        private DateTime _startTime;

        public async void UploadFile(IngestFile file, CancellationToken token)
        {
            _file = file;
            _startTime = DateTime.Now;

            App.Current.Dispatcher.Invoke((Action)delegate()
            {
                file.Status = IngestFileStatus.Uploading;
            });

            IAmazonS3 client = new AmazonS3Client(App.GetAWSCredentials(), S3Utilities.GetConfig());
            try
            {
                TransferUtility s3 = new TransferUtility(client);
                using (FileStream stream = new FileStream(file.FullPath, FileMode.Open, FileAccess.Read))
                {
                    TransferUtilityUploadRequest request = new TransferUtilityUploadRequest()
                    {
                        BucketName = Properties.Settings.Default.StorageBucket,
                        Key = String.Format("{0}/upload/{1}/{2}/{3}",
                            Properties.Settings.Default.InstitutionCode,
                            file.Ingest.MDImportProfile,
                            file.Ingest.Directory,
                            file.FileName.Replace("\\", "/")),
                        InputStream = stream
                    };
                    request.UploadProgressEvent += displayProgress;
                    await s3.UploadAsync(request, token);
                }

                // Upload thumbnail if setting checked
                if (Properties.Settings.Default.UploadThumbnails && file.Thumbnail != null)
                {
                    using (MemoryStream thumbnail = new MemoryStream(file.Thumbnail))
                    { 
                        TransferUtilityUploadRequest request = new TransferUtilityUploadRequest()
                        {
                            BucketName = Properties.Settings.Default.StorageBucket,
                            Key = String.Format("{0}/upload/{1}/{2}/{3}",
                                Properties.Settings.Default.InstitutionCode,
                                file.Ingest.MDImportProfile,
                                file.Ingest.Directory,
                                file.FileName.Replace("\\", "/") + ".thumb"),
                            InputStream = thumbnail
                        };
                        await s3.UploadAsync(request, token);
                    }
                }
                // TODO: BUG-Occasional file remains in 100% complete, Uploading status. Is this not called?
                Done(file);
            }
            catch (Exception e)
            { Error(file, e); }
        }

        private void displayProgress(object sender, UploadProgressArgs args)
        {
            App.Current.Dispatcher.Invoke((Action)delegate()
            {
                _file.Progress = new Progress()
                {
                    Percent = args.PercentDone,
                    Speed = Math.Round((args.TransferredBytes / (DateTime.Now - _startTime).TotalSeconds)/1000),
                    BytesTransferred = args.TransferredBytes
                };
            });
            ReportProgress(_file);
        }
    }
}

