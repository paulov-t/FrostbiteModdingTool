using FMT.FileTools;
using Frostbite.Deobfuscators;
using FrostySdk;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ModdingSupport.ModExecutor;

namespace StarWarsSquadronsPlugin
{
    public class SWSCompiler : Frostbite2022AssetCompiler, IAssetCompiler
    {

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
            if (modExecuter.m_modifiedSuperBundles.ContainsKey(0))
            {
                foreach (Guid id in modExecuter.m_modifiedSuperBundles[0].Modify.Chunks)
                {
                    ChunkAssetEntry entry = modExecuter.ModifiedChunks[id];
                    ManifestChunkInfo ci = FileSystem.Instance.GetManifestChunk(entry.Id);

                    if (ci != null)
                    {
                        //if (entry.TocChunkSpecialHack)
                        {
                            // change to using the first catalog
                            ci.file.file = new ManifestFileRef(0, false, 0);
                        }

                        modExecuter.m_casData.Add(FileSystem.Instance.GetCatalog(ci.file.file), entry.Sha1, entry, ci.file);
                    }
                }
                foreach (Guid id in modExecuter.m_modifiedSuperBundles[0].Add.Chunks)
                {
                    ChunkAssetEntry entry = modExecuter.ModifiedChunks[id];

                    ManifestChunkInfo ci = new ManifestChunkInfo
                    {
                        guid = entry.Id,
                        file = new ManifestFileInfo { file = new ManifestFileRef(0, false, 0), isChunk = true }
                    };
                    FileSystem.Instance.AddManifestChunk(ci);

                    modExecuter.m_casData.Add(FileSystem.Instance.GetCatalog(ci.file.file), entry.Sha1, entry, ci.file);
                }
            }

            return true;
        }

        private class ManifestBundleAction
        {
            private Dictionary<int, Dictionary<int, Dictionary<uint, CatResourceEntry>>> m_resources = new Dictionary<int, Dictionary<int, Dictionary<uint, CatResourceEntry>>>();

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
                try
                {
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

                                Dictionary<uint, CatResourceEntry> casList = m_resources[catFileHash][fi.file.CasIndex];
                                List<uint> offsets = casList.Keys.ToList();

                                uint totalSize = 0;
                                uint offset = fi.offset;

                                mfi.Add(fi);
                                sha1s.Add(Sha1.Zero);

                                if (!casList.ContainsKey(offset))
                                {
                                    offset += (uint)fi.size;

                                    int nextIdx = (!casList.ContainsKey(offset)) ? casList.Count : offsets.BinarySearch(offset);
                                    while (offset > fi.offset)
                                    {
                                        nextIdx--;
                                        offset = offsets[nextIdx];
                                    }

                                    fi.size += fi.offset - offset;
                                }

                                CatResourceEntry entry = casList[offset];
                                totalSize += entry.Size;
                                offset += entry.Size;

                                long size = fi.size;
                                fi.size = offset - fi.offset;

                                sha1s[sha1s.Count - 1] = entry.Sha1;
                                Debug.Assert(entry.Offset <= fi.offset);

                                int subIdx = i;
                                while (totalSize != size)
                                {
                                    Debug.Assert(totalSize <= size);
                                    CatResourceEntry nextEntry = casList[offset];
                                    {
                                        ManifestFileInfo newFi = new ManifestFileInfo
                                        {
                                            file = new ManifestFileRef(fi.file.CatalogIndex, fi.file.IsInPatch, fi.file.CasIndex),
                                            offset = nextEntry.Offset,
                                            size = nextEntry.Size
                                        };
#if FROSTY_DEVELOPER
                                        sha1s.Add(nextEntry.Sha1);
#endif
                                        mfi.Add(newFi);

                                        totalSize += nextEntry.Size;
                                        offset += nextEntry.Size;
                                    }
                                }
                            }

                            manifestBundle.files.Clear();
                            manifestBundle.files.Add(bundleFile);
                            manifestBundle.files.AddRange(mfi);

                            using (NativeReader reader = new NativeReader(new FileStream(fs.ResolvePath(bundleFile.file), FileMode.Open, FileAccess.Read)))
                            {
                                using (BinarySbReader sbReader = new BinarySbReader(reader.CreateViewStream(bundleFile.offset, bundleFile.size), 0, new NullDeobfuscator()))
                                    bundleObj = sbReader.ReadDbObject();
                            }
                        }

