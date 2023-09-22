﻿//using FMT.FileTools;
//using FMT.FileTools.Modding;
//using FrostySdk.Frostbite.PluginInterfaces;
//using FrostySdk.IO;
//using FrostySdk.Managers;
//using ModdingSupport;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using static FrostySdk.FileSystem;
//using static ModdingSupport.ModExecutor;

//namespace FrostySdk.Frostbite.Compilers
//{
//    /// <summary>
//    /// Original Code: https://github.com/CadeEvs/FrostyToolsuite/blob/1.0.7/FrostyModSupport/FrostyModExecutor.cs
//    /// </summary>
//    public class CasBundleAction
//    {
//        [Flags]
//        private enum Flags
//        {
//            HasBaseBundles = 1, // if the base toc has bundles that the patch doesnt have
//            HasBaseChunks = 2, // if the base toc has chunks that the patch doesnt have
//            HasCompressedNames = 4 // if the bundle names are huffman encoded
//        }


//        [Flags]
//        private enum InternalFlags : byte
//        {
//            HasCompressedStrings = 1 << 0,
//            HasInlineSb = 1 << 1,
//            HasInlineBundle = 1 << 2,
//        }

//        private struct CasFileInfo
//        {
//            public bool IsPatch;
//            public byte CatalogIndex;
//            public byte CasIndex;
//            public uint Offset;
//            public uint Size;
//        }

//        private class BundleInfo
//        {
//            public string Name;
//            public uint NameOffset;
//            public long Offset;
//            public uint Size;
//            public bool IsModified;
//            public bool IsPatch;
//            public string SbName;
//            public int SplitIndex;
//        }

//        private class ChunkInfo
//        {
//            public Guid Guid;
//            public bool IsPatch;
//            public CasFileInfo CasFileInfo;
//            public string SbName;
//            public int SplitIndex;
//        }

//        private static readonly object locker = new object();

//        public SuperBundleInfo SuperBundleInfo;
//        public bool HasErrored => Exception != null;
//        public Exception Exception { get; private set; }
//        public bool HasSb = true;

//        private ManualResetEvent doneEvent;
//        private ModExecutor parent;
//        private string m_catalog;

//        public static Dictionary<string, int> CasFiles = new Dictionary<string, int>();
//        //private static Dictionary<string, NativeWriter> CasWriters = new Dictionary<string, NativeWriter>();

//        public CasBundleAction(SuperBundleInfo inSb, ManualResetEvent inDoneEvent, ModExecutor inParent)
//        {
//            SuperBundleInfo = inSb;
//            parent = inParent;
//            doneEvent = inDoneEvent;

//            m_catalog = GetCatalog(SuperBundleInfo, FileSystem.Instance.CatalogObjects);
//        }

//        private void Run()
//        {
//            try
//            {
//                NativeWriter casWriter = null;
//                int casFileIndex = 0;

//                Dictionary<int, BundleInfo> bundles = new Dictionary<int, BundleInfo>();
//                Dictionary<Guid, ChunkInfo> chunks = new Dictionary<Guid, ChunkInfo>();
//                InternalFlags tocFlags = 0;

//                bool isDefaultTocModified = false;
//                bool[] isSplitTocModified = new bool[SuperBundleInfo.SplitSuperBundles.Count];

//                // read default toc
//                bool isPatch = true;
//                string tocPath = FileSystem.Instance.ResolvePath(string.Format("native_patch/{0}.toc", SuperBundleInfo.Name));
//                if (tocPath.Equals(string.Empty))
//                {
//                    tocPath = FileSystem.Instance.ResolvePath(string.Format("native_data/{0}.toc", SuperBundleInfo.Name));
//                    isPatch = false;
//                }

//                if (!tocPath.Equals(string.Empty))
//                {
//                    using (NativeReader reader = new NativeReader(tocPath))
//                    {
//                        reader.Skip(556);
//                        ReadToc(reader, ref bundles, ref chunks, ref tocFlags, isPatch);
//                    }
//                }

//                // read split toc's
//                for (int splitIndex = 0; splitIndex < SuperBundleInfo.SplitSuperBundles.Count; splitIndex++)
//                {
//                    string sbPath = SuperBundleInfo.Name.Replace("win32", SuperBundleInfo.SplitSuperBundles[splitIndex]);

//                    // parse superbundle toc files from patch and data
//                    isPatch = true;
//                    tocPath = FileSystem.Instance.ResolvePath(string.Format("native_patch/{0}.toc", sbPath));
//                    if (tocPath.Equals(string.Empty))
//                    {
//                        tocPath = FileSystem.Instance.ResolvePath(string.Format("native_data/{0}.toc", sbPath));
//                        isPatch = false;
//                    }

//                    if (!tocPath.Equals(string.Empty))
//                    {
//                        using (NativeReader reader = new NativeReader(tocPath))
//                        {
//                            reader.Skip(556);
//                            ReadToc(reader, ref bundles, ref chunks, ref tocFlags, isPatch, splitIndex);
//                        }
//                    }
//                }

//                string modPath = FileSystem.Instance.BasePath + parent.ModDirectoryName + "\\" + "Patch";

//                // TODO: newer games use little endian
//                Endian endian = Endian.Big;
//                if (ProfileManager.DataVersion >= (int)EGame.FIFA21)
//                {
//                    endian = Endian.Little;
//                }

//                BinarySbWriter modWriter = null;
//                BinarySbWriter modDefaultWriter = new BinarySbWriter(new MemoryStream(), inEndian: endian);
//                List<BinarySbWriter> modSplitWriters = null;
//                if (SuperBundleInfo.SplitSuperBundles.Count > 0)
//                {
//                    modSplitWriters = new List<BinarySbWriter>();
//                    foreach (string catalog in SuperBundleInfo.SplitSuperBundles)
//                    {
//                        modSplitWriters.Add(new BinarySbWriter(new MemoryStream(), inEndian: endian));
//                    }
//                }

//                // reading in unmodified data and modifying it
//                {
//                    string patchSbPath = "";
//                    string baseSbPath = "";
//                    NativeReader reader = null;
//                    NativeReader patchReader = null;
//                    NativeReader baseReader = null;

//                    // check for modified bundles
//                    foreach (int bundleHash in parent.m_modifiedBundles.Keys)
//                    {
//                        if (bundles.ContainsKey(bundleHash))
//                        {
//                            // modify
//                            BundleInfo bundleInfo = bundles[bundleHash];

//                            tocFlags &= ~InternalFlags.HasInlineBundle;
//                            tocFlags &= ~InternalFlags.HasInlineSb;
//                            if ((bundleInfo.Size & 0xC0000000) == 0x40000000)
//                            {
//                                tocFlags |= InternalFlags.HasInlineSb;
//                                HasSb = false;
//                                bundleInfo.Size &= ~0xC0000000;
//                                bundleInfo.Offset += 0x22C;
//                            }

//                            if (bundleInfo.IsPatch)
//                            {
//                                if (patchReader == null || patchSbPath != bundleInfo.SbName)
//                                {
//                                    patchSbPath = bundleInfo.SbName;
//                                    patchReader?.Dispose();
//                                    if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                                    {
//                                        patchReader = new NativeReader(FileSystem.Instance.ResolvePath(string.Format("native_patch/{0}.toc", patchSbPath)));
//                                        patchReader.Skip(556);
//                                    }
//                                    else
//                                    {
//                                        patchReader = new NativeReader(FileSystem.Instance.ResolvePath(string.Format("native_patch/{0}.sb", patchSbPath)));
//                                        patchReader.Skip(556);
//                                    }
//                                }
//                                reader = patchReader;
//                            }
//                            else
//                            {
//                                if (baseReader == null || baseSbPath != bundleInfo.SbName)
//                                {
//                                    baseSbPath = bundleInfo.SbName;
//                                    baseReader?.Dispose();
//                                    if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                                    {
//                                        baseReader = new NativeReader(FileSystem.Instance.ResolvePath(string.Format("native_data/{0}.toc", patchSbPath)));
//                                    }
//                                    else
//                                    {
//                                        baseReader = new NativeReader(FileSystem.Instance.ResolvePath(string.Format("native_data/{0}.sb", patchSbPath)));
//                                    }
//                                }
//                                reader = baseReader;
//                            }

//                            string catalog;
//                            if (bundleInfo.SplitIndex != -1)
//                            {
//                                modWriter = modSplitWriters[bundleInfo.SplitIndex];
//                                isSplitTocModified[bundleInfo.SplitIndex] = true;
//                                catalog = SuperBundleInfo.SplitSuperBundles[bundleInfo.SplitIndex];
//                            }
//                            else
//                            {
//                                modWriter = modDefaultWriter;
//                                isDefaultTocModified = true;
//                                catalog = m_catalog;
//                            }
//                            byte catalogIndex = (byte)FileSystem.Instance.GetCatalogIndex(catalog);

//                            Stream stream = reader.CreateViewStream(bundleInfo.Offset, bundleInfo.Size);

