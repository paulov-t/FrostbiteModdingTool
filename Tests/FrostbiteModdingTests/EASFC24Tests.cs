using FrostySdk.Frostbite;
using FrostySdk.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class EASFC24Tests : IFMTTest
    {
        public string GamePath => throw new NotImplementedException();

        public string GameName => throw new NotImplementedException();

        public string GameEXE => throw new NotImplementedException();

        public string GamePathEXE => "H:\\EA Games\\FC 24 Closed Beta\\FC24.exe";

        [TestMethod]
        public void BuildCache()
        {
            var buildCache = new CacheManager();
            buildCache.Load(GamePathEXE, this, true, true);

            var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            var resItems = AssetManager.Instance.EnumerateRes().ToList();
            var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        private string prevText = string.Empty;

        public void Log(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine($"[LOGGER][DEBUG][{DateTime.Now.ToShortTimeString()}] {text}");
                prevText = text;
            }
        }

        public void LogError(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine($"[LOGGER][ERROR][{DateTime.Now.ToShortTimeString()}] {text}");
                prevText = text;
            }
        }

        public void LogWarning(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine($"[LOGGER][WARN][{DateTime.Now.ToShortTimeString()}] {text}");
                prevText = text;
            }
        }

        public void LogProgress(int progress)
        {
        }

    }
}
