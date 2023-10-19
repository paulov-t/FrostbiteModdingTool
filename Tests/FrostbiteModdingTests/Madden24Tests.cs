using FrostySdk.Frostbite;
using FrostySdk.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using v2k4FIFAModding.Frosty;
using v2k4FIFAModdingCL;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class Madden24Tests : IFMTTest
    {
        public string GameName => throw new NotImplementedException();

        public string GameEXE => throw new NotImplementedException();

        public string GamePath
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\EA Sports\\Madden NFL 24"))
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
        public string GamePathEXE
        {
            get
            {

                return Path.Combine(GamePath, "Madden24.exe");
            }
        }

        public string TestMeshesPath
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }

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

        [TestMethod]
        public void ModSplashscreen()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "G:\\Work\\testsplashmadden24.fbmod" }.ToArray()).Wait();
        }

        [TestMethod]
        public void ModSplashscreenProject()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            ProjectManagement projectManagement = new ProjectManagement(GamePath);
            var p = projectManagement.LoadProjectFromFile("G:\\Work\\madden 24 splash project.fmtproj").Result;
            p.WriteToMod("test.fbmod");
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();
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
