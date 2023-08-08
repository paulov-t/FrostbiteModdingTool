using FMT.FileTools;
using Frostbite.Deobfuscators;
using FrostyModManager;
using FrostySdk;
using FrostySdk.Frostbite.Compilers;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using ModdingSupport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ModdingSupport.ModExecutor;

namespace StarWarsSquadronsPlugin
{
    public class SWSCompiler : Frostbite2022AssetCompiler, IAssetCompiler
    {

        public override string ModDirectory => "";

        public override bool PreCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            ModExecuter = modExecuter;

            CreateModDataDiretorysAndPopulate(logger);

            AllowGameToLaunchWithoutLauncherOrAC(logger);

            FillModExecutorResources(logger);

            return true;
        }

        private void FillModExecutorResources(ILogger logger)
        {
            foreach (string catalogName in FileSystem.Instance.Catalogs)
            {
                Dictionary<int, Dictionary<uint, CatResourceEntry>> entries = LoadCatalog("native_patch/" + catalogName + "/cas.cat", out int hash);
                if (entries != null)
                    ModExecuter.Resources.Add(hash, entries);

                foreach(var item in LoadCatalog("native_data/" + catalogName + "/cas.cat", out hash))
                {
                    if(!entries.ContainsKey(item.Key))
                        entries.Add(item.Key, item.Value);
                    else
                    {
                        foreach (var entry in entries[item.Key]) 
                        {
                            if(!entries[item.Key].ContainsKey(entry.Key))
                                entries[item.Key].Add(entry.Key, entry.Value);
                        }
                    }
                }
                if (entries != null)
                    ModExecuter.Resources.Add(hash, entries);
            }
        }

        private Dictionary<int, Dictionary<uint, CatResourceEntry>> LoadCatalog(string filename, out int catFileHash)
        {
            catFileHash = 0;
            string fullPath = FileSystem.Instance.ResolvePath(filename);
            if (!File.Exists(fullPath))
                return null;

            catFileHash = Fnv1.HashString(fullPath.ToLower());
            Dictionary<int, Dictionary<uint, CatResourceEntry>> resources = new Dictionary<int, Dictionary<uint, CatResourceEntry>>();
            using (CatReader reader = new CatReader(new FileStream(fullPath, FileMode.Open, FileAccess.Read), FileSystem.Instance.CreateDeobfuscator()))
            {
                for (int i = 0; i < reader.ResourceCount; i++)
                {
                    CatResourceEntry entry = reader.ReadResourceEntry();
                    if (!resources.ContainsKey(entry.ArchiveIndex))
                        resources.Add(entry.ArchiveIndex, new Dictionary<uint, CatResourceEntry>());
                    resources[entry.ArchiveIndex].Add(entry.Offset, entry);
                }
            }

            return resources;
        }

        private void AllowGameToLaunchWithoutLauncherOrAC(ILogger logger)
        {
            if (!Directory.Exists(FileSystem.Instance.BasePath))
                throw new DirectoryNotFoundException($"Unable to find the correct base path directory of {FileSystem.Instance.BasePath}");

            RemoveLauncher(logger);
            UninstallAC(logger);
            AddSteamId(logger);
        }

        private void AddSteamId(ILogger logger)
        {
            var pathToSteamAppId = Path.Combine(FileSystem.Instance.BasePath, "steam_appid.txt");
            if (File.Exists(pathToSteamAppId))
                return;

            File.WriteAllText(pathToSteamAppId, "1222730");
        }

        private void UninstallAC(ILogger logger)
        {
            try
            {
                //throw new NotImplementedException();
                if(Directory.Exists(Path.Combine(FileSystem.Instance.BasePath, "EasyAntiCheat")))
                    Directory.Delete(Path.Combine(FileSystem.Instance.BasePath, "EasyAntiCheat"), true);
            }
            catch
            {
                logger.Log("AC Removal has not been completed. Please do this manually!");
            }
        }

        private void RemoveLauncher(ILogger logger)
        {
            if (File.Exists(Path.Combine(FileSystem.Instance.BasePath, "starwarssquadrons_launcher.exe.bak"))
                && !File.Exists(Path.Combine(FileSystem.Instance.BasePath, "starwarssquadrons_launcher.exe"))
                )
            {
                logger.Log("starwarssquadrons_launcher.exe has already been removed");
                return;
            }

            if (File.Exists(Path.Combine(FileSystem.Instance.BasePath, "starwarssquadrons_launcher.exe")))
            {
                if (!File.Exists(Path.Combine(FileSystem.Instance.BasePath, "starwarssquadrons_launcher.exe.bak")))
                    File.Move(Path.Combine(FileSystem.Instance.BasePath, "starwarssquadrons_launcher.exe"), Path.Combine(FileSystem.Instance.BasePath, "starwarssquadrons_launcher.exe.bak"));
                
                // If it still exists for whatever reason
                if(File.Exists(Path.Combine(FileSystem.Instance.BasePath, "starwarssquadrons_launcher.exe")))
                    File.Delete(Path.Combine(FileSystem.Instance.BasePath, "starwarssquadrons_launcher.exe"));

                logger.Log("starwarssquadrons_launcher.exe has been removed");
            }
        }