                        int idx = 1;
                        foreach (DbObject ebx in bundleObj.GetValue<DbObject>("ebx"))
                        {
                            string name = ebx.GetValue<string>("name");
                            if (bundle.Modify.Ebx.Contains(name))
                            {
                                ManifestFileInfo fi = manifestBundle.files[idx];
                                EbxAssetEntry entry = parent.modifiedEbx[name];

                                ebx.SetValue("sha1", entry.Sha1);
                                ebx.SetValue("originalSize", entry.OriginalSize);

                                DataRefs.Add(entry.Sha1);
                                FileInfos.Add(new CasFileEntry() { Entry = null, FileInfo = fi });
                            }
                            idx++;
                        }
                        foreach (string name in bundle.Add.Ebx)
                        {
                            EbxAssetEntry entry = parent.modifiedEbx[name];

                            DbObject ebx = new DbObject();
                            ebx.SetValue("name", entry.Name);
                            ebx.SetValue("sha1", entry.Sha1);
                            ebx.SetValue("originalSize", entry.OriginalSize);
                            bundleObj.GetValue<DbObject>("ebx").Add(ebx);

                            ManifestFileInfo fi = new ManifestFileInfo { file = new ManifestFileRef(bundleFile.file.CatalogIndex, false, 0) };
                            manifestBundle.files.Insert(idx++, fi);

                            DataRefs.Add(entry.Sha1);
                            FileInfos.Add(new CasFileEntry() { Entry = null, FileInfo = fi });
                        }
                        foreach (DbObject res in bundleObj.GetValue<DbObject>("res"))
                        {
                            string name = res.GetValue<string>("name");
                            if (bundle.Modify.Res.Contains(name))
                            {
                                ManifestFileInfo fi = manifestBundle.files[idx];
                                ResAssetEntry entry = parent.modifiedRes[name];

                                //if (entry.ExtraData != null)
                                //{
                                //    lock (resourceLock)
                                //    {
                                //        // invoke custom handler to modify the base data with the custom data
                                //        HandlerExtraData extraData = (HandlerExtraData)entry.ExtraData;
                                //        if (extraData != null)
                                //        {
                                //            byte[] data = null;
                                //            Stream baseData = parent.rm.GetResourceData(parent.fs.GetFilePath(fi.file.CatalogIndex, fi.file.CasIndex, fi.file.IsInPatch), fi.offset, fi.size);
                                //            ResAssetEntry newEntry = (ResAssetEntry)extraData.Handler.Modify(entry, baseData, extraData.Data, out data);

                                //            if (!parent.archiveData.ContainsKey(newEntry.Sha1))
                                //                parent.archiveData.Add(newEntry.Sha1, new ArchiveInfo() { Data = data, RefCount = 1 });

                                //            entry.Sha1 = newEntry.Sha1;
                                //            entry.OriginalSize = newEntry.OriginalSize;
                                //            entry.ResMeta = newEntry.ResMeta;
                                //            entry.ExtraData = null;
                                //        }
                                //    }
                                //}

                                res.SetValue("sha1", entry.Sha1);
                                res.SetValue("originalSize", entry.OriginalSize);
                                if (entry.ResMeta != null)
                                    res.SetValue("resMeta", entry.ResMeta);

                                DataRefs.Add(entry.Sha1);
                                FileInfos.Add(new CasFileEntry() { Entry = null, FileInfo = fi });
                            }
                            idx++;
                        }
                        foreach (string name in bundle.Add.Res)
                        {
                            ResAssetEntry entry = parent.modifiedRes[name];

                            DbObject res = new DbObject();
                            res.SetValue("name", entry.Name);
                            res.SetValue("sha1", entry.Sha1);
                            res.SetValue("originalSize", entry.OriginalSize);
                            res.SetValue("resRid", (long)entry.ResRid);
                            res.SetValue("resType", entry.ResType);
                            res.SetValue("resMeta", entry.ResMeta);
                            bundleObj.GetValue<DbObject>("res").Add(res);
                            ManifestFileInfo fi = new ManifestFileInfo { file = new ManifestFileRef(bundleFile.file.CatalogIndex, false, 0) };
                            manifestBundle.files.Insert(idx++, fi);

                            DataRefs.Add(entry.Sha1);
                            FileInfos.Add(new CasFileEntry() { Entry = null, FileInfo = fi });
                        }

