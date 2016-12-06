using AlmaDUploader.Models;
using FirstFloor.ModernUI.Windows.Controls;
using FirstFloor.ModernUI.Windows.Navigation;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
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
using System.ComponentModel;
using FirstFloor.ModernUI.Windows;
using AlmaDUploader.Utils;
using System.Xml;
using System.Xml.Serialization;

namespace AlmaDUploader
{

    /// <summary>
    /// Interaction logic for Ingests.xaml
    /// </summary>
    public partial class Ingests : UserControl, IContent
    {
        IngestsContext db = App.DBContext;

        public bool ViewSubmitted { 
            get { return (Boolean)this.GetValue(ViewSubmittedProperty);}
            set { this.SetValue(ViewSubmittedProperty, value);  }
        }

        public static readonly DependencyProperty ViewSubmittedProperty = DependencyProperty.Register(
            "ViewSubmitted", typeof(Boolean), typeof(Ingests), new PropertyMetadata(false));

        public Ingests()
        {
            InitializeComponent();
            Init();
        }

        private void CollectionListChanged(object sender, PropertyChangedEventArgs e)
        {
            cbCollections.SelectedIndex = 0;
        }

        public void Init()
        {
            ViewSubmitted = false;
            cbCollections.DisplayMemberPath = "DisplayName";
            cbCollections.SelectedValuePath = "Id";
            cbCollections.ItemsSource = App.MDImportProfiles.Profiles.Values;
            App.MDImportProfiles.PropertyChanged += CollectionListChanged;
            cbCollections.SelectedIndex = 0;
            btnAddIngest.IsEnabled = cbCollections.Items.Count > 0;
            btnMdProfileInfo.IsEnabled = cbCollections.Items.Count > 0;
            btnMdProfileTrigger.IsEnabled = cbCollections.Items.Count > 0;
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            Ingest i = e.Item as Ingest;
            if (cbCollections.SelectedValue != null && 
                i.MDImportProfile == (long)cbCollections.SelectedValue)
                e.Accepted = ((ViewSubmitted && i.Status == IngestStatus.Submitted) ||
                    (!ViewSubmitted && i.Status != IngestStatus.Submitted));
            else e.Accepted = false;
        }

        private  void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadIngests();
        }

        #region Event handlers

        private async void AddIngest_Click(object sender, RoutedEventArgs e)
        {
            var form = new AddIngest();

            var dlg = new ModernDialog
            {
                Title = "",
                Content = form
            };
            dlg.Buttons = new Button[] { dlg.OkButton, dlg.CancelButton };
            dlg.ShowDialog();

            if (!dlg.DialogResult.Value)
                return;

            Ingest ingest = new Ingest();
            ingest.Name = form.txtIngestName.Text;
            ingest.MDImportProfile = (long)cbCollections.SelectedValue;

            db.Ingests.Add(ingest);
            await ingest.Lock();
            db.SaveChanges();
        }

        private async void IngestDelete_Click(object sender, RoutedEventArgs e)
        {
            var ingestsList = dgIngests.SelectedItems.Cast<Ingest>().ToList();

            bool purgeIngests;
            if (ingestsList.All(i => i.Status == IngestStatus.Submitted))
                purgeIngests = true;
            else if (ingestsList.All(i => i.Status != IngestStatus.Submitted))
                purgeIngests = false;
            else return; // mixture; bail

            MessageBoxButton btn = MessageBoxButton.YesNo;
            var result = ModernDialog.ShowMessage(
                String.Format("Are sure you wish to {0} these ingests?", purgeIngests ? "purge" : "delete"), "", btn);

            if (result.ToString() != "Yes")
                return;

            pbIngests.IsActive = true;

            // If it's purge submitted ingests, don't touch Amazon
            if (!purgeIngests)
            { 
                var deteleFilesTasks = ingestsList.Select(
                    i => i.DeleteFilesFromStorage());
                await Task.WhenAll(deteleFilesTasks);

                var unlockTasks = ingestsList.Select(
                    i => i.Unlock());
                await Task.WhenAll(unlockTasks);
            }

            db.Ingests.RemoveRange(ingestsList);
            db.SaveChanges();

            pbIngests.IsActive = false;
        }

