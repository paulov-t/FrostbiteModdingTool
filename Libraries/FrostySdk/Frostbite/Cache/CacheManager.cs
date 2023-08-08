using FMT.FileTools;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using v2k4FIFAModdingCL;

namespace FrostySdk.Frostbite
{
    /// <summary>
    /// A Build / Load Cache Data via FrostySDK
    /// </summary>
    public class CacheManager : ILogger
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="GameVersion"></param>
        /// <param name="GameLocation"></param>
        /// <param name="logger">ILogger</param>
        /// <param name="forceDeleteOfOld">Force the deletion of old Cache to rebuild it again</param>
        /// <param name="loadSDK">If you have already built the SDK, then just use the one you have</param>
        /// <returns></returns>
        public bool LoadData(string GameVersion, string GameLocation, ILogger logger = null, bool forceDeleteOfOld = false, bool loadSDK = false)
        {
            if (AssetManager.Instance != null)
            {
                AssetManager.Instance.Dispose();
                AssetManager.Instance = null;
            }

            var result = LoadDataAsync(GameVersion, GameLocation, logger, forceDeleteOfOld, loadSDK).Result;
            return result;
        }

        public async Task<bool> LoadDataAsync(string GameVersion, string gameLocation, ILogger logger = null, bool forceDeleteOfOld = false, bool loadSDK = false)
        {
            Debug.WriteLine($"[DEBUG] BuildCache::LoadDataAsync({GameVersion},{gameLocation})");
            if (ProfileManager.Initialize(GameVersion))
            {
                return await Task.Run(() => Load(gameLocation, logger, loadSDK, forceDeleteOfOld));
            }
            else
            {
                logger.LogError("Profile does not exist");
                Debug.WriteLine($"[ERROR] Failed to initialise");
            }
            return false;
        }

        public bool Load(ReadOnlySpan<char> gameLocation, ILogger logger, bool loadSDK, bool forceDeleteOfOld)
        {
            var profileName = string.Empty;
            if (gameLocation.EndsWith(".exe"))
            {
                var FileInfoEXE = new FileInfo(gameLocation.ToString());
                gameLocation = Directory.GetParent(FileInfoEXE.FullName).FullName;//.Replace(".exe", "");
                profileName = FileInfoEXE.Name.Replace(".exe", "");
            }
            if (!string.IsNullOrEmpty(profileName) && !ProfileManager.Initialize(profileName))
            {
                logger.LogError("Profile does not exist");
                Debug.WriteLine($"[ERROR] Failed to initialise");
                return false;
            }

            if (ProfileManager.RequiresKey)
            {
                KeyManager.ReadInKeys();
            }

            Debug.WriteLine($"[DEBUG] LoadDataAsync::Initialising Type Library");

            if (TypeLibrary.Initialize(loadSDK))
            {
                if (logger == null)
                    logger = this;

                logger.Log("Loaded Type Library SDK");
                if (FileSystem.Instance == null)
                    FileSystem.Instance = new FileSystem(gameLocation.ToString());

                if (File.Exists(CachePath) && forceDeleteOfOld)
                    File.Delete(CachePath);

                logger.Log("Initialised File & Resource System");
                if (AssetManager.Instance == null)
                    AssetManager.Instance = new AssetManager(logger);

                AssetManager.Instance.RegisterLegacyAssetManager();
                AssetManager.Instance.Initialize(loadSDK);

                logger.Log("Initialised Asset Manager");

                return true;
            }
            return false;

        }


        public static bool HasEbx(string name)
        {
            return GetEbx(name) != null;
        }

        public static EbxAssetEntry GetEbx(string name)
        {
            var reader = GetCacheReader();

            return EnumerateEbx(name, string.Empty, false, false).Result.First();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected static async ValueTask<IEnumerable<EbxAssetEntry>> EnumerateEbx(string name, string type, bool modifiedOnly, bool includeLinked)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            using (NativeReader nativeReader = new NativeReader(AssetManager.CacheDecompress()))
            {
                if (nativeReader.ReadLengthPrefixedString() != ProfileManager.ProfileName)
                    return null;

                _ = nativeReader.ReadULong();

                var EbxDataOffset = nativeReader.ReadULong();
                var ResDataOffset = nativeReader.ReadULong();
                var ChunkDataOffset = nativeReader.ReadULong();
                var NameToPositionOffset = nativeReader.ReadULong();

                nativeReader.Position = (long)EbxDataOffset;
                var ebxCount = nativeReader.ReadUInt();
                var positionOfAsset = -1L;
                for (var i = 0; i < ebxCount; i++)
                {
                    var ebxName = nativeReader.ReadLengthPrefixedString();
                    var ebxPositions = nativeReader.ReadLong();
                    if (ebxName == name)
                    {
                        positionOfAsset = ebxPositions;
                        break;
                    }
                }

                if (positionOfAsset == -1)
                    return null;

                nativeReader.Position = positionOfAsset;
                GetCacheReader().ReadEbxAssetEntry(nativeReader);

            }
            return new List<EbxAssetEntry>();
        }

        private string LastMessage = null;

