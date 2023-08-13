using FMT.FileTools;
using FMT.Logging;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Frostbite.Compilers;
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

namespace FIFA21Plugin
{

    /// <summary>
    /// FIFA 23 Asset Compiler. Solid and works. Uses .cache file to determine what needs editing
    /// Linked to FIFA21BundleAction
    /// </summary>
    public class FIFA21AssetCompiler : Frostbite2022AssetCompiler, IAssetCompiler
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
            if (!ProfileManager.IsFIFA21DataVersion())
            {
                logger.Log("[ERROR] Wrong compiler used for Game");
                return false;
            }

            bool result = false;
            ErrorCounts.Clear();
            ErrorCounts.Add(ModType.EBX, 0);
            ErrorCounts.Add(ModType.RES, 0);
            ErrorCounts.Add(ModType.CHUNK, 0);
            ModExecutor.UseModData = true;

            //if (!FrostyModExecutor.UseModData)
            //{
            //    result = RunEADesktopCompiler(fs, logger, frostyModExecuter);
            //    return result;
            //}
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


        //}

        ///// <summary>
        ///// The actual builder/modifier of the files
        ///// </summary>
        //public class FIFA23BundleAction
        //{
        private ModExecutor parent => ModExecuter;

        //public static Dictionary<string, List<string>> CatalogCasFiles { get; } = new Dictionary<string, List<string>>();

        public bool GameWasPatched => parent.GameWasPatched;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inCatalogInfo"></param>
        /// <param name="inDoneEvent"></param>
        /// <param name="inParent"></param>
        //public FIFA21BundleAction(Catalog inCatalogInfo, FrostyModExecutor inParent)
        //{
        //    catalogInfo = inCatalogInfo;
        //    parent = inParent;
        //}

        //private readonly bool UseModData;

        //public FIFA23BundleAction(FrostyModExecutor inParent, bool useModData = true)
        //{
        //    parent = inParent;
        //    ErrorCounts.Add(ModType.EBX, 0);
        //    ErrorCounts.Add(ModType.RES, 0);
        //    ErrorCounts.Add(ModType.CHUNK, 0);
        //    UseModData = useModData;

        //    CatalogCasFiles.Clear();
        //    foreach (Catalog catalog in FileSystem.Instance.EnumerateCatalogInfos())
        //    {
        //        int casFileNumber = 1;
        //        List<string> casFileLocation = new List<string>();
        //        string path2 = Path.Combine(FileSystem.Instance.BasePath, "Patch", catalog.Name, $"cas_{casFileNumber:D2}.cas");
        //        while (File.Exists(path2))
        //        {
        //            casFileLocation.Add(path2);
        //            casFileNumber++;
        //            path2 = Path.Combine(FileSystem.Instance.BasePath, "Patch", catalog.Name, $"cas_{casFileNumber:D2}.cas");
        //        }
        //        CatalogCasFiles.Add(catalog.Name, casFileLocation);
        //    }

        //}






        public List<ChunkAssetEntry> AddedChunks = new List<ChunkAssetEntry>();

        /// <summary>
        /// 
        /// </summary>
        /// <returns>cas to Tuple of Sha1, Name, Type, IsAdded</returns>
        //private Dictionary<string, List<ModdedFile>> GetModdedCasFiles()
        //{
        //    // Handle Legacy first to generate modified chunks
        //    ProcessLegacyMods();
        //    // ------ End of handling Legacy files ---------

        //    Dictionary<string, List<ModdedFile>> casToMods = new Dictionary<string, List<ModdedFile>>();
        //    foreach(var mod in parent.ModifiedAssets)
        //    {
        //        AssetEntry originalEntry = null;
        //        if (mod.Value is EbxAssetEntry)
        //            originalEntry = AssetManager.Instance.GetEbxEntry(mod.Value.Name);
        //        else if (mod.Value is ResAssetEntry)
        //            originalEntry = AssetManager.Instance.GetResEntry(mod.Value.Name);
        //        else if (mod.Value is ChunkAssetEntry)
        //            originalEntry = AssetManager.Instance.GetChunkEntry(Guid.Parse(mod.Value.Name));

