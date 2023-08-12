using FMT.FileTools.Modding;
using FrostbiteSdk;
using FrostySdk;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//namespace FIFAModdingUI.Mods
namespace FMT.Mods
{
    public class ModList
    {
        public class ModItem
        {
            public string Path { get; set; }
            public string Type
            {
                get
                {
                    var fi = new FileInfo(Path);
                    if (fi.Exists)
                    {
                        return fi.Extension;
                    }
                    return null;
                }
            }

            public string ModType
            {
                get
                {
                    var mt = string.Empty;
                    if (!string.IsNullOrEmpty(Type))
                    {
                        switch (Type)
                        {
                            case ".fbmod":
                                mt = "Frostbite";
                                break;
                            case ".lmod":
                                mt = "Legacy";
                                break;
                            case ".zip":
                                mt = "Bundle";
                                break;
                            case ".fifamod":
                                mt = "FIFA (FET)";
                                break;
                        }
                    }
                    return mt;
                }
            }

            public PackIconKind MaterialDesignKindIcon
            {
                get
                {
                    var pik = PackIconKind.Cog;
                    switch (ModType)
                    {
                        case "Frostbite":
                            pik = PackIconKind.Controller;
                            break;
                        case "Legacy":
                            pik = PackIconKind.ZipBox;
                            break;
                        case "Bundle":
                            pik = PackIconKind.FolderZip;
                            break;
                        case "FIFA (FET)":
                            pik = PackIconKind.Controller;
                            break;
                    }
                    return pik;
                }
            }

            public System.Windows.Media.Brush IconColor
            {
                get
                {
                    var pik = System.Windows.Media.Brushes.DarkSlateBlue; // Color.FromArgb(255, 0, 0, 0);
                    switch (ModType)
                    {
                        case "Frostbite":
                            break;
                        case "Legacy":
                            pik = System.Windows.Media.Brushes.Green;
                            break;
                        case "Bundle":
                            break;
                        case "FIFA (FET)":
                            pik = System.Windows.Media.Brushes.Red;
                            break;
                    }
                    return pik;
                }
            }

            public bool HasIssues
            {
                get
                {
                    return ModType == "FIFA (FET)";
                }
            }

            public string Warning
            {
                get
                {
                    if (HasIssues)
                    {
                        return "This mod type may have problems or missing features on this launcher.";
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }

            //private static Dictionary<string, IFrostbiteMod> FrostbiteMods = new Dictionary<string, IFrostbiteMod>(100);

            public IFrostbiteMod GetFrostbiteMod()
            {
                //if (FrostbiteMods.ContainsKey(Path))
                //    return FrostbiteMods[Path];

                IFrostbiteMod frostbiteMod;
                switch (ModType)
                {
                    case "Frostbite":
                        frostbiteMod = new FrostbiteMod(Path);
                        break;
                    case "FIFA (FET)":
                        frostbiteMod = new FIFAMod(string.Empty, Path);
                        break;
                    default:
                        return null;
                }

                //FrostbiteMods.Add(Path, frostbiteMod);

                return frostbiteMod;
            }

            public ModItem(string p)
            {
                Path = p;
            }

            public IEnumerable<BaseModResource> ModResources
            {
                get
                {
                    var fmod = GetFrostbiteMod();
                    if (fmod != null && fmod.Resources != null)
                        return fmod.Resources.Where(x => x.Type != ModResourceType.Embedded);
                    return null;
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is ModItem)
                {
                    return ((ModItem)obj).Path.ToLower() == this.Path.ToLower();
                }
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Path.GetHashCode();
            }

            public override string ToString()
            {
                FileInfo fileInfo = new FileInfo(Path);
                if (fileInfo.Exists)
                {
                    var fbm = GetFrostbiteMod();
                    if (fbm != null)
                    {
                        if (fbm.ModDetails != null)
                        {
                            return fbm.ModDetails.Title + " (" + fbm.ModDetails.Version + ")";
                        }
                        fbm.Dispose();
                    }
                    return fileInfo.Name;
                }
                return base.ToString();
            }
        }

        public List<ModItem> ModListItems = new List<ModItem>();

        public List<string> ModListItemErrors = new List<string>();
        public ModListProfile ModListProfile { get; set; }

        public ModList(ModListProfile profile = null)
        {
            ModListProfile = profile;
            if (!File.Exists(ModListLocation))
            {
                Save();
            }


            Load();
        }

        public string ModListLocation
        {
            get
            {
                string v = "Mods\\ModList.json";
                if (ModListProfile != null)
                {
                    Directory.CreateDirectory("Mods\\Profiles\\");
                    v = "Mods\\Profiles\\" + ModListProfile.ProfileName + "\\ModList.json";
                }

                return v;
            }
        }

        public void Load()
        {
            if (System.IO.File.Exists(ModListLocation))
            {
                var allTextRaw = System.IO.File.ReadAllText(ModListLocation);
                var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(allTextRaw);
                var oldCount = ModListItems.Count;
                foreach (var i in items)
                {
                    if (!File.Exists(i))
                    {
                        ModListItemErrors.Add($"Mod file {i} no longer exists");
                    }
                    else
                    {
                        ModListItems.Add(new ModItem(i));
                    }
                }

                if (oldCount != ModListItems.Count)
                    Save();
            }
        }

        public void Save()
        {
            System.IO.File.WriteAllText(ModListLocation, Newtonsoft.Json.JsonConvert.SerializeObject(ModListItems.Select(x => x.Path)));
        }
    }

    public class ModListProfile
    {
        public string ProfileName { get; set; }

        public string GameExecutableLocation { get; set; }

        public bool InstallLocaleFile { get; set; }

        public string DirectoryLocation
        {
            get
            {
                return "Mods\\Profiles\\" + ProfileName;
            }
        }

        private ModList ModList;

        public ModListProfile(string name)
        {
            if (name == "Default" || name == null)
                ProfileName = null;
            else
                ProfileName = name;

            Directory.CreateDirectory(DirectoryLocation);

            ModList = new ModList(this);
        }

        private string ModListProfileLocation
        {
            get
            {
                return DirectoryLocation + "\\Profile.json";
            }
        }

        public void Load()
        {
            if (System.IO.File.Exists(ModListProfileLocation))
            {
                var r = Newtonsoft.Json.JsonConvert.DeserializeObject<ModListProfile>(System.IO.File.ReadAllText(ModListProfileLocation));

                if (r != null)
                {
                    this.ProfileName = r.ProfileName;
                    this.InstallLocaleFile = r.InstallLocaleFile;
                    this.GameExecutableLocation = r.GameExecutableLocation;
                }
            }
        }

        public void Save()
        {
            System.IO.File.WriteAllText(ModListProfileLocation, Newtonsoft.Json.JsonConvert.SerializeObject(this));
        }

        public static IEnumerable<ModListProfile> LoadAll()
        {
            Directory.CreateDirectory("Mods\\Profiles\\");
            foreach (var d in Directory.GetDirectories("Mods\\Profiles\\"))
            {
                if (File.Exists(d + "\\ModList.json") && File.Exists(d + "\\Profile.json"))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(d);
                    var mlp = new ModListProfile(directoryInfo.Name);
                    mlp.Load();
                    yield return mlp;
                }
            }
        }
    }
}