//                            DbObject bundleObj;
//                            using (BinarySbReader bundleReader = new BinarySbReader(stream, 0, FileSystem.Instance.CreateDeobfuscator()))
//                            {
//                                bundleObj = ReadBundle(bundleReader, ref tocFlags);
//                            }

//                            stream.Dispose();

//                            ModBundleInfo modBundle = parent.m_modifiedBundles[bundleHash];

//                            foreach (DbObject ebx in bundleObj.GetValue<DbObject>("ebx"))
//                            {
//                                int idx = modBundle.Modify.Ebx.FindIndex((string a) => a.Equals(ebx.GetValue<string>("name")));
//                                if (idx != -1)
//                                {
//                                    EbxAssetEntry entry = parent.modifiedEbx[modBundle.Modify.Ebx[idx]];

//                                    // get next cas (if one hasnt been obtained or the current one will exceed 1gb)
//                                    if (casWriter == null || casWriter.Length + parent.archiveData[entry.Sha1].Data.Length > 1073741824)
//                                    {
//                                        casWriter?.Close();
//                                        casWriter = GetNextCas(catalog, out casFileIndex);
//                                    }

//                                    ebx.SetValue("sha1", entry.Sha1);
//                                    ebx.SetValue("originalSize", entry.OriginalSize);
//                                    ebx.SetValue("size", entry.Size);
//                                    ebx.SetValue("catalog", catalogIndex);
//                                    ebx.SetValue("cas", casFileIndex);
//                                    ebx.SetValue("offset", (int)casWriter.Position);
//                                    if (parent.HasPatchDirectory)
//                                    {
//                                        ebx.SetValue("patch", true);
//                                    }

//                                    casWriter.Write(parent.archiveData[entry.Sha1].Data);
//                                }
//                            }
//                            foreach (string name in modBundle.Add.Ebx)
//                            {
//                                EbxAssetEntry entry = parent.modifiedEbx[name];

//                                // get next cas (if one hasnt been obtained or the current one will exceed 1gb)
//                                if (casWriter == null || casWriter.Length + parent.archiveData[entry.Sha1].Data.Length > 1073741824)
//                                {
//                                    casWriter?.Close();
//                                    casWriter = GetNextCas(catalog, out casFileIndex);
//                                }

//                                DbObject ebx = new DbObject();
//                                ebx.SetValue("name", entry.Name);
//                                ebx.SetValue("sha1", entry.Sha1);
//                                ebx.SetValue("originalSize", entry.OriginalSize);
//                                ebx.SetValue("size", entry.Size);
//                                ebx.SetValue("catalog", catalogIndex);
//                                ebx.SetValue("cas", casFileIndex);
//                                ebx.SetValue("offset", (int)casWriter.Position);
//                                if (parent.HasPatchDirectory)
//                                {
//                                    ebx.SetValue("patch", true);
//                                }
//                                bundleObj.GetValue<DbObject>("ebx").Add(ebx);

//                                casWriter.Write(parent.archiveData[entry.Sha1].Data);
//                            }

//                            foreach (DbObject res in bundleObj.GetValue<DbObject>("res"))
//                            {
//                                int idx = modBundle.Modify.Res.FindIndex((string a) => a.Equals(res.GetValue<string>("name")));
//                                if (idx != -1)
//                                {
//                                    ResAssetEntry entry = parent.modifiedRes[modBundle.Modify.Res[idx]];

//                                    // get next cas (if one hasnt been obtained or the current one will exceed 1gb)
//                                    if (casWriter == null || casWriter.Length + parent.archiveData[entry.Sha1].Data.Length > 1073741824)
//                                    {
//                                        casWriter?.Close();
//                                        casWriter = GetNextCas(catalog, out casFileIndex);
//                                    }

//                                    res.SetValue("sha1", entry.Sha1);
//                                    res.SetValue("originalSize", entry.OriginalSize);
//                                    res.SetValue("size", entry.Size);
//                                    res.SetValue("catalog", catalogIndex);
//                                    res.SetValue("cas", casFileIndex);
//                                    res.SetValue("offset", (int)casWriter.Position);
//                                    res.SetValue("resRid", (long)entry.ResRid);
//                                    res.SetValue("resMeta", entry.ResMeta);
//                                    res.SetValue("resType", (int)entry.ResType);
//                                    if (parent.HasPatchDirectory)
//                                    {
//                                        res.SetValue("patch", true);
//                                    }

//                                    casWriter.Write(parent.archiveData[entry.Sha1].Data);
//                                }
//                            }
//                            foreach (string name in modBundle.Add.Res)
//                            {
//                                ResAssetEntry entry = parent.modifiedRes[name];

//                                // get next cas (if one hasnt been obtained or the current one will exceed 1gb)
//                                if (casWriter == null || casWriter.Length + parent.archiveData[entry.Sha1].Data.Length > 1073741824)
//                                {
//                                    casWriter?.Close();
//                                    casWriter = GetNextCas(catalog, out casFileIndex);
//                                }

//                                DbObject res = new DbObject();
//                                res.SetValue("name", entry.Name);
//                                res.SetValue("sha1", entry.Sha1);
//                                res.SetValue("originalSize", entry.OriginalSize);
//                                res.SetValue("size", entry.Size);
//                                res.SetValue("catalog", catalogIndex);
//                                res.SetValue("cas", casFileIndex);
//                                res.SetValue("offset", (int)casWriter.Position);
//                                res.SetValue("resRid", (long)entry.ResRid);
//                                res.SetValue("resMeta", entry.ResMeta);
//                                res.SetValue("resType", (int)entry.ResType);
//                                if (parent.HasPatchDirectory)
//                                {
//                                    res.SetValue("patch", true);
//                                }
//                                bundleObj.GetValue<DbObject>("res").Add(res);

//                                casWriter.Write(parent.archiveData[entry.Sha1].Data);
//                            }

//                            DbObject chunkMeta = bundleObj.GetValue<DbObject>("chunkMeta");
//                            int chunkIndex = 0;
//                            List<(int, int)> chunksToRemove = new List<(int, int)>();

//                            // modify chunks
//                            foreach (DbObject chunk in bundleObj.GetValue<DbObject>("chunks"))
//                            {
//                                int idx = modBundle.Remove.Chunks.FindIndex((Guid a) => a == chunk.GetValue<Guid>("id"));
//                                if (idx != -1)
//                                {
//                                    chunksToRemove.Add((chunkIndex, parent.ModifiedChunks[modBundle.Remove.Chunks[idx]].H32));
//                                }
//                                else
//                                {
//                                    idx = modBundle.Modify.Chunks.FindIndex((Guid a) => a == chunk.GetValue<Guid>("id"));
//                                    if (idx != -1)
//                                    {
//                                        ChunkAssetEntry entry = parent.ModifiedChunks[modBundle.Modify.Chunks[idx]];

//                                        // get next cas (if one hasnt been obtained or the current one will exceed 1gb)
//                                        if (casWriter == null || casWriter.Length + parent.archiveData[entry.Sha1].Data.Length > 1073741824)
//                                        {
//                                            casWriter?.Close();
//                                            casWriter = GetNextCas(catalog, out casFileIndex);
//                                        }

//                                        DbObject meta = chunkMeta.Find<DbObject>((object a) => { return (a as DbObject).GetValue<int>("h32") == entry.H32; });

//                                        byte[] data = parent.archiveData[entry.Sha1].Data;
//                                        if (entry.LogicalOffset != 0)
//                                        {
//                                            data = new byte[entry.RangeEnd - entry.RangeStart];
//                                            Array.Copy(parent.archiveData[entry.Sha1].Data, entry.RangeStart, data, 0, data.Length);
//                                        }

//                                        chunk.SetValue("sha1", entry.Sha1);
//                                        chunk.SetValue("originalSize", entry.OriginalSize);
//                                        chunk.SetValue("size", data.Length);
//                                        chunk.SetValue("catalog", catalogIndex);
//                                        chunk.SetValue("cas", casFileIndex);
//                                        chunk.SetValue("offset", (uint)casWriter.Position);
//                                        chunk.SetValue("logicalOffset", (int)entry.LogicalOffset);
//                                        chunk.SetValue("logicalSize", (int)entry.LogicalSize);
//                                        if (parent.HasPatchDirectory)
//                                        {
//                                            chunk.SetValue("patch", true);
//                                        }

//                                        if (entry.FirstMip != -1)
//                                        {
//                                            meta?.GetValue<DbObject>("meta").SetValue("firstMip", entry.FirstMip);
//                                        }

//                                        casWriter.Write(data);
//                                    }
//                                }

//                                chunkIndex++;
//                            }
//                            chunksToRemove.Reverse();
//                            foreach ((int, int) chunk in chunksToRemove)
//                            {
//                                bundleObj.GetValue<DbObject>("chunks").RemoveAt(chunk.Item1);
//                                int metaIndex = chunkMeta.FindIndex((object a) => { return (a as DbObject).GetValue<int>("h32") == chunk.Item2; });
//                                if (metaIndex != -1)
//                                {
//                                    bundleObj.GetValue<DbObject>("chunkMeta").RemoveAt(metaIndex);
//                                }
//                            }
//                            foreach (Guid name in modBundle.Add.Chunks)
//                            {
//                                ChunkAssetEntry entry = parent.ModifiedChunks[name];