        private void CreateModDataDiretorysAndPopulate(ILogger logger)
        {

            if (!Directory.Exists(FileSystem.Instance.BasePath))
                throw new DirectoryNotFoundException($"Unable to find the correct base path directory of {FileSystem.Instance.BasePath}");

            if (string.IsNullOrEmpty(ModDirectory))
                return;
            
            Directory.CreateDirectory(FileSystem.Instance.BasePath + ModDirectory + "\\Data");
            Directory.CreateDirectory(FileSystem.Instance.BasePath + ModDirectory + "\\Patch");

            logger.Log($"Copying files from Data to {ModDirectory}/Data");
            CopyDataFolder(FileSystem.Instance.BasePath + "\\Data\\", FileSystem.Instance.BasePath + ModDirectory + "\\Data\\", logger);
            logger.Log($"Copying files from Patch to {ModDirectory}/Patch");
            CopyDataFolder(FileSystem.Instance.BasePath + "\\Patch\\", FileSystem.Instance.BasePath + ModDirectory + "\\Patch\\", logger);
        }

        public override bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {

            List<ManifestBundleAction> actions = new List<ManifestBundleAction>();
            ManualResetEvent doneEvent = new ManualResetEvent(false);

            if (modExecuter.addedBundles.Count != 0)
            {
                foreach (string bundleName in modExecuter.addedBundles["<none>"])
                    FileSystem.Instance.AddManifestBundle(new ManifestBundleInfo() { hash = Fnv1.HashString(bundleName) });
            }

            Dictionary<string, List<ModBundleInfo>> tasks = new Dictionary<string, List<ModBundleInfo>>();
            foreach (ModBundleInfo bundle in modExecuter.modifiedBundles.Values)
            {
                ManifestBundleInfo manifestBundle = FileSystem.Instance.GetManifestBundle(bundle.Name);
                string catalog = FileSystem.Instance.GetCatalog(manifestBundle.files[0].file);

                if (!tasks.ContainsKey(catalog))
                    tasks.Add(catalog, new List<ModBundleInfo>());

                tasks[catalog].Add(bundle);
            }

            foreach (List<ModBundleInfo> task in tasks.Values)
            {
                ManifestBundleAction action = new ManifestBundleAction(task, doneEvent, modExecuter, CancellationToken.None);
                actions.Add(action);
            }

            List<Task> actionTasks = new List<Task>();
            foreach (ManifestBundleAction action in actions)
            {
                actionTasks.Add(Task.Run(action.Run));
            }

            Task.WaitAll(actionTasks.ToArray());

            foreach (ManifestBundleAction completedAction in actions)
            {
                if (completedAction.Exception != null)
                {
                    // if any of the threads caused an exception, throw it to the global handler
                    // as the game data is now in an inconsistent state
                    throw completedAction.Exception;
                }

                if (completedAction.DataRefs.Count > 0)
                {
                    // add bundle data to archive
                    for (int i = 0; i < completedAction.BundleRefs.Count; i++)
                    {
                        if (!modExecuter.archiveData.ContainsKey(completedAction.BundleRefs[i]))
                            modExecuter.archiveData.TryAdd(completedAction.BundleRefs[i], new ArchiveInfo() { Data = completedAction.BundleBuffers[i] });
                    }

                    // add refs to be added to cas (and manifest)
                    for (int i = 0; i < completedAction.DataRefs.Count; i++)
                        modExecuter.m_casData.Add(FileSystem.Instance.GetCatalog(completedAction.FileInfos[i].FileInfo.file), completedAction.DataRefs[i], completedAction.FileInfos[i].Entry, completedAction.FileInfos[i].FileInfo);
                }
            }

            // now process manifest chunk changes
            //if (modExecuter.m_modifiedSuperBundles.ContainsKey(0))
            //{
            //    foreach (Guid id in modExecuter.m_modifiedSuperBundles[0].Modify.Chunks)
            //    {
            //        ChunkAssetEntry entry = modExecuter.ModifiedChunks[id];
            //        ManifestChunkInfo ci = FileSystem.Instance.GetManifestChunk(entry.Id);

            //        if (ci != null)
            //        {
            //            //if (entry.TocChunkSpecialHack)
            //            {
            //                // change to using the first catalog
            //                ci.file.file = new ManifestFileRef(0, false, 0);
            //            }

            //            modExecuter.m_casData.Add(FileSystem.Instance.GetCatalog(ci.file.file), entry.Sha1, entry, ci.file);
            //        }
            //    }
            //    foreach (Guid id in modExecuter.m_modifiedSuperBundles[0].Add.Chunks)
            //    {
            //        ChunkAssetEntry entry = modExecuter.ModifiedChunks[id];

            //        ManifestChunkInfo ci = new ManifestChunkInfo
            //        {
            //            guid = entry.Id,
            //            file = new ManifestFileInfo { file = new ManifestFileRef(0, false, 0), isChunk = true }
            //        };
            //        FileSystem.Instance.AddManifestChunk(ci);

            //        modExecuter.m_casData.Add(FileSystem.Instance.GetCatalog(ci.file.file), entry.Sha1, entry, ci.file);
            //    }
            //}

            var casFilesToNewChunkPositions = new Dictionary<string, List<(ManifestFileInfo, ManifestChunkInfo, ChunkAssetEntry)>>();


            foreach(var manifestPath in FileSystem.Instance.ManifestPaths)
            {
                var chunksInManifestPath = FileSystem.Instance.ManifestPathsToChunks[manifestPath];
                if (chunksInManifestPath.Any(x => ModExecuter.ModifiedChunks.ContainsKey(x.guid)))
                {
                    var oldManifestChunkCopy = FileSystem.Instance.ManifestChunks.ToArray().ToList();
                    var oldManifestFileCopy = FileSystem.Instance.ManifestFiles.ToArray().ToList();
                    foreach (var chunk in ModExecuter.ModifiedChunks)
                    {
                        var manifestChunk = FileSystem.Instance.ManifestChunks.Single(x => x.guid == chunk.Key);
                        var manifestFileForChunk = FileSystem.Instance.ManifestFiles[manifestChunk.fileIndex];

                        var resolvedCasForChunk = FileSystem.Instance.GetCasFilePath(manifestFileForChunk.file.CatalogIndex, manifestFileForChunk.file.CasIndex, manifestFileForChunk.file.IsInPatch);
                        if (!casFilesToNewChunkPositions.ContainsKey(resolvedCasForChunk))
                            casFilesToNewChunkPositions.Add(resolvedCasForChunk, new List<(ManifestFileInfo, ManifestChunkInfo, ChunkAssetEntry)>());

                        casFilesToNewChunkPositions[resolvedCasForChunk].Add(new(manifestFileForChunk, manifestChunk, chunk.Value));
                    }
                }
            }

            foreach (var casFilesToNewChunkKey in casFilesToNewChunkPositions.Keys)
            {
                var casPath = FileSystem.Instance.ResolvePath(casFilesToNewChunkKey, true);
                if (!File.Exists(casPath))
                    continue;

                using (var nw = new NativeWriter(new FileStream(casPath, FileMode.Open)))
                {
                    var entries = casFilesToNewChunkPositions[casFilesToNewChunkKey];
                    foreach (var entry in entries) 
                    {
                        nw.Position = nw.Length;

                        var data = ModExecuter.archiveData[entry.Item3.Sha1].Data;
                        entry.Item1.ModifiedOffset = (uint)nw.Position;
                        entry.Item1.ModifiedSize = data.Length;
                        nw.Write(data);
                    }
                }
            }

            foreach (var casFilesToNewChunkKey in casFilesToNewChunkPositions.Keys)
            {
                var casPath = FileSystem.Instance.ResolvePath(casFilesToNewChunkKey, true);
                if (!File.Exists(casPath))
                    continue;

                using (var nw = new NativeWriter(new FileStream(casPath, FileMode.Open)))
                {
                    var entries = casFilesToNewChunkPositions[casFilesToNewChunkKey];
                    foreach (var entry in entries)
                    {
                        nw.Position = nw.Length;

                        var data = ModExecuter.archiveData[entry.Item3.Sha1].Data;
                        entry.Item1.ModifiedOffset = (uint)nw.Position;
                        entry.Item1.ModifiedSize = data.Length;
                        nw.Write(data);
                    }
                }
            }
            foreach (var m in FileSystem.Instance.ManifestPaths)
            {
                var resolvedPath =
                        !string.IsNullOrEmpty(ModDirectory)
                        ? m.Replace(FileSystem.Instance.BasePath, Path.Combine(FileSystem.Instance.BasePath, $"{ModDirectory}\\"), StringComparison.OrdinalIgnoreCase)
                        : m;
                //var casPath = FileSystem.Instance.ResolvePath(m, false);
                //if (!File.Exists(casPath))
                //    continue;

                using var nw = new NativeWriter(new FileStream(resolvedPath, FileMode.Open));

                foreach(var manifestChunk in FileSystem.Instance.ManifestPathsToChunks[m])
                {
                    foreach(var item in casFilesToNewChunkPositions.Values)
                    {
                        if (!item.Any(x => x.Item2.guid == manifestChunk.guid))
                            continue;


                        {
                            foreach (var entry in item.Where(x => x.Item1.ModifiedOffset.HasValue && x.Item1.ModifiedSize.HasValue))
                            {
                                nw.Position = entry.Item1.OffsetPosition;
                                nw.Write(entry.Item1.ModifiedOffset.Value);
                                nw.Write(entry.Item1.ModifiedSize.Value);
                            }
                        }

                    }
                }
                
            }





            // write out cas and modify cats
            //foreach (CasDataEntry entry in modExecuter.m_casData.EnumerateEntries())
            //{
            //    if (!entry.HasEntries)
            //        continue;

            //    var pathToDataCat = Path.Combine(FileSystem.Instance.BasePath, ModDirectory, "Data", entry.Catalog, "cas.cat");
            //    var pathToPatchCat = Path.Combine(FileSystem.Instance.BasePath, ModDirectory, "Patch", entry.Catalog, "cas.cat");
            //    var pathToMDDataCat = Path.Combine(FileSystem.Instance.BasePath, ModDirectory, "Data", entry.Catalog, "cas.cat");
            //    var pathToMDPatchCat = Path.Combine(FileSystem.Instance.BasePath, ModDirectory, "Patch", entry.Catalog, "cas.cat");

            //        using (NativeReader reader = new NativeReader(new FileStream(pathToDataCat, FileMode.Open, FileAccess.Read)))
            //        {
            //            FileInfo fi = new FileInfo(pathToMDPatchCat);
            //            if (!fi.Directory.Exists)
            //                Directory.CreateDirectory(fi.Directory.FullName);

            //            using (NativeWriter writer = new NativeWriter(new FileStream(pathToMDPatchCat, FileMode.Create)))
            //            {
            //                writer.Write(reader.ReadBytes(0x23C));
            //                writer.Write(0x00);
            //                writer.Write(0x00);
            //                //if (ProfilesLibrary.IsLoaded(ProfileVersion.MassEffectAndromeda,
            //                //    ProfileVersion.Fifa17,
            //                //    ProfileVersion.Fifa18,
            //                //    ProfileVersion.StarWarsBattlefrontII,
            //                //    ProfileVersion.NeedForSpeedPayback,
            //                //    ProfileVersion.Madden19,
            //                //    ProfileVersion.Battlefield5,
            //                //    ProfileVersion.StarWarsSquadrons))
            //                {
            //                    writer.Write(0x00);
            //                    writer.Write(0x00);
            //                    writer.Write(-1);
            //                    writer.Write(-1);
            //                }
            //            }
            //        }

            //    var pathToMDPatchCatalog = Path.Combine(FileSystem.Instance.BasePath, ModDirectory, "Patch", entry.Catalog);

            //    WriteArchiveData(pathToMDPatchCatalog, entry);
            //}








            return true;
        }

