///
///
/// ORIGINAL CODE: https://github.com/CadeEvs/FrostyToolsuite/blob/1.0.7/FrostySdk/Managers/Loaders/ManifestAssetLoader.cs
///
///
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;

namespace FrostySdk.Frostbite.Loaders
{

    /// <summary>
    /// ORIGINAL CODE: https://github.com/CadeEvs/FrostyToolsuite/blob/1.0.7/FrostySdk/Managers/Loaders/ManifestAssetLoader.cs
    /// </summary>
    public class ManifestAssetLoader : IAssetLoader
    {
        public void Load(AssetManager parent, BinarySbDataHelper helper)
        {
            parent.WriteToLog("Loading data from manifest");

            parent.superBundles.Add(new SuperBundleEntry { Name = "<none>" });

            foreach (DbObject bundle in parent.FileSystem.EnumerateBundles())
            {
                BundleEntry be = new BundleEntry { Name = bundle.GetValue<string>("name"), SuperBundleId = 0 };
                parent.Bundles.Add(be);

                if (bundle == null)
                    continue;

                // process assets
                parent.ProcessBundleEbx(bundle, parent.Bundles.Count - 1, helper);
                parent.ProcessBundleRes(bundle, parent.Bundles.Count - 1, helper);
                parent.ProcessBundleChunks(bundle, parent.Bundles.Count - 1, helper);
            }

            foreach (ChunkAssetEntry entry in parent.FileSystem.ProcessManifestChunks())
            {
                if (!parent.Chunks.ContainsKey(entry.Id))
                {
                    parent.Chunks.TryAdd(entry.Id, entry);
                }
                else
                {
                    //parent.Chunks[entry.Id].SuperBundles.AddRange(entry.SuperBundles);
                }
            }
        }

    }
}
