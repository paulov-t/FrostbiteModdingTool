﻿using FrostbiteSdk.Extras;
using Frosty.Hash;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using Madden21Plugin;
using paulv2k4ModdingExecuter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static FIFA22Plugin.AssetLoader_Fifa22;
using static paulv2k4ModdingExecuter.FrostyModExecutor;

namespace FIFA22Plugin
{

    /// <summary>
    /// Currently. The Madden 21 Compiler does not work in game.
    /// </summary>
    public class Fifa22AssetCompiler : IAssetCompiler
    {
        public const string ModDirectory = "ModData";
        public const string PatchDirectory = "Patch";


        public enum ModType
        {
            EBX,
            RES,
            CHUNKS
        }

        public struct ModdedFile
        {
            public Sha1 Sha1 { get; set; }
            public string NamePath { get; set; }
            public ModType ModType { get; set; }
            public bool IsAdded { get; set; }
            public AssetEntry OriginalEntry { get; set; }
            public int BundleIndex { get; set; }

            public ModdedFile(Sha1 inSha1, string inNamePath, ModType inModType, bool inAdded, AssetEntry inOrigEntry, int inBundleIndex)
            {
                Sha1 = inSha1;
                NamePath = inNamePath;
                ModType = inModType;
                IsAdded = inAdded;
                OriginalEntry = inOrigEntry;
                BundleIndex = inBundleIndex;
            }
        }

        private void MakeTOCOriginals(string dir)
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

        private void MakeTOCBackups(string dir)
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

        /// <summary>
        /// Construct the Modded Bundles within CAS files
        /// </summary>
        /// <returns>cas to Tuple of Sha1, Name, Type, IsAdded</returns>
        private void ProcessLegacyFiles()
        {
            // Handle Legacy first to generate modified chunks
            if (ModExecuter.modifiedLegacy.Count > 0)
            {
                BuildCache buildCache = new BuildCache();
                buildCache.LoadData(ProfilesLibrary.ProfileName, ModExecuter.GamePath, ModExecuter.Logger, false, true); ;

                ModExecuter.Logger.Log($"Legacy :: {ModExecuter.modifiedLegacy.Count} Legacy files found. Modifying associated chunks");

                Dictionary<string, byte[]> legacyData = new Dictionary<string, byte[]>();
                var countLegacyChunksModified = 0;
                foreach (var modLegacy in ModExecuter.modifiedLegacy)
                {
                    var originalEntry = AssetManager.Instance.GetCustomAssetEntry("legacy", modLegacy.Key);
                    var data = ModExecuter.archiveData[modLegacy.Value.Sha1].Data;
                    legacyData.Add(modLegacy.Key, data);

                }

                AssetManager.Instance.ModifyLegacyAssets(legacyData, true);

                var modifiedLegacyChunks = AssetManager.Instance.EnumerateChunks(true);
                foreach (var modLegChunk in modifiedLegacyChunks)
                {
                    modLegChunk.Sha1 = modLegChunk.ModifiedEntry.Sha1;
                    if (!ModExecuter.ModifiedChunks.ContainsKey(modLegChunk.Id))
                    {
                        ModExecuter.ModifiedChunks.Add(modLegChunk.Id, modLegChunk);
                    }
                    else
                    {
                        ModExecuter.ModifiedChunks[modLegChunk.Id] = modLegChunk;
                    }
                    countLegacyChunksModified++;
                }

                var modifiedChunks = AssetManager.Instance.EnumerateChunks(true);
                foreach (var chunk in modifiedChunks)
                {
                    if (ModExecuter.archiveData.ContainsKey(chunk.Sha1))
                        ModExecuter.archiveData[chunk.Sha1] = new ArchiveInfo() { Data = chunk.ModifiedEntry.Data };
                    else
                        ModExecuter.archiveData.TryAdd(chunk.Sha1, new ArchiveInfo() { Data = chunk.ModifiedEntry.Data });
                }
                ModExecuter.Logger.Log($"Legacy :: Modified {countLegacyChunksModified} associated chunks");
            }
            // ------ End of handling Legacy files ---------

        }

        FrostyModExecutor ModExecuter = null;
        FrostyModExecutor parent => ModExecuter;

