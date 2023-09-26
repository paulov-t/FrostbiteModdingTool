using FMT.FileTools;
using FMT.Logging;
using FrostySdk;
using FrostySdk.Frostbite.Compilers;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using ModdingSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FC24Plugin
{

    /// <summary>
    /// FIFA 23 Asset Compiler. Solid and works. Uses .cache file to determine what needs editing
    /// Linked to FIFA21BundleAction
    /// </summary>
    public class FC24AssetCompiler : Frostbite2022AssetCompiler, IAssetCompiler
    {
        /// <summary>
        /// This is run AFTER the compilation of the fbmod into resource files ready for the Actions to TOC/SB/CAS to be taken
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="logger"></param>
        /// <param name="frostyModExecuter">Frosty Mod Executer object</param>
        /// <returns></returns>
        public override bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            base.Compile(fs, logger, modExecuter);

            DateTime dtStarted = DateTime.Now;
            //if (!ProfileManager.IsFC24DataVersion())
            //{
            //    logger.Log("[ERROR] Wrong compiler used for Game");
            //    return false;
            //}

            bool result = false;
            ErrorCounts.Clear();
            ErrorCounts.Add(ModType.EBX, 0);
            ErrorCounts.Add(ModType.RES, 0);
            ErrorCounts.Add(ModType.CHUNK, 0);
            ModExecutor.UseModData = true;

            result = RunModDataCompiler(logger);

            logger.Log($"Compiler completed in {(DateTime.Now - dtStarted).ToString(@"mm\:ss")}");
            return result;
        }

        private bool RunModDataCompiler(ILogger logger)
        {
            if (!Directory.Exists(FileSystem.Instance.BasePath))
                throw new DirectoryNotFoundException($"Unable to find the correct base path directory of {FileSystem.Instance.BasePath}");

            Directory.CreateDirectory(FileSystem.Instance.BasePath + ModDirectory + "\\Data");
            Directory.CreateDirectory(FileSystem.Instance.BasePath + ModDirectory + "\\Patch");

            logger.Log("Copying files from Data to ModData/Data");
            CopyDataFolder(FileSystem.Instance.BasePath + "\\Data\\", FileSystem.Instance.BasePath + ModDirectory + "\\Data\\", logger);
            logger.Log("Copying files from Patch to ModData/Patch");
            CopyDataFolder(FileSystem.Instance.BasePath + "\\Patch\\", FileSystem.Instance.BasePath + ModDirectory + "\\Patch\\", logger);

            return Run();
        }


        private ModExecutor parent => ModExecuter;

        public bool GameWasPatched => parent.GameWasPatched;

        public List<ChunkAssetEntry> AddedChunks = new List<ChunkAssetEntry>();

        Dictionary<string, DbObject> SbToDbObject = new Dictionary<string, DbObject>();

        private void DeleteBakFiles(string path)
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.FullName.Contains(".bak"))
                {
                    fileInfo.Delete();
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                DeleteBakFiles(dir);
            }
        }

        public bool Run()
        {
            //parent.Logger.Log("Loading files to know what to change.");
            if (parent.modifiedEbx.Count == 0 && parent.modifiedRes.Count == 0 && parent.ModifiedChunks.Count == 0 && parent.modifiedLegacy.Count == 0)
                return true;

            //if (AssetManager.Instance == null)
            //{
            //    CacheManager buildCache = new CacheManager();
            //    buildCache.LoadData(ProfileManager.ProfileName, parent.GamePath, parent.Logger, false, true);
            //}

            if (!ModExecutor.UseModData && GameWasPatched)
            {
                DeleteBakFiles(parent.GamePath);
            }

            parent.Logger.Log("Retreiving list of Modified CAS Files.");
            var dictOfModsToCas = GetModdedCasFiles();
            if (dictOfModsToCas == null)
                return false;

            // Force delete of Live Tuning Updates
            if (dictOfModsToCas.Any(x => x.Value.Any(y => y.NamePath.Contains("gp_", StringComparison.OrdinalIgnoreCase))))
                parent.DeleteLiveUpdates = true;

            parent.Logger.Log("Modifying TOC Chunks.");
            var modifiedTocChunks = ModifyTOCChunks();

            var entriesToNewPosition = WriteNewDataToCasFiles(dictOfModsToCas);
            if (entriesToNewPosition == null || entriesToNewPosition.Count == 0)
                return true;

            bool result = WriteNewDataChangesToSuperBundles(ref entriesToNewPosition);
            result = WriteNewDataChangesToSuperBundles(ref entriesToNewPosition, "native_data");

            if (entriesToNewPosition.Count > 0)
            {
                var entriesErrorText = $"{entriesToNewPosition.Count} Entries were not written to TOC. Some parts of the mod may be removed.";
                FileLogger.WriteLine(entriesErrorText);
                parent.Logger.Log(entriesErrorText);
            }

            return result;

        }

        protected override List<Guid> ModifyTOCChunks(string directory = "native_patch")
        {
            List<Guid> result = new List<Guid>();

            if (!ModExecuter.ModifiedChunks.Any())
                return result;

            try
            {


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

                        using FC24TOCFile tocFileObj = new FC24TOCFile(pathToTOCFileRAW, false, false, true, sbIndex, true);

                        // read the changed toc file in ModData
                        if (!tocFileObj.TocChunks.Any())
                            continue;

                        if (!tocFileObj.TocChunks.Any(x => x != null && ModExecuter.ModifiedChunks.ContainsKey(x.Id)))
                            continue;

                        if (!ModExecuter.ModifiedChunks.Any(x =>
                            x.Value.Bundles.Contains(tocFileObj.ChunkDataBundleId)
                            || x.Value.Bundles.Contains(tocFileObj.ChunkDataBundleId_PreFMT2323)
                            || x.Value.Bundles.Contains(Fnv1a.HashString("TocChunks"))
                            || x.Value.Bundles.Contains(Fnv1a.HashStringByHashDepot("TocChunks"))
                            )
                            )
                            continue;

                        var tocChunks = tocFileObj.TocChunks.Where(x => x != null && x.ExtraData != null);

                        var patch = true;
                        var firstKey = ModExecuter.ModifiedChunks.Keys.First();
                        var instance = tocChunks.Where(x => x != null && ModExecuter.ModifiedChunks.ContainsKey(x.Id)).First();
                        var catalog = instance.ExtraData.Catalog.Value;
                        var cas = instance.ExtraData.Cas.Value;
                        var newCas = instance.ExtraData.Cas.Value;
                        patch = instance.ExtraData.IsPatch;

                        //var nextCasPath = GetNextCasInCatalog(catalogInfo, cas, patch, out int newCas);
                        var nextCasPath = FileSystem.Instance.ResolvePath(FileSystem.Instance.GetFilePath(catalog, cas, patch), ModExecutor.UseModData);
                        if (string.IsNullOrEmpty(nextCasPath))
                        {
                            throw new FileNotFoundException("Error finding nextCasPath in BaseAssetCompiler.ModifyTOCChunks!");
                        }

                        using (NativeWriter nw_cas = new NativeWriter(new FileStream(nextCasPath, FileMode.OpenOrCreate)))
                        {
                            using (NativeWriter nw_toc = new NativeWriter(new FileStream(pathToTOCFile, FileMode.Open)))
                            {
                                foreach (var modChunk in ModExecuter.ModifiedChunks)
                                {
                                    if (tocFileObj.TocChunkGuids.Contains(modChunk.Key))
                                    {
                                        var chunkIndex = tocFileObj.TocChunks.FindIndex(x =>
                                            x != null &&
                                            x.Id == modChunk.Key
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
                                            nw_toc.Write(Convert.ToUInt16(patch ? 1 : 0), Endian.Big);
                                            nw_toc.Write((int)AssetManager.Instance.FileSystem.CatalogObjects.ToArray()[catalog].PersistentIndex, Endian.Big);
                                            nw_toc.Write(Convert.ToUInt16(newCas), Endian.Big);

                                            nw_toc.Position = chunk.SB_CAS_Offset_Position;
                                            nw_toc.Write((uint)newPosition, Endian.Big);

                                            nw_toc.Position = chunk.SB_CAS_Size_Position;
                                            nw_toc.Write((uint)data.Length, Endian.Big);
                                            FileLogger.WriteLine($"Written TOC Chunk {chunkGuid} to {nextCasPath}");
                                            result.Add(chunkGuid);
                                            ProcessedChunks.Add(chunkGuid);
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

                        TOCFile.RebuildTOCSignatureOnly(pathToTOCFile);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            if (directory == "native_patch")
                result.AddRange(ModifyTOCChunks("native_data"));

            return result;
        }

        protected override bool WriteNewDataChangesToSuperBundles(ref List<AssetEntry> listOfModifiedAssets, string directory = "native_patch")
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
                foreach (var item in listOfModifiedAssets)
                {
                    var tocPath = !string.IsNullOrEmpty(item.SBFileLocation) ? item.SBFileLocation : item.TOCFileLocation;
                    if (tocPath == null)
                        continue;

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
                using FC24TOCFile tocFile = new FC24TOCFile(tocPath, false, false, true);
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
                    .Where(x => x != null && ModExecuter.ModifiedChunks.ContainsKey(x.Id))
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
                                    casPath = origDbo.GetValue<string>("ParentCASBundleLocation");
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
                                if (directory == "native_data") // If hasn't been found in patch or data, we have a problem
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
                        }





                    }

                    using (var fsTocSig = new FileStream(resolvedTocPath, FileMode.Open))
                        FC24TOCFile.RebuildTOCSignatureOnly(fsTocSig);

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
                    if (abtc.Value.Count > 1000)
                    {

                    }

                    if (abtc.Value.Count == 0)
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
    }



}
