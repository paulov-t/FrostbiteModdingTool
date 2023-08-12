using Frostbite.Textures;
using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using FrostySdk.Resources;
using Ionic.Zip;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace FMT.Windows
{
    /// <summary>
    /// Interaction logic for KitCreatorImport.xaml
    /// </summary>
    public partial class KitCreatorImport : Window, ILogger
    {
        public KitCreatorImport()
        {
            InitializeComponent();

            DataContext = this;
        }

        public string SelectedFile { get; set; }
        public string TeamId { get; set; }
        public string KitType { get; set; }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                txtLog.Text = string.Empty;
                //btnImport.IsEnabled = false;
                this.IsEnabled = false;
                pSuccess.Visibility = Visibility.Collapsed;
                pFail.Visibility = Visibility.Collapsed;
            });

            if (string.IsNullOrEmpty(SelectedFile) || string.IsNullOrEmpty(txtTeamId.Text) || string.IsNullOrEmpty(txtKitType.Text))
            {
                LogError("Unable to import as not all fields have been filled");
                return;
            }

            ZipFile zip = new ZipFile(SelectedFile);
            var entries = zip.Entries.ToList();
            await Dispatcher.InvokeAsync(() =>
            {
                pBar.Minimum = 0;
                pBar.Maximum = entries.Count > 0 ? entries.Count : 1;
                pBar.Value = 0;
            });

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                var resEntryPath = AssetManager.Instance.RES.Keys.FirstOrDefault(x => x.StartsWith("content/character/kit/") && x.Contains("_" + txtTeamId.Text + "/") && x.Contains(txtTeamId.Text + "_" + KitType + "_"));
                if (resEntryPath != null)
                {
                    var lastIndexSlash = resEntryPath.LastIndexOf("/");
                    resEntryPath = resEntryPath.Substring(0, lastIndexSlash) + "/";

                    var middleSection = "_" + txtTeamId.Text + "_" + txtKitType.Text + "_0_";

                    using (var entryStream = new MemoryStream())
                    {
                        entry.Extract(entryStream);
                        switch (entry.FileName)
                        {
                            case "jersey_color.png":
                                resEntryPath += "jersey" + middleSection + "color";
                                break;
                            case "jersey_normal.png":
                                resEntryPath += "jersey" + middleSection + "normal";
                                break;
                            case "jersey_taa.png":
                                resEntryPath += "jersey" + middleSection + "taa";
                                break;
                            case "jersey_coeff.png":
                                resEntryPath += "jersey" + middleSection + "coeff";
                                break;

                            case "shorts_color.png":
                                resEntryPath += "shorts" + middleSection + "color";
                                break;
                            case "shorts_normal.png":
                                resEntryPath += "shorts" + middleSection + "normal";
                                break;
                            case "shorts_taa.png":
                                resEntryPath += "shorts" + middleSection + "taa";
                                break;
                            case "shorts_coeff.png":
                                resEntryPath += "shorts" + middleSection + "coeff";
                                break;

                            case "socks_color.png":
                                resEntryPath += "socks" + middleSection + "color";
                                break;
                            case "socks_normal.png":
                                resEntryPath += "socks" + middleSection + "normal";
                                break;
                            case "socks_taa.png":
                                resEntryPath += "socks" + middleSection + "taa";
                                break;
                            case "socks_coeff.png":
                                resEntryPath += "socks" + middleSection + "coeff";
                                break;


                        }

                        if (entry.FileName == "blank.png")
                        {
                            var resEntryBlankBrandJersey = AssetManager.Instance.GetResEntry(resEntryPath + "brand_jersey" + middleSection + "color");
                            if (resEntryBlankBrandJersey != null)
                            {
                                Texture texture = new Texture(resEntryBlankBrandJersey);
                                TextureImporter textureImporter = new TextureImporter();
                                EbxAssetEntry ebxAssetEntry = AssetManager.Instance.GetEbxEntry(resEntryPath + "brand_jersey" + middleSection + "color");

                                if (ebxAssetEntry != null)
                                {
                                    textureImporter.DoImportFromPNGMemoryStream(entryStream, ebxAssetEntry, ref texture);
                                    Log($"Imported {entry.FileName} to {resEntryBlankBrandJersey}");
                                }
                            }

                            var resEntryBlankBrandShorts = AssetManager.Instance.GetResEntry(resEntryPath + "brand_shorts" + middleSection + "color");
                            if (resEntryBlankBrandShorts != null)
                            {
                                Texture texture = new Texture(resEntryBlankBrandShorts);
                                TextureImporter textureImporter = new TextureImporter();
                                EbxAssetEntry ebxAssetEntry = AssetManager.Instance.GetEbxEntry(resEntryPath + "brand_shorts" + middleSection + "color");

                                if (ebxAssetEntry != null)
                                {
                                    textureImporter.DoImportFromPNGMemoryStream(entryStream, ebxAssetEntry, ref texture);
                                    Log($"Imported {entry.FileName} to {resEntryBlankBrandShorts}");
                                }
                            }

                            var resEntryBlankCrest = AssetManager.Instance.GetResEntry(resEntryPath + "crest" + middleSection + "color");
                            if (resEntryBlankCrest != null)
                            {
                                Texture texture = new Texture(resEntryBlankCrest);
                                TextureImporter textureImporter = new TextureImporter();
                                EbxAssetEntry ebxAssetEntry = AssetManager.Instance.GetEbxEntry(resEntryPath + "crest" + middleSection + "color");

                                if (ebxAssetEntry != null)
                                {
                                    textureImporter.DoImportFromPNGMemoryStream(entryStream, ebxAssetEntry, ref texture);
                                    Log($"Imported {entry.FileName} to {resEntryBlankCrest}");
                                }
                            }

                            var resEntryBlankCrestShorts = AssetManager.Instance.GetResEntry(resEntryPath + "crest_shorts" + middleSection + "color");
                            if (resEntryBlankCrestShorts != null)
                            {
                                Texture texture = new Texture(resEntryBlankCrestShorts);
                                TextureImporter textureImporter = new TextureImporter();
                                EbxAssetEntry ebxAssetEntry = AssetManager.Instance.GetEbxEntry(resEntryPath + "crest_shorts" + middleSection + "color");

                                if (ebxAssetEntry != null)
                                {
                                    textureImporter.DoImportFromPNGMemoryStream(entryStream, ebxAssetEntry, ref texture);
                                    Log($"Imported {entry.FileName} to {resEntryBlankCrestShorts}");
                                }
                            }

                        }
                        else if (entry.FileName == "minikit.png")
                        {
                            var miniKitLocation = "data/ui/imgAssets/kits/j" + txtKitType.Text + "_" + txtTeamId.Text + "_0.dds";
                            var miniKitLegacyAsset = AssetManager.Instance.GetCustomAssetEntry("legacy", miniKitLocation);
                            if (miniKitLegacyAsset != null)
                            {
                                if (AssetManager.Instance.DoLegacyImageImport(entryStream, (LegacyFileEntry)miniKitLegacyAsset))
                                {
                                    Log($"Imported {entry.FileName} to {miniKitLocation}");
                                }
                            }

                        }
                        else
                        {
                            var resEntry = AssetManager.Instance.GetResEntry(resEntryPath);
                            if (resEntry != null)
                            {
                                Texture texture = new Texture(resEntry);
                                TextureImporter textureImporter = new TextureImporter();
                                EbxAssetEntry ebxAssetEntry = AssetManager.Instance.GetEbxEntry(resEntryPath);

                                if (ebxAssetEntry != null)
                                {
                                    textureImporter.DoImportFromPNGMemoryStream(entryStream, ebxAssetEntry, ref texture);
                                    Log($"Imported {entry.FileName} to {resEntryPath}");
                                }
                            }
                        }

                    }

                }

                Dispatcher.Invoke(() =>
                {
                    pBar.Value = i;
                });
            }

            Dispatcher.Invoke(() =>
            {
                //btnImport.IsEnabled = true;
                this.IsEnabled = true;
                pSuccess.Visibility = Visibility.Visible;
            });

        }



        private void btnKitCreatorZip_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Zip files|*.zip";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SelectedFile = openFileDialog.FileName;
                DataContext = null;
                DataContext = this;
            }
        }

        public void Log(string text, params object[] vars)
        {
            txtLog.Text += text + Environment.NewLine;
        }

        public void LogWarning(string text, params object[] vars)
        {
            txtLog.Text += text + Environment.NewLine;
        }

        public void LogError(string text, params object[] vars)
        {
            txtLog.Text += text + Environment.NewLine;
        }
    }
}