        private void Ingest_Selected(object sender, SelectionChangedEventArgs e)
        {
            btnIngestMD.IsEnabled = (dgIngests.SelectedItems.Count == 1 && dgIngests.SelectedItems.Cast<Ingest>().First().Status != IngestStatus.Submitted);
            btnSubmit.IsEnabled = (dgIngests.SelectedItems.Count > 0 && dgIngests.SelectedItems.Cast<Ingest>().All(i => i.Status == IngestStatus.Uploaded));

            // Cancel
            if (dgIngests.SelectedItems.Count == 0)
            {
                btnCancel.IsEnabled = true;
                btnCancel.ToolTip = "Cancel all uploads";
            }
            else
            {
                btnCancel.ToolTip = "Cancel upload";
                btnCancel.IsEnabled = dgIngests.SelectedItems.Cast<Ingest>().All(
                    i => i.Status == IngestStatus.Waiting || i.Status == IngestStatus.Uploading);
            }

            // Upload
            if (dgIngests.SelectedItems.Count == 0)
            {
                btnUpload.IsEnabled = true;
                btnUpload.ToolTip = "Upload all ingests";
            }
            else
            {
                btnUpload.ToolTip = "Upload ingest";
                btnUpload.IsEnabled = dgIngests.SelectedItems.Cast<Ingest>().All(
                    i => i.Status == IngestStatus.Pending || i.Status == IngestStatus.New);
            }

            // Delete
            if ((dgIngests.SelectedItems.Count > 0 && dgIngests.SelectedItems.Cast<Ingest>().All(i => i.Status != IngestStatus.Submitted)))
            {
                btnDelete.IsEnabled = true;
                btnDelete.ToolTip = "Delete ingests";
            }
            else if ((dgIngests.SelectedItems.Count > 0 && dgIngests.SelectedItems.Cast<Ingest>().All(i => i.Status == IngestStatus.Submitted)))
            {
                btnDelete.IsEnabled = true;
                btnDelete.ToolTip = "Purge submitted ingests";
            }
            else
                btnDelete.IsEnabled = false;
        }

