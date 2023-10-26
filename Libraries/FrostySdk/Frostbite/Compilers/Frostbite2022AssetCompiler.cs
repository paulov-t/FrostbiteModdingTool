using FMT.FileTools;
using FMT.Logging;
using Frostbite.FileManagers;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using ModdingSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace FrostySdk.Frostbite.Compilers
{
    /// <summary>
    /// An Asset Compiler that can be used for most 2022 games using Frostbite Engine
    /// </summary>
    public class Frostbite2022AssetCompiler : FrostbiteNullCompiler, IAssetCompiler
    {
        public string PatchDirectory { get; } = "Patch";

        public Dictionary<ModType, int> ErrorCounts { get; } = new Dictionary<ModType, int>();

        public Dictionary<string, string> AssetEntryErrorList { get; } = new Dictionary<string, string>();

        public ModExecutor ModExecuter { get; set; }

        public void MakeTOCOriginals(string dir)
        {
            foreach (var tFile in Directory.EnumerateFiles(dir, "*.toc"))
            {
                if (File.Exists(tFile + ".bak"))
                    File.Copy(tFile + ".bak", tFile, true);
            }

            foreach (var tFile in Directory.EnumerateFiles(dir, "*.sb"))
            {
                if (File.Exists(tFile + ".bak"))
                    File.Copy(tFile + ".bak", tFile, true);
            }

            foreach (var internalDir in Directory.EnumerateDirectories(dir))
            {
                MakeTOCOriginals(internalDir);
            }
        }

        public void MakeTOCBackups(string dir)
        {
            foreach (var tFile in Directory.EnumerateFiles(dir, "*.toc"))
            {
                File.Copy(tFile, tFile + ".bak", true);
            }

            foreach (var tFile in Directory.EnumerateFiles(dir, "*.sb"))
            {
                File.Copy(tFile, tFile + ".bak", true);
            }

            foreach (var internalDir in Directory.EnumerateDirectories(dir))
            {
                MakeTOCBackups(internalDir);
            }
        }

        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive, ILogger logger)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            var filesToCopy = new List<(FileInfo, FileInfo)>();

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                if (file.Extension.Contains("bak", StringComparison.OrdinalIgnoreCase))
                    continue;

                var targetFile = new FileInfo(targetFilePath);
                if (!file.Extension.Contains("cas", StringComparison.OrdinalIgnoreCase))
                {
                    filesToCopy.Add((file, targetFile));
                }
                else
                {
                    if (!targetFile.Exists)
                    {
                        filesToCopy.Add((file, targetFile));
                        continue;
                    }

                    if (targetFile.Length != file.Length)
                    {
                        filesToCopy.Add((file, targetFile));
                        continue;
                    }

                    if (targetFile.LastWriteTime != file.LastWriteTime)
                    {
                        filesToCopy.Add((file, targetFile));
                        continue;
                    }

                    //if (targetFile.LastAccessTime != file.LastAccessTime)
                    //{
                    //    filesToCopy.Add((file, targetFile));
                    //    continue;
                    //}
                }
            }

            //var index = 1;
            logger.Log($"Data Setup - Copying {sourceDir}");
            foreach (var ftc in filesToCopy)
            {
                ftc.Item1.CopyTo(ftc.Item2.FullName, true);
                //logger.Log($"Data Setup - Copied ({index}/{filesToCopy.Count}) - {ftc.Item1.FullName}");
                //index++;
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true, logger);
                }
            }
        }


        protected static void CopyDataFolder(string from_datafolderpath, string to_datafolderpath, ILogger logger)
        {
            CopyDirectory(from_datafolderpath, to_datafolderpath, true, logger);
        }

        public virtual bool RequiresCacheToCompile { get; } = true;

        public virtual void FindModdedCasFilesWithoutCache(ref Dictionary<string, List<ModdedFile>> casToMods, string directory = "native_patch")
        {
            foreach (var sbName in FileSystem.Instance.SuperBundles)
            {
                var tocFileRAW = $"{directory}/{sbName}.toc";
                string tocFileLocation = FileSystem.Instance.ResolvePath(tocFileRAW);
                if (string.IsNullOrEmpty(tocFileLocation) || !File.Exists(tocFileLocation))
                {
                    AssetManager.Instance.Logger.LogWarning($"Unable to find Toc {tocFileRAW}");
                    continue;
                }

                ModExecuter.Logger.Log($"Searching for edits to {tocFileRAW}");

                var tfBundles = (TOCFile)Activator.CreateInstance(FileSystem.Instance.TOCFileType, tocFileRAW);
                using (var nrTOC = new NativeReader(new FileStream(tocFileLocation, FileMode.Open)))
                {
                    tfBundles.ReadOnlyBundles(nrTOC);
                }
                

                // If mod doesn't have any bundles defined. Then we fallback to ALWAYS looking for bundles.
                bool hasBundles = ModExecuter.ModifiedAssets.Any(x => x.Value.Bundles.Count == 0);
                foreach (var mod in ModExecuter.ModifiedAssets)
                {
                    if (tfBundles.BundleEntries.Any(x => mod.Value.Bundles.Contains(x.NameHash)))
                        hasBundles = true;
                }

                //tfBundles.Dispose();
                tfBundles = null;

                if (!hasBundles)
                    continue;

                ModExecuter.Logger.Log($"Found edits to {tocFileRAW}. Finding files.");

                using var tf = (TOCFile)Activator.CreateInstance(FileSystem.Instance.TOCFileType, tocFileRAW, false, false, false, 0, false);

                var namesOfObjects = tf.GetNamesOfObjects();
                if (namesOfObjects.Count == 0)
                    continue;

                int countOfMods = 0;

                foreach (var mod in ModExecuter.ModifiedAssets)
                {
                    if (!namesOfObjects.Contains(mod.Key))
                        continue;

                    var objects = tf.GetObjects();

                    
                    {
                        AssetEntry originalEntry = null;
                        if (mod.Value is EbxAssetEntry)
                            originalEntry = objects["ebx"].ContainsKey(mod.Key) ? AssetLoaderHelpers.ConvertDbObjectToAssetEntry(objects["ebx"][mod.Key], new EbxAssetEntry()) : null;
                        else if (mod.Value is ResAssetEntry)
                            originalEntry = objects["res"].ContainsKey(mod.Key) ? AssetLoaderHelpers.ConvertDbObjectToAssetEntry(objects["res"][mod.Key], new ResAssetEntry()) : null;
                        else if (mod.Value is ChunkAssetEntry)
                            originalEntry = objects["chunks"].ContainsKey(mod.Key) ? AssetLoaderHelpers.ConvertDbObjectToAssetEntry(objects["chunks"][mod.Key], new ChunkAssetEntry()) : null;

                        if (originalEntry == null)
                            continue;

                        if (originalEntry.ExtraData == null || string.IsNullOrEmpty(originalEntry.ExtraData.CasPath))
                            continue;

                        var casPath = originalEntry.ExtraData.CasPath;
                        if (!casToMods.ContainsKey(casPath))
                            casToMods.Add(casPath, new List<ModdedFile>());

                        casToMods[casPath].Add(new ModdedFile(mod.Value.Sha1, mod.Value.Name, false, mod.Value, originalEntry));

                        countOfMods++;
                    }
                }

                ModExecuter.Logger.Log($"Found edits to {tocFileRAW}. Found {countOfMods} files.");
                namesOfObjects.Clear();
                namesOfObjects = null;
                

            }

            if (directory == "native_patch")
                FindModdedCasFilesWithoutCache(ref casToMods, "native_data");
        }

        protected Dictionary<string, List<ModdedFile>> GetModdedCasFiles(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            Dictionary<string, List<ModdedFile>> casToMods = new Dictionary<string, List<ModdedFile>>();

            // -----------------------------------------------------------
            // Only load cache when required
            //if (AssetManager.Instance == null)
            if (RequiresCacheToCompile && (AssetManager.Instance == null || !AssetManager.Instance.EBX.Any()))
            {
                CacheManager cacheManager = new CacheManager();
                cacheManager.LoadData(ProfileManager.ProfileName, ModExecuter.GamePath, ModExecuter.Logger, false, true);
            }
            else
            {
                FindModdedCasFilesWithoutCache(ref casToMods);
            }

            // ------ End of handling Legacy files ---------

            foreach (var mod in ModExecuter.ModifiedAssets)
            {
                AssetEntry originalEntry = null;
                if (mod.Value is EbxAssetEntry)
                    originalEntry = AssetManager.Instance.GetEbxEntry(mod.Value.Name);
                else if (mod.Value is ResAssetEntry)
                    originalEntry = AssetManager.Instance.GetResEntry(mod.Value.Name);
                else if (mod.Value is ChunkAssetEntry)
                    originalEntry = AssetManager.Instance.GetChunkEntry(Guid.Parse(mod.Value.Name));

                if (originalEntry == null)
                    continue;

                if (originalEntry.ExtraData == null || string.IsNullOrEmpty(originalEntry.ExtraData.CasPath))
                    continue;

                var casPath = originalEntry.ExtraData.CasPath;
                if (!casToMods.ContainsKey(casPath))
                    casToMods.Add(casPath, new List<ModdedFile>());

                casToMods[casPath].Add(new ModdedFile(mod.Value.Sha1, mod.Value.Name, false, mod.Value, originalEntry));

            }

            ProcessLegacyMods();

            if (cancellationToken.IsCancellationRequested)
                return null;

            return casToMods;
        }

        protected void ProcessLegacyMods()
        {
            List<Guid> ChunksToRemove = new List<Guid>();

            // -----------------------------------------------------------
            // process modified legacy chunks and make live changes
            if (ModExecuter.modifiedLegacy.Count > 0)
            {
                // -----------------------------------------------------------
                // Only load cache when required
                //if (AssetManager.Instance == null)
                //{
                //    CacheManager buildCache = new CacheManager();
                //    buildCache.LoadData(ProfileManager.ProfileName, ModExecuter.GamePath, ModExecuter.Logger, false, true);
                //}

                ModExecuter.Logger.Log($"Legacy :: {ModExecuter.modifiedLegacy.Count} Legacy files found. Modifying associated chunks");

                Dictionary<string, byte[]> legacyData = ModExecuter.modifiedLegacy.ToDictionary(x => x.Key, x => x.Value.ModifiedEntry.Data);
                var countLegacyChunksModified = 0;


                if (AssetManager.Instance.GetLegacyAssetManager() == null)
                    AssetManager.Instance.RegisterLegacyAssetManager();

                var chunkFileManager = AssetManager.Instance.GetLegacyAssetManager() as ChunkFileManager2022;
                if (chunkFileManager == null)
                    return;

                // Initialize the LFM
                chunkFileManager.Initialize(ModExecuter.Logger);

                chunkFileManager.ModifyAssets(legacyData, true);

                var modifiedLegacyChunks = chunkFileManager.ModifiedChunks.Distinct();
                foreach (var modLegChunk in modifiedLegacyChunks)
                {
                    if (ModExecuter.ModifiedChunks.ContainsKey(modLegChunk.Id))
                        ModExecuter.ModifiedChunks.Remove(modLegChunk.Id);

                    ModExecuter.ModifiedChunks.Add(modLegChunk.Id, modLegChunk);
                    countLegacyChunksModified++;
                }

                foreach (var chunk in modifiedLegacyChunks)
                {
                    if (ModExecuter.archiveData.ContainsKey(chunk.ModifiedEntry.Sha1))
                        ModExecuter.archiveData.Remove(chunk.ModifiedEntry.Sha1);

                    ModExecuter.archiveData.Add(chunk.ModifiedEntry.Sha1, new ArchiveInfo() { Data = chunk.ModifiedEntry.Data });
                }
                ModExecuter.Logger.Log($"Legacy :: Modified {countLegacyChunksModified} associated chunks");
            }
        }

        public override bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            //logger.Log($"Compiler {fs.BasePath}");

            ModExecuter = modExecuter;
            return false;
        }

        public override bool Cleanup(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return false;
        }

        protected virtual bool WriteChangesToSuperBundle(DbObject origDbo, NativeWriter writer, AssetEntry assetEntry)
        {
            if (assetEntry is EbxAssetEntry)
                return WriteChangesToSuperBundleEbx(origDbo, writer, assetEntry);
            else if (assetEntry is ResAssetEntry)
                return WriteChangesToSuperBundleRes(origDbo, writer, assetEntry);
            else if (assetEntry is ChunkAssetEntry)
                return WriteChangesToSuperBundleChunk(origDbo, writer, assetEntry);

            //switch(assetBundle.Key.GetType().Name)
            //{
            //    case "EbxAssetEntry":
            //        return WriteChangesToSuperBundleEbx(origDbo, writer, assetBundle);
            //    case "ResAssetEntry":
            //        return WriteChangesToSuperBundleRes(origDbo, writer, assetBundle);
            //    case "ChunkAssetEntry":
            //        return WriteChangesToSuperBundleChunk(origDbo, writer, assetBundle);
            //}

            return false;
        }

        protected virtual bool WriteChangesToSuperBundleEbx(DbObject origDbo, NativeWriter writer, AssetEntry assetEntry)
        {
#if DEBUG
            if (origDbo["name"].ToString().EndsWith("head_202231_0_0_mesh"))
            {

            }
#endif

            if (origDbo == null)
                throw new ArgumentNullException(nameof(origDbo));

            if (!ModExecuter.modifiedEbx.ContainsKey(assetEntry.Name))
                return false;

            var originalSizePosition = 
                origDbo.HasValue("SB_OriginalSize_Position") ? origDbo.GetValue<int>("SB_OriginalSize_Position")
                : origDbo.HasValue("SBOSizePos") ? origDbo.GetValue<int>("SBOSizePos")
                : 0;

            if (originalSizePosition <= 0 || originalSizePosition > writer.Length)
                return false;

            var originalSizeOfData = assetEntry.OriginalSize;
            if(originalSizeOfData == 0)
                return false;

            if (!origDbo.HasValue("SB_Sha1_Position"))
                return false;

            writer.Position = originalSizePosition;
            writer.Write((uint)originalSizeOfData, Endian.Little);

            if (assetEntry.Sha1 != Sha1.Zero)
            {
                writer.Position = origDbo.GetValue<long>("SB_Sha1_Position");

                if (writer.Position < 0 || writer.Position > writer.Length)
                    return false;

                writer.Write(assetEntry.Sha1);
            }

            return true;
        }

        protected virtual bool WriteChangesToSuperBundleRes(DbObject origDbo, NativeWriter writer, AssetEntry assetEntry)
        {
#if DEBUG
            if (origDbo["name"].ToString().EndsWith("head_202231_0_0_mesh"))
            {

            }
#endif


            if (origDbo == null)
                throw new ArgumentNullException(nameof(origDbo));

            if (!ModExecuter.modifiedRes.ContainsKey(assetEntry.Name))
            {
                AssetEntryErrorList.Add(assetEntry.Name, "[RES] No Modified Res found");
                return false;
            }

            if (!origDbo.HasValue("SB_ResMeta_Position") || origDbo.GetValue<int>("SB_ResMeta_Position") == 0)
            {
                AssetEntryErrorList.Add(assetEntry.Name, "[RES] No RES Meta position found");
                return false;
            }

            var resMetaPosition = origDbo.GetValue<int>("SB_ResMeta_Position");
            var originalSizePosition = 
                origDbo.HasValue("SB_OriginalSize_Position") ? origDbo.GetValue<int>("SB_OriginalSize_Position")
                : origDbo.HasValue("SBOSizePos") ? origDbo.GetValue<int>("SBOSizePos")
                : 0;
            long? sha1Position = origDbo.HasValue("SB_Sha1_Position") ? origDbo.GetValue<long>("SB_Sha1_Position") : null;

            if (originalSizePosition <= 0)
            {
                AssetEntryErrorList.Add(assetEntry.Name, "[RES] No Original Size position found");
                return false;
            }

            var originalSizeOfData = assetEntry.OriginalSize;
            if (originalSizeOfData <= 0)
            {
                AssetEntryErrorList.Add(assetEntry.Name, "[RES] Original Size of Data is 0");
                return false;
            }

            if (!origDbo.HasValue("SB_Sha1_Position"))
            {
                AssetEntryErrorList.Add(assetEntry.Name, "[RES] SB_Sha1_Position found");
                return false;
            }

            var modifiedResource = ModExecuter.modifiedRes[assetEntry.Name];

            //if (assetBundle.Key.Type != "MeshSet")
            //{
                writer.BaseStream.Position = resMetaPosition;
                writer.WriteBytes(modifiedResource.ResMeta);
            //}

            //if (assetBundle.Key.Type != "MeshSet")
            //{
                if (modifiedResource.ResRid != 0)
                {
                    writer.BaseStream.Position = origDbo.GetValue<int>("SB_ReRid_Position");
                    writer.WriteULong(modifiedResource.ResRid);
                }
            //}

            //if (assetBundle.Key.Type != "MeshSet")
            //{
                writer.Position = originalSizePosition;
                writer.Write((uint)originalSizeOfData, Endian.Little);
            //}

            //if (assetBundle.Key.Type != "MeshSet")
            //{ 
                if (sha1Position.HasValue && modifiedResource.Sha1 != Sha1.Zero)
                {
                    writer.Position = sha1Position.Value;
                    writer.Write(modifiedResource.Sha1);
                }
            //}

            return true;
        }

        protected virtual bool WriteChangesToSuperBundleChunk(DbObject origChunkDbo, NativeWriter writer, AssetEntry assetEntry)
        {
            if (origChunkDbo == null)
                throw new ArgumentNullException(nameof(origChunkDbo));

            if (!Guid.TryParse(assetEntry.Name, out Guid id))
            {
                AssetEntryErrorList.Add(assetEntry.Name, "[CHUNK] Could not parse Chunk Guid");
                return false;
            }

            if (!ModExecuter.ModifiedChunks.ContainsKey(id))
            {
                AssetEntryErrorList.Add(assetEntry.Name, "[CHUNK] No Modified Chunk found");
                return false;
            }

            writer.BaseStream.Position = origChunkDbo.GetValue<int>("SB_LogicalOffset_Position");
            writer.Write(ModExecuter.ModifiedChunks[id].LogicalOffset);

            var originalSizeOfData = assetEntry.OriginalSize;

            if (!origChunkDbo.HasValue("SB_OriginalSize_Position"))
            {
                AssetEntryErrorList.Add(assetEntry.Name, "[CHUNK] No SB_OriginalSize_Position found");
                return false;
            }

            writer.Position = origChunkDbo.GetValue<long>("SB_OriginalSize_Position");
            writer.Write((uint)originalSizeOfData, Endian.Little);

            if (origChunkDbo.HasValue("SB_Sha1_Position") && assetEntry.Sha1 != Sha1.Zero)
            {
                writer.Position = origChunkDbo.GetValue<long>("SB_Sha1_Position");
                writer.Write(assetEntry.Sha1);
            }

            return true;
        }


        public List<Guid> ProcessedChunks = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directory">native_patch or native_data?</param>
        /// <returns>List of Modded Chunk Ids</returns>
        protected virtual List<Guid> ModifyTOCChunks(string directory = "native_patch")
        {
            List<Guid> result = new List<Guid>();

            int sbIndex = -1;
            foreach (var catalogInfo in FileSystem.Instance.EnumerateCatalogInfos())
            {
                foreach (
                    string sbKey in catalogInfo.SuperBundles.Keys
                    //.Where(x => x.Contains("globals", StringComparison.OrdinalIgnoreCase))
                    )
                {
                    sbIndex++;
                    string tocFileSbKey = sbKey;
                    if (catalogInfo.SuperBundles[sbKey])
                    {
                        tocFileSbKey = sbKey.Replace("win32", catalogInfo.Name);
                    }

                    // Only handle Legacy stuff right now
                    //if (!tocFile.Contains("globals", StringComparison.OrdinalIgnoreCase))
                    //{
                    //    continue;
                    //}

                    var pathToTOCFileRAW = $"{directory}/{tocFileSbKey}.toc";
                    //string location_toc_file = FileSystem.Instance.ResolvePath(pathToTOCFileRAW);

                    var pathToTOCFile = FileSystem.Instance.ResolvePath(pathToTOCFileRAW, ModExecutor.UseModData);

                    using TOCFile tocFileObj = new TOCFile(pathToTOCFileRAW, false, false, true, sbIndex, true);

                    // read the changed toc file in ModData
                    if (!tocFileObj.TocChunks.Any())
                        continue;

                    if (!ModExecuter.ModifiedChunks.Any(x =>
                        x.Value.Bundles.Contains(tocFileObj.ChunkDataBundleId)
                        || x.Value.Bundles.Contains(tocFileObj.ChunkDataBundleId_PreFMT2323)
                        || x.Value.Bundles.Contains(Fnv1a.HashString("TocChunks"))
                        || x.Value.Bundles.Contains(Fnv1a.HashStringByHashDepot("TocChunks"))
                        )
                        )
                        continue;

                    var patch = true;
                    var catalog = tocFileObj.TocChunks.Max(x => x.ExtraData.Catalog.Value);
                    if (!tocFileObj.TocChunks.Any(x => x.ExtraData.IsPatch))
                        patch = false;

                    var cas = tocFileObj.TocChunks.Where(x => x.ExtraData.Catalog == catalog).Max(x => x.ExtraData.Cas.Value);

                    var newCas = cas;
                    //var nextCasPath = GetNextCasInCatalog(catalogInfo, cas, patch, out int newCas);
                    var nextCasPath = FileSystem.Instance.ResolvePath(FileSystem.Instance.GetFilePath(catalog, cas, patch), ModExecutor.UseModData);
                    if (!File.Exists(nextCasPath))
                    {
                        nextCasPath = FileSystem.Instance.ResolvePath(FileSystem.Instance.GetFilePath(catalog, cas, false), ModExecutor.UseModData);
                        patch = false;
                    }
                    if (string.IsNullOrEmpty(nextCasPath))
                    {
                        Debug.WriteLine("Error finding nextCasPath in BaseAssetCompiler.ModifyTOCChunks!");
                        continue;
                    }

                    using (NativeWriter nw_cas = new NativeWriter(new FileStream(nextCasPath, FileMode.OpenOrCreate)))
                    {
                        using (NativeWriter nw_toc = new NativeWriter(new FileStream(pathToTOCFile, FileMode.Open)))
                        {
                            foreach (var modChunk in ModExecuter.ModifiedChunks)
                            {
                                if (tocFileObj.TocChunkGuids.Contains(modChunk.Key))
                                {
                                    var chunkIndex = tocFileObj.TocChunks.FindIndex(x => x.Id == modChunk.Key
                                        && modChunk.Value.ModifiedEntry != null
                                        );
                                    if (chunkIndex != -1)
                                    {
                                        //var data = parent.archiveData[modChunk.Value.Sha1].Data;
                                        byte[] data = null;
                                        if (ModExecuter.archiveData.ContainsKey(modChunk.Value.ModifiedEntry.Sha1))
                                            data = ModExecuter.archiveData[modChunk.Value.ModifiedEntry.Sha1].Data;

                                        if (modChunk.Value.ModifiedEntry != null && modChunk.Value.ModifiedEntry.Data != null)
                                            data = modChunk.Value.ModifiedEntry.Data;

                                        if (data == null)
                                            continue;

                                        var chunkGuid = tocFileObj.TocChunkGuids[chunkIndex];
                                        if (ProcessedChunks.Contains(chunkGuid))
                                            continue;

                                        var chunk = tocFileObj.TocChunks[chunkIndex];

                                        nw_cas.Position = nw_cas.Length;
                                        var newPosition = nw_cas.Position;
                                        nw_cas.WriteBytes(data);
                                        modChunk.Value.Size = data.Length;
                                        modChunk.Value.ExtraData = new AssetExtraData()
                                        {
                                            DataOffset = (uint)newPosition,
                                            Cas = newCas,
                                            Catalog = catalog,
                                            IsPatch = patch,
                                        };

                                        nw_toc.Position = tocFileObj.TocChunkPatchPositions[chunkGuid];
                                        nw_toc.Write(Convert.ToByte(patch ? 1 : 0));
                                        nw_toc.Write(Convert.ToByte(catalog));
                                        nw_toc.Write(Convert.ToByte(newCas));

                                        nw_toc.Position = chunk.SB_CAS_Offset_Position;
                                        nw_toc.Write((uint)newPosition, Endian.Big);

                                        nw_toc.Position = chunk.SB_CAS_Size_Position;
                                        nw_toc.Write((uint)data.Length, Endian.Big);
                                        FileLogger.WriteLine($"Written TOC Chunk {chunkGuid} to {nextCasPath}");
                                        result.Add(chunkGuid);
                                        ProcessedChunks.Add(chunkGuid);
                                    }
                                }

                            }
                        }
                    }

                    TOCFile.RebuildTOCSignatureOnly(pathToTOCFile);
                }
            }

            if (directory == "native_patch")
                result.AddRange(ModifyTOCChunks("native_data"));

            return result;
        }


        /// <summary>
        /// Writes mod data to the CAS files the Asset Entry originally belongs to.
        /// </summary>
        /// <param name="dictOfModsToCas"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        //protected Dictionary<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> WriteNewDataToCasFiles(Dictionary<string, List<ModdedFile>> dictOfModsToCas)
        protected List<AssetEntry> WriteNewDataToCasFiles(Dictionary<string, List<ModdedFile>> dictOfModsToCas)
        {
            if (dictOfModsToCas == null || dictOfModsToCas.Count == 0)
                return null;

            if (ErrorCounts.Count > 0)
            {
                if (ErrorCounts[ModType.EBX] > 0)
                    ModExecuter.Logger.Log("EBX ERRORS:: " + ErrorCounts[ModType.EBX]);
                if (ErrorCounts[ModType.RES] > 0)
                    ModExecuter.Logger.Log("RES ERRORS:: " + ErrorCounts[ModType.RES]);
                if (ErrorCounts[ModType.CHUNK] > 0)
                    ModExecuter.Logger.Log("Chunk ERRORS:: " + ErrorCounts[ModType.CHUNK]);
            }

            Dictionary<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> EntriesToNewPosition = new Dictionary<AssetEntry, (long, int, int, FMT.FileTools.Sha1)>();

            foreach (var item in dictOfModsToCas)
            {


                string casPath = FileSystem.Instance.ResolvePath(item.Key, ModExecutor.UseModData);


                if (ModExecutor.UseModData && !casPath.Contains("ModData"))
                {
                    throw new Exception($"WRONG CAS PATH GIVEN! {casPath}");
                }

                if (!ModExecutor.UseModData)
                {
                    casPath = casPath.Replace("ModData\\", "", StringComparison.OrdinalIgnoreCase);
                }

                if (ModExecuter.UseVerboseLogging)
                    FileLogger.WriteLine($"Modifying CAS file - {casPath}");

                Debug.WriteLine($"Modifying CAS file - {casPath}");

                using (NativeWriter nwCas = new NativeWriter(new FileStream(casPath, FileMode.Open)))
                {
                    foreach (var modItem in item.Value.OrderBy(x => x.NamePath))
                    {
                        nwCas.Position = nwCas.Length;
                        byte[] data = new byte[0];
                        AssetEntry originalEntry = modItem.OriginalEntry;
                        if (originalEntry == null)
                            continue;

                        if (originalEntry != null && ModExecuter.archiveData.ContainsKey(modItem.Sha1))
                        {
                            data = ModExecuter.archiveData[modItem.Sha1].Data;
                        }
                        else
                        {
                            //parent.Logger.LogError($"Unable to find original archive data for {modItem.NamePath}");
                            continue;
                        }

                        if (data.Length == 0)
                        {
                            ModExecuter.Logger.LogError($"Unable to find any data for {modItem.NamePath}");
                            continue;
                        }

                        AssetEntry modifiedAsset = null;
                        
                        switch (modItem.ModType)
                        {
                            case ModType.EBX:
                                modifiedAsset = ModExecuter.modifiedEbx[modItem.NamePath];
                                break;
                            case ModType.RES:
                                modifiedAsset = ModExecuter.modifiedRes[modItem.NamePath];
                                break;
                            case ModType.CHUNK:
                                modifiedAsset = ModExecuter.ModifiedChunks[Guid.Parse(modItem.NamePath)];
                                break;
                        }

                        if (modifiedAsset == null)
                            continue;

                        var origSize = 0;
                       

                        if (modifiedAsset is ChunkAssetEntry)
                        {
                            var chunkModAsset = modifiedAsset as ChunkAssetEntry;

                            //if (chunkModAsset.IsTocChunk)
                            //    continue;

                            if (chunkModAsset.ModifiedEntry != null && chunkModAsset.ModifiedEntry.IsLegacyFile)
                            {
                                FileLogger.WriteLine($"Excluding {modItem.ModType} {modItem.NamePath} from WriteNewDataToCasFile as its a Legacy File");
                                continue;
                            }
                        }

                        origSize = Convert.ToInt32(modifiedAsset.OriginalSize);

                        if (origSize == 0)
                        {
                            if (modifiedAsset is ChunkAssetEntry cae && cae.LogicalSize > 0)
                            {
                                origSize = (int)cae.LogicalSize;
                                modifiedAsset.OriginalSize = origSize;
                            }
                            else
                            {
                                //parent.Logger.LogWarning($"OriginalSize is missing or 0 on {modItem.NamePath}, attempting calculation by reading it.");
                                using (var stream = new MemoryStream(data))
                                {
                                    var out_data = new CasReader(new MemoryStream(data)).Read();
                                    origSize = out_data.Length;
                                }

                            }
                        }

                        if (string.IsNullOrEmpty(originalEntry.TOCFileLocation))
                            continue;

                        var positionOfData = nwCas.Position;
                        // write the new data to end of the file (this should be fine)
                        nwCas.Write(data);

                        if (ModExecuter.UseVerboseLogging)
                            FileLogger.WriteLine($"Written {modItem.ModType} {modItem.NamePath} to {casPath}");

                        if (EntriesToNewPosition.ContainsKey(originalEntry))
                        {
                            FileLogger.WriteLine($"Excluding {modItem.ModType} {modItem.NamePath} from WriteNewDataToCasFile as it already been processed");
                        }
                        else
                        {
                            EntriesToNewPosition.Add(originalEntry, (positionOfData, data.Length, origSize, modItem.Sha1));

                            // Update Modified Asset with Information / Data
                            if (modifiedAsset.ExtraData == null)
                                modifiedAsset.ExtraData = new AssetExtraData();

                            modifiedAsset.SBFileLocation = originalEntry.SBFileLocation;
                            modifiedAsset.TOCFileLocation = originalEntry.TOCFileLocation;
                            modifiedAsset.ExtraData.DataOffset = Convert.ToUInt32(positionOfData);
                            modifiedAsset.Size = data.Length;
                            modifiedAsset.OriginalSize = origSize;
                            modifiedAsset.Sha1 = modItem.Sha1;
                            //modifiedAsset.Bundles = modifiedAsset.Bundles.Distinct().ToList();

                            switch (modItem.ModType)
                            {
                                case ModType.EBX:
                                    ModExecuter.modifiedEbx[modItem.NamePath] = (EbxAssetEntry)modifiedAsset;
                                    break;
                                case ModType.RES:
                                    ModExecuter.modifiedRes[modItem.NamePath] = (ResAssetEntry)modifiedAsset;
                                    break;
                                case ModType.CHUNK:
                                    ModExecuter.ModifiedChunks[Guid.Parse(modItem.NamePath)] = (ChunkAssetEntry)modifiedAsset;
                                    break;
                            }
                        }
                    }

                }
            }

            return ModExecuter.ModifiedAssets.Select(x => x.Value).ToList();
            //return EntriesToNewPosition;
        }

        protected virtual bool WriteNewDataChangesToSuperBundles(ref List<AssetEntry> listOfModifiedAssets, string directory = "native_patch")
        {
            if (listOfModifiedAssets == null)
            {
                ModExecuter.Logger.LogError($"Unable to find any entries to process");
                return false;
            }

            if (!listOfModifiedAssets.Any())
                return true;

            // ------------------------------------------------------------------------------
            // Step 1. Discovery phase. Find the Edited Bundles and what TOC/SB they affect
            //
            // ---------
            // Step 1a. Cache has the SBFileLocation or TOCFileLocation
            var editedBundles = listOfModifiedAssets.SelectMany(x => x.Bundles).Distinct();
            var groupedByTOCSB = new Dictionary<string, List<AssetEntry>>();
            if (listOfModifiedAssets.Any(
                x => !string.IsNullOrEmpty(x.SBFileLocation)
                || !string.IsNullOrEmpty(x.TOCFileLocation)
                ))
            {
                foreach(var item in listOfModifiedAssets)
                {
                    var tocPath = !string.IsNullOrEmpty(item.SBFileLocation) ? item.SBFileLocation : item.TOCFileLocation;
                    if (!tocPath.Contains(directory))
                        continue;

                    if (!groupedByTOCSB.ContainsKey(tocPath))
                        groupedByTOCSB.Add(tocPath, new List<AssetEntry>());

                    groupedByTOCSB[tocPath].Add(item);
                }
                // Group By SBFileLocation or TOCFileLocation
                //foreach(var grp in listOfModifiedAssets
                //    .Where(x => !string.IsNullOrEmpty(x.SBFileLocation) ? x.SBFileLocation.Contains(directory) : x.TOCFileLocation.Contains(directory))
                //    .GroupBy(x => !string.IsNullOrEmpty(x.SBFileLocation) ? x.SBFileLocation : x.TOCFileLocation).ToArray())
                //{
                //    if (!groupedByTOCSB.ContainsKey(grp.Key))
                //        groupedByTOCSB.Add(grp.Key, new List<AssetEntry>());
                //}
                //groupedByTOCSB = listOfModifiedAssets
                //    .Where(x => !string.IsNullOrEmpty(x.SBFileLocation) ? x.SBFileLocation.Contains(directory) : x.TOCFileLocation.Contains(directory))
                //    .GroupBy(x => !string.IsNullOrEmpty(x.SBFileLocation) ? x.SBFileLocation : x.TOCFileLocation)
                //    .ToDictionary(x => x, x => x);

            }

            var groupedByTOCSBCount = groupedByTOCSB.Values.Sum(y => y.Count);
            var doStep1b = false;// groupedByTOCSBCount != EntriesToNewPosition.Count;

            // ---------
            // Step 1b. Discover via Bundle Indexes
            if (doStep1b)
            {
                int sbIndex = -1;
                foreach (var catalogInfo in FileSystem.Instance.EnumerateCatalogInfos())
                {
                    foreach (
                        string sbKey in catalogInfo.SuperBundles.Keys
                        )
                    {
                        sbIndex++;
                        string tocFileKey = sbKey;
                        if (catalogInfo.SuperBundles[sbKey])
                        {
                            tocFileKey = sbKey.Replace("win32", catalogInfo.Name);
                        }

                        var nativePathToTOCFile = $"{directory}/{tocFileKey}.toc";
                        var actualPathToTOCFile = FileSystem.Instance.ResolvePath(nativePathToTOCFile, ModExecutor.UseModData);
                        using TOCFile tocFile = new TOCFile(nativePathToTOCFile, false, false, true, sbIndex, true);
#if DEBUG
                        if (nativePathToTOCFile.Contains("global"))
                        {

                        }
#endif

                        var hashedEntries = tocFile.BundleEntries.Select(x => Fnv1a.HashString(x.Name));
                        if (!hashedEntries.Any())
                            continue;

                        if (hashedEntries.Any(x => editedBundles.Contains(x)))
                        {
                            if (!groupedByTOCSB.ContainsKey(nativePathToTOCFile))
                                groupedByTOCSB.Add(nativePathToTOCFile, new List<AssetEntry>());

                            //    var editedBundleEntries = listOfModifiedAssets.Where(x => x.Bundles.Any(y => hashedEntries.Contains(y))).ToArray();

                            //    foreach (var item in editedBundleEntries)
                            //    {
                            //        if (!groupedByTOCSB[nativePathToTOCFile].Any(x => x == item))
                            //            groupedByTOCSB[nativePathToTOCFile].Add(item);
                            //    }

                        }
                    }
                }

                groupedByTOCSBCount = groupedByTOCSB.Values.Sum(y => y.Count);
            }


            if (!groupedByTOCSB.Any())
                return true;

            List<Task> tasks = new List<Task>();

            var assetBundleToCAS = new Dictionary<string, List<(AssetEntry, DbObject)>>();

            // ------------------------------------------------------------------------------
            // Step 2. Apply bundle changes to TOC Files
            //
            foreach (var tocGroup in groupedByTOCSB)
            {
                var tocPath = tocGroup.Key;
                if (string.IsNullOrEmpty(tocPath))
                    continue;

                //tasks.Add(Task.Run(() =>
                //{
                //using TOCFile tocFile = new TOCFile(tocPath, false, false, true, sbIndex, false);
                using TOCFile tocFile = new TOCFile(tocPath, false, false, true);
                {
                    DbObject dboOriginal = tocFile.TOCObjects;
                    if (dboOriginal == null)
                        continue;


                    ModExecuter.Logger.Log($"Processing: {tocPath}");
                    var origEbxBundles = dboOriginal.List
                    .Where(x => ((DbObject)x).HasValue("ebx"))
                    .Select(x => ((DbObject)x).GetValue<DbObject>("ebx"))
                    .Where(x => x.List != null && x.List.Any(y => ModExecuter.modifiedEbx.ContainsKey(((DbObject)y).GetValue<string>("name"))))
                    .ToList();

                    var origResBundles = dboOriginal.List
                    .Where(x => ((DbObject)x).HasValue("res"))
                    .Select(x => ((DbObject)x).GetValue<DbObject>("res"))
                    .Where(x => x.List != null && x.List.Any(y => ModExecuter.modifiedRes.ContainsKey(((DbObject)y).GetValue<string>("name"))))
                    .ToList();

                    var origChunkBundles = dboOriginal.List
                    .Where(x => ((DbObject)x).HasValue("chunks"))
                    .Select(x => ((DbObject)x).GetValue<DbObject>("chunks"))
                    .Where(x => x.List != null && x.List.Any(y => ModExecuter.ModifiedChunks.ContainsKey(((DbObject)y).GetValue<Guid>("id"))))
                    .ToList();

                    var origTocChunks = tocFile.TocChunks
                    .Where(x => ModExecuter.ModifiedChunks.ContainsKey(x.Id))
                    .ToList();

                    if (origEbxBundles.Count == 0 && origResBundles.Count == 0 && origChunkBundles.Count == 0 && origTocChunks.Count == 0)
                        continue;
                    //return;

                    var resolvedTocPath = FileSystem.Instance.ResolvePath(tocPath, true);
                    using (NativeWriter nw_toc = new NativeWriter(new FileStream(resolvedTocPath, FileMode.Open)))
                    {
                        foreach (var assetBundle in tocGroup.Value)
                        {
                            DbObject origDbo = null;
                            var casPath = string.Empty;
                            if (assetBundle is EbxAssetEntry)
                            {
                                foreach (DbObject dbInBundle in origEbxBundles)
                                {
                                    origDbo = (DbObject)dbInBundle.List.SingleOrDefault(z => ((DbObject)z)["name"].ToString() == assetBundle.Name);
                                    if (origDbo != null)
                                        break;
                                }

                                if (origDbo != null)
                                {
                                    casPath = origDbo.GetValue<string>("ParentCASBundleLocation");
                                }
                            }

                            else if (assetBundle is ResAssetEntry)
                            {

                                foreach (DbObject dbInBundle in origResBundles)
                                {
                                    var count = dbInBundle.List.Count(z => ((DbObject)z)["name"].ToString() == assetBundle.Name);
                                    if (count > 1)
                                    {

                                    }

                                    origDbo = (DbObject)dbInBundle.List.SingleOrDefault(z => ((DbObject)z)["name"].ToString() == assetBundle.Name);
                                    if (origDbo != null)
                                        break;
                                }

                                if (origDbo != null)
                                {
                                    casPath = origDbo.GetValue<string>("CASFileLocation");
                                }
                            }

                            else if (assetBundle is ChunkAssetEntry)
                            {
                                foreach (DbObject dbInBundle in origChunkBundles)
                                {
                                    origDbo = (DbObject)dbInBundle.List.SingleOrDefault(z => ((DbObject)z)["id"].ToString() == assetBundle.Name);
                                    if (origDbo != null)
                                        break;
                                }
                                //origDbo = chunkB.SingleOrDefault() as DbObject;

                                if (origDbo != null)
                                {
                                    casPath = origDbo.GetValue<string>("ParentCASBundleLocation");
                                }
                            }

                            if (origDbo == null)
                            {
                                if(directory == "native_data") // If hasn't been found in patch or data, we have a problem
                                    FileLogger.WriteLine($"Unable to find Original DBO for asset {assetBundle.Name} in {tocGroup.Key}");


                                continue;
                                //throw new Exception($"Unable to find Original DBO for asset {assetBundle.Key.Name}");
                            }

                            if (string.IsNullOrEmpty(casPath) && directory == "native_data")
                                throw new Exception($"Unable to find Cas Path for asset {assetBundle.Name} in {tocGroup.Key}");

                            //if (origDbo != null && !string.IsNullOrEmpty(casPath))
                            //{
#if DEBUG
                                if (origDbo.HasValue("name") && origDbo["name"].ToString().EndsWith("head_202231_0_0_mesh"))
                                {

                                }
#endif

                            //var positionOfNewData = assetBundle.Value.Item1;
                            //var sizeOfData = assetBundle.Value.Item2;
                            //var originalSizeOfData = assetBundle.Value.Item3;
                            //var sha = assetBundle.Value.Item4;

                            var positionOfNewData = assetBundle.ExtraData.DataOffset;
                            var sizeOfData = assetBundle.Size;

                            long sb_cas_size_position = origDbo.GetValue<long>("TOCSizePosition");// assetBundle.Key.SB_CAS_Size_Position;
                                var sb_cas_offset_position = origDbo.GetValue<long>("TOCOffsetPosition");// assetBundle.Key.SB_CAS_Offset_Position;
                                nw_toc.BaseStream.Position = sb_cas_offset_position;
                                nw_toc.Write((uint)positionOfNewData, Endian.Big);
                                nw_toc.Write((uint)sizeOfData, Endian.Big);

                                if (!assetBundleToCAS.ContainsKey(casPath))
                                    assetBundleToCAS.Add(casPath, new List<(AssetEntry, DbObject)>());

                                assetBundleToCAS[casPath].Add((assetBundle, origDbo));
                            //}
                        }





                    }

                    using (var fsTocSig = new FileStream(resolvedTocPath, FileMode.Open))
                        TOCFile.RebuildTOCSignatureOnly(fsTocSig);

                    ModExecuter.Logger.Log($"Processing Complete: {tocPath}");
                    if (dboOriginal != null)
                    {
                        dboOriginal = null;
                    }

                    //GC.Collect();
                    //GC.WaitForPendingFinalizers();
                }


                //}));
            }
            // Wait for above tasks to complete
            Task.WaitAll(tasks.ToArray());
            //
            // ------------------------------------------------------------------------------


            // ------------------------------------------------------------------------------
            // Step 3. Apply bundle changes to CAS Files
            //
            //foreach (var tocGroup in groupedByTOCSB)
            //{
            foreach (var abtc in assetBundleToCAS)
            {
                var resolvedCasPath = FileSystem.Instance.ResolvePath(abtc.Key, ModExecutor.UseModData);
                using (var nwCas = new NativeWriter(new FileStream(resolvedCasPath, FileMode.Open)))
                {
                    //ModExecuter.Logger.Log($"Writing {abtc.Value.Count} assets to {resolvedCasPath}");
                    FileLogger.WriteLine($"Writing {abtc.Value.Count} assets to {resolvedCasPath}");
                    Stopwatch swSuperBundleWriting = Stopwatch.StartNew();
#if DEBUG
                    if(abtc.Value.Count > 1000)
                    {

                    }

                    if(abtc.Value.Count == 0)
                    {

                    }
#endif 

                    if (!abtc.Value.Any())
                    {

                    }

                    foreach (var assetEntry in abtc.Value)
                    {
                        if (WriteChangesToSuperBundle(assetEntry.Item2, nwCas, assetEntry.Item1))
                        {
                            listOfModifiedAssets.Remove(assetEntry.Item1);
                        }
                        else
                        {
                            FileLogger.WriteLine($"Unable to write {assetEntry.Item1.Name} to Super Bundle");
                        }
                    }

                    FileLogger.WriteLine($"Written {abtc.Value.Count} assets to {resolvedCasPath} in {swSuperBundleWriting.Elapsed}");

                }
            }
            //}

            return true;
        }

        public override bool PreCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public override bool PostCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public override bool RunGame(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public struct ModdedFile
        {
            public FMT.FileTools.Sha1 Sha1 { get; set; }
            public string NamePath { get; set; }
            public ModType ModType { get; set; }
            public bool IsAdded { get; set; }
            public AssetEntry ModEntry { get; set; }
            public AssetEntry OriginalEntry { get; set; }

            public ModdedFile(FMT.FileTools.Sha1 inSha1, string inNamePath, bool inAdded)
            {
                ModType = ModType.EBX;
                Sha1 = inSha1;
                NamePath = inNamePath;
                IsAdded = inAdded;
                OriginalEntry = null;
                ModEntry = null;
            }

            public ModdedFile(FMT.FileTools.Sha1 inSha1, string inNamePath, bool inAdded, AssetEntry inModEntry, AssetEntry inOrigEntry)
            {
                Sha1 = inSha1;
                NamePath = inNamePath;

                ModType = ModType.EBX;
                if (inOrigEntry is EbxAssetEntry)
                    ModType = ModType.EBX;
                else if (inOrigEntry is ResAssetEntry)
                    ModType = ModType.RES;
                else if (inOrigEntry is ChunkAssetEntry)
                    ModType = ModType.CHUNK;

                IsAdded = inAdded;
                OriginalEntry = inOrigEntry;
                ModEntry = inModEntry;
            }

            public ModdedFile(FMT.FileTools.Sha1 inSha1, string inNamePath, ModType inModType, bool inAdded)
            {
                Sha1 = inSha1;
                NamePath = inNamePath;
                ModType = inModType;
                IsAdded = inAdded;
                OriginalEntry = null;
                ModEntry = null;
            }

            public ModdedFile(FMT.FileTools.Sha1 inSha1, string inNamePath, ModType inModType, bool inAdded, AssetEntry inOrigEntry)
            {
                Sha1 = inSha1;
                NamePath = inNamePath;
                ModType = inModType;
                IsAdded = inAdded;
                OriginalEntry = inOrigEntry;
                ModEntry = null;
            }

            public override string ToString()
            {
                return $"[{ModType.ToString()}]({NamePath})";
            }

        }

        public enum ModType
        {
            EBX,
            RES,
            CHUNK
        }

    }



}
