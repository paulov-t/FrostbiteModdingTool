using AvalonDock.Layout;
using FIFAModdingUI.Pages.Common;
using FrostySdk;
using FrostySdk.Frostbite.IO;
using FrostySdk.Managers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace FMT.Pages.Common
{
    /// <summary>
    /// Interaction logic for BrowserOfInitfs.xaml
    /// </summary>
    public partial class BrowserOfInitfs : UserControl
    {
        public BrowserOfInitfs()
        {
            InitializeComponent();
            Load();
        }

        public class FileEntry
        {
            public string FileName { get; }

            public string FileType
            {
                get
                {

                    var lastIndexOfDot = FileName.LastIndexOf('.');
                    if (lastIndexOfDot != -1)
                        return FileName.Substring(FileName.LastIndexOf('.'), FileName.Length - FileName.LastIndexOf('.'));

                    return "";

                }
            }

            public byte[] Data
            {
                get
                {
                    if (AssetManager.Instance != null && AssetManager.Instance.InitFSManager.DataModifications.ContainsKey(this.FileName))
                        return AssetManager.Instance.InitFSManager.DataModifications[FileName];

                    return FileSystem.Instance.memoryFs[FileName];
                }
            }

            public bool IsModified => AssetManager.Instance != null ? AssetManager.Instance.InitFSManager.DataModifications.ContainsKey(this.FileName) : false;

            public FileEntry(string fileName)
            {
                this.FileName = fileName;
            }

            public override string ToString()
            {
                return FileName;
            }

        }

        public AssetEntry AssetEntry;

        public FileEntry SelectedEntry => (FileEntry)lb.SelectedItem;

        public string SelectedEntryText { get; set; }
        public Browser ParentBrowser { get; internal set; }

        public void Load()
        {
            browserDocumentsPane.Children.Clear();

            RefreshList();
        }

        private void RefreshList()
        {
            List<FileEntry> list = new List<FileEntry>();
            list = FileSystem.Instance.memoryFs.Select(x => new FileEntry(x.Key)).ToList();
            lb.ItemsSource = list;
            DataContext = null;
            DataContext = this;
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            await ExportAsync().ConfigureAwait(continueOnCapturedContext: true);
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            await ImportAsync().ConfigureAwait(continueOnCapturedContext: true);
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
        }


        private async Task ExportAsync()
        {
            object selectedItem = lb.SelectedItem;
            FileEntry entry = selectedItem as FileEntry;
            if (entry == null)
            {
                return;
            }
            string filter = entry.FileType + " (*." + entry.FileType.ToLower() + ")|*." + entry.FileType.Replace(".", "");
            //if (entry.Type == "DDS")
            //{
            //    filter = "PNG (*.png)|*.png|" + filter;
            //}
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = entry.ToString().Replace('\\', '_').Replace('/', '_'),
                AddExtension = true,
                Title = "Export File"
            };
            if (dialog.ShowDialog() == true)
            {
                if (File.Exists(dialog.FileName))
                    File.Delete(dialog.FileName);

                await File.WriteAllBytesAsync(dialog.FileName, entry.Data);
                //string filename = dialog.FileName;
                //string extension = System.IO.Path.GetExtension(filename);
                //string filterExtension = filter.Split('|')[dialog.FilterIndex * 2 - 1];
                //if (string.IsNullOrEmpty(extension) || (filterExtension.Equals("*.DDS", StringComparison.OrdinalIgnoreCase) && !extension.Equals("." + entry.Type, StringComparison.Ordinal)))
                //{
                //    filename = filename + "." + entry.Type;
                //}

                //await ExportAsync(entry, filename);
            }
        }

        private async Task ImportAsync()
        {
            FileEntry entry = lb.SelectedItem as FileEntry;
            if (entry == null)
            {
                return;
            }
            //         string filter = entry.Type + " (*." + entry.Type.ToLower() + ")|*." + entry.Type;
            //         if (entry.Type == "DDS")
            //         {
            //             filter = "All Images|*.png;*.dds";
            //         }
            //         Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            //{
            //             Filter = filter,
            //             FileName = entry.Name.Replace('\\', '_').Replace('/', '_'),
            //             Title = "Import File"
            //         };
            //         if (dialog.ShowDialog() == true)
            //         {
            //             try
            //             {
            //                 await ImportAsync(entry, dialog.FileName);
            //             }
            //             catch (InvalidDataException)
            //             {
            //             }
            //         }

            //if(ParentBrowser != null)
            //         {
            //	ParentBrowser.UpdateAssetListView();
            //         }
        }

        private void lb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lb.SelectedItem != null)
            {
                //var bigFileEntry = lb.SelectedItem as BigFileEntry;

                // ---- ----------
                // stop duplicates
                browserDocumentsPane.AllowDuplicateContent = false;
                foreach (var child in browserDocumentsPane.Children)
                {
                    if (child.Title == SelectedEntry.ToString())
                    {
                        browserDocumentsPane.SelectedContentIndex = browserDocumentsPane.Children.IndexOf(child);
                        return;
                    }
                }
                // ---- ----------

                var newLayoutDoc = new LayoutDocument();
                newLayoutDoc.Title = SelectedEntry.ToString();
                switch (SelectedEntry.FileType)
                {
                    case ".json":
                    case ".ini":
                    case ".cfg":
                    case ".lua":
                        TextBox textBox = new TextBox();
                        textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                        textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                        textBox.Text = Encoding.UTF8.GetString(SelectedEntry.Data);
                        textBox.TextChanged += TextBox_TextChanged;
                        newLayoutDoc.Content = textBox;
                        break;
                    default:
                        WpfHexaEditor.HexEditor hexEditor = new WpfHexaEditor.HexEditor();
                        hexEditor.Stream = new MemoryStream(SelectedEntry.Data);
                        newLayoutDoc.Content = hexEditor;
                        hexEditor.BytesModified += HexEditor_BytesModified;
                        break;
                }

                browserDocumentsPane.Children.Insert(0, newLayoutDoc);
                browserDocumentsPane.SelectedContentIndex = 0;


            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var fileEntry = lb.SelectedItem as FileEntry;
            if (fileEntry == null)
                return;
            if (sender is TextBox editor)
            {
                var bytesOfTextbox = Encoding.UTF8.GetBytes(editor.Text);
                if (bytesOfTextbox != fileEntry.Data)
                {
                    ModifyFileWithBytes(fileEntry.FileName, bytesOfTextbox);
                }
            }
        }

        private void HexEditor_BytesModified(object sender, WpfHexaEditor.Core.EventArguments.ByteEventArgs e)
        {
            var fileEntry = lb.SelectedItem as FileEntry;
            if (fileEntry == null)
                return;

            var hexEditor = sender as WpfHexaEditor.HexEditor;
            if (hexEditor != null)
            {
                ModifyFileWithBytes(fileEntry.FileName, hexEditor.GetAllBytes(true));
                //if (!FileSystem.Instance.MemoryFileSystemModifiedItems.ContainsKey(fileEntry.FileName))
                //	FileSystem.Instance.MemoryFileSystemModifiedItems.Add(fileEntry.FileName, hexEditor.GetAllBytes(true));

                //FileSystem.Instance.MemoryFileSystemModifiedItems[fileEntry.FileName] = hexEditor.GetAllBytes(true);
            }
        }

        private void ModifyFileWithBytes(string fileName, byte[] bytes)
        {
            if (!AssetManager.Instance.InitFSManager.DataModifications.ContainsKey(fileName))
                AssetManager.Instance.InitFSManager.DataModifications.Add(fileName, bytes);

            AssetManager.Instance.InitFSManager.DataModifications[fileName] = bytes;

            RefreshList();
        }

    }
}
