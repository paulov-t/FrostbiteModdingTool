using CSharpImageLibrary;
using FMT.FileTools;
using FMT.Logging;
using Frostbite.FileManagers;
using Frostbite.Textures;
using FrostbiteSdk;
using FrostbiteSdk.Frostbite.FileManagers;
using FrostbiteSdk.FrostbiteSdk.Managers;
using FrostySdk.Ebx;
using FrostySdk.Frostbite;
using FrostySdk.Frostbite.IO;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Resources;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FrostySdk.Managers
{
    public class AssetManager : IDisposable
    {
        public static AssetManager Instance
        {
            get;set;
        }

        //public Dictionary<int, string> ModCASFiles = new Dictionary<int, string>();

        //private const ulong CacheMagic = 144213406785688134uL;

        //private const uint CacheVersion = 2u;

        public FileSystem FileSystem { get; private set; } = FileSystem.Instance;

        public ILogger Logger { get; private set; }

        public List<SuperBundleEntry> superBundles { get; private set; } = new List<SuperBundleEntry>(500);

        public List<BundleEntry> Bundles { get; private set; } = new List<BundleEntry>(350000);

        public ConcurrentDictionary<string, EbxAssetEntry> EBX { get; private set; }// = new ConcurrentDictionary<string, EbxAssetEntry>(4, 500000, StringComparer.OrdinalIgnoreCase);

        public ConcurrentDictionary<string, ResAssetEntry> RES { get; private set; }// = new ConcurrentDictionary<string, ResAssetEntry>(4, 350000);

        public ConcurrentDictionary<Guid, ChunkAssetEntry> Chunks { get; private set; }// = new ConcurrentDictionary<Guid, ChunkAssetEntry>(4, 1);

        public ConcurrentDictionary<int, ChunkAssetEntry> SuperBundleChunks { get; private set; } = new ConcurrentDictionary<int, ChunkAssetEntry>();

        public ConcurrentDictionary<ulong, ResAssetEntry> resRidList { get; private set; } = new ConcurrentDictionary<ulong, ResAssetEntry>();

        public IEnumerable<IAssetEntry> ModifiedEntries
        {
            get
            {
                var e = EBX.Values.Where(e => e.IsModified).Select(x => (IAssetEntry)x);
                var r = RES.Values.Where(e => e.IsModified).Select(x => (IAssetEntry)x);
                var c = Chunks.Values.Where(e => e.IsModified).Select(x => (IAssetEntry)x);
                //var custom = EBX.Values.Where(e => e.IsModified).Select(x => (IAssetEntry)x);
                //return e.Union(r).Union(c).Union(custom);
                return e.Union(r).Union(c);

            }
        }

        public Dictionary<string, ICustomAssetManager> CustomAssetManagers { get; private set; } = new Dictionary<string, ICustomAssetManager>(1);

        public List<EmbeddedFileEntry> EmbeddedFileEntries { get; private set; } = new List<EmbeddedFileEntry>();

        public LocaleIniManager LocaleINIMod { get; private set; }


        public InitFSManager InitFSManager { get; private set; }

        public AssetManager()
        {
            if (Instance != null)
                throw new Exception("There can only be one instance of the AssetManager");

            EBX = new ConcurrentDictionary<string, EbxAssetEntry>();
            RES = new ConcurrentDictionary<string, ResAssetEntry>();
            Chunks = new ConcurrentDictionary<Guid, ChunkAssetEntry>();
            Instance = this;

            LocaleINIMod = new LocaleIniManager();
            InitFSManager = new InitFSManager();
        }

        public AssetManager(in ILogger inLogger) : this()
        {
            Logger = inLogger;
        }

        // To detect redundant calls
        private bool _disposed = false;

        ~AssetManager() => Dispose(false);

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            //GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            // Managed Resources
            if (disposing)
            {
                FileSystem.Instance = null;
                AssetManagerInitialised = null;
                AssetManagerModified = null;

                PluginsInitialised = false;
                PluginAssemblies.Clear();

                CustomAssetManagers.Clear();
                ChunkFileManager.Instance = null;
                //if (AllSdkAssemblyTypes != null)
                //{
                //    AllSdkAssemblyTypes.Clear();
                //    AllSdkAssemblyTypes = null;
                //}
                if(CachedTypes != null && CachedTypes.Any())
                {
                    CachedTypes.Clear();
                    CachedTypes = null;
                }
                Bundles.Clear();
                Bundles = null;
                foreach (var c in EBX)
                {
                    EBX[c.Key] = null;
                }
                EBX.Clear();
                EBX = null;
                RES.Clear();
                RES = null;
                resRidList.Clear();
                resRidList = null;
                foreach (var c in Chunks)
                {
                    Chunks[c.Key] = null;
                }
                Chunks.Clear();
                Chunks = null;
                SuperBundleChunks.Clear();
                SuperBundleChunks = null;
                LocaleINIMod = null;
                TypeLibrary.ExistingAssembly = null;

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            }
        }

        //public void RegisterCustomAssetManager(string type, Type managerType)
        //{
        //	CustomAssetManagers.Add(type, (ICustomAssetManager)Activator.CreateInstance(managerType));
        //}

        public void RegisterLegacyAssetManager()
        {
            if (!InitialisePlugins() && !ProfileManager.DoesNotUsePlugin)
            {
                throw new Exception("Plugins could not be initialised!");
            }

            if (!string.IsNullOrEmpty(ProfileManager.LegacyFileManager))
            {
                ICustomAssetManager cam;
                cam = (ICustomAssetManager)LoadTypeFromPlugin(ProfileManager.LegacyFileManager);
                if (cam == null)
                    cam = (ICustomAssetManager)LoadTypeByName(ProfileManager.LegacyFileManager);


                if (cam != null && !CustomAssetManagers.ContainsKey("legacy"))
                    CustomAssetManagers.Add("legacy", cam);

            }

        }

        protected bool PluginsInitialised { get; set; }

        private static List<string> PluginAssemblies = new List<string>();

        public bool InitialisePlugins()
        {
            if (PluginsInitialised)
                return true;

            if (ProfileManager.DoesNotUsePlugin)
            {
                FileLogger.WriteLine($"{ProfileManager.ProfileName} does not use a Plugin.");
                return false;
            }

            var pluginsPath = Path.Combine(AppContext.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginsPath))
            {
                var pluginsDirectoryDoesntExistErrorMessage = $"Plugins Directory does not exist. Please reinstall FMT.";
                FileLogger.WriteLine(pluginsDirectoryDoesntExistErrorMessage);
                throw new Exception(pluginsDirectoryDoesntExistErrorMessage);
            }

            var pluginAssemblies = Directory.EnumerateFiles(pluginsPath)
                .Select(x => new FileInfo(x))
                .Where(x => x.Extension.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

            if (!pluginAssemblies.Any())
            {
                var pluginAssembliesDontExistErrorMessage = $"No Plugins in {pluginsPath} found. Please reinstall FMT.";
                FileLogger.WriteLine(pluginAssembliesDontExistErrorMessage);
                throw new Exception(pluginAssembliesDontExistErrorMessage);
            }

            foreach (var fiPlugin in pluginAssemblies)
            {
                if (fiPlugin.Name.Contains(ProfileManager.ProfileName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                {
                    if (Assembly.UnsafeLoadFrom(fiPlugin.FullName) != null)
                    {
                        if (!PluginAssemblies.Contains(fiPlugin.FullName))
                            PluginAssemblies.Add(fiPlugin.FullName);

                        PluginsInitialised = true;

                        return true;
                    }
                }
            }

            return false;
        }

        public static bool CacheUpdate = false;

        public object LoadTypeFromPluginByInterface(string interfaceName, params object[] args)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()
               .Where(x => x.FullName.Contains("Plugin", StringComparison.OrdinalIgnoreCase)))
            {
                var t = a.GetTypes().FirstOrDefault(x => x.GetInterface(interfaceName) != null);
                if (t != null)
                {
                    return Activator.CreateInstance(t, args: args);
                }
            }
            return null;
        }

        public object LoadTypeFromPlugin(string className, params object[] args)
        {
            if (CachedTypes == null)
                CachedTypes = new Dictionary<string, Type>();

            if (CachedTypes.Any() && CachedTypes.ContainsKey(className))
            {
                var t = CachedTypes[className];
                return Activator.CreateInstance(type: t, args: args);
            }

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()
                //.Where(x => x.FullName.Contains(ProfilesLibrary.ProfileName + "Plugin", StringComparison.OrdinalIgnoreCase)))
                .Where(x => x.FullName.Contains("Plugin", StringComparison.OrdinalIgnoreCase)))
            {
                var t = a.GetTypes().FirstOrDefault(x => x.Name == className);
                if (t != null)
                {
                    CachedTypes.Add(className, t);
                    return Activator.CreateInstance(t, args: args);
                }
            }
            return null;
        }


        public static object LoadTypeFromPlugin2(string className, params object[] args)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()
                //.Where(x => x.FullName.Contains(ProfilesLibrary.ProfileName + "Plugin", StringComparison.OrdinalIgnoreCase)))
                .Where(x => x.FullName.Contains("Plugin", StringComparison.OrdinalIgnoreCase)))
            {
                var t = a.GetTypes().FirstOrDefault(x => x.Name == className);
                if (t != null)
                {
                    return Activator.CreateInstance(t, args: args);
                }
            }
            return null;
        }

        public static Dictionary<string, Type> CachedTypes = new Dictionary<string, Type>();

        public static object LoadTypeByName(string className, params object[] args)
        {
            var errorText = $"Unable to find Class {className}";
            var exc = new ArgumentNullException(errorText);

            if (CachedTypes.Any() && CachedTypes.ContainsKey(className))
            {
                var cachedType = CachedTypes[className];
                exc = null;
                return Activator.CreateInstance(type: cachedType, args: args);
            }

            IEnumerable<Assembly> currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (currentAssemblies == null)
                throw exc;

            Type t = null;
            foreach(var assm in currentAssemblies)
            {
                if (assm.FullName.Contains("EbxClasses"))
                    continue;

                Type[] tps;
                try
                {
                    tps = assm.GetTypes();
                }
                catch
                {
                    continue;
                }
                for (var iT = 0; iT < tps.Length; iT++)
                {
                    var ty = tps[iT];
                    if (ty.FullName.EndsWith(className, StringComparison.OrdinalIgnoreCase))
                    {
                        t = ty;
                        break;
                    }
                }

                if (t != null)
                    break;
            }

            if (t != null)
            {
                if (!CachedTypes.ContainsKey(className))
                {
                    CachedTypes.Add(className, t);
                }
                exc = null;
                return Activator.CreateInstance(type: t, args: args);
            }

            throw exc;
        }


        public void Initialize(bool additionalStartup = true, AssetManagerImportResult result = null)
        {
            if (Logger == null)
                Logger = new NullLogger();

            DateTime dtAtStart = DateTime.Now;

            if (!ProfileManager.LoadedProfile.CanUseModData)
            {
                Logger.Log($"[WARNING] {ProfileManager.LoadedProfile.DisplayName} ModData is not supported. Making backups of your files!");
                FileSystem.MakeGameDataBackup(FileSystem.BasePath);
            }

            Logger.Log("Initialising Plugins");
            if (!InitialisePlugins() && !ProfileManager.DoesNotUsePlugin)
            {
                throw new Exception("Plugins could not be initialised!");
            }
            TypeLibrary.Initialize(additionalStartup || TypeLibrary.RequestLoadSDK);
            if (TypeLibrary.RequestLoadSDK && File.Exists("SDK/" + ProfileManager.SDKFilename + ".dll"))
            {
                Logger.Log($"Plugins and SDK {"SDK/" + ProfileManager.SDKFilename + ".dll"} Initialised");
            }


            //if (!additionalStartup || TypeLibrary.ExistingAssembly == null)
            //{
            //	return;
            //}
            List<EbxAssetEntry> prePatchCache = new List<EbxAssetEntry>();
            bool writtenToCache = false;
            if (!CacheRead(out prePatchCache))
            {
                Logger.Log($"Cache Needs to Built/Updated");

                BinarySbDataHelper binarySbDataHelper = new BinarySbDataHelper(this);
                if (ProfileManager.AssetLoader != null)
                    ((IAssetLoader)Activator.CreateInstance(ProfileManager.AssetLoader)).Load(this, binarySbDataHelper);
                else
                {
                    ((IAssetLoader)LoadTypeByName(ProfileManager.AssetLoaderName)).Load(this, binarySbDataHelper);
                    //((IAssetLoader)LoadTypeFromPlugin(ProfileManager.AssetLoaderName)).Load(this, binarySbDataHelper);
                }
                GC.Collect();
                CacheWrite();
                writtenToCache = true;
            }

            if (!additionalStartup || TypeLibrary.ExistingAssembly == null)
            {
                return;
            }

            if (writtenToCache)
                DoEbxIndexing();

            // Load these when you need them!
            //         foreach (ICustomAssetManager value in CustomAssetManagers.Values)
            //{
            //	value.Initialize(logger);
            //}

            TimeSpan timeSpan = DateTime.Now - dtAtStart;
            Logger.Log($"Loading complete {timeSpan.ToString(@"mm\:ss")}");

            if (AssetManagerInitialised != null)
            {
                AssetManagerInitialised(null);
            }

            _ = EBX;
            _ = RES;
            _ = Chunks;
        }

        public delegate void AssetManagerModifiedHandler(IAssetEntry modifiedAsset);

        public static event AssetManagerModifiedHandler AssetManagerInitialised;

        public static event AssetManagerModifiedHandler AssetManagerModified;


        //private List<Type> AllSdkAssemblyTypes { get; set; }

        private List<EbxAssetEntry> _EbxItemsWithNoType;
        private List<EbxAssetEntry> EbxItemsWithNoType
        {
            get
            {
                if (_EbxItemsWithNoType == null)
                {
                    _EbxItemsWithNoType = EBX.Values
                        .Where(x => x.ExtraData != null)
                        .Where(
                        x => string.IsNullOrEmpty(x.Type)
                        || x.Type == "UnknownType"
                    ).OrderBy(x => x.ExtraData.CasPath).ToList();

                    if(_EbxItemsWithNoType.Count == 0)
                    {
                        _EbxItemsWithNoType = EBX.Values
                            .Where(
                            x => string.IsNullOrEmpty(x.Type)
                            || x.Type == "UnknownType"
                        ).ToList();
                    }
                }

                return _EbxItemsWithNoType;
            }

        }

        public bool ForceChunkRemoval { get; set; }


        public Type EbxReaderType { get; set; }
        //public EbxReader EbxReaderInstance { get; set; }

        public void UpdateEbxListItem(EbxAssetEntry ebx)
        {
            //if (ProfileManager.DataVersion == 20230922)
            //{
            //    return;
            //}


            if (string.IsNullOrEmpty(ebx.Type))
            {
                using (Stream ebxStream = GetEbxStream(ebx))
                {
                    if (ebxStream != null && ebxStream.Length > 0)
                    {

                        if (!string.IsNullOrEmpty(ProfileManager.EBXReader))
                        {
                            if (EbxReaderType == null)
                            {
                                //                        EbxReaderInstance = (EbxReader)LoadTypeByName(ProfileManager.EBXReader, ebxStream, true);
                                //EbxReaderType = EbxReaderInstance.GetType();
                            }
                            ebxStream.Position = 0;
                            //EbxReaderInstance = (EbxReader)Activator.CreateInstance(EbxReaderType, ebxStream, true);
                            //EbxReaderInstance.Position = 0;
                            var readerInst = (EbxReader)LoadTypeByName(ProfileManager.EBXReader, ebxStream, true);
                            try
                            {
                                if (readerInst.GetType() == typeof(EbxReader) && !readerInst.IsValid)
                                    readerInst.InitialRead(ebxStream, false);
                                //EbxReaderInstance.InitialRead(ebxStream, false);
                                EBX[ebx.Name].Type = readerInst.RootType;
                                if (EBX[ebx.Name].Type != "NewWaveAsset")
                                {

                                }
                                EBX[ebx.Name].Id = readerInst.FileGuid;
                            }
                            catch (Exception)
                            {

                            }
                        }
                        else
                        {
                            throw new ArgumentNullException("EbxReader is not set against the Profile.");
                            //ebxReader = new EbxReaderV3(ebxStream, true);
                            //EBX[ebx.Name].Type = ebxReader.RootType;
                            //EBX[ebx.Name].Id = ebxReader.FileGuid;
                        }
                        return;
                    }
                }
            }

            if (string.IsNullOrEmpty(ebx.Type))
            {
                //EBX.TryRemove(ebx.Name, out _);
            }
        }



        public void DoEbxIndexing()
        {
            if (TypeLibrary.ExistingAssembly == null)
            {
                WriteToLog($"Unable to index data until SDK exists");
                return;
            }

            //if (AllSdkAssemblyTypes == null)
            //    AllSdkAssemblyTypes = TypeLibrary.ExistingAssembly.GetTypes().ToList();

            //ResourceManager.UseLastCasPath = true;

            var ebxListValues = EBX.Values.ToList();
            //if (ProfilesLibrary.IsMadden21DataVersion()
            //    || ProfilesLibrary.IsFIFA21DataVersion()
            //    || ProfilesLibrary.IsFIFA20DataVersion()
            //    || ProfilesLibrary.IsFIFA19DataVersion()
            //    )
            //{

            int ebxProgress = 0;

            var count = EbxItemsWithNoType.Count;
            if (count > 0)
            {
                WriteToLog($"Initial load - Indexing data - This will take some time");

                EbxItemsWithNoType.ForEach(x =>
                {
                    UpdateEbxListItem(x);
                    ebxProgress++;
                    WriteToLog($"Initial load - Indexing data ({Math.Round((ebxProgress / (double)count * 100.0), 1)}%)");
                });

                CacheWrite();
                WriteToLog("Initial load - Indexing complete");

            }

            //}

            AssetManager.UseLastCasPath = false;

        }

        public uint GetModifiedCount()
        {
            uint num = (uint)EBX.Values.Count((EbxAssetEntry entry) => entry.IsModified);
            uint num2 = (uint)RES.Values.Count((ResAssetEntry entry) => entry.IsModified);
            uint num3 = (uint)Chunks.Values.Count((ChunkAssetEntry entry) => entry.IsModified);
            uint num4 = 0u;
            foreach (ICustomAssetManager value in CustomAssetManagers.Values)
            {
                num4 = (uint)((int)num4 + value.EnumerateAssets(modifiedOnly: true).Count());
            }
            return num + num2 + num3 + num4;
        }

        public uint GetDirtyCount()
        {
            uint num = (uint)EBX.Values.Count((EbxAssetEntry entry) => entry.IsDirty);
            uint num2 = (uint)RES.Values.Count((ResAssetEntry entry) => entry.IsDirty);
            uint num3 = (uint)Chunks.Values.Count((ChunkAssetEntry entry) => entry.IsDirty);
            uint num4 = 0u;
            foreach (ICustomAssetManager value in CustomAssetManagers.Values)
            {
                num4 = (uint)((int)num4 + value.EnumerateAssets(modifiedOnly: true).Count((AssetEntry a) => a.IsDirty));
            }
            return num + num2 + num3 + num4;
        }

        public uint GetEbxCount(string ebxType)
        {
            return (uint)EBX.Values.Count((EbxAssetEntry entry) => entry.Type != null && entry.Type.Equals(ebxType));
        }

        public uint GetEbxCount()
        {
            return (uint)EBX.Count;
        }

        public uint GetResCount(uint resType)
        {
            return (uint)RES.Values.Count((ResAssetEntry entry) => entry.ResType == resType);
        }

        public uint GetEmbeddedCount(uint resType)
        {
            return (uint)EmbeddedFileEntries.Count();
        }

        public Task ResetAsync()
        {
            return Task.Run(Reset);
        }

        public void Reset()
        {
            List<EbxAssetEntry> list = EBX.Values.ToList();
            List<ResAssetEntry> list2 = RES.Values.ToList();
            List<ChunkAssetEntry> list3 = Chunks.Values.ToList();
            foreach (EbxAssetEntry item in list)
            {
                RevertAsset(item, dataOnly: false, suppressOnModify: false);
            }
            foreach (ResAssetEntry item2 in list2)
            {
                RevertAsset(item2, dataOnly: false, suppressOnModify: false);
            }
            foreach (ChunkAssetEntry item3 in list3)
            {
                RevertAsset(item3, dataOnly: false, suppressOnModify: false);
            }
            foreach (ICustomAssetManager value in CustomAssetManagers.Values)
            {
                foreach (AssetEntry item4 in value.EnumerateAssets(modifiedOnly: true))
                {
                    RevertAsset(item4, dataOnly: false, suppressOnModify: false);
                }
            }
            EmbeddedFileEntries.Clear();// = new List<EmbeddedFileEntry>();

            ChunkFileManager2022.CleanUpChunks(true);
            LocaleINIMod = new LocaleIniManager();

        }

        public void FullReset()
        {
            EBX.Clear();
            RES.Clear();
            resRidList.Clear();
            Chunks.Clear();

            if (!CustomAssetManagers.ContainsKey("legacy"))
                return;

            var lam = GetLegacyAssetManager() as ChunkFileManager2022;
            if (lam != null)
            {
                lam.LegacyEntries.Clear();
                lam.ChunkBatches.Clear();
                lam.ModifiedChunks.Clear();
            }
        }

        public void RevertAsset(IAssetEntry entry, bool dataOnly = false, bool suppressOnModify = true)
        {
            if (!entry.IsModified)
            {
                return;
            }

            if (entry is EbxAssetEntry || entry is LegacyFileEntry)
            {
                if (AssetManagerModified != null)
                    AssetManagerModified(entry);
            }

            if (entry is AssetEntry assetEntry)
            {

                foreach (AssetEntry linkedAsset in assetEntry.LinkedAssets)
                {
                    RevertAsset(linkedAsset, dataOnly, suppressOnModify);
                }

                if(assetEntry is EbxAssetEntry && !assetEntry.LinkedAssets.Any() && assetEntry.Type == "TextureAsset")
                {
                    var resEntry = this.GetResEntry(entry.Name);
                    if (resEntry != null)
                    {
                        RevertAsset(resEntry);
                        Texture texture = new Texture(resEntry);
                        if (texture != null)
                        {
                            var chunkEntry = this.GetChunkEntry(texture.ChunkId);
                            if (chunkEntry != null)
                                RevertAsset(chunkEntry);
                        }
                    }
                }

                assetEntry.ClearModifications();
                if (dataOnly)
                {
                    return;
                }
                assetEntry.LinkedAssets.Clear();
                assetEntry.AddBundles.Clear();
                assetEntry.RemBundles.Clear();

                var chunkFileManager = GetLegacyAssetManager() as IChunkFileManager;
                if (chunkFileManager != null)
                {
                    chunkFileManager.RevertAsset(assetEntry);
                }

                assetEntry.IsDirty = false;
                //if (!assetEntry.IsAdded && !suppressOnModify)
                //{
                //    assetEntry.OnModified();
                //}
            }
        }

        public ICustomAssetManager GetLegacyAssetManager()
        {
            if (!CustomAssetManagers.ContainsKey("legacy"))
                return null;

            return CustomAssetManagers["legacy"];
        }

        public void AddEmbeddedFile(EmbeddedFileEntry entry)
        {
            if (EmbeddedFileEntries.Contains(entry))
                EmbeddedFileEntries.Remove(entry);

            EmbeddedFileEntries.Add(entry);
        }


        /// <summary>
        /// Attempts to Add an EBX to the EBX List. You must add the patch versions first before the base data!
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool AddEbx(EbxAssetEntry entry)
        {
            bool result = EBX.TryAdd(entry.Name.ToLower(), entry);
            if (!result)
            {
                // If it already exists, then add bundles to the entry
                var existingEbxEntry = (EbxAssetEntry)AssetManager.Instance.EBX[entry.Name].Clone();

                foreach (var bundle in entry.Bundles.Where(x => !existingEbxEntry.Bundles.Contains(x)))
                    existingEbxEntry.Bundles.Add(bundle);

                foreach (var bundle in existingEbxEntry.Bundles.Where(x => !entry.Bundles.Contains(x)))
                    entry.Bundles.Add(bundle);

                // Always overwrite if the new item is a patch version
                if (entry.ExtraData != null && !existingEbxEntry.ExtraData.IsPatch && entry.ExtraData.IsPatch)
                    EBX[entry.Name] = entry;

                //if (ProfileManager.IsGameVersion(ProfileManager.EGame.FIFA22) 
                //	|| ProfileManager.IsGameVersion(ProfileManager.EGame.MADDEN23))
                //{
                //	// Add it anyway and link to the other one?
                //	entry.Name = $"{entry.Name}-FMTOther-{entry.ExtraData.IsPatch.ToString()}-{entry.ExtraData.Cas}-{entry.ExtraData.Catalog}";

                //	if (EBX.TryAdd(entry.Name, entry))
                //		existingEbxEntry.LinkAsset(entry);
                //}
            }
            return result;
        }

        /// <summary>
        /// Attempts to Add an RES to the RES List. You must add the patch versions first before the base data!
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public void AddRes(ResAssetEntry entry)
        {
            bool result = RES.TryAdd(entry.Name.ToLower(), entry);
            if (!result)
            {
                // If it already exists, then add bundles to the entry
                var existingEntry = (ResAssetEntry)AssetManager.Instance.RES[entry.Name].Clone();

                foreach (var bundle in existingEntry.Bundles)
                    entry.Bundles.Add(bundle);

                // Always overwrite if the new item is a patch version
                if (existingEntry.ExtraData != null && !existingEntry.ExtraData.IsPatch && entry.ExtraData.IsPatch)
                    RES[entry.Name] = entry;
            }

            //if (resRidList.ContainsKey(entry.ResRid))
            //    resRidList.Remove(entry.ResRid);

            resRidList.TryAdd(entry.ResRid, entry);
        }


        /// <summary>
        /// Attempts to Add a Chunk to the Chunk List. You must add the patch versions first before the base data!
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public void AddChunk(ChunkAssetEntry entry)
        {
            if (!Chunks.TryAdd(entry.Id, entry))
            {
                // If it already exists, then add bundles to the entry
                var existingChunk = (ChunkAssetEntry)Chunks[entry.Id].Clone();

                foreach (var bundle in entry.Bundles.Where(x => !existingChunk.Bundles.Contains(x)))
                    existingChunk.Bundles.Add(bundle);

                foreach (var bundle in existingChunk.Bundles.Where(x => !entry.Bundles.Contains(x)))
                    entry.Bundles.Add(bundle);

                // Always overwrite if the new item is a patch version
                if (existingChunk.ExtraData != null && !existingChunk.ExtraData.IsPatch && entry.ExtraData.IsPatch)
                    Chunks[entry.Id] = entry;
            }

            if (entry.IsTocChunk)
            {
                var hashedId = Fnv1a.HashString(entry.Id.ToString());

                if (!SuperBundleChunks.TryAdd(hashedId, entry))
                {
                    // If it already exists, then add bundles to the entry
                    var existingChunk = (ChunkAssetEntry)SuperBundleChunks[hashedId].Clone();

                    foreach (var bundle in entry.Bundles.Where(x => !existingChunk.Bundles.Contains(x)))
                        existingChunk.Bundles.Add(bundle);

                    foreach (var bundle in existingChunk.Bundles)
                        entry.Bundles.Add(bundle);

                    // Always overwrite if the new item is a patch version
                    if (existingChunk.ExtraData != null && !existingChunk.ExtraData.IsPatch && entry.ExtraData.IsPatch)
                        SuperBundleChunks[hashedId] = entry;
                }
            }

        }

        public BundleEntry AddBundle(string name, BundleType type, int sbIndex)
        {
            int num = Bundles.FindIndex((BundleEntry be) => be.Name == name);
            if (num != -1)
            {
                return Bundles[num];
            }
            BundleEntry bundleEntry = new BundleEntry();
            bundleEntry.Name = name;
            bundleEntry.SuperBundleId = sbIndex;
            bundleEntry.Type = type;
            bundleEntry.Added = true;
            Bundles.Add(bundleEntry);
            return bundleEntry;
        }

        public SuperBundleEntry AddSuperBundle(string name)
        {
            int num = superBundles.FindIndex((SuperBundleEntry sbe) => sbe.Name.Equals(name));
            if (num != -1)
            {
                return superBundles[num];
            }
            SuperBundleEntry superBundleEntry = new SuperBundleEntry();
            superBundleEntry.Name = name;
            superBundleEntry.Added = true;
            superBundles.Add(superBundleEntry);
            return superBundleEntry;
        }



        public Guid AddChunk(byte[] buffer, Guid? overrideGuid = null, Texture texture = null, params int[] bundles)
        {
            ChunkAssetEntry chunkAssetEntry = new ChunkAssetEntry();
            CompressionType compressionOverride = (ProfileManager.DataVersion == 20170929) ? CompressionType.Oodle : CompressionType.Default;
            chunkAssetEntry.ModifiedEntry = new ModifiedAssetEntry();
            chunkAssetEntry.ModifiedEntry.Data = ((texture != null) ? Utils.CompressTexture(buffer, texture, compressionOverride) : Utils.CompressFile(buffer, null, ResourceType.Invalid, compressionOverride));
            chunkAssetEntry.ModifiedEntry.Sha1 = GenerateSha1(chunkAssetEntry.ModifiedEntry.Data);
            chunkAssetEntry.ModifiedEntry.LogicalSize = (uint)buffer.Length;
            chunkAssetEntry.ModifiedEntry.FirstMip = -1;
            chunkAssetEntry.AddBundles.AddRange(bundles);
            if (texture != null)
            {
                chunkAssetEntry.ModifiedEntry.LogicalOffset = texture.LogicalOffset;
                chunkAssetEntry.ModifiedEntry.LogicalSize = texture.LogicalSize;
                chunkAssetEntry.ModifiedEntry.RangeStart = texture.RangeStart;
                chunkAssetEntry.ModifiedEntry.RangeEnd = texture.RangeEnd;
                chunkAssetEntry.ModifiedEntry.FirstMip = texture.FirstMip;
            }
            chunkAssetEntry.IsAdded = true;
            chunkAssetEntry.IsDirty = true;
            if (overrideGuid.HasValue)
            {
                chunkAssetEntry.Id = overrideGuid.Value;
            }
            else
            {
                byte[] array = Guid.NewGuid().ToByteArray();
                array[15] |= 1;
                chunkAssetEntry.Id = new Guid(array);
            }
            //chunkList.Add(chunkAssetEntry.Id, chunkAssetEntry);
            Chunks.TryAdd(chunkAssetEntry.Id, chunkAssetEntry);
            return chunkAssetEntry.Id;
        }

        public bool ModifyChunk(Guid chunkId
            , byte[] buffer
            , Texture texture = null
            , CompressionType compressionOverride = CompressionType.Default
            , bool addToChunkBundle = false)
        {
            if (!Chunks.ContainsKey(chunkId) && !SuperBundleChunks.ContainsKey(Fnv1a.HashString(chunkId.ToString())))
            {
                return false;
            }

            if (Chunks.ContainsKey(chunkId))
            {
                ChunkAssetEntry chunkAssetEntry = Chunks[chunkId];
                return ModifyChunk(chunkAssetEntry, buffer, texture, compressionOverride, addToChunkBundle);
            }

            throw new NotImplementedException("SuperBundleChunks has not been implemented!");
        }

        public bool ModifyChunk(
            ChunkAssetEntry chunkAssetEntry
            , byte[] buffer
            , Texture texture = null
            , CompressionType compressionOverride = CompressionType.Default
            , bool addToChunkBundle = false)
        {
            if (compressionOverride == CompressionType.Default)
                compressionOverride = ProfileManager.GetCompressionType(ProfileManager.CompTypeArea.Chunks);

            if (chunkAssetEntry.ModifiedEntry == null)
            {
                chunkAssetEntry.ModifiedEntry = new ModifiedAssetEntry();
            }
            chunkAssetEntry.ModifiedEntry.OriginalSize = buffer.Length;
            chunkAssetEntry.ModifiedEntry.Data = ((texture != null) ? Utils.CompressTexture(buffer, texture, compressionOverride) : Utils.CompressFile(buffer, null, ResourceType.Invalid, compressionOverride));
            chunkAssetEntry.ModifiedEntry.Size = chunkAssetEntry.ModifiedEntry.Data.Length;
            chunkAssetEntry.ModifiedEntry.Sha1 = GenerateSha1(chunkAssetEntry.ModifiedEntry.Data);
            chunkAssetEntry.ModifiedEntry.LogicalSize = (uint)buffer.Length;
            if (texture != null)
            {
                chunkAssetEntry.ModifiedEntry.LogicalOffset = texture.LogicalOffset;
                chunkAssetEntry.ModifiedEntry.LogicalSize = texture.LogicalSize;
                chunkAssetEntry.ModifiedEntry.RangeStart = texture.RangeStart;
                chunkAssetEntry.ModifiedEntry.RangeEnd = (uint)chunkAssetEntry.ModifiedEntry.Data.Length;
                chunkAssetEntry.ModifiedEntry.FirstMip = texture.FirstMip;
            }
            chunkAssetEntry.IsDirty = true;
            //chunkAssetEntry.ModifiedEntry.AddToChunkBundle = addToChunkBundle;
            return true;
        }

        public void ModifyRes(ulong resRid, byte[] buffer, byte[] meta = null)
        {
            if (resRidList.ContainsKey(resRid))
            {
                ResAssetEntry resAssetEntry = resRidList[resRid];
                //CompressionType compressionOverride = (ProfilesLibrary.DataVersion == 20170929) ? CompressionType.Oodle : CompressionType.Default;
                CompressionType compressionOverride = ProfileManager.GetCompressionType(ProfileManager.CompTypeArea.RES);
                //if (ProfilesLibrary.IsMadden21DataVersion()) compressionOverride = CompressionType.Oodle;


                if (resAssetEntry.ModifiedEntry == null)
                {
                    resAssetEntry.ModifiedEntry = new ModifiedAssetEntry();
                }
                resAssetEntry.ModifiedEntry.Data = Utils.CompressFile(buffer, null, (ResourceType)resAssetEntry.ResType, compressionOverride);
                resAssetEntry.ModifiedEntry.OriginalSize = buffer.Length;
                resAssetEntry.ModifiedEntry.Sha1 = GenerateSha1(resAssetEntry.ModifiedEntry.Data);
                if (meta != null)
                {
                    resAssetEntry.ModifiedEntry.ResMeta = meta;
                }
                resAssetEntry.IsDirty = true;
            }
        }

        public void ModifyRes(string resName, byte[] buffer, byte[] meta = null, CompressionType compressionOverride = CompressionType.Default)
        {
            if (RES.ContainsKey(resName))
            {
                ResAssetEntry resAssetEntry = RES[resName];
                if (compressionOverride == CompressionType.Default)
                {
                    compressionOverride = ProfileManager.GetCompressionType(ProfileManager.CompTypeArea.RES);
                }

                resAssetEntry.ModifiedEntry = new ModifiedAssetEntry();
                resAssetEntry.ModifiedEntry.Data = Utils.CompressFile(buffer, null, (ResourceType)resAssetEntry.ResType, compressionOverride);
                resAssetEntry.ModifiedEntry.OriginalSize = buffer.Length;
                resAssetEntry.ModifiedEntry.Sha1 = GenerateSha1(resAssetEntry.ModifiedEntry.Data);
                if (meta != null)
                {
                    resAssetEntry.ModifiedEntry.ResMeta = meta;
                }
                resAssetEntry.IsDirty = true;
            }
        }

        public void ModifyResCompressed(string resName, byte[] resData, byte[] resMeta = null)
        {
            if (RES.ContainsKey(resName))
            {
                ResAssetEntry resAssetEntry = RES[resName];

                resAssetEntry.ModifiedEntry = new ModifiedAssetEntry();
                resAssetEntry.ModifiedEntry.Data = resData;
                using (CasReader casReader = new CasReader(new MemoryStream(resData)))
                    resAssetEntry.ModifiedEntry.OriginalSize = casReader.Read().Length;

                resAssetEntry.ModifiedEntry.Sha1 = GenerateSha1(resAssetEntry.ModifiedEntry.Data);

                if (resMeta != null)
                {
                    resAssetEntry.ModifiedEntry.ResMeta = resMeta;
                }
                resAssetEntry.IsDirty = true;
            }
        }

        //public void ModifyRes(string resName, Resource resource, byte[] meta = null)
        //{
        //	if (RES.ContainsKey(resName))
        //	{
        //		ResAssetEntry resAssetEntry = RES[resName];
        //		if (resAssetEntry.ModifiedEntry == null)
        //		{
        //			resAssetEntry.ModifiedEntry = new ModifiedAssetEntry();
        //		}
        //		resAssetEntry.ModifiedEntry.DataObject = resource.Save();
        //		if (meta != null)
        //		{
        //			resAssetEntry.ModifiedEntry.ResMeta = meta;
        //		}
        //		resAssetEntry.IsDirty = true;
        //	}
        //}

        public void ModifyEbx(string name, EbxAsset asset)
        {
            name = name.ToLower();
            if (EBX.ContainsKey(name))
            {
                EbxAssetEntry ebxAssetEntry = EBX[name];
                if (ebxAssetEntry.ModifiedEntry == null)
                {
                    ebxAssetEntry.ModifiedEntry = new ModifiedAssetEntry();
                }
                ebxAssetEntry.ModifiedEntry.Data = null;
                ((ModifiedAssetEntry)ebxAssetEntry.ModifiedEntry).DataObject = asset;
                ebxAssetEntry.ModifiedEntry.OriginalSize = 0L;
                ebxAssetEntry.ModifiedEntry.Sha1 = Sha1.Zero;
                ebxAssetEntry.ModifiedEntry.IsTransientModified = asset.TransientEdit;
                ebxAssetEntry.ModifiedEntry.DependentAssets.Clear();
                ebxAssetEntry.ModifiedEntry.DependentAssets.AddRange(asset.Dependencies);
                ebxAssetEntry.IsDirty = true;
                ebxAssetEntry.IsBinary = false;

                if (AssetManagerModified != null)
                    AssetManagerModified(ebxAssetEntry);

            }
        }

        public async Task ModifyEbxAsync(string name, EbxAsset asset)
        {
            await Task.Run(() => 
            {

                try
                {
                    ModifyEbx(name, asset);
                }
                catch (Exception)
                {
                    //AssetManager.Instance.LogError($"Unable to modify EBX {AssetEntry.Name}");
                    if(Logger != null)
                        Logger.LogError($"Unable to modify EBX {name}");
                    FileLogger.WriteLine($"Unable to modify EBX {name}");
                }
               
            });
        }

        public void ModifyEbxBinary(string name, byte[] data)
        {
            name = name.ToLower();
            if (EBX.ContainsKey(name))
            {
                var ebxEntry = EBX[name];
                var patch = ebxEntry.IsInPatch || ebxEntry.ExtraData.IsPatch;
                var ebxAsset = GetEbxAssetFromStream(new MemoryStream(data), patch);
                ModifyEbx(name, ebxAsset);
            }
        }

        public void ModifyEbxJson(string name, string json)
        {
            name = name.ToLower();
            if (EBX.ContainsKey(name))
            {
                var ebxEntry = EBX[name];
                var patch = ebxEntry.IsInPatch || ebxEntry.ExtraData.IsPatch;
                var ebxAsset = GetEbx(ebxEntry);
                if (ebxAsset != null)
                {

                    var rootObject = ebxAsset.RootObject;
                    ExpandoObject newObject = JsonConvert.DeserializeObject<ExpandoObject>(json);
                    RecursiveExpandoObjectToObject(rootObject, newObject);

                    ModifyEbx(name, ebxAsset);


                }
            }
        }

        public void RecursiveExpandoObjectToObject(object rootObject, ExpandoObject newObject)
        {
            if (rootObject != null && newObject != null)
            {
                PropertyInfo[] properties = rootObject.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                foreach (KeyValuePair<string, object> kvp in newObject)
                {
                    if (Utilities.PropertyExists(rootObject, kvp.Key))
                    {
                        try
                        {
                            var newValue = kvp.Value;
                            PropertyInfo propertyInfo = properties.FirstOrDefault(x => x.Name == kvp.Key);
                            if (kvp.Value is ExpandoObject)
                            {
                                var innerObj = propertyInfo?.GetValue(rootObject);
                                RecursiveExpandoObjectToObject(innerObj, (ExpandoObject)newValue);
                            }
                            else
                            {
                                if (newValue is List<object>)
                                {
                                    var lst = (List<object>)newValue;
                                    if (lst.Count > 0)
                                    {
                                        if (Single.TryParse(lst[0].ToString(), out float f))
                                        {
                                            var newList = new List<Single>();
                                            foreach (var iO in lst)
                                            {
                                                newList.Add(Single.Parse(iO.ToString()));
                                            }
                                            newValue = newList;
                                        }
                                        else if (lst[0] is ExpandoObject)
                                        {
                                            Type propertyType = propertyInfo?.PropertyType;
                                            var typeArg = propertyType.GenericTypeArguments[0];
                                            var innerObjType = Activator.CreateInstance(typeArg);
                                            Type listType = typeof(List<>).MakeGenericType(new[] { typeArg });
                                            IList newList = (IList)Activator.CreateInstance(listType);
                                            foreach (ExpandoObject iO in lst)
                                            {
                                                RecursiveExpandoObjectToObject(innerObjType, iO);
                                                newList.Add(innerObjType);
                                            }
                                            newValue = newList;

                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    if (newValue is Double)
                                    {
                                        newValue = Convert.ToSingle(newValue);
                                    }
                                }
                                propertyInfo?.SetValue(rootObject, newValue);
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }



        public void ModifyLegacyAsset(string name, byte[] data, bool rebuildChunk = true)
        {
            if (CustomAssetManagers.ContainsKey("legacy"))
            {
                CustomAssetManagers["legacy"].ModifyAsset(name, data, rebuildChunk);

                var assetEntry = CustomAssetManagers["legacy"].GetAssetEntry(name);
                if (AssetManagerModified != null)
                {
                    AssetManagerModified(assetEntry);
                }
            }


        }

        public void ModifyLegacyAssets(Dictionary<string, byte[]> data, bool rebuildChunk = true)
        {
            var lm = CustomAssetManagers["legacy"] as ChunkFileManager2022;
            if (lm != null)
            {
                lm.ModifyAssets(data, true);
            }
        }

        public void ModifyCustomAsset(string type, string name, byte[] data)
        {
            if (CustomAssetManagers.ContainsKey(type))
            {
                CustomAssetManagers[type].ModifyAsset(name, data);
            }
        }

        public void DuplicateAsset(AssetEntry entryToDuplicate, string newEntryPath)
        {
            DuplicateEntry(entryToDuplicate, newEntryPath);
        }

        public void DuplicateEntry(AssetEntry entryToDuplicate, string newEntryPath)
        {
            if (entryToDuplicate == null)
                throw new ArgumentNullException("Entry to duplicate must be provided!");

            if (newEntryPath == null)
                throw new ArgumentNullException("A new Path must be provided!");

            if (entryToDuplicate is LegacyFileEntry)
            {
                LegacyFileEntry ae = JsonConvert.DeserializeObject<LegacyFileEntry>(JsonConvert.SerializeObject(entryToDuplicate));
                ae.Name = newEntryPath;
                ICustomAssetManager customAssetManager = AssetManager.Instance.GetLegacyAssetManager();
                customAssetManager.DuplicateAsset(newEntryPath, (LegacyFileEntry)entryToDuplicate);
            }
            else
            {

                EbxAssetEntry ae = entryToDuplicate.Clone() as EbxAssetEntry;
                var originalEbxData = AssetManager.Instance.GetEbx(ae);

                ae.Name = newEntryPath;
                ae.DuplicatedFromName = entryToDuplicate.Name;
                ae.Sha1 = FMT.FileTools.Sha1.Create();
                AssetManager.Instance.AddEbx(ae);

                // Check for "Resource" property
                if (Utilities.PropertyExists(originalEbxData.RootObject, "Resource"))
                {
                    var dynamicRO = (dynamic)originalEbxData.RootObject;
                    ResAssetEntry resAssetEntry = AssetManager.Instance.GetResEntry(((dynamic)originalEbxData.RootObject).Resource);
                    var rae = resAssetEntry.Clone() as ResAssetEntry;
                    rae.Name = newEntryPath;
                    rae.ResRid = GetNextRID();
                    rae.Sha1 = FMT.FileTools.Sha1.Create();
                    rae.DuplicatedFromName = entryToDuplicate.Name;

                    dynamicRO.Resource = new ResourceRef(rae.ResRid);

                    if (ae.Type == "TextureAsset")
                    {
                        using (Texture textureAsset = new Texture(rae))
                        {
                            var cae = textureAsset.ChunkEntry.Clone() as ChunkAssetEntry;
                            cae.Id = AssetManager.Instance.GenerateChunkId(cae);
                            textureAsset.ChunkId = cae.Id;
                            var newTextureData = textureAsset.ToBytes();
                            rae.ModifiedEntry = new ModifiedAssetEntry() { UserData = "DUP;" + entryToDuplicate.Name, Data = Utils.CompressFile(newTextureData, textureAsset) };
                            cae.ModifiedEntry = new ModifiedAssetEntry() { UserData = "DUP;" + textureAsset.ChunkEntry.Name, Data = Utils.CompressFile(AssetManager.Instance.GetChunkData(cae).ToArray()) };
                            cae.Sha1 = Sha1.Create();
                            cae.DuplicatedFromName = textureAsset.ChunkEntry.Name;
                            AssetManager.Instance.AddChunk(cae);
                        }
                    }

                    // Modify the newly Added EBX
                    AssetManager.Instance.ModifyEbx(newEntryPath, originalEbxData);
                    ae.ModifiedEntry.UserData = "DUP;" + entryToDuplicate.Name;
                    // Add the RESOURCE
                    AssetManager.Instance.AddRes(rae);
                }


            }
        }

        public ulong GetNextRID()
        {
            return AssetManager.Instance.resRidList.Keys.Max() + 1;
        }

        public IEnumerable<SuperBundleEntry> EnumerateSuperBundles(bool modifiedOnly = false)
        {
            foreach (SuperBundleEntry superBundle in superBundles)
            {
                if (!modifiedOnly || superBundle.Added)
                {
                    yield return superBundle;
                }
            }
        }

        public IEnumerable<BundleEntry> EnumerateBundles(BundleType type = BundleType.None, bool modifiedOnly = false)
        {
            foreach (BundleEntry bundle in Bundles)
            {
                if ((type == BundleType.None || bundle.Type == type) && (!modifiedOnly || bundle.Added))
                {
                    yield return bundle;
                }
            }
        }

        public IEnumerable<EbxAssetEntry> EnumerateEbx(string type = "", bool modifiedOnly = false, bool includeLinked = false, bool includeHidden = true, string bundleSubPath = "")
        {
            List<int> list = new List<int>();
            if (bundleSubPath != "")
            {
                bundleSubPath = bundleSubPath.ToLower();
                for (int i = 0; i < Bundles.Count; i++)
                {
                    if (Bundles[i].Name.Equals(bundleSubPath) || Bundles[i].Name.StartsWith(bundleSubPath + "/"))
                    {
                        list.Add(i);
                    }
                }
            }

            return EnumerateEbx(type, modifiedOnly, includeLinked, includeHidden, list.ToArray());
        }

        protected IEnumerable<EbxAssetEntry> EnumerateEbx(string type, bool modifiedOnly, bool includeLinked, bool includeHidden, params int[] bundles)
        {
            //foreach (EbxAssetEntry value in EBX.Values)
            //{
            //	if (
            //		(!modifiedOnly 
            //		|| (
            //			value.IsModified && (!value.IsIndirectlyModified || includeLinked || value.IsDirectlyModified)
            //			)
            //		) 
            //		&& (!(type != "") || (value.Type != null && TypeLibrary.IsSubClassOf(value.Type, type))))
            //	{
            //		yield return value;
            //	}
            //}

#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            Span<EbxAssetEntry> ebxAssetEntries = CollectionsMarshal.AsSpan(EBX.Values.ToList());
            EbxAssetEntry[] assetEntriesArray = new EbxAssetEntry[ebxAssetEntries.Length];
            for (var i = 0; i < ebxAssetEntries.Length; i++)
            {
                var value = ebxAssetEntries[i];
                if (
                    (!modifiedOnly
                    || (
                        value.IsModified && (!value.IsIndirectlyModified || includeLinked || value.IsDirectlyModified)
                        )
                    )
                    && (!(type != "") || (value.Type != null && TypeLibrary.IsSubClassOf(value.Type, type))))
                {
                    assetEntriesArray[i] = value;
                }
            }
#if DEBUG
            sw.Stop();
            Debug.WriteLine($"EnumerateEbx:Span:{sw.Elapsed}");
#endif
            return assetEntriesArray.Where(x => x != null);


        }

        public IEnumerable<ResAssetEntry> EnumerateRes(BundleEntry bentry)
        {
            int num = Bundles.IndexOf(bentry);
            if (num != -1)
            {
                foreach (ResAssetEntry item in EnumerateRes(0u, false, num))
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<ResAssetEntry> EnumerateRes(uint resType = 0u, bool modifiedOnly = false, string bundleSubPath = "")
        {
            List<int> list = new List<int>();
            if (bundleSubPath != "")
            {
                bundleSubPath = bundleSubPath.ToLower();
                for (int i = 0; i < Bundles.Count; i++)
                {
                    if (Bundles[i].Name.Equals(bundleSubPath) || Bundles[i].Name.StartsWith(bundleSubPath + "/"))
                    {
                        list.Add(i);
                    }
                }
                if (list.Count == 0)
                {
                    yield break;
                }
            }
            foreach (ResAssetEntry item in EnumerateRes(resType, modifiedOnly, list.ToArray()))
            {
                yield return item;
            }
        }

        protected IEnumerable<ResAssetEntry> EnumerateRes(uint resType, bool modifiedOnly, params int[] bundles)
        {
            foreach (ResAssetEntry value in RES.Values)
            {
                if ((!modifiedOnly || value.IsDirectlyModified) && (resType == 0 || value.ResType == resType))
                {
                    if (bundles.Length != 0)
                    {
                        bool flag = false;
                        foreach (int item in bundles)
                        {
                            if (value.Bundles.Contains(item))
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            continue;
                        }
                    }
                    yield return value;
                }
            }
        }

        public IEnumerable<ChunkAssetEntry> EnumerateChunks(BundleEntry bentry)
        {
            int bindex = Bundles.IndexOf(bentry);
            if (bindex != -1)
            {
                foreach (ChunkAssetEntry value in Chunks.Values.OrderBy(x => x.ExtraData != null ? x.ExtraData.CasPath : string.Empty))
                {
                    if (value.Bundles.Contains(bindex))
                    {
                        yield return value;
                    }
                }
            }
        }

        public IEnumerable<ChunkAssetEntry> EnumerateChunks(bool modifiedOnly = false)
        {
            foreach (ChunkAssetEntry value in Chunks.Values.OrderBy(x => x.ExtraData != null ? x.ExtraData.CasPath : string.Empty))
            {
                if (!modifiedOnly || value.IsDirectlyModified)
                {
                    yield return value;
                }
            }
        }

        public IEnumerable<AssetEntry> EnumerateCustomAssets(string type, bool modifiedOnly = false)
        {
            if (type == "ebx")
                return EnumerateEbx(type, modifiedOnly);



            if (CustomAssetManagers.ContainsKey(type))
            {
                return CustomAssetManagers[type].EnumerateAssets(modifiedOnly);
                //{
                //    yield return item;
                //}
                //            foreach (AssetEntry item in CustomAssetManagers[type].EnumerateAssets(modifiedOnly))
                //{
                //	yield return item;
                //}
            }
            return null;
        }

        public IEnumerable<AssetEntry> ModifiedAssetEntries
        {
            get
            {
                List<AssetEntry> entries = new List<AssetEntry>();
                entries.AddRange(EnumerateEbx(modifiedOnly: true));
                entries.AddRange(EnumerateRes(modifiedOnly: true));
                entries.AddRange(EnumerateChunks(modifiedOnly: true));
                entries.AddRange(EnumerateCustomAssets("legacy", modifiedOnly: true));
                return entries;
            }
        }

        public int GetSuperBundleId(SuperBundleEntry sbentry)
        {
            return superBundles.FindIndex((SuperBundleEntry sbe) => sbe.Name.Equals(sbentry.Name));
        }

        public int GetSuperBundleId(string sbname)
        {
            return superBundles.FindIndex((SuperBundleEntry sbe) => sbe.Name.Equals(sbname, StringComparison.OrdinalIgnoreCase));
        }

        public SuperBundleEntry GetSuperBundle(int id)
        {
            if (id >= superBundles.Count)
            {
                return null;
            }
            return superBundles[id];
        }

        public int GetBundleId(BundleEntry bentry)
        {
            return Bundles.FindIndex((BundleEntry be) => be.Name.Equals(bentry.Name));
        }

        public int GetBundleId(string name)
        {
            return Bundles.FindIndex((BundleEntry be) => be.Name.Equals(name));
        }

        public BundleEntry GetBundleEntry(int bundleId)
        {
            if (Bundles.Count == 0)
                return null;

            var bundleIndex = (Bundles.FindIndex(x => bundleId == Fnv1a.HashString(x.Name)));
            if (bundleIndex != -1)
                bundleId = bundleIndex;

            if (bundleId >= Bundles.Count)
                return null;

            if (bundleId < 0)
                return null;

            return Bundles[bundleId];
        }

        public AssetEntry GetCustomAssetEntry(string type, string key)
        {
            if (!CustomAssetManagers.ContainsKey(type))
            {
                return null;
            }
            return CustomAssetManagers[type].GetAssetEntry(key);
        }

        public async Task<AssetEntry> GetCustomAssetEntryAsync(string type, string key)
        {
            return await Task.Run(() =>
            {

                return GetCustomAssetEntry(type, key);

            });
        }

        public T GetCustomAssetEntry<T>(string type, string key) where T : AssetEntry
        {
            return (T)GetCustomAssetEntry(type, key);
        }

        public EbxAssetEntry GetEbxEntry(ReadOnlySpan<char> name)
        {
            if (name.IsEmpty || name.Length == 0)
                return null;
            // Old school, search by string
            if (EBX.TryGetValue(name.ToString().ToLower(), out var ent))// ContainsKey(name.ToString()))
                return ent;// EBX[name.ToString()];

            // Search by string but with the typed name (for Assets searching)
            if (EBX.ContainsKey($"[{typeof(EbxAssetEntry).Name}]({name.ToString().ToLower()})"))
                return EBX[$"[{typeof(EbxAssetEntry).Name}]({name.ToString().ToLower()})"];

            // Search by Fnv1
            if (EBX.ContainsKey($"{Fnv1.HashString(name.ToString().ToLower())}"))
                return EBX[$"{Fnv1.HashString(name.ToString().ToLower())}"];

            if (CacheManager.HasEbx(name.ToString()))
            {
                EBX.TryAdd(name.ToString(), CacheManager.GetEbx(name.ToString()));
            }

            return null;
        }

        public EbxAssetEntry GetEbxEntry(Guid id)
        {
            var ebxGuids = EBX.Values.Where(x => x.Guid != null);
            return ebxGuids.FirstOrDefault(x => x.Guid == id);
        }

        public ResAssetEntry GetResEntry(ulong resRid)
        {
            return RES.Values.FirstOrDefault(x => x.ResRid == resRid);
        }

        public ResAssetEntry GetResEntry(string name)
        {
            var loweredString = name.ToString().ToLower();

            // Old school, search by string
            if (RES.ContainsKey(loweredString))
                return RES[loweredString];

            // Search by string but with the typed name (for Assets searching)
            if (RES.ContainsKey($"[{typeof(ResAssetEntry).Name}]({loweredString})"))
                return RES[$"[{typeof(ResAssetEntry).Name}]({loweredString})"];

            // Search by Fnv1
            if (RES.ContainsKey($"{Fnv1.HashString(loweredString)}"))
                return RES[$"{Fnv1.HashString(loweredString)}"];

            return null;
        }

        public ChunkAssetEntry GetChunkEntry(Guid id)
        {
            if (Chunks.TryGetValue(id, out var entry))
                return entry;

            if (SuperBundleChunks.TryGetValue(Fnv1a.HashString(id.ToString()), out var sbChunkEntry))
                return sbChunkEntry;


            return null;
        }

        public async ValueTask<ChunkAssetEntry> GetChunkEntryAsync(Guid id, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.FromResult(GetChunkEntry(id));
        }

        public Stream GetCustomAsset(string type, AssetEntry entry)
        {
            if (!CustomAssetManagers.ContainsKey(type))
            {
                return null;
            }
            return CustomAssetManagers[type].GetAsset(entry);
        }

        public async Task<MemoryStream> GetCustomAssetAsync(string type, AssetEntry entry)
        {
            return await Task.FromResult(GetCustomAsset(type, entry) as MemoryStream);
        }

        public EbxAsset GetEbx(EbxAssetEntry entry, bool getModified = true)
        {
            Stream assetStream = null;
            if (getModified)
            {
                if (entry != null && entry.ModifiedEntry != null && ((ModifiedAssetEntry)entry.ModifiedEntry).DataObject != null)
                {
                    if (entry.IsBinary || entry.ModifiedEntry.Data != null)
                    {
                        assetStream = new MemoryStream(entry.ModifiedEntry.Data);
                    }
                    else
                    {
                        var r = ((ModifiedAssetEntry)entry.ModifiedEntry).DataObject as EbxAsset;
                        r.ParentEntry = entry;

                        return r;
                    }
                }
            }

            if (assetStream == null)
            {
                assetStream = GetAsset(entry, getModified);
                if (assetStream == null)
                {
                    return null;
                }
            }
            bool inPatched = false;
            if (
                entry.ExtraData != null 
                && entry.ExtraData.CasPath.StartsWith("native_patch"))
            {
                inPatched = true;
            }

            return GetEbxAssetFromStream(assetStream, inPatched);
        }

        public async ValueTask<EbxAsset> GetEbxAsync(EbxAssetEntry entry, bool getModified = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await Task.Run(() =>
            {
                EbxAsset ebx = null;
                Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ebx = GetEbx(entry, getModified);
                }).Wait(TimeSpan.FromSeconds(EbxBaseWriter.GetEbxLoadWaitSeconds), cancellationToken);
                return ebx;
            }, cancellationToken);
        }


        public EbxAsset GetEbxAssetFromStream(Stream asset, bool inPatched = true)
        {
            EbxReader ebxReader = null;

            if (!string.IsNullOrEmpty(ProfileManager.EBXReader))
            {
                //ebxReader = (EbxReader)LoadTypeByName(ProfileManager.EBXReader, asset, inPatched);
                ebxReader = EbxReader.GetEbxReader(asset, inPatched);
            }
            else
            {

                if (ProfileManager.IsFIFA21DataVersion())
                {
                    ebxReader = new EbxReaderV3(asset, inPatched);

                }
                else if (ProfileManager.IsMadden21DataVersion(ProfileManager.Game))
                {
                    ebxReader = new EbxReaderV3(asset, inPatched);

                }
                else if (ProfileManager.DataVersion == 20181207
                    || ProfileManager.IsFIFA20DataVersion()
                    || ProfileManager.DataVersion == 20190905)
                {
                    ebxReader = new EbxReaderV2(asset, inPatched);
                }
                else
                {
                    ebxReader = new EbxReader(asset);
                }
            }


            return ebxReader.ReadAsset();
        }

        public Stream GetEbxStream(EbxAssetEntry entry, bool getModified = false)
        {
            return GetAsset(entry, getModified);
        }

        public Stream GetRes(ResAssetEntry entry)
        {
            return GetAsset(entry);
        }

        public async Task<Stream> GetResAsync(ResAssetEntry entry)
        {
            return await Task.Run(() =>
            {
                return GetRes(entry);
            });
        }

        //public T GetResAs<T>(ResAssetEntry entry) where T : Resource, new()
        //{
        //	using (NativeReader reader = new NativeReader(GetAsset(entry)))
        //	{
        //		ModifiedResource modifiedData = null;
        //		if (entry.ModifiedEntry != null && entry.ModifiedEntry.DataObject != null)
        //		{
        //			modifiedData = (entry.ModifiedEntry.DataObject as ModifiedResource);
        //		}
        //		T val = new T();
        //		val.Read(reader, this, entry, modifiedData);
        //		return val;
        //	}
        //}

        //public Stream GetChunk(Guid id)
        //{
        //	return GetAsset(GetChunkEntry(id));
        //}

        public Stream GetChunk(ChunkAssetEntry entry)
        {
            return GetAsset(entry);
        }

        public ReadOnlySpan<byte> GetChunkData(ChunkAssetEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (entry.ModifiedEntry != null)
                return entry.ModifiedEntry.Data;

            if(entry.ExtraData != null)
                return GetResourceData2(entry.ExtraData.CasPath, entry.ExtraData.DataOffset, entry.Size, entry);// GetChunkData(chunkEntry);
            else
                return ((MemoryStream)GetAsset(entry)).ToArray();
        }

        public FMT.FileTools.Sha1 GetBaseSha1(FMT.FileTools.Sha1 sha1)
        {
            return sha1;
        }

        private Stream GetAsset(AssetEntry entry, bool getModified = true)
        {
            if (entry == null)
            {
                throw new Exception("Failed to find Asset Entry");
            }

            if (entry.ModifiedEntry != null && (entry.ModifiedEntry.Data != null || entry.ModifiedEntry.DataObject != null) && getModified)
            {
                if(entry is EbxAssetEntry ebxAssetEntry)
                {
                    return new MemoryStream(EbxWriter.GetEbxArrayDecompressed(ebxAssetEntry));
                }
                else
                    return GetResourceData(entry.ModifiedEntry.Data);
            }

            var entryLocation = entry.Location;
            if (entryLocation == AssetDataLocation.CasNonIndexed && (entry.ExtraData == null || entry.ExtraData.DataOffset == 0))
            {
                entryLocation = AssetDataLocation.Cas;
            }

            Stream streamOfData = null;

            switch (entryLocation)
            {
                case AssetDataLocation.Cas:
                    if (entry.ExtraData == null || entry.ExtraData.DataOffset == 0)
                    {
                        streamOfData = GetResourceData(entry.Sha1);
                    }
                    break;
                case AssetDataLocation.CasNonIndexed:
                    streamOfData = GetResourceData(entry.ExtraData.CasPath, entry.ExtraData.DataOffset, entry.Size, entry);
                    break;
            }
            return streamOfData;
        }

        public Stream GetResourceData(byte[] buffer)
        {
            byte[] array = null;
            using (MemoryStream inBaseStream = new MemoryStream(buffer))
            {
                using (CasReader casReader = new CasReader(inBaseStream))
                {
                    array = casReader.Read();
                }
            }
            if (array == null)
            {
                return null;
            }
            return new MemoryStream(array);
        }

        public static string LastCasPath;
        public static MemoryStream LastCasPathInMemory;
        public static bool UseLastCasPath = false;

        public MemoryStream GetResourceData(string superBundleName, long offset, long size, AssetEntry entry = null)
        {
            //if (UseLastCasPath)
            //    return GetResourceDataUseLastCas(superBundleName, offset, size);

            superBundleName = superBundleName.Replace("/cs/", "/");

            try
            {
                var path = FileSystem.ResolvePath($"{superBundleName}");
                if (!string.IsNullOrEmpty(path))
                {

                    using (var f = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        using (var nr = new NativeReader(f))
                        {
                            byte[] array = null;
                            using (CasReader casReader = new CasReader(nr.CreateViewStream(offset, size)))
                            {
                                casReader.AssociatedAssetEntry = entry;
                                array = casReader.Read();
                            }
                            if (array == null)
                            {
                                return null;
                            }
                            return new MemoryStream(array);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("[DEBUG] [ERROR] " + e.Message);
            }
            return null;
        }

        public ReadOnlySpan<byte> GetResourceData2(string superBundleName, long offset, long size, AssetEntry entry = null)
        {
            superBundleName = superBundleName.Replace("/cs/", "/");

            try
            {
                var path = FileSystem.ResolvePath($"{superBundleName}");
                if (!string.IsNullOrEmpty(path))
                {

                    using (var f = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        using (var nr = new NativeReader(f))
                        {
                            byte[] array = null;
                            using (CasReader casReader = new CasReader(nr.CreateViewStream(offset, size)))
                            {
                                casReader.AssociatedAssetEntry = entry;
                                array = casReader.Read();
                            }
                            if (array == null)
                            {
                                return null;
                            }
                            return new ReadOnlySpan<byte>(array);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("[DEBUG] [ERROR] " + e.Message);
            }
            return null;
        }

        public MemoryStream GetResourceDataUseLastCas(string superBundleName, long offset, long size)
        {
            superBundleName = superBundleName.Replace("/cs/", "/");

            try
            {
                var path = FileSystem.ResolvePath($"{superBundleName}");
                if (LastCasPath != path && LastCasPathInMemory != null)
                {
                    LastCasPathInMemory.Close();
                    LastCasPathInMemory.Dispose();
                    LastCasPathInMemory = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                LastCasPath = path;
                if (!string.IsNullOrEmpty(LastCasPath))
                {
                    if (LastCasPathInMemory == null)
                    {
                        using (var f = new FileStream(LastCasPath, FileMode.Open, FileAccess.Read))
                        {
                            using (NativeReader reader = new NativeReader(f))
                            {
                                LastCasPathInMemory = new MemoryStream(reader.ReadToEnd());
                                LastCasPathInMemory.Position = 0;
                            }
                        }
                    }
                    //using (var f = new FileStream(LastCasPath, FileMode.Open, FileAccess.Read))
                    //{
                    NativeReader mR = new NativeReader(LastCasPathInMemory);
                    {
                        byte[] array = null;
                        using (CasReader casReader = new CasReader(mR.CreateViewStream(offset, size)))
                        {
                            array = casReader.Read();
                        }
                        if (array == null)
                        {
                            return null;
                        }
                        return new MemoryStream(array);
                    }
                    //}
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("[DEBUG] [ERROR] " + e.Message);
            }
            return null;
        }

        public Stream GetResourceData(Sha1 sha1)
        {
            if (FileSystem.CatPatchEntries.ContainsKey(sha1))
            {
                CatPatchEntry patchEntry = FileSystem.CatPatchEntries[sha1];
                return GetResourceData(patchEntry.BaseSha1, patchEntry.DeltaSha1);
            }

            if (!FileSystem.CatResourceEntries.ContainsKey(sha1))
            {
                return null;
            }

            CatResourceEntry entry = FileSystem.CatResourceEntries[sha1];
            byte[] buffer = null;

            if (entry.IsEncrypted && !KeyManager.Instance.HasKey(entry.KeyId))
            {
                return null;
            }

            using (NativeReader casReader = new NativeReader(new FileStream(FileSystem.CasFiles[entry.ArchiveIndex], FileMode.Open, FileAccess.Read)))
            {
                using (CasReader reader = new CasReader(casReader.CreateViewStream(entry.Offset, entry.Size),
                           (entry.IsEncrypted) ? KeyManager.Instance.GetKey(entry.KeyId) : null, entry.EncryptedSize))
                {
                    buffer = reader.Read();
                }
            }

            return (buffer != null) ? new MemoryStream(buffer) : null;
        }

        public Stream GetResourceData(Sha1 baseSha1, Sha1 deltaSha1)
        {
            if (!FileSystem.CatResourceEntries.ContainsKey(baseSha1) || !FileSystem.CatResourceEntries.ContainsKey(deltaSha1))
            {
                return null;
            }

            CatResourceEntry baseEntry = FileSystem.CatResourceEntries[baseSha1];
            CatResourceEntry deltaEntry = FileSystem.CatResourceEntries[deltaSha1];

            byte[] buffer = null;
            using (NativeReader baseReader = new NativeReader(new FileStream(FileSystem.CasFiles[baseEntry.ArchiveIndex], FileMode.Open, FileAccess.Read)))
            {
                using (NativeReader deltaReader = (deltaEntry.ArchiveIndex == baseEntry.ArchiveIndex) ? baseReader : new NativeReader(new FileStream(FileSystem.CasFiles[deltaEntry.ArchiveIndex], FileMode.Open, FileAccess.Read)))
                {
                    byte[] baseKey = (baseEntry.IsEncrypted && KeyManager.Instance.HasKey(baseEntry.KeyId)) ? KeyManager.Instance.GetKey(baseEntry.KeyId) : null;
                    byte[] deltaKey = (deltaEntry.IsEncrypted && KeyManager.Instance.HasKey(deltaEntry.KeyId)) ? KeyManager.Instance.GetKey(deltaEntry.KeyId) : null;

                    if (baseEntry.IsEncrypted && baseKey == null || deltaEntry.IsEncrypted && deltaKey == null)
                    {
                        return null;
                    }

                    using (CasReader reader =
                           new CasReader(baseReader.CreateViewStream(baseEntry.Offset, baseEntry.Size), baseKey,
                               baseEntry.EncryptedSize,
                               deltaReader.CreateViewStream(deltaEntry.Offset, deltaEntry.Size), deltaKey,
                               deltaEntry.EncryptedSize))
                    {
                        buffer = reader.Read();
                    }
                }
            }

            return (buffer != null) ? new MemoryStream(buffer) : null;
        }

        public void ProcessBundleEbx(DbObject sb, int bundleId, BinarySbDataHelper helper)
        {
            if (sb.GetValue<DbObject>("ebx") != null)
            {
                foreach (DbObject item in sb.GetValue<DbObject>("ebx"))
                {
                    EbxAssetEntry ebxAssetEntry = new EbxAssetEntry();
                    ebxAssetEntry = (EbxAssetEntry)AssetLoaderHelpers.ConvertDbObjectToAssetEntry(item, ebxAssetEntry, false);
                    AddEbx(ebxAssetEntry);
                }
            }
        }

        public void ProcessBundleRes(DbObject sb, int bundleId, BinarySbDataHelper helper)
        {
            if (sb.GetValue<DbObject>("res") != null)
            {
                foreach (DbObject item in sb.GetValue<DbObject>("res"))
                {
                    if (!ProfileManager.IsResTypeIgnored((ResourceType)item.GetValue("resType", 0L)))
                    {
                        ResAssetEntry resAssetEntry = new ResAssetEntry();
                        resAssetEntry = (ResAssetEntry)AssetLoaderHelpers.ConvertDbObjectToAssetEntry(item, resAssetEntry, false);

                        if (item.HasValue("SBFileLocation"))
                        {
                            resAssetEntry.SBFileLocation = item.GetValue<string>("SBFileLocation");
                        }

                        if (item.HasValue("TOCFileLocation"))
                        {
                            resAssetEntry.TOCFileLocation = item.GetValue<string>("TOCFileLocation");
                        }

                        if (item.HasValue("SB_CAS_Offset_Position"))
                        {
                            resAssetEntry.SB_CAS_Offset_Position = item.GetValue<int>("SB_CAS_Offset_Position");
                        }

                        if (item.HasValue("SB_CAS_Size_Position"))
                        {
                            resAssetEntry.SB_CAS_Size_Position = item.GetValue<int>("SB_CAS_Size_Position");
                        }

                        if (item.HasValue("SB_OriginalSize_Position"))
                            resAssetEntry.SB_OriginalSize_Position = item.GetValue<int>("SB_OriginalSize_Position");

                        if (item.HasValue("SB_Sha1_Position"))
                            resAssetEntry.SB_Sha1_Position = item.GetValue<int>("SB_Sha1_Position");

                        resAssetEntry.Bundles.Add(bundleId);
                        AddRes(resAssetEntry);
                    }
                }
            }
        }

        public void ProcessBundleChunks(DbObject sb, int bundleId, BinarySbDataHelper helper)
        {
            if (sb.GetValue<DbObject>("chunks") != null)
            {
                foreach (DbObject item in sb.GetValue<DbObject>("chunks"))
                {
                    ChunkAssetEntry chunkAssetEntry = new ChunkAssetEntry();
                    chunkAssetEntry = (ChunkAssetEntry)AssetLoaderHelpers.ConvertDbObjectToAssetEntry(item, chunkAssetEntry, false);

                    chunkAssetEntry.Bundles.Add(bundleId);
#if DEBUG
                    if (chunkAssetEntry.Id.ToString() == "a92100e2-6386-7c42-fd75-88571e788b05")
                    {

                    }
#endif 
                    AddChunk(chunkAssetEntry);
                }
            }
        }

        public DbObject ProcessTocChunks(string superBundleName, BinarySbDataHelper helper, bool isBase = false)
        {
            string text = FileSystem.ResolvePath(superBundleName);
            if (text == "")
            {
                return null;
            }
            DbObject dbObject = null;
            using (DbReader dbReader = new DbReader(new FileStream(text, FileMode.Open, FileAccess.Read), FileSystem.CreateDeobfuscator()))
            {
                dbObject = dbReader.ReadDbObject();
            }
            if (isBase && ProfileManager.DataVersion != 20141118 && ProfileManager.DataVersion != 20141117 && ProfileManager.DataVersion != 20151103 && ProfileManager.DataVersion != 20150223 && ProfileManager.DataVersion != 20131115 && ProfileManager.DataVersion != 20140225)
            {
                return dbObject;
            }
            if (dbObject.GetValue<DbObject>("chunks") != null)
            {
                foreach (DbObject item in dbObject.GetValue<DbObject>("chunks"))
                {
                    Guid value = item.GetValue<Guid>("id");
                    ChunkAssetEntry chunkAssetEntry = null;
                    if (Chunks.ContainsKey(value))
                    {
                        chunkAssetEntry = Chunks[value];
                        //chunkList.Remove(value);
                        Chunks.TryRemove(value, out _);
                        helper.RemoveChunkData(chunkAssetEntry.Id.ToString());
                    }
                    else
                    {
                        chunkAssetEntry = new ChunkAssetEntry();
                    }
                    chunkAssetEntry.Id = item.GetValue<Guid>("id");
                    chunkAssetEntry.Sha1 = item.GetValue<FMT.FileTools.Sha1>("sha1");
                    if (item.GetValue("size", 0L) != 0L)
                    {
                        chunkAssetEntry.Location = AssetDataLocation.SuperBundle;
                        chunkAssetEntry.Size = item.GetValue("size", 0L);
                        chunkAssetEntry.ExtraData = new AssetExtraData();
                        chunkAssetEntry.ExtraData.DataOffset = (uint)item.GetValue("offset", 0L);
                        //chunkAssetEntry.ExtraData.SuperBundleId = superBundles.Count - 1;
                        chunkAssetEntry.ExtraData.IsPatch = superBundleName.StartsWith("native_patch");
                    }

                    chunkAssetEntry.SB_LogicalOffset_Position = item.GetValue("SB_LogicalOffset_Position", 0u);
                    chunkAssetEntry.SB_LogicalSize_Position = item.GetValue("SB_LogicalSize_Position", 0u);
                    Chunks.TryAdd(chunkAssetEntry.Id, chunkAssetEntry);
                    //chunkList.Add(chunkAssetEntry.Id, chunkAssetEntry);
                }
                return dbObject;
            }
            return dbObject;
        }

        //public EbxAssetEntry AddEbx(DbObject ebx, bool returnExisting = false)
        //{
        //	EbxAssetEntry originalEbx = null;
        //	string text = ebx.GetValue<string>("name").ToLower();
        //	if (EBX.ContainsKey(text))
        //	{
        //		if(returnExisting)
        //			return EBX[text];
        //		else
        //			EBX.TryRemove(text, out originalEbx);
        //	}
        //	EbxAssetEntry ebxAssetEntry = new EbxAssetEntry();
        //	ebxAssetEntry.Name = text;
        //	ebxAssetEntry.Sha1 = ebx.GetValue<FMT.FileTools.Sha1>("sha1");
        //	ebxAssetEntry.BaseSha1 = GetBaseSha1(ebxAssetEntry.Sha1);
        //	ebxAssetEntry.Size = ebx.GetValue("size", 0L);
        //	ebxAssetEntry.OriginalSize = ebx.GetValue("originalSize", 0L);
        //	ebxAssetEntry.IsInline = ebx.HasValue("idata");
        //	ebxAssetEntry.Location = AssetDataLocation.Cas;
        //	if (ebx.HasValue("cas"))
        //	{
        //		ebxAssetEntry.Location = AssetDataLocation.CasNonIndexed;
        //		ebxAssetEntry.ExtraData = new AssetExtraData();
        //		ebxAssetEntry.ExtraData.DataOffset = (uint)ebx.GetValue("offset", 0L);
        //		ebxAssetEntry.ExtraData.Cas = ebx.HasValue("cas") ? ebx.GetValue<ushort>("cas") : null;
        //		ebxAssetEntry.ExtraData.Catalog = ebx.HasValue("catalog") ? ebx.GetValue<ushort>("catalog") : null;
        //              ebxAssetEntry.ExtraData.IsPatch = ebx.HasValue("patch") ? ebx.GetValue<bool>("patch") : false;
        //		ebxAssetEntry.ExtraData.CasPath = FileSystem.Instance.GetFilePath(ebx.GetValue("catalog", 0), ebx.GetValue("cas", 0), ebx.GetValue("patch", false));
        //          }
        //	else if (ebx.GetValue("sb", defaultValue: false))
        //	{
        //		ebxAssetEntry.Location = AssetDataLocation.SuperBundle;
        //		ebxAssetEntry.ExtraData = new AssetExtraData();
        //		ebxAssetEntry.ExtraData.DataOffset = (uint)ebx.GetValue("offset", 0L);
        //		ebxAssetEntry.ExtraData.SuperBundleId = superBundles.Count - 1;
        //	}

        //	if(ebx.HasValue("SBFileLocation"))
        //		ebxAssetEntry.SBFileLocation = ebx.GetValue<string>("SBFileLocation");
        //	if(ebx.HasValue("TOCFileLocation"))
        //		ebxAssetEntry.TOCFileLocation = ebx.GetValue<string>("TOCFileLocation");
        //	if(ebx.HasValue("SB_CAS_Offset_Position"))
        //		ebxAssetEntry.SB_CAS_Offset_Position = ebx.GetValue<int>("SB_CAS_Offset_Position");
        //	if (ebx.HasValue("SB_CAS_Size_Position"))
        //		ebxAssetEntry.SB_CAS_Size_Position = ebx.GetValue<int>("SB_CAS_Size_Position");
        //	if(ebx.HasValue("SB_OriginalSize_Position"))
        //		ebxAssetEntry.SB_OriginalSize_Position = ebx.GetValue<int>("SB_OriginalSize_Position");
        //	if(ebx.HasValue("SB_Sha1_Position"))
        //		ebxAssetEntry.SB_Sha1_Position = ebx.GetValue<int>("SB_Sha1_Position");

        //	if(originalEbx != null)
        //          {
        //		ebxAssetEntry.Bundles.AddRange(originalEbx.Bundles);
        //		ebxAssetEntry.Bundles.Add(Bundles.Count - 1);
        //          }

        //	EBX.TryAdd(text, ebxAssetEntry);
        //	return ebxAssetEntry;
        //}

        //public ResAssetEntry AddRes(DbObject res, bool returnExisting = false)
        //{
        //	string value = res.GetValue<string>("name");
        //	if (RES.ContainsKey(value))
        //	{
        //		if(returnExisting)
        //			return RES[value];

        //		RES.Remove(value);
        //		//return resList[value];
        //	}
        //	ResAssetEntry resAssetEntry = new ResAssetEntry();
        //	resAssetEntry.Name = value;
        //	resAssetEntry.Sha1 = res.GetValue<FMT.FileTools.Sha1>("sha1");
        //	resAssetEntry.BaseSha1 = rm.GetBaseSha1(resAssetEntry.Sha1);
        //	resAssetEntry.Size = res.GetValue("size", 0L);
        //	resAssetEntry.OriginalSize = res.GetValue("originalSize", 0L);
        //	var rrid = res.GetValue<string>("resRid");
        //	//if (rrid < 0) rrid *= -1;
        //	resAssetEntry.ResRid = Convert.ToUInt64(rrid);
        //	resAssetEntry.ResType = (uint)res.GetValue("resType", 0L);
        //	resAssetEntry.ResMeta = res.GetValue<byte[]>("resMeta");
        //	resAssetEntry.IsInline = res.HasValue("idata");
        //	resAssetEntry.Location = AssetDataLocation.Cas;
        //	if (res.HasValue("cas"))
        //	{
        //		resAssetEntry.Location = AssetDataLocation.CasNonIndexed;
        //		resAssetEntry.ExtraData = new AssetExtraData();
        //		resAssetEntry.ExtraData.DataOffset = (uint)res.GetValue("offset", 0L);
        //              resAssetEntry.ExtraData.Cas = res.HasValue("cas") ? res.GetValue<ushort>("cas") : null;
        //              resAssetEntry.ExtraData.Catalog = res.HasValue("catalog") ? res.GetValue<ushort>("catalog") : null;
        //              resAssetEntry.ExtraData.IsPatch = res.HasValue("patch") ? res.GetValue<bool>("patch") : false;
        //              resAssetEntry.ExtraData.CasPath = (res.HasValue("catalog") ? fs.GetFilePath(res.GetValue("catalog", 0), res.GetValue("cas", 0), res.HasValue("patch")) : fs.GetFilePath(res.GetValue("cas", 0)));
        //	}
        //	else if (res.GetValue("sb", defaultValue: false))
        //	{
        //		resAssetEntry.Location = AssetDataLocation.SuperBundle;
        //		resAssetEntry.ExtraData = new AssetExtraData();
        //		resAssetEntry.ExtraData.DataOffset = (uint)res.GetValue("offset", 0L);
        //		resAssetEntry.ExtraData.SuperBundleId = superBundles.Count - 1;
        //	}
        //	else if (res.GetValue("cache", defaultValue: false))
        //	{
        //		resAssetEntry.Location = AssetDataLocation.Cache;
        //		resAssetEntry.ExtraData = new AssetExtraData();
        //		//resAssetEntry.ExtraData.DataOffset = 3735928559L;
        //	}
        //	else if (res.GetValue("casPatchType", 0) == 2)
        //	{
        //		resAssetEntry.ExtraData = new AssetExtraData();
        //		resAssetEntry.ExtraData.BaseSha1 = res.GetValue<FMT.FileTools.Sha1>("baseSha1");
        //		resAssetEntry.ExtraData.DeltaSha1 = res.GetValue<FMT.FileTools.Sha1>("deltaSha1");
        //	}

        //	resAssetEntry.SBFileLocation = res.GetValue<string>("SBFileLocation");
        //	resAssetEntry.TOCFileLocation = res.GetValue<string>("TOCFileLocation");
        //	resAssetEntry.SB_CAS_Offset_Position = res.GetValue<int>("SB_CAS_Offset_Position");
        //	resAssetEntry.SB_CAS_Size_Position = res.GetValue<int>("SB_CAS_Size_Position");
        //	resAssetEntry.SB_OriginalSize_Position = res.GetValue<int>("SB_OriginalSize_Position");
        //	resAssetEntry.SB_Sha1_Position = res.GetValue<int>("SB_Sha1_Position");

        //	RES.Add(value, resAssetEntry);
        //	if (resAssetEntry.ResRid != 0L)
        //	{
        //		if (resRidList.ContainsKey(resAssetEntry.ResRid))
        //			resRidList.Remove(resAssetEntry.ResRid);

        //		resRidList.Add(resAssetEntry.ResRid, resAssetEntry);
        //	}
        //	return resAssetEntry;
        //}

        //public ChunkAssetEntry AddChunk(DbObject chunk, bool returnExisting = false)
        //{
        //	Guid value = chunk.GetValue<Guid>("id");
        //	if(value.ToString() == "3e0a186b-c286-1dff-455b-7eb097c3e8f9")
        //          {

        //          }

        //	ChunkAssetEntry chunkAssetEntry = new ChunkAssetEntry();
        //	if (Chunks.ContainsKey(value))
        //	{
        //		chunkAssetEntry = Chunks[value];
        //		chunkAssetEntry.IsTocChunk = false;
        //	}

        //	chunkAssetEntry.Id = value;
        //	chunkAssetEntry.Sha1 = chunk.GetValue<FMT.FileTools.Sha1>("sha1");
        //	chunkAssetEntry.Size = chunk.GetValue("size", 0L);
        //	chunkAssetEntry.LogicalOffset = chunk.GetValue("logicalOffset", 0u);
        //	chunkAssetEntry.LogicalSize = chunk.GetValue("logicalSize", 0u);
        //	chunkAssetEntry.RangeStart = chunk.GetValue("rangeStart", 0u);
        //	chunkAssetEntry.RangeEnd = chunk.GetValue("rangeEnd", 0u);
        //	chunkAssetEntry.BundledSize = chunk.GetValue("bundledSize", 0u);
        //	chunkAssetEntry.IsInline = chunk.HasValue("idata");
        //	chunkAssetEntry.Location = AssetDataLocation.Cas;
        //	if (chunk.HasValue("cas"))
        //	{
        //		chunkAssetEntry.Location = AssetDataLocation.CasNonIndexed;
        //		chunkAssetEntry.ExtraData = new AssetExtraData();
        //		chunkAssetEntry.ExtraData.DataOffset = (uint)chunk.GetValue("offset", 0L);
        //		chunkAssetEntry.ExtraData.Cas = (ushort)chunk.GetValue("cas", 0);
        //		chunkAssetEntry.ExtraData.Catalog = (ushort)chunk.GetValue("catalog", 0);
        //		chunkAssetEntry.ExtraData.IsPatch = chunk.GetValue("patch", false);
        //		if(string.IsNullOrEmpty(chunkAssetEntry.ExtraData.CasPath))
        //			chunkAssetEntry.ExtraData.CasPath = (chunk.HasValue("catalog") ? fs.GetFilePath(chunk.GetValue("catalog", 0), chunk.GetValue("cas", 0), chunk.GetValue("patch", false)) : fs.GetFilePath(chunk.GetValue("cas", 0)));

        //	}

        //	else if (chunk.GetValue("sb", defaultValue: false))
        //	{
        //		chunkAssetEntry.Location = AssetDataLocation.SuperBundle;
        //		chunkAssetEntry.ExtraData = new AssetExtraData();
        //		chunkAssetEntry.ExtraData.DataOffset = (uint)chunk.GetValue("offset", 0L);
        //		chunkAssetEntry.ExtraData.SuperBundleId = superBundles.Count - 1;
        //	}
        //	else if (chunk.GetValue("cache", defaultValue: false))
        //	{
        //		chunkAssetEntry.Location = AssetDataLocation.Cache;
        //		chunkAssetEntry.ExtraData = new AssetExtraData();
        //	}


        //	var tocfL = chunk.GetValue<string>("TOCFileLocation");
        //	chunkAssetEntry.TOCFileLocation = tocfL;
        //	chunkAssetEntry.TOCFileLocations.Add(tocfL);

        //	var sbfl = chunk.GetValue<string>("SBFileLocation");
        //          if (!string.IsNullOrEmpty(sbfl))
        //          {
        //		chunkAssetEntry.SBFileLocation = sbfl;
        //		chunkAssetEntry.SBFileLocations.Add(tocfL);
        //	}

        //	chunkAssetEntry.SB_CAS_Offset_Position = chunk.GetValue<int>("SB_CAS_Offset_Position");
        //	chunkAssetEntry.SB_CAS_Size_Position = chunk.GetValue<int>("SB_CAS_Size_Position");
        //	chunkAssetEntry.SB_OriginalSize_Position = chunk.GetValue<int>("SB_OriginalSize_Position");
        //	chunkAssetEntry.SB_Sha1_Position = chunk.GetValue<int>("SB_Sha1_Position");
        //	chunkAssetEntry.CASFileLocation = chunk.GetValue<string>("CASFileLocation");

        //	AddChunk(chunkAssetEntry);

        //	return chunkAssetEntry;
        //}

        public void SendManagerCommand(string type, string command, params object[] value)
        {
            if (CustomAssetManagers.ContainsKey(type))
            {
                CustomAssetManagers[type].OnCommand(command, value);
            }
        }

        public static MemoryStream CacheDecompress()
        {
            return CacheManager.CacheDecompress();
        }

        public static async Task<MemoryStream> CacheDecompressAsync()
        {
            return await CacheManager.CacheDecompressAsync();
        }

        public static bool CacheCompress(MemoryStream msCache)
        {
            return CacheManager.CacheCompress(msCache);
        }

        public bool CacheRead(out List<EbxAssetEntry> prePatchCache)
        {
            prePatchCache = null;
            return CacheManager.CacheRead(out prePatchCache);
        }

        public void CacheWrite()
        {
            CacheManager.CacheWrite();
        }

        public bool DoLegacyImageImport(MemoryStream stream, LegacyFileEntry lfe)
        {
            var bytes = ((MemoryStream)GetCustomAsset("legacy", lfe)).ToArray();
            ImageEngineImage originalImage = new ImageEngineImage(bytes);

            ImageEngineImage newImage = new ImageEngineImage(stream);

            var mipHandling = originalImage.MipMaps.Count > 1 ? MipHandling.GenerateNew : MipHandling.KeepTopOnly;


            if (originalImage.Format == ImageEngineFormat.DDS_DXT5)
            {
                bytes = newImage.Save(
                    new ImageFormats.ImageEngineFormatDetails(
                        ImageEngineFormat.DDS_DXT5
                        , CSharpImageLibrary.Headers.DDS_Header.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM)
                    , mipHandling
                    , removeAlpha: false);
            }
            else if (originalImage.Format == ImageEngineFormat.DDS_DXT3)
            {
                bytes = newImage.Save(
                    new ImageFormats.ImageEngineFormatDetails(
                        ImageEngineFormat.DDS_DXT3
                        , CSharpImageLibrary.Headers.DDS_Header.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB)
                    , MipHandling.KeepTopOnly
                    , removeAlpha: false);
            }
            else if (originalImage.Format == ImageEngineFormat.DDS_DXT1)
            {
                bytes = newImage.Save(
                    new ImageFormats.ImageEngineFormatDetails(
                        ImageEngineFormat.DDS_DXT1
                        , CSharpImageLibrary.Headers.DDS_Header.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB)
                    , mipHandling
                    , removeAlpha: false);
            }
            else
            {
                bytes = newImage.Save(
                    new ImageFormats.ImageEngineFormatDetails(
                        ImageEngineFormat.DDS_DXT1
                        , CSharpImageLibrary.Headers.DDS_Header.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB)
                    , mipHandling
                    , removeAlpha: false);
            }


            ModifyLegacyAsset(lfe.Name, bytes, false);
            return true;
        }

        public async Task<bool> DoLegacyImageImportAsync(string importFilePath, LegacyFileEntry lfe)
        {
            return await Task.Run(() => { return DoLegacyImageImport(importFilePath, lfe); });
        }

        public bool DoLegacyImageImport(string importFilePath, LegacyFileEntry lfe)
        {
            var extension = "DDS";
            var spl = importFilePath.Split('.');
            extension = spl[spl.Length - 1].ToUpper();

            var compatible_extensions = new List<string>() { "DDS", "PNG" };
            if (!compatible_extensions.Contains(extension))
            {
                throw new NotImplementedException("Incorrect file type used in Texture Importer");
            }

            // -------------------------------- //
            // Gets Image Format from Extension //
            TextureUtils.ImageFormat imageFormat = TextureUtils.ImageFormat.DDS;
            imageFormat = (TextureUtils.ImageFormat)Enum.Parse(imageFormat.GetType(), extension);
            //if (MainEditorWindow != null && imageFormat == TextureUtils.ImageFormat.PNG)
            //{
            //	MainEditorWindow.LogWarning("Legacy PNG Image conversion is EXPERIMENTAL. Please dont use it in your production Mods!" + Environment.NewLine);
            //}
            // -------------------------------- //

            MemoryStream memoryStream = (MemoryStream)AssetManager.Instance.GetCustomAsset("legacy", lfe);
            var bytes = memoryStream.ToArray();

            //TextureUtils.BlobData pOutData = default(TextureUtils.BlobData);
            if (imageFormat == TextureUtils.ImageFormat.DDS)
            {
                ImageEngineImage originalImage = new ImageEngineImage(bytes);
                ImageEngineImage newImage = new ImageEngineImage(importFilePath);
                if (originalImage.Format != newImage.Format)
                {
                    var mipHandling = originalImage.MipMaps.Count > 1 ? MipHandling.GenerateNew : MipHandling.KeepTopOnly;

                    bytes = newImage.Save(
                        new ImageFormats.ImageEngineFormatDetails(
                            ImageEngineFormat.DDS_DXT1
                            , CSharpImageLibrary.Headers.DDS_Header.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB)
                        , mipHandling
                        , removeAlpha: false);
                }
                else
                {
                    bytes = File.ReadAllBytes(importFilePath);
                }
            }
            else
            {

                ImageEngineImage originalImage = new ImageEngineImage(bytes);

                ImageEngineImage imageEngineImage = new ImageEngineImage(importFilePath);
                //imageEngineImage.Resize(
                //	(imageEngineImage.Height + imageEngineImage.Width)
                //	/ (originalImage.Height + originalImage.Width)
                //	);
                if (imageEngineImage.Height > originalImage.Height)
                {
                    //imageEngineImage.Resize(
                    //	(imageEngineImage.Height + imageEngineImage.Width)
                    //	*
                    //	(originalImage.Height + originalImage.Width)
                    //	- (imageEngineImage.Height + imageEngineImage.Width)
                    //	);
                }
                var mipHandling = originalImage.MipMaps.Count > 1 ? MipHandling.GenerateNew : MipHandling.KeepTopOnly;


                if (originalImage.Format == ImageEngineFormat.DDS_DXT5)
                {
                    bytes = imageEngineImage.Save(
                        new ImageFormats.ImageEngineFormatDetails(
                            ImageEngineFormat.DDS_DXT5
                            , CSharpImageLibrary.Headers.DDS_Header.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM)
                        , mipHandling
                        , removeAlpha: false);
                }
                else if (originalImage.Format == ImageEngineFormat.DDS_DXT3)
                {
                    bytes = imageEngineImage.Save(
                        new ImageFormats.ImageEngineFormatDetails(
                            ImageEngineFormat.DDS_DXT3
                            , CSharpImageLibrary.Headers.DDS_Header.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB)
                        , MipHandling.KeepTopOnly
                        , removeAlpha: false);
                }
                else if (originalImage.Format == ImageEngineFormat.DDS_DXT1)
                {
                    bytes = imageEngineImage.Save(
                        new ImageFormats.ImageEngineFormatDetails(
                            ImageEngineFormat.DDS_DXT1
                            , CSharpImageLibrary.Headers.DDS_Header.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB)
                        , mipHandling
                        , removeAlpha: false);
                }
                else
                {
                    bytes = imageEngineImage.Save(
                        new ImageFormats.ImageEngineFormatDetails(
                            ImageEngineFormat.DDS_DXT1
                            , CSharpImageLibrary.Headers.DDS_Header.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB)
                        , mipHandling
                        , removeAlpha: false);
                }

            }

            AssetManager.Instance.ModifyLegacyAsset(lfe.Name, bytes, false);
            return true;

        }


        public static string ApplicationDirectory
        {
            get
            {
                return AppContext.BaseDirectory + "\\";
            }
        }

        private string lastLogMessage = null;
        public void WriteToLog(string text, params object[] vars)
        {
            if (Logger != null)
            {
                if (lastLogMessage == text)
                    return;

                lastLogMessage = text;
                Logger.Log(text, vars);
            }
        }

        public FMT.FileTools.Sha1 GenerateSha1(byte[] buffer)
        {
            using (var sha1instance = SHA1.Create())
                return new FMT.FileTools.Sha1(sha1instance.ComputeHash(buffer));
            //using (SHA1Managed sHA1Managed = new SHA1Managed())
            //{
            //	return new FMT.FileTools.Sha1(sHA1Managed.ComputeHash(buffer));
            //}
        }

        public Guid GenerateChunkId(AssetEntry ae)
        {
            ulong num = Murmur2.HashString64(ae.Filename, 18532uL);
            ulong value = Murmur2.HashString64(ae.Path, 18532uL);
            int num2 = 1;
            Guid guid = Guid.Empty;
            do
            {
                using (NativeWriter nativeWriter = new NativeWriter(new MemoryStream()))
                {
                    nativeWriter.Write(value);
                    nativeWriter.Write((ulong)((long)num ^ num2));
                    byte[] array = ((MemoryStream)nativeWriter.BaseStream).ToArray();
                    array[15] = 1;
                    guid = new Guid(array);
                }
                num2++;
            }
            while (AssetManager.Instance.GetChunkEntry(guid) != null);
            return guid;
        }

        public bool ModifyEntry(IAssetEntry entry, byte[] d2)
        {
            if (entry is EbxAssetEntry ebxEntry)
            {
                ModifyEbxBinary(ebxEntry.Name, d2);
                return true;
            }

            if (entry is ResAssetEntry resEntry)
            {
                ModifyRes(resEntry.Name, d2);
                return true;
            }

            if (entry is ChunkAssetEntry chunkEntry)
                return ModifyChunk(chunkEntry, d2);

            if (entry is EmbeddedFileEntry embeddedFileEntry)
            {
                if(!Instance.EmbeddedFileEntries.Any(x=>x.Name == embeddedFileEntry.Name))
                    Instance.EmbeddedFileEntries.Add(embeddedFileEntry);
            }

            return false;
        }

        //public void Log(string text, params object[] vars)
        //{
        //	Debug.WriteLine($"[AM][{DateTime.Now.ToShortTimeString()}] {text}");
        //}

        //public void LogWarning(string text, params object[] vars)
        //{
        //          Debug.WriteLine($"[AM][{DateTime.Now.ToShortTimeString()}][WARNING] {text}");
        //      }

        //      public void LogError(string text, params object[] vars)
        //{
        //          Debug.WriteLine($"[AM][{DateTime.Now.ToShortTimeString()}][ERROR] {text}");
        //      }
    }


    public class BinarySbDataHelper
    {
        protected Dictionary<string, byte[]> ebxDataFiles = new Dictionary<string, byte[]>();

        protected Dictionary<string, byte[]> resDataFiles = new Dictionary<string, byte[]>();

        protected Dictionary<string, byte[]> chunkDataFiles = new Dictionary<string, byte[]>();

        private AssetManager am;

        public BinarySbDataHelper(AssetManager inParent)
        {
            am = inParent;
        }

        public void FilterAndAddBundleData(DbObject baseList, DbObject deltaList)
        {
            //FilterBinaryBundleData(baseList, deltaList, "ebx", ebxDataFiles);
            //FilterBinaryBundleData(baseList, deltaList, "res", resDataFiles);
            //FilterBinaryBundleData(baseList, deltaList, "chunks", chunkDataFiles);
        }

        public void RemoveEbxData(string name)
        {
            ebxDataFiles.Remove(name);
        }

        public void RemoveResData(string name)
        {
            resDataFiles.Remove(name);
        }

        public void RemoveChunkData(string name)
        {
            chunkDataFiles.Remove(name);
        }

    }

}
#pragma warning restore SYSLIB0021 // Type or member is obsolete