        public void Log(string text, params object[] vars)
        {
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(LastMessage))
            {
                Debug.WriteLine(text);
            }
            LastMessage = text;
        }

        public void LogError(string text, params object[] vars)
        {
        }

        public void LogWarning(string text, params object[] vars)
        {
        }
        public static string ApplicationDirectory
        {
            get
            {
                return AppContext.BaseDirectory + "\\";
            }
        }

        public static string CacheDirectoryPath => Path.Combine(ApplicationDirectory, "_GameCaches");
        public static string CachePath
        {
            get
            {
                return $"{CacheDirectoryPath}\\{FileSystem.Instance.CacheName}.cache";
            }
        }

        public static MemoryStream CacheDecompress()
        {
            Directory.CreateDirectory(CacheDirectoryPath);

            // Decompress the Cache File into a Memory Stream
            using (Ionic.Zip.ZipFile zipCache = Ionic.Zip.ZipFile.Read(CachePath))
            {
                Ionic.Zip.ZipEntry zipCacheEntry = zipCache.Entries.First();
                var msCache = new MemoryStream();
                zipCacheEntry.Extract(msCache);
                msCache.Seek(0, SeekOrigin.Begin);
                return msCache;
            }
        }

        public static async Task<MemoryStream> CacheDecompressAsync()
        {
            return await Task.FromResult(CacheDecompress());
        }

        public static bool CacheCompress(MemoryStream msCache)
        {
            Directory.CreateDirectory(CacheDirectoryPath);

            msCache.Seek(0, SeekOrigin.Begin);
            // Compress the Cache File into a Memory Stream
            Ionic.Zip.ZipFile zipCache = new Ionic.Zip.ZipFile(CachePath);
            if (zipCache.ContainsEntry("cacheEntry"))
                zipCache.RemoveEntry("cacheEntry");
            zipCache.AddEntry("cacheEntry", msCache);
            zipCache.Save();
            return File.Exists(CachePath);
        }

        public static async Task<bool> CacheCompressAsync(MemoryStream msCache)
        {
            return await Task.FromResult(CacheCompress(msCache));
        }

        public static ICacheReader GetCacheReader()
        {
            if (!string.IsNullOrEmpty(ProfileManager.CacheReader))
            {
                var resultFromPlugin = ((ICacheReader)AssetManager.Instance.LoadTypeFromPlugin(ProfileManager.CacheReader));
                return resultFromPlugin;
            }
            return null;
        }

        public static bool CacheRead(out List<EbxAssetEntry> prePatchCache)
        {
            prePatchCache = null;
            if (!File.Exists(CachePath))
            {
                AssetManager.Instance.WriteToLog($"Did not find {CachePath}.");
                return false;
            }
            AssetManager.Instance.WriteToLog($"Loading data ({FileSystem.Instance.CacheName}.cache)");

            if (!string.IsNullOrEmpty(ProfileManager.CacheReader))
            {
                var resultTypeFromPlugin = AssetManager.Instance.LoadTypeFromPlugin(ProfileManager.CacheReader);
                if (resultTypeFromPlugin != null)
                {
                    var resultFromPlugin = ((ICacheReader)resultTypeFromPlugin).Read();
                    return resultFromPlugin;
                }

                var resultTypeFromAssembly = AssetManager.LoadTypeByName(ProfileManager.CacheReader);
                if (resultTypeFromAssembly != null)
                {
                    var resultFromPlugin = ((ICacheReader)resultTypeFromAssembly).Read();
                    return resultFromPlugin;
                }
            }

            return false;
        }

        public static void CacheWrite()
        {
            if (!string.IsNullOrEmpty(ProfileManager.CacheWriter))
            {
                //((ICacheWriter)AssetManager.Instance.LoadTypeFromPlugin(ProfileManager.CacheWriter)).Write();
                var cacheWriter = AssetManager.Instance.LoadTypeFromPlugin(ProfileManager.CacheWriter);
                if (cacheWriter == null)
                    cacheWriter = AssetManager.LoadTypeByName(ProfileManager.CacheWriter);


                if (cacheWriter != null)
                {
                    ((ICacheWriter)cacheWriter).Write();
                }

                return;
            }

            throw new NotImplementedException("Cannot find a CacheWriter for the specified game. Assign one in your Game Profile.");
        }

        public static bool DoesCacheNeedsRebuilding()
        {
            if (GameInstanceSingleton.Instance == null)
                throw new ArgumentNullException("Game Instance Singleton has not been setup");

            if (!GameInstanceSingleton.Instance.INITIALIZED)
                throw new ArgumentNullException("Game Instance Singleton - Game has not been selected");

            if (ProfileManager.RequiresKey)
            {
                KeyManager.ReadInKeys();
            }

            if (ProfileManager.Initialize(GameInstanceSingleton.Instance.GAMEVERSION))
            {
                if (FileSystem.Instance != null)
                    FileSystem.Instance = null;

                new FileSystem(new string(GameInstanceSingleton.Instance.GAMERootPath));
                if (!File.Exists(CachePath))
                {
                    return true;
                }
                using (NativeReader nativeReader = new NativeReader(AssetManager.CacheDecompress()))
                {
                    if (nativeReader.ReadLengthPrefixedString() != ProfileManager.ProfileName)
                    {
                        return true;
                    }
                    var cacheHead = nativeReader.ReadULong();
                    if (cacheHead != FileSystem.Instance.SystemIteration)
                    {
                        return true;
                    }
                }

            }

            return false;
        }

    }
}