//                                // get next cas (if one hasnt been obtained or the current one will exceed 1gb)
//                                if (casWriter == null || casWriter.Length + parent.archiveData[entry.Sha1].Data.Length > 1073741824)
//                                {
//                                    casWriter?.Close();
//                                    casWriter = GetNextCas(catalog, out casFileIndex);
//                                }

//                                byte[] data = parent.archiveData[entry.Sha1].Data;
//                                if (entry.LogicalOffset != 0)
//                                {
//                                    data = new byte[entry.RangeEnd - entry.RangeStart];
//                                    Array.Copy(parent.archiveData[entry.Sha1].Data, entry.RangeStart, data, 0, data.Length);
//                                }

//                                DbObject chunk = new DbObject();
//                                chunk.SetValue("id", name);
//                                chunk.SetValue("sha1", entry.Sha1);
//                                chunk.SetValue("originalSize", entry.OriginalSize);
//                                chunk.SetValue("size", data.Length);
//                                chunk.SetValue("catalog", catalogIndex);
//                                chunk.SetValue("cas", casFileIndex);
//                                chunk.SetValue("offset", (uint)casWriter.Position);
//                                chunk.SetValue("logicalOffset", (int)entry.LogicalOffset);
//                                chunk.SetValue("logicalSize", (int)entry.LogicalSize);
//                                if (parent.HasPatchDirectory)
//                                {
//                                    chunk.SetValue("patch", true);
//                                }

//                                DbObject meta = new DbObject();
//                                meta.SetValue("h32", entry.H32);
//                                meta.SetValue("meta", new DbObject());
//                                chunkMeta.Add(meta);

//                                if (entry.FirstMip != -1)
//                                {
//                                    meta?.GetValue<DbObject>("meta").SetValue("firstMip", entry.FirstMip);
//                                }

//                                casWriter.Write(data);

//                                bundleObj.GetValue<DbObject>("chunks").Add(chunk);
//                            }

//                            bundleInfo.Offset = modWriter.Position;
//                            bundleInfo.IsModified = true;
//                            bundleInfo.IsPatch = true;

//                            // fifa 21 uses different layouts in data and patch
//                            if (ProfileManager.IsLoaded(EGame.FIFA21))
//                            {
//                                tocFlags |= InternalFlags.HasInlineBundle;
//                            }

//                            // write bundle
//                            uint bundleOffset = 0;
//                            uint bundleSize = 0;
//                            uint locationOffset = 0;
//                            uint totalCount = (uint)(bundleObj.GetValue<DbObject>("ebx").Count + bundleObj.GetValue<DbObject>("res").Count + bundleObj.GetValue<DbObject>("chunks").Count + (tocFlags.HasFlag(InternalFlags.HasInlineBundle) ? 0 : 1));
//                            uint dataOffset = 0;

//                            modWriter.Write(0xDEADBABE, Endian.Big); // bundleOffset
//                            modWriter.Write(0xDEADBABE, Endian.Big); // bundleSize
//                            modWriter.Write(0xDEADBABE, Endian.Big); // locationOffset
//                            modWriter.Write(0xDEADBABE, Endian.Big); // totalCount
//                            modWriter.Write(0xDEADBABE, Endian.Big); // dataOffset


//                            modWriter.Write(0xDEADBABE, Endian.Big); // dataOffset
//                            modWriter.Write(0xDEADBABE, Endian.Big); // dataOffset
//                            modWriter.Write(0, Endian.Big);

//                            if (tocFlags.HasFlag(InternalFlags.HasInlineBundle))
//                            {
//                                bundleOffset = (uint)(modWriter.Position - bundleInfo.Offset);
//                                modWriter.Write(bundleObj);
//                                bundleSize = (uint)(modWriter.Position - bundleOffset);
//                            }

//                            byte[] flags = new byte[totalCount];
//                            byte unused = 0;
//                            bool patch = false;
//                            byte catIndex = 0;
//                            byte casIndex = 0;
//                            int z = 0;

//                            dataOffset = (uint)(modWriter.Position - bundleInfo.Offset);

//                            if (!tocFlags.HasFlag(InternalFlags.HasInlineBundle))
//                            {
//                                MemoryStream ms = new MemoryStream();
//                                using (BinarySbWriter subWriter = new BinarySbWriter(ms, true))
//                                    subWriter.Write(bundleObj);

//                                byte[] bundleBuffer = ms.ToArray();
//                                ms.Dispose();

//                                // get next cas (if one hasnt been obtained or the current one will exceed 1gb)
//                                if (casWriter == null || casWriter.Length + bundleBuffer.Length > 1073741824)
//                                {
//                                    casWriter?.Close();
//                                    casWriter = GetNextCas(catalog, out casFileIndex);
//                                }

//                                flags[z] = 1;

//                                modWriter.Write(unused);
//                                modWriter.Write(patch = parent.HasPatchDirectory);
//                                modWriter.Write(catIndex = catalogIndex);
//                                modWriter.Write(casIndex = (byte)casFileIndex);
//                                modWriter.Write((uint)casWriter.Position, Endian.Big);
//                                modWriter.Write(bundleBuffer.Length, Endian.Big);
//                                z++;

//                                casWriter.Write(bundleBuffer);
//                            }

//                            foreach (DbObject ebx in bundleObj.GetValue<DbObject>("ebx"))
//                            {
//                                if (patch != ebx.HasValue("patch") || catIndex != ebx.GetValue<byte>("catalog") ||
//                                    casIndex != ebx.GetValue<byte>("cas"))
//                                {
//                                    flags[z] = 1;

//                                    modWriter.Write(unused);
//                                    modWriter.Write(patch = ebx.HasValue("patch"));
//                                    modWriter.Write(catIndex = ebx.GetValue<byte>("catalog"));
//                                    modWriter.Write(casIndex = ebx.GetValue<byte>("cas"));
//                                }

//                                modWriter.Write(ebx.GetValue<int>("offset"), Endian.Big);
//                                modWriter.Write(ebx.GetValue<int>("size"), Endian.Big);
//                                z++;
//                            }

//                            foreach (DbObject res in bundleObj.GetValue<DbObject>("res"))
//                            {
//                                if (patch != res.HasValue("patch") || catIndex != res.GetValue<byte>("catalog") ||
//                                    casIndex != res.GetValue<byte>("cas"))
//                                {
//                                    flags[z] = 1;

//                                    modWriter.Write(unused);
//                                    modWriter.Write(patch = res.HasValue("patch"));
//                                    modWriter.Write(catIndex = res.GetValue<byte>("catalog"));
//                                    modWriter.Write(casIndex = res.GetValue<byte>("cas"));
//                                }

//                                modWriter.Write(res.GetValue<int>("offset"), Endian.Big);
//                                modWriter.Write(res.GetValue<int>("size"), Endian.Big);
//                                z++;
//                            }

//                            foreach (DbObject chunk in bundleObj.GetValue<DbObject>("chunks"))
//                            {
//                                if (patch != chunk.HasValue("patch") || catIndex != chunk.GetValue<byte>("catalog") ||
//                                    casIndex != chunk.GetValue<byte>("cas"))
//                                {
//                                    flags[z] = 1;

//                                    modWriter.Write(unused);
//                                    modWriter.Write(patch = chunk.HasValue("patch"));
//                                    modWriter.Write(catIndex = chunk.GetValue<byte>("catalog"));
//                                    modWriter.Write(casIndex = chunk.GetValue<byte>("cas"));
//                                }

//                                modWriter.Write(chunk.GetValue<int>("offset"), Endian.Big);
//                                modWriter.Write(chunk.GetValue<int>("size"), Endian.Big);
//                                z++;
//                            }

//                            locationOffset = (uint)(modWriter.Position - bundleInfo.Offset);
//                            modWriter.Write(flags);

//                            uint size = (uint)(modWriter.Position - bundleInfo.Offset);

//                            modWriter.WritePadding(4);

//                            // update offsets and sizes
//                            modWriter.Position = bundleInfo.Offset;

//                            modWriter.Write(bundleOffset, Endian.Big);
//                            modWriter.Write(bundleSize, Endian.Big);

//                            modWriter.Write(locationOffset, Endian.Big);
//                            modWriter.Write(totalCount, Endian.Big);
//                            modWriter.Write(dataOffset, Endian.Big);

//                            modWriter.Write(dataOffset, Endian.Big);
//                            modWriter.Write(dataOffset, Endian.Big);
//#if DEBUG
//                                Debug.Assert(tocFlags.HasFlag(InternalFlags.HasInlineBundle) ? (modWriter.Position + 4 - bundleInfo.Offset) == bundleOffset : (modWriter.Position + 4 - bundleInfo.Offset) == dataOffset);
//#endif
//                            modWriter.Position = modWriter.Length;
//                            bundleInfo.Size = size;
//                        }
//                    }

