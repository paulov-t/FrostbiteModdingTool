using FrostbiteSdk.FrostbiteSdk.Managers;
using FrostySdk.Managers;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace FrostbiteModdingUI.Windows
{
    /// <summary>
    /// Interaction logic for FrostbiteModEmbeddedFiles.xaml
    /// </summary>
    public partial class FrostbiteModEmbeddedFiles : Window
    {
        //public IEnumerable<EmbeddedFileEntry> EmbeddedFiles
        //{
        //    get
        //    {
        //        return new ObservableCollection<EmbeddedFileEntry>().AddRange(
        //            AssetManager.Instance.EmbeddedFileEntries
        //            .Where(x=> x != null)
        //            .OrderBy(x=>x.Name)
        //            );
        //    }
        //}

        private EmbeddedFileEntry embeddedFileEntry;

        public EmbeddedFileEntry SelectedFileEntry
        {
            get { return embeddedFileEntry; }
            set
            {

                if (embeddedFileEntry != value)
                {
                    embeddedFileEntry = value;


                    AssetManager.Instance.AddEmbeddedFile(value);
                    DataContext = null;
                    DataContext = this;
                }

            }
        }


        public FrostbiteModEmbeddedFiles()
        {
            InitializeComponent();
            Loaded += FrostbiteModEmbeddedFiles_Loaded;
            DataContext = this;
        }

        private void FrostbiteModEmbeddedFiles_Loaded(object sender, RoutedEventArgs e)
        {
            //lstFiles.SelectionChanged += LstFiles_SelectionChanged;
        }

        private void LstFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var entry = lstFiles.SelectedItem as EmbeddedFileEntry;
            if (entry != null)
            {
                SelectedFileEntry = entry;
                //DataContext = null;
                //DataContext = this;
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstFiles.SelectedItem == null)
                return;

            AssetManager.Instance.EmbeddedFileEntries.RemoveAll(x => x == null || x.ImportedFileLocation == SelectedFileEntry.ImportedFileLocation || x.Name == SelectedFileEntry.Name);
            SelectedFileEntry = null;
            DataContext = null;
            DataContext = this;
        }

        private void btnReImport_Click(object sender, RoutedEventArgs e)
        {
            if (lstFiles.SelectedItem == null)
                return;

            var embeddedFile = lstFiles.SelectedItem as EmbeddedFileEntry;
            if (embeddedFile != null)
            {
                if (File.Exists(embeddedFile.ImportedFileLocation))
                {
                    embeddedFile.Data = File.ReadAllBytes(embeddedFile.ImportedFileLocation);
                }
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Any files (*.*)|*.*";
            openFileDialog.Multiselect = true;
            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    EmbeddedFileEntry embeddedFile = new EmbeddedFileEntry();
                    embeddedFile.ImportedFileLocation = file;
                    embeddedFile.Name = fileInfo.Name;
                    embeddedFile.Data = File.ReadAllBytes(file);

                    AssetManager.Instance.AddEmbeddedFile(embeddedFile);
                    DataContext = null;
                    DataContext = this;
                }
            }
        }
    }
}
