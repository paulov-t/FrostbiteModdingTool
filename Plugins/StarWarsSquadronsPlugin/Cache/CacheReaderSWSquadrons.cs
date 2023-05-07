using FMT.FileTools;

using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using System;

namespace StarWarsSquadronsPlugin.Cache
{
    public class CacheReaderSWSquadrons : ICacheReader
    {
        public ulong EbxDataOffset { get; set; }
        public ulong ResDataOffset { get; set; }
        public ulong ChunkDataOffset { get; set; }
        public ulong NameToPositionOffset { get; set; }

        public bool Read()
        {
            var fs = AssetManager.Instance.FileSystem;
            bool patched = false;
            //using (NativeReader nativeReader = new NativeReader(new FileStream(fs.CacheName + ".cache", FileMode.Open, FileAccess.Read)))
            using (NativeReader nativeReader = new NativeReader(AssetManager.CacheDecompress()))
            {
                if (nativeReader.ReadLengthPrefixedString() != ProfileManager.ProfileName)
                    return false;

                var cacheHead = nativeReader.ReadULong();
                if (cacheHead != fs.SystemIteration)
                {
                    patched = true;
                }

                EbxDataOffset = nativeReader.ReadULong();
                ResDataOffset = nativeReader.ReadULong();
                ChunkDataOffset = nativeReader.ReadULong();
                NameToPositionOffset = nativeReader.ReadULong();
                int count = 0;
                // bundle count
                count = nativeReader.ReadInt();
                for (int k = 0; k < count; k++)
                {
                    BundleEntry bE = new BundleEntry();
                    bE.Name = nativeReader.ReadLengthPrefixedString();
                    bE.SuperBundleId = nativeReader.ReadInt();
                    AssetManager.Instance.Bundles.Add(bE);
                }

                count = nativeReader.ReadInt();
                for (int k = 0; k < count; k++)
                {
                    EbxAssetEntry ebxAssetEntry = new EbxAssetEntry();
                    ebxAssetEntry.Name = nativeReader.ReadLengthPrefixedString();
                    ebxAssetEntry.Sha1 = nativeReader.ReadSha1();
                    ebxAssetEntry.BaseSha1 = AssetManager.Instance.GetBaseSha1(ebxAssetEntry.Sha1);
                    ebxAssetEntry.Size = nativeReader.ReadLong();
                    ebxAssetEntry.OriginalSize = nativeReader.ReadLong();
                    ebxAssetEntry.Location = (AssetDataLocation)nativeReader.ReadInt();
                    //ebxAssetEntry.IsInline = nativeReader.ReadBoolean();
                    ebxAssetEntry.Type = nativeReader.ReadLengthPrefixedString();
                    Guid guid = nativeReader.ReadGuid();
                    if (nativeReader.ReadBoolean())
                    {
                        ebxAssetEntry.ExtraData = new AssetExtraData();
                        ebxAssetEntry.ExtraData.DataOffset = nativeReader.ReadUInt();
                        ebxAssetEntry.ExtraData.Catalog = nativeReader.ReadUShort();
                        ebxAssetEntry.ExtraData.Cas = nativeReader.ReadUShort();
                        ebxAssetEntry.ExtraData.IsPatch = nativeReader.ReadBoolean();
                    }

                    if (nativeReader.ReadBoolean())
                        ebxAssetEntry.TOCFileLocation = nativeReader.ReadLengthPrefixedString();
                    if (nativeReader.ReadBoolean())
                        ebxAssetEntry.CASFileLocation = nativeReader.ReadLengthPrefixedString();

                    ebxAssetEntry.SB_CAS_Offset_Position = nativeReader.ReadInt();
                    ebxAssetEntry.SB_CAS_Size_Position = nativeReader.ReadInt();
                    ebxAssetEntry.SB_Sha1_Position = nativeReader.ReadInt();
                    ebxAssetEntry.SB_OriginalSize_Position = nativeReader.ReadInt();

                    int bundleCount = nativeReader.ReadInt();
                    for (int bundleIndex = 0; bundleIndex < bundleCount; bundleIndex++)
                    {
                        ebxAssetEntry.Bundles.Add(nativeReader.ReadInt());
                    }
                    AssetManager.Instance.AddEbx(ebxAssetEntry);
                }
                count = nativeReader.ReadInt();
                for (int n = 0; n < count; n++)
                {
                    ResAssetEntry resAssetEntry = new ResAssetEntry();
                    resAssetEntry.Name = nativeReader.ReadLengthPrefixedString();
                    resAssetEntry.Sha1 = nativeReader.ReadSha1();
                    resAssetEntry.BaseSha1 = AssetManager.Instance.GetBaseSha1(resAssetEntry.Sha1);
                    resAssetEntry.Size = nativeReader.ReadLong();
                    resAssetEntry.OriginalSize = nativeReader.ReadLong();
                    resAssetEntry.Location = (AssetDataLocation)nativeReader.ReadInt();
                    resAssetEntry.IsInline = nativeReader.ReadBoolean();
                    resAssetEntry.ResRid = nativeReader.ReadULong();
                    resAssetEntry.ResType = nativeReader.ReadUInt();
                    resAssetEntry.ResMeta = nativeReader.ReadBytes(nativeReader.ReadInt());
                    if (nativeReader.ReadBoolean())
                    {
                        resAssetEntry.ExtraData = new AssetExtraData();
                        resAssetEntry.ExtraData.DataOffset = nativeReader.ReadUInt();
                        resAssetEntry.ExtraData.Catalog = nativeReader.ReadUShort();
                        resAssetEntry.ExtraData.Cas = nativeReader.ReadUShort();
                        resAssetEntry.ExtraData.IsPatch = nativeReader.ReadBoolean();
                    }

                    var numTFL = nativeReader.ReadInt();
                    //resAssetEntry.TOCFileLocations = new HashSet<string>();
                    for (int iTFL = 0; iTFL < numTFL; iTFL++)
                    {
                        resAssetEntry.TOCFileLocations.Add(nativeReader.ReadLengthPrefixedString());
                    }

                    if (nativeReader.ReadBoolean())
                        resAssetEntry.TOCFileLocation = nativeReader.ReadLengthPrefixedString();
                    if (nativeReader.ReadBoolean())
                        resAssetEntry.CASFileLocation = nativeReader.ReadLengthPrefixedString();

                    resAssetEntry.SB_CAS_Offset_Position = nativeReader.ReadInt();
                    resAssetEntry.SB_CAS_Size_Position = nativeReader.ReadInt();
                    resAssetEntry.SB_Sha1_Position = nativeReader.ReadInt();
                    resAssetEntry.SB_OriginalSize_Position = nativeReader.ReadInt();

                    int bundleCount = nativeReader.ReadInt();
                    for (int num4 = 0; num4 < bundleCount; num4++)
                    {
                        resAssetEntry.Bundles.Add(nativeReader.ReadInt());
                    }

                    AssetManager.Instance.RES.TryAdd(resAssetEntry.Name, resAssetEntry);
                    if (resAssetEntry.ResRid != 0L)
                    {
                        if (!AssetManager.Instance.resRidList.ContainsKey(resAssetEntry.ResRid))
                            AssetManager.Instance.resRidList.TryAdd(resAssetEntry.ResRid, resAssetEntry);
                    }

                }

                // ------------------------------------------------------------------------
                // Chunks
                var chunkCount = nativeReader.ReadInt();
                for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
                {
                    ChunkAssetEntry chunkAssetEntry = ReadChunkFromCache(nativeReader);
                    AssetManager.Instance.AddChunk(chunkAssetEntry);
                }

                // ------------------------------------------------------------------------
                // Chunks in Bundles
                var sbChunkCount = nativeReader.ReadInt();
                for (int chunkIndex = 0; chunkIndex < sbChunkCount; chunkIndex++)
                {
                    ChunkAssetEntry chunkAssetEntry = ReadChunkFromCache(nativeReader);
                    chunkAssetEntry.IsTocChunk = true;
                    AssetManager.Instance.AddChunk(chunkAssetEntry);
                }
            }
            return !patched;
        }

