using AlmaDUploader.Models;
using AlmaDUploader.Utils;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Controls;
using FirstFloor.ModernUI.Windows.Navigation;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

namespace AlmaDUploader
{
    /// <summary>
    /// Interaction logic for IngestFiles.xaml
    /// </summary>
    public partial class IngestFiles : UserControl, IContent
    {
        IngestsContext db = App.DBContext;

        int _ingestId;
        Ingest _ingest;

        public IngestFiles()
        {
            InitializeComponent();
        }


        #region Event Handlers

        private async void Files_Drop(object sender, DragEventArgs e)
        {
            progressFiles.IsActive = true;

            var AddFiles = Task.Factory.StartNew(() =>
            {
                string[] fileNames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                foreach (var path in fileNames)
                {
                    FileAttributes attr = File.GetAttributes(path);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                            AddFile(file, path);
                    }
                    else // it's a file
                        AddFile(path, "");
                }
                db.SaveChanges();
            });

            await AddFiles;

            progressFiles.IsActive = false;
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Title = "Select Files";

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                foreach (String file in dlg.FileNames)
                    AddFile(file, "");
                db.SaveChanges();
            }
        }

        private void Files_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;

            // Mark the event as handled, so TextBox's native DragOver handler is not called.
            e.Handled = true;
        }

        private async void DeleteFiles_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxButton btn = MessageBoxButton.YesNo;
            var result = ModernDialog.ShowMessage("Are sure you wish to delete these files?", "", btn);

            if (result.ToString() != "Yes")
                return;

            progressFiles.IsActive = true;

            var deteleFilesTasks = dgFiles.SelectedItems.Cast<IngestFile>().ToList().Select(
                f => f.DeleteFromStorage());
            await Task.WhenAll(deteleFilesTasks);

            // Remove from IngestFiles collection because orphans aren't deleted in EF
            // http://blog.oneunicorn.com/2012/06/02/deleting-orphans-with-entity-framework/
            db.IngestFiles.RemoveRange(dgFiles.SelectedItems.Cast<IngestFile>().ToList());
            db.SaveChanges();

            progressFiles.IsActive = false;

        }

        private void Files_Selected(object sender, SelectionChangedEventArgs e)
        {
            btnDelete.IsEnabled = (dgFiles.SelectedItems.Count > 0);

            // Cancel
            if (dgFiles.SelectedItems.Count == 0)
            {
                btnCancel.IsEnabled = true;
                btnCancel.ToolTip = "Cancel all uploads";
            }
            else
            {
                btnCancel.ToolTip = "Cancel upload";
                btnCancel.IsEnabled = dgFiles.SelectedItems.Cast<IngestFile>().All(
                    i => i.Status == IngestFileStatus.Waiting || i.Status == IngestFileStatus.Uploading);
            }

            // Upload
            if (dgFiles.SelectedItems.Count == 0)
            {
                btnUpload.IsEnabled = true;
                btnUpload.ToolTip = "Upload all files";
            }
            else
            {
                btnUpload.ToolTip = "Upload file";
                btnUpload.IsEnabled = dgFiles.SelectedItems.Cast<IngestFile>().All(
                    i => i.Status == IngestFileStatus.New);
            }

        }

        private void UploadFiles_Click(object sender, RoutedEventArgs e)
        {
            var ingestFiles = (dgFiles.SelectedItems.Count == 0) ?
                dgFiles.Items.Cast<IngestFile>().Where(f => f.Status == IngestFileStatus.New).ToList() :
                dgFiles.SelectedItems.Cast<IngestFile>().ToList();

            ingestFiles.ForEach(i => i.Status = IngestFileStatus.Waiting);
            dgFiles.UnselectAll();
            db.SaveChanges();
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            await _ingest.Submit();
            db.SaveChanges();
        }

        private async void IngestMetaData_Click(object sender, RoutedEventArgs e)
        {
            var form = new MetadataForm();
            form.DataContext = _ingest.Metadata;

            var dlg = new ModernDialog
            {
                Title = "",
                Content = form
            };
            dlg.Buttons = new Button[] { dlg.OkButton };
            dlg.ShowDialog();

            // If submit when ready
            if (Properties.Settings.Default.SubmitWhenReady &&
                _ingest.Status == IngestStatus.Uploaded &&
                _ingest.ReadyForSubmit)
            {
                await _ingest.Submit();
            }
            db.SaveChanges();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var ingestsFiles = (dgFiles.SelectedItems.Count == 0) ?
                dgFiles.Items.Cast<IngestFile>().ToList() :
                dgFiles.SelectedItems.Cast<IngestFile>().ToList();

            ingestsFiles.ForEach(f => f.CancelUpload());
            db.SaveChanges();
        }

        #endregion

        #region Helper Methods

        private void AddFile(string path, string rootDir)
        {
            FileInfo fi = new FileInfo(path);
            
            // Exclude hidden files
            if ((fi.Attributes & FileAttributes.Hidden) != 0)
                return;

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                try { 
                    _ingest.Files.Add(new IngestFile
                    {
                        FileName = Utilities.RelativePath(path, rootDir),
                        FileSize = fi.Length,
                        FileType = fi.Extension.Substring(1),
                        FullPath = path,
                        Status = (Properties.Settings.Default.UploadOnAdd ? IngestFileStatus.Waiting :
                        IngestFileStatus.New)
                    });
                }
                catch (ItemExistsException)
                { }
            });
        }

        #endregion

        #region IContent implementation

        public void OnFragmentNavigation(FragmentNavigationEventArgs e)
        {
            _ingestId = int.Parse(e.Fragment);
            _ingest = db.Ingests.Find(_ingestId);

            this.DataContext = _ingest;
        }

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
        }

        public void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        public void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {

        }
        #endregion

    }
}