        private void WriteArchiveData(string catalog, CasDataEntry casDataEntry)
        {
            List<int> casEntries = new List<int>();
            int casIndex = 1;
            int totalSize = 0;

            // find the next available cas
        


            // write data to cas
            Stream currentCasStream = null;
            foreach (Sha1 sha1 in casDataEntry.EnumerateDataRefs())
            {
                ArchiveInfo info = ModExecuter.archiveData[sha1];

                int casMaxBytes = 1073741824;
                //switch (Config.Get("MaxCasFileSize", "512MB"))
                //{
                //    case "1GB": casMaxBytes = 1073741824; break;
                //    case "512MB": casMaxBytes = 536870912; break;
                //    case "256MB": casMaxBytes = 268435456; break;
                //}

                // if cas exceeds max size, create a new one (incrementing index)
                if (currentCasStream == null || ((totalSize + info.Data.Length) > casMaxBytes))
                {
                    if (currentCasStream != null)
                    {
                        currentCasStream.Dispose();
                        casIndex++;
                    }

                    FileInfo casFi = new FileInfo(string.Format("{0}\\cas_{1}.cas", catalog, casIndex.ToString("D2")));
                    Directory.CreateDirectory(casFi.DirectoryName);

                    currentCasStream = new FileStream(casFi.FullName, FileMode.Create, FileAccess.Write);
                    totalSize = 0;
                }

                //if (ProfilesLibrary.IsLoaded(ProfileVersion.DragonAgeInquisition, ProfileVersion.Battlefield4, ProfileVersion.NeedForSpeed, ProfileVersion.NeedForSpeedRivals))
                //{
                //    byte[] tmpBuf = new byte[0x20];
                //    using (NativeWriter tmpWriter = new NativeWriter(new MemoryStream(tmpBuf)))
                //    {
                //        tmpWriter.Write(0xF00FCEFA);
                //        tmpWriter.Write(sha1);
                //        tmpWriter.Write((long)info.Data.Length);
                //    }
                //    currentCasStream.Write(tmpBuf, 0, 0x20);
                //    totalSize += 0x20;
                //}

                foreach (CasFileEntry casEntry in casDataEntry.EnumerateFileInfos(sha1))
                {
                    if (casEntry.Entry != null && casEntry.Entry.RangeStart != 0 && !casEntry.FileInfo.isChunk)
                    {
                        casEntry.FileInfo.offset = (uint)(currentCasStream.Position + casEntry.Entry.RangeStart);
                        casEntry.FileInfo.size = (casEntry.Entry.RangeEnd - casEntry.Entry.RangeStart);
                    }
                    else
                    {
                        casEntry.FileInfo.offset = (uint)currentCasStream.Position;
                        casEntry.FileInfo.size = info.Data.Length;
                    }
                    //if (ProfilesLibrary.IsLoaded(ProfileVersion.Battlefield5))
                    //{
                    //    casEntry.FileInfo.file = new ManifestFileRef(casEntry.FileInfo.file.CatalogIndex, false, casIndex);
                    //}
                    //else
                    {
                        casEntry.FileInfo.file = new ManifestFileRef(casEntry.FileInfo.file.CatalogIndex, true, casIndex);
                    }
                }

                currentCasStream.Write(info.Data, 0, info.Data.Length);

                casEntries.Add(casIndex);
                totalSize += info.Data.Length;
            }
            currentCasStream.Dispose();

            FileInfo fi = new FileInfo(string.Format("{0}\\cas.cat", catalog));
            List<CatResourceEntry> entries = new List<CatResourceEntry>();
            List<CatPatchEntry> patchEntries = new List<CatPatchEntry>();
            List<CatResourceEntry> encEntries = new List<CatResourceEntry>();

            // read in original cat
            using (CatReader reader = new CatReader(new FileStream(fi.FullName, FileMode.Open, FileAccess.Read), FileSystem.Instance.CreateDeobfuscator()))
            {
                for (int i = 0; i < reader.ResourceCount; i++)
                    entries.Add(reader.ReadResourceEntry());

                //if (ProfilesLibrary.IsLoaded(ProfileVersion.MassEffectAndromeda,
                //    ProfileVersion.Fifa17,
                //    ProfileVersion.Fifa18,
                //    ProfileVersion.NeedForSpeedPayback,
                //    ProfileVersion.Madden19))
                //{
                //    for (int i = 0; i < reader.EncryptedCount; i++)
                //        encEntries.Add(reader.ReadEncryptedEntry());
                //}

                for (int i = 0; i < reader.PatchCount; i++)
                    patchEntries.Add(reader.ReadPatchEntry());

                reader.Position = 0;
            }

            // write out new modified cat
            using (NativeWriter writer = new NativeWriter(new FileStream(fi.FullName, FileMode.Create)))
            {
                //    int numEntries = 0;
                //    int numPatchEntries = 0;

                //    MemoryStream ms = new MemoryStream();
                //    using NativeWriter tmpWriter = new NativeWriter(ms);
                //    //{
                //        // unmodified entries
                //        foreach (CatResourceEntry entry in entries)
                //        {
                //            //if (casDataEntry.Contains(entry.Sha1))
                //            //    continue;

                //            tmpWriter.Write(entry.Sha1);
                //            tmpWriter.Write(entry.Offset);
                //            tmpWriter.Write(entry.Size);
                //            //if (!ProfilesLibrary.IsLoaded(ProfileVersion.DragonAgeInquisition,
                //            //    ProfileVersion.Battlefield4,
                //            //    ProfileVersion.NeedForSpeed,
                //            //    ProfileVersion.NeedForSpeedRivals))
                //            {
                //                tmpWriter.Write(entry.LogicalOffset);
                //            }
                //            tmpWriter.Write(entry.ArchiveIndex);
                //            numEntries++;
                //        }

                //        int offset = 0;
                //        int index = 0;

                //        // new entries
                //        foreach (Sha1 sha1 in casDataEntry.EnumerateDataRefs())
                //        {
                //            //if (ProfilesLibrary.IsLoaded(ProfileVersion.DragonAgeInquisition,
                //            //    ProfileVersion.Battlefield4,
                //            //    ProfileVersion.NeedForSpeed,
                //            //    ProfileVersion.NeedForSpeedRivals))
                //            //{
                //            //    offset += 0x20;
                //            //}

                //            ArchiveInfo info = ModExecuter.archiveData[sha1];

                //            tmpWriter.Write(sha1);
                //            tmpWriter.Write(offset);
                //            tmpWriter.Write(info.Data.Length);
                //            //if (!ProfilesLibrary.IsLoaded(ProfileVersion.DragonAgeInquisition,
                //            //    ProfileVersion.Battlefield4,
                //            //    ProfileVersion.NeedForSpeed,
                //            //    ProfileVersion.NeedForSpeedRivals))
                //            {
                //                tmpWriter.Write(0x00);
                //            }
                //            tmpWriter.Write(casEntries[index++]);

                //            offset += info.Data.Length;
                //            numEntries++;
                //        }

                //        //if (ProfilesLibrary.IsLoaded(ProfileVersion.MassEffectAndromeda,
                //        //    ProfileVersion.Fifa17,
                //        //    ProfileVersion.Fifa18,
                //        //    ProfileVersion.NeedForSpeedPayback,
                //        //    ProfileVersion.Madden19))
                //        //{
                //        //    // encrypted entries
                //        //    foreach (CatResourceEntry entry in encEntries)
                //        //    {
                //        //        tmpWriter.Write(entry.Sha1);
                //        //        tmpWriter.Write(entry.Offset);
                //        //        tmpWriter.Write(entry.EncryptedSize);
                //        //        tmpWriter.Write(entry.LogicalOffset);
                //        //        tmpWriter.Write(entry.ArchiveIndex);
                //        //        tmpWriter.Write(entry.Unknown);
                //        //        tmpWriter.WriteFixedSizedString(entry.KeyId, entry.KeyId.Length);
                //        //        tmpWriter.Write(entry.UnknownData);
                //        //    }
                //        //}

                //        // unmodified patch entries
                //        foreach (CatPatchEntry entry in patchEntries)
                //        {
                //            //if (casDataEntry.Contains(entry.Sha1))
                //            //    continue;

                //            tmpWriter.Write(entry.Sha1);
                //            tmpWriter.Write(entry.BaseSha1);
                //            tmpWriter.Write(entry.DeltaSha1);
                //            numPatchEntries++;
                //        }

                //        // write it to file
                //        //if (!ProfilesLibrary.IsLoaded(ProfileVersion.DragonAgeInquisition,
                //        //    ProfileVersion.Battlefield4,
                //        //    ProfileVersion.NeedForSpeed,
                //        //    ProfileVersion.NeedForSpeedRivals))
                //        {
                //            writer.Write(header);
                //        }
                //        writer.WriteFixedSizedString("NyanNyanNyanNyan", 16);
                //        //if (!ProfilesLibrary.IsLoaded(ProfileVersion.DragonAgeInquisition,
                //        //    ProfileVersion.Battlefield4,
                //        //    ProfileVersion.NeedForSpeed,
                //        //    ProfileVersion.NeedForSpeedRivals))
                //        //{
                //            writer.Write(numEntries);
                //            writer.Write(numPatchEntries);
                //            //if (ProfilesLibrary.IsLoaded(ProfileVersion.MassEffectAndromeda,
                //            //    ProfileVersion.Fifa17,
                //            //    ProfileVersion.Fifa18,
                //            //    ProfileVersion.StarWarsBattlefrontII,
                //            //    ProfileVersion.NeedForSpeedPayback,
                //            //    ProfileVersion.Madden19,
                //            //    ProfileVersion.Battlefield5,
                //            //    ProfileVersion.StarWarsSquadrons))
                //            {
                //                writer.Write(encEntries.Count);
                //                writer.Write(0x00);
                //                writer.Write(-1);
                //                writer.Write(-1);
                //            }
                //        //}
                //        writer.Write(ms.ToArray());
                //    //}

            }
            TOCFile.RebuildTOCSignatureOnly(fi.FullName);

        }