        /// <summary>
        /// This is run AFTER the compilation of the fbmod into resource files ready for the Actions to TOC/SB/CAS to be taken
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="logger"></param>
        /// <param name="frostyModExecuter">Frosty Mod Executer object</param>
        /// <returns></returns>
        public bool Compile(FileSystem fs, ILogger logger, object frostyModExecuter)
        {
            ModExecuter = (FrostyModExecutor)frostyModExecuter;
            // ------------------------------------------------------------------------------------------
            // You will need to change this to ProfilesLibrary.DataVersion if you change the Profile.json DataVersion field
            if (ProfilesLibrary.IsFIFA22DataVersion())
            {
                if (UseModData)
                {
                    Directory.CreateDirectory(fs.BasePath + ModDirectory + "\\Data");
                    Directory.CreateDirectory(fs.BasePath + ModDirectory + "\\Patch");

                    logger.Log($"Copying files from Data to {ModDirectory}/Data");
                    CopyDataFolder(fs.BasePath + "\\Data\\", fs.BasePath + ModDirectory + "\\Data\\", logger);
                    logger.Log($"Copying files from Patch to {ModDirectory}/Patch");
                    CopyDataFolder(fs.BasePath + PatchDirectory, fs.BasePath + ModDirectory + "\\" + PatchDirectory, logger);
                }
                else
                {
                    var notAnyBackups = !Directory.EnumerateFiles(ModExecuter.GamePath + "\\Data\\", "*.bak").Any();
                    ModExecuter.GameWasPatched = ModExecuter.GameWasPatched || notAnyBackups;
                    if (!ModExecuter.GameWasPatched)
                    {
                        ModExecuter.Logger.Log("Same Game Version detected. Using vanilla backups.");

                        MakeTOCOriginals(ModExecuter.GamePath + "\\Data\\");
                        MakeTOCOriginals(ModExecuter.GamePath + "\\Patch\\");

                    }
                    else
                    {
                        ModExecuter.Logger.Log("Game was patched. Creating backups.");

                        MakeTOCBackups(ModExecuter.GamePath + "\\Data\\");
                        MakeTOCBackups(ModExecuter.GamePath + "\\Patch\\");
                    }
                }

                //BuildCache buildCache = new BuildCache();
                //buildCache.LoadData(ProfilesLibrary.ProfileName, parent.GamePath, parent.Logger, false, true); ;

                ModExecuter.Logger.Log("Enumerating modified bundles.");

                ProcessLegacyFiles();
                ProcessBundles();

                ModExecuter.Logger.Log($"Modified {ModifiedCount_EBX} ebx, {ModifiedCount_RES} res, {ModifiedCount_Chunks} chunks");


                return true;
            }
            return false;
        }

        public int ModifiedCount_EBX = 0;
        public int ModifiedCount_RES = 0;
        public int ModifiedCount_Chunks = 0;

        private class BundleFileEntry
            {
                public int CasIndex;

                public int Offset;

                public int Size;

                public BundleFileEntry(int inCasIndex, int inOffset, int inSize)
                {
                    CasIndex = inCasIndex;
                    Offset = inOffset;
                    Size = inSize;
                }
            }

            private static readonly object locker = new object();

            public static int CasFileCount = 0;

            private Dictionary<int, string> casFiles = new Dictionary<int, string>();

            public Dictionary<int, string> CasFiles => casFiles;


