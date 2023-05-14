using FMT.FileTools;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public class CASDataLoader
    {
        public TOCFile AssociatedTOCFile { get; set; }
        public string NativeFileLocation { get; set; }

        public List<EbxAssetEntry> CASEBXEntries = new List<EbxAssetEntry>();
        public List<ResAssetEntry> CASRESEntries = new List<ResAssetEntry>();
        public List<ChunkAssetEntry> CASCHUNKEntries = new List<ChunkAssetEntry>();
        public DbObject CASBinaryMeta { get; set; }

        public CASDataLoader(TOCFile inTOC)
        {
            AssociatedTOCFile = inTOC;
        }

        public void Load(int catalog, int cas, List<CASBundle> casBundles)
        {
            NativeFileLocation = AssetManager.Instance.FileSystem.GetFilePath(catalog, cas, false);
            var path = FileSystem.Instance.ResolvePath(NativeFileLocation);// @"E:\Origin Games\FIFA 21\Data\Win32\superbundlelayout\fifa_installpackage_03\cas_03.cas";
            Load(path, casBundles);
        }

        /// <summary>
        /// Loads all items of the CasBundle into the DbObject. Will return null if it cannot parse the path!
        /// </summary>
        /// <param name="path"></param>
        /// <param name="casBundles"></param>
        /// <returns></returns>
        public DbObject Load(string path, List<CASBundle> casBundles)
        {
            DbObject dboAll = new DbObject(false);
            NativeFileLocation = path;
            path = FileSystem.Instance.ResolvePath(NativeFileLocation);
            if (string.IsNullOrEmpty(path))
                return null;

            using (NativeReader nr_cas = new NativeReader(path))
            {
                nr_cas.Position = 0;
                int index = 0;
                foreach (CASBundle casBundle in casBundles.Where(x => x.TotalSize > 0))
                {
                    if (AssetManager.Instance != null && AssociatedTOCFile != null && AssociatedTOCFile.DoLogging)
                        AssetManager.Instance.Logger.Log($"{path} [{Math.Round(((double)index / casBundles.Count) * 100).ToString()}%]");

                    //AssetManager.Instance.Bundles.Add(new BundleEntry() { });


                    index++;

                    // go back 4 from the magic
                    var actualPos = casBundle.BundleOffset;
                    nr_cas.Position = actualPos;

                    BaseBundleInfo baseBundleInfo = new BaseBundleInfo();
                    baseBundleInfo.Offset = actualPos;
                    baseBundleInfo.Size = casBundle.TotalSize;

                    //AssetManager.Instance.bundles.Add(new BundleEntry() { Type = BundleType.None, Name = path + "-" + baseBundleInfo.Offset });

                    //using (
                    //    NativeReader inner_reader = new NativeReader(
                    //    nr_cas.CreateViewStream(baseBundleInfo.Offset, casBundle.TotalSize)
                    //    ))
                    {
                        var binaryReader = new BinaryReader22();
                        var binaryObject = new DbObject();
                        //var pos = inner_reader.Position;
                        //binaryReader.BinaryRead_FIFA21((int)baseBundleInfo.Offset, ref binaryObject, inner_reader, false);
                        //inner_reader.Position = pos;
                        nr_cas.Position = baseBundleInfo.Offset;
                        if (binaryReader.BinaryRead(0, ref binaryObject, nr_cas, false) == null)
                        {
                            if (AssetManager.Instance != null && AssociatedTOCFile != null && AssociatedTOCFile.DoLogging)
                                AssetManager.Instance.Logger.LogError("Unable to find data in " + casBundle.ToString());

                            continue;
                        }

                        if (AssetManager.Instance != null && AssociatedTOCFile != null)
                        {
                            var EbxObjectList = binaryObject.GetValue<DbObject>("ebx");
                            var ResObjectList = binaryObject.GetValue<DbObject>("res");
                            var ChunkObjectList = binaryObject.GetValue<DbObject>("chunks");

                            var ebxCount = binaryObject.GetValue<DbObject>("ebx").Count;
                            var resCount = binaryObject.GetValue<DbObject>("res").Count;
                            var chunkCount = binaryObject.GetValue<DbObject>("chunks").Count;

                            var allObjectList = EbxObjectList.List.Union(ResObjectList.List).Union(ChunkObjectList.List).ToArray();
                            var indexInList = 0;
                            foreach(DbObject dbo in allObjectList)
                            {
                                dbo.SetValue("offset", casBundle.Offsets[indexInList]);
                                dbo.SetValue("size", casBundle.Sizes[indexInList]);

                                dbo.SetValue("TOCOffsetPosition", casBundle.TOCOffsets[indexInList]);
                                dbo.SetValue("TOCSizePosition", casBundle.TOCSizes[indexInList]);

                                dbo.SetValue("CASFileLocation", NativeFileLocation);

                                dbo.SetValue("TOCFileLocation", AssociatedTOCFile.NativeFileLocation);
                                dbo.SetValue("SB_CAS_Offset_Position", casBundle.TOCOffsets[indexInList]);
                                dbo.SetValue("SB_CAS_Size_Position", casBundle.TOCSizes[indexInList]);
                                dbo.SetValue("ParentCASBundleLocation", NativeFileLocation);

                                dbo.SetValue("cas", casBundle.TOCCas[indexInList]);
                                dbo.SetValue("catalog", casBundle.TOCCatalog[indexInList]);
                                dbo.SetValue("patch", casBundle.TOCPatch[indexInList]);

                                dbo.SetValue("BundleIndex", BaseBundleInfo.BundleItemIndex);
                                dbo.SetValue("Bundle", casBundle.BaseEntry.Name);

                                indexInList++;
                            }

                            //
//                            for (var i = 0; i < ebxCount; i++)
//                            {
//                                var ebxobjectinlist = EbxObjectList[i] as DbObject;

//                                ebxobjectinlist.SetValue("ebx", true);
//                                ebxobjectinlist.SetValue("offset", casBundle.Offsets[i]);
//                                ebxobjectinlist.SetValue("size", casBundle.Sizes[i]);

//                                ebxobjectinlist.SetValue("TOCOffsetPosition", casBundle.TOCOffsets[i]);
//                                ebxobjectinlist.SetValue("TOCSizePosition", casBundle.TOCSizes[i]);

//                                ebxobjectinlist.SetValue("CASFileLocation", NativeFileLocation);

//                                ebxobjectinlist.SetValue("TOCFileLocation", AssociatedTOCFile.NativeFileLocation);
//                                ebxobjectinlist.SetValue("SB_CAS_Offset_Position", casBundle.TOCOffsets[i]);
//                                ebxobjectinlist.SetValue("SB_CAS_Size_Position", casBundle.TOCSizes[i]);
//                                ebxobjectinlist.SetValue("ParentCASBundleLocation", NativeFileLocation);

//                                ebxobjectinlist.SetValue("cas", casBundle.TOCCas[i]);
//                                ebxobjectinlist.SetValue("catalog", casBundle.TOCCatalog[i]);
//                                ebxobjectinlist.SetValue("patch", casBundle.TOCPatch[i]);

//                                ebxobjectinlist.SetValue("BundleIndex", BaseBundleInfo.BundleItemIndex);
//                                ebxobjectinlist.SetValue("Bundle", casBundle.BaseEntry.Name);

//#if DEBUG
//                                //if (ebxobjectinlist["name"].ToString().Contains("antony"))
//                                //if (ebxobjectinlist["name"].ToString().Contains("head_255475")) // antony
//                                //{
//                                    // patch cas data loader
//                                    // CAS Patch\\win32\superbundlelayout\fifa_installpackage_06\cas_02.cas
//                                    // native_patch/win32/contentsb.toc
//                                    // TOCOffsetPosition = 2566052
//                                    // TOCSizePosition = 2566056
//                                //}
//#endif

//                            }
//                            for (var i = 0; i < resCount; i++)
//                            {
//                                var resobjectinlist = ResObjectList[i] as DbObject;


//                                resobjectinlist.SetValue("res", true);
//                                resobjectinlist.SetValue("offset", casBundle.Offsets[ebxCount + i]);
//                                resobjectinlist.SetValue("size", casBundle.Sizes[ebxCount + i]);
//                                resobjectinlist.SetValue("TOCOffsetPosition", casBundle.TOCOffsets[ebxCount + i]);
//                                resobjectinlist.SetValue("TOCSizePosition", casBundle.TOCSizes[ebxCount + i]);
//                                resobjectinlist.SetValue("CASFileLocation", NativeFileLocation);

//                                resobjectinlist.SetValue("TOCFileLocation", AssociatedTOCFile.NativeFileLocation);
//                                resobjectinlist.SetValue("SB_CAS_Offset_Position", casBundle.TOCOffsets[ebxCount + i]);
//                                resobjectinlist.SetValue("SB_CAS_Size_Position", casBundle.TOCSizes[ebxCount + i]);
//                                resobjectinlist.SetValue("ParentCASBundleLocation", NativeFileLocation);

//                                resobjectinlist.SetValue("cas", casBundle.TOCCas[ebxCount + i]);
//                                resobjectinlist.SetValue("catalog", casBundle.TOCCatalog[ebxCount + i]);
//                                resobjectinlist.SetValue("patch", casBundle.TOCPatch[ebxCount + i]);

//                                resobjectinlist.SetValue("BundleIndex", BaseBundleInfo.BundleItemIndex);
//                                resobjectinlist.SetValue("Bundle", casBundle.BaseEntry.Name);

//                            }

//                            for (var i = 0; i < chunkCount; i++)
//                            {
//                                var chunkObjectInList = ChunkObjectList[i] as DbObject;

//                                chunkObjectInList.SetValue("chunk", true);
//                                chunkObjectInList.SetValue("offset", casBundle.Offsets[ebxCount + resCount + i]);
//                                chunkObjectInList.SetValue("size", casBundle.Sizes[ebxCount + resCount + i]);

//                                chunkObjectInList.SetValue("CASFileLocation", NativeFileLocation);


//                                chunkObjectInList.SetValue("TOCFileLocation", AssociatedTOCFile.NativeFileLocation);
//                                chunkObjectInList.SetValue("SB_CAS_Offset_Position", casBundle.TOCOffsets[ebxCount + resCount + i]);
//                                chunkObjectInList.SetValue("SB_CAS_Size_Position", casBundle.TOCSizes[ebxCount + resCount + i]);
//                                chunkObjectInList.SetValue("ParentCASBundleLocation", NativeFileLocation);


//                                chunkObjectInList.SetValue("cas", casBundle.TOCCas[ebxCount + resCount + i]);
//                                chunkObjectInList.SetValue("catalog", casBundle.TOCCatalog[ebxCount + resCount + i]);
//                                chunkObjectInList.SetValue("patch", casBundle.TOCPatch[ebxCount + resCount + i]);

//                                chunkObjectInList.SetValue("BundleIndex", BaseBundleInfo.BundleItemIndex);
//                                chunkObjectInList.SetValue("Bundle", casBundle.BaseEntry.Name);

//                            }

                            dboAll.Add(binaryObject);

                            if (AssociatedTOCFile.ProcessData)
                            {
                                foreach (DbObject item in
                                    EbxObjectList.List
                                    .Union(ResObjectList.List)
                                    .Union(ChunkObjectList.List)
                                    )
                                {
                                    //EbxAssetEntry ebxAssetEntry = new EbxAssetEntry();
                                    //ebxAssetEntry = (EbxAssetEntry)AssetLoaderHelpers.ConvertDbObjectToAssetEntry(item, ebxAssetEntry);

                                    //ebxAssetEntry.CASFileLocation = NativeFileLocation;
                                    //ebxAssetEntry.TOCFileLocation = AssociatedTOCFile.NativeFileLocation;

                                    //if (AssociatedTOCFile.ProcessData)
                                    //    AssetManager.Instance.AddEbx(ebxAssetEntry);

                                    AssetEntry asset = null;
                                    if (item.HasValue("ebx"))
                                        asset = new EbxAssetEntry();
                                    else if (item.HasValue("res"))
                                        asset = new ResAssetEntry();
                                    else if (item.HasValue("chunk"))
                                        asset = new ChunkAssetEntry();

                                    asset = AssetLoaderHelpers.ConvertDbObjectToAssetEntry(item, asset);
                                    asset.CASFileLocation = NativeFileLocation;
                                    asset.TOCFileLocation = AssociatedTOCFile.NativeFileLocation;
                                    if (AssociatedTOCFile.ProcessData)
                                    {
                                        if (asset is EbxAssetEntry ebxAssetEntry)
                                            AssetManager.Instance.AddEbx(ebxAssetEntry);
                                        else if (asset is ResAssetEntry resAssetEntry)
                                            AssetManager.Instance.AddRes(resAssetEntry);
                                        else if (asset is ChunkAssetEntry chunkAssetEntry)
                                            AssetManager.Instance.AddChunk(chunkAssetEntry);
                                    }
                                }

                                //var iRes = 0;
                                //foreach (DbObject item in ResObjectList)
                                //{
                                //    ResAssetEntry resAssetEntry = new ResAssetEntry();
                                //    resAssetEntry = (ResAssetEntry)AssetLoaderHelpers.ConvertDbObjectToAssetEntry(item, resAssetEntry);

                                //    resAssetEntry.CASFileLocation = NativeFileLocation;
                                //    resAssetEntry.TOCFileLocation = AssociatedTOCFile.NativeFileLocation;

                                //    resAssetEntry.Bundles.Add(BaseBundleInfo.BundleItemIndex);
                                //    if (AssociatedTOCFile.ProcessData)
                                //        AssetManager.Instance.AddRes(resAssetEntry);

                                //    iRes++;
                                //}

                                //var iChunk = 0;
                                //foreach (DbObject item in ChunkObjectList)
                                //{
                                //    ChunkAssetEntry chunkAssetEntry = new ChunkAssetEntry();
                                //    chunkAssetEntry = (ChunkAssetEntry)AssetLoaderHelpers.ConvertDbObjectToAssetEntry(item, chunkAssetEntry);

                                //    chunkAssetEntry.CASFileLocation = NativeFileLocation;
                                //    chunkAssetEntry.TOCFileLocation = AssociatedTOCFile.NativeFileLocation;

                                //    chunkAssetEntry.Bundles.Add(BaseBundleInfo.BundleItemIndex);
                                //    if (AssociatedTOCFile.ProcessData)
                                //        AssetManager.Instance.AddChunk(chunkAssetEntry);

                                //    iChunk++;
                                //}
                            }
                        }

                    }


                    BaseBundleInfo.BundleItemIndex++;

                }
            }

            return dboAll;
        }




    }


}