        private class ManifestBundleAction
        {
            private static readonly object resourceLock = new object();

            public List<Sha1> DataRefs { get; } = new List<Sha1>();
            public List<Sha1> BundleRefs { get; } = new List<Sha1>();
            public List<CasFileEntry> FileInfos { get; } = new List<CasFileEntry>();
            public List<byte[]> BundleBuffers { get; } = new List<byte[]>();

            public Exception Exception { get; private set; }

            private List<ModBundleInfo> bundles;
            private ManualResetEvent doneEvent;
            private ModExecutor parent;
            private CancellationToken cancelToken;

            public ManifestBundleAction(List<ModBundleInfo> inBundles, ManualResetEvent inDoneEvent, ModExecutor inParent, CancellationToken inCancelToken)
            {
                bundles = inBundles;
                doneEvent = inDoneEvent;
                parent = inParent;
                cancelToken = inCancelToken;
            }

            public void Run()
            {
                //                try
                //                {
                FileSystem fs = FileSystem.Instance;

                foreach (ModBundleInfo bundle in bundles)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    List<Sha1> sha1s = new List<Sha1>();

                    ManifestBundleInfo manifestBundle = fs.GetManifestBundle(bundle.Name);
                    ManifestFileInfo bundleFile = null;
                    DbObject bundleObj = null;

                    if (manifestBundle.files.Count == 0)
                    {
                        bundleFile = new ManifestFileInfo() { file = new ManifestFileRef(1, false, 0) };
                        manifestBundle.files.Add(bundleFile);

                        bundleObj = new DbObject();
                        bundleObj.SetValue("ebx", DbObject.CreateList());
                        bundleObj.SetValue("res", DbObject.CreateList());
                        bundleObj.SetValue("chunks", DbObject.CreateList());
                        bundleObj.SetValue("chunkMeta", DbObject.CreateList());
                    }
                    else
                    {
                        bundleFile = manifestBundle.files[0];
                        string catalogName = fs.GetCatalog(bundleFile.file);
                        List<ManifestFileInfo> mfi = new List<ManifestFileInfo>();

                        for (int i = 1; i < manifestBundle.files.Count; i++)
                        {
                            ManifestFileInfo fi = manifestBundle.files[i];

                            string catFile = fs.ResolvePath(((fi.file.IsInPatch) ? "native_patch/" : "native_data/") + fs.GetCatalog(fi.file) + "/cas.cat");
                            int catFileHash = Fnv1.HashString(catFile.ToLower());
                        }
                    }
                }