//                    int sbId = AssetManager.Instance.GetSuperBundleId(SuperBundleInfo.Name);
//                    if (parent.m_modifiedSuperBundles.ContainsKey(sbId))
//                    {
//                        ModBundleInfo bundleInfo = parent.m_modifiedSuperBundles[sbId];

//                        foreach (Guid chunkId in bundleInfo.Modify.Chunks)
//                        {
//                            if (chunks.ContainsKey(chunkId))
//                            {
//                                ChunkInfo chunkInfo = chunks[chunkId];

//                                ChunkAssetEntry entry = parent.ModifiedChunks[chunkId];

//                                string catalog;
//                                if (chunkInfo.SplitIndex != -1)
//                                {
//                                    isSplitTocModified[chunkInfo.SplitIndex] = true;
//                                    catalog = SuperBundleInfo.SplitSuperBundles[chunkInfo.SplitIndex];
//                                }
//                                else
//                                {
//                                    isDefaultTocModified = true;
//                                    catalog = m_catalog;
//                                }

//                                // get next cas (if one hasnt been obtained or the current one will exceed 1gb)
//                                if (casWriter == null || casWriter.Length + parent.archiveData[entry.Sha1].Data.Length > 1073741824)
//                                {
//                                    casWriter?.Close();
//                                    casWriter = GetNextCas(catalog, out casFileIndex);
//                                }

//                                chunkInfo.IsPatch = true;
//                                chunkInfo.CasFileInfo.IsPatch = parent.HasPatchDirectory;
//                                chunkInfo.CasFileInfo.CatalogIndex = (byte)FileSystem.Instance.GetCatalogIndex(catalog);
//                                chunkInfo.CasFileInfo.CasIndex = (byte)casFileIndex;
//                                chunkInfo.CasFileInfo.Offset = (uint)casWriter.Position;
//                                chunkInfo.CasFileInfo.Size = (uint)parent.archiveData[entry.Sha1].Data.Length;

//                                casWriter.Write(parent.archiveData[entry.Sha1].Data);
//                            }
//                            else
//                            {
//                                //bundleInfo.Add.AddChunk(chunkId);
//                                throw new Exception($"tried to modify chunk {chunkId} in {SuperBundleInfo.Name}, but chunk is not in there");
//                            }
//                        }
//                        foreach (Guid chunkId in bundleInfo.Add.Chunks)
//                        {
//                            ChunkInfo chunkInfo = new ChunkInfo()
//                            {
//                                Guid = chunkId,
//                                SplitIndex = -1,
//                                SbName = SuperBundleInfo.Name
//                            };

//                            chunks.Add(chunkId, chunkInfo);

//                            ChunkAssetEntry entry = parent.ModifiedChunks[chunkId];

//                            string catalog;
//                            if (chunkInfo.SplitIndex != -1)
//                            {
//                                isSplitTocModified[chunkInfo.SplitIndex] = true;
//                                catalog = SuperBundleInfo.SplitSuperBundles[chunkInfo.SplitIndex];
//                            }
//                            else
//                            {
//                                isDefaultTocModified = true;
//                                catalog = m_catalog;
//                            }

//                            // get next cas (if one hasnt been obtained or the current one will exceed 1gb)
//                            if (casWriter == null || casWriter.Length + parent.archiveData[entry.Sha1].Data.Length > 1073741824)
//                            {
//                                casWriter?.Close();
//                                casWriter = GetNextCas(catalog, out casFileIndex);
//                            }

//                            chunkInfo.IsPatch = true;
//                            chunkInfo.CasFileInfo.IsPatch = parent.HasPatchDirectory;
//                            chunkInfo.CasFileInfo.CatalogIndex = (byte)FileSystem.Instance.GetCatalogIndex(catalog);
//                            chunkInfo.CasFileInfo.CasIndex = (byte)casFileIndex;
//                            chunkInfo.CasFileInfo.Offset = (uint)casWriter.Position;
//                            chunkInfo.CasFileInfo.Size = (uint)parent.archiveData[entry.Sha1].Data.Length;

//                            casWriter.Write(parent.archiveData[entry.Sha1].Data);
//                        }
//                    }

//                    if (isDefaultTocModified || isSplitTocModified.FirstOrDefault(b => b))
//                    {
//                        foreach (BundleInfo bundleInfo in bundles.Values)
//                        {
//                            if (!bundleInfo.IsModified && (bundleInfo.IsPatch || !parent.HasPatchDirectory))
//                            {
//                                tocFlags &= ~InternalFlags.HasInlineSb;
//                                if ((bundleInfo.Size & 0xC0000000) == 0x40000000)
//                                {
//                                    tocFlags |= InternalFlags.HasInlineSb;
//                                    bundleInfo.Size &= ~0xC0000000;
//                                    bundleInfo.Offset += 0x22C;
//                                }

//                                if (bundleInfo.IsPatch)
//                                {
//                                    if (patchReader == null || patchSbPath != bundleInfo.SbName)
//                                    {
//                                        patchSbPath = bundleInfo.SbName;
//                                        patchReader?.Dispose();
//                                        if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                                        {
//                                            patchReader = new NativeReader(FileSystem.Instance.ResolvePath(string.Format("native_patch/{0}.toc", patchSbPath)));
//                                            patchReader.Skip(556);
//                                        }
//                                        else
//                                        {
//                                            patchReader = new NativeReader(FileSystem.Instance.ResolvePath(string.Format("native_patch/{0}.sb", patchSbPath)));
//                                            patchReader.Skip(556);
//                                        }
//                                    }
//                                    reader = patchReader;
//                                }
//                                else
//                                {
//                                    if (baseReader == null || baseSbPath != bundleInfo.SbName)
//                                    {
//                                        baseSbPath = bundleInfo.SbName;
//                                        baseReader?.Dispose();
//                                        if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                                        {
//                                            baseReader = new NativeReader(FileSystem.Instance.ResolvePath(string.Format("native_data/{0}.toc", patchSbPath)));
//                                        }
//                                        else
//                                        {
//                                            baseReader = new NativeReader(FileSystem.Instance.ResolvePath(string.Format("native_data/{0}.sb", patchSbPath)));
//                                        }
//                                    }
//                                    reader = baseReader;
//                                }
//                                //if (patchReader == null || patchSbPath != bundleInfo.SbName)
//                                //{
//                                //    patchSbPath = bundleInfo.SbName;
//                                //    patchReader?.Dispose();
//                                //    if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                                //    {
//                                //        patchReader = new NativeReader(new FileStream(FileSystem.Instance.ResolvePath(string.Format("native_patch/{0}.toc", patchSbPath)), FileMode.Open, FileAccess.Read), FileSystem.Instance.CreateDeobfuscator());
//                                //    }
//                                //    else
//                                //    {
//                                //        patchReader = new NativeReader(new FileStream(FileSystem.Instance.ResolvePath(string.Format("native_patch/{0}.sb", patchSbPath)), FileMode.Open, FileAccess.Read), FileSystem.Instance.CreateDeobfuscator());
//                                //    }
//                                //}

//                                if (bundleInfo.SplitIndex != -1)
//                                {
//                                    modWriter = modSplitWriters[bundleInfo.SplitIndex];
//                                }
//                                else
//                                {
//                                    modWriter = modDefaultWriter;
//                                }

//                                reader.Position = bundleInfo.Offset;
//                                bundleInfo.Offset = modWriter.Position;
//                                modWriter.Write(reader.ReadBytes((int)bundleInfo.Size));
//                                modWriter.WritePadding(4);
//                            }
//                        }
//                    }

//                    baseReader?.Dispose();
//                    patchReader?.Dispose();
//                }

//                // fifa 21 uses different layouts in data and patch
//                if (ProfileManager.IsLoaded(EGame.FIFA21))
//                {
//                    tocFlags |= InternalFlags.HasInlineBundle;
//                    tocFlags &= ~InternalFlags.HasInlineSb;
//                    HasSb = true;
//                }

//                // TODO: huffman encode strings, for now just store them uncompressed
//                tocFlags &= ~InternalFlags.HasCompressedStrings;

//                // rewrite toc and sb if necessary
//                if (isDefaultTocModified)
//                {
//                    string modTocPath = string.Format("{0}\\{1}.toc", modPath, SuperBundleInfo.Name);
//                    FileInfo fi = new FileInfo(modTocPath);
//                    Directory.CreateDirectory(fi.DirectoryName);
//                    NativeWriter sbWriter = null;
//                    if (!tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                    {
//                        string modSbPath = string.Format("{0}\\{1}.sb", modPath, SuperBundleInfo.Name);
//                        sbWriter = new NativeWriter(new FileStream(modSbPath, FileMode.Create, FileAccess.Write));
//                    }
//                    using (DbWriter writer = new DbWriter(new FileStream(fi.FullName, FileMode.Create, FileAccess.Write)))
//                    {
//                        writer.Write(0x01CED100);
//                        writer.Position += 0x228;

