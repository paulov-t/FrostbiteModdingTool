using FMT.Logging;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class FIFA20Tests : ILogger
    {
        private string prevText = string.Empty;

        public const string GamePath = @"E:\Origin Games\FIFA 20";
        public const string GamePathEXE = @"E:\Origin Games\FIFA 20\FIFA20.exe";
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
            buildCache.LoadData("FIFA20", GamePath, this, true, true);
        }

        public void LogProgress(int progress)
        {
            throw new System.NotImplementedException();
        }
    }
}