                            //                                Dictionary<uint, CatResourceEntry> casList = parent.Resources[catFileHash][fi.file.CasIndex];
                            //                                List<uint> offsets = casList.Keys.ToList();

                            //                                uint totalSize = 0;
                            //                                uint offset = fi.offset;

                            //                                mfi.Add(fi);
                            //                                sha1s.Add(Sha1.Zero);

                            //                                if (!casList.ContainsKey(offset))
                            //                                {
                            //                                    offset += (uint)fi.size;

                            //                                    int nextIdx = (!casList.ContainsKey(offset)) ? casList.Count : offsets.BinarySearch(offset);
                            //                                    while (offset > fi.offset)
                            //                                    {
                            //                                        nextIdx--;
                            //                                        offset = offsets[nextIdx];
                            //                                    }

                            //                                    fi.size += fi.offset - offset;
                            //                                }

                            //                                CatResourceEntry entry = casList[offset];
                            //                                totalSize += entry.Size;
                            //                                offset += entry.Size;

                            //                                long size = fi.size;
                            //                                fi.size = offset - fi.offset;

                            //                                sha1s[sha1s.Count - 1] = entry.Sha1;
                            //                                Debug.Assert(entry.Offset <= fi.offset);

                            //                                int subIdx = i;
                            //                                while (totalSize != size)
                            //                                {
                            //                                    //Debug.Assert(totalSize <= size);
                            //                                    CatResourceEntry nextEntry = casList[offset];
                            //                                    {
                            //                                        ManifestFileInfo newFi = new ManifestFileInfo
                            //                                        {
                            //                                            file = new ManifestFileRef(fi.file.CatalogIndex, fi.file.IsInPatch, fi.file.CasIndex),
                            //                                            offset = nextEntry.Offset,
                            //                                            size = nextEntry.Size
                            //                                        };
                            //#if FROSTY_DEVELOPER
                            //                                        sha1s.Add(nextEntry.Sha1);
                            //#endif
                            //                                        mfi.Add(newFi);