//                        uint bundleHashMapOffset;
//                        uint bundleDataOffset;
//                        int bundlesCount = 0;

//                        uint chunkHashMapOffset;
//                        uint chunkGuidOffset;
//                        int chunksCount = 0;

//                        uint namesOffset;

//                        uint chunkDataOffset;

//                        Flags flags = 0;

//                        int namesCount = 0;
//                        int tableCount = 0;
//                        uint tableOffset = 0;

//                        long startPos = writer.Position;

//                        writer.Write(0xDEADBEEF, Endian.Big);
//                        writer.Write(0xDEADBEEF, Endian.Big);
//                        writer.Write(0xDEADBEEF, Endian.Big);

//                        writer.Write(0xDEADBEEF, Endian.Big);
//                        writer.Write(0xDEADBEEF, Endian.Big);
//                        writer.Write(0xDEADBEEF, Endian.Big);

//                        writer.Write(0xDEADBEEF, Endian.Big);
//                        writer.Write(0xDEADBEEF, Endian.Big);

//                        writer.Write(0xDEADBEEF, Endian.Big);

//                        writer.Write(0xDEADBEEF, Endian.Big);
//                        writer.Write(0xDEADBEEF, Endian.Big);

//                        writer.Write(0xDEADBEEF, Endian.Big);

//                        if (tocFlags.HasFlag(InternalFlags.HasCompressedStrings))
//                        {
//                            flags |= Flags.HasCompressedNames;
//                            writer.Write(0xDEADBEEF, Endian.Big);
//                            writer.Write(0xDEADBEEF, Endian.Big);
//                            writer.Write(0xDEADBEEF, Endian.Big);
//                        }

//                        Dictionary<byte[], BundleInfo> bundleDic = new Dictionary<byte[], BundleInfo>();
//                        foreach (BundleInfo bundle in bundles.Values)
//                        {
//                            if (bundle.SplitIndex == -1)
//                            {
//                                if (bundle.IsPatch || !parent.HasPatchDirectory)
//                                {
//                                    bundleDic.Add(Encoding.ASCII.GetBytes(bundle.Name.ToLower()), bundle);
//                                }
//                                else
//                                {
//                                    flags |= Flags.HasBaseBundles;
//                                }
//                            }
//                        }
//                        bundlesCount = bundleDic.Count;

//                        Dictionary<byte[], ChunkInfo> chunkDic = new Dictionary<byte[], ChunkInfo>();
//                        foreach (ChunkInfo chunk in chunks.Values)
//                        {
//                            if (chunk.SplitIndex == -1)
//                            {
//                                if (chunk.IsPatch || !parent.HasPatchDirectory)
//                                {
//                                    chunkDic.Add(chunk.Guid.ToByteArray(), chunk);
//                                }
//                                else
//                                {
//                                    flags |= Flags.HasBaseChunks;
//                                }
//                            }
//                        }
//                        chunksCount = chunkDic.Count;

//                        int[] bundleHashMap = CalculateHashMap(bundleDic, out BundleInfo[] bi);

//                        byte[] stringData;
//                        using (NativeWriter stringWriter = new NativeWriter(new MemoryStream()))
//                        {
//                            for (int i = 0; i < bundlesCount; i++)
//                            {
//                                if (tocFlags.HasFlag(InternalFlags.HasCompressedStrings))
//                                {
//                                    // TODO: huffman strings
//                                    throw new NotImplementedException("compressed names");
//                                }
//                                else
//                                {
//                                    bi[i].NameOffset = (uint)stringWriter.Position;
//                                    stringWriter.WriteNullTerminatedString(bi[i].Name);
//                                }
//                            }
//                            stringData = stringWriter.ToByteArray();
//                        }

//                        bundleHashMapOffset = (uint)(writer.Position - startPos);
//                        for (int i = 0; i < bundlesCount; i++)
//                        {
//#if DEBUG
//                                Debug.Assert(i == GetIndex(Encoding.ASCII.GetBytes(bi[i].Name.ToLower()), bundleHashMap));
//#endif
//                            writer.Write(bundleHashMap[i], Endian.Big);
//                        }

//                        while (((writer.Position - startPos) % 8) != 0)
//                        {
//                            writer.Position++;
//                        }

//                        bundleDataOffset = (uint)(writer.Position - startPos);
//                        for (int i = 0; i < bundlesCount; i++)
//                        {
//#if DEBUG
//                                Debug.Assert(bi[i].Size <= ~0xC0000000);
//#endif
//                            writer.Write(bi[i].NameOffset, Endian.Big);
//                            writer.Write(bi[i].Size, Endian.Big);
//                            writer.Write(bi[i].Offset, Endian.Big);
//                        }

//                        while (((writer.Position - startPos) % 8) != 0)
//                        {
//                            writer.Position++;
//                        }

//                        int[] chunkHashMap = CalculateHashMap(chunkDic, out ChunkInfo[] ci);

//                        chunkHashMapOffset = (uint)(writer.Position - startPos);
//                        for (int i = 0; i < chunksCount; i++)
//                        {
//#if DEBUG
//                                Debug.Assert(i == GetIndex(ci[i].Guid.ToByteArray(), chunkHashMap));
//#endif
//                            writer.Write(chunkHashMap[i], Endian.Big);
//                        }

//                        while (((writer.Position - startPos) % 8) != 0)
//                        {
//                            writer.Position++;
//                        }

//                        byte[] b;
//                        chunkGuidOffset = (uint)(writer.Position - startPos);
//                        for (int i = 0; i < chunksCount; i++)
//                        {
//                            b = ci[i].Guid.ToByteArray();
//                            Array.Reverse(b);
//                            writer.Write(b);
//                            writer.Write((i * 3) | (1 << 24), Endian.Big);
//                        }

//                        while (((writer.Position - startPos) % 8) != 0)
//                        {
//                            writer.Position++;
//                        }

//                        chunkDataOffset = (uint)(writer.Position - startPos);
//                        for (int i = 0; i < chunksCount; i++)
//                        {
//                            writer.Write((byte)0);
//                            writer.Write(ci[i].CasFileInfo.IsPatch);
//                            writer.Write(ci[i].CasFileInfo.CatalogIndex);
//                            writer.Write(ci[i].CasFileInfo.CasIndex);
//                            writer.Write(ci[i].CasFileInfo.Offset, Endian.Big);
//                            writer.Write(ci[i].CasFileInfo.Size, Endian.Big);
//                        }

//                        while (((writer.Position - startPos) % 8) != 0)
//                        {
//                            writer.Position++;
//                        }

//                        namesOffset = (uint)(writer.Position - startPos);
//                        writer.Write(stringData);

//                        if (flags.HasFlag(Flags.HasCompressedNames))
//                        {
//                            tableOffset = (uint)(writer.Position - startPos);
//                        }

//                        if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                        {
//                            // maybe padding not quite sure, since it always ends with huffman table which is always 4 bytes aligned
//                            // no game has normal string table and inline sb
//                            while (((writer.Position - startPos) % 4) != 0)
//                            {
//                                writer.Position++;
//                            }
//                        }

//                        writer.Position = startPos;
//                        writer.Write(bundleHashMapOffset, Endian.Big);
//                        writer.Write(bundleDataOffset, Endian.Big);
//                        writer.Write(bundlesCount, Endian.Big);

//                        writer.Write(chunkHashMapOffset, Endian.Big);
//                        writer.Write(chunkGuidOffset, Endian.Big);
//                        writer.Write(chunksCount, Endian.Big);

//                        writer.Write(chunkDataOffset, Endian.Big);
//                        writer.Write(chunkDataOffset, Endian.Big);

//                        writer.Write(namesOffset, Endian.Big);

//                        writer.Write(chunkDataOffset, Endian.Big);
//                        writer.Write(chunksCount * 3, Endian.Big);

//                        writer.Write((uint)flags, Endian.Big);
//                        if (tocFlags.HasFlag(InternalFlags.HasCompressedStrings))
//                        {
//                            writer.Write(namesCount, Endian.Big);
//                            writer.Write(tableCount, Endian.Big);
//                            writer.Write(tableOffset, Endian.Big);
//                        }

//                        if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                        {
//                            long size = writer.Length - startPos;
//                            writer.Position = bundleDataOffset + startPos;
//                            for (int i = 0; i < bundlesCount; i++)
//                            {
//                                writer.Write(bi[i].NameOffset, Endian.Big);
//                                writer.Write(bi[i].Size | 0x40000000u, Endian.Big);
//                                writer.Write(bi[i].Offset + size, Endian.Big);
//                            }
//                        }

//                        // write sb
//                        writer.Position = writer.Length;

//                        if (bundlesCount > 0)
//                        {
//                            if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                            {
//                                writer.Write(modDefaultWriter.ToByteArray());
//                            }
//                            else
//                            {
//                                sbWriter.Write(modDefaultWriter.ToByteArray());
//                            }
//                        }
//                        sbWriter?.Close();
//                    }
//                }
//                modDefaultWriter.Close();

