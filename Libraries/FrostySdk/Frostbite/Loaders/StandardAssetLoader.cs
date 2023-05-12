using FMT.FileTools;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Frostbite.Loaders
{
    public class StandardAssetLoader : IAssetLoader
    {
        public void Load(AssetManager parent, BinarySbDataHelper helper)
        {
            foreach (string superBundleName in parent.FileSystem.SuperBundles)
            {
                DbObject toc = parent.ProcessTocChunks(string.Format("{0}.toc", superBundleName), helper, false);
                if (toc == null)
                    continue;

                parent.superBundles.Add(new SuperBundleEntry() { Name = superBundleName });
                parent.WriteToLog($"Loading data ({superBundleName})");

                using (NativeReader sbReader = new NativeReader(new FileStream(parent.FileSystem.ResolvePath(string.Format("{0}.sb", superBundleName)), FileMode.Open, FileAccess.Read)))
                {
                    foreach (DbObject bundle in toc.GetValue<DbObject>("bundles"))
                    {
                        string bundleName = bundle.GetValue<string>("id").ToLower();
                        long offset = bundle.GetValue<long>("offset");
                        long size = bundle.GetValue<long>("size");
                        bool isDeltaBundle = bundle.GetValue<bool>("delta");
                        bool isBaseBundle = bundle.GetValue<bool>("base");

                        // add new bundle entry
                        parent.Bundles.Add(new BundleEntry { Name = bundleName, SuperBundleId = parent.superBundles.Count - 1 });
                        int bundleId = parent.Bundles.Count - 1;

                        DbObject sb = null;
                        using (DbReader reader = new DbReader(sbReader.CreateViewStream(offset, size), parent.FileSystem.CreateDeobfuscator()))
                            sb = reader.ReadDbObject();

                        // process assets
                        parent.ProcessBundleEbx(sb, bundleId, helper);
                        parent.ProcessBundleRes(sb, bundleId, helper);
                        parent.ProcessBundleChunks(sb, bundleId, helper);
                    }
                }
            }
        }
    }
}
