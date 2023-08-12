using FrostySdk;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FrostbiteModdingUI.Models
{
    public class LauncherOptions
    {
        public bool? InstallEmbeddedFiles { get; set; }
        public bool? CleanLegacyMods { get; set; }

        private bool? useModData;

        public bool? UseModData
        {
            get
            {
                if (ProfileManager.LoadedProfile.CanUseModData)
                    return useModData;
                else
                    return false;
            }
            set { useModData = ProfileManager.LoadedProfile.CanUseModData ? value : false; }
        }


        private bool? useLegacyModSupport;

        public bool? UseLegacyModSupport
        {
            get
            {
                if (ProfileManager.LoadedProfile.CanUseLiveLegacyMods)
                    return useLegacyModSupport;
                else
                    return false;
            }
            set { useLegacyModSupport = value; }
        }

        public bool? UseLiveEditor { get; internal set; }

        public bool? AutoCloseAppAfterLaunch { get; set; }

        public static string OldSaveFileLocation
        {
            get
            {
                return AppContext.BaseDirectory + "LauncherOptions.json";

            }
        }
        public static string SaveFileLocation
        {
            get
            {
                var dir = AppContext.BaseDirectory + "/Mods/Profiles/" + ProfileManager.ProfileName + "/";
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                return dir + "/LauncherOptions.json";

            }
        }

        public static LauncherOptions Load()
        {
            if (File.Exists(OldSaveFileLocation) && !File.Exists(SaveFileLocation))
            {
                File.Move(OldSaveFileLocation, SaveFileLocation);
            }

            if (File.Exists(SaveFileLocation))
            {
                var loText = File.ReadAllText(SaveFileLocation);
                return JsonConvert.DeserializeObject<LauncherOptions>(loText);
            }
            return new LauncherOptions();
        }

        public static async Task<LauncherOptions> LoadAsync()
        {
            return await Task.Run(() => { return Load(); });
        }

        public void Save()
        {
            File.WriteAllText(SaveFileLocation, JsonConvert.SerializeObject(this));
        }
    }
}