//                for (int splitIndex = 0; splitIndex < isSplitTocModified.Length; splitIndex++)
//                {
//                    if (isSplitTocModified[splitIndex])
//                    {
//                        string sbName = SuperBundleInfo.Name.Replace("win32", SuperBundleInfo.SplitSuperBundles[splitIndex]);
//                        string modTocPath = string.Format("{0}\\{1}.toc", modPath, sbName);
//                        FileInfo fi = new FileInfo(modTocPath);
//                        Directory.CreateDirectory(fi.DirectoryName);
//                        NativeWriter sbWriter = null;
//                        if (!tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                        {
//                            string modSbPath = string.Format("{0}\\{1}.sb", modPath, sbName);
//                            sbWriter = new NativeWriter(new FileStream(modSbPath, FileMode.Create, FileAccess.Write));
//                        }
//                        using (DbWriter writer = new DbWriter(new FileStream(fi.FullName, FileMode.Create, FileAccess.Write)))
//                        {
//                            writer.Write(0x01CED100);
//                            writer.Position += 0x228;

//                            uint bundleHashMapOffset;
//                            uint bundleDataOffset;
//                            int bundlesCount = 0;

//                            uint chunkHashMapOffset;
//                            uint chunkGuidOffset;
//                            int chunksCount = 0;

//                            uint namesOffset;

//                            uint chunkDataOffset;

//                            Flags flags = 0;

//                            int namesCount = 0;
//                            int tableCount = 0;
//                            uint tableOffset = 0;

//                            long startPos = writer.Position;

//                            writer.Write(0xDEADBEEF, Endian.Big);
//                            writer.Write(0xDEADBEEF, Endian.Big);
//                            writer.Write(0xDEADBEEF, Endian.Big);

//                            writer.Write(0xDEADBEEF, Endian.Big);
//                            writer.Write(0xDEADBEEF, Endian.Big);
//                            writer.Write(0xDEADBEEF, Endian.Big);

//                            writer.Write(0xDEADBEEF, Endian.Big);
//                            writer.Write(0xDEADBEEF, Endian.Big);

//                            writer.Write(0xDEADBEEF, Endian.Big);

//                            writer.Write(0xDEADBEEF, Endian.Big);
//                            writer.Write(0xDEADBEEF, Endian.Big);

//                            writer.Write(0xDEADBEEF, Endian.Big);

//                            if (tocFlags.HasFlag(InternalFlags.HasCompressedStrings))
//                            {
//                                flags |= Flags.HasCompressedNames;
//                                writer.Write(0xDEADBEEF, Endian.Big);
//                                writer.Write(0xDEADBEEF, Endian.Big);
//                                writer.Write(0xDEADBEEF, Endian.Big);
//                            }

//                            Dictionary<byte[], BundleInfo> bundleDic = new Dictionary<byte[], BundleInfo>();
//                            foreach (BundleInfo bundle in bundles.Values)
//                            {
//                                if (bundle.SplitIndex == splitIndex)
//                                {
//                                    if (bundle.IsPatch || !parent.HasPatchDirectory)
//                                    {
//                                        bundleDic.Add(Encoding.ASCII.GetBytes(bundle.Name.ToLower()), bundle);
//                                    }
//                                    else
//                                    {
//                                        flags |= Flags.HasBaseBundles;
//                                    }
//                                }
//                            }
//                            bundlesCount = bundleDic.Count;

//                            Dictionary<byte[], ChunkInfo> chunkDic = new Dictionary<byte[], ChunkInfo>();
//                            foreach (ChunkInfo chunk in chunks.Values)
//                            {
//                                if (chunk.SplitIndex == splitIndex)
//                                {
//                                    if (chunk.IsPatch || !parent.HasPatchDirectory)
//                                    {
//                                        chunkDic.Add(chunk.Guid.ToByteArray(), chunk);
//                                    }
//                                    else
//                                    {
//                                        flags |= Flags.HasBaseChunks;
//                                    }
//                                }
//                            }
//                            chunksCount = chunkDic.Count;

//                            int[] bundleHashMap = CalculateHashMap(bundleDic, out BundleInfo[] bi);

//                            byte[] stringData;
//                            using (NativeWriter stringWriter = new NativeWriter(new MemoryStream()))
//                            {
//                                for (int i = 0; i < bundlesCount; i++)
//                                {
//                                    if (tocFlags.HasFlag(InternalFlags.HasCompressedStrings))
//                                    {
//                                        // // TODO: huffman strings
//                                        throw new NotImplementedException("compressed names");
//                                    }
//                                    else
//                                    {
//                                        bi[i].NameOffset = (uint)stringWriter.Position;
//                                        stringWriter.WriteNullTerminatedString(bi[i].Name);
//                                    }
//                                }
//                                stringData = stringWriter.ToByteArray();
//                            }

//                            bundleHashMapOffset = (uint)(writer.Position - startPos);
//                            for (int i = 0; i < bundlesCount; i++)
//                            {
//#if DEBUG
//                                    Debug.Assert(i == GetIndex(Encoding.ASCII.GetBytes(bi[i].Name.ToLower()), bundleHashMap));
//#endif
//                                writer.Write(bundleHashMap[i], Endian.Big);
//                            }

//                            while (((writer.Position - startPos) % 8) != 0)
//                            {
//                                writer.Position++;
//                            }

//                            bundleDataOffset = (uint)(writer.Position - startPos);
//                            for (int i = 0; i < bundlesCount; i++)
//                            {
//#if DEBUG
//                                    Debug.Assert(bi[i].Size <= ~0xC0000000);
//#endif
//                                writer.Write(bi[i].NameOffset, Endian.Big);
//                                writer.Write(bi[i].Size, Endian.Big);
//                                writer.Write(bi[i].Offset, Endian.Big);
//                            }

//                            while (((writer.Position - startPos) % 8) != 0)
//                            {
//                                writer.Position++;
//                            }

//                            int[] chunkHashMap = CalculateHashMap(chunkDic, out ChunkInfo[] ci);

//                            chunkHashMapOffset = (uint)(writer.Position - startPos);
//                            for (int i = 0; i < chunksCount; i++)
//                            {
//#if DEBUG
//                                    Debug.Assert(i == GetIndex(ci[i].Guid.ToByteArray(), chunkHashMap));
//#endif
//                                writer.Write(chunkHashMap[i], Endian.Big);
//                            }

//                            while (((writer.Position - startPos) % 8) != 0)
//                            {
//                                writer.Position++;
//                            }

//                            byte[] b;
//                            chunkGuidOffset = (uint)(writer.Position - startPos);
//                            for (int i = 0; i < chunksCount; i++)
//                            {
//                                b = ci[i].Guid.ToByteArray();
//                                Array.Reverse(b);
//                                writer.Write(b);
//                                writer.Write((i * 3) | (1 << 24), Endian.Big);
//                            }

//                            while (((writer.Position - startPos) % 8) != 0)
//                            {
//                                writer.Position++;
//                            }

//                            chunkDataOffset = (uint)(writer.Position - startPos);
//                            for (int i = 0; i < chunksCount; i++)
//                            {
//                                writer.Write((byte)0);
//                                writer.Write(ci[i].CasFileInfo.IsPatch);
//                                writer.Write(ci[i].CasFileInfo.CatalogIndex);
//                                writer.Write(ci[i].CasFileInfo.CasIndex);
//                                writer.Write(ci[i].CasFileInfo.Offset, Endian.Big);
//                                writer.Write(ci[i].CasFileInfo.Size, Endian.Big);
//                            }

//                            while (((writer.Position - startPos) % 8) != 0)
//                            {
//                                writer.Position++;
//                            }

//                            namesOffset = (uint)(writer.Position - startPos);
//                            writer.Write(stringData);

//                            if (flags.HasFlag(Flags.HasCompressedNames))
//                            {
//                                tableOffset = (uint)(writer.Position - startPos);
//                                //writer.Write(huffmanTree);
//                            }

//                            if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                            {
//                                // maybe padding not quite sure, since it always ends with huffman table which is always 4 bytes aligned
//                                // no game has normal string table and inline sb
//                                while (((writer.Position - startPos) % 4) != 0)
//                                {
//                                    writer.Position++;
//                                }
//                            }

//                            writer.Position = startPos;
//                            writer.Write(bundleHashMapOffset, Endian.Big);
//                            writer.Write(bundleDataOffset, Endian.Big);
//                            writer.Write(bundlesCount, Endian.Big);

//                            writer.Write(chunkHashMapOffset, Endian.Big);
//                            writer.Write(chunkGuidOffset, Endian.Big);
//                            writer.Write(chunksCount, Endian.Big);

//                            writer.Write(chunkDataOffset, Endian.Big);
//                            writer.Write(chunkDataOffset, Endian.Big);

//                            writer.Write(namesOffset, Endian.Big);

//                            writer.Write(chunkDataOffset, Endian.Big);
//                            writer.Write(chunksCount * 3, Endian.Big);