        public EbxAssetEntry ReadEbxAssetEntry(NativeReader nativeReader)
        {
            throw new NotImplementedException();
        }

        private ChunkAssetEntry ReadChunkFromCache(NativeReader nativeReader)
        {
            ChunkAssetEntry chunkAssetEntry = new ChunkAssetEntry();
            chunkAssetEntry.Id = nativeReader.ReadGuid();
            chunkAssetEntry.Sha1 = nativeReader.ReadSha1();
            chunkAssetEntry.BaseSha1 = AssetManager.Instance.GetBaseSha1(chunkAssetEntry.Sha1);
            chunkAssetEntry.Size = nativeReader.ReadLong();
            chunkAssetEntry.Location = (AssetDataLocation)nativeReader.ReadInt();
            chunkAssetEntry.IsInline = nativeReader.ReadBoolean();
            chunkAssetEntry.BundledSize = nativeReader.ReadUInt();
            chunkAssetEntry.RangeStart = nativeReader.ReadUInt();
            chunkAssetEntry.RangeEnd = nativeReader.ReadUInt();
            chunkAssetEntry.LogicalOffset = nativeReader.ReadUInt();
            chunkAssetEntry.LogicalSize = nativeReader.ReadUInt();
            chunkAssetEntry.H32 = nativeReader.ReadInt();
            chunkAssetEntry.FirstMip = nativeReader.ReadInt();
            //if (nativeReader.ReadBoolean())
            //{
            //    chunkAssetEntry.ExtraData = new AssetExtraData();
            //    chunkAssetEntry.ExtraData.DataOffset = nativeReader.ReadUInt();
            //    chunkAssetEntry.ExtraData.CasPath = nativeReader.ReadLengthPrefixedString();
            //}
            //else
            //{
            //    throw new Exception("No Extra Data!");
            //}

            //var numTFL = nativeReader.ReadInt();
            ////chunkAssetEntry.TOCFileLocations = new HashSet<string>();
            //for (int iTFL = 0; iTFL < numTFL; iTFL++)
            //{
            //    chunkAssetEntry.TOCFileLocations.Add(nativeReader.ReadLengthPrefixedString());
            //}

            //if (nativeReader.ReadBoolean())
            //    chunkAssetEntry.SBFileLocation = nativeReader.ReadLengthPrefixedString();
            //if (nativeReader.ReadBoolean())
            //    chunkAssetEntry.TOCFileLocation = nativeReader.ReadLengthPrefixedString();
            //if (nativeReader.ReadBoolean())
            //    chunkAssetEntry.CASFileLocation = nativeReader.ReadLengthPrefixedString();


            //chunkAssetEntry.SB_CAS_Offset_Position = nativeReader.ReadInt();
            //chunkAssetEntry.SB_CAS_Size_Position = nativeReader.ReadInt();
            //chunkAssetEntry.SB_Sha1_Position = nativeReader.ReadInt();
            //chunkAssetEntry.SB_OriginalSize_Position = nativeReader.ReadInt();

            //chunkAssetEntry.SB_LogicalOffset_Position = nativeReader.ReadUInt();
            //chunkAssetEntry.SB_LogicalSize_Position = nativeReader.ReadUInt();


            //if (nativeReader.ReadBoolean())
            //    chunkAssetEntry.Bundle = nativeReader.ReadLengthPrefixedString();

            //int num6 = nativeReader.ReadInt();
            //for (int num7 = 0; num7 < num6; num7++)
            //{
            //    chunkAssetEntry.Bundles.Add(nativeReader.ReadInt());
            //}

            return chunkAssetEntry;
        }

    }
}
