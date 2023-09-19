using FMT.FileTools;
using FMT.Logging;
using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FC24Plugin
{
    public class FC24TOCFile : TOCFile
    {
        /// <summary>
        /// Reads the TOC file and process any data within it (Chunks) and its Bundles (In Cas files)
        /// </summary>
        /// <param name="nativeFilePath"></param>
        /// <param name="log"></param>
        /// <param name="process"></param>
        /// <param name="modDataPath"></param>
        /// <param name="sbIndex"></param>
        /// <param name="headerOnly">If true then do not read/process Cas Bundles</param>
        public FC24TOCFile(string nativeFilePath, bool log = true, bool process = true, bool modDataPath = false, int sbIndex = -1, bool headerOnly = false)
            : base(nativeFilePath, log, process, modDataPath, sbIndex, headerOnly)
        {
#if DEBUG
            if (nativeFilePath.Contains("/en"))
            {

            }
            if (nativeFilePath.Contains("chants_preview"))
            {

            }
            if (nativeFilePath.Contains("win32/fc/fcgame/fcgame"))
            {

            }
#endif
        }

        protected override void ReadChunkData(NativeReader nativeReader)
        {
#if DEBUG
          
            if (NativeFileLocation.Contains("win32/fc/fcgame/fcgame"))
            {

            }

            if (NativeFileLocation.Contains("globals"))
            {

            }

#endif
            //base.ReadChunkData(nativeReader);
            nativeReader.Position = 556 + MetaData.ChunkFlagOffsetPosition;
            if (MetaData.ChunkCount > 0)
            {
                for (int chunkIndex = 0; chunkIndex < MetaData.ChunkCount; chunkIndex++)
                {
                    ListTocChunkFlags.Add(nativeReader.ReadInt(Endian.Big));
                }
                nativeReader.Position = 556 + MetaData.ChunkGuidOffset;
                TocChunkGuids = new Guid[MetaData.ChunkCount];

                var TOCChunkByOffset = new Dictionary<uint, ChunkAssetEntry>();

                for (int chunkIndex = 0; chunkIndex < MetaData.ChunkCount; chunkIndex++)
                {
                    
                    Guid guid = nativeReader.ReadGuid();
                    nativeReader.Position -= 16;
                    Guid guidReverse = nativeReader.ReadGuidReverse();

#if DEBUG
                    if (guidReverse.ToString() == "19104422-214d-40b9-e26c-198089f37621")
                    {

                    }
                    if (guidReverse.ToString() == "bb78c273-1bec-eb61-5e4b-6a1390e564e9"
                        || guid.ToString() == "bb78c273-1bec-eb61-5e4b-6a1390e564e9"
                        )
                    {

                    }
#endif

                    uint decodeAndOffset = nativeReader.ReadUInt(Endian.Big);
                    uint order = decodeAndOffset & 0xFFFFFFu;
                    (Guid, uint) superBundleChunk = new (guidReverse, decodeAndOffset);
                    while (TocChunks.Count <= order / 3u)
                    {
                        TocChunks.Add(null);
                    }
                    TocChunks[(int)(order / 3u)] = new ChunkAssetEntry
                    {
                        Id = guidReverse
                    };
                    TOCChunkByOffset.Add(order, TocChunks[(int)(order / 3u)]);
                }


                // -----------------------------------------------
                var foundCatalog = 0;
                var allCatalogs = AssetManager.Instance.FileSystem.CatalogObjects.ToList();
                var keyToFindSb = NativeFileLocation.Replace("native_data/", "").Replace(".toc", "");
                if (!allCatalogs.Any(x => x.SuperBundles.ContainsKey(keyToFindSb)))
                {
                    FileLogger.WriteLine($"{nameof(ReadChunkData)} No SuperBundle found for {keyToFindSb} in {NativeFileLocation}");
                    return;
                }
                var singleCatalog = allCatalogs.Last(x => x.SuperBundles.ContainsKey(keyToFindSb) && !x.SuperBundles[keyToFindSb]);
                foundCatalog = allCatalogs.IndexOf(singleCatalog);
                ///

                nativeReader.Position = 556 + MetaData.DataOffset;
                for (int chunkIndex = 0; chunkIndex < MetaData.ChunkCount; chunkIndex++)
                {
                    uint chunkIdentificationOffset = (uint)(nativeReader.Position - 556 - MetaData.DataOffset) / 4u;
                    ChunkAssetEntry tocChunk = TOCChunkByOffset[chunkIdentificationOffset];
                    tocChunk.IsTocChunk = true;

                    nativeReader.ReadByte();
                    var patch = nativeReader.ReadBoolean();
                    nativeReader.ReadBytes(4); // magic
                    nativeReader.ReadByte();
                    var cas = nativeReader.ReadByte(); // cas
                    var catalog = foundCatalog;
                    tocChunk.SB_CAS_Offset_Position = (int)nativeReader.Position;
                    var offset = nativeReader.ReadUInt(Endian.Big);
                    tocChunk.SB_CAS_Size_Position = (int)nativeReader.Position;
                    var size = nativeReader.ReadUInt(Endian.Big);
                    tocChunk.Sha1 = Sha1.Create(Encoding.ASCII.GetBytes(tocChunk.Id.ToString()));

                    tocChunk.LogicalOffset = offset;
                    tocChunk.OriginalSize = (tocChunk.LogicalOffset & 0xFFFF) | size;
                    tocChunk.Size = size;
                    tocChunk.Location = AssetDataLocation.CasNonIndexed;
                    tocChunk.ExtraData = new AssetExtraData();
                    tocChunk.ExtraData.Unk = 0;
                    tocChunk.ExtraData.Catalog = (ushort)catalog;
                    tocChunk.ExtraData.Cas = cas;
                    tocChunk.ExtraData.IsPatch = patch;
                    tocChunk.ExtraData.DataOffset = offset;
                    tocChunk.Bundles.Add(ChunkDataBundleId);
                    AssetManager.Instance.AddChunk(tocChunk);
                }
            }
        }

        protected override void ReadBundleData(NativeReader nativeReader)
        {

            nativeReader.Position = 556 + MetaData.BundleOffset;
            if (MetaData.BundleCount > 0 && MetaData.BundleCount != MetaData.BundleOffset)
            {
                BundleReferences = new int[MetaData.BundleCount];
                Bundles = new BaseBundleInfo[MetaData.BundleCount];
                for (int index = 0; index < MetaData.BundleCount; index++)
                {
                    BundleReferences[index] = nativeReader.ReadInt(Endian.Big);
                }
                nativeReader.Position = 556 + MetaData.BundleOffset;
                for (int indexOfBundleCount = 0; indexOfBundleCount < MetaData.BundleCount; indexOfBundleCount++)
                {
                    int bundleNameOffset = nativeReader.ReadInt(Endian.Big);
                    int size = 0;
                    _ = nativeReader.ReadByte();
                    _ = nativeReader.ReadByte();
                   size = nativeReader.ReadShort(Endian.Big);
                    //int size = nativeReader.ReadInt(Endian.Big);
                    long dataOffset = nativeReader.ReadLong(Endian.Big);
                    BaseBundleInfo newBundleInfo = new BaseBundleInfo
                    {
                        BundleNameOffset = bundleNameOffset,
                        Offset = dataOffset,
                        Size = size,
                        TocBundleIndex = indexOfBundleCount,
                        BundleReference = BundleReferences[indexOfBundleCount]
                    };
                    Bundles[indexOfBundleCount] = newBundleInfo;

                }
            }
        }

        protected override void ReadCasBundles(NativeReader nativeReader)
        {

#if DEBUG
            if (NativeFileLocation.Contains("loc"))
            {

            }
            if (NativeFileLocation.Contains("/en"))
            {

            }
            if (NativeFileLocation.Contains("chants_preview"))
            {

            }
            if (NativeFileLocation.Contains("win32/fc/fcgame/fcgame"))
            {

            }
            if (NativeFileLocation.Contains("contentlaunchsb"))
            {

            }

#endif
            var remainingByteLength = nativeReader.Length - nativeReader.Position;
            if (remainingByteLength > 0)
            {

                if (AssetManager.Instance != null && DoLogging)
                    AssetManager.Instance.Logger.Log("Searching for CAS Data from " + FileLocation);

#if DEBUG
                var fs = AssetManager.Instance.FileSystem;
                var cats = fs.Catalogs;
                var catObjs = fs.CatalogObjects;
                var sbs = fs.SuperBundles;
#endif

                var foundCatalog = 0;
                var allCatalogs = AssetManager.Instance.FileSystem.CatalogObjects.ToList();
                var keyToFindSb = NativeFileLocation.Replace("native_data/", "").Replace("native_patch/", "").Replace(".toc", "");

                // --------------------------------------------------------------------------------------------------
                // Find single catalog
                //var singleCatalog = allCatalogs.Single(x => x.SuperBundles.ContainsKey(keyToFindSb) && !x.SuperBundles[keyToFindSb]);
                if (!allCatalogs.Any(x => x.SuperBundles.ContainsKey(keyToFindSb)))
                {
                    Debug.WriteLine($"{nameof(ReadCasBundles)} No SuperBundle found for {keyToFindSb} in {NativeFileLocation}");
                    FileLogger.WriteLine($"{nameof(ReadCasBundles)} No SuperBundle found for {keyToFindSb} in {NativeFileLocation}");
                    return;
                }

                var singleCatalog = allCatalogs.Last(x => x.SuperBundles.ContainsKey(keyToFindSb) && !x.SuperBundles[keyToFindSb]);

                foundCatalog = allCatalogs.IndexOf(singleCatalog);

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

                    var actualFlagsOffset = startPosition + bundle.FlagsOffset;
                    nativeReader.Position = actualFlagsOffset;
                    bundle.Flags = nativeReader.ReadBytes(bundle.EntriesCount);
                    nativeReader.Position = actualFlagsOffset;

                    var actualEntriesOffset = startPosition + bundle.EntriesOffset;
                    nativeReader.Position = actualEntriesOffset;

                    byte unk = 0;
                    bool isInPatch = false;
                    byte catalog = 0;
                    byte cas = 0;

                    for (int j2 = 0; j2 < bundle.EntriesCount; j2++)
                    {
                        bool hasCasIdentifier = bundle.Flags[j2] == 128;
                        if (hasCasIdentifier)
                        {
                            // 8 bytes of something? 
                            var unkByte1 = nativeReader.ReadByte(); // 00 Unknown
                            isInPatch = nativeReader.ReadBoolean(); // Patch?
                            var unkMagic1 = nativeReader.ReadBytes(4); // A3 A0 0D catalog identifier?
                            var unkByte = nativeReader.ReadByte();
                            cas = nativeReader.ReadByte(); // Cas number
                                                           //catalog = (byte)unkShortCas;
                                                           //cas = (byte)unkShortCas;
                                                           //catalog = 0;
                            catalog = (byte)foundCatalog;
                        }

                        long locationOfOffset = nativeReader.Position;
                        uint bundleOffsetInCas = nativeReader.ReadUInt(Endian.Big);
                        long locationOfSize = nativeReader.Position;
                        uint bundleSizeInCas = nativeReader.ReadUInt(Endian.Big);

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
                    }
                    
                    CasBundles.Add(bundle);

                }


                if (CasBundles.Count > 0)
                {
#if DEBUG
                    _ = this;
                    _ = AssetManager.Instance;
                    _ = AssetManager.Instance.FileSystem.MemoryFileSystem;
                    _ = AssetManager.Instance.FileSystem.CatalogObjects.ToList();
                    //string fileSystemIni = Encoding.UTF8.GetString(AssetManager.Instance.FileSystem.MemoryFileSystem["Scripts/ini/FileSystem.ini"]);

#endif
                    if (AssetManager.Instance != null && DoLogging)
                        AssetManager.Instance.Logger.Log($"Found {CasBundles.Count} bundles for CasFiles");

                    foreach (var bundle in CasBundles)
                    {
                        if (bundle.Cas == 0)
                        {
                            continue;
                        }
                        if (bundle.Catalog == 0)
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
                        {
                            continue;
                        }
                        foreach (var d in dbo)
                        {
                            TOCObjects.Add(d);
                        }



                    }
                }
            }
        }
    }
}
