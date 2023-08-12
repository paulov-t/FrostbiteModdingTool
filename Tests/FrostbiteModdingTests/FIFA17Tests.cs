using FrostySdk.Frostbite.FIFA;
using FrostySdk.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class FIFA17Tests : ILogger, IFMTTest
    {
        public string GamePath
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\EA Sports\\FIFA 17"))
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
        public string GamePathEXE => GamePath + "FIFA17.exe";

        public string GameName { get; }

        public string GameEXE { get; }

        public void BuildCache()
        {
        }

        [TestMethod]    
        public void TestAttribdbManager()
        {
            AttribdbManager attribdbManager = new AttribdbManager(
                File.ReadAllBytes("G:\\Work\\FIFA Modding\\attribdb.VLT")
                , null
                ); ;
        }

        public void Log(string text, params object[] vars)
        {
        }

        public void LogError(string text, params object[] vars)
        {
        }

        public void LogWarning(string text, params object[] vars)
        {
        }
    }
}