        private void Ingest_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            Ingest selectedIngest = (Ingest)dgIngests.SelectedItem;
            if (selectedIngest != null)
                NavigationCommands.GoToPage.Execute("/Pages/IngestFiles.xaml#" + selectedIngest.Id,
                this);
        }

        private void Ingest_DragOver(object sender, DragEventArgs e)
        {
            // Perhaps remove this check and only create ingests for the folders
            if (IsFoldersOnly(e) && btnAddIngest.IsEnabled) e.Effects = DragDropEffects.Copy;
            else e.Effects = DragDropEffects.None;

            // Mark the event as handled, so TextBox's native DragOver handler is not called.
            e.Handled = true;
        }

        private async void Ingest_Drop(object sender, DragEventArgs e)
        {
            pbIngests.IsActive = true;
            long mdImportProfileId = (long)cbCollections.SelectedValue;

            var ingests = new List<Task<Ingest>>();

            string[] fileNames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            foreach (var path in fileNames)
            {
                FileAttributes attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    ingests.Add(AddIngest(path, mdImportProfileId));
            }

            await Task.WhenAll(ingests);

            // Get non-null results
            db.Ingests.AddRange(ingests.Where(i => i.Result != null).Select(i => i.Result).ToList());
            db.SaveChanges();
            pbIngests.IsActive = false;
        }

        private async void RefreshCollections_Click(object sender, RoutedEventArgs e)
        {
            await App.MDImportProfiles.Load();
        }

        private async void MDProfileTrigger_Click(object sender, RoutedEventArgs e)
        {
            bool success = await ((MDImportProfile)cbCollections.SelectedItem).Trigger();
            if (success)
            {
                MessageBox.Show("MD Import job successfully triggered.", "Trigger MD Import Job", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Error when triggering MD Import job.", "Trigger MD Import Job", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void Collection_Changed(object sender, SelectionChangedEventArgs e)
        {
            btnAddIngest.IsEnabled = cbCollections.Items.Count > 0;
            btnMdProfileInfo.IsEnabled = cbCollections.Items.Count > 0;
            btnMdProfileTrigger.IsEnabled = cbCollections.Items.Count > 0;
            RefreshIngestList();
        }

        private async void IngestMetaData_Click(object sender, RoutedEventArgs e)
        {
            var form = new MetadataForm();
            Ingest selectedIngest = (Ingest)dgIngests.SelectedItem;
            form.DataContext = selectedIngest.Metadata;

            var dlg = new ModernDialog
            {
                Title = "",
                Content = form
            };
            dlg.Buttons = new Button[] { dlg.OkButton };
            dlg.ShowDialog();

            // If submit when ready
            if (Properties.Settings.Default.SubmitWhenReady &&
                selectedIngest.Status == IngestStatus.Uploaded &&
                selectedIngest.ReadyForSubmit)
            {
                await selectedIngest.Submit();
            }

            db.SaveChanges();
        }

        private void IngestUpload_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Make this ForEach
            var ingests = (dgIngests.SelectedItems.Count == 0) ?
                dgIngests.Items.Cast<Ingest>().Where(i => i.Status == IngestStatus.Pending || i.Status == IngestStatus.New).ToList() :
                dgIngests.SelectedItems.Cast<Ingest>().ToList();

            foreach (var ingest in ingests)
            {
                foreach (var file in ingest.Files.Where(f => f.Status == IngestFileStatus.New))
                    file.Status = IngestFileStatus.Waiting;
            }
            dgIngests.UnselectAll();
            db.SaveChanges();
        }

        private async void IngestSubmit_Click(object sender, RoutedEventArgs e)
        {
            pbIngests.IsActive = true;
            var tasks = dgIngests.SelectedItems.Cast<Ingest>().ToList().Select(
                i => i.Submit());
            await Task.WhenAll(tasks);
            db.SaveChanges();
            pbIngests.IsActive = false;
        }

        private void MDProfileInfo_Click(object sender, RoutedEventArgs e)
        {
            var form = new MDImportProfileDetails();
            MDImportProfile mdProfile = (MDImportProfile)cbCollections.SelectedItem;
            form.DataContext = mdProfile;

            var dlg = new ModernDialog
            {
                Title = "",
                Content = form
            };
            dlg.Buttons = new Button[] { dlg.OkButton };
            dlg.ShowDialog();
        }

        private void CancelUpload_Click(object sender, RoutedEventArgs e)
        {
            // If nothing is selected, cancel all
            var ingests = (dgIngests.SelectedItems.Count == 0) ?
                dgIngests.Items.Cast<Ingest>().ToList() :
                dgIngests.SelectedItems.Cast<Ingest>().ToList();

            ingests.ForEach(i => i.CancelUpload());
            db.SaveChanges();
        }

        #endregion

        #region Helpers

        private async void LoadIngests()
        {
            pbIngests.IsActive = true;

            var LoadIngests = Task.Factory.StartNew(() =>
            {
                db.Ingests.OrderBy(i => i.DateAdded).Load();
                App.Current.Dispatcher.Invoke((Action)delegate()
                {
                    CollectionViewSource cvs = (CollectionViewSource)FindResource("IngestsView");
                    cvs.Source = db.Ingests.Local;
                });
            });

            await LoadIngests;

            dgIngests.UnselectAll();
            pbIngests.IsActive = false;
        }

        private void RefreshIngestList()
        {
            long i;
            if (cbCollections.SelectedValue != null && long.TryParse(cbCollections.SelectedValue.ToString(), out i))
            { 
                CollectionViewSource csv = (CollectionViewSource)FindResource("IngestsView");
                if (csv != null && csv.View != null)
                    csv.View.Refresh();
            }
        }

        private bool IsFoldersOnly(DragEventArgs args)
        {
            // Check for files in the hovering data object.
            if (args.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] fileNames = args.Data.GetData(DataFormats.FileDrop, true) as string[];
                // Check for folders only
                foreach (var path in fileNames)
                {
                    FileAttributes attr = File.GetAttributes(path);
                    if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                        return false;
                }
            }
            return true;
        }

        async Task<Ingest> AddIngest(string path, long mdImportProfileId)
        {
            Ingest ingest = new Ingest();
            ingest.Name = System.IO.Path.GetFileName(path);
            ingest.MDImportProfile = mdImportProfileId;

            // If we're not connected, show an error
            try
            {
                await ingest.Lock();
            }
            catch (Amazon.Runtime.AmazonServiceException)
            {
                MessageBox.Show("It appears you're not connected to the Internet. Your ingest cannot be added at this time.",
                    "Cannot add ingest", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            // loop through files
            await Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    FileInfo fi = new FileInfo(file);
                    if ((fi.Attributes & FileAttributes.Hidden) == 0) // Exclude hidden files
                    {
                        ingest.Files.Add(new IngestFile
                        {
                            FileName = Utilities.RelativePath(file, path),
                            FileSize = fi.Length,
                            FileType = (String.IsNullOrEmpty(fi.Extension)) ? "" : fi.Extension.Substring(1),
                            FullPath = file,
                            Status = (Properties.Settings.Default.UploadOnAdd ? IngestFileStatus.Waiting :
                                IngestFileStatus.New)
                        });
                    }
                }
            });

            return ingest;
        }

        #endregion

        #region IContent implementation

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
        }

        public void OnNavigatedTo(NavigationEventArgs e)
        {

        }

        public void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {

        }

        public void OnFragmentNavigation(FragmentNavigationEventArgs e)
        {
            ViewSubmitted = (e.Fragment != null && e.Fragment.IndexOf("submitted") >= 0);
            RefreshIngestList();
        }
        #endregion

    }
}
