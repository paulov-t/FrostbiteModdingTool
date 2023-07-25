using FMT.FileTools;
using FMT.FileTools.Modding;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public class AssetLoaderHelpers
    {
        public static AssetEntry ConvertDbObjectToAssetEntry(DbObject item, AssetEntry assetEntry, bool useByteSha1 = true)
        {
            if (!item.HasValue("sha1"))
            {
                return null;
            }

            if(useByteSha1 || item.GetValueType("sha1") == typeof(byte[]))
                assetEntry.Sha1 = new FMT.FileTools.Sha1(item.GetValue<byte[]>("sha1"));
            else if (item.HasValue("sha1bytes"))
                assetEntry.Sha1 = new FMT.FileTools.Sha1(item.GetValue<byte[]>("sha1bytes"));
            else
                assetEntry.Sha1 = item.GetValue<Sha1>("sha1");


            assetEntry.BaseSha1 = AssetManager.Instance.GetBaseSha1(assetEntry.Sha1);
            assetEntry.Size = item.GetValue("size", 0L);
            assetEntry.OriginalSize = item.GetValue("originalSize", 0L);

            if (item.HasValue("offset"))
            {
                assetEntry.Location = AssetDataLocation.CasNonIndexed;
                assetEntry.ExtraData = new AssetExtraData();
                assetEntry.ExtraData.DataOffset = (uint)item.GetValue("offset", 0L);
            }

            if (item.HasValue("cas"))
            {
                int cas = item.GetValue("cas", 0);
                assetEntry.ExtraData.Cas = (ushort)cas;
            }
            if (item.HasValue("catalog"))
            {
                int catalog = item.GetValue("catalog", 0);
                assetEntry.ExtraData.Catalog = (ushort)catalog;
            }
            if (item.HasValue("patch"))
            {
                bool patch = item.GetValue("patch", false);
                assetEntry.ExtraData.IsPatch = patch;
            }
            //chunkAssetEntry.ExtraData.CasPath = FileSystem.Instance.GetFilePath(catalog, cas, patch);

            assetEntry.Id = item.GetValue<Guid>("id", Guid.Empty);

            if (assetEntry is EbxAssetEntry || (item.HasValue("Type") && item.HasValue("name")))
            {
                ((EbxAssetEntry)assetEntry).Name = item.GetValue("name", string.Empty);
                ((EbxAssetEntry)assetEntry).Type = item.GetValue("Type", string.Empty);
            }
            else if (assetEntry is ResAssetEntry || item.HasValue("resRid"))
            {
                ((ResAssetEntry)assetEntry).Name = item.GetValue("name", string.Empty);
                ((ResAssetEntry)assetEntry).ResRid = item.GetValueType("resRid") == typeof(ulong) ? item.GetValue<ulong>("resRid", 0ul) : (ulong)item.GetValue<long>("resRid", 0L);
                ((ResAssetEntry)assetEntry).ResType = item.GetValueType("resType") == typeof(uint) ? item.GetValue<uint>("resType", 0) : (uint)item.GetValue<int>("resType", 0);
                ((ResAssetEntry)assetEntry).ResMeta = item.GetValue<byte[]>("resMeta", null);
            }
            else if (assetEntry is ChunkAssetEntry || item.HasValue("logicalOffset"))
            {
                ((ChunkAssetEntry)assetEntry).LogicalOffset = item.GetValue<uint>("logicalOffset");
                ((ChunkAssetEntry)assetEntry).LogicalSize = item.GetValue<uint>("logicalSize");
                ((ChunkAssetEntry)assetEntry).IsInline = item.HasValue("idata");
                ((ChunkAssetEntry)assetEntry).RangeStart = item.HasValue("rangeStart") ? item.GetValue<uint>("rangeStart") : 0;
                ((ChunkAssetEntry)assetEntry).RangeEnd = item.HasValue("rangeEnd") ? item.GetValue<uint>("rangeEnd") : 0;
                ((ChunkAssetEntry)assetEntry).BundledSize = item.HasValue("bundledSize") ? item.GetValue<uint>("bundledSize") : 0;
            }
            //assetEntry.CASFileLocation = NativeFileLocation;
            //assetEntry.TOCFileLocation = AssociatedTOCFile.NativeFileLocation;

            assetEntry.SB_OriginalSize_Position = item.GetValue("SB_OriginalSize_Position", 0);
            assetEntry.SB_CAS_Offset_Position = item.GetValue("SB_CAS_Offset_Position", 0);
            assetEntry.SB_CAS_Size_Position = item.GetValue("SB_CAS_Size_Position", 0);
            assetEntry.SB_Sha1_Position = item.GetValue("SB_Sha1_Position", 0);

            if (item.HasValue("SBFileLocation"))
                assetEntry.SBFileLocation = item.GetValue<string>("SBFileLocation");

            if (item.HasValue("TOCFileLocation"))
                assetEntry.TOCFileLocation = item.GetValue<string>("TOCFileLocation");

            if (item.HasValue("Bundles"))
            {
                var bundles = item.GetValue<List<string>>("Bundles");
                foreach (var bundle in bundles)
                {
                    assetEntry.AddToBundle(Fnv1a.HashString(bundle));
                }
            }
            if (item.HasValue("Bundle"))
            {
                assetEntry.Bundle = item.GetValue<string>("Bundle");
                assetEntry.AddToBundle(Fnv1a.HashString(assetEntry.Bundle));
                assetEntry.Bundles.Add(Fnv1a.HashString(assetEntry.Bundle));
            }
            return assetEntry;
        }
    }
}
