//using FMT.FileTools;
//using FrostySdk.IO;
//using FrostySdk.Managers;
//using ModdingSupport;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using static ModdingSupport.ModExecutor;

//namespace FrostySdk.Frostbite.Compilers
//{
//    public class ManifestBundleAction
//    {
//        private static readonly object resourceLock = new object();

//        private List<ModBundleInfo> bundles;

//        private ManualResetEvent doneEvent;

//        private ModExecutor parent;

//        private Exception errorException;

//        private List<FMT.FileTools.Sha1> dataRefs = new List<FMT.FileTools.Sha1>();

//        private List<FMT.FileTools.Sha1> bundleRefs = new List<FMT.FileTools.Sha1>();

//        private List<CasFileEntry> fileInfos = new List<CasFileEntry>();

//        private List<byte[]> bundleBuffers = new List<byte[]>();

//        public List<FMT.FileTools.Sha1> DataRefs => dataRefs;

//        public List<FMT.FileTools.Sha1> BundleRefs => bundleRefs;

//        public List<CasFileEntry> FileInfos => fileInfos;

//        public List<byte[]> BundleBuffers => bundleBuffers;

//        public bool HasErrored => errorException != null;

//        public Exception Exception => errorException;

//        public ManifestBundleAction(List<ModBundleInfo> inBundles, ManualResetEvent inDoneEvent, ModExecutor inParent)
//        {
//            bundles = inBundles;
//            doneEvent = inDoneEvent;
//            parent = inParent;
//        }

