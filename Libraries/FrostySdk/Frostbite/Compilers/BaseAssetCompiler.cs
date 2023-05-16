﻿using FMT.FileTools;
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
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FrostySdk.Frostbite.Compilers
{
    public abstract class BaseAssetCompiler : IAssetCompiler
    {
        public string ModDirectory { get; } = "ModData";
        public string PatchDirectory { get; } = "Patch";

        public Dictionary<ModType, int> ErrorCounts = new Dictionary<ModType, int>();

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

        protected Dictionary<string, List<ModdedFile>> GetModdedCasFiles()
        {
            // -----------------------------------------------------------
            // Only load cache when required
            if (AssetManager.Instance == null)
            {
                CacheManager cacheManager = new CacheManager();
                cacheManager.LoadData(ProfileManager.ProfileName, ModExecuter.GamePath, ModExecuter.Logger, false, true);
            }

            // ------ End of handling Legacy files ---------

            Dictionary<string, List<ModdedFile>> casToMods = new Dictionary<string, List<ModdedFile>>();
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
                if (AssetManager.Instance == null)
                {
                    CacheManager buildCache = new CacheManager();
                    buildCache.LoadData(ProfileManager.ProfileName, ModExecuter.GamePath, ModExecuter.Logger, false, true);
                }

                ModExecuter.Logger.Log($"Legacy :: {ModExecuter.modifiedLegacy.Count} Legacy files found. Modifying associated chunks");

                Dictionary<string, byte[]> legacyData = ModExecuter.modifiedLegacy.ToDictionary(x => x.Key, x => x.Value.ModifiedEntry.Data);
                var countLegacyChunksModified = 0;

                var legacyFileManager = AssetManager.Instance.GetLegacyAssetManager() as ChunkFileManager2022;
                if (legacyFileManager != null)
                {
                    legacyFileManager.ModifyAssets(legacyData, true);

                    var modifiedLegacyChunks = legacyFileManager.ModifiedChunks.Distinct();
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
        }

        public virtual bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            //logger.Log($"Compiler {fs.BasePath}");

            ModExecuter = modExecuter;
            return false;
        }

        public virtual bool Cleanup(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return false;
        }

        protected virtual bool WriteChangesToSuperBundle(DbObject origDbo, NativeWriter writer, KeyValuePair<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> assetBundle)
        {
            if (assetBundle.Key is EbxAssetEntry)
                return WriteChangesToSuperBundleEbx(origDbo, writer, assetBundle);
            else if (assetBundle.Key is ResAssetEntry)
                return WriteChangesToSuperBundleRes(origDbo, writer, assetBundle);
            else if (assetBundle.Key is ChunkAssetEntry)
                return WriteChangesToSuperBundleChunk(origDbo, writer, assetBundle);

            return false;
        }

        protected virtual bool WriteChangesToSuperBundleEbx(DbObject origDbo, NativeWriter writer, KeyValuePair<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> assetBundle)
        {
#if DEBUG
            if (origDbo["name"].ToString().EndsWith("head_202231_0_0_mesh"))
            {

            }
#endif

            if (origDbo == null)
                throw new ArgumentNullException(nameof(origDbo));

            if (!ModExecuter.modifiedEbx.ContainsKey(assetBundle.Key.Name))
                return false;

            var originalSizePosition = 
                origDbo.HasValue("SB_OriginalSize_Position") ? origDbo.GetValue<int>("SB_OriginalSize_Position")
                : origDbo.HasValue("SBOSizePos") ? origDbo.GetValue<int>("SBOSizePos")
                : 0;

            if (originalSizePosition == 0)
                return false;

            var originalSizeOfData = assetBundle.Value.Item3;
            if(originalSizeOfData == 0)
                return false;

            if (!origDbo.HasValue("SB_Sha1_Position"))
                return false;

            writer.Position = originalSizePosition;
            writer.Write((uint)originalSizeOfData, Endian.Little);

            if (assetBundle.Value.Item4 != Sha1.Zero)
            {
                writer.Position = origDbo.GetValue<long>("SB_Sha1_Position");
                writer.Write(assetBundle.Value.Item4);
            }

            return true;
        }

        protected virtual bool WriteChangesToSuperBundleRes(DbObject origDbo, NativeWriter writer, KeyValuePair<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> assetBundle)
        {
#if DEBUG
            if (origDbo["name"].ToString().EndsWith("head_202231_0_0_mesh"))
            {

            }
#endif


            if (origDbo == null)
                throw new ArgumentNullException(nameof(origDbo));

            if (!ModExecuter.modifiedRes.ContainsKey(assetBundle.Key.Name))
                return false;

            if (!origDbo.HasValue("SB_ResMeta_Position") || origDbo.GetValue<int>("SB_ResMeta_Position") == 0)
                return false;

            var resMetaPosition = origDbo.GetValue<int>("SB_ResMeta_Position");
            var originalSizePosition = 
                origDbo.HasValue("SB_OriginalSize_Position") ? origDbo.GetValue<int>("SB_OriginalSize_Position")
                : origDbo.HasValue("SBOSizePos") ? origDbo.GetValue<int>("SBOSizePos")
                : 0;
            long? sha1Position = origDbo.HasValue("SB_Sha1_Position") ? origDbo.GetValue<long>("SB_Sha1_Position") : null;

            if (originalSizePosition == 0)
                return false;

            var originalSizeOfData = assetBundle.Value.Item3;
            if (originalSizeOfData == 0)
                return false;

            if (!origDbo.HasValue("SB_Sha1_Position"))
                return false;

            var modifiedResource = ModExecuter.modifiedRes[assetBundle.Key.Name];

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

        protected virtual bool WriteChangesToSuperBundleChunk(DbObject origChunkDbo, NativeWriter writer, KeyValuePair<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> assetBundle)
        {
            if (origChunkDbo == null)
                throw new ArgumentNullException(nameof(origChunkDbo));

            if (!Guid.TryParse(assetBundle.Key.Name, out Guid id))
                return false;

            if (!ModExecuter.ModifiedChunks.ContainsKey(id))
                return false;

            writer.BaseStream.Position = origChunkDbo.GetValue<int>("SB_LogicalOffset_Position");
            writer.Write(ModExecuter.ModifiedChunks[id].LogicalOffset);

            var originalSizeOfData = assetBundle.Value.Item3;

            if (!origChunkDbo.HasValue("SB_OriginalSize_Position"))
                return false;

            writer.Position = origChunkDbo.GetValue<long>("SB_OriginalSize_Position");
            writer.Write((uint)originalSizeOfData, Endian.Little);

            if (origChunkDbo.HasValue("SB_Sha1_Position") && assetBundle.Value.Item4 != Sha1.Zero)
            {
                writer.Position = origChunkDbo.GetValue<long>("SB_Sha1_Position");
                writer.Write(assetBundle.Value.Item4);
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directory">native_patch or native_data?</param>
        /// <returns>List of Modded Chunk Ids</returns>
        public virtual IEnumerable<Guid> ModifyTOCChunks(string directory = "native_patch")
        {
            int sbIndex = -1;
            foreach (var catalogInfo in FileSystem.Instance.EnumerateCatalogInfos())
            {
                foreach (
                    string sbKey in catalogInfo.SuperBundles.Keys
                    .Where(x => x.Contains("globals", StringComparison.OrdinalIgnoreCase))
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
                                        //&& (modChunk.Value.ModifiedEntry.AddToTOCChunks || modChunk.Value.ModifiedEntry.AddToChunkBundle)
                                        );
                                    if (chunkIndex != -1)
                                    {
                                        //var data = parent.archiveData[modChunk.Value.Sha1].Data;
                                        byte[] data = null;
                                        if (ModExecuter.archiveData.ContainsKey(modChunk.Value.ModifiedEntry.Sha1))
                                            data = ModExecuter.archiveData[modChunk.Value.ModifiedEntry.Sha1].Data;

                                        if (data == null)
                                            continue;

                                        var chunkGuid = tocFileObj.TocChunkGuids[chunkIndex];

                                        var chunk = tocFileObj.TocChunks[chunkIndex];
                                        //DbObject dboChunk = tocFile2.TocChunkInfo[modChunk.Key];

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

                                        nw_toc.Position = tocFileObj.TocChunkPatchPositions[chunkGuid];// dboChunk.GetValue<long>("patchPosition");
                                        nw_toc.Write(Convert.ToByte(patch ? 1 : 0));
                                        nw_toc.Write(Convert.ToByte(catalog));
                                        nw_toc.Write(Convert.ToByte(newCas));

                                        nw_toc.Position = chunk.SB_CAS_Offset_Position;
                                        nw_toc.Write((uint)newPosition, Endian.Big);

                                        nw_toc.Position = chunk.SB_CAS_Size_Position;
                                        nw_toc.Write((uint)data.Length, Endian.Big);
                                        FileLogger.WriteLine($"Written TOC Chunk {chunkGuid} to {nextCasPath}");
                                        yield return chunkGuid;
                                    }
                                }

                                // Added / Duplicate chunk -- Does nothing at the moment
                                //if (modChunk.Value.ExtraData == null && tocFile == "win32/globalsfull")
                                //{
                                //    var data = ModExecuter.archiveData[modChunk.Value.Sha1].Data;
                                //    nw_cas.Position = nw_cas.Length;
                                //    var newPosition = nw_cas.Position;
                                //    //nw_cas.WriteBytes(data);
                                //    modChunk.Value.Size = data.Length;
                                //    modChunk.Value.ExtraData = new AssetExtraData()
                                //    {
                                //        DataOffset = (uint)newPosition,
                                //        Cas = newCas,
                                //        Catalog = catalog,
                                //        IsPatch = patch,
                                //    };
                                //    //tocSb.TOCFile.TocChunks.Add(modChunk.Value);
                                //}
                            }
                        }
                    }

                    //using (var fsToc = new FileStream(pathToTOCFile, FileMode.Open))
                    //{
                    //    tocSb.TOCFile.Write(fsToc);
                    //}
                    TOCFile.RebuildTOCSignatureOnly(pathToTOCFile);
                }
            }

            if (directory == "native_patch")
                ModifyTOCChunks("native_data");
        }


        /// <summary>
        /// Writes mod data to the CAS files the Asset Entry originally belongs to.
        /// </summary>
        /// <param name="dictOfModsToCas"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected Dictionary<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> WriteNewDataToCasFiles(Dictionary<string, List<ModdedFile>> dictOfModsToCas)
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
                        FileLogger.WriteLine($"Written {modItem.ModType} {modItem.NamePath} to {casPath}");

                        if (EntriesToNewPosition.ContainsKey(originalEntry))
                        {
                            FileLogger.WriteLine($"Excluding {modItem.ModType} {modItem.NamePath} from WriteNewDataToCasFile as it already been processed");
                        }
                        else
                        {
                            EntriesToNewPosition.Add(originalEntry, (positionOfData, data.Length, origSize, modItem.Sha1));
                        }
                    }

                }
            }

            return EntriesToNewPosition;
        }


        protected bool WriteNewDataChangesToSuperBundles(ref Dictionary<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> EntriesToNewPosition, string directory = "native_patch")
        {
            if (EntriesToNewPosition == null)
            {
                ModExecuter.Logger.LogError($"Unable to find any entries to process");
                return false;
            }

            if (!EntriesToNewPosition.Any())
                return true;

            // ------------------------------------------------------------------------------
            // Step 1. Discovery phase. Find the Edited Bundles and what TOC/SB they affect
            //
            // ---------
            // Step 1a. Cache has the SBFileLocation or TOCFileLocation
            var editedBundles = EntriesToNewPosition.SelectMany(x => x.Key.Bundles).Distinct();
            var groupedByTOCSB = new Dictionary<string, List<KeyValuePair<AssetEntry, (long, int, int, FMT.FileTools.Sha1)>>>();
            if(EntriesToNewPosition.Any(
                x => !string.IsNullOrEmpty(x.Key.SBFileLocation)
                || !string.IsNullOrEmpty(x.Key.TOCFileLocation)
                ))
            {
                // Group By SBFileLocation or TOCFileLocation
                groupedByTOCSB = EntriesToNewPosition
                    .Where(x => !string.IsNullOrEmpty(x.Key.SBFileLocation) ? x.Key.SBFileLocation.Contains(directory) : x.Key.TOCFileLocation.Contains(directory))
                    .GroupBy(x => !string.IsNullOrEmpty(x.Key.SBFileLocation) ? x.Key.SBFileLocation : x.Key.TOCFileLocation)
                    .ToDictionary(x => x.Key, x => x.ToList());


            }

            var groupedByTOCSBCount = groupedByTOCSB.Values.Sum(y => y.Count);
            var doStep1b = true;// groupedByTOCSBCount != EntriesToNewPosition.Count;

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
                                groupedByTOCSB.Add(nativePathToTOCFile, new List<KeyValuePair<AssetEntry, (long, int, int, FMT.FileTools.Sha1)>>());

                            var editedBundleEntries = EntriesToNewPosition.Where(x => x.Key.Bundles.Any(y => hashedEntries.Contains(y))).ToArray();

                            foreach (var item in editedBundleEntries)
                            {
                                if (!groupedByTOCSB[nativePathToTOCFile].Any(x => x.Key == item.Key))
                                    groupedByTOCSB[nativePathToTOCFile].Add(item);
                            }

                        }
                    }
                }

                groupedByTOCSBCount = groupedByTOCSB.Values.Sum(y => y.Count);
            }
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
                            if (assetBundle.Key is EbxAssetEntry)
                            {
                                foreach (DbObject dbInBundle in origEbxBundles)
                                {
                                    origDbo = (DbObject)dbInBundle.List.SingleOrDefault(z => ((DbObject)z)["name"].ToString() == assetBundle.Key.Name);
                                    if (origDbo != null)
                                        break;
                                }

                                if (origDbo != null)
                                {
                                    casPath = origDbo.GetValue<string>("ParentCASBundleLocation");
                                }
                            }

                            else if (assetBundle.Key is ResAssetEntry)
                            {

                                foreach (DbObject dbInBundle in origResBundles)
                                {
                                    var count = dbInBundle.List.Count(z => ((DbObject)z)["name"].ToString() == assetBundle.Key.Name);
                                    if (count > 1)
                                    {

                                    }

                                    origDbo = (DbObject)dbInBundle.List.SingleOrDefault(z => ((DbObject)z)["name"].ToString() == assetBundle.Key.Name);
                                    if (origDbo != null)
                                        break;
                                }

                                if (origDbo != null)
                                {
                                    casPath = origDbo.GetValue<string>("CASFileLocation");
                                }
                            }

                            else if (assetBundle.Key is ChunkAssetEntry)
                            {
                                foreach (DbObject dbInBundle in origChunkBundles)
                                {
                                    origDbo = (DbObject)dbInBundle.List.SingleOrDefault(z => ((DbObject)z)["id"].ToString() == assetBundle.Key.Name);
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
                                    FileLogger.WriteLine($"Unable to find Original DBO for asset {assetBundle.Key.Name} in {tocGroup.Key}");


                                continue;
                                //throw new Exception($"Unable to find Original DBO for asset {assetBundle.Key.Name}");
                            }

                            if (string.IsNullOrEmpty(casPath) && directory == "native_data")
                                throw new Exception($"Unable to find Cas Path for asset {assetBundle.Key.Name} in {tocGroup.Key}");

                            //if (origDbo != null && !string.IsNullOrEmpty(casPath))
                            //{
#if DEBUG
                                if (origDbo.HasValue("name") && origDbo["name"].ToString().EndsWith("head_202231_0_0_mesh"))
                                {

                                }
#endif

                                var positionOfNewData = assetBundle.Value.Item1;
                                var sizeOfData = assetBundle.Value.Item2;
                                var originalSizeOfData = assetBundle.Value.Item3;
                                var sha = assetBundle.Value.Item4;

                                long sb_cas_size_position = origDbo.GetValue<long>("TOCSizePosition");// assetBundle.Key.SB_CAS_Size_Position;
                                var sb_cas_offset_position = origDbo.GetValue<long>("TOCOffsetPosition");// assetBundle.Key.SB_CAS_Offset_Position;
                                nw_toc.BaseStream.Position = sb_cas_offset_position;
                                nw_toc.Write((uint)positionOfNewData, Endian.Big);
                                nw_toc.Write((uint)sizeOfData, Endian.Big);

                                if (!assetBundleToCAS.ContainsKey(casPath))
                                    assetBundleToCAS.Add(casPath, new List<(AssetEntry, DbObject)>());

                                assetBundleToCAS[casPath].Add((assetBundle.Key, origDbo));
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
            {
                foreach (var abtc in assetBundleToCAS)
                {
                    var resolvedCasPath = FileSystem.Instance.ResolvePath(abtc.Key, ModExecutor.UseModData);
                    using (var nwCas = new NativeWriter(new FileStream(resolvedCasPath, FileMode.Open)))
                    {
                        ModExecuter.Logger.Log($"Writing {abtc.Value.Count} assets to {resolvedCasPath}");
                        FileLogger.WriteLine($"Writing {abtc.Value.Count} assets to {resolvedCasPath}");
                        foreach (var assetEntry in abtc.Value)
                        {


                            var assetBundles = EntriesToNewPosition.Where(x => x.Key.Equals(assetEntry.Item1)).ToArray();
                            if(assetBundles.Count() > 1)
                            {
                                throw new Exception($"There are too many AssetEntries of a similar type/name!");
                            }
                            var assetBundle = assetBundles.Single();
                            //var assetBundles = tocGroup.Value.Where(x => x.Key.Equals(assetEntry.Item1));
                            //foreach (var assetBundle in assetBundles)
                            //{
                            if (WriteChangesToSuperBundle(assetEntry.Item2, nwCas, assetBundle))
                            {
                                EntriesToNewPosition.Remove(assetEntry.Item1);
                            }

                                // Remove from EntriesToNewPosition to stop any false errors occuring 
                                //EntriesToNewPosition.Remove(assetEntry.Item1);
                            //}
                        }
                    }
                }
            }

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
