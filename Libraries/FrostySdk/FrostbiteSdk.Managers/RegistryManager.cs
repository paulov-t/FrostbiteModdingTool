using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.FrostbiteSdk.Managers
{
    public static class RegistryManager
    {
        public static string GamePathEXE
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"Software\\EA Sports\\{ProfileManager.DisplayName}"))
                {
                    if (key != null)
                    {
                        string installDir = key.GetValue("Install Dir").ToString();
                        installDir += $"{ProfileManager.ProfileName}.exe";
                        if(File.Exists(installDir))
                            return installDir;
                    }
                }

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"Software\\EA Games\\{ProfileManager.DisplayName}"))
                {
                    if (key != null)
                    {
                        string installDir = key.GetValue("Install Dir").ToString();
                        installDir += $"{ProfileManager.ProfileName}.exe";
                        if (File.Exists(installDir))
                            return installDir;
                    }
                }
                return string.Empty;
            }
        }

        public static bool FoundExe => GamePathEXE != string.Empty;
    }
}
