using FMT.FileTools;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FC24Plugin
{
    public class FC24CustomAssetEntryEnumerations : ICustomAssetEntryEnumerations
    {
        public Dictionary<string, IEnumerable<IAssetEntry>> GetCustomAssetEntriesEnumerations()
        {
            if (AssetManager.Instance == null)
                return null;

            Dictionary<string, IEnumerable<IAssetEntry>> assetCollections = new();
            assetCollections.Add("Gameplay", AssetManager.Instance
            .EnumerateEbx()
            .Where(x => x.Path.Contains("fifa/attribulator", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Path)
            .Select(x => (IAssetEntry)x).ToList());


            return assetCollections;
        }
    }
}