                            //                                        totalSize += nextEntry.Size;
                            //                                        offset += nextEntry.Size;
                            //                                    }
                            //                                }
                            //                            }

                            //                            manifestBundle.files.Clear();
                            //                            manifestBundle.files.Add(bundleFile);
                            //                            manifestBundle.files.AddRange(mfi);

                            //                            using (NativeReader reader = new NativeReader(new FileStream(fs.ResolvePath(bundleFile.file), FileMode.Open, FileAccess.Read)))
                            //                            {
                            //                                using (BinarySbReader sbReader = new BinarySbReader(reader.CreateViewStream(bundleFile.offset, bundleFile.size), 0, new NullDeobfuscator()))
                            //                                    bundleObj = sbReader.ReadDbObject();
                            //                            }
                            //                        }

                            //                        int idx = 1;
                            //                        foreach (DbObject ebx in bundleObj.GetValue<DbObject>("ebx"))
                            //                        {
                            //                            string name = ebx.GetValue<string>("name");
                            //                            if (bundle.Modify.Ebx.Contains(name))
                            //                            {
                            //                                ManifestFileInfo fi = manifestBundle.files[idx];
                            //                                EbxAssetEntry entry = parent.modifiedEbx[name];

                            //                                ebx.SetValue("sha1", entry.Sha1);
                            //                                ebx.SetValue("originalSize", entry.OriginalSize);

