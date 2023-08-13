using FMT.FileTools;
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
            //base.ReadChunkData(nativeReader);
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
            if (NativeFileLocation.Contains("/en"))
            {

            }
            if (NativeFileLocation.Contains("chants_preview"))
            {

            }
            if (NativeFileLocation.Contains("win32/fc/fcgame/fcgame"))
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

                    // 8 bytes of something? 
                    var unkBytes = nativeReader.ReadBytes(6);
                    var unkShortCas = nativeReader.ReadUShort(Endian.Big);
                    //catalog = (byte)unkShortCas;
                    cas = (byte)unkShortCas;
                    catalog = 0;

                    var allCatalogs = AssetManager.Instance.FileSystem.CatalogObjects.ToList();
                    for (var indexCatalog = 0; indexCatalog < allCatalogs.Count; indexCatalog++)
                    {
                        var cat = allCatalogs[indexCatalog];
                        if (cat.SuperBundles.ContainsKey(NativeFileLocation.Replace("native_data/", "").Replace(".toc", "")))
                        {
                            catalog = (byte)indexCatalog;
                            break;
                        }
                    }

                    //bundle.BundleSize = nativeReader.ReadUInt(Endian.Big);
                    //bundle.BundleOffset = nativeReader.ReadUInt(Endian.Big);
                    for (int j2 = 0; j2 < bundle.EntriesCount; j2++)
                    {
                        long locationOfOffset = nativeReader.Position;
                        uint bundleOffsetInCas = nativeReader.ReadUInt(Endian.Big);
                        long locationOfSize = nativeReader.Position;
                        uint bundleSizeInCas = nativeReader.ReadUInt(Endian.Big);
                        if(j2 == 0)
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
                    var actualFlagsOffset = startPosition + bundle.FlagsOffset;
                    nativeReader.Position = actualFlagsOffset;
                    //bundle.Flags = nativeReader.ReadBytes(bundle.EntriesCount);
                    //var actualEntriesOffset = startPosition + bundle.EntriesOffset;
                    //nativeReader.Position = actualEntriesOffset;
                    //var sum = 0;
                    for (int j2 = 0; j2 < bundle.EntriesCount; j2++)
                    {
                        var unkFlagByte = nativeReader.ReadByte();
                        if (unkFlagByte == 128)
                            break;

                        throw new NotImplementedException();
                        //    bool hasCasIdentifier = bundle.Flags[j2] == 1;
                        //    if (hasCasIdentifier)
                        //    {
                        //        unk = nativeReader.ReadByte();
                        //        isInPatch = nativeReader.ReadBoolean();
                        //        catalog = nativeReader.ReadByte();
                        //        cas = nativeReader.ReadByte();
                        //        sum += 4;

                        //    }
                        //    long locationOfOffset = nativeReader.Position;
                        //    uint bundleOffsetInCas = nativeReader.ReadUInt(Endian.Big);
                        //    long locationOfSize = nativeReader.Position;
                        //    uint bundleSizeInCas = nativeReader.ReadUInt(Endian.Big);
                        //    sum += 8;
                        //    if (j2 == 0)
                        //    {
                        //        bundle.Unk = unk;
                        //        bundle.BundleOffset = bundleOffsetInCas;
                        //        bundle.BundleSize = bundleSizeInCas;
                        //        bundle.Cas = cas;
                        //        bundle.Catalog = catalog;
                        //        bundle.Patch = isInPatch;
                        //    }
                        //    else
                        //    {
                        //        bundle.TOCOffsets.Add(locationOfOffset);
                        //        bundle.Offsets.Add(bundleOffsetInCas);

                        //        bundle.TOCSizes.Add(locationOfSize);
                        //        bundle.Sizes.Add(bundleSizeInCas);

                        //        bundle.TOCCas.Add(cas);
                        //        bundle.TOCCatalog.Add(catalog);
                        //        bundle.TOCPatch.Add(isInPatch);
                        //    }
                        
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
                        //var path = FileSystem.Instance.GetFilePath(bundle.Cas);
                        //foreach(var cat in AssetManager.Instance.FileSystem.CatalogObjects)
                        if (bundle.Catalog == 0)
                        {
                            //var allCatalogs = AssetManager.Instance.FileSystem.CatalogObjects.ToList();
                            //for (var indexCatalog = 0; indexCatalog < allCatalogs.Count; indexCatalog++)
                            //{
                            //    var cat = allCatalogs[indexCatalog];
                            //    if (cat.SuperBundles.ContainsKey(NativeFileLocation.Replace("native_data/", "").Replace(".toc", "")))
                            //    {
                            //        bundle.Catalog = (byte)indexCatalog;
                            //        break;
                            //    }
                            //}
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
    }
}
