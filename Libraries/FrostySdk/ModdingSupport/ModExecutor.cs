using FMT.FileTools;
using FMT.FileTools.Modding;
using FrostbiteSdk;
using FrostbiteSdk.FrostbiteSdk.Managers;
using FrostbiteSdk.Frosty.Abstract;
using FrostySdk;
using FrostySdk.Frostbite.Compilers;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Xml;
using v2k4FIFAModdingCL;
using Sha1 = FMT.FileTools.Sha1;

namespace ModdingSupport
{
    public class ArchiveInfo
    {
        public byte[] Data;

        public int RefCount;
    }

    public class ModExecutor
    {
        public class ModBundleInfo
        {


            public class ModBundleAction
            {
                public List<string> Ebx = new List<string>();

                public List<string> Res = new List<string>();

                public List<Guid> Chunks = new List<Guid>();

                public List<string> Legacy = new List<string>();

                public void AddEbx(string name)
                {
                    if (!Ebx.Contains(name))
                    {
                        Ebx.Add(name);
                    }
                }

                public void AddRes(string name)
                {
                    if (!Res.Contains(name))
                    {
                        Res.Add(name);
                    }
                }

                public void AddChunk(Guid guid)
                {
                    if (!Chunks.Contains(guid))
                    {
                        Chunks.Add(guid);
                    }
                }

                public void AddLegacy(string name)
                {
                    if (!Legacy.Contains(name))
                    {
                        Legacy.Add(name);
                    }
                }
            }

            public int Name;

            public ModBundleAction Add = new ModBundleAction();

            public ModBundleAction Remove = new ModBundleAction();

            public ModBundleAction Modify = new ModBundleAction();
        }

        public class CasFileEntry
        {
            public ManifestFileInfo FileInfo;

            public ChunkAssetEntry Entry;
        }

        public class CasDataEntry
        {
            private string catalog;

            private List<FMT.FileTools.Sha1> dataRefs = new List<FMT.FileTools.Sha1>();

            private Dictionary<Sha1, List<CasFileEntry>> fileInfos = new Dictionary<Sha1, List<CasFileEntry>>();

            public string Catalog => catalog;

            public bool HasEntries => dataRefs.Count != 0;

            public CasDataEntry(string inCatalog, params Sha1[] sha1)
            {
                catalog = inCatalog;
                if (sha1.Length != 0)
                {
                    dataRefs.AddRange(sha1);
                }
            }

            public void Add(Sha1 sha1, ChunkAssetEntry entry = null, ManifestFileInfo file = null)
            {
                if (!dataRefs.Contains(sha1))
                {
                    dataRefs.Add(sha1);
                    fileInfos.Add(sha1, new List<CasFileEntry>());
                }
                if (entry != null || file != null)
                {
                    fileInfos[sha1].Add(new CasFileEntry
                    {
                        Entry = entry,
                        FileInfo = file
                    });
                }
            }

            public bool Contains(Sha1 sha1)
            {
                return dataRefs.Contains(sha1);
            }

            public IEnumerable<FMT.FileTools.Sha1> EnumerateDataRefs()
            {
                foreach (Sha1 dataRef in dataRefs)
                {
                    yield return dataRef;
                }
            }

            public IEnumerable<CasFileEntry> EnumerateFileInfos(Sha1 sha1)
            {
                int num = dataRefs.IndexOf(sha1);
                if (num != -1 && num < fileInfos.Count)
                {
                    foreach (CasFileEntry item in fileInfos[sha1])
                    {
                        yield return item;
                    }
                }
            }
        }

        public class CasDataInfo
        {
            private Dictionary<string, CasDataEntry> entries = new Dictionary<string, CasDataEntry>();

            public void Add(string catalog, Sha1 sha1, ChunkAssetEntry entry = null, ManifestFileInfo file = null)
            {
                if (!entries.ContainsKey(catalog))
                {
                    entries.Add(catalog, new CasDataEntry(catalog));
                }
                entries[catalog].Add(sha1, entry, file);
            }

            public IEnumerable<CasDataEntry> EnumerateEntries()
            {
                foreach (CasDataEntry value in entries.Values)
                {
                    yield return value;
                }
            }
        }

        public class FrostySymLinkException : Exception
        {
            public override string Message => "One ore more symbolic links could not be created, please restart tool as Administrator.";
        }

        public class HandlerExtraData : AssetExtraData
        {
            public Frosty.ModSupport.Handlers.ICustomActionHandler Handler
            //public FrostySdk.ICustomActionHandler Handler
            {
                get;
                set;
            }

            public object Data
            {
                get;
                set;
            }
        }

        //public struct SymLinkStruct
        //{
        //    public string dest;

        //    public string src;

        //    public bool isFolder;

        //    public SymLinkStruct(string inDst, string inSrc, bool inFolder)
        //    {
        //        dest = inDst;
        //        src = inSrc;
        //        isFolder = inFolder;
        //    }
        //}
        
        public FileSystem fs;

        private ILogger logger { get; set; }

        public Dictionary<int, ModBundleInfo> modifiedBundles { get; } = new Dictionary<int, ModBundleInfo>();

        public Dictionary<string, List<string>> addedBundles { get; } = new Dictionary<string, List<string>>();

        public Dictionary<string, AssetEntry> ModifiedAssets
        {
            get
            {
                Dictionary<string, AssetEntry> entries = new Dictionary<string, AssetEntry>();
                foreach (var item in modifiedEbx)
                {
                    entries.Add(item.Key, item.Value);
                }
                foreach (var item in modifiedRes)
                {
                    entries.Add(item.Value.ToString(), item.Value);
                }
                foreach (var item in ModifiedChunks)
                {
                    entries.Add(item.Key.ToString(), item.Value);
                }
                return entries;
            }
        }


        public Dictionary<string, EbxAssetEntry> modifiedEbx { get; } = new Dictionary<string, EbxAssetEntry>();

        public Dictionary<string, ResAssetEntry> modifiedRes { get; } = new Dictionary<string, ResAssetEntry>();

        public Dictionary<Guid, ChunkAssetEntry> ModifiedChunks { get; } = new Dictionary<Guid, ChunkAssetEntry>();

        /// <summary>
        /// Added by PG 24 May 2021 to add (not modify) TOC Chunks
        /// </summary>
        public Dictionary<Guid, ChunkAssetEntry> AddedChunks { get; } = new Dictionary<Guid, ChunkAssetEntry>();

        public Dictionary<string, LegacyFileEntry> modifiedLegacy { get; } = new Dictionary<string, LegacyFileEntry>();

