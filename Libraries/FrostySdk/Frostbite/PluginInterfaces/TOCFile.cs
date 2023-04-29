using FMT.FileTools;
using FrostbiteSdk;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FrostySdk.Frostbite.PluginInterfaces
{

    public class TOCFile : IDisposable
    {

        [Flags]
        public enum TocFlags : byte
        {
            HasBaseBundles = 1,
            HasBaseChunks = 2,
            CompressedStrings = 4
        }

        [Flags]
        public enum AssetUsage : byte
        {
            NotUsedInGame,
            UsedInGame
        }

        public string FileLocation { get; set; }
        public string NativeFileLocation { get; set; }

        public bool DoLogging = true;

        public bool ProcessData = true;

        public int SuperBundleIndex { get; }

        //public int[] ArrayOfInitialHeaderData = new int[12];

        public ContainerMetaData MetaData { get; } = new ContainerMetaData();
        public BaseBundleInfo[] Bundles { get; private set; } = new BaseBundleInfo[0];
        public int[] BundleReferences { get; private set; } = new int[0];

        public List<BundleEntry> BundleEntries { get; } = new List<BundleEntry>();

        public Guid[] TocChunkGuids { get; set; }

        public string ChunkDataBundleName { get { return $"{NativeFileLocation}-TOC"; } }
        public int ChunkDataBundleId { get { return Fnv1a.HashString(ChunkDataBundleName); } }


        /// <summary>
        /// ONLY USED FOR TESTING
        /// </summary>
        public TOCFile()
        {

        }

        /// <summary>
        /// Reads the TOC file and process any data within it (Chunks) and its Bundles (In Cas files)
        /// </summary>
        /// <param name="nativeFilePath"></param>
        /// <param name="log"></param>
        /// <param name="process"></param>
        /// <param name="modDataPath"></param>
        /// <param name="sbIndex"></param>
        /// <param name="headerOnly">If true then do not read/process Cas Bundles</param>
        public TOCFile(string nativeFilePath, bool log = true, bool process = true, bool modDataPath = false, int sbIndex = -1, bool headerOnly = false)
        {
            NativeFileLocation = nativeFilePath;
            FileLocation = FileSystem.Instance.ResolvePath(nativeFilePath, modDataPath);

            if (string.IsNullOrEmpty(FileLocation))
            {
                //Debug.WriteLine("Unable to process " + nativeFilePath);
                return;
            }

            DoLogging = log;
            ProcessData = process;
            SuperBundleIndex = sbIndex;

            if (headerOnly)
            {
                ShouldReadCASBundles = false;
            }

            using (NativeReader reader = new NativeReader(new FileStream(FileLocation, FileMode.Open)))
                Read(reader);
        }

        public class ContainerMetaData
        {
            public int Magic { get; set; }
            public int BundleOffset { get; set; }
            public int BundleCount { get; set; }
            public int ChunkFlagOffsetPosition { get; set; }
            public int ChunkGuidOffset { get; set; }
            public int ChunkCount { get; set; }
            public int ChunkEntryOffset { get; set; }
            public int Unk1_Offset { get; set; }
            public int NameOffset { get; set; }
            public int DataOffset { get; set; }
            public int Unk9_Count { get; set; }
            public TocFlags TOCFlags { get; set; }
            public int CompressedStringCount { get; set; }
            public int CompressedStringSize { get; set; }
            public int CompressedStringOffset { get; set; }

            public int SizeOfUnkBlock { get; set; }

            public void Read(NativeReader nativeReader)
            {
                // 0
                Magic = nativeReader.ReadInt(Endian.Big); // 4
                BundleOffset = nativeReader.ReadInt(Endian.Big); // 8
                BundleCount = nativeReader.ReadInt(Endian.Big); // 12
                ChunkFlagOffsetPosition = nativeReader.ReadInt(Endian.Big); // 16
                ChunkGuidOffset = nativeReader.ReadInt(Endian.Big);  // 20
                ChunkCount = nativeReader.ReadInt(Endian.Big);  // 24
                ChunkEntryOffset = nativeReader.ReadInt(Endian.Big); // 28
                Unk1_Offset = nativeReader.ReadInt(Endian.Big); // 32
                NameOffset = nativeReader.ReadInt(Endian.Big); // 36
                DataOffset = nativeReader.ReadInt(Endian.Big); // 40
                Unk9_Count = nativeReader.ReadInt(Endian.Big); // 44
                var flagNumber = nativeReader.ReadInt(Endian.Big); // 48
                TOCFlags = (TocFlags)flagNumber;
                //if (TOCFlags.HasFlag(TocFlags.CompressedStrings))
                {
                    CompressedStringCount = nativeReader.ReadInt(Endian.Big); // 52
                    CompressedStringSize = nativeReader.ReadInt(Endian.Big); // 56
                    CompressedStringOffset = nativeReader.ReadInt(Endian.Big); // 60
                }

                SizeOfUnkBlock = (BundleOffset - Magic) / 4;
            }

            public void Write(NativeWriter nativeWriter)
            {
                nativeWriter.Write(Magic, Endian.Big);
                nativeWriter.Write(BundleOffset, Endian.Big);
                nativeWriter.Write(BundleCount, Endian.Big);
                nativeWriter.Write(ChunkFlagOffsetPosition, Endian.Big);
                nativeWriter.Write(ChunkGuidOffset, Endian.Big);
                nativeWriter.Write(ChunkCount, Endian.Big);
                nativeWriter.Write(ChunkEntryOffset, Endian.Big);
                nativeWriter.Write(Unk1_Offset, Endian.Big);
                nativeWriter.Write(NameOffset, Endian.Big);
                nativeWriter.Write(DataOffset, Endian.Big);
                nativeWriter.Write(Unk9_Count, Endian.Big);
                nativeWriter.Write((int)TOCFlags, Endian.Big);
                nativeWriter.Write(CompressedStringCount, Endian.Big);
                nativeWriter.Write(CompressedStringSize, Endian.Big);
                nativeWriter.Write(CompressedStringOffset, Endian.Big);
            }
        }

        public DbObject TOCObjects { get; private set; } = new DbObject(false);
        public int[] tocMetaData { get; } = new int[15];

        public List<ChunkAssetEntry> TocChunks { get; } = new List<ChunkAssetEntry>();
        public Dictionary<int, Guid> ChunkIndexToChunkId { get; } = new Dictionary<int, Guid>();
        long actualInternalPos { get; } = 556L;

        private byte[] ToCVersion;
        private byte[] ToCSig;
        private byte[] ToCXor;
        //private byte[] InitialHeaderData;

        //public Dictionary<Guid, DbObject> TocChunkInfo { get; } = new Dictionary<Guid, DbObject>();
        public Dictionary<Guid, uint> TocChunkPatchPositions { get; } = new Dictionary<Guid, uint>();

        public bool ShouldReadCASBundles { get; set; } = true;

        public List<DbObject> Read(in NativeReader nativeReader)
        {
            TocChunks.Clear();
            ChunkIndexToChunkId.Clear();
            TocChunkPatchPositions.Clear();
            //         BundleReferences.Clear();
            //Bundles.Clear();

            nativeReader.Position = 0;

            ToCVersion = nativeReader.ReadBytes(8);
            ToCSig = nativeReader.ReadBytes(256);
            ToCXor = nativeReader.ReadBytes(289);

            nativeReader.Position = actualInternalPos;
            var magic = nativeReader.ReadInt(Endian.Big);
            if (magic != 0x3c)
                throw new Exception("Magic is not the expected value of 0x3c");

            nativeReader.Position -= 4;

            MetaData.Read(nativeReader);
            if (MetaData.BundleCount == 0)
                return TOCObjects.List.Select(o => (DbObject)o).ToList();

            ReadBundleData(nativeReader);
            ReadChunkData(nativeReader);
            ReadCompressedStringData(nativeReader);

            CasBundlePosition = nativeReader.Position;
            if (
                ShouldReadCASBundles
                && Bundles.Length > 0
                && nativeReader.Position < nativeReader.Length)
            {
                ReadCasBundles(nativeReader);
            }

            return TOCObjects.List.Select(o => (DbObject)o).ToList();
        }

        void ReadCompressedStringData(NativeReader nativeReader)
        {
            if (!MetaData.TOCFlags.HasFlag(TocFlags.CompressedStrings))
                return;

            if (MetaData.CompressedStringCount == 0)
                return;

            CompressedStringNames = new int[MetaData.CompressedStringCount];
            nativeReader.Position = 556 + MetaData.NameOffset;
            for (int k = 0; k < MetaData.CompressedStringCount; k++)
            {
                CompressedStringNames[k] = (int)nativeReader.ReadUInt(Endian.Big);
            }
            CompressedStringTable = new int[MetaData.CompressedStringSize];
            nativeReader.Position = 556 + MetaData.CompressedStringOffset;
            for (int j = 0; j < MetaData.CompressedStringSize; j++)
            {
                CompressedStringTable[j] = (int)nativeReader.ReadUInt(Endian.Big);
            }
            CompressedStringHandler stringHandler = new CompressedStringHandler(CompressedStringTable, CompressedStringNames);
            foreach (var bundle in Bundles)
            {
                var bundleName = (stringHandler?.ReadCompressedString(bundle.BundleNameOffset) ?? string.Empty);
                if (!string.IsNullOrEmpty(bundleName))
                {
                    bundle.Name = bundleName;
                    var bE = new BundleEntry()
                    {
                        Type = bundleName.Contains("sublevel", StringComparison.OrdinalIgnoreCase) ? BundleType.SubLevel : BundleType.None,
                        PersistedIndex = AssetManager.Instance.Bundles.Count,
                        Name = bundleName,
                        SuperBundleId = Fnv1a.HashString(NativeFileLocation.Replace("native_patch", "").Replace("native_data", "")),
                        BundleReference = bundle.BundleReference
                    };
                    //if(MetaData.TOCFlags.HasFlag(TocFlags.HasBaseBundles))

                    BundleEntries.Add(bE);
                    if (ProcessData)
                        AssetManager.Instance.Bundles.Add(bE);
                }
            }
        }

        private void ReadChunkData(NativeReader nativeReader)
        {
            nativeReader.Position = actualInternalPos + MetaData.ChunkFlagOffsetPosition;
            if (MetaData.ChunkCount > 0)
            {
                nativeReader.Position = actualInternalPos + MetaData.ChunkFlagOffsetPosition;
                for (int chunkIndex = 0; chunkIndex < MetaData.ChunkCount; chunkIndex++)
                {
                    ListTocChunkFlags.Add(nativeReader.ReadInt(Endian.Big));
                }
                nativeReader.Position = actualInternalPos + MetaData.ChunkGuidOffset;

                TocChunkGuids = new Guid[MetaData.ChunkCount];
                for (int chunkIndex = 0; chunkIndex < MetaData.ChunkCount; chunkIndex++)
                {
                    if (NativeFileLocation.Contains("globals"))
                    {

                    }
                    Guid tocChunkGuid = nativeReader.ReadGuidReverse();
                    //               if (tocChunkGuid.ToString() == "76c53a5f-b788-f470-9013-fd2ebc74843f")
                    //               {

                    //               }
                    //nativeReader.Position -= 16;
                    //Guid tocChunkGuidCorrectWay = nativeReader.ReadGuid();
                    //               if (tocChunkGuidCorrectWay.ToString() == "76c53a5f-b788-f470-9013-fd2ebc74843f")
                    //               {

                    //               }
                    //               nativeReader.Position -= 16;
                    //               Guid tocChunkGuidCorrectWayBig = nativeReader.ReadGuid(Endian.Big);
                    //               if (tocChunkGuidCorrectWayBig.ToString() == "76c53a5f-b788-f470-9013-fd2ebc74843f")
                    //               {

                    //               }
                    int origIndex = nativeReader.ReadInt(Endian.Big);
                    ChunkIndexToChunkId.Add(origIndex, tocChunkGuid);

                    var actualIndex = origIndex & 0xFFFFFF;
                    var actualIndexDiv3 = actualIndex / 3;
                    TocChunkGuids[actualIndexDiv3] = tocChunkGuid;
                }

                //nativeReader.Position = actualInternalPos + MetaData.ChunkEntryOffset;
                nativeReader.Position = actualInternalPos + MetaData.DataOffset;
                for (int chunkIndex = 0; chunkIndex < MetaData.ChunkCount; chunkIndex++)
                {
                    //DbObject dboChunk = new DbObject();
                    //uint dIndex = (uint)(nativeReader.Position - 556 - MetaData.DataOffset) / 4u;
                    //var cid = TocChunkGuids[dIndex];
                    ChunkAssetEntry chunkAssetEntry2 = new ChunkAssetEntry();
                    chunkAssetEntry2.TOCFileLocation = this.NativeFileLocation;
                    chunkAssetEntry2.IsTocChunk = true;

                    var unk2 = nativeReader.ReadByte();
                    uint patchPosition = (uint)(int)nativeReader.Position;
                    //dboChunk.SetValue("patchPosition", (int)nativeReader.Position);
                    bool patch = nativeReader.ReadBoolean();
                    //dboChunk.SetValue("catalogPosition", (int)nativeReader.Position);
                    byte catalog2 = nativeReader.ReadByte();
                    //dboChunk.SetValue("casPosition", (int)nativeReader.Position);
                    byte cas2 = nativeReader.ReadByte();

                    chunkAssetEntry2.SB_CAS_Offset_Position = (int)nativeReader.Position;
                    //dboChunk.SetValue("chunkOffsetPosition", (int)nativeReader.Position);
                    uint chunkOffset = nativeReader.ReadUInt(Endian.Big);
                    //dboChunk.SetValue("chunkSizePosition", (int)nativeReader.Position);
                    chunkAssetEntry2.SB_CAS_Size_Position = (int)nativeReader.Position;
                    uint chunkSize = nativeReader.ReadUInt(Endian.Big);

                    chunkAssetEntry2.Id = TocChunkGuids[chunkIndex];

                    TocChunkPatchPositions.Add(chunkAssetEntry2.Id, patchPosition);

                    if (chunkAssetEntry2.Id == Guid.Empty)
                    {
                        throw new ArgumentException("");
                    }

                    // Generate a Sha1 since we dont have one.
                    chunkAssetEntry2.Sha1 = Sha1.Create(Encoding.ASCII.GetBytes(chunkAssetEntry2.Id.ToString()));

                    chunkAssetEntry2.LogicalOffset = chunkOffset;
                    chunkAssetEntry2.OriginalSize = (chunkAssetEntry2.LogicalOffset & 0xFFFF) | chunkSize;
                    chunkAssetEntry2.Size = chunkSize;
                    chunkAssetEntry2.Location = AssetDataLocation.CasNonIndexed;
                    chunkAssetEntry2.ExtraData = new AssetExtraData();
                    chunkAssetEntry2.ExtraData.Unk = unk2;
                    chunkAssetEntry2.ExtraData.Catalog = catalog2;
                    chunkAssetEntry2.ExtraData.Cas = cas2;
                    chunkAssetEntry2.ExtraData.IsPatch = patch;
                    chunkAssetEntry2.ExtraData.CasPath = FileSystem.Instance.GetFilePath(catalog2, cas2, patch);
                    chunkAssetEntry2.ExtraData.DataOffset = chunkOffset;
                    chunkAssetEntry2.Bundles.Add(ChunkDataBundleId);

                    TocChunks.Add(chunkAssetEntry2);

                    //TocChunkInfo.Add(chunkAssetEntry2.Id, dboChunk);
                }

                var numberOfChunksBefore = AssetManager.Instance.Chunks.Count;

                for (int chunkIndex = 0; chunkIndex < MetaData.ChunkCount; chunkIndex++)
                {
                    var chunkAssetEntry = TocChunks[chunkIndex];
                    if (AssetManager.Instance != null && ProcessData)
                    {
                        AssetManager.Instance.AddChunk(chunkAssetEntry);
                    }

                }

                var numberOfChunksAdded = AssetManager.Instance.Chunks.Count - numberOfChunksBefore;

                if (DoLogging && AssetManager.Instance != null)
                    AssetManager.Instance.Logger.Log($"{NativeFileLocation} Added {numberOfChunksAdded} TOC Chunks");

            }
        }

        private void ReadBundleData(NativeReader nativeReader)
        {
            nativeReader.Position = actualInternalPos + MetaData.BundleOffset;
            if (MetaData.BundleCount > 0 && MetaData.BundleCount != MetaData.BundleOffset)
            {
                BundleReferences = new int[MetaData.BundleCount];
                Bundles = new BaseBundleInfo[MetaData.BundleCount];
                for (int index = 0; index < MetaData.BundleCount; index++)
                {
                    BundleReferences[index] = nativeReader.ReadInt(Endian.Big);
                }
                nativeReader.Position = actualInternalPos + MetaData.BundleOffset;
                for (int indexOfBundleCount = 0; indexOfBundleCount < MetaData.BundleCount; indexOfBundleCount++)
                {
                    int bundleNameOffset = nativeReader.ReadInt(Endian.Big);
                    int size = nativeReader.ReadInt(Endian.Big);
                    long dataOffset = nativeReader.ReadLong(Endian.Big);
                    BaseBundleInfo newBundleInfo = new BaseBundleInfo
                    {
                        BundleNameOffset = bundleNameOffset,
                        Offset = dataOffset,
                        Size = size,
                        TocBundleIndex = indexOfBundleCount,
                        BundleReference = BundleReferences[indexOfBundleCount]
                    };
                    //Bundles.Add(newBundleInfo);
                    Bundles[indexOfBundleCount] = newBundleInfo;

                }
            }
        }

        public static int CalculateChunkIndexFromListIndex(int listIndex)
        {
            return ((listIndex * 3) + 16777216);
        }

        private List<CASBundle> casBundles = new List<CASBundle>();

        public List<CASBundle> CasBundles
        {
            get { return casBundles; }
            set { casBundles = value; }
        }

        public Dictionary<string, List<CASBundle>> CASToBundles = new Dictionary<string, List<CASBundle>>();

        public long CasBundlePosition { get; set; }
        public byte[] CasBundleData { get; set; }
        public long TocChunkPosition { get; set; }

        public List<int> ListTocChunkFlags = new List<int>();
        private bool disposedValue;

        public int[] CompressedStringNames { get; set; }
        public int[] CompressedStringTable { get; set; }

        public void ReadCasBundles(NativeReader nativeReader)
        {
            var remainingByteLength = nativeReader.Length - nativeReader.Position;
            if (remainingByteLength > 0)
            {

                if (AssetManager.Instance != null && DoLogging)
                    AssetManager.Instance.Logger.Log("Searching for CAS Data from " + FileLocation);

                for (int i = 0; i < MetaData.BundleCount; i++)
                {
                    nativeReader.Position = (Bundles[i].Offset + 556);

                    CASBundle bundle = new CASBundle();
                    if (BundleEntries.Count == 0)
                        continue;

                    bundle.BaseEntry = BundleEntries[i];

                    long startPosition = nativeReader.Position;
                    bundle.unk1 = nativeReader.ReadInt(Endian.Big);
                    bundle.unk2 = nativeReader.ReadInt(Endian.Big);
                    bundle.FlagsOffset = nativeReader.ReadInt(Endian.Big);
                    bundle.EntriesCount = nativeReader.ReadInt(Endian.Big);
                    bundle.EntriesOffset = nativeReader.ReadInt(Endian.Big);
                    bundle.HeaderSize = nativeReader.ReadInt(Endian.Big);
                    if (bundle.HeaderSize != 32)
                    {
                        throw new Exception("Bundle Header Size should be 32!");
                    }
                    bundle.unk4 = nativeReader.ReadInt(Endian.Big);
                    bundle.unk5 = nativeReader.ReadInt(Endian.Big);
                    byte unk = 0;
                    bool isInPatch = false;
                    byte catalog = 0;
                    byte cas = 0;
                    nativeReader.Position = startPosition + bundle.FlagsOffset;
                    bundle.Flags = nativeReader.ReadBytes(bundle.EntriesCount);
                    nativeReader.Position = startPosition + bundle.EntriesOffset;
                    var sum = 0;
                    for (int j2 = 0; j2 < bundle.EntriesCount; j2++)
                    {
                        bool hasCasIdentifier = bundle.Flags[j2] == 1;
                        if (hasCasIdentifier)
                        {
                            unk = nativeReader.ReadByte();
                            isInPatch = nativeReader.ReadBoolean();
                            catalog = nativeReader.ReadByte();
                            cas = nativeReader.ReadByte();
                            sum += 4;

                        }
                        long locationOfOffset = nativeReader.Position;
                        uint bundleOffsetInCas = nativeReader.ReadUInt(Endian.Big);
                        long locationOfSize = nativeReader.Position;
                        uint bundleSizeInCas = nativeReader.ReadUInt(Endian.Big);
                        sum += 8;
                        if (j2 == 0)
                        {
                            bundle.Unk = unk;
                            bundle.BundleOffset = bundleOffsetInCas;
                            bundle.BundleSize = bundleSizeInCas;
                            bundle.Cas = cas;
                            bundle.Catalog = catalog;
                            bundle.Patch = isInPatch;
                        }
                        else
                        {
                            bundle.TOCOffsets.Add(locationOfOffset);
                            bundle.Offsets.Add(bundleOffsetInCas);

                            bundle.TOCSizes.Add(locationOfSize);
                            bundle.Sizes.Add(bundleSizeInCas);

                            bundle.TOCCas.Add(cas);
                            bundle.TOCCatalog.Add(catalog);
                            bundle.TOCPatch.Add(isInPatch);
                        }
                        bundle.Entries.Add(new CASBundleEntry()
                        {
                            unk = unk,
                            isInPatch = isInPatch,
                            catalog = catalog,
                            cas = cas,
                            bundleSizeInCas = bundleSizeInCas,
                            locationOfSize = locationOfSize,
                            bundleOffsetInCas = bundleOffsetInCas,
                            locationOfOffset = locationOfOffset
                        }
                        );
                    }
                    CasBundles.Add(bundle);

                }


                if (CasBundles.Count > 0)
                {
                    if (AssetManager.Instance != null && DoLogging)
                        AssetManager.Instance.Logger.Log($"Found {CasBundles.Count} bundles for CasFiles");

                    foreach (var bundle in CasBundles)
                    {
                        if (bundle.Cas == 0)
                        {
                            continue;
                        }
                        var path = FileSystem.Instance.GetFilePath(bundle.Catalog, bundle.Cas, bundle.Patch);
                        if (!string.IsNullOrEmpty(path))
                        {
                            var lstBundles = new List<CASBundle>();
                            if (CASToBundles.ContainsKey(path))
                            {
                                lstBundles = CASToBundles[path];
                            }
                            else
                            {
                                CASToBundles.Add(path, lstBundles);
                            }

                            lstBundles.Add(bundle);
                            CASToBundles[path] = lstBundles;
                        }
                        else
                        {
                            Debug.WriteLine("Unable to find path for Bundle");
                        }
                    }

                    foreach (var ctb in CASToBundles)
                    {
                        DbObject dbo = new DbObject(false);
                        CASDataLoader casDataLoader = new CASDataLoader(this);
                        dbo = casDataLoader.Load(ctb.Key, ctb.Value);
                        if (dbo == null)
                            continue;
                        foreach (var d in dbo)
                        {
                            TOCObjects.Add(d);
                        }



                    }
                }
            }
        }

        public static IEnumerable<int> PatternAt(byte[] source, byte[] pattern)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    yield return i;
                }
            }
        }

        public void Write(Stream stream)
        {
            NativeWriter writer = new NativeWriter(stream, leaveOpen: true);
            writer.Write(ToCVersion);
            var sigPosition = writer.Position;
            writer.Write(ToCSig);
            writer.Write(ToCXor);
            while (writer.Position != 556)
                writer.Write((byte)0);

            long metaDataOffset = writer.Position;
            MetaData.Write(writer);
            foreach (int bundleRef in BundleReferences)
            {
                writer.Write(bundleRef, Endian.Big);
            }
            while ((writer.Position - actualInternalPos) % 8 != 0L)
            {
                writer.Write((byte)0);
            }
            MetaData.BundleOffset = (int)writer.Position - (int)actualInternalPos;
            foreach (var bundle in Bundles)
            {
                writer.Write(bundle.BundleNameOffset, Endian.Big);
                writer.Write((int)bundle.Size, Endian.Big);
                writer.Write(bundle.Offset, Endian.Big);
            }
            MetaData.ChunkFlagOffsetPosition = (int)writer.Position - (int)actualInternalPos;
            foreach (int chunkFlag in ListTocChunkFlags)
            {
                writer.Write(chunkFlag, Endian.Big);
            }
            MetaData.ChunkGuidOffset = (int)writer.Position - (int)actualInternalPos;
            foreach (var chunk in ChunkIndexToChunkId)
            {
                foreach (byte guidByte in chunk.Value.ToByteArray().Reverse())
                {
                    writer.Write(guidByte);
                }
                writer.Write(chunk.Key, Endian.Big);
            }
            MetaData.ChunkEntryOffset = (int)writer.Position - (int)actualInternalPos;
            foreach (var chunk in TocChunks)
            {
                writer.Write(chunk.ExtraData.Unk);
                writer.Write((byte)(chunk.ExtraData.IsPatch ? 1 : 0));
                writer.Write((byte)chunk.ExtraData.Catalog.Value);
                writer.Write((byte)chunk.ExtraData.Cas.Value);
                writer.Write(chunk.ExtraData.DataOffset, Endian.Big);
                writer.Write((uint)chunk.Size, Endian.Big);
            }
            MetaData.NameOffset = (int)writer.Position - (int)actualInternalPos;
            foreach (int offset2Value in CompressedStringNames)
            {
                writer.Write(offset2Value, Endian.Big);
            }
            MetaData.CompressedStringOffset = (int)writer.Position - (int)actualInternalPos;
            foreach (int offset12Value in CompressedStringTable)
            {
                writer.Write(offset12Value, Endian.Big);
            }
            if (CasBundles.Count > 0)
            {
                List<long> newCasBundleOffsets = new List<long>(CasBundles.Count);

                foreach (var cBundle in CasBundles)
                {
                    long casBundleOffsetPosition = writer.Position;
                    newCasBundleOffsets.Add(casBundleOffsetPosition);

                    writer.Write(cBundle.unk1, Endian.Big);
                    writer.Write(cBundle.unk2, Endian.Big);
                    long FlagsOffsetLocation = writer.Position;
                    writer.Write(cBundle.FlagsOffset, Endian.Big);
                    writer.Write(cBundle.EntriesCount, Endian.Big);
                    long EntriesOffsetLocation = writer.Position;
                    writer.Write(cBundle.EntriesOffset, Endian.Big);
                    writer.Write(cBundle.HeaderSize, Endian.Big);
                    writer.Write(cBundle.unk4, Endian.Big);
                    writer.Write(cBundle.unk5, Endian.Big);

                    var currentCas = -1;
                    var currentCatalog = -1;
                    bool? currentPatch = null;
                    var newFlags = new List<byte>();
                    for (int j2 = 0; j2 < cBundle.EntriesCount; j2++)
                    {
                        var entry = cBundle.Entries[j2];

                        bool hasCasIdentifier =
                            j2 == 0
                            || currentCas != entry.cas
                            || currentCatalog != entry.catalog
                            || !currentPatch.HasValue
                            || currentPatch.Value != entry.isInPatch;
                        if (hasCasIdentifier)
                        {
                            writer.Write((byte)0);
                            writer.Write(entry.isInPatch);
                            writer.Write((byte)entry.catalog);
                            writer.Write((byte)entry.cas);

                        }
                        newFlags.Add(Convert.ToByte(hasCasIdentifier ? 0x1 : 0x0));
                        writer.Write(entry.bundleOffsetInCas, Endian.Big);
                        writer.Write(entry.bundleSizeInCas, Endian.Big);

                        currentCas = entry.cas;
                        currentCatalog = entry.catalog;
                        currentPatch = entry.isInPatch;
                    }
                    cBundle.Flags = newFlags.ToArray();
                    cBundle.FlagsOffset = (int)writer.Position - (int)casBundleOffsetPosition;
                    writer.WriteBytes(cBundle.Flags);
                    long endOfCasBundleOffsetPosition = writer.Position;
                    writer.Position = FlagsOffsetLocation;
                    writer.Write(cBundle.FlagsOffset, Endian.Big);
                    writer.Position = endOfCasBundleOffsetPosition;
                }
            }

            writer.Position = metaDataOffset;
            MetaData.Write(writer);

            writer.Position = 556;
            byte[] streamBuffer = new byte[writer.Length - 556];
            writer.BaseStream.Read(streamBuffer, 0, (int)writer.Length - 556);
            var newTocSig = streamBuffer.ToTOCSignature();
            writer.Position = 8;
            writer.Write(newTocSig);

            //writer.Position = 0;
            //using (var fs = new FileStream("_TestNewToc.dat", FileMode.Create))
            //             writer.BaseStream.CopyTo(fs);
        }

        public static void RebuildTOCSignatureOnly(Stream stream)
        {
            if (!stream.CanWrite)
                throw new IOException("Unable to Write to this Stream!");

            if (!stream.CanRead)
                throw new IOException("Unable to Read to this Stream!");

            if (!stream.CanSeek)
                throw new IOException("Unable to Seek this Stream!");

            byte[] streamBuffer = new byte[stream.Length - 556];
            stream.Position = 556;
            stream.Read(streamBuffer, 0, (int)stream.Length - 556);
            var newTocSig = streamBuffer.ToTOCSignature();
            stream.Position = 8;
            stream.Write(newTocSig);
        }

        public static void RebuildTOCSignatureOnly(string filePath)
        {
            using (var fsTocSig = new FileStream(filePath, FileMode.Open))
                TOCFile.RebuildTOCSignatureOnly(fsTocSig);
        }

        public static int CreateCasInt(byte unk, bool isPatch, byte catalog, byte cas)
        {
            return (unk << 24) | ((isPatch ? 1 : 0) << 16) | (catalog << 8) | cas;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (CASToBundles != null && CASToBundles.Count > 0)
                        CASToBundles.Clear();

                    CASToBundles = null;

                    //if (Bundles != null && Bundles.Count > 0)
                    //	Bundles.Clear();
                    Bundles = null;
                    BundleReferences = null;

                    if (TOCObjects != null)
                    {
                        if (TOCObjects.Dictionary != null)
                            TOCObjects.Dictionary.Clear();

                        if (TOCObjects.List != null)
                            TOCObjects.List.Clear();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null

                FileLocation = null;
                NativeFileLocation = null;
                TOCObjects = null;

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }


    public struct BundleEntryInfo
    {
        /// <summary>
        /// Is it a EBX, RES or Chunk
        /// </summary>
        public string Type { get; set; }
        public Guid? ChunkGuid { get; set; }
        public Sha1? Sha { get; set; }
        public string Name { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
        public int Flag { get; set; }
        public long StringOffset { get; set; }
        public int Index { get; set; }
        public int CasIndex { get; internal set; }
        public int Offset2 { get; internal set; }
        public int OriginalSize { get; internal set; }

        public override string ToString()
        {
            var builtString = string.Empty;

            if (!string.IsNullOrEmpty(Type))
            {
                builtString += Type;
            }

            if (!string.IsNullOrEmpty(Name))
            {
                builtString += " " + Name;
            }

            //if (Sha.HasValue)
            //{
            //    builtString += " " + Sha.Value.ToString();
            //}


            if (!string.IsNullOrEmpty(builtString))
            {
                builtString = base.ToString();
            }

            return builtString;


        }
    }


    internal class CompressedStringHandler
    {
        private readonly int[] table;

        private readonly int[] data;

        public CompressedStringHandler(int[] table, int[] data)
        {
            this.table = table ?? throw new ArgumentNullException("table");
            this.data = data ?? throw new ArgumentNullException("data");
        }

        public static uint[] ReadCompressedData(NativeReader reader, int count, Endian endian = Endian.Little)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            uint[] data = new uint[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = reader.ReadUInt(endian);
            }
            return data;
        }

        public static int[] ReadHuffmanTable(NativeReader reader, int count, Endian endian = Endian.Little)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            int[] table = new int[count];
            for (int i = 0; i < count; i++)
            {
                table[i] = reader.ReadInt(endian);
            }
            return table;
        }

        public string ReadCompressedString(int bitIndex)
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                int val = table.Length / 2 - 1;
                do
                {
                    uint index = (uint)(data[bitIndex / 32] >> bitIndex % 32) & 1u;
                    val = table[val * 2 + index];
                    bitIndex++;
                }
                while (val >= 0);
                char character = (char)(-1 - val);
                if (character == '\0')
                {
                    break;
                }
                sb.Append(character);
            }
            return sb.ToString();
        }
    }



}