                            //                                DataRefs.Add(entry.Sha1);
                            //                                FileInfos.Add(new CasFileEntry() { Entry = null, FileInfo = fi });
                            //                            }
                            //                            idx++;
                            //                        }
                            //                        foreach (string name in bundle.Add.Ebx)
                            //                        {
                            //                            EbxAssetEntry entry = parent.modifiedEbx[name];

                            //                            DbObject ebx = new DbObject();
                            //                            ebx.SetValue("name", entry.Name);
                            //                            ebx.SetValue("sha1", entry.Sha1);
                            //                            ebx.SetValue("originalSize", entry.OriginalSize);
                            //                            bundleObj.GetValue<DbObject>("ebx").Add(ebx);

                            //                            ManifestFileInfo fi = new ManifestFileInfo { file = new ManifestFileRef(bundleFile.file.CatalogIndex, false, 0) };
                            //                            manifestBundle.files.Insert(idx++, fi);

                            //                            DataRefs.Add(entry.Sha1);
                            //                            FileInfos.Add(new CasFileEntry() { Entry = null, FileInfo = fi });
                            //                        }
                            //                        foreach (DbObject res in bundleObj.GetValue<DbObject>("res"))
                            //                        {
                            //                            string name = res.GetValue<string>("name");
                            //                            if (bundle.Modify.Res.Contains(name))
                            //                            {
                            //                                ManifestFileInfo fi = manifestBundle.files[idx];
                            //                                ResAssetEntry entry = parent.modifiedRes[name];

                            //                                //if (entry.ExtraData != null)
                            //                                //{
                            //                                //    lock (resourceLock)
                            //                                //    {
                            //                                //        // invoke custom handler to modify the base data with the custom data
                            //                                //        HandlerExtraData extraData = (HandlerExtraData)entry.ExtraData;
                            //                                //        if (extraData != null)
                            //                                //        {
                            //                                //            byte[] data = null;
                            //                                //            Stream baseData = parent.rm.GetResourceData(parent.fs.GetFilePath(fi.file.CatalogIndex, fi.file.CasIndex, fi.file.IsInPatch), fi.offset, fi.size);
                            //                                //            ResAssetEntry newEntry = (ResAssetEntry)extraData.Handler.Modify(entry, baseData, extraData.Data, out data);

                            //                                //            if (!parent.archiveData.ContainsKey(newEntry.Sha1))
                            //                                //                parent.archiveData.Add(newEntry.Sha1, new ArchiveInfo() { Data = data, RefCount = 1 });

                            //                                //            entry.Sha1 = newEntry.Sha1;
                            //                                //            entry.OriginalSize = newEntry.OriginalSize;
                            //                                //            entry.ResMeta = newEntry.ResMeta;
                            //                                //            entry.ExtraData = null;
                            //                                //        }
                            //                                //    }
                            //                                //}

                            //                                res.SetValue("sha1", entry.Sha1);
                            //                                res.SetValue("originalSize", entry.OriginalSize);
                            //                                if (entry.ResMeta != null)
                            //                                    res.SetValue("resMeta", entry.ResMeta);

                            //                                DataRefs.Add(entry.Sha1);
                            //                                FileInfos.Add(new CasFileEntry() { Entry = null, FileInfo = fi });
                            //                            }
                            //                            idx++;
                            //                        }
                            //                        foreach (string name in bundle.Add.Res)
                            //                        {
                            //                            ResAssetEntry entry = parent.modifiedRes[name];