        //        if (originalEntry == null)
        //            continue;

        //        var casPath = originalEntry.ExtraData.CasPath;
        //        if (!casToMods.ContainsKey(casPath))
        //            casToMods.Add(casPath, new List<ModdedFile>());

        //        casToMods[casPath].Add(new ModdedFile(mod.Value.Sha1, mod.Value.Name, false, mod.Value, originalEntry));

        //    }

        //    return casToMods;
        //}

        //private void ProcessLegacyMods()
        //{
        //    List<Guid> ChunksToRemove = new List<Guid>();

        //    // -----------------------------------------------------------
        //    // process modified legacy chunks and make live changes
        //    if (parent.modifiedLegacy.Count > 0)
        //    {
        //        parent.Logger.Log($"Legacy :: {parent.modifiedLegacy.Count} Legacy files found. Modifying associated chunks");

        //        Dictionary<string, byte[]> legacyData = new Dictionary<string, byte[]>();
        //        var countLegacyChunksModified = 0;
        //        foreach (var modLegacy in parent.modifiedLegacy)
        //        {
        //            byte[] data = null;
        //            //if (modLegacy.Value.ModifiedEntry != null && modLegacy.Value.ModifiedEntry.Data != null)
        //            //    data = new CasReader(new MemoryStream(modLegacy.Value.ModifiedEntry.Data)).Read();
        //            //else 
        //            if (parent.archiveData.ContainsKey(modLegacy.Value.Sha1))
        //                data = parent.archiveData[modLegacy.Value.Sha1].Data;

        //            if (data != null)
        //            {
        //                legacyData.Add(modLegacy.Key, data);
        //            }
        //        }

        //        LegacyFileManager_FMTV2 legacyFileManager = AssetManager.Instance.GetLegacyAssetManager() as LegacyFileManager_FMTV2;
        //        if (legacyFileManager != null)
        //        {
        //            legacyFileManager.ModifyAssets(legacyData, true);

        //            var modifiedLegacyChunks = AssetManager.Instance.EnumerateChunks(true);
        //            foreach (var modLegChunk in modifiedLegacyChunks.Where(x => !parent.ModifiedChunks.ContainsKey(x.Id)))
        //            {
        //                if (modLegChunk.Id.ToString() == "f0ca4187-b95e-5153-a1eb-1e0a7fff6371")
        //                {

        //                }
        //                if (modLegChunk.Id.ToString() == "3e3ea546-1d18-6ed0-c3e4-2af56e6e8b6d")
        //                {

        //                }
        //                modLegChunk.Sha1 = modLegChunk.ModifiedEntry.Sha1;
        //                parent.ModifiedChunks.Add(modLegChunk.Id, modLegChunk);
        //                countLegacyChunksModified++;
        //            }

        //            var modifiedChunks = AssetManager.Instance.EnumerateChunks(true);
        //            foreach (var chunk in modifiedChunks)
        //            {
        //                if (parent.archiveData.ContainsKey(chunk.Sha1))
        //                    parent.archiveData[chunk.Sha1] = new ArchiveInfo() { Data = chunk.ModifiedEntry.Data };
        //                else
        //                    parent.archiveData.TryAdd(chunk.Sha1, new ArchiveInfo() { Data = chunk.ModifiedEntry.Data });
        //            }
        //            parent.Logger.Log($"Legacy :: Modified {countLegacyChunksModified} associated chunks");
        //        }
        //    }
        //}

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
            //try
            //{
            parent.Logger.Log("Loading files to know what to change.");
            if (parent.modifiedEbx.Count == 0 && parent.modifiedRes.Count == 0 && parent.ModifiedChunks.Count == 0 && parent.modifiedLegacy.Count == 0)
                return true;