//        public void Run()
//        {
//            try
//            {
//                FileSystem fs = parent.fs;
//                foreach (ModBundleInfo bundle in bundles)
//                {
//                    ManifestBundleInfo manifestBundle = fs.GetManifestBundle(bundle.Name);
//                    ManifestFileInfo manifestFileInfo = null;
//                    DbObject dbObject = null;
//                    if (manifestBundle.files.Count == 0)
//                    {
//                        manifestFileInfo = new ManifestFileInfo
//                        {
//                            file = new ManifestFileRef(1, inPatch: false, inCasIndex: 0)
//                        };
//                        manifestBundle.files.Add(manifestFileInfo);
//                        dbObject = new DbObject();
//                        dbObject.SetValue("ebx", DbObject.CreateList());
//                        dbObject.SetValue("res", DbObject.CreateList());
//                        dbObject.SetValue("chunks", DbObject.CreateList());
//                        dbObject.SetValue("chunkMeta", DbObject.CreateList());
//                    }
//                    else
//                    {
//                        manifestFileInfo = manifestBundle.files[0];
//                        fs.GetCatalog(manifestFileInfo.file);
//                        List<ManifestFileInfo> list = new List<ManifestFileInfo>();
//                        for (int i = 1; i < manifestBundle.files.Count; i++)
//                        {
//                            ManifestFileInfo manifestFileInfo2 = manifestBundle.files[i];
//                            int hashedPath = Fnv1a.HashString(fs.ResolvePath((manifestFileInfo2.file.IsInPatch ? "native_patch/" : "native_data/") + fs.GetCatalog(manifestFileInfo2.file) + "/cas.cat").ToLower());
//                            Dictionary<uint, CatResourceEntry> dictionary = parent.Resources[hashedPath][manifestFileInfo2.file.CasIndex];
//                            List<uint> list2 = dictionary.Keys.ToList();
//                            uint num = 0u;
//                            uint num2 = manifestFileInfo2.offset;
//                            list.Add(manifestFileInfo2);
//                            if (!dictionary.ContainsKey(num2))
//                            {
//                                num2 = (uint)((int)num2 + (int)manifestFileInfo2.size);
//                                int num3 = (!dictionary.ContainsKey(num2)) ? dictionary.Count : list2.BinarySearch(num2);
//                                while (num2 > manifestFileInfo2.offset)
//                                {
//                                    num3--;
//                                    num2 = list2[num3];
//                                }
//                                manifestFileInfo2.size += manifestFileInfo2.offset - num2;
//                            }
//                            CatResourceEntry catResourceEntry = dictionary[num2];
//                            num += catResourceEntry.Size;
//                            num2 += catResourceEntry.Size;
//                            long size = manifestFileInfo2.size;
//                            manifestFileInfo2.size = num2 - manifestFileInfo2.offset;
//                            while (num != size)
//                            {
//                                CatResourceEntry catResourceEntry2 = dictionary[num2];
//                                ManifestFileInfo manifestFileInfo3 = new ManifestFileInfo();
//                                manifestFileInfo3.file = new ManifestFileRef(manifestFileInfo2.file.CatalogIndex, manifestFileInfo2.file.IsInPatch, manifestFileInfo2.file.CasIndex);
//                                manifestFileInfo3.offset = catResourceEntry2.Offset;
//                                manifestFileInfo3.size = catResourceEntry2.Size;
//                                list.Add(manifestFileInfo3);
//                                num += catResourceEntry2.Size;
//                                num2 += catResourceEntry2.Size;
//                            }
//                        }
//                        manifestBundle.files.Clear();
//                        manifestBundle.files.Add(manifestFileInfo);
//                        manifestBundle.files.AddRange(list);
//                        using (NativeReader nativeReader = new NativeReader(new FileStream(fs.ResolvePath(manifestFileInfo.file), FileMode.Open, FileAccess.Read)))
//                        {
//                            using (BinarySbReader binarySbReader = new BinarySbReader(nativeReader.CreateViewStream(manifestFileInfo.offset, manifestFileInfo.size), 0L, null))
//                            {
//                                dbObject = binarySbReader.ReadDbObject();
//                            }
//                        }
//                    }
//                    int num4 = 1;
//                    foreach (DbObject item2 in dbObject.GetValue<DbObject>("ebx"))
//                    {
//                        string value = item2.GetValue<string>("name");
//                        if (bundle.Modify.Ebx.Contains(value))
//                        {
//                            ManifestFileInfo fileInfo = manifestBundle.files[num4];
//                            EbxAssetEntry ebxAssetEntry = parent.modifiedEbx[value];
//                            item2.SetValue("sha1", ebxAssetEntry.Sha1);
//                            item2.SetValue("originalSize", ebxAssetEntry.OriginalSize);
//                            dataRefs.Add(ebxAssetEntry.Sha1);
//                            fileInfos.Add(new CasFileEntry
//                            {
//                                Entry = null,
//                                FileInfo = fileInfo
//                            });
//                        }
//                        num4++;
//                    }
//                    foreach (string item3 in bundle.Add.Ebx)
//                    {
//                        EbxAssetEntry ebxAssetEntry2 = parent.modifiedEbx[item3];
//                        DbObject dbObject3 = new DbObject();
//                        dbObject3.SetValue("name", ebxAssetEntry2.Name);
//                        dbObject3.SetValue("sha1", ebxAssetEntry2.Sha1);
//                        dbObject3.SetValue("originalSize", ebxAssetEntry2.OriginalSize);
//                        dbObject.GetValue<DbObject>("ebx").Add(dbObject3);
//                        ManifestFileInfo manifestFileInfo4 = new ManifestFileInfo();
//                        manifestFileInfo4.file = new ManifestFileRef(manifestFileInfo.file.CatalogIndex, inPatch: false, inCasIndex: 0);
//                        manifestBundle.files.Insert(num4++, manifestFileInfo4);
//                        dataRefs.Add(ebxAssetEntry2.Sha1);
//                        fileInfos.Add(new CasFileEntry
//                        {
//                            Entry = null,
//                            FileInfo = manifestFileInfo4
//                        });
//                    }
//                    foreach (DbObject item4 in dbObject.GetValue<DbObject>("res"))
//                    {
//                        string value2 = item4.GetValue<string>("name");
//                        if (bundle.Modify.Res.Contains(value2))
//                        {
//                            ManifestFileInfo manifestFileInfo5 = manifestBundle.files[num4];
//                            ResAssetEntry resAssetEntry = parent.modifiedRes[value2];
//                            if (resAssetEntry.ExtraData != null)
//                            {
//                                lock (resourceLock)
//                                {
//                                    HandlerExtraData handlerExtraData = (HandlerExtraData)resAssetEntry.ExtraData;
//                                    if (handlerExtraData != null)
//                                    {
//                                        byte[] outData = null;
//                                        Stream resourceData = AssetManager.Instance.GetResourceData(parent.fs.GetFilePath(manifestFileInfo5.file.CatalogIndex, manifestFileInfo5.file.CasIndex, manifestFileInfo5.file.IsInPatch), manifestFileInfo5.offset, manifestFileInfo5.size);
//                                        ResAssetEntry resAssetEntry2 = (ResAssetEntry)handlerExtraData.Handler.Modify(resAssetEntry, resourceData, handlerExtraData.Data, out outData);
//                                        if (!parent.archiveData.ContainsKey(resAssetEntry2.Sha1))
//                                        {
//                                            parent.archiveData.TryAdd(resAssetEntry2.Sha1, new ArchiveInfo
//                                            {
//                                                Data = outData,
//                                                RefCount = 1
//                                            });
//                                        }
//                                        resAssetEntry.Sha1 = resAssetEntry2.Sha1;
//                                        resAssetEntry.OriginalSize = resAssetEntry2.OriginalSize;
//                                        resAssetEntry.ResMeta = resAssetEntry2.ResMeta;
//                                        resAssetEntry.ExtraData = null;
//                                    }
//                                }
//                            }
//                            item4.SetValue("sha1", resAssetEntry.Sha1);
//                            item4.SetValue("originalSize", resAssetEntry.OriginalSize);
//                            if (resAssetEntry.ResMeta != null)
//                            {
//                                item4.SetValue("resMeta", resAssetEntry.ResMeta);
//                            }
//                            dataRefs.Add(resAssetEntry.Sha1);
//                            fileInfos.Add(new CasFileEntry
//                            {
//                                Entry = null,
//                                FileInfo = manifestFileInfo5
//                            });
//                        }
//                        num4++;
//                    }
//                    foreach (string re in bundle.Add.Res)
//                    {
//                        ResAssetEntry resAssetEntry3 = parent.modifiedRes[re];
//                        DbObject dbObject5 = new DbObject();
//                        dbObject5.SetValue("name", resAssetEntry3.Name);
//                        dbObject5.SetValue("sha1", resAssetEntry3.Sha1);
//                        dbObject5.SetValue("originalSize", resAssetEntry3.OriginalSize);
//                        dbObject5.SetValue("resRid", (long)resAssetEntry3.ResRid);
//                        dbObject5.SetValue("resType", resAssetEntry3.ResType);
//                        dbObject5.SetValue("resMeta", resAssetEntry3.ResMeta);
//                        dbObject.GetValue<DbObject>("res").Add(dbObject5);
//                        ManifestFileInfo manifestFileInfo6 = new ManifestFileInfo();
//                        manifestFileInfo6.file = new ManifestFileRef(manifestFileInfo.file.CatalogIndex, inPatch: false, inCasIndex: 0);
//                        manifestBundle.files.Insert(num4++, manifestFileInfo6);
//                        dataRefs.Add(resAssetEntry3.Sha1);
//                        fileInfos.Add(new CasFileEntry
//                        {
//                            Entry = null,
//                            FileInfo = manifestFileInfo6
//                        });
//                    }
//                    DbObject value3 = dbObject.GetValue<DbObject>("chunkMeta");
//                    int num5 = 0;
//                    List<int> list3 = new List<int>();
//                    foreach (DbObject item5 in dbObject.GetValue<DbObject>("chunks"))
//                    {
//                        Guid value4 = item5.GetValue<Guid>("id");
//                        if (bundle.Remove.Chunks.Contains(value4))
//                        {
//                            list3.Add(num5);
//                        }
//                        else if (bundle.Modify.Chunks.Contains(value4))
//                        {
//                            ChunkAssetEntry entry = parent.ModifiedChunks[value4];
//                            DbObject dbObject7 = value3.Find<DbObject>((object a) => (a as DbObject).GetValue("h32", 0) == entry.H32);
//                            item5.SetValue("sha1", entry.Sha1);
//                            item5.SetValue("logicalOffset", entry.LogicalOffset);
//                            item5.SetValue("logicalSize", entry.LogicalSize);
//                            if (entry.FirstMip != -1)
//                            {
//                                item5.SetValue("rangeStart", entry.RangeStart);
//                                item5.SetValue("rangeEnd", entry.RangeEnd);
//                                dbObject7?.GetValue<DbObject>("meta").SetValue("firstMip", entry.FirstMip);
//                            }
//                            if (num4 < manifestBundle.files.Count)
//                            {
//                                dataRefs.Add(entry.Sha1);
//                                ManifestFileInfo fileInfo2 = manifestBundle.files[num4];
//                                fileInfos.Add(new CasFileEntry
//                                {
//                                    Entry = entry,
//                                    FileInfo = fileInfo2
//                                });
//                            }
//                        }
//                        num4++;
//                        num5++;
//                    }
//                    list3.Reverse();
//                    foreach (int item6 in list3)
//                    {
//                        dbObject.GetValue<DbObject>("chunks").RemoveAt(item6);
//                        dbObject.GetValue<DbObject>("chunkMeta").RemoveAt(item6);
//                        manifestBundle.files.RemoveAt(item6 + num4);
//                    }
//                    foreach (Guid chunk in bundle.Add.Chunks)
//                    {
//                        ChunkAssetEntry chunkAssetEntry = parent.ModifiedChunks[chunk];
//                        DbObject dbObject8 = new DbObject();
//                        dbObject8.SetValue("id", chunk);
//                        dbObject8.SetValue("sha1", chunkAssetEntry.Sha1);
//                        dbObject8.SetValue("logicalOffset", chunkAssetEntry.LogicalOffset);
//                        dbObject8.SetValue("logicalSize", chunkAssetEntry.LogicalSize);
//                        DbObject dbObject9 = new DbObject();
//                        dbObject9.SetValue("h32", chunkAssetEntry.H32);
//                        dbObject9.SetValue("meta", new DbObject());
//                        value3.Add(dbObject9);
//                        if (chunkAssetEntry.FirstMip != -1)
//                        {
//                            dbObject8.SetValue("rangeStart", chunkAssetEntry.RangeStart);
//                            dbObject8.SetValue("rangeEnd", chunkAssetEntry.RangeEnd);
//                            dbObject9.GetValue<DbObject>("meta").SetValue("firstMip", chunkAssetEntry.FirstMip);
//                        }
//                        dbObject.GetValue<DbObject>("chunks").Add(dbObject8);
//                        ManifestFileInfo manifestFileInfo7 = new ManifestFileInfo();
//                        manifestFileInfo7.file = new ManifestFileRef(manifestFileInfo.file.CatalogIndex, inPatch: false, inCasIndex: 0);
//                        manifestBundle.files.Insert(num4++, manifestFileInfo7);
//                        dataRefs.Add(chunkAssetEntry.Sha1);
//                        fileInfos.Add(new CasFileEntry
//                        {
//                            Entry = chunkAssetEntry,
//                            FileInfo = manifestFileInfo7
//                        });
//                    }
//                    MemoryStream memoryStream = new MemoryStream();
//                    using (NativeWriter nativeWriter = new NativeWriter(memoryStream, leaveOpen: true))
//                    {
//                        nativeWriter.Write(3735927486u, Endian.Big);
//                        long position = nativeWriter.BaseStream.Position;
//                        nativeWriter.Write(2641989333u, Endian.Big);
//                        nativeWriter.Write(dbObject.GetValue<DbObject>("ebx").Count + dbObject.GetValue<DbObject>("res").Count + dbObject.GetValue<DbObject>("chunks").Count, Endian.Big);
//                        nativeWriter.Write(dbObject.GetValue<DbObject>("ebx").Count, Endian.Big);
//                        nativeWriter.Write(dbObject.GetValue<DbObject>("res").Count, Endian.Big);
//                        nativeWriter.Write(dbObject.GetValue<DbObject>("chunks").Count, Endian.Big);
//                        nativeWriter.Write(3735927486u, Endian.Big);
//                        nativeWriter.Write(3735927486u, Endian.Big);
//                        nativeWriter.Write(3735927486u, Endian.Big);
//                        foreach (DbObject item7 in dbObject.GetValue<DbObject>("ebx"))
//                        {
//                            nativeWriter.Write(item7.GetValue<FMT.FileTools.Sha1>("sha1"));
//                        }
//                        foreach (DbObject item8 in dbObject.GetValue<DbObject>("res"))
//                        {
//                            nativeWriter.Write(item8.GetValue<FMT.FileTools.Sha1>("sha1"));
//                        }
//                        foreach (DbObject item9 in dbObject.GetValue<DbObject>("chunks"))
//                        {
//                            nativeWriter.Write(item9.GetValue<FMT.FileTools.Sha1>("sha1"));
//                        }
//                        long num6 = 0L;
//                        Dictionary<uint, long> dictionary2 = new Dictionary<uint, long>();
//                        List<string> list4 = new List<string>();
//                        foreach (DbObject item10 in dbObject.GetValue<DbObject>("ebx"))
//                        {
//                            uint key2 = (uint)Fnv1.HashString(item10.GetValue<string>("name"));
//                            if (!dictionary2.ContainsKey(key2))
//                            {
//                                list4.Add(item10.GetValue<string>("name"));
//                                dictionary2.Add(key2, num6);
//                                num6 += item10.GetValue<string>("name").Length + 1;
//                            }
//                            nativeWriter.Write((uint)dictionary2[key2], Endian.Big);
//                            nativeWriter.Write(item10.GetValue("originalSize", 0), Endian.Big);
//                        }
//                        foreach (DbObject item11 in dbObject.GetValue<DbObject>("res"))
//                        {
//                            uint key3 = (uint)Fnv1.HashString(item11.GetValue<string>("name"));
//                            if (!dictionary2.ContainsKey(key3))
//                            {
//                                list4.Add(item11.GetValue<string>("name"));
//                                dictionary2.Add(key3, num6);
//                                num6 += item11.GetValue<string>("name").Length + 1;
//                            }
//                            nativeWriter.Write((uint)dictionary2[key3], Endian.Big);
//                            nativeWriter.Write(item11.GetValue("originalSize", 0), Endian.Big);
//                        }
//                        foreach (DbObject item12 in dbObject.GetValue<DbObject>("res"))
//                        {
//                            nativeWriter.Write(item12.GetValue("resType", 0), Endian.Big);
//                        }
//                        foreach (DbObject item13 in dbObject.GetValue<DbObject>("res"))
//                        {
//                            nativeWriter.Write(item13.GetValue<byte[]>("resMeta"));
//                        }
//                        foreach (DbObject item14 in dbObject.GetValue<DbObject>("res"))
//                        {
//                            nativeWriter.Write(item14.GetValue("resRid", 0L), Endian.Big);
//                        }
//                        foreach (DbObject item15 in dbObject.GetValue<DbObject>("chunks"))
//                        {
//                            nativeWriter.Write(item15.GetValue<Guid>("id"), Endian.Big);
//                            nativeWriter.Write(item15.GetValue("logicalOffset", 0), Endian.Big);
//                            nativeWriter.Write(item15.GetValue("logicalSize", 0), Endian.Big);
//                        }
//                        long num7 = 0L;
//                        long num8 = 0L;
//                        if (dbObject.GetValue<DbObject>("chunkMeta") != null && dbObject.GetValue<DbObject>("chunks").Count != 0)
//                        {
//                            num7 = nativeWriter.BaseStream.Position - position;
//                            using (DbWriter dbWriter = new DbWriter(new MemoryStream()))
//                            {
//                                nativeWriter.Write(dbWriter.WriteDbObject("chunkMeta", dbObject.GetValue<DbObject>("chunkMeta")));
//                            }
//                            num8 = nativeWriter.BaseStream.Position - position - num7;
//                        }
//                        long num9 = nativeWriter.BaseStream.Position - position;
//                        foreach (string item16 in list4)
//                        {
//                            nativeWriter.WriteNullTerminatedString(item16);
//                        }
//                        while ((nativeWriter.BaseStream.Position - (position - 4)) % 16 != 0L)
//                        {
//                            nativeWriter.Write((byte)0);
//                        }
//                        long position2 = nativeWriter.BaseStream.Position;
//                        nativeWriter.BaseStream.Position = position + 20;
//                        nativeWriter.Write((uint)num9, Endian.Big);
//                        nativeWriter.Write((uint)num7, Endian.Big);
//                        nativeWriter.Write((uint)num8, Endian.Big);
//                        nativeWriter.BaseStream.Position = position - 4;
//                        nativeWriter.Write((uint)(position2 - 4), Endian.Big);
//                    }
//                    byte[] array = memoryStream.ToArray();
//                    Sha1 item = Utils.GenerateSha1(array);
//                    bundleRefs.Add(item);
//                    dataRefs.Add(item);
//                    fileInfos.Add(new CasFileEntry
//                    {
//                        Entry = null,
//                        FileInfo = manifestFileInfo
//                    });
//                    bundleBuffers.Add(array);
//                }
//            }
//            catch (Exception ex)
//            {
//                Exception ex2 = errorException = ex;
//            }
//        }

//        public void ThreadPoolCallback(object threadContext)
//        {
//            Run();
//            if (Interlocked.Decrement(ref parent.numTasks) == 0)
//            {
//                doneEvent.Set();
//            }
//        }
//    }

//}
