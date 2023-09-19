using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    /// <summary>
    /// A useful interface for Default Editor to load custom tabs without using the full CustomAssetManager interface
    /// </summary>
    public interface ICustomAssetEntryEnumerations
    {
        public Dictionary<string, IEnumerable<IAssetEntry>> GetCustomAssetEntriesEnumerations();
    }
}
