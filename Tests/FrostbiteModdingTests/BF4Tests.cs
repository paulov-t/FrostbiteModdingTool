using FMT.Logging;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SdkGenerator;
using System.Diagnostics;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class BF4Tests : ILogger
    {
        private string prevText = string.Empty;

        public const string GamePath = @"E:\Origin Games\Battlefield 4\";
        public const string GamePathEXE = @"E:\Origin Games\Battlefield 4\bf4.exe";

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
            buildCache.LoadData("BF4", GamePath, this, true, false);
        }

        [TestMethod]
        public void BuildSDK()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("BF4", GamePath, this, false);

            var buildSDK = new BuildSDK();
            buildSDK.Build().Wait();
        }

        public void LogProgress(int progress)
        {
            throw new System.NotImplementedException();
        }
    }
}