        Catalog catalogInfo;
        public void ProcessBundles(bool bundlesInPatch = false)
        {
            var folder = bundlesInPatch ? "native_patch" : "native_data";

            foreach (Catalog ci in FileSystem.Instance.EnumerateCatalogInfos())
            {
                catalogInfo = ci;

                NativeWriter writer_new_cas_file = null;
                foreach (string sbName in catalogInfo.SuperBundles.Keys)
                {
                    string tocFile = sbName;
                    if (catalogInfo.SuperBundles[sbName])
                    {
                        tocFile = sbName.Replace("win32", catalogInfo.Name);
                    }

                    tocFile = sbName.Replace("win32", catalogInfo.Name).Replace("cs/", "");
                    if (parent.fs.ResolvePath(folder + tocFile + ".toc") == "")
                    {
                        tocFile = sbName;
                    }
                    var tocFileRAW = $"{folder}{tocFile}.toc";
                    string location_toc_file = ModExecuter.fs.ResolvePath(tocFileRAW);

                    if (string.IsNullOrEmpty(location_toc_file))
                        continue;

                    parent.Logger.Log($"Compiling: {location_toc_file}");
                    var msNewTOCFile = new MemoryStream();

                    TocSbReader_Fifa22 tocSbReader = new TocSbReader_Fifa22();
                    tocSbReader.ProcessData = false;
                    tocSbReader.DoLogging = false;

                    tocSbReader.Read(location_toc_file, 0, null, null, true, tocFile);

                    //if (!FrostyModExecutor.UseModData && !File.Exists(location_toc_file + ".ebak"))
                    //    File.Copy(location_toc_file, location_toc_file + ".ebak");

                    if (tocSbReader.TOCFile.TOCObjects != null && tocSbReader.TOCFile.TOCObjects.List.Any())
                    {
                        Dictionary<string, List<(DbObject, AssetEntry)>> modToCas = new Dictionary<string, List<(DbObject, AssetEntry)>>();

                        foreach (DbObject o in tocSbReader.TOCFile.TOCObjects.List)
                        {
                            DbObject res = o.GetValue<DbObject>("res");
                            if (res != null && parent.modifiedRes.Any())
                            {

                                var resNames = res.List.Select(x => ((DbObject)x).GetValue<string>("name"));
                                if (resNames != null)
                                {
                                    if (resNames.Any(x => parent.modifiedRes.ContainsKey(x)))
                                    {
                                        for (var dboI = 0; dboI < res.Count; dboI++)
                                        {
                                            for (var modChunkI = 0; modChunkI < parent.ModifiedChunks.Count; modChunkI++)
                                            {
                                                var dboItem = res.List[dboI] as DbObject;
                                                var dboName = dboItem.GetValue<string>("name");
                                                if (parent.modifiedRes.ContainsKey(dboName))
                                                {
                                                    var modItem = parent.modifiedRes[dboName];

                                                    var dboCCas = dboItem.GetValue<int>("cas");
                                                    var dboCCatalog = dboItem.GetValue<int>("catalog");
                                                    var dboCPatch = dboItem.GetValue<bool>("patch");
                                                    var dboRawFilePath = FileSystem.Instance.GetFilePath(dboCCatalog, dboCCas, dboCPatch);

                                                    if (!modToCas.ContainsKey(dboRawFilePath))
                                                        modToCas.Add(dboRawFilePath, new List<(DbObject, AssetEntry)>());

                                                    if (modToCas.ContainsKey(dboRawFilePath))
                                                        modToCas[dboRawFilePath].Add((dboItem, modItem));

                                                    ModifiedCount_RES++;
                                                }

                                            }
                                        }
                                    }
                                }
                            }

                            DbObject chunks = o.GetValue<DbObject>("chunks");
                            if (chunks != null && parent.ModifiedChunks.Any())
                            {

                                var chunkIds = chunks.List.Select(x => ((DbObject)x).GetValue<Guid>("id"));
                                if (chunkIds != null)
                                {
                                    if (chunkIds.Any(x => parent.ModifiedChunks.ContainsKey(x)))
                                    {
                                        for (var dboChunkI = 0; dboChunkI < chunks.Count; dboChunkI++)
                                        {
                                            for (var modChunkI = 0; modChunkI < parent.ModifiedChunks.Count; modChunkI++)
                                            {
                                                var dboC = chunks.List[dboChunkI] as DbObject;
                                                var dboCGuid = dboC.GetValue<Guid>("id");
                                                if (parent.ModifiedChunks.ContainsKey(dboCGuid))
                                                {
                                                    var modC = parent.ModifiedChunks[dboCGuid];

                                                    var dboCCas = dboC.GetValue<int>("cas");
                                                    var dboCCatalog = dboC.GetValue<int>("catalog");
                                                    var dboCPatch = dboC.GetValue<bool>("patch");
                                                    var dboRawFilePath = FileSystem.Instance.GetFilePath(dboCCatalog, dboCCas, dboCPatch);

                                                    if (!modToCas.ContainsKey(dboRawFilePath))
                                                        modToCas.Add(dboRawFilePath, new List<(DbObject, AssetEntry)>());

                                                    if (modToCas.ContainsKey(dboRawFilePath))
                                                        modToCas[dboRawFilePath].Add((dboC, modC));

                                                    ModifiedCount_Chunks++;
                                                }

                                            }
                                        }
                                    }
                                }


                            }


                            foreach (var mcToCas in modToCas)
                            {
                                var resolvedPath = FileSystem.Instance.ResolvePath(mcToCas.Key);
                                if (resolvedPath != null)
                                {
                                    using (var nwCas = new NativeWriter(new FileStream(resolvedPath, FileMode.Open)))
                                    {
                                        nwCas.Position = nwCas.Length;
                                        foreach (var t in mcToCas.Value)
                                        {
                                            var data = parent.archiveData[t.Item2.Sha1].Data;
                                            if (data != null)
                                            {
                                                var newDataPosition = nwCas.Position;
                                                nwCas.Write(data);
                                                t.Item1.SetValue("originalSize", new CasReader(new MemoryStream(data)).Read().Length);
                                                var cBundle = tocSbReader.TOCFile.CasBundles.FirstOrDefault(
                                                     x => x.Entries.Any(
                                                         y => y.locationOfSize == t.Item1.GetValue<long>("SB_CAS_Size_Position")
                                                         ));
                                                var cEntry = cBundle.Entries.FirstOrDefault(
                                                         y => y.locationOfSize == t.Item1.GetValue<long>("SB_CAS_Size_Position")
                                                         );
                                                if (cEntry != null)
                                                {
                                                    cEntry.bundleSizeInCas = data.Length;
                                                    cEntry.bundleOffsetInCas = (int)newDataPosition;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }

                    if (tocSbReader.TOCFile.TocChunks != null && tocSbReader.TOCFile.TocChunks.Any())
                    {
                        var patch = true;
                        var catalog = tocSbReader.TOCFile.TocChunks.Max(x => x.ExtraData.Catalog.Value);
                        if (!tocSbReader.TOCFile.TocChunks.Any(x => x.ExtraData.IsPatch))
                            patch = false;

                        var cas = tocSbReader.TOCFile.TocChunks.Where(x => x.ExtraData.Catalog == catalog).Max(x => x.ExtraData.Cas.Value);

                        var nextCasPath = GetNextCasInCatalog(catalogInfo, cas, patch, out int newCas);

                        // Modified Toc chunks
                        foreach (var modChunk in parent.ModifiedChunks)
                        {
                            if (tocSbReader.TOCFile.TocChunkGuids.Contains(modChunk.Key))
                            {
                                var chunkIndex = tocSbReader.TOCFile.TocChunks.FindIndex(x => x.Id == modChunk.Key
                                    && modChunk.Value.ModifiedEntry != null
                                    && (modChunk.Value.ModifiedEntry.AddToTOCChunks || modChunk.Value.ModifiedEntry.AddToChunkBundle));
                                if (chunkIndex != -1)
                                {
                                    var data = parent.archiveData[modChunk.Value.Sha1].Data;

                                    var chunkGuid = tocSbReader.TOCFile.TocChunkGuids[chunkIndex];

                                    var chunk = tocSbReader.TOCFile.TocChunks[chunkIndex];
                                    DbObject dboChunk = tocSbReader.TOCFile.TocChunkInfo[modChunk.Key];

                                    using (NativeWriter nw_cas = new NativeWriter(new FileStream(nextCasPath, FileMode.OpenOrCreate)))
                                    {
                                        nw_cas.Position = nw_cas.Length;
                                        var newPosition = nw_cas.Position;
                                        nw_cas.WriteBytes(data);
                                        chunk.ExtraData.IsPatch = patch;
                                        chunk.ExtraData.Catalog = catalog;
                                        chunk.ExtraData.Cas = (ushort)newCas;
                                        chunk.ExtraData.DataOffset = (uint)newPosition;
                                        chunk.Size = data.Length;
                                        tocSbReader.TOCFile.TocChunks[chunkIndex] = chunk;

                                        ModifiedCount_Chunks++;

                                    }
                                }
                            }
                        }
                    }

                    tocSbReader.TOCFile.Write(msNewTOCFile);


                    if (msNewTOCFile != null && msNewTOCFile.Length > 0)
                    {
                        File.WriteAllBytes(location_toc_file, msNewTOCFile.ToArray());
                    }
                }
                if (writer_new_cas_file != null)
                {
                    writer_new_cas_file?.Close();
                }

                
            }

            if (!bundlesInPatch)
                ProcessBundles(true);
        }

        DbObject layoutToc = null;

        private string GetNextCasInCatalog(Catalog catalogInfo, int lastCas, bool patch, out int newCas)
        {
            newCas = lastCas + 1;
            //newCas = lastCas;
            string stub = parent.fs.BasePath;// + $"ModData\\{(patch ? "Patch" : "Data")}\\" + catalogInfo.Name + "\\cas_";
            if (FrostyModExecutor.UseModData)
                stub += $"ModData\\{(patch ? "Patch" : "Data")}\\" + catalogInfo.Name + "\\cas_";
            else
                stub += $"\\{(patch ? "Patch" : "Data")}\\" + catalogInfo.Name + "\\cas_";

            string text = stub + (newCas).ToString("D2") + ".cas";

            var fiCas = new FileInfo(text);
            while (fiCas.Exists && fiCas.Length > 1073741824)
            {
                newCas++;
                text = stub + (newCas).ToString("D2") + ".cas";
                fiCas = new FileInfo(text);
            }

            //if (!FrostyModExecutor.UseModData)
            //    text = text.Replace("ModData", "", StringComparison.OrdinalIgnoreCase);

            fiCas = null;
            return text;
        }

        private uint HashString(string strToHash, uint initial = 0u)
            {
                uint num = 2166136261u;
                if (initial != 0)
                {
                    num = initial;
                }
                for (int i = 0; i < strToHash.Length; i++)
                {
                    num = (strToHash[i] ^ (16777619 * num));
                }
                return num;
            }

            private static uint HashData(byte[] b, uint initial = 0u)
            {
                uint num = (uint)((sbyte)b[0] ^ 0x50C5D1F);
                int num2 = 1;
                if (initial != 0)
                {
                    num = initial;
                    num2 = 0;
                }
                for (int i = num2; i < b.Length; i++)
                {
                    num = (uint)((int)(sbyte)b[i] ^ (int)(16777619 * num));
                }
                return num;
            }

           


        private static void CopyDataFolder(string from_datafolderpath, string to_datafolderpath, ILogger logger)
        {
            Directory.CreateDirectory(to_datafolderpath);

            var dataFiles = Directory.EnumerateFiles(from_datafolderpath, "*.*", SearchOption.AllDirectories);
            var dataFileCount = dataFiles.Count();
            var indexOfDataFile = 0;
            //ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = 8 };
            //Parallel.ForEach(dataFiles, (f) =>
            foreach (var originalFilePath in dataFiles)
            {
                var finalDestinationPath = originalFilePath.ToLower().Replace(from_datafolderpath.ToLower(), to_datafolderpath.ToLower());

                bool Copied = false;

                var lastIndexOf = finalDestinationPath.LastIndexOf("\\");
                var newDirectory = finalDestinationPath.Substring(0, lastIndexOf) + "\\";
                if (!Directory.Exists(newDirectory))
                {
                    Directory.CreateDirectory(newDirectory);
                }


                if (!finalDestinationPath.Contains(ModDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Incorrect Copy of Files to " + ModDirectory);
                }

                var fIDest = new FileInfo(finalDestinationPath);
                var fIOrig = new FileInfo(originalFilePath);

                if (fIDest.Exists && finalDestinationPath.Contains(ModDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    var isCas = fIDest.Extension.Contains("cas", StringComparison.OrdinalIgnoreCase);

                    if (
                        isCas
                        && fIDest.Length != fIOrig.Length
                        )
                    {
                        fIDest.Delete();
                    }
                    else if
                        (
                            !isCas
                            &&
                            (
                                fIDest.Length != fIOrig.Length
                                ||
                                    (
                                        //fIDest.LastWriteTime.Day != fIOrig.LastWriteTime.Day
                                        //&& fIDest.LastWriteTime.Hour != fIOrig.LastWriteTime.Hour
                                        //&& fIDest.LastWriteTime.Minute != fIOrig.LastWriteTime.Minute
                                        !File.ReadAllBytes(finalDestinationPath).SequenceEqual(File.ReadAllBytes(originalFilePath))
                                    )
                            )
                        )
                    {
                        File.Delete(finalDestinationPath);
                    }
                }

                if (!File.Exists(finalDestinationPath))
                {
                    // Quick Copy
                    if (fIOrig.Length < 1024 * 100)
                    {
                        using (var inputStream = new NativeReader(File.Open(originalFilePath, FileMode.Open)))
                        using (var outputStream = new NativeWriter(File.Open(finalDestinationPath, FileMode.Create)))
                        {
                            outputStream.Write(inputStream.ReadToEnd());
                        }
                    }
                    else
                    {
                        //File.Copy(f, finalDestination);
                        CopyFile(originalFilePath, finalDestinationPath);
                    }
                    Copied = true;
                }
                indexOfDataFile++;

                if (Copied)
                    logger.Log($"Data Setup - Copied ({indexOfDataFile}/{dataFileCount}) - {originalFilePath}");
                //});
            }
        }

        public static void CopyFile(string inputFilePath, string outputFilePath)
        {
            using (var inStream = new FileStream(inputFilePath, FileMode.Open))
            {
                int bufferSize = 1024 * 1024;

                using (FileStream fileStream = new FileStream(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fileStream.SetLength(inStream.Length);
                    int bytesRead = -1;
                    byte[] bytes = new byte[bufferSize];

                    while ((bytesRead = inStream.Read(bytes, 0, bufferSize)) > 0)
                    {
                        fileStream.Write(bytes, 0, bytesRead);
                    }
                }
            }
        }

    }
}