//                            writer.Write((uint)flags, Endian.Big);
//                            if (tocFlags.HasFlag(InternalFlags.HasCompressedStrings))
//                            {
//                                writer.Write(namesCount, Endian.Big);
//                                writer.Write(tableCount, Endian.Big);
//                                writer.Write(tableOffset, Endian.Big);
//                            }

//                            if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                            {
//                                long size = writer.Length - startPos;
//                                writer.Position = bundleDataOffset + startPos;
//                                for (int i = 0; i < bundlesCount; i++)
//                                {
//                                    writer.Write(bi[i].NameOffset, Endian.Big);
//                                    writer.Write(bi[i].Size | 0x40000000u, Endian.Big);
//                                    writer.Write(bi[i].Offset + size, Endian.Big);
//                                }
//                            }

//                            // write sb
//                            writer.Position = writer.Length;

//                            if (bundlesCount > 0)
//                            {
//                                if (tocFlags.HasFlag(InternalFlags.HasInlineSb))
//                                {
//                                    writer.Write(modSplitWriters[splitIndex].ToByteArray());
//                                }
//                                else
//                                {
//                                    sbWriter.Write(modSplitWriters[splitIndex].ToByteArray());
//                                }
//                            }
//                            sbWriter?.Close();
//                        }
//                    }
//                    modSplitWriters[splitIndex].Close();
//                }

//                casWriter?.Close();
//            }
//            catch (Exception e)
//            {
//                Exception = e;
//            }
//        }

//        private string GetCatalog(SuperBundleInfo sb, IEnumerable<Catalog> catalogs)
//        {
//            foreach (Catalog catalog in catalogs)
//            {
//                if (catalog.SuperBundles.Keys.Contains(sb.Name))
//                {
//                    return catalog.Name;
//                }
//            }
//            return catalogs.First().Name;
//        }

//        private void ReadToc(NativeReader reader, ref Dictionary<int, BundleInfo> bundles, ref Dictionary<Guid, ChunkInfo> chunks, ref InternalFlags tocFlags, bool patch, int splitIndex = -1)
//        {
//            uint startPos = (uint)reader.Position;

//            uint bundleHashMapOffset = reader.ReadUInt(Endian.Big) + startPos;
//            uint bundleDataOffset = reader.ReadUInt(Endian.Big) + startPos;
//            int bundlesCount = reader.ReadInt(Endian.Big);

//            uint chunkHashMapOffset = reader.ReadUInt(Endian.Big) + startPos;
//            uint chunkGuidOffset = reader.ReadUInt(Endian.Big) + startPos;
//            int chunksCount = reader.ReadInt(Endian.Big);

//            uint unknownOffset1 = reader.ReadUInt(Endian.Big) + startPos; // not used
//            uint unknownOffset2 = reader.ReadUInt(Endian.Big) + startPos; // not used

//            uint namesOffset = reader.ReadUInt(Endian.Big) + startPos;

//            uint chunkDataOffset = reader.ReadUInt(Endian.Big) + startPos;
//            int dataCount = reader.ReadInt(Endian.Big);

//            Flags flags = (Flags)reader.ReadInt(Endian.Big);

//            if (flags.HasFlag(Flags.HasBaseBundles) || flags.HasFlag(Flags.HasBaseChunks))
//            {
//                string tocPath = FileSystem.Instance.ResolvePath(string.Format("native_data/{0}.toc", splitIndex != -1 ? SuperBundleInfo.Name.Replace("win32", SuperBundleInfo.SplitSuperBundles[splitIndex]) : SuperBundleInfo.Name));
//                using (NativeReader baseReader = new NativeReader(tocPath))
//                {
//                    InternalFlags discard = 0;
//                    baseReader.Skip(556);
//                    ReadToc(baseReader, ref bundles, ref chunks, ref discard, false, splitIndex);
//                }
//            }

//            uint namesCount = 0;
//            uint tableCount = 0;
//            uint tableOffset = uint.MaxValue;
//            CompressedStringHandler huffmanDecoder = null;

//            if (flags.HasFlag(Flags.HasCompressedNames))
//            {
//                tocFlags |= InternalFlags.HasCompressedStrings;
//                huffmanDecoder = new CompressedStringHandler();
//                namesCount = reader.ReadUInt(Endian.Big);
//                tableCount = reader.ReadUInt(Endian.Big);
//                tableOffset = reader.ReadUInt(Endian.Big) + startPos;
//            }

//#if DEBUG
//                Debug.Assert(unknownOffset1 == chunkDataOffset && unknownOffset2 == chunkDataOffset);
//#endif

//            if (bundlesCount != 0)
//            {
//                if (flags.HasFlag(Flags.HasCompressedNames))
//                {
//                    reader.Position = namesOffset;
//                    huffmanDecoder.ReadEncodedData(reader, namesCount, Endian.Big);

//                    reader.Position = tableOffset;
//                    huffmanDecoder.ReadHuffmanTable(reader, tableCount, Endian.Big);
//                }

//                List<int> bundleHashMap = new List<int>(bundlesCount);
//                reader.Position = bundleHashMapOffset;
//                for (int i = 0; i < bundlesCount; i++)
//                {
//                    bundleHashMap.Add(reader.ReadInt(Endian.Big));
//                }

//                reader.Position = bundleDataOffset;

//                for (int i = 0; i < bundlesCount; i++)
//                {
//                    int nameOffset = reader.ReadInt(Endian.Big);
//                    uint bundleSize = reader.ReadUInt(Endian.Big); // flag in first 2 bits: 0x40000000 inline sb
//                    long bundleOffset = reader.ReadLong(Endian.Big);

//                    string name = "";

//                    if (flags.HasFlag(Flags.HasCompressedNames))
//                    {
//                        name = huffmanDecoder.ReadCompressedString(nameOffset);
//                    }
//                    else
//                    {
//                        long curPos = reader.Position;
//                        reader.Position = namesOffset + nameOffset;
//                        name = reader.ReadNullTerminatedString();
//                        reader.Position = curPos;
//                    }

//                    int hash = Fnv1.HashString(name.ToLower());
//                    if (bundles.ContainsKey(hash))
//                    {
//                        bundles.Remove(hash);
//                    }

//                    BundleInfo bi = new BundleInfo()
//                    {
//                        Name = name,
//                        Offset = bundleOffset,
//                        Size = bundleSize,
//                        IsPatch = patch,
//                        SbName = splitIndex != -1 ? SuperBundleInfo.Name.Replace("win32", SuperBundleInfo.SplitSuperBundles[splitIndex]) : SuperBundleInfo.Name,
//                        SplitIndex = splitIndex
//                    };

//                    if (bundleSize != uint.MaxValue && bundleOffset != -1)
//                    {
//                        bundles.Add(hash, bi);
//                    }
//                }
//            }
//            if (chunksCount != 0)
//            {
//                reader.Position = chunkHashMapOffset;
//                List<int> chunkHashMap = new List<int>(chunksCount);
//                for (int i = 0; i < chunksCount; i++)
//                {
//                    chunkHashMap.Add(reader.ReadInt(Endian.Big));
//                }

//                reader.Position = chunkGuidOffset;
//                Guid[] chunkGuids = new Guid[dataCount / 3];
//                for (int i = 0; i < chunksCount; i++)
//                {
//                    byte[] b = reader.ReadBytes(16);
//                    Guid guid = new Guid(new byte[]
//                    {
//                            b[15], b[14], b[13], b[12],
//                            b[11], b[10], b[9], b[8],
//                            b[7], b[6], b[5], b[4],
//                            b[3], b[2], b[1], b[0]
//                    });

//                    // 0xFFFFFFFF remove chunk
//                    int index = reader.ReadInt(Endian.Big);

//                    if (index != -1)
//                    {
//                        // im guessing the unknown offsets are connected to this
//                        byte flag = (byte)((index & 0xFF000000) >> 24);
//#if DEBUG
//                            Debug.Assert(flag == 1);
//#endif
//                        index = (index & 0xFFFFFF) / 3;

//                        chunkGuids[index] = guid;
//                    }
//                }
//                reader.Position = chunkDataOffset;
//                for (int i = 0; i < (dataCount / 3); i++)
//                {
//                    byte unk = reader.ReadByte();
//                    bool isPatch = reader.ReadBoolean();
//                    byte catalogIndex = reader.ReadByte();
//                    byte casIndex = reader.ReadByte();
//                    uint offset = reader.ReadUInt(Endian.Big);
//                    uint size = reader.ReadUInt(Endian.Big);

//                    ChunkInfo ci = new ChunkInfo()
//                    {
//                        Guid = chunkGuids[i],
//                        IsPatch = patch,
//                        CasFileInfo = new CasFileInfo() { IsPatch = isPatch, CatalogIndex = catalogIndex, CasIndex = casIndex, Offset = offset, Size = size },
//                        SbName = splitIndex != -1 ? SuperBundleInfo.Name.Replace("win32", SuperBundleInfo.SplitSuperBundles[splitIndex]) : SuperBundleInfo.Name,
//                        SplitIndex = splitIndex
//                    };

//                    if (chunks.ContainsKey(ci.Guid))
//                    {
//                        chunks.Remove(ci.Guid);
//                    }