                        DbObject chunkMeta = bundleObj.GetValue<DbObject>("chunkMeta");

                        // modify chunks
                        int chunkIndex = 0;
                        List<int> chunksToRemove = new List<int>();
                        foreach (DbObject chunk in bundleObj.GetValue<DbObject>("chunks"))
                        {
                            Guid name = chunk.GetValue<Guid>("id");
                            if (bundle.Remove.Chunks.Contains(name))
                            {
                                chunksToRemove.Add(chunkIndex);
                            }
                            else if (bundle.Modify.Chunks.Contains(name))
                            {
                                ChunkAssetEntry entry = parent.ModifiedChunks[name];
                                DbObject meta = chunkMeta.Find<DbObject>((object a) => { return (a as DbObject).GetValue<int>("h32") == entry.H32; });

                                chunk.SetValue("sha1", entry.Sha1);
                                chunk.SetValue("logicalOffset", entry.LogicalOffset);
                                chunk.SetValue("logicalSize", entry.LogicalSize);
                                if (entry.FirstMip != -1)
                                {
                                    chunk.SetValue("rangeStart", entry.RangeStart);
                                    chunk.SetValue("rangeEnd", entry.RangeEnd);

                                    meta?.GetValue<DbObject>("meta").SetValue("firstMip", entry.FirstMip);
                                }

                                if (idx < manifestBundle.files.Count)
                                {
                                    DataRefs.Add(entry.Sha1);
                                    ManifestFileInfo fi = manifestBundle.files[idx];
                                    FileInfos.Add(new CasFileEntry() { Entry = entry, FileInfo = fi });
                                }
                            }

                            idx++;
                            chunkIndex++;
                        }
                        chunksToRemove.Reverse();
                        foreach (int index in chunksToRemove)
                        {
                            bundleObj.GetValue<DbObject>("chunks").RemoveAt(index);
                            bundleObj.GetValue<DbObject>("chunkMeta").RemoveAt(index);
                            manifestBundle.files.RemoveAt(index + idx);
                        }
                        foreach (Guid name in bundle.Add.Chunks)
                        {
                            ChunkAssetEntry entry = parent.ModifiedChunks[name];

                            DbObject chunk = new DbObject();
                            chunk.SetValue("id", name);
                            chunk.SetValue("sha1", entry.Sha1);
                            chunk.SetValue("logicalOffset", entry.LogicalOffset);
                            chunk.SetValue("logicalSize", entry.LogicalSize);

                            DbObject meta = new DbObject();
                            meta.SetValue("h32", entry.H32);
                            meta.SetValue("meta", new DbObject());
                            chunkMeta.Add(meta);

                            if (entry.FirstMip != -1)
                            {
                                chunk.SetValue("rangeStart", entry.RangeStart);
                                chunk.SetValue("rangeEnd", entry.RangeEnd);
                                meta.GetValue<DbObject>("meta").SetValue("firstMip", entry.FirstMip);
                            }

                            bundleObj.GetValue<DbObject>("chunks").Add(chunk);

                            ManifestFileInfo fi = new ManifestFileInfo { file = new ManifestFileRef(bundleFile.file.CatalogIndex, false, 0) };
                            manifestBundle.files.Insert(idx++, fi);

                            DataRefs.Add(entry.Sha1);
                            FileInfos.Add(new CasFileEntry() { Entry = entry, FileInfo = fi });
                        }

                        // finally write out new binary superbundle
                        MemoryStream ms = new MemoryStream();
                        using (BinarySbWriter writer = new BinarySbWriter(ms, true))
                        {
                            writer.Write(bundleObj);
                        }

                        byte[] bundleBuffer = ms.ToArray();
                        Sha1 newSha1 = Sha1.Create(bundleBuffer);
                        ms.Dispose();

                        BundleRefs.Add(newSha1);
                        DataRefs.Add(newSha1);
                        FileInfos.Add(new CasFileEntry { Entry = null, FileInfo = bundleFile });
                        BundleBuffers.Add(bundleBuffer);
                    }
                }
                catch (Exception e)
                {
                    Exception = e;
                }
            }
        }
    }
}