        public Dictionary<Sha1, ArchiveInfo> archiveData { get; } = new Dictionary<Sha1, ArchiveInfo>();


        public int numTasks;

        public CasDataInfo casData = new CasDataInfo();

        public static int chunksBundleHash = Fnv1.HashString("chunks");

        //public Dictionary<int, Dictionary<int, Dictionary<uint, CatResourceEntry>>> resources = new Dictionary<int, Dictionary<int, Dictionary<uint, CatResourceEntry>>>();

        public ILogger Logger
        {
            get
            {
                return logger;
            }
            set
            {
                logger = value;
            }
        }

        public string GamePath { get { return fs.BasePath; } }
        public string GameEXEPath { get { return Path.Combine(GamePath, ProfileManager.ProfileName + ".exe"); } }
        public string GameEXEPathNoExtension { get { return Path.Combine(GamePath, ProfileManager.ProfileName); } }

        public bool EADesktopIsInstalled
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Electronic Arts\\EA Desktop"))
                {
                    if (key != null)
                    {
                        string installDir = key.GetValue("InstallLocation").ToString();
                        string installSuccessful = key.GetValue("InstallSuccessful").ToString();
                        return !string.IsNullOrEmpty(installDir) && !string.IsNullOrEmpty(installSuccessful);
                    }
                }