            if (AssetManager.Instance == null)
            {
                CacheManager buildCache = new CacheManager();
                buildCache.LoadData(ProfileManager.ProfileName, parent.GamePath, parent.Logger, false, true);
            }

            parent.Logger.Log("Loading Cached Super Bundles.");

            if (!ModExecutor.UseModData && GameWasPatched)
            {
                DeleteBakFiles(parent.GamePath);
            }

            parent.Logger.Log("Finished loading files. Enumerating modified bundles.");

            var dictOfModsToCas = GetModdedCasFiles();
            if (dictOfModsToCas != null && dictOfModsToCas.Count > 0)
            {
                if (ErrorCounts.Count > 0)
                {
                    if (ErrorCounts[ModType.EBX] > 0)
                        parent.Logger.Log("EBX ERRORS:: " + ErrorCounts[ModType.EBX]);
                    if (ErrorCounts[ModType.RES] > 0)
                        parent.Logger.Log("RES ERRORS:: " + ErrorCounts[ModType.RES]);
                    if (ErrorCounts[ModType.CHUNK] > 0)
                        parent.Logger.Log("Chunk ERRORS:: " + ErrorCounts[ModType.CHUNK]);
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
                    //parent.Logger.Log($"Modifying CAS file - {casPath}");


                    using (NativeWriter nwCas = new NativeWriter(new FileStream(casPath, FileMode.Open)))
                    {
                        foreach (var modItem in item.Value.OrderBy(x => x.NamePath))
                        {
                            nwCas.Position = nwCas.Length;
                            byte[] data = new byte[0];
                            AssetEntry originalEntry = modItem.OriginalEntry;
                            if (originalEntry == null)
                                continue;

                            if (originalEntry.SBFileLocation != null &&
                                (
                                    //originalEntry.SBFileLocation.Contains("story", StringComparison.OrdinalIgnoreCase)
                                    //|| 
                                    originalEntry.SBFileLocation.Contains("storycharsb", StringComparison.OrdinalIgnoreCase)
                                    || originalEntry.SBFileLocation.Contains("careersba", StringComparison.OrdinalIgnoreCase)
                                    )
                                )
                                continue;

                            if (originalEntry.TOCFileLocation != null &&
                                (
                                    //originalEntry.TOCFileLocation.Contains("story", StringComparison.OrdinalIgnoreCase)
                                    //|| 
                                    originalEntry.TOCFileLocation.Contains("storycharsb", StringComparison.OrdinalIgnoreCase)
                                    || originalEntry.TOCFileLocation.Contains("careersba", StringComparison.OrdinalIgnoreCase)
                                    )
                                )
                                continue;

                            if (originalEntry != null && parent.archiveData.ContainsKey(modItem.Sha1))
                            {
                                data = parent.archiveData[modItem.Sha1].Data;
                            }
                            else
                            {
                                //parent.Logger.LogError($"Unable to find original archive data for {modItem.NamePath}");
                                continue;
                            }

                            if (data.Length == 0)
                            {
                                parent.Logger.LogError($"Unable to find any data for {modItem.NamePath}");
                                continue;
                            }

                            AssetEntry modifiedAsset = null;

                            var origSize = 0;
                            var positionOfData = nwCas.Position;
                            // write the new data to end of the file (this should be fine)
                            nwCas.Write(data);

                            switch (modItem.ModType)
                            {
                                case ModType.EBX:
                                    modifiedAsset = parent.modifiedEbx[modItem.NamePath];
                                    break;
                                case ModType.RES:
                                    modifiedAsset = parent.modifiedRes[modItem.NamePath];
                                    break;
                                case ModType.CHUNK:
                                    modifiedAsset = parent.ModifiedChunks[Guid.Parse(modItem.NamePath)];
                                    break;
                            }

                            if (modifiedAsset != null && modifiedAsset is ChunkAssetEntry)
                            {
                                var chunkModAsset = modifiedAsset as ChunkAssetEntry;
                                if (chunkModAsset.IsTocChunk)
                                    continue;

                                if (chunkModAsset.ModifiedEntry != null && chunkModAsset.ModifiedEntry.IsLegacyFile)
                                    continue;
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

                            if (string.IsNullOrEmpty(originalEntry.TOCFileLocation) && string.IsNullOrEmpty(originalEntry.SBFileLocation))
                                continue;

                            EntriesToNewPosition.Add(originalEntry, (positionOfData, data.Length, origSize, modItem.Sha1));
                        }

                    }




                }

                if (EntriesToNewPosition == null)
                {
                    parent.Logger.LogError($"Unable to find any entries to process");
                    return false;
                }

                var groupedByTOCSB = EntriesToNewPosition.GroupBy(x =>
                            !string.IsNullOrEmpty(x.Key.SBFileLocation)
                            ? x.Key.SBFileLocation
                            : x.Key.TOCFileLocation
                            )
                    .ToDictionary(gdc => gdc.Key, gdc => gdc.ToList());

                List<Task> tasks = new List<Task>();

                foreach (var tocGroup in groupedByTOCSB)
                {
                    var tocPath = tocGroup.Key;
                    if (string.IsNullOrEmpty(tocPath))
                        continue;

                    tasks.Add(Task.Run(() =>
                    {
                        var nativeTocPath = tocGroup.Key;
                        tocPath = parent.fs.ResolvePath(tocPath, ModExecutor.UseModData);

                        if (ModExecutor.UseModData && !tocPath.Contains("moddata", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new Exception($"WRONG SB PATH GIVEN! {tocPath}");
                        }

                        using (var tocSbReader = new TocSbReader_FIFA21(false, false))
                        {
                            DbObject dboOriginal = null;
                            List<DbObject> dboOriginal2 = null;
                            if (!SbToDbObject.ContainsKey(tocGroup.Key))
                            {
                                var timeStarted = DateTime.Now;

                                dboOriginal2 = tocSbReader.Read(tocPath.Replace(".sb", ".toc", StringComparison.OrdinalIgnoreCase), 0, nativeTocPath, nativePath: nativeTocPath);

                                SbToDbObject.Add(tocGroup.Key, new DbObject(dboOriginal2));
                                Debug.WriteLine("Time Taken to Read SB: " + (DateTime.Now - timeStarted).ToString());
                            }

                            if (SbToDbObject.ContainsKey(tocGroup.Key))
                                dboOriginal = SbToDbObject[tocGroup.Key];
                            if (dboOriginal != null)
                            {
                                parent.Logger.Log($"Processing: {tocPath}");
                                var origEbxBundles = dboOriginal.List
                                .Where(x => ((DbObject)x).HasValue("ebx"))
                                .Select(x => ((DbObject)x).GetValue<DbObject>("ebx"))
                                .Where(x => x.List != null && x.List.Any(y => parent.modifiedEbx.ContainsKey(((DbObject)y).GetValue<string>("name"))))
                                .ToList();

                                var origResBundles = dboOriginal.List
                                .Where(x => ((DbObject)x).HasValue("res"))
                                .Select(x => ((DbObject)x).GetValue<DbObject>("res"))
                                .Where(x => x.List != null && x.List.Any(y => parent.modifiedRes.ContainsKey(((DbObject)y).GetValue<string>("name"))))
                                .ToList();

                                var origChunkBundles = dboOriginal.List
                                .Where(x => ((DbObject)x).HasValue("chunks"))
                                .Select(x => ((DbObject)x).GetValue<DbObject>("chunks"))
                                .Where(x => x.List != null && x.List.Any(y => parent.ModifiedChunks.ContainsKey(((DbObject)y).GetValue<Guid>("id"))))
                                .ToList();

                                using (NativeWriter nw_toc = new NativeWriter(new FileStream(tocPath, FileMode.Open)))
                                {
                                    var assetBundleToCAS = new Dictionary<string, List<AssetEntry>>();
                                    foreach (var assetBundle in tocGroup.Value)
                                    {


                                        var casPath = string.Empty;
                                        if (assetBundle.Key is EbxAssetEntry)
                                        {
                                            DbObject origEbxDbo = null;
                                            foreach (DbObject dbInBundle in origEbxBundles)
                                            {
                                                origEbxDbo = (DbObject)dbInBundle.List.FirstOrDefault(z => ((DbObject)z)["name"].ToString() == assetBundle.Key.Name);
                                                if (origEbxDbo != null)
                                                    break;
                                            }

                                            if (origEbxDbo != null)
                                            {
                                                casPath = origEbxDbo.GetValue<string>("ParentCASBundleLocation");
                                            }
                                        }

                                        if (assetBundle.Key is ResAssetEntry)
                                        {
                                            DbObject origResDbo = null;
                                            foreach (DbObject dbInBundle in origResBundles)
                                            {
                                                origResDbo = (DbObject)dbInBundle.List.FirstOrDefault(z => ((DbObject)z)["name"].ToString() == assetBundle.Key.Name);
                                                if (origResDbo != null)
                                                    break;
                                            }

                                            if (origResDbo != null)
                                            {
                                                casPath = origResDbo.GetValue<string>("ParentCASBundleLocation");
                                            }
                                        }

                                        if (assetBundle.Key is ChunkAssetEntry)
                                        {
                                            DbObject origChunkDbo = null;
                                            foreach (DbObject dbInBundle in origChunkBundles)
                                            {
                                                origChunkDbo = (DbObject)dbInBundle.List.FirstOrDefault(z => ((DbObject)z)["id"].ToString() == assetBundle.Key.Name);
                                                if (origChunkDbo != null)
                                                    break;
                                            }

                                            if (origChunkDbo != null)
                                            {
                                                casPath = origChunkDbo.GetValue<string>("ParentCASBundleLocation");
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(casPath))
                                        {
                                            if (!assetBundleToCAS.ContainsKey(casPath))
                                                assetBundleToCAS.Add(casPath, new List<AssetEntry>());

                                            assetBundleToCAS[casPath].Add(assetBundle.Key);

                                            var positionOfNewData = assetBundle.Value.Item1;
                                            var sizeOfData = assetBundle.Value.Item2;
                                            var originalSizeOfData = assetBundle.Value.Item3;
                                            var sha = assetBundle.Value.Item4;

                                            int sb_cas_size_position = assetBundle.Key.SB_CAS_Size_Position;
                                            var sb_cas_offset_position = assetBundle.Key.SB_CAS_Offset_Position;
                                            nw_toc.BaseStream.Position = sb_cas_offset_position;
                                            nw_toc.Write((uint)positionOfNewData, Endian.Big);
                                            nw_toc.Write((uint)sizeOfData, Endian.Big);

                                        }
                                    }

                                    foreach (var abtc in assetBundleToCAS)
                                    {
                                        var resolvedCasPath = FileSystem.Instance.ResolvePath(abtc.Key, ModExecutor.UseModData);
                                        using (var nwCas = new NativeWriter(new FileStream(resolvedCasPath, FileMode.Open)))
                                        {
                                            foreach (var assetEntry in abtc.Value)
                                            {
                                                var assetBundle = tocGroup.Value.LastOrDefault(x => x.Key == assetEntry);
                                                if (assetBundle.Key is EbxAssetEntry)
                                                    WriteEbxChangesToSuperBundle(origEbxBundles, nwCas, assetBundle);
                                                else if (assetBundle.Key is ResAssetEntry)
                                                    WriteResChangesToSuperBundle(origResBundles, nwCas, assetBundle);
                                                else if (assetBundle.Key is ChunkAssetEntry)
                                                    WriteChunkChangesToSuperBundle(origChunkBundles, nwCas, assetBundle);
                                            }
                                        }
                                    }



                                }

                                using (var fsTocSig = new FileStream(tocPath, FileMode.Open))
                                    TOCFile.RebuildTOCSignatureOnly(fsTocSig);
                            }
                            parent.Logger.Log($"Processing Complete: {tocPath}");
                            if (dboOriginal != null)
                            {
                                dboOriginal = null;
                            }
                            if (dboOriginal2 != null)
                            {
                                dboOriginal2.Clear();
                                dboOriginal2 = null;
                            }
                            if (SbToDbObject != null)
                            {
                                //SbToDbObject.Clear();
                                //SbToDbObject = null;
                            }
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }


                    }));


                }

                Task.WaitAll(tasks.ToArray());

            }


            ModifyTOCChunks();


            return true;
            //}
            //catch (Exception e)
            //{
            //    throw e;
            //}

        }

        private void WriteEbxChangesToSuperBundle(List<DbObject> origEbxBundles, NativeWriter writer, KeyValuePair<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> assetBundle)
        {
            DbObject origEbxDbo = null;
            foreach (DbObject dbInBundle in origEbxBundles)
            {
                origEbxDbo = (DbObject)dbInBundle.List.FirstOrDefault(z => ((DbObject)z)["name"].ToString() == assetBundle.Key.Name);
                if (origEbxDbo != null)
                    break;
            }

            if (origEbxDbo != null)
            {
                var originalSizeOfData = assetBundle.Value.Item3;

                if (origEbxDbo.HasValue("SB_OriginalSize_Position"))
                {
                    writer.Position = origEbxDbo.GetValue<long>("SB_OriginalSize_Position");
                    writer.Write((uint)originalSizeOfData, Endian.Little);
                }

                if (origEbxDbo.HasValue("SB_Sha1_Position") && assetBundle.Value.Item4 != FMT.FileTools.Sha1.Zero)
                {
                    writer.Position = origEbxDbo.GetValue<long>("SB_Sha1_Position");
                    writer.Write(assetBundle.Value.Item4);
                }
            }
        }

        private void WriteResChangesToSuperBundle(List<DbObject> origResBundles, NativeWriter writer, KeyValuePair<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> assetBundle)
        {
            DbObject origResDbo = null;
            foreach (DbObject dbInBundle in origResBundles)
            {
                origResDbo = (DbObject)dbInBundle.List.FirstOrDefault(z => ((DbObject)z)["name"].ToString() == assetBundle.Key.Name);
                if (origResDbo != null)
                    break;
            }

            if (origResDbo != null
                && parent.modifiedRes.ContainsKey(assetBundle.Key.Name)
                && (assetBundle.Key.Type == "SkinnedMeshAsset"
                || assetBundle.Key.Type == "MeshSet"
                || assetBundle.Key.Type == "Texture"))
            {
                writer.BaseStream.Position = origResDbo.GetValue<int>("SB_ResMeta_Position");
                writer.WriteBytes(parent.modifiedRes[assetBundle.Key.Name].ResMeta);

                if (parent.modifiedRes[assetBundle.Key.Name].ResRid != 0)
                {
                    writer.BaseStream.Position = origResDbo.GetValue<int>("SB_ReRid_Position");
                    writer.Write(parent.modifiedRes[assetBundle.Key.Name].ResRid);
                }
            }

            var originalSizeOfData = assetBundle.Value.Item3;

            if (origResDbo.HasValue("SB_OriginalSize_Position"))
            {
                writer.Position = origResDbo.GetValue<long>("SB_OriginalSize_Position");
                writer.Write((uint)originalSizeOfData, Endian.Little);
            }

            if (origResDbo.HasValue("SB_Sha1_Position") && assetBundle.Value.Item4 != FMT.FileTools.Sha1.Zero)
            {
                writer.Position = origResDbo.GetValue<long>("SB_Sha1_Position");
                writer.Write(assetBundle.Value.Item4);
            }
        }

        private void WriteChunkChangesToSuperBundle(List<DbObject> origChunkBundles, NativeWriter writer, KeyValuePair<AssetEntry, (long, int, int, FMT.FileTools.Sha1)> assetBundle)
        {
            DbObject origChunkDbo = null;
            foreach (DbObject dbInBundle in origChunkBundles)
            {
                origChunkDbo = (DbObject)dbInBundle.List.FirstOrDefault(z => ((DbObject)z)["id"].ToString() == assetBundle.Key.Name);
                if (origChunkDbo != null)
                    break;
            }

            if (Guid.TryParse(assetBundle.Key.Name, out Guid bndleId))
            {
                if (origChunkDbo != null
                    && parent.ModifiedChunks.ContainsKey(bndleId)
                    )
                {
                    writer.BaseStream.Position = origChunkDbo.GetValue<int>("SB_LogicalOffset_Position");
                    writer.Write(parent.ModifiedChunks[bndleId].LogicalOffset);
                }
            }

            var originalSizeOfData = assetBundle.Value.Item3;

            if (origChunkDbo.HasValue("SB_OriginalSize_Position"))
            {
                writer.Position = origChunkDbo.GetValue<long>("SB_OriginalSize_Position");
                writer.Write((uint)originalSizeOfData, Endian.Little);
            }

            if (origChunkDbo.HasValue("SB_Sha1_Position") && assetBundle.Value.Item4 != FMT.FileTools.Sha1.Zero)
            {
                writer.Position = origChunkDbo.GetValue<long>("SB_Sha1_Position");
                writer.Write(assetBundle.Value.Item4);
            }
        }

        public override bool Cleanup(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            var r = base.Cleanup(fs, logger, modExecuter);

            return r;
        }

        //public override void ModifyTOCChunks(string directory = "native_patch")
        //{
        //    int sbIndex = -1;
        //    foreach (var catalogInfo in FileSystem.Instance.EnumerateCatalogInfos())
        //    {
        //        foreach (string sbKey in catalogInfo.SuperBundles.Keys)
        //        {
        //            sbIndex++;
        //            string tocFile = sbKey;
        //            if (catalogInfo.SuperBundles[sbKey])
        //            {
        //                tocFile = sbKey.Replace("win32", catalogInfo.Name);
        //            }

        //            // Only handle Legacy stuff right now
        //            if (!tocFile.Contains("globals", StringComparison.OrdinalIgnoreCase))
        //            {
        //                continue;
        //            }

        //            var pathToTOCFileRAW = $"{directory}/{tocFile}.toc";
        //            string location_toc_file = parent.fs.ResolvePath(pathToTOCFileRAW);
        //            using TocSbReader_FIFA21 tocSb = new TocSbReader_FIFA21(false, false);

        //            var pathToTOCFile = ModExecutor.UseModData
        //                ? location_toc_file
        //                .Replace("Data", "ModData\\Data", StringComparison.OrdinalIgnoreCase)
        //                .Replace("Patch", "ModData\\Patch", StringComparison.OrdinalIgnoreCase)
        //                : location_toc_file;

        //            // read the changed toc file in ModData
        //            tocSb.Read(pathToTOCFile, sbIndex, pathToTOCFileRAW, nativePath: pathToTOCFileRAW);
        //            if (tocSb.TOCFile == null || !tocSb.TOCFile.TocChunks.Any())
        //                continue;

        //            var patch = true;
        //            var catalog = tocSb.TOCFile.TocChunks.Max(x => x.ExtraData.Catalog.Value);
        //            if (!tocSb.TOCFile.TocChunks.Any(x => x.ExtraData.IsPatch))
        //                patch = false;

        //            var cas = tocSb.TOCFile.TocChunks.Where(x => x.ExtraData.Catalog == catalog).Max(x => x.ExtraData.Cas.Value);

        //            var newCas = cas;
        //            //var nextCasPath = GetNextCasInCatalog(catalogInfo, cas, patch, out int newCas);
        //            var nextCasPath = FileSystem.Instance.ResolvePath(FileSystem.Instance.GetFilePath(catalog, cas, patch), ModExecutor.UseModData);
        //            //if(!File.Exists(nextCasPath))
        //            //{
        //            //    nextCasPath = FileSystem.Instance.ResolvePath(FileSystem.Instance.GetFilePath(catalog, cas, false), ModExecutor.UseModData);
        //            //    patch = false;
        //            //}
        //            using (NativeWriter nw_cas = new NativeWriter(new FileStream(nextCasPath, FileMode.OpenOrCreate)))
        //            {
        //                using (NativeWriter nw_toc = new NativeWriter(new FileStream(pathToTOCFile, FileMode.Open)))
        //                {
        //                    foreach (var modChunk in parent.ModifiedChunks)
        //                    {
        //                        if (tocSb.TOCFile.TocChunkGuids.Contains(modChunk.Key))
        //                        {
        //                            var chunkIndex = tocSb.TOCFile.TocChunks.FindIndex(x => x.Id == modChunk.Key
        //                                && modChunk.Value.ModifiedEntry != null
        //                                && (modChunk.Value.ModifiedEntry.AddToTOCChunks || modChunk.Value.ModifiedEntry.AddToChunkBundle));
        //                            if (chunkIndex != -1)
        //                            {
        //                                //var data = parent.archiveData[modChunk.Value.Sha1].Data;
        //                                var data = parent.archiveData[modChunk.Value.ModifiedEntry.Sha1].Data;

        //                                var chunkGuid = tocSb.TOCFile.TocChunkGuids[chunkIndex];

        //                                var chunk = tocSb.TOCFile.TocChunks[chunkIndex];
        //                                DbObject dboChunk = tocSb.TOCFile.TocChunkInfo[modChunk.Key];

        //                                nw_cas.Position = nw_cas.Length;
        //                                var newPosition = nw_cas.Position;
        //                                nw_cas.WriteBytes(data);
        //                                modChunk.Value.Size = data.Length;
        //                                modChunk.Value.ExtraData = new AssetExtraData()
        //                                {
        //                                    DataOffset = (uint)newPosition,
        //                                    Cas = newCas,
        //                                    Catalog = catalog,
        //                                    IsPatch = patch,
        //                                };

        //                                nw_toc.Position = dboChunk.GetValue<long>("patchPosition");
        //                                nw_toc.Write(Convert.ToByte(patch ? 1 : 0));
        //                                nw_toc.Write(Convert.ToByte(catalog));
        //                                nw_toc.Write(Convert.ToByte(newCas));

        //                                nw_toc.Position = chunk.SB_CAS_Offset_Position;
        //                                nw_toc.Write((uint)newPosition, Endian.Big);

        //                                nw_toc.Position = chunk.SB_CAS_Size_Position;
        //                                nw_toc.Write((uint)data.Length, Endian.Big);
        //                            }
        //                        }

        //                        // Added / Duplicate chunk -- Does nothing at the moment
        //                        if (modChunk.Value.ExtraData == null && tocFile == "win32/globalsfull")
        //                        {
        //                            var data = parent.archiveData[modChunk.Value.Sha1].Data;
        //                            nw_cas.Position = nw_cas.Length;
        //                            var newPosition = nw_cas.Position;
        //                            //nw_cas.WriteBytes(data);
        //                            modChunk.Value.Size = data.Length;
        //                            modChunk.Value.ExtraData = new AssetExtraData()
        //                            {
        //                                DataOffset = (uint)newPosition,
        //                                Cas = newCas,
        //                                Catalog = catalog,
        //                                IsPatch = patch,
        //                            };
        //                            //tocSb.TOCFile.TocChunks.Add(modChunk.Value);
        //                        }
        //                    }
        //                }
        //            }

        //            //using (var fsToc = new FileStream(pathToTOCFile, FileMode.Open))
        //            //{
        //            //    tocSb.TOCFile.Write(fsToc);
        //            //}
        //            TOCFile.RebuildTOCSignatureOnly(pathToTOCFile);
        //        }
        //    }

        //    if (directory == "native_patch")
        //        ModifyTOCChunks("native_data");
        //}
    }



}
