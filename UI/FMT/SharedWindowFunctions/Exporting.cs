using CSharpImageLibrary;
using FMT.FileTools;
using Frostbite.Textures;
using FrostbiteModdingUI.Windows;
using FrostySdk;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.Managers;
using FrostySdk.Resources;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FMT.SharedWindowFunctions
{
    internal class Exporting
    {
        private IEditorWindow MainEditorWindow
        {
            get
            {
                return App.MainEditorWindow as IEditorWindow;
            }
        }

        public async Task Export(IAssetEntry asset) 
        {

            if (asset == null)
            {
                return;
            }

            if (asset is LegacyFileEntry SelectedLegacyEntry)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                var filt = "*." + SelectedLegacyEntry.Type;
                if (SelectedLegacyEntry.Type == "DDS")
                    saveFileDialog.Filter = "Image files (*.png,*.dds)|*.png;*.dds;";
                else
                    saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;

                saveFileDialog.FileName = SelectedLegacyEntry.Filename;

                if (saveFileDialog.ShowDialog().Value)
                {
                    await ExportAsset(SelectedLegacyEntry, saveFileDialog.FileName);
                }
            }
            else if (asset is EbxAssetEntry ebxAssetEntry)
            {
                if (ebxAssetEntry.Type == "TextureAsset")
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    //var imageFilter = "Image files (*.DDS, *.PNG)|*.DDS;*.PNG";
                    var imageFilter = "PNG files (*.PNG)|*.PNG|BIN files (*.BIN)|*.BIN|DDS files (*.DDS)|*.DDS";
                    saveFileDialog.Filter = imageFilter;
                    saveFileDialog.FileName = ebxAssetEntry.Filename;
                    saveFileDialog.AddExtension = true;
                    if (saveFileDialog.ShowDialog().Value)
                    {
                        await ExportAsset(ebxAssetEntry, saveFileDialog.FileName);
                    }
                }

                else if (ebxAssetEntry.Type == "HotspotDataAsset")
                {
                    var ebx = AssetManager.Instance.GetEbx((EbxAssetEntry)ebxAssetEntry);
                    if (ebx != null)
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        var filt = "*.json";
                        saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                        saveFileDialog.FileName = ebxAssetEntry.Filename;
                        var dialogAnswer = saveFileDialog.ShowDialog();
                        if (dialogAnswer.HasValue && dialogAnswer.Value)
                        {
                            var json = JsonConvert.SerializeObject(ebx.RootObject, Formatting.Indented);
                            File.WriteAllText(saveFileDialog.FileName, json);
                            MainEditorWindow.Log($"Exported {ebxAssetEntry.Filename} to {saveFileDialog.FileName}");
                        }
                    }
                    else
                    {
                        MainEditorWindow.Log("Failed to export file");
                    }
                }

                else if (ebxAssetEntry.Type == "SkinnedMeshAsset")
                {
                    var skinnedMeshEntry = (EbxAssetEntry)ebxAssetEntry;
                    if (skinnedMeshEntry == null)
                        return;

                    {

                        var skinnedMeshEbx = AssetManager.Instance.GetEbx(skinnedMeshEntry);
                        if (skinnedMeshEbx == null)
                            return;

                        {

                            var skeletonEntryText = "content/character/rig/skeleton/player/skeleton_player";
                            var fifaMasterSkeleton = AssetManager.Instance.EBX.ContainsKey(skeletonEntryText);
                            if (!fifaMasterSkeleton)
                            {
                                MeshSkeletonSelector meshSkeletonSelector = new MeshSkeletonSelector();
                                var meshSelectorResult = meshSkeletonSelector.ShowDialog();
                                if (meshSelectorResult.HasValue && meshSelectorResult.Value)
                                {
                                    if (!meshSelectorResult.Value)
                                    {
                                        MessageBox.Show("Cannot export without a Skeleton");
                                        return;
                                    }

                                    skeletonEntryText = meshSkeletonSelector.AssetEntry.Name;

                                }
                                else
                                {
                                    MessageBox.Show("Cannot export without a Skeleton");
                                    return;
                                }
                            }

                            SaveFileDialog saveFileDialog = new SaveFileDialog();
                            var filt = "*.fbx";
                            saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                            saveFileDialog.FileName = ebxAssetEntry.Filename;
                            var dialogAnswer = saveFileDialog.ShowDialog();
                            if (dialogAnswer.HasValue && dialogAnswer.Value)
                            {
                                var exporter = new MeshSetToFbxExport();
                                MeshSet meshSet = exporter.LoadMeshSet(skinnedMeshEntry);
                                exporter.Export(AssetManager.Instance
                                    , skinnedMeshEbx.RootObject
                                    , saveFileDialog.FileName, "FBX_2012", "Meters", true, skeletonEntryText, "*.fbx", meshSet);


                                MainEditorWindow.Log($"Exported {ebxAssetEntry.Name} to {saveFileDialog.FileName}");


                            }
                        }
                    }
                }
                else if (ebxAssetEntry.Type == "CompositeMeshAsset" || ebxAssetEntry.Type == "RigidMeshAsset")
                {
                    var rigidMeshEntry = (EbxAssetEntry)ebxAssetEntry;
                    if (rigidMeshEntry != null)
                    {

                        var skinnedMeshEbx = AssetManager.Instance.GetEbx(rigidMeshEntry);
                        if (skinnedMeshEbx != null)
                        {

                            var skeletonEntryText = "content/character/rig/skeleton/player/skeleton_player";
                            var fifaMasterSkeleton = AssetManager.Instance.EBX.ContainsKey(skeletonEntryText);
                            if (!fifaMasterSkeleton)
                            {
                                MeshSkeletonSelector meshSkeletonSelector = new MeshSkeletonSelector();
                                var meshSelectorResult = meshSkeletonSelector.ShowDialog();
                                if (meshSelectorResult.HasValue && meshSelectorResult.Value)
                                {
                                    if (!meshSelectorResult.Value)
                                    {
                                        MessageBox.Show("Cannot export without a Skeleton");
                                        return;
                                    }

                                    skeletonEntryText = meshSkeletonSelector.AssetEntry.Name;

                                }
                                else
                                {
                                    MessageBox.Show("Cannot export without a Skeleton");
                                    return;
                                }
                            }

                            SaveFileDialog saveFileDialog = new SaveFileDialog();
                            var filt = "*.fbx";
                            saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                            saveFileDialog.FileName = ebxAssetEntry.Filename;
                            var dialogAnswer = saveFileDialog.ShowDialog();
                            if (dialogAnswer.HasValue && dialogAnswer.Value)
                            {
                                var exporter = new MeshSetToFbxExport();
                                MeshSet meshSet = exporter.LoadMeshSet(rigidMeshEntry);
                                exporter.Export(AssetManager.Instance
                                    , skinnedMeshEbx.RootObject
                                    , saveFileDialog.FileName, "FBX_2012", "Meters", true, null, "*.fbx", meshSet);


                                MainEditorWindow.Log($"Exported {ebxAssetEntry.Name} to {saveFileDialog.FileName}");


                            }
                        }
                    }
                }

                else
                {
                    var ebx = AssetManager.Instance.GetEbxStream((EbxAssetEntry)ebxAssetEntry);
                    if (ebx != null)
                    {
                        MessageBoxResult useJsonResult = MessageBox.Show(
                                                                "Would you like to Export as JSON?"
                                                                , "Export as JSON?"
                                                                , MessageBoxButton.YesNoCancel);
                        if (useJsonResult == MessageBoxResult.Yes)
                        {
                            SaveFileDialog saveFileDialog = new SaveFileDialog();
                            var filt = "*.json";
                            saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                            saveFileDialog.FileName = ebxAssetEntry.Filename;
                            var dialogAnswer = saveFileDialog.ShowDialog();
                            if (dialogAnswer.HasValue && dialogAnswer.Value)
                            {

                                var obj = await Task.Run(() =>
                                {
                                    return AssetManager.Instance.GetEbx((EbxAssetEntry)ebxAssetEntry).RootObject;
                                });
                                var serialisedObj = await Task.Run(() =>
                                {
                                    return JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings()
                                    {
                                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                        MaxDepth = 4,
                                    });
                                });
                                await File.WriteAllTextAsync(saveFileDialog.FileName, serialisedObj);
                                MainEditorWindow.Log($"Exported {ebxAssetEntry.Filename} to {saveFileDialog.FileName}");

                            }
                        }
                        else if (useJsonResult == MessageBoxResult.No)
                        {
                            SaveFileDialog saveFileDialog = new SaveFileDialog();
                            var filt = "*.bin";
                            saveFileDialog.Filter = filt.Split('.')[1] + " files (" + filt + ")|" + filt;
                            saveFileDialog.FileName = ebxAssetEntry.Filename;
                            var dialogAnswer = saveFileDialog.ShowDialog();
                            if (dialogAnswer.HasValue && dialogAnswer.Value)
                            {
                                File.WriteAllBytes(saveFileDialog.FileName, ((MemoryStream)ebx).ToArray());
                                MainEditorWindow.Log($"Exported {ebxAssetEntry.Filename} to {saveFileDialog.FileName}");
                            }
                        }
                        else
                        {

                        }



                    }
                    else
                    {
                        MainEditorWindow.Log("Failed to export file");
                    }
                }
            }




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
                            Texture texture = new Texture(resStream, resEntry);
                            TextureExporter textureExporter = new TextureExporter();
                            if (isFolder)
                                saveLocation += ".png";

                            textureExporter.Export(texture, saveLocation, "*" + new FileInfo(saveLocation).Extension.ToLower());

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
    }
}