                return Process.GetProcessesByName("EADesktop.exe").Any();
            }
        }
        public bool LaunchedViaEADesktop { get; set; } = false;

        public static bool UseACBypass { get; set; }
            = ProfileManager.IsFIFA23DataVersion()
            //&& FileSystem.Instance.Head <= 1572210
            && ProfileManager.LoadedProfile.UseACBypass;
        //public bool UseACBypass { get; set; } = false;

        public bool UseSymbolicLinks = false;

        [DllImport("kernel32.dll")]
        protected static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        public Dictionary<int, Dictionary<uint, CatResourceEntry>> LoadCatalog(FileSystem fs, string filename, out int catFileHash)
        {

            catFileHash = 0;
            string text = fs.ResolvePath(filename);
            if (!File.Exists(text))
            {
                return null;
            }
            catFileHash = Fnv1.HashString(text.ToLower());
            Dictionary<int, Dictionary<uint, CatResourceEntry>> dictionary = new Dictionary<int, Dictionary<uint, CatResourceEntry>>();
            using (CatReader catReader = new CatReader(new FileStream(text, FileMode.Open, FileAccess.Read), fs.CreateDeobfuscator()))
            {
                for (int i = 0; i < catReader.ResourceCount; i++)
                {
                    CatResourceEntry value = catReader.ReadResourceEntry();
                    if (!dictionary.ContainsKey(value.ArchiveIndex))
                    {
                        dictionary.Add(value.ArchiveIndex, new Dictionary<uint, CatResourceEntry>());
                    }
                    dictionary[value.ArchiveIndex].Add(value.Offset, value);
                }
                return dictionary;
            }
        }
        string modDirName = "ModData";

        public bool UseLegacyLauncher = false;

        List<Assembly> PluginAssemblies = new List<Assembly>();

        private bool FileIsSymbolic(string path)
        {
            FileInfo pathInfo = new FileInfo(path);
            return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        public async Task<bool> BuildModData(ILogger inLogger, params string[] allModPaths)
        {
            Logger = inLogger;

            if (!AssetManager.InitialisePlugins())
            {
                throw new Exception("Unable to initialize Plugins");
            }
            string modPath = fs.BasePath + modDirName + "\\";
            string patchPath = "Patch";

            string profileName = ProfileManager.ProfileName;
            if (Process.GetProcesses().Any(x => x.ProcessName.Equals(profileName, StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Game process is already running, please close and relaunch");

            bool FrostyModsFound = false;

            //string[] allModPaths = modPaths;
            var frostyMods = new Dictionary<Stream, IFrostbiteMod>();

            Logger.Log("Deleting cached mods");

            if (Directory.Exists(ApplicationDirectory + "TempMods"))
                Directory.Delete(ApplicationDirectory + "TempMods", true);

            var compatibleModExtensions = new List<string>() { ".fbmod", ".fifamod" };
            Logger.Log("Loading mods");

            FileLogger.WriteLine("========================================================");
            FileLogger.WriteLine("Loading mod Streams");
            FileLogger.WriteLine("========================================================");

            foreach (var f in allModPaths.Select(x => new FileInfo(x)))
            {
                FileLogger.WriteLine($">>> Loading {f.FullName}");
                ReadFrostbiteMods(f.FullName, ref FrostyModsFound, ref frostyMods);
                //ReadFrostbiteMods(rootPath + f, ref FrostyModsFound, ref frostyMods);
            }


            foreach (KeyValuePair<Stream, IFrostbiteMod> kvpMods in frostyMods)
            {
                //Logger.Log("Compiling mod " + kvpMods.Value.Filename);


                int indexCompleted = -1;
                var frostbiteMod = kvpMods.Value;
                //Parallel.ForEach(frostbiteMod.Resources, (BaseModResource resource) =>
                foreach (
                    (BaseModResource, byte[]) r
                    in
                    frostbiteMod.Resources
                    .OrderBy(x=>x.Type == ModResourceType.Ebx)
                    .ThenBy(x=>x.Type == ModResourceType.Res)
                    .Select(x => (x, frostbiteMod.GetResourceData(x)))
                    .Where(x => x.Item2 != null)
                    //.OrderBy(x => x.Item2.Length)
                    )
                {
                    indexCompleted++;

                    // ------------------------------------------------------------------
                    // Get the Resource Data out of the mod
                    BaseModResource resource = r.Item1;
                    byte[] resourceData = r.Item2;
                    // ------------------------------------------------------------------
                    // Embedded Files
                    // Export to the Game Directory and create sub folders if neccessary
                    if (resource is BaseModReader.EmbeddedFileResource)
                    {
                        EmbeddedFileEntry efAssetEntry = new EmbeddedFileEntry();
                        resource.FillAssetEntry(efAssetEntry);

                        var parentDirectoryPath = Directory.GetParent(GamePath + "//" + efAssetEntry.ExportedRelativePath).FullName;
                        if (!Directory.Exists(parentDirectoryPath))
                            Directory.CreateDirectory(parentDirectoryPath);

                        var exportedFilePath = GamePath + "//" + efAssetEntry.ExportedRelativePath;
                        var exportedFileBackupPath = GamePath + "//" + efAssetEntry.ExportedRelativePath + ".bak";
                        //File.WriteAllBytes(GamePath + "//" + efAssetEntry.Name, resourceData);
                        if (!File.Exists(exportedFileBackupPath)
                            && File.Exists(exportedFilePath)
                            )
                            File.Move(exportedFilePath, exportedFileBackupPath);

                        await File.WriteAllBytesAsync(exportedFilePath, resourceData);
                        FileLogger.WriteLine($"Written {kvpMods.Value.ModDetails.Title} Embedded File Resource to {exportedFilePath}");
                    }
                    //
                    // ------------------------------------------------------------------


                    foreach (int modifiedBundle in resource.ModifiedBundles)
                    {
                        if (!modifiedBundles.ContainsKey(modifiedBundle))
                        {
                            modifiedBundles.Add(modifiedBundle, new ModBundleInfo
                            {
                                Name = modifiedBundle
                            });
                        }
                        ModBundleInfo modBundleInfo = modifiedBundles[modifiedBundle];
                        switch (resource.Type)
                        {
                            case ModResourceType.Ebx:
                                modBundleInfo.Modify.AddEbx(resource.Name);
                                break;
                            case ModResourceType.Res:
                                modBundleInfo.Modify.AddRes(resource.Name);
                                break;
                            case ModResourceType.Chunk:
                                modBundleInfo.Modify.AddChunk(new Guid(resource.Name));
                                break;
                            case ModResourceType.Legacy:
                                modBundleInfo.Modify.AddLegacy(resource.Name);
                                break;
                        }
                    }


                    foreach (int addedBundle in resource.AddedBundles)
                    {
                        if (!modifiedBundles.ContainsKey(addedBundle))
                        {
                            modifiedBundles.Add(addedBundle, new ModBundleInfo
                            {
                                Name = addedBundle
                            });
                        }
                        ModBundleInfo modBundleInfo2 = modifiedBundles[addedBundle];
                        switch (resource.Type)
                        {
                            case ModResourceType.Ebx:
                                modBundleInfo2.Add.AddEbx(resource.Name);
                                break;
                            case ModResourceType.Res:
                                modBundleInfo2.Add.AddRes(resource.Name);
                                break;
                            case ModResourceType.Chunk:
                                modBundleInfo2.Add.AddChunk(new Guid(resource.Name));
                                break;
                        }
                    }


                    switch (resource.Type)
                    {
                        case ModResourceType.Ebx:

                            if (modifiedEbx.ContainsKey(resource.Name))
                            {
                                modifiedEbx.Remove(resource.Name);
                                if (archiveData.ContainsKey(resource.Sha1))
                                    archiveData.Remove(resource.Sha1, out ArchiveInfo _);

                                FileLogger.WriteLine($"Replacing Ebx {resource.Name} with {kvpMods.Value.ModDetails.Title} in ModifiedEbx list");
                            }
                            else
                            {
                                FileLogger.WriteLine($"Adding Ebx {resource.Name} from {kvpMods.Value.ModDetails.Title} to ModifiedEbx list");
                            }
                            EbxAssetEntry ebxEntry = new EbxAssetEntry();
                            resource.FillAssetEntry(ebxEntry);
                            ebxEntry.Size = resourceData.Length;
                            modifiedEbx.Add(ebxEntry.Name, ebxEntry);
                            if (!archiveData.ContainsKey(ebxEntry.Sha1))
                                archiveData.Add(ebxEntry.Sha1, new ArchiveInfo
                                {
                                    Data = resourceData,
                                    RefCount = 1
                                });

                            archiveData[ebxEntry.Sha1].Data = resourceData;

                            break;
                        case ModResourceType.Res:
                            if (modifiedRes.ContainsKey(resource.Name))
                            {
                                modifiedRes.Remove(resource.Name);
                                if (archiveData.ContainsKey(resource.Sha1))
                                    archiveData.Remove(resource.Sha1, out ArchiveInfo _);

                                FileLogger.WriteLine($"Replacing {resource.Type} with {kvpMods.Value.ModDetails.Title} in ModifiedRes list");
                            }
                            else
                            {
                                FileLogger.WriteLine($"Adding {resource.Type} {resource.Name} from {kvpMods.Value.ModDetails.Title} to ModifiedRes list");
                            }
                            ResAssetEntry resEntry = new ResAssetEntry();
                            resource.FillAssetEntry(resEntry);
                            resEntry.Size = resourceData.Length;
                            modifiedRes.Add(resEntry.Name, resEntry);
                            if (archiveData.ContainsKey(resEntry.Sha1))
                            {
                                FileLogger.WriteLine($"Replacing {resEntry.Sha1} ArchiveData with {kvpMods.Value.ModDetails.Title} in ModifiedRes list");
                                archiveData[resEntry.Sha1].Data = resourceData;
                            }
                            else
                            {
                                archiveData.Add(resEntry.Sha1, new ArchiveInfo
                                {
                                    Data = resourceData,
                                    RefCount = 1
                                });
                            }



                            break;
                        case ModResourceType.Chunk:
                            Guid guid = new Guid(resource.Name);
                            
                            ChunkAssetEntry chunkAssetEntry = new ChunkAssetEntry();
                            resource.FillAssetEntry(chunkAssetEntry);
                            chunkAssetEntry.Size = resourceData.Length;

                            // -------------------------------------------------------------------------
                            // If this is a legacy file here, it likely means its a *.fifamod
                            // Ignore this chunk and send to the Legacy Mod system
                            if (resource.IsLegacyFile && frostbiteMod is FIFAMod)
                            {
                                // -------------------------------------------------------------------------------------
                                // Remove the Chunk File Collector changes. This is done ourselves via the Legacy System
                                if (resource.LegacyFullName.Contains("CFC", StringComparison.OrdinalIgnoreCase)
                                    || resource.LegacyFullName.Contains("Collector", StringComparison.OrdinalIgnoreCase)
                                    //||
                                    //    // -------------------------------------------------------------------------
                                    //    // 
                                    //    ProfileManager.CheckIsFIFA(ProfileManager.Game)
                                    //    && 
                                    //    (
                                    //        resource.LegacyFullName.Contains("player.lua", StringComparison.OrdinalIgnoreCase)
                                    //        || resource.LegacyFullName.Contains("player_kit.lua", StringComparison.OrdinalIgnoreCase)
                                    //    )
                                    )
                                    continue;

                                // -------------------------------------------------------------------------
                                // Create the Legacy Files from the Compressed Chunks
                                LegacyFileEntry legacyAssetEntry = new LegacyFileEntry();
                                legacyAssetEntry.Name = resource.LegacyFullName;
                                legacyAssetEntry.Sha1 = resource.Sha1;
                                // -------------------------------------------------------------------------
                                // Decompress the Chunks back to their normal format
                                var decompressedChunk = new CasReader(new MemoryStream(resourceData)).Read();
                                legacyAssetEntry.ModifiedEntry = new FrostySdk.FrostbiteSdk.Managers.ModifiedLegacyAssetEntry() { Data = decompressedChunk };
                                // -------------------------------------------------------------------------
                                // Actual Size is the Decompressed Size
                                legacyAssetEntry.Size = decompressedChunk.Length;

                                if (!modifiedLegacy.ContainsKey(legacyAssetEntry.Name))
                                    modifiedLegacy.Add(legacyAssetEntry.Name, legacyAssetEntry);
                                else
                                    modifiedLegacy[legacyAssetEntry.Name] = legacyAssetEntry;
                            }
                            else
                            {
                                if (ModifiedChunks.ContainsKey(guid))
                                {
                                    ModifiedChunks.Remove(guid);
                                    FileLogger.WriteLine($"Replacing {resource.Type} with {kvpMods.Value.ModDetails.Title} in ModifiedChunks list");
                                }
                                else
                                    FileLogger.WriteLine($"Adding {resource.Type} {resource.Name} from {kvpMods.Value.ModDetails.Title} to ModifiedChunks list");

                                ModifiedChunks.Add(guid, chunkAssetEntry);
                                if (!archiveData.ContainsKey(chunkAssetEntry.Sha1))
                                {
                                    archiveData.TryAdd(chunkAssetEntry.Sha1, new ArchiveInfo
                                    {
                                        Data = resourceData,
                                    });
                                }
                                else
                                {
                                    archiveData[chunkAssetEntry.Sha1].Data = resourceData;
                                }
                            }
                            break;
                    }


                    if (resource.Type == ModResourceType.Legacy)
                    {
                        LegacyFileEntry legacyAssetEntry = new LegacyFileEntry();
                        resource.FillAssetEntry(legacyAssetEntry);
                        legacyAssetEntry.ModifiedEntry = new FrostySdk.FrostbiteSdk.Managers.ModifiedLegacyAssetEntry() { Data = resourceData };
                        legacyAssetEntry.Size = resourceData.Length;

                        if (!modifiedLegacy.ContainsKey(legacyAssetEntry.Name))
                            modifiedLegacy.Add(legacyAssetEntry.Name, legacyAssetEntry);
                        else
                            modifiedLegacy[legacyAssetEntry.Name] = legacyAssetEntry;
                    }
                }

            }

            // ----------------------------------------------------------------
            // Clear out memory and mods
            foreach (KeyValuePair<Stream, IFrostbiteMod> kvpMods in frostyMods)
            {
                kvpMods.Key.Dispose();
                kvpMods.Value.Dispose();
                //if (kvpMods.Value is FrostbiteMod)
                //{
                //    //kvpMods.Value.ModBytes = null;
                //}
            }
            frostyMods.Clear();

            //Logger.Log("Cleaning up mod data directory");
            //List<SymLinkStruct> SymbolicLinkList = new List<SymLinkStruct>();
            fs.ResetManifest();


            //Logger.Log("Creating mod data directory");

            // ----------------------------------------------------------------
            // Create ModData Directory in the Game Path
            Directory.CreateDirectory(modPath);
            Directory.CreateDirectory(Path.Combine(modPath, "Data"));
            Directory.CreateDirectory(Path.Combine(modPath, "Patch"));
            Directory.CreateDirectory(Path.Combine(modPath, "Update"));


            int workerThreads = 0;
            int completionPortThreads = 0;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMaxThreads(Environment.ProcessorCount, completionPortThreads);
            //Logger.Log("Applying mods");
            //SymbolicLinkList.Clear();


            var pluginCompiler = AssetManager.LoadTypeFromPlugin2(ProfileManager.AssetCompilerName);
            if (pluginCompiler == null && !string.IsNullOrEmpty(ProfileManager.AssetCompilerName))
                throw new NotImplementedException($"Could not find class {ProfileManager.AssetCompilerName} in any plugin! Remember this is case sensitive!!");

            if (pluginCompiler != null)
            {
                if (!((IAssetCompiler)pluginCompiler).Compile(fs, logger, this))
                {
                    Logger.LogError("An error occurred within the Plugin Compiler. Stopping.");
                    return false;
                }
            }
            else
            {

                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()
                    .Where(x => x.FullName.ToLower().Contains("plugin")))
                {
                    foreach (Type t in a.GetTypes())
                    {
                        if (t.GetInterface("IAssetCompiler") != null)
                        {
                            //try
                            //{
                            if (t.Name == ProfileManager.AssetCompilerName)
                            {
                                Logger.Log("Attempting to load Compiler for " + GameEXEPath);

                                if (!((IAssetCompiler)Activator.CreateInstance(t)).Compile(fs, Logger, this))
                                {
                                    Logger.LogError("Unable to load Compiler. Stopping");
                                    return false;
                                }
                            }
                            //}
                            //catch (Exception e)
                            //{
                            //    Logger.LogError($"Error in Compiler :: {e.Message}");

                            //}
                        }
                    }
                }
            }

            if (ProfileManager.IsFIFA20DataVersion())
            {
                DbObject layoutToc = null;


                using (DbReader dbReaderOfLayoutTOC = new DbReader(new FileStream(fs.BasePath + patchPath + "/layout.toc", FileMode.Open, FileAccess.Read), fs.CreateDeobfuscator()))
                {
                    layoutToc = dbReaderOfLayoutTOC.ReadDbObject();
                }


                FifaBundleAction.CasFileCount = fs.CasFileCount;
                List<FifaBundleAction> fifaBundleActions = new List<FifaBundleAction>();
                ManualResetEvent inDoneEvent = new ManualResetEvent(initialState: false);

                var numberOfCatalogs = fs.Catalogs.Count();
                var numberOfCatalogsCompleted = 0;

                foreach (Catalog catalogItem in fs.EnumerateCatalogInfos())
                {
                    FifaBundleAction fifaBundleAction = new FifaBundleAction(catalogItem, inDoneEvent, this);
                    fifaBundleAction.Run();
                    numberOfCatalogsCompleted++;
                    logger.Log($"Compiling Mod Progress: {Math.Round((double)numberOfCatalogsCompleted / numberOfCatalogs, 2) * 100} %");

                    fifaBundleActions.Add(fifaBundleAction);
                }

                foreach (FifaBundleAction bundleAction in fifaBundleActions.Where(x => !x.HasErrored && x.CasFiles.Count > 0))
                {
                    if (bundleAction.HasErrored)
                    {
                        throw bundleAction.Exception;
                    }
                    if (bundleAction.CasFiles.Count > 0)
                    {
                        foreach (DbObject installManifestChunks in layoutToc.GetValue<DbObject>("installManifest").GetValue<DbObject>("installChunks"))
                        {
                            if (bundleAction.CatalogInfo.Name.Equals("win32/" + installManifestChunks.GetValue<string>("name")))
                            {
                                foreach (int key in bundleAction.CasFiles.Keys)
                                {
                                    DbObject dbObject6 = DbObject.CreateObject();
                                    dbObject6.SetValue("id", key);
                                    dbObject6.SetValue("path", bundleAction.CasFiles[key]);
                                    installManifestChunks.GetValue<DbObject>("files").Add(dbObject6);


                                }
                                break;
                            }
                        }
                    }
                }

                logger.Log("Writing new Layout file to Game");
                using (DbWriter dbWriter = new DbWriter(new FileStream(modPath + patchPath + "/layout.toc", FileMode.Create), inWriteHeader: true))
                {
                    dbWriter.Write(layoutToc);
                }
            }


            if (UseModData)
            {
                logger.Log("Copying initfs_win32");
                Directory.CreateDirectory(modPath + patchPath);
                CopyFileIfRequired(fs.BasePath + patchPath + "/initfs_win32", modPath + patchPath + "/initfs_win32");
            }




            return FrostyModsFound;
        }

        /// <summary>
        /// Constructs the IFrostbiteMod into the Dictionary
        /// </summary>
        /// <param name="modPath"></param>
        /// <param name="FrostyModsFound"></param>
        /// <param name="frostbiteMods"></param>
        private void ReadFrostbiteMods(string modPath, ref bool FrostyModsFound, ref Dictionary<Stream, IFrostbiteMod> frostbiteMods)
        {
            FileInfo fileInfo = new FileInfo(modPath);
            Stream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);

            Stream modStream = null;
            modStream = fs;

            Logger.Log("Loading mod " + fileInfo.Name);
            if (modPath.Contains(".fbmod", StringComparison.OrdinalIgnoreCase))
            {
                FrostyModsFound = true;
                frostbiteMods.Add(modStream, new FrostbiteMod(fs, fileInfo.FullName));
            }
            if (modPath.Contains(".fifamod", StringComparison.OrdinalIgnoreCase))
            {
                FrostyModsFound = true;
                frostbiteMods.Add(modStream, new FIFAMod(string.Empty, fileInfo.FullName));
            }
        }

        public bool ForceRebuildOfMods = false;

        public static string ApplicationDirectory
        {
            get
            {
                //return System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                return AppContext.BaseDirectory;
            }
        }

        public string LastLaunchedModsPath
        {
            get
            {
                return fs.BasePath + "\\" + "FMT.LastLaunchedMods.json";
            }
        }

        public string LastPatchedVersionPath
        {
            get
            {
                return fs.BasePath + "\\" + "FMT.LastPatchedVersion.json";
            }
        }


        public static bool UseModData { get; set; } = true;

        public bool GameWasPatched { get; set; }

        public bool DeleteLiveUpdates { get; set; } = true;

        public bool LowMemoryMode { get; set; } = false;

        public async Task<bool> Run(ILogger inLogger, string gameRootPath, params string[] modPaths)
        {
            if (string.IsNullOrEmpty(gameRootPath))
                throw new ArgumentNullException(nameof(gameRootPath));

            //if (string.IsNullOrEmpty(modsRootPath))
            //    throw new ArgumentNullException(nameof(modsRootPath));

            // -----------------------------------------------------------------------------------
            // Reset the GAME_DATA_DIR
            //Environment.SetEnvironmentVariable("dataPath", "", EnvironmentVariableTarget.User);
            //Environment.SetEnvironmentVariable("GAME_DATA_DIR", "", EnvironmentVariableTarget.User);


            Logger = inLogger;

            if (FileSystem.Instance != null)
            {
                fs = FileSystem.Instance;
            }
            else
            {
                fs = new FileSystem(gameRootPath);
                fs.Initialize();
            }

            // -----------------------------------------------------------------------------------
            // Always uninstall InstallerData.xml change
            if (ProfileManager.IsFIFA23DataVersion())
            {
                ConfigureInstallerDataXml(false);
            }

            //string modPath = fs.BasePath + modDirName + "\\";
            string modPath = "\\" + modDirName + "\\";

            var foundMods = false;
            var lastModPaths = new Dictionary<string, DateTime>();
            if (File.Exists(LastLaunchedModsPath))
            {
                var LastLaunchedModsData = File.ReadAllText(LastLaunchedModsPath);
                lastModPaths = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(LastLaunchedModsData);
            }
            var sameCountAsLast = lastModPaths.Count == modPaths.Count();
            var sameAsLast = sameCountAsLast;// && lastModPaths.Equals(modPaths);
            if (sameCountAsLast)
            {
                foreach (FileInfo f in modPaths.Select(x => new FileInfo(x)))
                {
                    if (f.Exists)
                    {
                        if (f.Extension.Contains("fbmod", StringComparison.OrdinalIgnoreCase) || f.Extension.Contains("fifamod", StringComparison.OrdinalIgnoreCase))
                            foundMods = true;

                        if (lastModPaths.ContainsKey(f.FullName))
                        {
                            sameAsLast = (f.LastWriteTime == lastModPaths[f.FullName]);
                            if (!sameAsLast)
                                break;
                        }
                        else
                        {
                            sameAsLast = false;
                        }
                    }
                    else
                    {
                        sameAsLast = false;
                        break;
                    }
                }
            }
            else
            {
                sameAsLast = false;
            }

            // Delete the Live Updates
            RunDeleteLiveTuningUpdates();

            // ---------------------------------------------
            // Load Last Patched Version
            uint? lastHead = null;
            var LastHeadData = new Dictionary<string, uint>();
            if (File.Exists(LastPatchedVersionPath))
            {
                LastHeadData = JsonConvert.DeserializeObject<Dictionary<string, uint>>(File.ReadAllText(LastPatchedVersionPath));
                if (LastHeadData.ContainsKey(fs.BasePath))
                {
                    lastHead = LastHeadData[fs.BasePath];
                }
            }

            //// Notify if new Patch detected
            if (fs.Head != lastHead)
            {
                Logger.Log("Detected New Version of " + ProfileManager.ProfileName + ".exe, rebuilding mods");
                // If new patch detected, force rebuild of mods
                sameAsLast = false;
                GameWasPatched = true;
                await Task.Delay(1000);
            }

            {
                // Notify if NO changes are made to mods
                if (sameAsLast && !ForceRebuildOfMods)
                {
                    Logger.Log("Detected NO changes in mods for " + ProfileManager.ProfileName + ".exe");
                    await Task.Delay(1000);
                }
                // Rebuild mods
                else
                {
                    foundMods = await BuildModData(inLogger, modPaths);
                    lastModPaths.Clear();
                    foreach (FileInfo f in modPaths.Select(x => new FileInfo(x)))
                    {
                        lastModPaths.Add(f.FullName, f.LastWriteTime);
                    }

                    // Save Last Launched Mods
                    File.WriteAllText(LastLaunchedModsPath, JsonConvert.SerializeObject(lastModPaths));
                    // ----------

                    // ---------------------------------------------
                    // Save Last Patched Version
                    lastHead = fs.Head;

                    if (LastHeadData.ContainsKey(fs.BasePath))
                        LastHeadData[fs.BasePath] = lastHead.Value;
                    else
                        LastHeadData.Add(fs.BasePath, lastHead.Value);

                    File.WriteAllText(LastPatchedVersionPath, JsonConvert.SerializeObject(LastHeadData));

                    // ---------------------------------------------

                }
            }

            //- -----------------------
            // Clear out the memory of archive data after compilation and before launching the game
            archiveData.Clear();
            GC.Collect();

            RemoveDXGIAndCryptBaseDll();

            RunFIFA23Setup();

            // Delete the Live Updates
            RunDeleteLiveTuningUpdates();

            //RunSetupFIFAConfig();
            //RunPowershellToUnblockDLLAtLocation(fs.BasePath);
            var fifaconfigexelocation = fs.BasePath + "\\FIFASetup\\fifaconfig.exe";
            var fifaconfigexe_origlocation = fs.BasePath + "\\FIFASetup\\fifaconfig_orig.exe";
            FileInfo fIGameExe = new FileInfo(GameEXEPath);
            FileInfo fiFifaConfig = new FileInfo(Path.Combine(AppContext.BaseDirectory, "thirdparty", "fifaconfig.exe"));

            //if (ProfileManager.IsFIFA21DataVersion()
            //    || ProfileManager.IsFIFA22DataVersion()
            //    //|| ProfilesLibrary.IsFIFA23DataVersion()
            //    )
            //{
            //    CopyFileIfRequired("ThirdParty/CryptBase.dll", fs.BasePath + "CryptBase.dll");
            //}

            CopyFileIfRequired(fs.BasePath + "user.cfg", modPath + "user.cfg");
            if ((ProfileManager.IsFIFADataVersion()
                || ProfileManager.IsFIFA21DataVersion()
                || ProfileManager.IsFIFA22DataVersion())
                && UseModData)
            {
                if (!new FileInfo(fs.BasePath + "\\FIFASetup\\fifaconfig_orig.exe").Exists)
                {
                    FileInfo fileInfo10 = new FileInfo(fs.BasePath + "\\FIFASetup\\fifaconfig.exe");
                    fileInfo10.MoveTo(fileInfo10.FullName.Replace(".exe", "_orig.exe"));
                }
                CopyFileIfRequired("thirdparty/fifaconfig.exe", fs.BasePath + "\\FIFASetup\\fifaconfig.exe");
            }
            else if (new FileInfo(fifaconfigexe_origlocation).Exists)
            {
                File.Delete(fifaconfigexelocation); // delete the addon
                File.Move(fifaconfigexe_origlocation, fifaconfigexelocation); // replace
            }

            //if (foundMods && UseModData)// || sameAsLast)
            //{
            //    Logger.Log("Launching game: " + fs.BasePath + ProfilesLibrary.ProfileName + ".exe (with Frostbite Mods in ModData)");
            //    ExecuteProcess(fs.BasePath + ProfilesLibrary.ProfileName + ".exe", "-dataPath \"" + modPath.Trim('\\') + "\" " + "");
            //}
            //else 
            //var dataPathArgument = "-dataPath \"" + modPath.Trim('\\') + "\" " + "";
            var dataPathArgument = "-dataPath ModData";
            var arguments = dataPathArgument
                //+ " " + fifaNonRetailArgument
                //+ " " + dataModulesPathArgument
                //+ " " + noConfigArgument
                ;



            if (foundMods && !UseModData)
            {
                Logger.Log("Launching game: " + fs.BasePath + ProfileManager.ProfileName + ".exe (with Frostbite Mods)");
                await ExecuteProcess(fs.BasePath + ProfileManager.ProfileName + ".exe", "");
            }
            else if (UseModData)
            {
                if (EADesktopIsInstalled)
                {
                    RunEADesktop();
                    LaunchedViaEADesktop = true;
                }
                else
                {
                    if (!foundMods)
                        Logger.Log("Launching game: " + fs.BasePath + ProfileManager.ProfileName + ".exe");
                    else
                        Logger.Log("Launching game: " + fs.BasePath + ProfileManager.ProfileName + ".exe (with Frostbite Mods in ModData)");

                    await ExecuteProcess(GameEXEPath, arguments);
                }
            }
            else
            {
                Logger.Log("Launching game: " + fs.BasePath + ProfileManager.ProfileName + ".exe");
                await ExecuteProcess(fs.BasePath + ProfileManager.ProfileName + ".exe", "");
            }

            if (UseACBypass && ProfileManager.IsFIFA23DataVersion())
            {
                var r = GameInstanceSingleton.InjectDLL(new FileInfo(@"ThirdParty\\FIFA23\\FIFA.dll").FullName, true).Result;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return true;
        }

        private void RemoveDXGIAndCryptBaseDll()
        {
            // --------------------------------------------------------------
            // Unistall that crappy dxgi "fix" ------------------------------
            if (File.Exists(FileSystem.Instance.BasePath + "\\dxgi.dll"))
                File.Delete(FileSystem.Instance.BasePath + "\\dxgi.dll");

            // --------------------------------------------------------------
            // Cryptbase.dll no longer used ------------------------------
            if (File.Exists(FileSystem.Instance.BasePath + "CryptBase.dll"))
                File.Delete(FileSystem.Instance.BasePath + "CryptBase.dll");
        }

        private async void RunEADesktop()
        {
            FileLogger.WriteLine("ModExecutor:RunEADesktop");

            Logger.Log($"Launching {ProfileManager.DisplayName} via EADesktop.");

            // -------------------------------------------------------------------------------------------------------------------
            // IF EADesktopCommandLineSetting hasn't been set. Throw exception. This process requires EADesktopCommandLineSetting
            if (string.IsNullOrEmpty(ProfileManager.LoadedProfile.EADesktopCommandLineSetting))
            {
                FileLogger.WriteLine($"ModExecutor:RunEADesktop: Profile's EADesktopCommandLineSetting has not been set in {ProfileManager.ProfileName}Profile.json. Please set this before using EADesktop with this game.");
                throw new Exception($"ModExecutor:RunEADesktop: Profile's EADesktopCommandLineSetting has not been set in {ProfileManager.ProfileName}Profile.json. Please set this before using EADesktop with this game.");
            }

            // ----------------------------------------------------------------------------------
            // edit user_*.ini with -dataPath ModData
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists(localAppDataPath))
            {
                FileLogger.WriteLine("ModExecutor:RunEADesktop: Unable to find LocalApplicationData");
                throw new DirectoryNotFoundException("ModExecutor:RunEADesktop: Unable to find LocalApplicationData");
            }
            var eaDesktopConfigAppDataPath = Path.Combine(localAppDataPath, "Electronic Arts", "EA Desktop");
            if (!Directory.Exists(eaDesktopConfigAppDataPath))
            {
                FileLogger.WriteLine("ModExecutor:RunEADesktop: Unable to find EA Desktop Directory in LocalApplicationData");
                throw new DirectoryNotFoundException("ModExecutor:RunEADesktop: Unable to find EA Desktop Directory in LocalApplicationData");
            }
            var userIniPaths = Directory.GetFiles(eaDesktopConfigAppDataPath, "user_*.ini");
            if (!userIniPaths.Any())
            {
                FileLogger.WriteLine("ModExecutor:RunEADesktop: Unable to find user *.ini to apply -dataPath=ModData");
                throw new FileNotFoundException("Unable to find user *.ini to apply -dataPath=ModData. Please ensure EADesktop is properly installed and run at least once!");
            }

            var desiredCommandLineSetting = ProfileManager.LoadedProfile.EADesktopCommandLineSetting + "=-dataPath ModData";
            foreach (var userIniPath in userIniPaths)
            {
                var allTextOfUserIni = await File.ReadAllTextAsync(userIniPath);
                StringBuilder sb = new StringBuilder(allTextOfUserIni);
                if (!allTextOfUserIni.Contains(ProfileManager.LoadedProfile.EADesktopCommandLineSetting)
                    || !allTextOfUserIni.Contains(desiredCommandLineSetting)
                    )
                {
                    FileLogger.WriteLine("ModExecutor:RunEADesktop: -dataPath=ModData does not exist for this game. Setting it up.");

                    // ----------------------------------------------------------------------------------
                    // If we have to make the change. Find opened EA Desktop process and close it
                    try
                    {
                        var eaDesktopProcesses = Process.GetProcessesByName("EADesktop");
                        if (eaDesktopProcesses != null)
                        {
                            foreach (var proc in eaDesktopProcesses)
                            {
                                FileLogger.WriteLine($"ModExecutor:RunEADesktop: Killing {proc.ProcessName} to apply changes");
                                Logger.Log("Killing EADesktop process to apply changes");
                                proc.Kill();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.WriteLine("ModExecutor:RunEADesktop: Unable to Kill EA Process. You may need to do this manually before running the game.");
                        FileLogger.WriteLine(ex.ToString());
                    }

                    sb.AppendLine(string.Empty);
                    sb.Append(desiredCommandLineSetting);
                    sb.AppendLine(string.Empty);
                    var finalUserIniText = sb.ToString();
                    // ----------------------------------------------------------------------------------
                    // Write the new config for this game
                    await File.WriteAllTextAsync(userIniPath, finalUserIniText);
                }

            }

            await ExecuteProcess(fs.BasePath + ProfileManager.ProfileName + ".exe", "");
            //ExecuteCommand("start \"" + fs.BasePath + ProfileManager.ProfileName + ".exe\"");
        }

        private void RunFIFA23Setup()
        {
            if (!ProfileManager.IsFIFA23DataVersion())
                return;

            // --------------------------------------------------------------
            // Unistall that crappy dxgi "fix" ------------------------------
            if (File.Exists(FileSystem.Instance.BasePath + "\\dxgi.dll"))
                File.Delete(FileSystem.Instance.BasePath + "\\dxgi.dll");

            // --------------------------------------------------------------
            // Cryptbase.dll no longer used ------------------------------
            if (File.Exists(fs.BasePath + "CryptBase.dll"))
                File.Delete(fs.BasePath + "CryptBase.dll");

            // --------------------------------------------------------------
            // 
            if (UseACBypass)
            {
                ConfigureInstallerDataXml(true);
            }
            else
            {
                ConfigureInstallerDataXml(false);
            }
        }

        private static void ConfigureInstallerDataXml(bool install = true)
        {
            var installerXmlPath = FileSystem.Instance.BasePath + "\\__Installer\\installerdata.xml";

            if (!File.Exists(installerXmlPath))
                throw new FileNotFoundException($"Unable to find installer data for {ProfileManager.DisplayName} at path {installerXmlPath}");

            if (install && !File.Exists(installerXmlPath + ".bak"))
                File.Copy(installerXmlPath, installerXmlPath + ".bak", false);
            // Uninstalling -------------------------------------------------
            else if (!install && File.Exists(installerXmlPath + ".bak"))
            {
                File.Copy(installerXmlPath + ".bak", installerXmlPath, true);
                File.Delete(installerXmlPath + ".bak");
            }

            // Load from file -----------------------------------------------
            XmlDocument xmldoc = new XmlDocument();
            using (FileStream fs = new FileStream(installerXmlPath, FileMode.Open, FileAccess.Read))
            {
                xmldoc.Load(fs);
            }
            XmlNode xmlnode = xmldoc.GetElementsByTagName("runtime").Item(0);
            var secondNode = xmlnode.ChildNodes.Item(1);

            // Installing ---------------------------------------------------
            if (install)
            {
                secondNode.InnerXml = secondNode.InnerXml.Replace("]EAAntiCheat.GameServiceLauncher.exe", "]FIFA23.exe", StringComparison.OrdinalIgnoreCase);
            }
            // Uninstalling -------------------------------------------------
            else
            {
                secondNode.InnerXml = secondNode.InnerXml.Replace("]FIFA23.exe", "]EAAntiCheat.GameServiceLauncher.exe", StringComparison.OrdinalIgnoreCase);
            }
            // Save to file -------------------------------------------------
            using (FileStream fs = new FileStream(installerXmlPath, FileMode.Open, FileAccess.Write))
            {
                xmldoc.Save(fs);
            }
        }

        /// <summary>
        /// Deletes the Temporary folder with updates from EA in it
        /// </summary>
        private void RunDeleteLiveTuningUpdates()
        {
            if (!DeleteLiveUpdates)
                return;

            try
            {
                if (ProfileManager.IsMadden20DataVersion() || ProfileManager.IsMadden21DataVersion(ProfileManager.Game))
                {
                    string path = Environment.ExpandEnvironmentVariables("%ProgramData%\\Frostbite\\Madden NFL 20");
                    if (Directory.Exists(path))
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(path);
                        directoryInfo.Delete(true);
                    }

                    path = Environment.ExpandEnvironmentVariables("%ProgramData%\\Frostbite\\Madden NFL 21");
                    if (Directory.Exists(path))
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(path);
                        // delete or throw???
                        directoryInfo.Delete(true);
                    }
                }

                var pathToFIFATempCacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", ProfileManager.DisplayName);
                if (Directory.Exists(pathToFIFATempCacheFolder))
                {
                    Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", ProfileManager.DisplayName), recursive: true);

                    Logger.Log("Successfully deleted the Live Updates folder.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] Failed to delete Live Updates folder with message: {ex.Message}.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void RunSetupFIFAConfig()
        {
            if (ProfileManager.IsFIFA21DataVersion())
            {
                var configIni = new FileInfo(fs.BasePath + "\\FIFASetup\\config.ini");
                if (configIni.Exists)
                {
                    StringBuilder newConfig = new StringBuilder();
                    newConfig.AppendLine("LAUNCH_EXE = fifa21.exe");
                    newConfig.AppendLine("SETTING_FOLDER = 'FIFA 21'");
                    newConfig.AppendLine("AUTO_LAUNCH = 1");
                    File.WriteAllText(configIni.FullName, newConfig.ToString());
                }

            }

            if (ProfileManager.IsFIFA22DataVersion())
            {
                var configIni = new FileInfo(fs.BasePath + "\\FIFASetup\\config.ini");
                if (configIni.Exists)
                {
                    StringBuilder newConfig = new StringBuilder();
                    newConfig.AppendLine($"LAUNCH_EXE = {ProfileManager.CacheName.ToLower()}.exe");
                    newConfig.AppendLine("SETTING_FOLDER = 'FIFA 22'");
                    newConfig.AppendLine("AUTO_LAUNCH = 1");
                    File.WriteAllText(configIni.FullName, newConfig.ToString());
                }


            }
        }

        //private void RecursiveDeleteFiles(string path)
        //{
        //    DirectoryInfo directoryInfo = new DirectoryInfo(path);
        //    DirectoryInfo[] directories = directoryInfo.GetDirectories();
        //    FileInfo[] files = directoryInfo.GetFiles();
        //    foreach (FileInfo fileInfo in files)
        //    {
        //        if ((fileInfo.Extension == ".cat" || fileInfo.Extension == ".toc" || fileInfo.Extension == ".sb" || fileInfo.Name.ToLower() == "mods.txt") && !(fileInfo.Name.ToLower() == "layout.toc"))
        //        {
        //            fileInfo.Delete();
        //        }
        //    }
        //    DirectoryInfo[] array = directories;
        //    foreach (DirectoryInfo directoryInfo2 in array)
        //    {
        //        string path2 = Path.Combine(path, directoryInfo2.Name);
        //        RecursiveDeleteFiles(path2);
        //    }
        //}

        public void ExecuteCommand(string Command)
        {
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + Command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = true;

            Process = Process.Start(ProcessInfo);
        }

        public async Task ExecuteProcess(string processName, string args, bool waitForExit = false, bool asAdmin = false)
        {
            FileLogger.WriteLine($"Launching {processName} {args}");
            Logger.Log($"Launching {processName} {args}");
            //Process p = new Process();
            //p.StartInfo.FileName = "cmd.exe";
            //p.StartInfo.Arguments = $"/K \"\"{processName}\" \"{args}\"\"";
            //p.Start();

            using (Process process = new Process())
            {
                FileInfo fileInfo = new FileInfo(processName);
                process.StartInfo.FileName = processName;
                process.StartInfo.WorkingDirectory = fileInfo.DirectoryName;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                if (asAdmin)
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runas";
                }
                process.Start();
                while(true)
                {
                    await Task.Delay(1000);
                    if (Process.GetProcesses().Any(x => x.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)))
                        break;
                }
            }
            //FileInfo fileInfo = new FileInfo(processName);
            //Process.Start(new ProcessStartInfo
            //{
            //    FileName = fileInfo.FullName,
            //    WorkingDirectory = fileInfo.DirectoryName,
            //    Arguments = args,
            //    UseShellExecute = false
            //});
        }

        private byte[] GetResourceData(string modFilename, int archiveIndex, long offset, int size)
        {
            string path = modFilename.Replace(".fbmod", "_" + archiveIndex.ToString("D2") + ".archive");
            if (!File.Exists(path))
            {
                return null;
            }
            using (NativeReader nativeReader = new NativeReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
            {
                nativeReader.Position = offset;
                return nativeReader.ReadBytes(size);
            }
        }

        private byte[] GetResourceData(Stream stream, long offset, int size)
        {
            using (NativeReader nativeReader = new NativeReader(stream))
            {
                nativeReader.Position = offset;
                return nativeReader.ReadBytes(size);
            }
        }

        private void CopyFileIfRequired(string source, string dest)
        {
            FileInfo fileInfo = new FileInfo(source);
            FileInfo fileInfo2 = new FileInfo(dest);
            if (fileInfo.Exists && (!fileInfo2.Exists || (fileInfo2.Exists && fileInfo.LastWriteTimeUtc > fileInfo2.LastWriteTimeUtc) || fileInfo.Length != fileInfo2.Length))
            {
                File.Copy(fileInfo.FullName, fileInfo2.FullName, overwrite: true);
            }
        }

        public static async void RunPowershellToUnblockDLLAtLocation(string loc)
        {
            var psCommmand = $"dir \"{loc}\" -Recurse|Unblock-File";
            var psCommandBytes = System.Text.Encoding.Unicode.GetBytes(psCommmand);
            var psCommandBase64 = Convert.ToBase64String(psCommandBytes);

            var startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy unrestricted -WindowStyle hidden -EncodedCommand {psCommandBase64}",
                UseShellExecute = true
            };
            startInfo.Verb = "runAs";
            Process.Start(startInfo);

            await Task.Delay(9000);
        }
    }
}
