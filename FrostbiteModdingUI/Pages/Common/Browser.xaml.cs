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

            //await Update();
            //do
            //{

            //}
            //while (assetTreeView.ItemsSource.GetEnumerator().MoveNext());
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
            var filterText = "";

            Dispatcher.Invoke(() =>
            {
                onlymodified = chkShowOnlyModified.IsChecked.Value;
                filterText = txtFilter.Text;
            });

            return await Task.FromResult(GetFilteredAssets(filterText, onlymodified));

        }

        private IEnumerable<IAssetEntry> GetFilteredAssets(ReadOnlySpan<char> filterSpan, bool onlymodified)
        {
            var assets = allAssets;
            if (assets == null)
                return null;

            if (!filterSpan.IsEmpty)
            {
                var filterText = new string(filterSpan.ToArray());
                assets = assets.Where(x =>
                    x.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                    );
            }

            //assets = assets.Where(x => !x.Name.Contains("-False-", StringComparison.OrdinalIgnoreCase));
            //assets = assets.Where(x => !x.Name.Contains("FMTOther", StringComparison.OrdinalIgnoreCase));
          
            assets = assets.Where(x =>
                (
                onlymodified == true
                && x.IsModified
                )
                || onlymodified == false
                );

            return assets;
        }

        //public List<string> CurrentAssets { get; set; }

        //public int CurrentTier { get; set; }

        //public string CurrentPath { get; set; }

        //public void SelectOption(string name)
        //{

        //}

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

            var assets = await GetFilteredAssetEntriesAsync();

            await Task.Run(() =>
            {

                //foreach (AssetEntry item in assets)
                foreach (IAssetEntry item in assets)
                {
                    var path = item.Path;
                    if (!path.StartsWith("/"))
                        path = path.Insert(0, "/");
                    //if ((!ShowOnlyModified || item.IsModified) && FilterText(item.Name, item))
                    {
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
                }
                if (!assetPathMapping.ContainsKey("/"))
                {
                    //assetPathMapping.Add("/", new AssetPath("![root]", "", null, bInRoot: true));
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
            });

            BrowserItems.Clear();
            foreach (var aP in AssetPath.Children.OrderBy(x => x.PathName).ToList())
            {
                BrowserItems.Add(aP);
            }

            //         await Dispatcher.InvokeAsync((Action)(() =>
            //{
            //	assetTreeView.ItemsSource = assetPath.Children;
            //	assetTreeView.Items.SortDescriptions.Add(new SortDescription("PathName", ListSortDirection.Ascending));
            //}));
            //       await Dispatcher.InvokeAsync(() =>
            //{
            // assetTreeView.ItemsSource = AssetPath.Children.OrderBy(x=>x.PathName);
            // //assetTreeView.Items.SortDescriptions.Add(new SortDescription("PathName", ListSortDirection.Ascending));
            //});

            //Dispatcher.Invoke(() =>
            //{
            //	this.DataContext = null;
            //	this.DataContext = this;
            //});
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
            else if (assetEntry is EbxAssetEntry)
            {
                if (assetEntry.Type == "TextureAsset")
                {
                    var resEntry = AssetManager.Instance.GetResEntry(assetEntry.Name);
                    if (resEntry != null)
                    {
                        using (var resStream = AssetManager.Instance.GetRes(resEntry))
                        {
                            Texture texture = new Texture(resStream, ProjectManagement.Instance.Project.AssetManager);
                            TextureExporter textureExporter = new TextureExporter();
                            if (isFolder)
                                saveLocation += ".png";
                            textureExporter.Export(texture, saveLocation, "*.png");
                            MainEditorWindow.Log($"Exported {assetEntry.Filename} to {saveLocation}");
                        }
                    }
                }
                else
                {
                    var ebx = AssetManager.Instance.GetEbxStream((EbxAssetEntry)assetEntry);
                    if (ebx != null)
                    {
                        if (isFolder)
                            saveLocation += ".bin";
                        File.WriteAllBytes(saveLocation, ((MemoryStream)ebx).ToArray());
                        MainEditorWindow.Log($"Exported {assetEntry.Filename} to {saveLocation}");

                    }
                    else
                    {
                        MainEditorWindow.Log("Failed to export file");
                    }
                }
            }
        }

        AssetPath SelectedAssetPath = null;

        //private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //	Label label = sender as Label;
        //	if (label != null && label.Tag != null)
        //	{
        //		SelectedAssetPath = label.Tag as AssetPath;
        //		UpdateAssetListView();
        //	}
        //}

        //private void Label_KeyUp(object sender, KeyEventArgs e)
        //{
        //	if (e.Key == Key.Enter || e.Key == Key.Return)
        //	{
        //		Label label = sender as Label;
        //		if (label != null && label.Tag != null)
        //		{
        //			SelectedAssetPath = label.Tag as AssetPath;
        //			UpdateAssetListView();
        //		}
        //	}
        //}

        //public void UpdateAssetListView()
        //{
        //	var filterText = string.Empty;
        //	bool onlyModified = false;
        //	Dispatcher.Invoke(() => {
        //		filterText = txtFilter.Text;
        //		onlyModified = chkShowOnlyModified.IsChecked.HasValue && chkShowOnlyModified.IsChecked.Value;
        //	});


        //	if (SelectedAssetPath != null)
        //	{
        //		if (SelectedAssetPath.FullPath.Length > 3)
        //		{
        //			var filterPath = (SelectedAssetPath.FullPath.Substring(1, SelectedAssetPath.FullPath.Length - 1));
        //			var filteredAssets = AllAssetEntries.Where(x => x.Path.ToLower() == filterPath.ToLower());
        //			filteredAssets = filteredAssets.Where(x => x.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase));

        //			filteredAssets = filteredAssets.Where(x =>

        //				(
        //				onlyModified == true
        //				&& x.IsModified
        //				)
        //				|| onlyModified == false

        //				).ToList();

        //			//Dispatcher.Invoke(() =>
        //			//{
        //			//	var selectedit = assetListView.SelectedItem;
        //			//	assetListView.ItemsSource = filteredAssets.OrderBy(x => x.Name);
        //			//	assetListView.SelectedItem = selectedit;
        //			//});

        //		}
        //	}
        //}



        //      private void AssetEntry_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //          IAssetEntry entry = (IAssetEntry)((TextBlock)sender).Tag;

        //          if (entry is EbxAssetEntry ebxAssetEntry)
        //              OpenAsset(ebxAssetEntry);
        //          else if (entry is LegacyFileEntry legacyFileEntry)
        //              OpenAsset(legacyFileEntry);
        //          else if (entry is LiveTuningUpdate.LiveTuningUpdateEntry ltuEntry)
        //              OpenAsset(ltuEntry);
        //      }

        //private void AssetEntry_KeyUp(object sender, KeyEventArgs e)
        //{
        //	if (e.Key == Key.Enter || e.Key == Key.Return)
        //	{
        //		IAssetEntry entry = (IAssetEntry)((TextBlock)sender).Tag;

        //              if (entry is EbxAssetEntry ebxAssetEntry)
        //			OpenAsset(ebxAssetEntry);
        //		else if (entry is LegacyFileEntry legacyFileEntry)
        //			OpenAsset(legacyFileEntry);
        //		else if (entry is LiveTuningUpdate.LiveTuningUpdateEntry ltuEntry)
        //			OpenAsset(ltuEntry);
        //          }
        //      }

        MainViewModel ModelViewerModel;

        public event PropertyChangedEventHandler PropertyChanged;

        private void ResetViewers()
        {
            SelectedEntry = null;
            SelectedLegacyEntry = null;
            //btnImport.IsEnabled = false;
            //btnExport.IsEnabled = false;
            //btnRevert.IsEnabled = false;

            //ImageViewerScreen.Visibility = Visibility.Collapsed;
            //TextViewer.Visibility = Visibility.Collapsed;
            //EBXViewer.Visibility = Visibility.Collapsed;
            ////BackupEBXViewer.Visibility = Visibility.Collapsed;
            //UnknownFileViewer.Visibility = Visibility.Collapsed;
            //ModelDockingManager.Visibility = Visibility.Collapsed;
            //BIGViewer.Visibility = Visibility.Collapsed;
        }

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

        private async Task OpenEbxAsset(EbxAssetEntry ebxEntry)
        {
            try
            {
                SelectedEntry = ebxEntry;
                SelectedEbxAsset = AssetManager.Instance.GetEbx(ebxEntry);
                if (ebxEntry.Type == "TextureAsset")
                {
                    try
                    {
                        MainEditorWindow.Log("Loading Texture " + ebxEntry.Filename);
                        var res = AssetManager.Instance.GetResEntry(ebxEntry.Name);
                        if (res != null)
                        {
                            MainEditorWindow.Log("Loading RES " + ebxEntry.Filename);

                            BuildTextureViewerFromAssetEntry(res);
                        }
                        else
                        {
                            throw new Exception("Unable to find RES Entry for " + ebxEntry.Name);
                        }
                    }
                    catch (Exception e)
                    {
                        MainEditorWindow.Log($"Failed to load texture with the message :: {e.Message}");
                    }



                }
                else if (ebxEntry.Type == "SkinnedMeshAsset")
                {
                    if (ebxEntry == null || ebxEntry.Type == "EncryptedAsset")
                    {
                        return;
                    }

                    MainEditorWindow.Log("Loading 3D Model " + ebxEntry.Filename);

                    var resentry = AssetManager.Instance.GetResEntry(ebxEntry.Name);
                    var res = AssetManager.Instance.GetRes(resentry);
                    MeshSet meshSet = new MeshSet(res);

                    var exporter = new MeshSetToFbxExport();
                    exporter.Export(AssetManager.Instance, SelectedEbxAsset.RootObject, "test_noSkel.obj", "2012", "Meters", true, null, "*.obj", meshSet);
                    Thread.Sleep(250);

                    if (ModelViewerModel != null)
                        ModelViewerModel.Dispose();

                    ModelViewerModel = new MainViewModel(skinnedMeshAsset: SelectedEbxAsset, meshSet: meshSet);
                    //this.ModelViewer.DataContext = ModelViewerModel;
                    //this.ModelDockingManager.Visibility = Visibility.Visible;
                    //await ModelViewerEBX.LoadEbx(ebxEntry, SelectedEbxAsset, ProjectManagement.Instance.Project, MainEditorWindow);

                    //               this.btnExport.IsEnabled = ProfileManager.CanExportMeshes;
                    //this.btnImport.IsEnabled = ProfileManager.CanImportMeshes;
                    //this.btnRevert.IsEnabled = SelectedEntry.HasModifiedData;

                }
                else if (string.IsNullOrEmpty(ebxEntry.Type) || ebxEntry.Type == "UnknownType")
                {
                    DisplayUnknownFileViewer(AssetManager.Instance.GetEbxStream(ebxEntry));
                }
                else
                {
                    if (ebxEntry == null || ebxEntry.Type == "EncryptedAsset")
                    {
                        return;
                    }
                    var ebx = ProjectManagement.Instance.Project.AssetManager.GetEbx(ebxEntry);
                    if (ebx != null)
                    {
                        MainEditorWindow.Log("Loading EBX " + ebxEntry.Filename);

                        //var successful = await EBXViewer.LoadEbx(ebxEntry, ebx, ProjectManagement.Instance.Project, MainEditorWindow);
                        //EBXViewer.Visibility = Visibility.Visible;

                        //btnRevert.IsEnabled = true;
                        //btnImport.IsEnabled = true;
                        //btnExport.IsEnabled = true;
                    }
                }
            }
            catch (Exception e)
            {
                MainEditorWindow.Log($"Failed to load file with message {e.Message}");
                Debug.WriteLine(e.ToString());

                DisplayUnknownFileViewer(AssetManager.Instance.GetEbxStream(ebxEntry));

            }
        }

        //private async Task OpenAsset(IAssetEntry entry)
        //{
        //	ResetViewers();
        //	if (entry is EbxAssetEntry ebxEntry)
        //          {
        //		await OpenEbxAsset(ebxEntry);
        //		return;
        //	}

        //	if(entry is LiveTuningUpdate.LiveTuningUpdateEntry ltuEntry)
        //	{
        //		await OpenLTUAsset(ltuEntry);
        //		return;
        //	}

        //	try
        //	{



        //					LegacyFileEntry legacyFileEntry = entry as LegacyFileEntry;
        //					if (legacyFileEntry != null)
        //					{
        //						SelectedLegacyEntry = legacyFileEntry;
        //						//btnImport.IsEnabled = true;

        //						List<string> textViewers = new List<string>()
        //				{
        //					"LUA",
        //					"XML",
        //					"INI",
        //					"NAV",
        //					"JSON",
        //					"TXT",
        //					"CSV",
        //					"TG", // some custom XML / JS / LUA file that is used in FIFA
        //					"JLT", // some custom XML / LUA file that is used in FIFA
        //					"PLS" // some custom XML / LUA file that is used in FIFA
        //				};

        //						List<string> imageViewers = new List<string>()
        //				{
        //					"PNG",
        //					"DDS"
        //				};

        //					List<string> bigViewers = new List<string>()
        //				{
        //					"BIG",
        //					"AST"
        //				};

        //					if (textViewers.Contains(legacyFileEntry.Type))
        //						{
        //							MainEditorWindow.Log("Loading Legacy File " + SelectedLegacyEntry.Filename);

        //							//btnImport.IsEnabled = true;
        //							//btnExport.IsEnabled = true;
        //							//btnRevert.IsEnabled = true;

        //							//TextViewer.Visibility = Visibility.Visible;
        //							//using (var nr = new NativeReader(AssetManager.Instance.GetCustomAsset("legacy", legacyFileEntry)))
        //							//{
        //							//	TextViewer.Text = UTF8Encoding.UTF8.GetString(nr.ReadToEnd());
        //							//}
        //						}
        //						else if (imageViewers.Contains(legacyFileEntry.Type))
        //						{
        //							MainEditorWindow.Log("Loading Legacy File " + SelectedLegacyEntry.Filename);
        //							//btnImport.IsEnabled = true;
        //							//btnExport.IsEnabled = true;
        //							//ImageViewerScreen.Visibility = Visibility.Visible;

        //							BuildTextureViewerFromStream((MemoryStream)ProjectManagement.Instance.Project.AssetManager.GetCustomAsset("legacy", legacyFileEntry));


        //						}
        //					else if (bigViewers.Contains(legacyFileEntry.Type))
        //                          {
        //						//BIGViewer.Visibility = Visibility.Visible;
        //						//BIGViewer.AssetEntry = legacyFileEntry;
        //						//BIGViewer.ParentBrowser = this;
        //						//switch(legacyFileEntry.Type)
        //      //                        {
        //						//	default:
        //						//		BIGViewer.LoadBig();
        //						//		break;

        //						//}

        //						//btnImport.IsEnabled = true;
        //						//btnExport.IsEnabled = true;
        //						//btnRevert.IsEnabled = true;
        //                          }
        //					else
        //					{
        //						MainEditorWindow.Log("Loading Unknown Legacy File " + SelectedLegacyEntry.Filename);
        //						//btnExport.IsEnabled = true;
        //      //                        btnImport.IsEnabled = true;
        //      //                        btnRevert.IsEnabled = true;

        //						//unknownFileDocumentsPane.Children.Clear();
        //						//var newLayoutDoc = new LayoutDocument();
        //						//newLayoutDoc.Title = SelectedEntry.DisplayName;
        //						//WpfHexaEditor.HexEditor hexEditor = new WpfHexaEditor.HexEditor();
        //      //                        using (var nr = new NativeReader(ProjectManagement.Instance.Project.AssetManager.GetCustomAsset("legacy", legacyFileEntry)))
        //      //                        {
        //      //                            hexEditor.Stream = new MemoryStream(nr.ReadToEnd());
        //      //                        }
        //      //                        newLayoutDoc.Content = hexEditor;
        //						//hexEditor.BytesModified += HexEditor_BytesModified;
        //						//unknownFileDocumentsPane.Children.Insert(0, newLayoutDoc);
        //						//unknownFileDocumentsPane.SelectedContentIndex = 0;


        //						//UnknownFileViewer.Visibility = Visibility.Visible;
        //					}

        //				}

        //	}
        //	catch (Exception e)
        //	{
        //		MainEditorWindow.Log($"Failed to load file with message {e.Message}");
        //		Debug.WriteLine(e.ToString());

        //		//DisplayUnknownFileViewer(AssetManager.Instance.GetEbxStream(ebxEntry));

        //	}

        //	DataContext = null;
        //	DataContext = this;
        //}

        //private async Task OpenLTUAsset(LiveTuningUpdate.LiveTuningUpdateEntry entry)
        //{
        //          MainEditorWindow.Log("Loading EBX " + entry.Filename);
        //	var ebx = entry.GetAsset();
        //          //var successful = await EBXViewer.LoadEbx(entry, ebx, ProjectManagement.Instance.Project, MainEditorWindow);
        //          //EBXViewer.Visibility = Visibility.Visible;
        //      }

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

        private void BackupEBXViewer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //Xceed.Wpf.Toolkit.PropertyGrid.PropertyGrid propertyGrid = sender as Xceed.Wpf.Toolkit.PropertyGrid.PropertyGrid;
            //if (propertyGrid != null)
            //{
            //	var ebxAsset = AssetManager.Instance.GetEbx((EbxAssetEntry)SelectedEntry);
            //	ebxAsset.SetRootObject(propertyGrid.SelectedObject);
            //	AssetManager.Instance.ModifyEbx(SelectedEntry.Name, ebxAsset);
            //	UpdateAssetListView();
            //}
        }

        private void BackupEBXViewer_SelectedPropertyItemChanged(object sender, RoutedPropertyChangedEventArgs<Xceed.Wpf.Toolkit.PropertyGrid.PropertyItemBase> e)
        {
            Xceed.Wpf.Toolkit.PropertyGrid.PropertyGrid propertyGrid = sender as Xceed.Wpf.Toolkit.PropertyGrid.PropertyGrid;
            if (propertyGrid != null)
            {
                var ebxAsset = AssetManager.Instance.GetEbx((EbxAssetEntry)SelectedEntry);

                bool hasChanged = false;
                var obj1 = ebxAsset.RootObject;
                var obj2 = propertyGrid.SelectedObject;
                var props = obj1.GetProperties();
                foreach (var p in props)
                {
                    foreach (var p2 in Utilities.GetProperties(obj2))
                    {
                        if (p.Name == p2.Name)
                        {
                            if (!p.GetValue(obj1).Equals(p2.GetValue(obj2)))
                            {
                                hasChanged = true;
                                break;
                            }
                        }

                        if (hasChanged)
                            break;
                    }
                }
                if (hasChanged)
                {
                    ebxAsset.SetRootObject(propertyGrid.SelectedObject);
                    AssetManager.Instance.ModifyEbx(SelectedEntry.Name, ebxAsset);
                    //UpdateAssetListView();
                }
            }

        }

        private void BuildTextureViewerFromAssetEntry(ResAssetEntry res)
        {

            using (Texture textureAsset = new Texture(res))
            {
                //try
                //{
                //	ImageViewer.Source = null;
                //	CurrentDDSImageFormat = textureAsset.PixelFormat;


                //	var bPath = Directory.GetCurrentDirectory() + @"\temp.png";

                //	TextureExporter textureExporter = new TextureExporter();
                //	MemoryStream memoryStream = new MemoryStream();

                //                Stream expToStream = null;
                //                try
                //	{
                //                    expToStream = textureExporter.ExportToStream(textureAsset, TextureUtils.ImageFormat.PNG);
                //		expToStream.Position = 0;
                //                    //var ddsData = textureExporter.WriteToDDS(textureAsset);
                //                    //BuildTextureViewerFromStream(new MemoryStream(ddsData));

                //                }
                //	catch (Exception exception_ToStream)
                //	{
                //		MainEditorWindow.LogError($"Error loading texture with message :: {exception_ToStream.Message}");
                //		MainEditorWindow.LogError(exception_ToStream.ToString());
                //		ImageViewer.Source = null; ImageViewerScreen.Visibility = Visibility.Collapsed;

                //		textureExporter.Export(textureAsset, res.Filename + ".DDS", "*.DDS");
                //		MainEditorWindow.LogError($"As the viewer failed. The image has been exported to {res.Filename}.dds instead.");
                //		return;
                //	}

                //	//using var nr = new NativeReader(expToStream);
                //	//nr.Position = 0;
                //	//var textureBytes = nr.ReadToEnd();

                //	//ImageViewer.Source = LoadImage(textureBytes);
                //	ImageViewer.Source = LoadImage(((MemoryStream)expToStream).ToArray());
                //	ImageViewerScreen.Visibility = Visibility.Visible;
                //	ImageViewer.MaxHeight = textureAsset.Height;
                //	ImageViewer.MaxWidth = textureAsset.Width;

                //                btnExport.IsEnabled = true;
                //	btnImport.IsEnabled = true;
                //	btnRevert.IsEnabled = true;

                //}
                //catch (Exception e)
                //{
                //	MainEditorWindow.LogError($"Error loading texture with message :: {e.Message}");
                //	MainEditorWindow.LogError(e.ToString());
                //	ImageViewer.Source = null; ImageViewerScreen.Visibility = Visibility.Collapsed;
                //}
            }
        }

        public string CurrentDDSImageFormat { get; set; }

        //private void BuildTextureViewerFromStream(Stream stream, AssetEntry assetEntry = null)
        private void BuildTextureViewerFromStream(MemoryStream stream)
        {

            //try
            //{
            //	ImageViewer.Source = null;

            //	var bPath = Directory.GetCurrentDirectory() + @"\temp.png";

            //	ImageEngineImage imageEngineImage = new ImageEngineImage(((MemoryStream)stream).ToArray());
            //	var iData = imageEngineImage.Save(new ImageFormats.ImageEngineFormatDetails(ImageEngineFormat.BMP), MipHandling.KeepTopOnly, removeAlpha: false);

            //	ImageViewer.Source = LoadImage(iData);
            //	ImageViewerScreen.Visibility = Visibility.Visible;

            //	btnExport.IsEnabled = true;
            //	btnImport.IsEnabled = true;
            //	btnRevert.IsEnabled = true;

            //}
            //catch (Exception e)
            //{
            //	MainEditorWindow.LogError(e.Message);
            //	ImageViewer.Source = null; ImageViewerScreen.Visibility = Visibility.Collapsed;
            //}

        }

        private static System.Windows.Media.Imaging.BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new System.Windows.Media.Imaging.BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
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

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async void txtFilter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await Update();
            }
        }

        private void chkShowOnlyModified_Checked(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void chkShowOnlyModified_Unchecked(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void TextViewer_LostFocus(object sender, RoutedEventArgs e)
        {
            //var bytes = ASCIIEncoding.UTF8.GetBytes(TextViewer.Text);

            //if (SelectedLegacyEntry != null)
            //{
            //	AssetManager.Instance.ModifyLegacyAsset(SelectedLegacyEntry.Name
            //				, bytes
            //				, false);
            //	UpdateAssetListView();
            //}
        }

        private void TextBlock_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                TextBlock label = sender as TextBlock;
                if (label != null && label.Tag != null)
                {
                    SelectedAssetPath = label.Tag as AssetPath;
                    //UpdateAssetListView();
                }
            }
        }

        private void TextBlock_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock label = sender as TextBlock;
            if (label != null && label.Tag != null)
            {
                SelectedAssetPath = label.Tag as AssetPath;
                //UpdateAssetListView();
            }
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

        //private void assetListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    IAssetEntry entry = (IAssetEntry)((ListView)sender).SelectedItem;

        //    if (entry is EbxAssetEntry ebxAssetEntry)
        //        OpenAsset(ebxAssetEntry);
        //    else if (entry is LegacyFileEntry legacyFileEntry)
        //        OpenAsset(legacyFileEntry);
        //    else if (entry is LiveTuningUpdate.LiveTuningUpdateEntry ltuEntry)
        //        OpenAsset(ltuEntry);
        //}

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
            MenuItem parent = sender as MenuItem;
            if (parent != null)
            {
                var assetPath = parent.Tag as AssetPath;

                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.AllowMultiSelect = false;
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folder = folderBrowserDialog.SelectedFolder;
                    var ebxInPath = AssetManager.Instance.EnumerateEbx().Where(x => x.Name.Contains(assetPath.FullPath.Substring(1)));
                    if (ebxInPath.Any())
                    {
                        foreach (var ebx in ebxInPath)
                        {
                            await ExportAsset(ebx, folder);
                        }
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
                if (child.Title.StartsWith(assetEntry.Filename))
                {
                    browserDocuments.SelectedContentIndex = browserDocuments.Children.IndexOf(child);
                    return;
                }
            }
            // ---- ----------
            var layoutDocument = new LayoutDocument();
            layoutDocument.Title = assetEntry.Filename;
            layoutDocument.Content = new OpenedFile(assetEntry);
            browserDocuments.Children.Insert(0, layoutDocument);
            browserDocuments.SelectedContentIndex = 0;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _ = Update();
            RequiresRefresh = false;
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
