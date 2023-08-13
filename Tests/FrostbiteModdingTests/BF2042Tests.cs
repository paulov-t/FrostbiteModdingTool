using FMT.Logging;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SdkGenerator;
using System.Diagnostics;
using System.Linq;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class BF2042Tests : ILogger
    {
        private string prevText = string.Empty;

        public const string GamePath = @"F:\Origin Games\Battlefield 2042 Technical Playtest\";
        public const string GamePathEXE = @"F:\Origin Games\Battlefield 2042 Technical Playtest\bf.exe";

        public void Log(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine("[LOGGER] [DEBUG] " + text);
                prevText = text;
            }
        }

        public void LogError(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine("[LOGGER] [ERROR] " + text);
                prevText = text;
            }
        }

        public void LogWarning(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine("[LOGGER] [WARNING] " + text);
                prevText = text;
            }
        }

        [TestMethod]
        public void BuildCache()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("BF2042", GamePath, this, true, false);

            var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            var resItems = AssetManager.Instance.EnumerateRes().ToList();
            var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        [TestMethod]
        public void BuildSDK()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("BF2042", GamePath, this, false);

            var buildSDK = new BuildSDK();
            buildSDK.OverrideProfileName = "BF2042";
            buildSDK.Build().Wait();

            var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            var resItems = AssetManager.Instance.EnumerateRes().ToList();
            var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        public void LogProgress(int progress)
        {
            throw new System.NotImplementedException();
        }
    }
}
