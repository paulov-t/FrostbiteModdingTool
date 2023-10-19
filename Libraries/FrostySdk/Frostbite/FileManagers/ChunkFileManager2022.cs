using FMT.FileTools;
using FMT.FileTools.Modding;
using FMT.Logging;
using FrostbiteSdk.Frostbite.FileManagers;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Frostbite.FileManagers
{
    public class ChunkFileManager2022 : IChunkFileManager, ICustomAssetManager//, IDisposable
    {
        protected ILogger Logger { get; set; }

        public long ChunkFileCollectorDataSize { get; } = 10485760L;

        public long ChunkFileCollectorFixup { get; } = 680960L;

        public List<LegacyFileEntry> AddedFileEntries { get; set; } = new List<LegacyFileEntry>();

        public List<LegacyFileEntry> OriginalLegacyEntries { get; } = new List<LegacyFileEntry>();

        public Dictionary<string, LegacyFileEntry> LegacyEntries { get; private set; } = new Dictionary<string, LegacyFileEntry>();

        public List<ChunkAssetEntry> ModifiedChunks { get; private set; } = new List<ChunkAssetEntry>();

        protected Dictionary<Guid, List<LegacyFileEntry>> LegacyChunksToParent = new Dictionary<Guid, List<LegacyFileEntry>>();

        public AssetManager AssetManager => AssetManager.Instance;

        public class ChunkBatch
        {
            public uint UnkCount1 { get; set; }
            public long Offset1_Unk { get; set; }
            public int NumberOfFiles { get; set; }

            public long Offset2_Files { get; set; }

            public uint BootableItemCount { get; set; }


            public byte[] Initial64Data { get; set; }

            public EbxAssetEntry EbxAssetEntry { get; set; }

            public ChunkAssetEntry ChunkAssetEntry { get; set; }

            public IEnumerable<LegacyFileEntry> BatchLegacyFiles
            {
                get
                {
                    var lam = (ChunkFileManager2022)AssetManager.Instance.GetLegacyAssetManager();
                    if (lam != null)
                    {
                        return lam.LegacyEntries.Values.Where(x => x.ParentGuid == ChunkAssetEntry.Id);
                    }
                    return null;
                }
            }

            public ChunkBatch()
            {
            }

            public override string ToString()
            {
                return ChunkAssetEntry != null ? ChunkAssetEntry.Id.ToString() : base.ToString();
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public byte[] WriteChunkBatch()
            {
                return null;
            }

            public Dictionary<Guid, List<LegacyFileEntry>> ChunkGroupsInBatch
            {
                get
                {
                    return BatchLegacyFiles.GroupBy(x => x.ChunkId).ToDictionary(x => x.Key, x => x.ToList());
                }
            }

            public Dictionary<Guid, List<LegacyFileEntry>> ChunkGroupsInBatchModified
            {
                get
                {
                    return BatchLegacyFiles
                        .Where(x => x.ModifiedEntry != null && x.ModifiedEntry.ChunkId.HasValue)
                        .GroupBy(x => x.ModifiedEntry.ChunkId.Value)
                        .ToDictionary(x => x.Key, x => x.ToList());
                }
            }

            public Dictionary<Guid, ChunkAssetEntry> ChunkAssetEntriesInBatch
            {
                get
                {
                    return BatchLegacyFiles.GroupBy(x => x.ChunkId).ToDictionary(x => x.Key, x => AssetManager.Instance.GetChunkEntry(x.Key));
                }
            }

            public LegacyFileEntry GetLegacyFileByName(string name)
            {
                return BatchLegacyFiles.FirstOrDefault(x => x.Name == name);
            }

            public int EndOfStrings { get; set; }
            public byte[] EndData { get; set; }
            public long BootableItemOffset { get; set; }
            public uint LinkedChunkCount { get; set; }
            public long LinkedChunkOffset { get; set; }
            public EbxAsset EbxAsset { get; set; }

            public List<ChunkAssetEntry> CompressedItemChunks = new List<ChunkAssetEntry>();

            public List<BootableItem> BootableItems = new List<BootableItem>();

            public List<ChunkAssetEntry> LinkedChunks = new List<ChunkAssetEntry>();

            public List<UnknownItem> UnknownItems = new List<UnknownItem>();

            public List<int> BottomUnknownOffsets { get; } = new List<int>();

            public class BootableItem
            {
                public long NameOffset;
                public string Name;
                public uint Index;
                public uint Active;
            }

            public class UnknownItem
            {
                public long Offset { get; set; }
                public string Name { get; set; }
            }
        }

        /// <summary>
        /// Chunk Batches are the entire batch of chunks and locations of the chunks. Each is an EBX
        /// </summary>
        public List<ChunkBatch> ChunkBatches { get; private set; } = new List<ChunkBatch>();


        public ChunkFileManager2022()
        {
        }

        public virtual bool HasChunkFileCollectorsLoaded()
        {
            return AssetManager.EnumerateEbx("ChunkFileCollector").Count() > 0;
        }

        public virtual void LoadChunkFileCollectors(string directory = "native_patch")
        {
            foreach (var sbName in FileSystem.Instance.SuperBundles.Where(x=>x.Contains("global")))
            {
                var tocFileRAW = $"{directory}/{sbName}.toc";
                string tocFileLocation = FileSystem.Instance.ResolvePath(tocFileRAW);
                if (string.IsNullOrEmpty(tocFileLocation) || !File.Exists(tocFileLocation))
                {
                    AssetManager.Instance.Logger.LogWarning($"Unable to find Toc {tocFileRAW}");
                    continue;
                }

                Logger.Log($"Loading data ({tocFileRAW})");
                using var tf = (IDisposable)Activator.CreateInstance(FileSystem.Instance.TOCFileType, tocFileRAW, true, true, false, 0, false);
            }

            if (directory == "native_patch")
                LoadChunkFileCollectors("native_data");
        }

        public virtual void Initialize(ILogger logger)
        {
            Logger = logger;

            logger.Log("Loading legacy files");
            AddedFileEntries = new List<LegacyFileEntry>();
            OriginalLegacyEntries.Clear();
            LegacyEntries.Clear();
            ModifiedChunks.Clear();
            LegacyChunksToParent.Clear();
            ChunkBatches.Clear();

            if (!HasChunkFileCollectorsLoaded())
            {
                LoadChunkFileCollectors();
                AssetManager.Instance.DoEbxIndexing();
            }

            var chunkFileCollectorEbxEntries = AssetManager.EnumerateEbx("ChunkFileCollector");
            chunkFileCollectorEbxEntries = chunkFileCollectorEbxEntries.Where(x => !x.Name.Contains("_EAData", StringComparison.OrdinalIgnoreCase));

            foreach (EbxAssetEntry ebxEntry in chunkFileCollectorEbxEntries)
            {
                AssetManager.Instance.RevertAsset(ebxEntry);
            }

            foreach (EbxAssetEntry ebxEntry in chunkFileCollectorEbxEntries)
            {
                GetChunkAssetForEbx(ebxEntry, out ChunkAssetEntry chunkAssetEntry, out EbxAsset ebxAsset);

                if (chunkAssetEntry == null)
                    continue;

                AssetManager.Instance.RevertAsset(chunkAssetEntry);

                chunkAssetEntry.IsLegacy = true;

                MemoryStream chunk = GetChunkStreamForEbx(ebxEntry) as MemoryStream;
                if (chunk == null)
                    continue;

                {
                    using (NativeReader nativeReader = new NativeReader(chunk))
                    {
                        //File.WriteAllBytes("_debug_legacy_" + ebxEntry.Name.Replace(@"/","_") + ".dat", chunk.ToArray());
                        nativeReader.Position = 0;

                        var chunkBatch = new ChunkBatch()
                        {
                            EbxAssetEntry = ebxEntry,
                            EbxAsset = ebxAsset,
                            ChunkAssetEntry = chunkAssetEntry,
                            UnkCount1 = nativeReader.ReadUInt(), // 0
                            Offset1_Unk = nativeReader.ReadLong(), // 4
                            NumberOfFiles = nativeReader.ReadInt(), // 12
                            Offset2_Files = nativeReader.ReadLong(), // 16
                            BootableItemCount = nativeReader.ReadUInt(), // 24 
                            BootableItemOffset = nativeReader.ReadLong(), // 28
                            LinkedChunkCount = nativeReader.ReadUInt(), // 36
                            LinkedChunkOffset = nativeReader.ReadLong() // 40
                        };
                        //nativeReader.Position = 0;
                        //chunkBatch.Initial64Data = nativeReader.ReadBytes((int)chunkBatch.FileFirstPosition);
                        nativeReader.Position = chunkBatch.Offset1_Unk;
                        for (uint index = 0u; index < chunkBatch.UnkCount1; index++)
                        {
                            var unkLong = nativeReader.ReadLong();
                            var name = string.Empty;
                            chunkBatch.UnknownItems.Add(new ChunkBatch.UnknownItem() { Offset = unkLong, Name = name });
                        }

                        chunkBatch.EndOfStrings = 0;
                        nativeReader.Position = chunkBatch.Offset2_Files;
                        for (uint index = 0u; index < chunkBatch.NumberOfFiles; index++)
                        {
                            LegacyFileEntry legacyFileEntry = new LegacyFileEntry();
                            legacyFileEntry.EbxAssetEntry = ebxEntry;
                            legacyFileEntry.FileNameInBatchOffset = nativeReader.ReadLong(); // 0 - 8

                            //legacyFileEntry.BatchOffset = positionOfItem;
                            legacyFileEntry.ParentGuid = chunkAssetEntry.Id;

                            legacyFileEntry.CompressedOffsetPosition = nativeReader.Position;
                            legacyFileEntry.CompressedOffset = nativeReader.ReadLong();
                            legacyFileEntry.CompressedOffsetStart = legacyFileEntry.CompressedOffset;
                            legacyFileEntry.CompressedSizePosition = nativeReader.Position;
                            legacyFileEntry.CompressedOffsetEnd = nativeReader.ReadLong();
                            legacyFileEntry.CompressedSize = legacyFileEntry.CompressedOffsetEnd - legacyFileEntry.CompressedOffset;

                            legacyFileEntry.ActualOffsetPosition = (int)nativeReader.Position;
                            legacyFileEntry.ExtraData = new AssetExtraData() { DataOffset = (uint)nativeReader.ReadLong() };
                            legacyFileEntry.ActualSizePosition = (int)nativeReader.Position;
                            legacyFileEntry.Size = nativeReader.ReadLong();

                            legacyFileEntry.ChunkIdPosition = nativeReader.Position;
                            var chunkId = nativeReader.ReadGuid();
                            if (chunkId != Guid.Empty)
                            {
                                var cha = AssetManager.Instance.GetChunkEntry(chunkId);
                                AssetManager.Instance.RevertAsset(cha);
                                cha.IsLegacy = true;

                                legacyFileEntry.ChunkId = chunkId;

                                nativeReader.Position = legacyFileEntry.FileNameInBatchOffset;
                                string name = nativeReader.ReadNullTerminatedString();

                                if (!LegacyEntries.ContainsKey(name))
                                {
                                    legacyFileEntry.Name = name;
                                    LegacyEntries.Add(name, legacyFileEntry);
                                }
                                else
                                {
                                    legacyFileEntry = LegacyEntries[name];
                                }
                            }

                            nativeReader.Position = chunkBatch.Offset2_Files + ((8 + 8 + 8 + 8 + 8 + 16) * (index + 1));

                        }

                        if (ProfileManager.IsLoaded(EGame.FIFA21, EGame.FIFA22, EGame.FIFA23, EGame.FC24)
                            )
                        {
                            nativeReader.Position = chunkBatch.BootableItemOffset;
                            for (uint j = 0u; j < chunkBatch.BootableItemCount; j++)
                            {
                                long nameOffset3 = nativeReader.ReadLong();
                                var unknumber1 = nativeReader.ReadUInt();
                                var unknumber2 = nativeReader.ReadUInt();
                                nativeReader.Position = nameOffset3;
                                var unkName2 = nativeReader.ReadNullTerminatedString();
                                if (nativeReader.Position > chunkBatch.EndOfStrings)
                                    chunkBatch.EndOfStrings = (int)nativeReader.Position;

                                nativeReader.Position = chunkBatch.BootableItemOffset + (16 * (j + 1));


                                ChunkBatch.BootableItem bootableItem = new ChunkBatch.BootableItem()
                                {
                                    NameOffset = nameOffset3,
                                    Name = unkName2,
                                    Index = unknumber1,
                                    Active = unknumber2
                                };
                                chunkBatch.BootableItems.Add(bootableItem);
                            }

                            nativeReader.Position = chunkBatch.LinkedChunkOffset;
                            for (uint i = 0u; i < chunkBatch.LinkedChunkCount; i++)
                            {
                                var chunkOriginalSize = nativeReader.ReadLong();
                                var chunkGuid = nativeReader.ReadGuid();
                                var otherChunk = AssetManager.Instance.GetChunkEntry(chunkGuid);
                                otherChunk.OriginalSize = chunkOriginalSize;
                                chunkBatch.LinkedChunks.Add(otherChunk);
                            }

                            nativeReader.Position = chunkBatch.EndOfStrings;
                            nativeReader.Pad(16);
                            double numberOfEndUnkItems = (nativeReader.Length - (double)nativeReader.Position) / 4;
                            for (int index = 0; index < numberOfEndUnkItems; index++)
                            {
                                //chunkBatch.BottomUnknownOffsets.Add(nativeReader.ReadInt());
                            }
                        }

                        ChunkBatches.Add(chunkBatch);

                    }
                }
            }

            logger.Log($"Loaded {LegacyEntries.Count} legacy files");
            ChunkFileManager.Instance = this;
        }

        public void ReInitialize(ILogger logger)
        {
            Initialize(logger);
        }

        public void WriteAllLegacy(bool assertEquivalent = false)
        {
            // A chunk batch corresponds to each Ebx in the CFC
            foreach (ChunkBatch chunkBatch in ChunkBatches)
            {
                var originalData = (AssetManager.Instance.GetChunk(chunkBatch.ChunkAssetEntry) as MemoryStream).ToArray();

                MemoryStream ms_newChunkBatch = new MemoryStream();
                using (NativeWriter nw = new NativeWriter(ms_newChunkBatch, true))
                {
                    Debug.WriteLine("Processing Legacy Item: " + chunkBatch.EbxAssetEntry.Name);
                    nw.Write(chunkBatch.UnkCount1);
                    nw.Write(chunkBatch.Offset1_Unk);
                    var oldNumberOfFiles = chunkBatch.NumberOfFiles;
                    var newNumberOfFiles = (uint)chunkBatch.BatchLegacyFiles.Count();
                    nw.Write(newNumberOfFiles);
                    nw.Write(chunkBatch.Offset2_Files);
                    nw.Write(chunkBatch.BootableItemCount);
                    nw.Write(chunkBatch.BootableItemOffset);
                    nw.Write(chunkBatch.LinkedChunkCount);
                    nw.Write(chunkBatch.LinkedChunkOffset);

                    foreach (var item in chunkBatch.UnknownItems)
                    {
                        nw.Write(item.Offset);
                    }

                    chunkBatch.Offset2_Files = nw.Position;
                    // Quick write before Strings
                    foreach (var item in chunkBatch.BatchLegacyFiles)
                    {
                        if (item.ModifiedEntry != null)
                        {

                        }
                        if (item.Name.Contains("p258487"))
                        {

                        }
                        nw.Write((ulong)item.FileNameInBatchOffset);
                        // compressed
                        nw.Write(item.ModifiedEntry != null && item.ModifiedEntry.CompressedOffset.HasValue ? (ulong)item.ModifiedEntry.CompressedOffset : (ulong)item.CompressedOffset);
                        // compressed end
                        nw.Write(item.ModifiedEntry != null && item.ModifiedEntry.CompressedOffsetEnd.HasValue ? (ulong)item.ModifiedEntry.CompressedOffsetEnd : (ulong)item.CompressedOffsetEnd);
                        // data offset
                        nw.Write(item.ModifiedEntry != null && item.ModifiedEntry.NewOffset.HasValue ? (ulong)item.ModifiedEntry.NewOffset : item.ExtraData.DataOffset);
                        // data size
                        nw.Write(item.ModifiedEntry != null && item.ModifiedEntry.Data != null ? (ulong)item.ModifiedEntry.Data.Length : (ulong)item.Size);
                        nw.Write(item.ModifiedEntry != null && item.ModifiedEntry.ChunkId.HasValue ? item.ModifiedEntry.ChunkId.Value : item.ChunkId);
                    }

                    nw.WritePadding(16);

                    chunkBatch.BootableItemOffset = nw.Position;
                    if (chunkBatch.BootableItems.Any())
                    {
                        foreach (var item in chunkBatch.BootableItems)
                        {
                            nw.Write(item.NameOffset);
                            nw.Write(item.Index);
                            nw.Write(item.Active);
                        }


                    }

                    chunkBatch.LinkedChunkOffset = nw.Position;
                    if (chunkBatch.LinkedChunks.Any())
                    {
                        foreach (var item in chunkBatch.LinkedChunks)
                        {
                            nw.Write(item.OriginalSize);
                            nw.Write(item.Id);
                        }
                    }

                    nw.WritePadding(16);
                    nw.WriteNullTerminatedString("Source/runtime/fifa/pc64");
                    nw.WriteNullTerminatedString("Source/runtime/fifa/cmn");

                    // Strings
                    foreach (var item in chunkBatch.BatchLegacyFiles)
                    {
                        item.FileNameInBatchOffset = nw.Position;
                        nw.WriteNullTerminatedString(item.Name);
                    }
                    foreach (var item in chunkBatch.BootableItems)
                    {
                        item.NameOffset = nw.Position;
                        nw.WriteNullTerminatedString(item.Name);
                    }

                    nw.WritePadding(16);
                    var endOfStrings = nw.Position;

                    nw.Position = chunkBatch.BootableItemOffset;
                    foreach (var item in chunkBatch.BootableItems)
                    {
                        nw.Write(item.NameOffset);
                        nw.Write(item.Index);
                        nw.Write(item.Active);
                    }


                    nw.Position = 0;
                    nw.Write(chunkBatch.UnkCount1);
                    nw.Write(chunkBatch.Offset1_Unk);
                    nw.Write(newNumberOfFiles);
                    nw.Write(chunkBatch.Offset2_Files);
                    nw.Write(chunkBatch.BootableItemCount);
                    nw.Write(chunkBatch.BootableItemOffset);
                    nw.Write(chunkBatch.LinkedChunkCount);
                    nw.Write(chunkBatch.LinkedChunkOffset);

                    nw.Position = nw.Length;
                    foreach (var item in chunkBatch.BottomUnknownOffsets)
                    {
                        nw.Write(item);
                    }

                    nw.Position = chunkBatch.Offset2_Files;
                    foreach (var item in chunkBatch.BatchLegacyFiles)
                    {
                        if (item.ModifiedEntry != null)
                        {

                        }
                        if (item.Name.Contains("p258487"))
                        {

                        }
                        nw.Write((ulong)item.FileNameInBatchOffset);
                        nw.Position += 8;
                        nw.Position += 8;
                        nw.Position += 8;
                        nw.Position += 8;
                        nw.Position += 16;

                    }
                }


                //File.WriteAllBytes("_debug_legacy_old_" + chunkBatch.EbxAssetEntry.Name.Replace(@"/", "_") + ".dat", originalData);
                //File.WriteAllBytes("_debug_legacy_new_" + chunkBatch.EbxAssetEntry.Name.Replace(@"/", "_") + ".dat", ms_newChunkBatch.ToArray());


                AssetManager.Instance.ModifyChunk(chunkBatch.ChunkAssetEntry.Id, ms_newChunkBatch.ToArray(), null, ProfileManager.GetCompressionType(ProfileManager.CompTypeArea.Legacy));
                //AssetManager.Instance.ModifyEbx(chunkBatch.EbxAssetEntry.Name, chunkBatch.EbxAsset);
            }
        }

        public static void GetChunkAssetForEbx(EbxAssetEntry ebxAssetEntry, out ChunkAssetEntry chunkAssetEntry, out EbxAsset ebxAsset)
        {
            chunkAssetEntry = null;
            ebxAsset = AssetManager.Instance.GetEbx(ebxAssetEntry);
            if(ebxAsset == null)
            {
                Debug.WriteLine($"Unable to find the ebxAsset for {ebxAssetEntry.Name}");
                FileLogger.WriteLine($"Unable to find the ebxAsset for {ebxAssetEntry.Name}");
                return;
            }

            dynamic rootObject = ebxAsset.RootObject;
            if (rootObject == null)
            {
                Debug.WriteLine($"Unable to find the rootObject for {ebxAsset}:{ebxAssetEntry}");
                FileLogger.WriteLine($"Unable to find the rootObject for {ebxAsset}:{ebxAssetEntry}");
                return;
            }

            if (rootObject != null)
            {
                dynamic val = rootObject.Manifest;
                chunkAssetEntry = AssetManager.Instance.GetChunkEntry(val.ChunkId);
                if (chunkAssetEntry == null)
                {
                    Debug.WriteLine($"Unable to find the ChunkAssetEntry for {ebxAssetEntry.Name}");
                    FileLogger.WriteLine($"Unable to find the ChunkAssetEntry for {ebxAssetEntry.Name}");
                }
            }
        }

        public Stream GetChunkStreamForEbx(EbxAssetEntry ebxAssetEntry)
        {
            GetChunkAssetForEbx(ebxAssetEntry, out ChunkAssetEntry chunkAssetEntry, out EbxAsset asset);
            if (chunkAssetEntry != null)
                return AssetManager.GetChunk(chunkAssetEntry);

            return null;
        }

        public void SetCacheModeEnabled(bool enabled)
        {
            //cacheMode = enabled;
        }

        public void FlushCache()
        {
            //cachedChunks.Clear();
        }

        public virtual IEnumerable<AssetEntry> EnumerateAssets(bool modifiedOnly)
        {
            if (LegacyEntries.Count == 0)
                Initialize(Logger == null ? new NullLogger() : Logger);

            var lE = LegacyEntries
                .Select(x => x.Value)
                .Where(x =>
                !modifiedOnly
                || x.HasModifiedData);
            return lE;
        }

        public virtual AssetEntry GetAssetEntry(string key)
        {
            if (LegacyEntries.Count == 0)
                Initialize(Logger == null ? new NullLogger() : Logger);

            LegacyFileEntry legacyFileEntry = null;
            if (LegacyEntries.ContainsKey(key))
            {
                legacyFileEntry = LegacyEntries[key];
            }
            else if (LegacyEntries.ContainsKey(key.ToLower()))
            {
                legacyFileEntry = LegacyEntries[key];
            }
            else if (Guid.TryParse(key, out Guid keyGuid))
            // extremely slow in performance diagnostic!
            //else if (Guid.TryParse(key, out Guid keyGuid) && LegacyEntries.Any(x => x.Value.ChunkId == Guid.Parse(key)))
            {
                legacyFileEntry = LegacyEntries.FirstOrDefault(x => x.Value.ChunkId == keyGuid).Value;
            }
            return legacyFileEntry;
        }

        public LegacyFileEntry GetLFEntry(string key)
        {
            if (LegacyEntries.Count == 0)
                Initialize(Logger == null ? new NullLogger() : Logger);

            return GetAssetEntry(key) as LegacyFileEntry;
        }

        //long? testDO;
        private bool disposedValue;

        public virtual Stream GetAsset(AssetEntry entry)
        {
            if (LegacyEntries.Count == 0)
                Initialize(Logger == null ? new NullLogger() : Logger);

            LegacyFileEntry legacyFileEntry = (LegacyFileEntry)GetAssetEntry(entry.Name);

            if (legacyFileEntry == null)
                return null;

            return new MemoryStream(GetAssetAsSpan(entry).ToArray());
        }

        public ReadOnlySpan<byte> GetAssetAsSpan(AssetEntry entry)
        {
            if (LegacyEntries.Count == 0)
                Initialize(Logger == null ? new NullLogger() : Logger);

            LegacyFileEntry legacyFileEntry = (LegacyFileEntry)GetAssetEntry(entry.Name);

            if (legacyFileEntry == null)
                return null;

            if (legacyFileEntry.ModifiedEntry != null && legacyFileEntry.ModifiedEntry.Data != null)
            {
                return legacyFileEntry.ModifiedEntry.Data;
            }

            return GetChunkData(legacyFileEntry);
        }

        //public void RebuildOneFile(LegacyFileEntry legacyFileEntry)
        //{
        //	AssetManager.ModifyChunk(legacyFileEntry.ChunkId, legacyFileEntry.ModifiedEntry.Data);
        //}


        public virtual List<LegacyFileEntry> RebuildEntireChunk(
            Guid chunkId
            , List<LegacyFileEntry> replaceFileEntries
            , List<LegacyFileEntry> newFileEntries = null
            )
        {
            if (LegacyEntries.Count == 0)
                Initialize(Logger == null ? new NullLogger() : Logger);

            replaceFileEntries.ForEach(x => { if (x.ModifiedEntry != null && x.ModifiedEntry.ChunkId.HasValue) x.ModifiedEntry.ChunkId = null; });

            CompressionType compressionType = ProfileManager.GetCompressionType(ProfileManager.CompTypeArea.Legacy);

            var editedGroups = replaceFileEntries
                .GroupBy(x => x.ChunkId)
                .ToDictionary(x => x.Key, x => x.ToList());
            List<LegacyFileEntry> editedLegacyFilesByProcess = new List<LegacyFileEntry>();

            // get the chunk batch (the main batch with offsets etc)
            var chunkBatchIndex = ChunkBatches.FindIndex(x => x.ChunkAssetEntry.Id == chunkId);
            ChunkBatch chunkBatch = ChunkBatches.SingleOrDefault(x => x.ChunkAssetEntry.Id == chunkId);
            if (chunkBatch == null)
                return null;

            foreach (var editedGroupItem in editedGroups)
            {
#if DEBUG
                if (editedGroupItem.Value.Count > 1)
                {

                }
#endif
                // Easily handle Singular Chunk
                if (editedGroupItem.Value.Count == 1
                    && !ModifiedChunks.Any(y => y.Id == editedGroupItem.Key)
                    && chunkBatch.ChunkGroupsInBatch[editedGroupItem.Value.First().ChunkId].Count == 1
                    )
                {
                    var originalEntry = AssetManager.Instance.GetChunkEntry(editedGroupItem.Key);
                    var chunkEntryClone = originalEntry.Clone<ChunkAssetEntry>();
                    var legacyItem = editedGroupItem.Value.First();
                    legacyItem.ModifiedEntry.NewOffset = 0;
                    legacyItem.ModifiedEntry.Size = legacyItem.ModifiedEntry.Data.Length;

                    AssetManager.Instance.ModifyChunk(chunkEntryClone, legacyItem.ModifiedEntry.Data, null, compressionType);

                    legacyItem.ModifiedEntry.CompressedOffset = 0;
                    legacyItem.ModifiedEntry.CompressedOffsetEnd = 0;// chunkEntryClone.ModifiedEntry.Size;
                    //if (ProfileManager.IsLoaded(FMT.FileTools.Modding.EGame.FC24))
                    //{
                    //    legacyItem.ModifiedEntry.CompressedOffset = 0;
                    //    legacyItem.ModifiedEntry.CompressedOffsetEnd = chunkEntryClone.ModifiedEntry.Size;
                    //}

                    chunkEntryClone.ModifiedEntry.Sha1 = AssetManager.Instance.GenerateSha1(Encoding.UTF8.GetBytes(legacyItem.Name));
                    ModifiedChunks.Add(chunkEntryClone);

                    editedLegacyFilesByProcess.Add(legacyItem);
                }
                // Otherwise handle Chunk Batch
                else
                {
                    var batchGuid = editedGroupItem.Key;
                    var groupOfLegacyFilesWithOnlyOne = chunkBatch.ChunkGroupsInBatch
                            .Where(x => !ModifiedChunks.Any(y => y.Id == x.Key))
                            .Where(x => x.Value.Count == 1)
                            .First();
                    foreach (var gItem2 in editedGroupItem.Value)
                    {
                        // This is vital in this current implementation. It will batch into separate existing files.
                        groupOfLegacyFilesWithOnlyOne = chunkBatch.ChunkGroupsInBatch
                            .Where(x => !ModifiedChunks.Any(y => y.Id == x.Key))
                            .Where(x => x.Value.Count == 1)
                            .Where(x => AssetManager.Instance.GetChunkEntry(x.Key) != null)
                            .First(x => AssetManager.Instance.GetChunk(AssetManager.Instance.GetChunkEntry(x.Key)) != null);
                        batchGuid = groupOfLegacyFilesWithOnlyOne.Key;
                        gItem2.ModifiedEntry.ChunkId = batchGuid;


                        var groupOfLegacyFiles = chunkBatch.ChunkGroupsInBatch.First(x => x.Key == groupOfLegacyFilesWithOnlyOne.Key).Value;
                        //groupOfLegacyFiles.AddRange(gItem.Value);
                        groupOfLegacyFiles.Add(gItem2);
                        // standard way of doing it
                        //var groupChunkEntryClone = AssetManager.Instance.GetChunkEntry(gItem.Key).Clone<ChunkAssetEntry>();
                        var originalChunkEntry = AssetManager.Instance.GetChunkEntry(batchGuid);
                        var groupChunkEntryClone = originalChunkEntry.Clone<ChunkAssetEntry>();
                        var groupChunk = AssetManager.Instance.GetChunk(originalChunkEntry);

                        if (groupChunk == null)
                        {
                            FileLogger.WriteLine($"ChunkFileManager2022: Unable to find Group Chunk {groupChunkEntryClone.Id} to mod files");
                            Logger.Log($"ChunkFileManager2022: Unable to find Group Chunk {groupChunkEntryClone.Id} to mod files");
                            continue;
                        }

                        var ms_newChunkGroup = new MemoryStream();
                        using (var nw_newChunkGroup = new NativeWriter(ms_newChunkGroup, leaveOpen: true))
                        {
                            using (var nr_GroupChunk = new NativeReader(groupChunk))
                            {
                                long lastOffset = 0;
                                foreach (var itemInChunkGroup in groupOfLegacyFiles)
                                {
                                    byte[] d = null;

                                    if (itemInChunkGroup.HasModifiedData)
                                    {
                                        itemInChunkGroup.ModifiedEntry.Size = itemInChunkGroup.ModifiedEntry.Data.Length;
                                        d = itemInChunkGroup.ModifiedEntry.Data;
                                        itemInChunkGroup.ModifiedEntry.ChunkId = batchGuid;
                                    }
                                    else
                                    {
                                        nr_GroupChunk.Position = itemInChunkGroup.ExtraData.DataOffset;
                                        d = nr_GroupChunk.ReadBytes((int)itemInChunkGroup.Size);
                                        itemInChunkGroup.ModifiedEntry = new FrostySdk.FrostbiteSdk.Managers.ModifiedLegacyAssetEntry();
                                    }
                                    itemInChunkGroup.ModifiedEntry.NewOffset = lastOffset;
                                    itemInChunkGroup.ModifiedEntry.Size = d.Length;
                                    lastOffset += d.Length;

                                    //itemInChunkGroup.ModifiedEntry.CompressedOffset = 0;
                                    nw_newChunkGroup.Write(d);
                                    //itemInChunkGroup.ModifiedEntry.CompressedOffsetEnd = d.Length;
                                    editedLegacyFilesByProcess.Add(itemInChunkGroup);
                                }

                            }
                        }
                        // Modify the Chunk
                        ms_newChunkGroup.Position = 0;
                        byte[] newChunkGroupData = new NativeReader(ms_newChunkGroup).ReadToEnd();

                        var newChunkAlreadyCompressed = CompressChunkGroup(ms_newChunkGroup, groupOfLegacyFiles, compressionType);
                        groupChunkEntryClone.ModifiedEntry = new ModifiedAssetEntry()
                        {
                            Data = newChunkAlreadyCompressed.newChunkData,
                            Size = newChunkAlreadyCompressed.newChunkData.Length,
                            LogicalSize = (uint)newChunkAlreadyCompressed.newChunkData.Length,
                            OriginalSize = ms_newChunkGroup.Length,
                            Sha1 = groupChunkEntryClone.Sha1,
                        };
                        ModifiedChunks.Add(groupChunkEntryClone);
//#if DEBUG
//                        var testStream = new CasReader(new MemoryStream(ModifiedChunks[0].ModifiedEntry.Data)).Read();
//                        if (testStream.Length != newChunkGroupData.Length)
//                            throw new Exception("Decompressed Test Data should be the same length as the Raw Uncompressed data");

//#endif
                    }


                }

            }


            byte[] oldBatchData;
            using (var nrOldBatch = new NativeReader(AssetManager.Instance.GetChunk(chunkBatch.ChunkAssetEntry)))
            {
                oldBatchData = nrOldBatch.ReadToEnd();
            }

            var msNewBatch = new MemoryStream();
            using (var nwNewBatch = new NativeWriter(msNewBatch, leaveOpen: true))
            {
                nwNewBatch.Write(oldBatchData);
                //var changedBatchLegacy = chunkBatch.BatchLegacyFiles.ToList().Union(editedLegacyFilesByProcess);
                var changedBatchLegacy = editedLegacyFilesByProcess;

                foreach (var lfe in changedBatchLegacy)
                {
                    nwNewBatch.Position = lfe.ActualOffsetPosition;
                    if (lfe.ModifiedEntry != null && lfe.ModifiedEntry.NewOffset.HasValue)
                        nwNewBatch.Write(lfe.ModifiedEntry.NewOffset.Value);
                    else
                        nwNewBatch.Write((long)lfe.ExtraData.DataOffset);

                    nwNewBatch.Position = lfe.ActualSizePosition;
                    if (lfe.ModifiedEntry != null && lfe.ModifiedEntry.Data != null)
                        nwNewBatch.Write((long)lfe.ModifiedEntry.Data.Length);
                    else
                        nwNewBatch.Write(lfe.Size);

                    // Compressed Offset
                    nwNewBatch.Position = lfe.CompressedOffsetPosition;
                    if (lfe.ModifiedEntry != null && lfe.ModifiedEntry.CompressedOffset != null)
                        nwNewBatch.Write((long)lfe.ModifiedEntry.CompressedOffset);
                    else
                        nwNewBatch.Write(lfe.CompressedOffset);

                    // Compressed Size
                    nwNewBatch.Position = lfe.CompressedSizePosition;
                    if (lfe.ModifiedEntry != null && lfe.ModifiedEntry.CompressedOffsetEnd != null)
                        nwNewBatch.Write((long)lfe.ModifiedEntry.CompressedOffsetEnd);
                    else
                        nwNewBatch.Write(lfe.CompressedOffsetEnd);

                    nwNewBatch.Position = lfe.ChunkIdPosition;
                    if (lfe.ModifiedEntry != null && lfe.ModifiedEntry.ChunkId.HasValue)
                        nwNewBatch.Write(lfe.ModifiedEntry.ChunkId.Value);
                    else
                        nwNewBatch.Write(lfe.ChunkId);

                }

            }
            msNewBatch.Position = 0;

            var newBatchData = new NativeReader(msNewBatch).ReadToEnd();
            //byte[] newBatchData = msNewBatch.ToArray();
            msNewBatch.Close();
            msNewBatch.Dispose();


            var cE = AssetManager.Instance.GetChunkEntry(chunkBatch.ChunkAssetEntry.Id).Clone<ChunkAssetEntry>();
            AssetManager.Instance.ModifyChunk(cE, newBatchData, compressionOverride: compressionType);
            ModifiedChunks.Add(cE);

#if DEBUG

            var testChunk = AssetManager.Instance.GetChunk(cE);

            if(testChunk != null)
            {

            }
            //var stringTest = Encoding.UTF8.GetString(testStream);
            //if (stringTest != null)
            //{
            //    var cutOfString = stringTest.Substring((int)groupOfLegacyFiles[1].ModifiedEntry.NewOffset.Value, (int)groupOfLegacyFiles[1].ModifiedEntry.Size);
            //}
#endif


            var allFiles = new List<LegacyFileEntry>();
            if (replaceFileEntries != null)
                allFiles.AddRange(replaceFileEntries);
            if (newFileEntries != null)
                allFiles.AddRange(newFileEntries);
            return allFiles;
        }


        //private static void BuildNewChunkForLegacyItem(KeyValuePair<Guid, List<LegacyFileEntry>> gItem)
        //      {
        //          foreach (var i in gItem.Value)
        //          {
        //              var newGuid = GenerateDeterministicGuid(i);
        //              i.ChunkId = newGuid;
        //              i.ModifiedEntry.NewOffset = 0;
        //              i.ModifiedEntry.CompressedOffset = 0;
        //              i.ModifiedEntry.CompressedOffsetEnd = 0;
        //              i.ModifiedEntry.AddToChunkBundle = true;
        //              i.ModifiedEntry.AddToTOCChunks = true;
        //          }
        //      }

        public void ModifyAsset(string key, byte[] data)
        {
            ModifyAsset(key, data, true);
        }

        public void ModifyAsset(string key, byte[] data, bool rebuildChunk = true)
        {
            if (LegacyEntries.ContainsKey(key))
            {
                LegacyFileEntry legacyFileEntry = LegacyEntries[key];

                if (!OriginalLegacyEntries.Contains(legacyFileEntry))
                    OriginalLegacyEntries.Add(legacyFileEntry);

                legacyFileEntry.ModifiedEntry = new FrostySdk.FrostbiteSdk.Managers.ModifiedLegacyAssetEntry()
                {
                    Data = data,
                    //AddToTOCChunks = true,
                    //AddToChunkBundle = true,
                };

                legacyFileEntry.IsDirty = true;

                //if (rebuildChunk)
                //{
                //	ChunkBatches.ForEach(x => x.ChunkGroupsInBatchModified.Clear());
                //	RebuildEntireChunk(legacyFileEntry.ParentGuid, new List<LegacyFileEntry>() { legacyFileEntry }, null);
                //}
            }
        }

        public List<LegacyFileEntry> ModifyAssets(Dictionary<string, byte[]> data, bool rebuildChunk = true)
        {
            if (LegacyEntries.Count == 0)
                Initialize(Logger == null ? new NullLogger() : Logger);

            ModifiedChunks.Clear();

            List<LegacyFileEntry> filesEdited = new List<LegacyFileEntry>();

            foreach (var dpi in data.Where(x => LegacyEntries.ContainsKey(x.Key)))
            {
                LegacyFileEntry legacyFileEntry = LegacyEntries[dpi.Key].Clone<LegacyFileEntry>();
                legacyFileEntry.ModifiedEntry = new FrostySdk.FrostbiteSdk.Managers.ModifiedLegacyAssetEntry()
                {
                    Data = dpi.Value,
                    //AddToTOCChunks = true,
                    //AddToChunkBundle = true,
                };

                legacyFileEntry.IsDirty = true;
                filesEdited.Add(legacyFileEntry);
            }

            if (rebuildChunk)
            {
                ChunkBatches.ForEach(x => x.ChunkGroupsInBatchModified.Clear());

                var groupedFiles = filesEdited.GroupBy(x => x.ParentGuid).ToDictionary(x => x.Key, x => x.ToList());
                foreach (var grpFile in groupedFiles)
                {
                    filesEdited.AddRange(RebuildEntireChunk(grpFile.Key, grpFile.Value));
                }

            }


            return filesEdited;
        }

        /// <summary>
        /// Modifies multiple assets and expects the entries to already has a ModifiedEntry object instance
        /// </summary>
        /// <param name="entries"></param>
        public void LoadEntriesModifiedFromProject(List<LegacyFileEntry> entries)
        {
            if (LegacyEntries.Count == 0)
                Initialize(Logger == null ? new NullLogger() : Logger);

            var groupedAssets = entries.GroupBy(x => x.ParentGuid).ToDictionary(x => x.Key, x => x.ToList());
            foreach (var group in groupedAssets)
            {
                foreach (var lfe in group.Value)
                {
                    if (LegacyEntries.ContainsKey(lfe.Name))
                    {
                        LegacyEntries[lfe.Name].ModifiedEntry = new FrostySdk.FrostbiteSdk.Managers.ModifiedLegacyAssetEntry()
                        {
                            Data = lfe.ModifiedEntry.Data
                        };
                    };
                }
            }

            // Reset Chunks broken by patches
            foreach (var chunk in ChunkFileManager.LegacyChunks)
            {
                AssetManager.Instance.RevertAsset(chunk);
            }
        }

        /// <summary>
        /// Adds multiple assets and expects the entries to already have a ModifiedEntry object instance
        /// </summary>
        /// <param name="entries"></param>
        public void LoadEntriesAddedFromProject(List<LegacyFileEntry> entries)
        {
            var groupedAssets = entries.GroupBy(x => x.ParentGuid).ToDictionary(x => x.Key, x => x.ToList());
            foreach (var group in groupedAssets)
            {
                foreach (var lfe in group.Value)
                {
                    LegacyEntries[lfe.Name].ModifiedEntry = new FrostySdk.FrostbiteSdk.Managers.ModifiedLegacyAssetEntry()
                    {
                        Data = lfe.ModifiedEntry.Data
                    };

                }
            }
            AddedFileEntries = entries;
        }

        private Stream GetChunkStream(LegacyFileEntry lfe)
        {
            var chunkEntry = AssetManager.GetChunkEntry(lfe.ChunkId);
            if (chunkEntry == null)
            {
                return null;
            }

            if (chunkEntry.ModifiedEntry != null && chunkEntry.ModifiedEntry.Data != null)
            {
                return new MemoryStream(chunkEntry.ModifiedEntry.Data);
            }

            var chunkStream = AssetManager.GetChunk(chunkEntry);
            return chunkStream;
        }

        private ReadOnlySpan<byte> GetChunkData(LegacyFileEntry lfe)
        {
            var chunkEntry = AssetManager.GetChunkEntry(lfe.ChunkId);
            if (chunkEntry == null)
            {
                return null;
            }

            return AssetManager.GetChunkData(chunkEntry).Slice((int)lfe.ExtraData.DataOffset, (int)lfe.Size);
        }

        /// <summary>
        /// Cleans up asset and associated chunks (chunks should not be modified but this checks them over)
        /// </summary>
        /// <param name="entry"></param>
        public void RevertAsset(AssetEntry entry)
        {
            LegacyFileEntry legacyFileEntry = entry as LegacyFileEntry;
            if (legacyFileEntry != null && AssetManager.Instance != null)
            {
                legacyFileEntry.ModifiedEntry = null;
                legacyFileEntry.IsDirty = false;
                var chunkEntry = AssetManager.Instance.GetChunkEntry(legacyFileEntry.ParentGuid);
                AssetManager.Instance.RevertAsset(chunkEntry);
                foreach (EbxAssetEntry item in AssetManager.EnumerateEbx("ChunkFileCollector"))
                {
                    GetChunkAssetForEbx(item, out ChunkAssetEntry chunkAssetEntry, out EbxAsset ebxAsset);
                    if (chunkAssetEntry == null || (chunkAssetEntry != null && !chunkAssetEntry.HasModifiedData))
                        continue;
                    AssetManager.Instance.RevertAsset(chunkAssetEntry);
                }
            }

            ChunkAssetEntry chunkAssetEntry1 = entry as ChunkAssetEntry;
            if (chunkAssetEntry1 != null)
            {
                AssetManager.Instance.RevertAsset(chunkAssetEntry1);
            }

        }

        public static Task ResetAsync(bool fullReset = false)
        {
            return Task.Run(() => { CleanUpChunks(fullReset); });
        }

        public static void CleanUpChunks(bool fullReset = false)
        {
            var iLAM = AssetManager.Instance.GetLegacyAssetManager();
            if (iLAM is ChunkFileManager2022 lam)
            {
                var movedEntries = lam.LegacyEntries.Where(x => x.Value.ModifiedEntry != null
                        && x.Value.ModifiedEntry.Data == null
                        && x.Value.ModifiedEntry.NewOffset.HasValue
                        );

                // Revert all the moved files
                foreach (var lfe in movedEntries)
                {
                    lfe.Value.ModifiedEntry = null;
                }

                // Revert all the chunks
                foreach (EbxAssetEntry item in AssetManager.Instance.EnumerateEbx("ChunkFileCollector"))
                {
                    GetChunkAssetForEbx(item, out ChunkAssetEntry chunkAssetEntry, out EbxAsset ebxAsset);
                    if (chunkAssetEntry != null)
                    {
                        AssetManager.Instance.RevertAsset(chunkAssetEntry);
                    }
                    AssetManager.Instance.RevertAsset(item);
                }

                foreach (ChunkAssetEntry assetEntry in lam.ModifiedChunks)
                {
                    AssetManager.Instance.RevertAsset(assetEntry);
                    //assetEntry.ModifiedEntry = null;
                }
                lam.ModifiedChunks.Clear();

                // Revert all legacy files
                if (fullReset)
                {
                    var resetEntryCount = 0;
                    var modifiedEntries = lam.LegacyEntries.Where(x => x.Value.ModifiedEntry != null).Select(x => x.Value);
                    foreach (var lfe in modifiedEntries)
                    {
                        lfe.ModifiedEntry = null;
                        resetEntryCount++;
                    }
                    //AssetManager.Instance.Logger.Log($"Reset {resetEntryCount} legacy files");
                }
            }
        }

        public void AddAsset(string key, LegacyFileEntry lfe)
        {
            if (LegacyEntries.ContainsKey(key))
            {
                throw new ArgumentException("An asset with this name already exists.", "name");
            }
            byte[] data = ((MemoryStream)GetAsset(lfe)).ToArray();
            LegacyFileEntry legacyFileEntry = new LegacyFileEntry()
            {
                Name = key,
                IsAdded = true
            };
            Guid guid = AssetManager.AddChunk(data, GenerateDeterministicGuid(legacyFileEntry));
            ChunkAssetEntry chunkEntry = AssetManager.GetChunkEntry(guid);
            //chunkEntry.ModifiedEntry.AddToChunkBundle = true;
            chunkEntry.ModifiedEntry.UserData = "legacy;" + legacyFileEntry.Name;
            legacyFileEntry.LinkAsset(chunkEntry);
            legacyFileEntry.IsDirty = true;
            LegacyEntries[key] = legacyFileEntry;
        }

        public void DuplicateAsset(string name, LegacyFileEntry originalAsset)
        {
            if (LegacyEntries.ContainsKey(name))
            {
                throw new ArgumentException("An asset with this name already exists.", "name");
            }
            byte[] data = ((MemoryStream)GetAsset(originalAsset)).ToArray();
            LegacyFileEntry legacyFileEntry = new LegacyFileEntry()
            {
                Name = name,
                IsAdded = true
            };
            LegacyEntries.Add(name, legacyFileEntry);
            Guid guid = AssetManager.AddChunk(data, GenerateDeterministicGuid(legacyFileEntry));
            ChunkAssetEntry chunkEntry = AssetManager.GetChunkEntry(guid);
            chunkEntry.ModifiedEntry.UserData = "legacy;" + legacyFileEntry.Name + ";dupof;" + originalAsset.Name;
            legacyFileEntry.LinkAsset(chunkEntry);
            legacyFileEntry.IsDirty = true;
            legacyFileEntry.ParentGuid = originalAsset.ParentGuid;
            LegacyEntries[name] = legacyFileEntry;
        }

        public static (byte[] newChunkData, long uncompressedSize) CompressChunkGroup(
            Stream uncompressedChunkStream
            , List<LegacyFileEntry> filesInChunk
            , CompressionType compressionType)
        {
            MemoryStream compressedStream = new MemoryStream();
            var modDCS = new NativeReader(uncompressedChunkStream);
            modDCS.Position = 0L;
            int progress = 0;

            //filesInChunk = filesInChunk.OrderBy(x => x.ModifiedEntry.NewOffset).ToList();

            long lastOffsetEnd = 0;
            while (modDCS.Position < modDCS.Length)
            {
                bool isFirstSet = modDCS.Position == 0;

                int iP = Convert.ToInt32(Math.Round((modDCS.Position / (double)modDCS.Length) * 100));
                if (iP != progress)
                {
                    progress = iP;
                    Debug.WriteLine("Progress of Recompress: " + progress.ToString());
                }

                long remainingInStream = modDCS.Length - modDCS.Position;
                int amountToRead = (int)Math.Min(Utils.MaxBufferSize, remainingInStream);
                List<LegacyFileEntry> list = (from f in filesInChunk
                                              where f.ModifiedEntry.NewOffset.Value
                                              >= modDCS.Position
                                              && f.ModifiedEntry.NewOffset.Value
                                              <= modDCS.Position + amountToRead
                                              select f).ToList();
                List<LegacyFileEntry> filesEndingInBlock = (from f in filesInChunk
                                                            where
                                                            (
                                                            f.ModifiedEntry.NewOffset.Value + f.ModifiedEntry.Size
                                                            >= modDCS.Position
                                                            && f.ModifiedEntry.NewOffset.Value + f.ModifiedEntry.Size
                                                            <= modDCS.Position + amountToRead)
                                                            select f).ToList();
                foreach (LegacyFileEntry item in list)
                {
                    item.ModifiedEntry.CompressedOffset = compressedStream.Position;
                }
                byte[] compressedBlock = Utils.CompressFile(modDCS.ReadBytes(amountToRead), null, ResourceType.Invalid, compressionType);
                compressedStream.Write(compressedBlock);
                lastOffsetEnd = compressedStream.Position;
                foreach (LegacyFileEntry item in filesEndingInBlock)
                {
                    item.ModifiedEntry.CompressedOffsetEnd = lastOffsetEnd;
                }
            }
            return (compressedStream.ToArray(), modDCS.Length);
        }


        public static Guid GenerateDeterministicGuid(LegacyFileEntry lfe)
        {
            ulong filenameHash = Murmur2.HashString64(lfe.Filename, 18532uL);
            ulong pathHash = Murmur2.HashString64(lfe.Path, 18532uL);
            int counter = 1;
            Guid guid = Guid.Empty;
            Span<byte> guidSpan = stackalloc byte[16];
            do
            {
                BinaryPrimitives.WriteUInt64LittleEndian(guidSpan, pathHash);
                Span<byte> span = guidSpan;
                BinaryPrimitives.WriteUInt64LittleEndian(span[8..], filenameHash ^ (ulong)counter);
                guidSpan[15] = 1;
                guid = new Guid(guidSpan);
                counter++;
            }
            while (AssetManager.Instance.GetChunkEntry(guid) != null);
            return guid;
        }

        public void OnCommand(string command, params object[] value)
        {
            //throw new NotImplementedException();
        }

        public void Reset()
        {
            //if (LegacyEntries != null)
            //{
            //	ResetAsync(true).Wait();
            //}
        }

        public void ResetAndDispose()
        {
            //Reset();

            //if (LegacyEntries != null)
            //{
            //	LegacyEntries.Clear();
            //	LegacyEntries = null;
            //}
        }

        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!disposedValue)
        //    {
        //        if (disposing)
        //        {
        //            // TODO: dispose managed state (managed objects)
        //            LegacyEntries.Clear();
        //            LegacyEntries = null;
        //            ChunkBatches.Clear();
        //            ChunkBatches = null;
        //        }

        //        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        //        // TODO: set large fields to null
        //        ChunkFileManager.Instance = null;
        //        disposedValue = true;
        //    }
        //}

        //// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        //// ~ChunkFileManager2022()
        //// {
        ////     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        ////     Dispose(disposing: false);
        //// }

        //void IDisposable.Dispose()
        //{
        //    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //    Dispose(disposing: true);
        //    GC.SuppressFinalize(this);
        //}
    }


}