                            //                            DbObject res = new DbObject();
                            //                            res.SetValue("name", entry.Name);
                            //                            res.SetValue("sha1", entry.Sha1);
                            //                            res.SetValue("originalSize", entry.OriginalSize);
                            //                            res.SetValue("resRid", (long)entry.ResRid);
                            //                            res.SetValue("resType", entry.ResType);
                            //                            res.SetValue("resMeta", entry.ResMeta);
                            //                            bundleObj.GetValue<DbObject>("res").Add(res);
                            //                            ManifestFileInfo fi = new ManifestFileInfo { file = new ManifestFileRef(bundleFile.file.CatalogIndex, false, 0) };
                            //                            manifestBundle.files.Insert(idx++, fi);

                            //                            DataRefs.Add(entry.Sha1);
                            //                            FileInfos.Add(new CasFileEntry() { Entry = null, FileInfo = fi });
                            //                        }

                            //                        DbObject chunkMeta = bundleObj.GetValue<DbObject>("chunkMeta");

                            //                        // modify chunks
                            //                        int chunkIndex = 0;
                            //                        List<int> chunksToRemove = new List<int>();
                            //                        foreach (DbObject chunk in bundleObj.GetValue<DbObject>("chunks"))
                            //                        {
                            //                            Guid name = chunk.GetValue<Guid>("id");
                            //                            if (bundle.Remove.Chunks.Contains(name))
                            //                            {
                            //                                chunksToRemove.Add(chunkIndex);
                            //                            }
                            //                            else if (bundle.Modify.Chunks.Contains(name))
                            //                            {
                            //                                ChunkAssetEntry entry = parent.ModifiedChunks[name];
                            //                                DbObject meta = chunkMeta.Find<DbObject>((object a) => { return (a as DbObject).GetValue<int>("h32") == entry.H32; });

                            //                                chunk.SetValue("sha1", entry.Sha1);
                            //                                chunk.SetValue("logicalOffset", entry.LogicalOffset);
                            //                                chunk.SetValue("logicalSize", entry.LogicalSize);
                            //                                if (entry.FirstMip != -1)
                            //                                {
                            //                                    chunk.SetValue("rangeStart", entry.RangeStart);
                            //                                    chunk.SetValue("rangeEnd", entry.RangeEnd);

                            //                                    meta?.GetValue<DbObject>("meta").SetValue("firstMip", entry.FirstMip);
                            //                                }

                            //                                if (idx < manifestBundle.files.Count)
                            //                                {
                            //                                    DataRefs.Add(entry.Sha1);
                            //                                    ManifestFileInfo fi = manifestBundle.files[idx];
                            //                                    FileInfos.Add(new CasFileEntry() { Entry = entry, FileInfo = fi });
                            //                                }
                            //                            }

                            //                            idx++;
                            //                            chunkIndex++;
                            //                        }
                            //                        chunksToRemove.Reverse();
                            //                        foreach (int index in chunksToRemove)
                            //                        {
                            //                            bundleObj.GetValue<DbObject>("chunks").RemoveAt(index);
                            //                            bundleObj.GetValue<DbObject>("chunkMeta").RemoveAt(index);
                            //                            manifestBundle.files.RemoveAt(index + idx);
                            //                        }
                            //                        foreach (Guid name in bundle.Add.Chunks)
                            //                        {
                            //                            ChunkAssetEntry entry = parent.ModifiedChunks[name];

                            //                            DbObject chunk = new DbObject();
                            //                            chunk.SetValue("id", name);
                            //                            chunk.SetValue("sha1", entry.Sha1);
                            //                            chunk.SetValue("logicalOffset", entry.LogicalOffset);
                            //                            chunk.SetValue("logicalSize", entry.LogicalSize);

                            //                            DbObject meta = new DbObject();
                            //                            meta.SetValue("h32", entry.H32);
                            //                            meta.SetValue("meta", new DbObject());
                            //                            chunkMeta.Add(meta);

                            //                            if (entry.FirstMip != -1)
                            //                            {
                            //                                chunk.SetValue("rangeStart", entry.RangeStart);
                            //                                chunk.SetValue("rangeEnd", entry.RangeEnd);
                            //                                meta.GetValue<DbObject>("meta").SetValue("firstMip", entry.FirstMip);
                            //                            }

                            //                            bundleObj.GetValue<DbObject>("chunks").Add(chunk);

                            //                            ManifestFileInfo fi = new ManifestFileInfo { file = new ManifestFileRef(bundleFile.file.CatalogIndex, false, 0) };
                            //                            manifestBundle.files.Insert(idx++, fi);

                            //                            DataRefs.Add(entry.Sha1);
                            //                            FileInfos.Add(new CasFileEntry() { Entry = entry, FileInfo = fi });
                            //                        }

                            //                        // finally write out new binary superbundle
                            //                        MemoryStream ms = new MemoryStream();
                            //                        using (BinarySbWriter writer = new BinarySbWriter(ms, true))
                            //                        {
                            //                            writer.Write(bundleObj);
                            //                        }

                            //                        byte[] bundleBuffer = ms.ToArray();
                            //                        Sha1 newSha1 = Sha1.Create(bundleBuffer);
                            //                        ms.Dispose();

                            //                        BundleRefs.Add(newSha1);
                            //                        DataRefs.Add(newSha1);
                            //                        FileInfos.Add(new CasFileEntry { Entry = null, FileInfo = bundleFile });
                            //                        BundleBuffers.Add(bundleBuffer);
                            //                    }
                            //                }
                            //                catch (Exception e)
                            //                {
                            //                    Exception = e;
                            //                }
                        }
        }
    }
}
