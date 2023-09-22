using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using NetDiscordRpc.Core.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FC24Plugin
{

    public class FC24AssetLoader : IAssetLoader
    {
        public void LoadData(AssetManager assetManager, BinarySbDataHelper helper, string folder = "native_data/")
        {
            if (assetManager == null || assetManager.FileSystem.SuperBundles.Count() == 0)
                return;

            int sbIndex = -1;

            //foreach (var catalog in assetManager.FileSystem.CatalogObjects)
            //{
            //    foreach (var sb in catalog.SuperBundles)
            //    {
                    //var sbName = sb.Key;

            foreach(var sbName in assetManager.FileSystem.SuperBundles)
            { 

#if DEBUG
                    if (sbName.Contains("globalsfull"))
                    {

                    }
#endif
                    var tocFileRAW = $"{folder}{sbName}.toc";
                    string tocFileLocation = assetManager.FileSystem.ResolvePath(tocFileRAW);
                    if (string.IsNullOrEmpty(tocFileLocation) || !File.Exists(tocFileLocation))
                    {
                        AssetManager.Instance.Logger.LogWarning($"Unable to find Toc {tocFileRAW}");
                        continue;
                    }

                    assetManager.Logger.Log($"Loading data ({tocFileRAW})");
                    using FC24TOCFile tocFile = new FC24TOCFile(tocFileRAW, true, true, false, sbIndex, false);
                    sbIndex++;
                }
            //}
        }

        public void LoadPatch(AssetManager parent, BinarySbDataHelper helper)
        {
            LoadData(parent, helper, "native_patch/");
        }

        public void Load(AssetManager parent, BinarySbDataHelper helper)
        {
            LoadPatch(parent, helper);
            LoadData(parent, helper);
        }

    }


}
