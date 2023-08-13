using FMT.Logging;
using FrostbiteModdingUI.CEM;
using FrostySdk.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using v2k4FIFAModdingCL;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class CareerAndDBTests : ILogger, IFMTTest
    {
        private string prevText = string.Empty;

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
        public string GamePathEXE
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\EA Sports\\FIFA 23"))
                {
                    if (key != null)
                    {
                        string installDir = key.GetValue("Install Dir").ToString();
                        return installDir + "FIFA23.exe";
                    }
                }
                return string.Empty;
            }
        }

        public string TestMeshesPath
        {
            get
            {
                return @"C:\Users\paula\Desktop\";
            }
        }

        public string GameName => throw new NotImplementedException();

        public string GameEXE => throw new NotImplementedException();

        public void BuildCache()
        {
            throw new NotImplementedException();
        }

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



        [TestMethod]
        public void ReadStatsFromCareerFile()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE);
            Directory.GetFiles("C:\\Users\\paula\\Documents\\FIFA 23\\settings\\", "Career*")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            var cem = new CEMCore2("FIFA23");

            Stopwatch sw1 = new Stopwatch();
            sw1.Start();
            var stats = cem.GetPlayerStatsAsync().Result;
            sw1.Stop();
            Debug.WriteLine($"Time taken to get stats : {sw1.Elapsed}");
            var statsDoncaster = cem.GetPlayerStatsAsync(142).Result;
        }


        [TestMethod]
        public void ReadFinancesFromCareerFile()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE);
            var cem = new CEMCore2("FIFA23");

            Stopwatch sw1 = new Stopwatch();
            sw1.Start();
            var finances = cem.GetFinances().Result;
            sw1.Stop();
            Debug.WriteLine($"Time taken to get finances : {sw1.Elapsed}");
        }

        public void LogProgress(int progress)
        {
            throw new NotImplementedException();
        }
    }
}