//                    chunks.Add(ci.Guid, ci);
//                }
//            }
//        }

//        private DbObject ReadBundle(BinarySbReader reader, ref InternalFlags tocFlags)
//        {
//            int bundleOffset = reader.ReadInt(Endian.Big);
//            int bundleSize = reader.ReadInt(Endian.Big);
//            uint locationOffset = reader.ReadUInt(Endian.Big);
//            uint totalCount = reader.ReadUInt(Endian.Big);
//            uint dataOffset = reader.ReadUInt(Endian.Big);

//            if (!(bundleOffset == 0 && bundleSize == 0))
//            {
//                tocFlags |= InternalFlags.HasInlineBundle;
//            }

//            reader.Position = locationOffset;

//            bool[] flags = new bool[totalCount];
//            for (uint i = 0; i < totalCount; i++)
//                flags[i] = reader.ReadBoolean();

//            byte unused = 0;
//            bool isPatch = false;
//            int catalogIndex = 0;
//            int casIndex = 0;
//            int offset = 0;
//            int size = 0;
//            int z = 0;

//            DbObject bundle;

//            if (tocFlags.HasFlag(InternalFlags.HasInlineBundle))
//            {
//                reader.Position = bundleOffset;
//                bundle = reader.ReadDbObject();

//                reader.Position = dataOffset;
//            }
//            else
//            {
//                reader.Position = dataOffset;

//                if (flags[z++])
//                {
//                    unused = reader.ReadByte();
//#if DEBUG
//                        Debug.Assert(unused == 0);
//#endif
//                    isPatch = reader.ReadBoolean();
//                    catalogIndex = reader.ReadByte();
//                    casIndex = reader.ReadByte();
//                }
//                offset = reader.ReadInt(Endian.Big);
//                size = reader.ReadInt(Endian.Big);

//                string path = FileSystem.Instance.GetFilePath(catalogIndex, casIndex, isPatch);

//                using (Stream casStream = new FileStream(FileSystem.Instance.ResolvePath(path), FileMode.Open, FileAccess.Read))
//                {
//                    byte[] buffer = new byte[size];
//                    casStream.Position = offset;
//                    casStream.Read(buffer, 0, size);

//                    using (BinarySbReader casBundleReader = new BinarySbReader(new MemoryStream(buffer), 0, FileSystem.Instance.CreateDeobfuscator()))
//                    {
//                        bundle = casBundleReader.ReadDbObject();
//#if DEBUG
//                            Debug.Assert(casBundleReader.TotalCount == totalCount - 1);
//#endif
//                    }
//                }
//            }

//            for (int i = 0; i < bundle.GetValue<DbObject>("ebx").Count; i++)
//            {
//                if (flags[z++])
//                {
//                    unused = reader.ReadByte();
//                    isPatch = reader.ReadBoolean();
//                    catalogIndex = reader.ReadByte();
//                    casIndex = reader.ReadByte();
//                }

//                DbObject ebx = bundle.GetValue<DbObject>("ebx")[i] as DbObject;
//                offset = reader.ReadInt(Endian.Big);
//                size = reader.ReadInt(Endian.Big);

//                ebx.SetValue("catalog", catalogIndex);
//                ebx.SetValue("cas", casIndex);
//                ebx.SetValue("offset", offset);
//                ebx.SetValue("size", size);
//                if (isPatch)
//                {
//                    ebx.SetValue("patch", true);
//                }
//            }

//            for (int i = 0; i < bundle.GetValue<DbObject>("res").Count; i++)
//            {
//                if (flags[z++])
//                {
//                    unused = reader.ReadByte();
//                    isPatch = reader.ReadBoolean();
//                    catalogIndex = reader.ReadByte();
//                    casIndex = reader.ReadByte();
//                }

//                DbObject res = bundle.GetValue<DbObject>("res")[i] as DbObject;
//                offset = reader.ReadInt(Endian.Big);
//                size = reader.ReadInt(Endian.Big);

//                res.SetValue("catalog", catalogIndex);
//                res.SetValue("cas", casIndex);
//                res.SetValue("offset", offset);
//                res.SetValue("size", size);
//                if (isPatch)
//                {
//                    res.SetValue("patch", true);
//                }
//            }

//            for (int i = 0; i < bundle.GetValue<DbObject>("chunks").Count; i++)
//            {
//                if (flags[z++])
//                {
//                    unused = reader.ReadByte();
//                    isPatch = reader.ReadBoolean();
//                    catalogIndex = reader.ReadByte();
//                    casIndex = reader.ReadByte();
//                }

//                DbObject chunk = bundle.GetValue<DbObject>("chunks")[i] as DbObject;
//                offset = reader.ReadInt(Endian.Big);
//                size = reader.ReadInt(Endian.Big);

//                chunk.SetValue("catalog", catalogIndex);
//                chunk.SetValue("cas", casIndex);
//                chunk.SetValue("offset", offset);
//                chunk.SetValue("size", size);
//                if (isPatch)
//                {
//                    chunk.SetValue("patch", true);
//                }
//            }

//            return bundle;
//        }

//        private NativeWriter GetNextCas(string catName, out int casFileIndex)
//        {
//            lock (locker)
//            {
//                // TODO: see if this works
//                casFileIndex = CasFiles[catName];
//                CasFiles[catName]++;
//            }

//            FileInfo fi = new FileInfo(FileSystem.Instance.BasePath + parent.ModDirectoryName + "\\" + "Patch" + "\\" + catName + "\\cas_" + casFileIndex.ToString("D2") + ".cas");
//            Directory.CreateDirectory(fi.DirectoryName);

//            return new NativeWriter(new FileStream(fi.FullName, FileMode.Create));
//        }

//        private int[] CalculateHashMap<T>(Dictionary<byte[], T> input, out T[] sortedItems)
//        {
//            int size = input.Keys.Count;

//            sortedItems = new T[size];

//            // default value is -1, so it just returns the first element
//            int[] hashMap = new int[size];
//            for (int i = 0; i < size; i++)
//            {
//                hashMap[i] = -1;
//            }

//            // add all hashes
//            List<byte[]>[] hashDict = new List<byte[]>[size];
//            for (int i = 0; i < size; i++)
//            {
//                hashDict[i] = new List<byte[]>();
//            }
//            foreach (var key in input.Keys)
//            {
//                hashDict[(int)(Hash(key) % size)].Add(key);
//            }

//            // sort them so that the ones with the most duplicates are first
//            Array.Sort(hashDict, (List<byte[]> x, List<byte[]> y) => y.Count.CompareTo(x.Count));

//            bool[] used = new bool[size];

//            List<int> indices;

//            int hash = 0;
//            // process hash conflicts
//            for (; hash < size; hash++)
//            {
//                int duplicateCount = hashDict[hash].Count;

//                if (hashDict[hash].Count <= 1)
//                {
//                    break;
//                }

//                indices = new List<int>(duplicateCount);

//                // find seed for which all hashes are unique and not already used
//                uint seed = 1;
//                int i = 0;
//                while (i < duplicateCount)
//                {
//                    int index = (int)(Hash(hashDict[hash][i], seed) % size);
//                    if (used[index] || indices.FindIndex(k => k == index) != -1)
//                    {
//                        seed++;
//                        i = 0;
//                        indices.Clear();
//                    }
//                    else
//                    {
//                        indices.Add(index);
//                        i++;
//                    }
//                }

//                // set seed as hashmap value
//                hashMap[(int)(Hash(hashDict[hash][0]) % size)] = (int)seed;

//                // set output values and make indices used
//                for (int j = 0; j < duplicateCount; j++)
//                {
//                    int index = indices[j];
//                    sortedItems[index] = input[hashDict[hash][j]];
//                    used[index] = true;
//                }
//            }

//            // process unique hashes
//            int idx = 0;
//            for (; hash < size; hash++)
//            {
//                if (hashDict[hash].Count == 0)
//                {
//                    break;
//                }

//                // find first free spot
//                while (used[idx])
//                {
//                    idx = (idx + 1) % size;
//                }

//                // set correct index for hash
//                hashMap[Hash(hashDict[hash][0]) % size] = (-1 * idx) - 1;
//                sortedItems[idx] = input[hashDict[hash][0]];

//                // advance index
//                idx = (idx + 1) % size;
//            }

//            return hashMap;
//        }

//        private int GetIndex(byte[] key, int[] hashMap)
//        {
//            int index = hashMap[Hash(key) % hashMap.Length];

//            if (index < 0)
//                index = (index + 1) * -1;
//            else
//                index = (int)(Hash(key, (uint)index) % hashMap.Length);

//            return index;
//        }

//        private uint Hash(byte[] bytes, uint offset = 0x811c9dc5)
//        {
//            uint prime = 0x01000193;

//            uint hash = offset;

//            for (int i = 0; i < bytes.Length; i++)
//            {
//                hash = (hash * prime) ^ (uint)(sbyte)bytes[i];
//            }

//            return hash % prime;
//        }
//    }
//}
