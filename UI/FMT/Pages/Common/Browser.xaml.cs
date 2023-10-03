using AvalonDock.Layout;
using CSharpImageLibrary;
using FMT;
using FMT.FileTools;
using FMT.Pages.Common;
using FolderBrowserEx;
using Frostbite.Textures;
using FrostbiteModdingUI.Models;
using FrostbiteModdingUI.Windows;
using FrostbiteSdk;
using FrostySdk;
using FrostySdk.Frostbite.IO.Input;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using v2k4FIFAModding.Frosty;

namespace FIFAModdingUI.Pages.Common
{
    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class Browser : UserControl, INotifyPropertyChanged
    {

        private IEditorWindow MainEditorWindow
        {
            get
            {
                return App.MainEditorWindow as IEditorWindow;
            }
        }

        public Browser()
        {
            InitializeComponent();
            DataContext = this;

            AssetManager.AssetManagerModified += AssetManager_AssetManagerModified;
        }

        private void AssetManager_AssetManagerModified(IAssetEntry modifiedAsset)
        {
            RequiresRefresh = true;
        }

        public int HalfMainWindowWidth { get { return MainEditorWindow != null ? (int)Math.Round(((Window)MainEditorWindow).ActualWidth / 2) : 400; } }


        private IEnumerable<IAssetEntry> allAssets;

        public IEnumerable<IAssetEntry> AllAssetEntries
        {
            get { return allAssets; }
            set { allAssets = value; _ = Update(); }
        }



        public string FilterText { get; set; }

        public HelixToolkit.Wpf.SharpDX.Camera Camera { get; set; }

        private bool m_RequiresRefresh;

        public bool RequiresRefresh
        {
            get { return m_RequiresRefresh; }
            set { m_RequiresRefresh = value; Dispatcher.Invoke(() => { btnRefresh.IsEnabled = value; }); }
        }



        #region Entry Properties

        private AssetEntry assetEntry1;

        public AssetEntry SelectedEntry
        {
            get
            {
                if (assetEntry1 == null && SelectedLegacyEntry != null)
                    return SelectedLegacyEntry;

                return assetEntry1;
            }
            set { assetEntry1 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedEntry))); }
        }

        private LegacyFileEntry legacyFileEntry;

        public LegacyFileEntry SelectedLegacyEntry
        {
            get { return legacyFileEntry; }
            set { legacyFileEntry = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedLegacyEntry))); }
        }


        private EbxAsset ebxAsset;

        public EbxAsset SelectedEbxAsset
        {
            get { return ebxAsset; }
            set { ebxAsset = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedEbxAsset))); }
        }


        #endregion

        public async Task<IEnumerable<IAssetEntry>> GetFilteredAssetEntriesAsync()
        {
            var onlymodified = false;
            var showEAData = false;
            var filterText = "";

            await Dispatcher.InvokeAsync(() =>
            {
                onlymodified = chkShowOnlyModified.IsChecked.Value;
                showEAData = chkShowEADuplicateDataFiles.IsChecked.Value;   
                filterText = txtFilter.Text;
            });

            return await Task.FromResult(GetFilteredAssets(filterText, onlymodified, showEAData));

        }

        private IEnumerable<IAssetEntry> GetFilteredAssets(ReadOnlySpan<char> filterSpan, bool onlymodified, bool showEAData)
        {
            var assets = allAssets ?? allAssets.ToList();
            if (assets == null)
                return null;

            if (!filterSpan.IsEmpty)
            {
                var filterText = new string(filterSpan.ToArray());
                assets = assets.Where(x =>
                    x.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                    );
            }

            assets = assets.Where(x => !x.Name.EndsWith("EAData") || showEAData && x.Name.EndsWith("EAData"));


            assets = assets.Where(x =>
                (
                onlymodified == true
                && x.IsModified
                )
                || onlymodified == false
                );

            return assets;
        }

        private Dictionary<string, AssetPath> assetPathMapping = new Dictionary<string, AssetPath>(StringComparer.OrdinalIgnoreCase);
        
        private AssetPath selectedPath = null;

        private AssetPath assetPath;

        public AssetPath AssetPath
        {
            get { return assetPath; }
            set { assetPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AssetPath))); }
        }


        public ObservableCollection<AssetPath> BrowserItems { get; set; } = new ObservableCollection<AssetPath>();

        public bool IsUpdating { get; set; } = false;

        public async Task Update()
        {
            if (IsUpdating)
                return;

            IsUpdating = true;

            //if (AssetPath == null)
            AssetPath = new AssetPath("", "", null, true);

            var assets = (await GetFilteredAssetEntriesAsync()).ToList();

            await Task.Run(() =>
            {

                foreach (IAssetEntry item in assets)
                {
                    var path = item.Path;
                    if (!path.StartsWith("/"))
                        path = path.Insert(0, "/");

                    string[] directories = path.Split(new char[1]
                    {
                        '/'
                    }, StringSplitOptions.RemoveEmptyEntries);
                    AssetPath assetPath2 = AssetPath;
                    foreach (string directory in directories)
                    {
                        bool flag = false;
                        foreach (AssetPath child in assetPath2.Children)
                        {
                            if (child.PathName.Equals(directory, StringComparison.OrdinalIgnoreCase))
                            {
                                if (directory.ToCharArray().Any((char a) => char.IsUpper(a)))
                                {
                                    child.UpdatePathName(directory);
                                }
                                assetPath2 = child;
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            string text2 = assetPath2.FullPath + "/" + directory;
                            AssetPath assetPath3 = null;
                            if (!assetPathMapping.ContainsKey(text2))
                            {
                                assetPath3 = new AssetPath(directory, text2, assetPath2);
                                assetPathMapping.Add(text2, assetPath3);
                            }
                            else
                            {
                                assetPath3 = assetPathMapping[text2];
                                assetPath3.Children.Clear();
                                if (assetPath3 == selectedPath)
                                {
                                    selectedPath.IsSelected = true;
                                }
                            }
                            assetPath2.Children.Add(assetPath3);
                            assetPath2 = assetPath3;
                        }
                    }
                }
                if (!assetPathMapping.ContainsKey("/"))
                {
                    assetPathMapping.Add("/", new AssetPath("", "", null, bInRoot: true));
                }
                AssetPath.Children.Insert(0, assetPathMapping["/"]);

                foreach (IAssetEntry item in assets.OrderBy(x => x.Name))
                {
                    if (assetPathMapping.ContainsKey("/" + item.Path))
                    {
                        var fileAssetPath = new AssetPath(item.DisplayName, "/" + item.Path + "/" + item.Filename, assetPathMapping["/" + item.Path], false);
                        fileAssetPath.Asset = item;
                        assetPathMapping["/" + item.Path].Children.Add(fileAssetPath);
                    }
                }

                // assets is no longer needed
                assets = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            });

            BrowserItems.Clear();
            foreach (var aP in AssetPath.Children.OrderBy(x => x.PathName).ToList())
            {
                BrowserItems.Add(aP);
            }

            
            IsUpdating = false;

        }

        private async Task ExportAsset(AssetEntry assetEntry, string saveLocation)
        {
            bool isFolder = false;
            if (new DirectoryInfo(saveLocation).Exists)
            {
                saveLocation += "\\" + assetEntry.Filename;
                isFolder = true;
            }

            if (assetEntry is LegacyFileEntry)
            {
                var legacyData = ((MemoryStream)AssetManager.Instance.GetCustomAsset("legacy", assetEntry)).ToArray();
                if (assetEntry.Type == "DDS"
                    &&
                    (saveLocation.Contains("PNG", StringComparison.OrdinalIgnoreCase)
                     || isFolder)
                    )
                {
                    if (isFolder)
                    {
                        saveLocation += ".png";
                    }

                    ImageEngineImage originalImage = new ImageEngineImage(legacyData);

                    var imageBytes = originalImage.Save(
                        new ImageFormats.ImageEngineFormatDetails(
                            ImageEngineFormat.PNG)
                        , MipHandling.KeepTopOnly
                        , removeAlpha: false);

                    await File.WriteAllBytesAsync(saveLocation, imageBytes);
                }
                else if (assetEntry.Type == "DDS" && saveLocation.Contains("DDS", StringComparison.OrdinalIgnoreCase))
                {
                    ImageEngineImage originalImage = new ImageEngineImage(legacyData);

                    await originalImage.Save(saveLocation
                        , new ImageFormats.ImageEngineFormatDetails(ImageEngineFormat.DDS_DXT5)
                        , GenerateMips: MipHandling.KeepExisting
                        , removeAlpha: false);
                }
                else
                {
                    if (isFolder)
                        saveLocation += "." + assetEntry.Type;

                    await File.WriteAllBytesAsync(saveLocation, legacyData);
                }
                MainEditorWindow.Log($"Exported {assetEntry.Filename} to {saveLocation}");
            }
            else if (assetEntry is EbxAssetEntry ebxAssetEntry)
            {
                AssetEntryExporter entryExporter = new AssetEntryExporter(assetEntry);

                if (assetEntry.Type == "TextureAsset")
                {
                    var resEntry = AssetManager.Instance.GetResEntry(assetEntry.Name);
                    if (resEntry != null)
                    {
                        using (var resStream = AssetManager.Instance.GetRes(resEntry))
                        {
                            Texture texture = new Texture(resStream, resEntry);
                            TextureExporter textureExporter = new TextureExporter();
                            if (isFolder)
                                saveLocation += ".png";
                            textureExporter.Export(texture, saveLocation, "*.png");
                            MainEditorWindow.Log($"Exported {assetEntry.Filename} to {saveLocation}");
                        }
                    }
                }
                else if (assetEntry.Type == "SkinnedMeshAsset" || assetEntry.Type == "CompositeMeshAsset" || assetEntry.Type == "RigidMeshAsset")
                {
                    entryExporter.Export(saveLocation);
                }
                else
                {
                    File.WriteAllText(!saveLocation.EndsWith(".json") && isFolder ? saveLocation + ".json" : saveLocation, entryExporter.ExportToJson());
                }
            }
        }

        AssetPath SelectedAssetPath = null;

        MainViewModel ModelViewerModel;

        public event PropertyChangedEventHandler PropertyChanged;

        private void DisplayUnknownFileViewer(Stream stream)
        {
            //btnExport.IsEnabled = true;
            //btnImport.IsEnabled = true;
            //btnRevert.IsEnabled = true;

            //unknownFileDocumentsPane.Children.Clear();
            var newLayoutDoc = new LayoutDocument();
            newLayoutDoc.Title = SelectedEntry.DisplayName;
            WpfHexaEditor.HexEditor hexEditor = new WpfHexaEditor.HexEditor();
            hexEditor.Stream = stream;
            newLayoutDoc.Content = hexEditor;
            hexEditor.BytesModified -= HexEditor_BytesModified;
            hexEditor.BytesModified += HexEditor_BytesModified;
            //unknownFileDocumentsPane.Children.Insert(0, newLayoutDoc);
            //unknownFileDocumentsPane.SelectedContentIndex = 0;

            //UnknownFileViewer.Visibility = Visibility.Visible;
        }

        private void HexEditor_BytesModified(object sender, WpfHexaEditor.Core.EventArguments.ByteEventArgs e)
        {
            var hexEditor = sender as WpfHexaEditor.HexEditor;
            if (hexEditor != null)
            {
                if (this.SelectedLegacyEntry != null)
                {
                    AssetManager.Instance.ModifyLegacyAsset(this.SelectedLegacyEntry.Name, hexEditor.GetAllBytes(true), false);
                    //UpdateAssetListView();
                }
                else
                {

                }
            }
        }

        private async void btnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var labelTag = ((MenuItem)sender).Tag as AssetPath;
                if (labelTag == null)
                    return;

                SelectedEntry = (AssetEntry)labelTag.Asset;
                if (SelectedEntry == null)
                    return;

                try
                {
                    AssetEntryImporter assetEntryImporter = new AssetEntryImporter(SelectedEntry);
                    var openFileDialog = assetEntryImporter.GetOpenDialogWithFilter();
                    var dialogResult = openFileDialog.ShowDialog();
                    if (dialogResult.HasValue && dialogResult.Value == true)
                    {
                        MainEditorWindow.Log("Importing " + openFileDialog.FileName);

                        if (SelectedEntry.Type == "SkinnedMeshAsset")
                        {
                            var skeletonEntryText = "content/character/rig/skeleton/player/skeleton_player";
                            MeshSkeletonSelector meshSkeletonSelector = new MeshSkeletonSelector();
                            var meshSelectorResult = meshSkeletonSelector.ShowDialog();
                            if (meshSelectorResult.HasValue && meshSelectorResult.Value)
                            {
                                if (!meshSelectorResult.Value)
                                {
                                    MessageBox.Show("Cannot import without a Skeleton");
                                    return;
                                }

                                skeletonEntryText = meshSkeletonSelector.AssetEntry.Name;
                                assetEntryImporter.ImportEbxSkinnedMesh(openFileDialog.FileName, skeletonEntryText);

                            }
                            else
                            {
                                MessageBox.Show("Cannot import without a Skeleton");
                                return;
                            }
                        }
                        else
                        {
                            var importResult = await assetEntryImporter.ImportAsync(openFileDialog.FileName);
                            if (!importResult)
                            {
                                MainEditorWindow.LogError("Failed to import file to " + SelectedEntry.Name);
                                return;
                            }

                            MainEditorWindow.Log($"Imported file {openFileDialog.FileName} to {SelectedEntry.Name} successfully.");
                        }
                    }

                }
                catch (Exception ex)
                {
                    MainEditorWindow.LogError(ex.Message + Environment.NewLine);
                }
            }
            catch (Exception)
            {
            }
        }

        private void btnRevert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var labelTag = ((MenuItem)sender).Tag as AssetPath;
                if (labelTag == null)
                    return;

                var assetEntry = labelTag.Asset;
                if (assetEntry == null)
                    return;

                AssetManager.Instance.RevertAsset(assetEntry);
            }
            catch (Exception)
            {
            }
        }

        private async void txtFilter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await Update();
            }
        }

        private async void chkShowOnlyModified_Checked(object sender, RoutedEventArgs e)
        {
            await Update();
        }

        private async void chkShowOnlyModified_Unchecked(object sender, RoutedEventArgs e)
        {
            await Update();
        }

        private void assetTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var assetTreeViewSelectedItem = assetTreeView.SelectedItem as AssetPath;
            if (assetTreeViewSelectedItem != null)
            {
                SelectedAssetPath = assetTreeViewSelectedItem;
                //UpdateAssetListView();
            }
        }

        private void btnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            FMT.Controls.Windows.DuplicateItem dupWindow = new FMT.Controls.Windows.DuplicateItem();
            dupWindow.EntryToDuplicate = SelectedEntry != null ? SelectedEntry : SelectedLegacyEntry;
            dupWindow.IsLegacy = SelectedLegacyEntry != null;
            var result = dupWindow.ShowDialog();
            if (result.HasValue && result.Value)
            {
                //if (MainEditorWindow != null)
                //	MainEditorWindow.UpdateAllBrowsersFull();
            }
            dupWindow = null;
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnImportFolder_Click(object sender, RoutedEventArgs e)
        {

            bool importedSomething = false;
            MenuItem parent = sender as MenuItem;
            if (parent != null)
            {
                var assetPath = parent.Tag as AssetPath;
                //LoadingDialog loadingDialog = new LoadingDialog($"Importing into {assetPath.FullPath}", "Import Started");

                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.AllowMultiSelect = false;
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folder = folderBrowserDialog.SelectedFolder;
                    var filesGathered = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
                    var filesImportAttempted = 0;


                    foreach (string fileName in filesGathered)
                    {
                        filesImportAttempted++;

                        FileInfo fi = new FileInfo(fileName);
                        if (fi.Extension.Contains(".png"))
                        {
                            var importFileInfo = new FileInfo(fileName);
                            var importFileInfoSplit = importFileInfo.Name.Split("_");
                            if (importFileInfoSplit.Length > 1)
                            {
                                importFileInfoSplit[1] = importFileInfoSplit[1].Replace(".png", "");

                                var resEntryPath = AssetManager.Instance.RES.Keys.FirstOrDefault(
                                    x => x.EndsWith(
                                        "/" + importFileInfo.Name.Replace(".png", "", StringComparison.OrdinalIgnoreCase)
                                        , StringComparison.OrdinalIgnoreCase)
                                    );

                                if (resEntryPath == null)
                                {
                                    resEntryPath = AssetManager.Instance.RES.Keys.FirstOrDefault(
                                        x => x.StartsWith(assetPath.FullPath.Substring(1))
                                        && x.Contains(importFileInfoSplit[0])
                                        && x.Contains(importFileInfoSplit[1])
                                        && !x.Contains("brand")
                                        && !x.Contains("crest")
                                        );
                                }

                                if (resEntryPath != null)
                                {

                                    var resEntry = AssetManager.Instance.GetResEntry(resEntryPath);
                                    if (resEntry != null)
                                    {
                                        Texture texture = new Texture(resEntry);
                                        TextureImporter textureImporter = new TextureImporter();
                                        EbxAssetEntry ebxAssetEntry = AssetManager.Instance.GetEbxEntry(resEntryPath);

                                        if (ebxAssetEntry != null)
                                        {
                                            await textureImporter.ImportAsync(fileName, ebxAssetEntry, texture);
                                            importedSomething = true;
                                        }
                                    }

                                }
                                else
                                {
                                    var ext = fi.Extension.ToLower();
                                    if (ext == ".png")
                                        ext = ".dds";

                                    var legAssetPath = assetPath.FullPath.Substring(1, assetPath.FullPath.Length - 1) + "/" + fi.Name.Substring(0, fi.Name.LastIndexOf(".")) + ext;

                                    var legEntry = (LegacyFileEntry)AssetManager.Instance.EnumerateCustomAssets("legacy").FirstOrDefault(x => x.Name.EndsWith(legAssetPath));
                                    if (legEntry != null)
                                    {
                                        var isImage = legEntry.Type == "DDS";
                                        if (isImage)
                                        {
                                            await AssetManager.Instance.DoLegacyImageImportAsync(fileName, legEntry);
                                            importedSomething = true;
                                        }

                                    }
                                }
                            }
                        }
                        else
                        {
                            var legAssetPath = assetPath.FullPath.Substring(1, assetPath.FullPath.Length - 1) + "/" + fi.Name.Substring(0, fi.Name.LastIndexOf(".")) + fi.Extension.ToLower();
                            var legEntry = (LegacyFileEntry)AssetManager.Instance.GetCustomAssetEntry("legacy", legAssetPath);
                            if (legEntry != null)
                            {
                                AssetManager.Instance.ModifyLegacyAsset(legEntry.Name, File.ReadAllBytes(fileName), false);
                                importedSomething = true;
                            }
                        }
                    }

                    if (importedSomething)
                    {
                        MainEditorWindow.Log($"Imported {folder} to {assetPath.FullPath}");
                    }
                }
            }



        }

        private async void btnExportFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem parent = sender as MenuItem;
                if (parent != null)
                {
                    var assetPath = parent.Tag as AssetPath;

                    FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                    folderBrowserDialog.AllowMultiSelect = false;
                    if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string folder = folderBrowserDialog.SelectedFolder;
                        Directory.CreateDirectory(folder);
                        var ebxInPath = AssetManager.Instance.EnumerateEbx().Where(x => x.Name.Contains(assetPath.FullPath.Substring(1)));
                        if (ebxInPath.Any())
                        {
                            MainEditorWindow.ShowLoadingDialog("Exporting folder", "Exporting folder", 0);
                            var countOfItemsInEbxPath = ebxInPath.Count();
                            var indexOfItemInEbxPath = 0;
                            foreach (var ebx in ebxInPath)
                            {
                                await ExportAsset(ebx, folder);
                                indexOfItemInEbxPath++;
                                MainEditorWindow.ShowLoadingDialog("Exporting file " + ebx.Name, "Exporting folder", (int)Math.Round((double)indexOfItemInEbxPath / countOfItemsInEbxPath));
                            }
                            MainEditorWindow.ShowLoadingDialog("", "", 0);
                        }

                        MainEditorWindow.Log($"Exported {assetPath.FullPath} to {folder}");

                        var legacyInPath = AssetManager.Instance.EnumerateCustomAssets("legacy").Where(x => x.Name.Contains(assetPath.FullPath.Substring(1)));
                        if (legacyInPath.Any())
                        {
                            foreach (var l in legacyInPath)
                            {
                                await ExportAsset(l, folder);
                            }
                        }
                    }
                }
            }
            catch 
            {
                MainEditorWindow.ShowLoadingDialog("", "", 0);
            }
        }

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var labelTag = ((ContentControl)sender).Tag as AssetPath;
            if (labelTag == null)
                return;

            var assetEntry = labelTag.Asset;
            if (assetEntry == null)
                return;

            // ---- ----------
            // stop duplicates
            browserDocuments.AllowDuplicateContent = false;
            foreach (var child in browserDocuments.Children)
            {
                if (child is LayoutDocument document)
                {
                    if (document.Description.Equals(assetEntry.Path + assetEntry.Filename, StringComparison.OrdinalIgnoreCase))
                    {
                        browserDocuments.SelectedContentIndex = browserDocuments.Children.IndexOf(child);
                        return;
                    }
                }
            }
            // ---- ----------
            var layoutDocument = new LayoutDocument();
            layoutDocument.Title = assetEntry.Filename;
            layoutDocument.Description = assetEntry.Path + assetEntry.Filename;
            layoutDocument.Content = new OpenedFile(assetEntry);
            browserDocuments.Children.Insert(0, layoutDocument);
            browserDocuments.SelectedContentIndex = 0;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _ = Update();
            RequiresRefresh = false;
        }

        private void chkShowEADuplicateDataFiles_Checked(object sender, RoutedEventArgs e)
        {
            _ = Update();

        }

        private void chkShowEADuplicateDataFiles_Unchecked(object sender, RoutedEventArgs e)
        {
            _ = Update();
        }
    }

    public class AssetPath : INotifyPropertyChanged
    {
        private string fullPath;

        private string name;

        private bool expanded;

        private bool selected;

        private bool root;

        private AssetPath parent;

        private List<AssetPath> children = new List<AssetPath>();

        public event PropertyChangedEventHandler PropertyChanged;

        public string DisplayName => name.Trim('!');

        public string PathName
        {
            get
            {

                return IsModified ? "!" + name : name;

            }
            set { name = value; }
        }

        public string FullPath => fullPath;

        public AssetPath Parent => parent;

        public List<AssetPath> Children => children;

        public bool IsExpanded
        {
            get
            {
                if (expanded)
                {
                    return Children.Count != 0;
                }
                return false;
            }
            set
            {
                expanded = value;
            }
        }

        public bool IsSelected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;
            }
        }

        public bool IsRoot => root;

        public IAssetEntry Asset { get; internal set; }

        public bool IsModified
        {
            get
            {

                return Asset != null && Asset.IsModified;

            }
        }

        public AssetPath(string inName, string path, AssetPath inParent, bool bInRoot = false)
        {
            name = inName;
            fullPath = path;
            root = bInRoot;
            parent = inParent;
        }

        public void UpdatePathName(string newName)
        {
            name = newName;
        }
    }

}
