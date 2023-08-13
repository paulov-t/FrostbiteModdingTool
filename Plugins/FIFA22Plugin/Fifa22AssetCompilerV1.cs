using FMT.FileTools;
using FMT.Logging;
using FrostySdk;
using FrostySdk.Frostbite.Compilers;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using ModdingSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FIFA22Plugin
{

    /// <summary>
    /// FIFA 23 Asset Compiler. Solid and works. Uses .cache file to determine what needs editing
    /// Linked to FIFA21BundleAction
    /// </summary>
    public class Fifa22AssetCompilerV1 : Frostbite2022AssetCompiler, IAssetCompiler
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
            if (!ProfileManager.IsFIFA22DataVersion())
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
            if (parent.modifiedEbx.Count == 0 && parent.modifiedRes.Count == 0 && parent.ModifiedChunks.Count == 0 && parent.modifiedLegacy.Count == 0)
                return true;

            if (!ModExecutor.UseModData && GameWasPatched)
            {
                DeleteBakFiles(parent.GamePath);
            }

            parent.Logger.Log("Retreiving list of Modified CAS Files.");
            var dictOfModsToCas = GetModdedCasFiles();

            // Force delete of Live Tuning Updates
            if (dictOfModsToCas.Any(x => x.Value.Any(y => y.NamePath.Contains("gp_", StringComparison.OrdinalIgnoreCase))))
                parent.DeleteLiveUpdates = true;

            parent.Logger.Log("Modifying TOC Chunks.");
            ModifyTOCChunks();

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




        private void ModifyTOCCasBundles(string directory = "native_patch")
        {
            foreach (var catalogInfo in FileSystem.Instance.EnumerateCatalogInfos())
            {
                byte[] key_2_from_key_manager = KeyManager.Instance.GetKey("Key2");
                foreach (string key3 in catalogInfo.SuperBundles.Keys)
                {

                    string tocFile = key3;
                    if (catalogInfo.SuperBundles[key3])
                    {
                        tocFile = key3.Replace("win32", catalogInfo.Name);
                    }

                    var tocFileRAW = $"{directory}/{tocFile}.toc";
                    string location_toc_file = parent.fs.ResolvePath(tocFileRAW);
                    using TocSbReader_Fifa22 tocSb = new TocSbReader_Fifa22();
                    tocSb.DoLogging = false;
                    tocSb.ProcessData = false;

                    var location_toc_file_new = ModExecutor.UseModData
                        ? location_toc_file
                        .Replace("Data", "ModData\\Data", StringComparison.OrdinalIgnoreCase)
                        .Replace("Patch", "ModData\\Patch", StringComparison.OrdinalIgnoreCase)
                        : location_toc_file;

                    // read the changed toc file in ModData
                    tocSb.Read(location_toc_file_new, 0, tocFileRAW);
                    if (tocSb.TOCFile == null || !tocSb.TOCFile.CasBundles.Any())
                        continue;

                }
            }

            if (directory == "native_patch")
                ModifyTOCCasBundles("native_data");
        }

        private string GetNextCasInCatalog(Catalog catalogInfo, int lastCas, bool patch, out int newCas)
        {
            newCas = lastCas + 1;
            //newCas = lastCas;
            string stub = parent.fs.BasePath + $"ModData\\{(patch ? "Patch" : "Data")}\\" + catalogInfo.Name + "\\cas_";

            string text = stub + (newCas).ToString("D2") + ".cas";

            var fiCas = new FileInfo(text);
            while (fiCas.Exists && fiCas.Length > 1073741824)
            {
                newCas++;
                text = stub + (newCas).ToString("D2") + ".cas";
                fiCas = new FileInfo(text);
            }

            if (!ModExecutor.UseModData)
                text = text.Replace("ModData", "", StringComparison.OrdinalIgnoreCase);

            fiCas = null;
            return text;
        }

        public override bool Cleanup(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            var r = base.Cleanup(fs, logger, modExecuter);

            return r;
        }

        public void GetChunkAssetForEbx(EbxAssetEntry ebxAssetEntry, out ChunkAssetEntry chunkAssetEntry, out EbxAsset ebxAsset)
        {
            chunkAssetEntry = null;
            ebxAsset = AssetManager.Instance.GetEbx(ebxAssetEntry);
            if (ebxAsset != null)
            {
                dynamic rootObject = ebxAsset.RootObject;
                if (rootObject != null)
                {
                    dynamic val = rootObject.Manifest;
                    chunkAssetEntry = AssetManager.Instance.GetChunkEntry(val.ChunkId);
                }
            }
        }
    }



}
