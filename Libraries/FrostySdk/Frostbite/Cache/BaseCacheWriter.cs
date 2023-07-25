using FMT.FileTools;
using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;

namespace StarWarsSquadronsPlugin.Cache
{
    public class BaseCacheWriter : ICacheWriter
    {
        public void Write()
        {
            MemoryStream msCache = new MemoryStream();
            FileSystem fs = AssetManager.Instance.FileSystem;
            //using (NativeWriter nativeWriter = new NativeWriter(new FileStream(fs.CacheName + ".cache", FileMode.Create)))
            using (NativeWriter nativeWriter = new NativeWriter(msCache, leaveOpen: true))
            {
                nativeWriter.WriteLengthPrefixedString(ProfileManager.ProfileName);
                nativeWriter.Write(fs.SystemIteration);

                var stuffOffsetPositions = nativeWriter.Position;
                nativeWriter.Write(0uL); // ebx offsets
                nativeWriter.Write(0uL); // res offsets
                nativeWriter.Write(0uL); // chunk offsets
                nativeWriter.Write(0uL); // name to position offets


                //nativeWriter.Write(AssetManager.Instance.superBundles.Count);
                //foreach (SuperBundleEntry superBundle in AssetManager.Instance.superBundles)
                //{
                //    nativeWriter.WriteNullTerminatedString(superBundle.Name);
                //}
                nativeWriter.Write(AssetManager.Instance.Bundles.Count);
                foreach (BundleEntry bundle in AssetManager.Instance.Bundles)
                {
                    nativeWriter.WriteLengthPrefixedString(bundle.Name);
                    nativeWriter.Write(bundle.SuperBundleId);
                }

                Dictionary<string, long> EbxNameToPosition = new Dictionary<string, long>();
                var ebxOffsetPosition = nativeWriter.Position;
                nativeWriter.Write(AssetManager.Instance.EBX.Values.Count);
                foreach (EbxAssetEntry ebxEntry in AssetManager.Instance.EBX.Values)
                {
                    EbxNameToPosition.Add(ebxEntry.Name, nativeWriter.Position);
                    WriteEbxEntry(nativeWriter, ebxEntry);
                }

                Dictionary<string, long> ResNameToPosition = new Dictionary<string, long>();
                var resOffsetPosition = nativeWriter.Position;
                nativeWriter.Write(AssetManager.Instance.RES.Values.Count);
                foreach (ResAssetEntry resEntry in AssetManager.Instance.RES.Values)
                {
                    ResNameToPosition.Add(resEntry.Name, nativeWriter.Position);
                    WriteResEntry(nativeWriter, resEntry);
                }

                Dictionary<Guid, long> ChunkGuidToPosition = new Dictionary<Guid, long>();
                var chunkOffsetPosition = nativeWriter.Position;
                nativeWriter.Write(AssetManager.Instance.Chunks.Count);
                foreach (ChunkAssetEntry chunkEntry in AssetManager.Instance.Chunks.Values)
                {
                    ChunkGuidToPosition.Add(chunkEntry.Id, nativeWriter.Position);
                    WriteChunkEntry(nativeWriter, chunkEntry);
                }

                nativeWriter.Write(AssetManager.Instance.SuperBundleChunks.Count);
                foreach (ChunkAssetEntry chunkEntry in AssetManager.Instance.SuperBundleChunks.Values)
                {
                    WriteChunkEntry(nativeWriter, chunkEntry);
                }

                var nameToPositionOffsets = nativeWriter.Position;
                nativeWriter.Write(EbxNameToPosition.Count);
                foreach (var kvp in EbxNameToPosition)
                {
                    nativeWriter.WriteLengthPrefixedString(kvp.Key);
                    nativeWriter.Write(kvp.Value);
                }
                nativeWriter.Write(ResNameToPosition.Count);
                foreach (var kvp in ResNameToPosition)
                {
                    nativeWriter.WriteLengthPrefixedString(kvp.Key);
                    nativeWriter.Write(kvp.Value);
                }
                nativeWriter.Write(ChunkGuidToPosition.Count);
                foreach (var kvp in ChunkGuidToPosition)
                {
                    nativeWriter.WriteGuid(kvp.Key);
                    nativeWriter.Write(kvp.Value);
                }

                nativeWriter.Position = stuffOffsetPositions;
                nativeWriter.Write(ebxOffsetPosition);
                nativeWriter.Write(resOffsetPosition);
                nativeWriter.Write(chunkOffsetPosition);
                nativeWriter.Write(nameToPositionOffsets);

            }

            AssetManager.CacheCompress(msCache);
        }

