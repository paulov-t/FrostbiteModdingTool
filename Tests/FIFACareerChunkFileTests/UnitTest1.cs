using FMT.Logging;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using Microsoft.Win32;
using System.Diagnostics;
using System.Linq;

namespace FIFACareerChunkFileTests
{
    [TestClass]
    public class UnitTest1 : ILogger
    {
        public string GamePath
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\EA Sports\\FIFA 23"))
                {
                    if (key != null)
                    {
                        string installDir = key.GetValue("Install Dir").ToString();
                        return installDir;
                    }
                }
                return string.Empty;
            }
        }

        [TestMethod]
        public void LoadNewsItems()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("FIFA23", GamePath, this, false, true);

            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
            var folder = legacyItems.Where(x => x.Path.StartsWith("dlc/dlc_Saga/dlc/Saga/data/FolderAsset"));
            var displayName = ((LegacyFileEntry)folder.First()).GetFIFAXMLDisplayName();
        }

        public void Log(string text, params object[] vars)
        {
            Debug.WriteLine(text);
        }

        public void LogError(string text, params object[] vars)
        {
        }

        public void LogProgress(int progress)
        {
            throw new System.NotImplementedException();
        }

        public void LogWarning(string text, params object[] vars)
        {
        }
    }
}