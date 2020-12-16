﻿using Frostbite.Textures;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using KUtility;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using v2k4FIFAModding.Frosty;
using Xceed.Wpf.AvalonDock.Converters;
using FrostySdk.FrostySdk.Managers;
using Microsoft.Win32;
using FIFAModdingUI.Windows;
using Newtonsoft.Json;
using FrostbiteModdingUI.Windows;

namespace FIFAModdingUI.Pages.Common
{
    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class Browser : UserControl
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
        }


        private List<IAssetEntry> allAssets;

        public List<IAssetEntry> AllAssetEntries
        {
            get { return allAssets; }
            set { allAssets = value; Update(); }
        }

		public string FilterText { get; set; }

		public async Task<List<IAssetEntry>> GetFilteredAssetEntries()
		{

			return await Dispatcher.InvokeAsync(() =>
			{
				var assets = allAssets;
				if (assets != null)
				{
					bool OnlyModified 
					= chkShowOnlyModified.IsChecked.HasValue 
					&& chkShowOnlyModified.IsChecked.Value;

					if (!string.IsNullOrEmpty(txtFilter.Text))
					{
						assets = assets.Where(x =>
							x.Name.Contains(txtFilter.Text)
							).ToList();
					}

					assets = assets.Where(x => 
						
						(
						chkShowOnlyModified.IsChecked == true 
						&& x.IsModified
						)
						|| chkShowOnlyModified.IsChecked == false
						
						).ToList();

					//assets = assets.Where(
					//	x =>
					//	(chkShowOnlyModified.IsChecked == true && x.IsModified)
					//	|| chkShowOnlyModified.IsChecked == false

					//	//(chkShowOnlyModified.IsChecked.HasValue
					//	//&& chkShowOnlyModified.IsChecked.Value == true
					//	//&& x.IsModified == true)
					//	//|| 
					//	//(chkShowOnlyModified.IsChecked == false)
					//	).ToList();

				}
				return assets;

			});

		}

		public List<string> CurrentAssets { get; set; }

        public int CurrentTier { get; set; }

        public string CurrentPath { get; set; }

        public void SelectOption(string name)
        {

        }

        public void BrowseTo(string path)
        {
            if(!string.IsNullOrEmpty(CurrentPath))
                CurrentPath = CurrentPath + "/" + path;
            else
                CurrentPath = path;

            Update();
        }

		private Dictionary<string, AssetPath> assetPathMapping = new Dictionary<string, AssetPath>(StringComparer.OrdinalIgnoreCase);
		private AssetPath selectedPath;

		public async void Update()
        {
			AssetPath assetPath = new AssetPath("", "", null);

			var assets = await GetFilteredAssetEntries();

			foreach (AssetEntry item in assets)
			{
				//if ((!ShowOnlyModified || item.IsModified) && FilterText(item.Name, item))
				{
					string[] array = item.Path.Split(new char[1]
					{
							'/'
					}, StringSplitOptions.RemoveEmptyEntries);
					AssetPath assetPath2 = assetPath;
					string[] array2 = array;
					foreach (string text in array2)
					{
						bool flag = false;
						foreach (AssetPath child in assetPath2.Children)
						{
							if (child.PathName.Equals(text, StringComparison.OrdinalIgnoreCase))
							{
								if (text.ToCharArray().Any((char a) => char.IsUpper(a)))
								{
									child.UpdatePathName(text);
								}
								assetPath2 = child;
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							string text2 = assetPath2.FullPath + "/" + text;
							AssetPath assetPath3 = null;
							if (!assetPathMapping.ContainsKey(text2))
							{
								assetPath3 = new AssetPath(text, text2, assetPath2);
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
            assetPath.Children.Insert(0, assetPathMapping["/"]);

			//Dispatcher.BeginInvoke((Action)(() =>
			//{
			//	assetTreeView.ItemsSource = assetPath.Children;
			//	assetTreeView.Items.SortDescriptions.Add(new SortDescription("PathName", ListSortDirection.Ascending));
			//}));

			await Dispatcher.InvokeAsync((Action)(() =>
			{
				assetTreeView.ItemsSource = assetPath.Children;
				assetTreeView.Items.SortDescriptions.Add(new SortDescription("PathName", ListSortDirection.Ascending));
			}));





		}

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
			var imageFilter = "Image files (*.DDS, *.PNG)|*.DDS;*.PNG";
			if (SelectedLegacyEntry != null)
			{
				OpenFileDialog openFileDialog = new OpenFileDialog();
				openFileDialog.Filter = $"Files (*.{SelectedLegacyEntry.Type})|*.{SelectedLegacyEntry.Type}";
				openFileDialog.FileName = SelectedLegacyEntry.Filename;

				bool isImage = false;
				if (SelectedLegacyEntry.Type == "DDS")
				{
					openFileDialog.Filter = imageFilter;
					isImage = true;
				}

				var result = openFileDialog.ShowDialog();
				if (result.HasValue && result.Value == true)
				{
					byte[] bytes = File.ReadAllBytes(openFileDialog.FileName);

					if (isImage)
					{
						using (var nr = new NativeReader(ProjectManagement.Instance.FrostyProject.AssetManager.GetCustomAsset("legacy", SelectedLegacyEntry)))
						{
							try
							{
								var oldImage = new CSharpImageLibrary.ImageEngineImage(nr.ReadToEnd());

								var newImage = new CSharpImageLibrary.ImageEngineImage(openFileDialog.FileName);
								if (oldImage.Header.Width != newImage.Header.Width)
									throw new Exception("Image size is incorrect");

								bytes = newImage.Save(oldImage.FormatDetails, CSharpImageLibrary.MipHandling.KeepExisting);
								ImageViewer.Source = newImage.GetWPFBitmap();

							}
							catch (Exception ConversionEx)
							{
								MainEditorWindow.LogError(ConversionEx.Message);
							}
						}

					}
					else
                    {
						TextViewer.Text = ASCIIEncoding.ASCII.GetString(bytes);
                    }

					AssetManager.Instance.ModifyCustomAsset("legacy"
						, SelectedLegacyEntry.Name
						, bytes);


				}
			}
			else if (SelectedEntry != null)
			{
				if (SelectedEntry.Type == "TextureAsset" || SelectedEntry.Type == "Texture")
				{
					OpenFileDialog openFileDialog = new OpenFileDialog();
					openFileDialog.Filter = imageFilter;
					openFileDialog.FileName = SelectedEntry.Filename;
					if (openFileDialog.ShowDialog().Value)
					{
						var resEntry = ProjectManagement.Instance.FrostyProject.AssetManager.GetResEntry(SelectedEntry.Name);
						if (resEntry != null)
						{

							using (var resStream = ProjectManagement.Instance.FrostyProject.AssetManager.GetRes(resEntry))
							{
								Texture texture = new Texture(resStream, ProjectManagement.Instance.FrostyProject.AssetManager);
								TextureImporter textureImporter = new TextureImporter();
								EbxAssetEntry ebxAssetEntry = SelectedEntry as EbxAssetEntry;
								ResAssetEntry resAssetEntry = SelectedEntry as ResAssetEntry;

								if (ebxAssetEntry != null)
									textureImporter.Import(openFileDialog.FileName, ebxAssetEntry, ref texture);
								else if (resAssetEntry != null)
								{
									textureImporter.ImportTextureFromFileToTextureAsset(openFileDialog.FileName, ref texture, out string Message); 
									MainEditorWindow.Log($"{Message}");
									UpdateAssetListView();



								}

								if (ebxAssetEntry != null)
								{
									var res = AssetManager.Instance.GetResEntry(ebxAssetEntry.Name);
									if (res != null)
									{
										BuildTextureViewerFromAssetEntry(res);
									}
								}

								MainEditorWindow.Log($"Imported {openFileDialog.FileName} to {SelectedEntry.Filename}");
							}
						}
					}
				}

				if (SelectedEntry.Type == "HotspotDataAsset")
				{
					OpenFileDialog openFileDialog = new OpenFileDialog();
					var filt = "*.json";
					openFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
					openFileDialog.FileName = SelectedEntry.Filename;
					var dialogResult = openFileDialog.ShowDialog();
					if (dialogResult.HasValue && dialogResult.Value)
					{
						var ebx = AssetManager.Instance.GetEbx((EbxAssetEntry)SelectedEntry);
						if(ebx != null)
                        {
							var robj = (dynamic)ebx.RootObject;
							var fileHS = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(openFileDialog.FileName)).Hotspots;
							var fhs2 = fileHS.ToObject<List<dynamic>>();
							for (var i = 0; i < robj.Hotspots.Count; i++)
							{
								robj.Hotspots[i].Bounds.x = (float)fhs2[i].Bounds.x;
								robj.Hotspots[i].Bounds.y = (float)fhs2[i].Bounds.y;
								robj.Hotspots[i].Bounds.z = (float)fhs2[i].Bounds.z;
							}
							AssetManager.Instance.ModifyEbx(SelectedEntry.Name, ebx);
							UpdateAssetListView();

							EBXViewer.Visibility = Visibility.Collapsed;
						}
					}
				}
			}
		}

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
			if (SelectedLegacyEntry != null)
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog();
				var filt = "*." + SelectedLegacyEntry.Type;
				saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
				saveFileDialog.FileName = SelectedLegacyEntry.Filename;
				if (saveFileDialog.ShowDialog().Value)
				{
					using (NativeWriter nativeWriter = new NativeWriter(new FileStream(saveFileDialog.FileName, FileMode.Create)))
					{
						nativeWriter.Write(new NativeReader(ProjectManagement.Instance.FrostyProject.AssetManager.GetCustomAsset("legacy", SelectedLegacyEntry)).ReadToEnd());
						MainEditorWindow.Log($"Exported {SelectedLegacyEntry.Filename} to {saveFileDialog.FileName}");
					}
				}
			}

			if (SelectedEntry != null)
			{
				if (SelectedEntry.Type == "TextureAsset")
				{
					SaveFileDialog saveFileDialog = new SaveFileDialog();
					var filt = "*.DDS";
					saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
					saveFileDialog.FileName = SelectedEntry.Filename;
					if (saveFileDialog.ShowDialog().Value)
					{
						var resEntry = ProjectManagement.Instance.FrostyProject.AssetManager.GetResEntry(SelectedEntry.Name);
						if(resEntry != null) 
						{

							using (var resStream = ProjectManagement.Instance.FrostyProject.AssetManager.GetRes(resEntry))
                            {
								Texture texture = new Texture(resStream, ProjectManagement.Instance.FrostyProject.AssetManager);
								TextureExporter textureExporter = new TextureExporter();
								textureExporter.Export(texture, saveFileDialog.FileName, "*.dds");
								MainEditorWindow.Log($"Exported {SelectedEntry.Filename} to {saveFileDialog.FileName}");

							}


						}
					}
				}

				if (SelectedEntry.Type == "HotspotDataAsset")
				{
					var ebx = AssetManager.Instance.GetEbx((EbxAssetEntry)SelectedEntry);
					if (ebx != null)
					{
						SaveFileDialog saveFileDialog = new SaveFileDialog();
						var filt = "*.json";
						saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
						saveFileDialog.FileName = SelectedEntry.Filename;
						var dialogAnswer = saveFileDialog.ShowDialog();
						if (dialogAnswer.HasValue && dialogAnswer.Value)
						{
							var json = JsonConvert.SerializeObject(ebx.RootObject, Formatting.Indented);
							File.WriteAllText(saveFileDialog.FileName, json);
							MainEditorWindow.Log($"Exported {SelectedEntry.Filename} to {saveFileDialog.FileName}");
						}
					}
					else
                    {
						MainEditorWindow.Log("Failed to export file");
                    }
				}
			}
		}

		AssetPath SelectedAssetPath = null;

        private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        {
			Label label = sender as Label;
			if (label != null && label.Tag != null) 
			{
				SelectedAssetPath = label.Tag as AssetPath;
				UpdateAssetListView();
			}
        }

		private void Label_KeyUp(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Enter || e.Key == Key.Return)
            {
				Label label = sender as Label;
				if (label != null && label.Tag != null)
				{
					SelectedAssetPath = label.Tag as AssetPath;
					UpdateAssetListView();
				}
			}
		}

		public void UpdateAssetListView()
        {
			if (SelectedAssetPath != null)
			{
				if (SelectedAssetPath.FullPath.Length > 3)
				{
					var filterPath = (SelectedAssetPath.FullPath.Substring(1, SelectedAssetPath.FullPath.Length - 1));
					var filteredAssets = AllAssetEntries.Where(x => x.Path.ToLower() == filterPath.ToLower());

					filteredAssets = filteredAssets.Where(x =>

						(
						chkShowOnlyModified.IsChecked == true
						&& x.IsModified
						)
						|| chkShowOnlyModified.IsChecked == false

						).ToList();

					assetListView.ItemsSource = filteredAssets.OrderBy(x => x.Name);

				}
			}
		}

		public AssetEntry SelectedEntry;
		public LegacyFileEntry SelectedLegacyEntry;

        private void AssetEntry_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
			OpenAsset(sender);
		}

		private void AssetEntry_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.Key == Key.Return)
			{
				OpenAsset(sender);
			}
		}

		private void OpenAsset(object sender)
		{
			try
			{
				SelectedEntry = null;
				SelectedLegacyEntry = null;
				btnImport.IsEnabled = false;
				btnExport.IsEnabled = false;
				btnRevert.IsEnabled = false;

				ImageViewerScreen.Visibility = Visibility.Collapsed;
				TextViewer.Visibility = Visibility.Collapsed;
				EBXViewer.Visibility = Visibility.Collapsed;
				UnknownLegacyFileViewer.Visibility = Visibility.Collapsed;
				HEXViewer.Visibility = Visibility.Collapsed;

				Control control = sender as Control;
				if (control != null)
				{
					EbxAssetEntry ebxEntry = control.Tag as EbxAssetEntry;
					if (ebxEntry != null)
					{
						SelectedEntry = ebxEntry;
						if (ebxEntry.Type == "TextureAsset")
						{
							try
							{
								MainEditorWindow.Log("Loading Texture " + ebxEntry.Filename);

								var eb = AssetManager.Instance.GetEbx(ebxEntry);
								if (eb != null)
								{
									var res = AssetManager.Instance.GetResEntry(ebxEntry.Name);
									if (res != null)
									{
										BuildTextureViewerFromAssetEntry(res);
									}
								}
							}
							catch (Exception)
							{
								MainEditorWindow.Log("Failed to load texture");
							}



						}
						else
						{
							if (ebxEntry == null || ebxEntry.Type == "EncryptedAsset")
							{
								return;
							}
							EBXViewer.Children.Clear();
							var ebx = ProjectManagement.Instance.FrostyProject.AssetManager.GetEbx(ebxEntry);
							if (ebx != null)
							{
								MainEditorWindow.Log("Loading EBX " + ebxEntry.Filename);

								EBXViewer.Children.Add(new Editor(ebxEntry, ebx, ProjectManagement.Instance.FrostyProject));
								EBXViewer.Visibility = Visibility.Visible;
								btnRevert.IsEnabled = true;
								if (ebxEntry.Type == "HotspotDataAsset")
								{
									btnImport.IsEnabled = true;
									btnExport.IsEnabled = true;
								}
							}
						}
					}

					else
					{

						ResAssetEntry resEntry = control.Tag as ResAssetEntry;
						if (resEntry != null)
						{
							SelectedEntry = resEntry;
							if (SelectedEntry.Type == "Texture")
							{
								try
								{
									MainEditorWindow.Log("Loading Texture " + SelectedEntry.Filename);

									BuildTextureViewerFromAssetEntry(resEntry);
								}
								catch (Exception)
								{
									MainEditorWindow.Log("Failed to load texture");
								}



							}

						}
						else
						{

							LegacyFileEntry legacyFileEntry = control.Tag as LegacyFileEntry;
							if (legacyFileEntry != null)
							{
								SelectedLegacyEntry = legacyFileEntry;
								btnImport.IsEnabled = true;

								List<string> textViewers = new List<string>()
						{
							"LUA",
							"XML",
							"INI",
							"NAV",
							"JSON",
							"CSV"
						};

								List<string> imageViewers = new List<string>()
						{
							"PNG",
							"DDS"
						};

								if (textViewers.Contains(legacyFileEntry.Type))
								{
									MainEditorWindow.Log("Loading Legacy File " + SelectedLegacyEntry.Filename);

									btnExport.IsEnabled = true;
									TextViewer.Visibility = Visibility.Visible;
									using (var nr = new NativeReader(ProjectManagement.Instance.FrostyProject.AssetManager.GetCustomAsset("legacy", legacyFileEntry)))
									{
										TextViewer.Text = ASCIIEncoding.ASCII.GetString(nr.ReadToEnd());
									}
								}
								else if (imageViewers.Contains(legacyFileEntry.Type))
								{
									MainEditorWindow.Log("Loading Legacy File " + SelectedLegacyEntry.Filename);
									btnImport.IsEnabled = true;
									btnExport.IsEnabled = true;
									ImageViewerScreen.Visibility = Visibility.Visible;
									using (var nr = new NativeReader(ProjectManagement.Instance.FrostyProject.AssetManager.GetCustomAsset("legacy", legacyFileEntry)))
									{
										var oldImage = new CSharpImageLibrary.ImageEngineImage(nr.ReadToEnd());
										ImageViewer.Source = oldImage.GetWPFBitmap();
									}
								}
								else
								{
									MainEditorWindow.Log("Loading Unknown Legacy File " + SelectedLegacyEntry.Filename);
									btnExport.IsEnabled = true;
									UnknownLegacyFileViewer.Visibility = Visibility.Visible;
									//HEXViewer.Visibility = Visibility.Visible;
									//byte[] bytes;
									//using (var nr = new NativeReader(ProjectManagement.Instance.FrostyProject.AssetManager.GetCustomAsset("legacy", legacyFileEntry)))
									//	bytes = nr.ReadToEnd();

									//if (File.Exists("HexViewer.dat"))
									//	File.Delete("HexViewer.dat");

									//File.WriteAllBytes("HexViewer.dat", bytes);
									//HEXViewer.FileName = "HexViewer.dat";
								}

							}
						}

					}
				}
			}
			catch
			{
				MainEditorWindow.Log("Failed to load file");

			}
		}

		private void BuildTextureViewerFromAssetEntry(ResAssetEntry res)
        {
			using (var resStream = ProjectManagement.Instance.FrostyProject.AssetManager.GetRes(res))
			{
				using (Texture textureAsset = new Texture(resStream, ProjectManagement.Instance.FrostyProject.AssetManager))
				{
					try
					{
						ImageViewer.Source = null;

						var bPath = Directory.GetCurrentDirectory() + @"\temp.png";

						TextureExporter textureExporter = new TextureExporter();
						MemoryStream memoryStream = new MemoryStream();
						var textureBytes = new NativeReader(textureExporter.ExportToSteam(textureAsset)).ReadToEnd();

						var bImage = LoadImage(textureBytes);
						ImageViewer.Source = bImage;
						ImageViewerScreen.Visibility = Visibility.Visible;

						lblImageName.Content = res.Filename;
						lblImageDDSType.Content = textureAsset.PixelFormat;
						lblImageRESType.Content = textureAsset.Type;
						lblImageSize.Content = textureAsset.Width + "x" + textureAsset.Height;
						lblImageCASFile.Content = res.ExtraData.CasPath;
						lblImageCASOffset.Content = res.ExtraData.DataOffset;
						lblImageBundleFile.Content = !string.IsNullOrEmpty(res.SBFileLocation) ? res.SBFileLocation : res.TOCFileLocation;

						btnExport.IsEnabled = true;
						btnImport.IsEnabled = true; 
						btnRevert.IsEnabled = true;

					}
					catch { ImageViewer.Source = null; ImageViewerScreen.Visibility = Visibility.Collapsed; }
				}
			}
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

        private void btnRevert_Click(object sender, RoutedEventArgs e)
        {
			if(SelectedEntry.Type == "TextureAsset" || SelectedEntry.Type == "Texture")
            {
				AssetManager.Instance.RevertAsset(SelectedEntry);
			}
			else if (EBXViewer.Children.Count > 0)
			{
				var ebxviewer = EBXViewer.Children[0] as Editor;
				if (ebxviewer != null)
				{
					ebxviewer.RevertAsset();
				}
			}
			

        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
			//Update();
        }

        private void txtFilter_KeyUp(object sender, KeyEventArgs e)
        {
			if (e.Key == Key.Enter)
			{
				Update();
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
			var bytes = ASCIIEncoding.ASCII.GetBytes(TextViewer.Text);

			AssetManager.Instance.ModifyCustomAsset("legacy"
						, SelectedLegacyEntry.Name
						, bytes);
		}
    }

    internal class AssetPath
	{
		private static ImageSource ClosedImage;// = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/CloseFolder.png") as ImageSource;

		private static ImageSource OpenImage;// = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/OpenFolder.png") as ImageSource;

		private string fullPath;

		private string name;

		private bool expanded;

		private bool selected;

		private bool root;

		private AssetPath parent;

		private List<AssetPath> children = new List<AssetPath>();

		public string DisplayName => name.Trim('!');

		public string PathName => name;

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