        public void WriteEbxEntry(NativeWriter nativeWriter, EbxAssetEntry ebxEntry)
        {
            nativeWriter.WriteLengthPrefixedString(ebxEntry.Name);
            nativeWriter.Write(ebxEntry.Sha1);
            nativeWriter.Write(ebxEntry.Size);
            nativeWriter.Write(ebxEntry.OriginalSize);
            nativeWriter.Write((int)ebxEntry.Location);
            //nativeWriter.Write(ebxEntry.IsInline);
            nativeWriter.WriteLengthPrefixedString((ebxEntry.Type != null) ? ebxEntry.Type : "");
            nativeWriter.Write(!ebxEntry.Guid.HasValue ? Guid.NewGuid() : ebxEntry.Guid.Value);

            var extraDataExists = ebxEntry.ExtraData != null
                && ebxEntry.ExtraData.DataOffset > 0
                && ebxEntry.ExtraData.Catalog.HasValue
                && ebxEntry.ExtraData.Cas.HasValue;
            nativeWriter.Write(extraDataExists);
            if (extraDataExists)
            {
                nativeWriter.Write(ebxEntry.ExtraData.DataOffset);
                nativeWriter.Write(ebxEntry.ExtraData.Catalog.Value);
                nativeWriter.Write(ebxEntry.ExtraData.Cas.Value);
                nativeWriter.Write(ebxEntry.ExtraData.IsPatch);
            }

            nativeWriter.Write(!string.IsNullOrEmpty(ebxEntry.TOCFileLocation));
            if (!string.IsNullOrEmpty(ebxEntry.TOCFileLocation))
                nativeWriter.WriteLengthPrefixedString(ebxEntry.TOCFileLocation);

            nativeWriter.Write(!string.IsNullOrEmpty(ebxEntry.CASFileLocation));
            if (!string.IsNullOrEmpty(ebxEntry.CASFileLocation))
                nativeWriter.WriteLengthPrefixedString(ebxEntry.CASFileLocation);

            nativeWriter.Write(ebxEntry.SB_CAS_Offset_Position);
            nativeWriter.Write(ebxEntry.SB_CAS_Size_Position);
            nativeWriter.Write(ebxEntry.SB_Sha1_Position);
            nativeWriter.Write(ebxEntry.SB_OriginalSize_Position);

            nativeWriter.Write(ebxEntry.Bundles.Count);
            foreach (int bundle2 in ebxEntry.Bundles)
            {
                nativeWriter.Write(bundle2);
            }
        }

        public void WriteResEntry(NativeWriter nativeWriter, ResAssetEntry resEntry)
        {
            nativeWriter.WriteLengthPrefixedString(resEntry.Name);
            nativeWriter.Write(resEntry.Sha1);
            nativeWriter.Write(resEntry.Size);
            nativeWriter.Write(resEntry.OriginalSize);
            nativeWriter.Write((int)resEntry.Location);
            nativeWriter.Write(resEntry.IsInline);
            nativeWriter.Write(resEntry.ResRid);
            nativeWriter.Write(resEntry.ResType);
            nativeWriter.Write(resEntry.ResMeta.Length);
            nativeWriter.Write(resEntry.ResMeta);
            var extraDataExists = resEntry.ExtraData != null
                && resEntry.ExtraData.DataOffset > 0
                && resEntry.ExtraData.Catalog.HasValue
                && resEntry.ExtraData.Cas.HasValue;
            nativeWriter.Write(extraDataExists);
            if (extraDataExists)
            {
                nativeWriter.Write(resEntry.ExtraData.DataOffset);
                nativeWriter.Write(resEntry.ExtraData.Catalog.Value);
                nativeWriter.Write(resEntry.ExtraData.Cas.Value);
                nativeWriter.Write(resEntry.ExtraData.IsPatch);
            }

            nativeWriter.Write(resEntry.TOCFileLocations.Count);
            foreach (var tfl in resEntry.TOCFileLocations)
            {
                nativeWriter.WriteLengthPrefixedString(tfl);
            }

            nativeWriter.Write(!string.IsNullOrEmpty(resEntry.TOCFileLocation));
            if (!string.IsNullOrEmpty(resEntry.TOCFileLocation))
                nativeWriter.WriteLengthPrefixedString(resEntry.TOCFileLocation);

            nativeWriter.Write(!string.IsNullOrEmpty(resEntry.CASFileLocation));
            if (!string.IsNullOrEmpty(resEntry.CASFileLocation))
                nativeWriter.WriteLengthPrefixedString(resEntry.CASFileLocation);

            nativeWriter.Write(resEntry.SB_CAS_Offset_Position);
            nativeWriter.Write(resEntry.SB_CAS_Size_Position);
            nativeWriter.Write(resEntry.SB_Sha1_Position);
            nativeWriter.Write(resEntry.SB_OriginalSize_Position);

            nativeWriter.Write(resEntry.Bundles.Count);
            foreach (int bundle3 in resEntry.Bundles)
            {
                nativeWriter.Write(bundle3);
            }
        }

        public void WriteChunkEntry(NativeWriter nativeWriter, ChunkAssetEntry chunkEntry)
        {
            nativeWriter.Write(chunkEntry.Id);
            nativeWriter.Write(chunkEntry.Sha1);
            nativeWriter.Write(chunkEntry.Size);
            nativeWriter.Write((int)chunkEntry.Location);
            nativeWriter.Write(chunkEntry.IsInline);
            nativeWriter.Write(chunkEntry.BundledSize);
            nativeWriter.Write(chunkEntry.RangeStart);
            nativeWriter.Write(chunkEntry.RangeEnd);
            nativeWriter.Write(chunkEntry.LogicalOffset);
            nativeWriter.Write(chunkEntry.LogicalSize);
            nativeWriter.Write(chunkEntry.H32);
            nativeWriter.Write(chunkEntry.FirstMip);


            var extraDataExists = chunkEntry.ExtraData != null;
            nativeWriter.Write(extraDataExists);
            if (extraDataExists)
            {
                nativeWriter.Write(chunkEntry.ExtraData.DataOffset);
                nativeWriter.WriteLengthPrefixedString(chunkEntry.ExtraData.CasPath);
            }
        }
    }
}
