using FMT.Logging;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SdkGenerator;
using System;
using System.Diagnostics;
using System.Linq;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class Madden22Tests : ILogger
    {
        private string prevText = string.Empty;

        public const string GamePath = @"K:\Origin Games\Madden NFL 22\";
        public const string GamePathEXE = @"K:\Origin Games\Madden NFL 22\Madden22.exe";

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
            buildCache.LoadData("Madden22", GamePath, this, true, false);

            var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            var resItems = AssetManager.Instance.EnumerateRes().ToList();
            var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        [TestMethod]
        public void BuildCacheIndexing()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("Madden22", GamePath, this, false, true);
            AssetManager.Instance.DoEbxIndexing();
        }

        [TestMethod]
        public void BuildSDK()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("Madden22", GamePath, this, false, false);

            var buildSDK = new BuildSDK();
            buildSDK.Build().Wait();

            var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            var resItems = AssetManager.Instance.EnumerateRes().ToList();
            var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        [TestMethod]
        public void ReadSharedTypeDescriptor()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("Madden22", GamePath, this, false, true);
            EbxSharedTypeDescriptorV2 std = new EbxSharedTypeDescriptorV2("SharedTypeDescriptors.ebx", false);
        }

        [TestMethod]
        public void LoadLegacy()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("Madden22", GamePath, this, false, true);

            var ebxFCC = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("legacy", StringComparison.OrdinalIgnoreCase));
            var ebxFile = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("file", StringComparison.OrdinalIgnoreCase));
            var ebxCollector = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("collector", StringComparison.OrdinalIgnoreCase));
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        public void LogProgress(int progress)
        {
            throw new NotImplementedException();
        }
    }
}

