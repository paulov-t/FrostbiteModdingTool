using FMT.FileTools;
using FrostySdk.Managers;
using FrostySdk.ModsAndProjects.Projects;
using Standart.Hash.xxHash;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FrostySdk.Frosty.FET
{
    public class FIFAModWriter : FrostbiteModWriter
    {
        private readonly AssetManager assetManager;

        private readonly FileSystem fileSystem;

        private readonly string gameName;

        private ModSettings overrideSettings;

        private long positionOfPlaceholders;

        private long positionOfEndOfHeader;

        public FIFAModWriter(string gameName, AssetManager assetManager, FileSystem fileSystem, Stream inStream, ModSettings inOverrideSettings = null)
            : base(inStream)
        {
            this.gameName = gameName;
            this.assetManager = assetManager ?? throw new ArgumentNullException("assetManager");
            this.fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
            overrideSettings = inOverrideSettings;
        }

        public override void WriteProject(IProject project, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }
            WriteUInt64LittleEndian(5498700893333637446uL);
            WriteUInt32LittleEndian(28u);
            Write((byte)0); // do checksum
            var positionOfPlaceholders = base.Position;
            WriteInt64LittleEndian(0L); // End of Header Offset
            WriteUInt64LittleEndian(0uL); // checksum
            var positionOfDataOffset = base.Position;
            WriteUInt64LittleEndian(0uL); // Data Offset
            WriteUInt32LittleEndian(0u); // Data Count
            WriteLengthPrefixedString(gameName);
            WriteUInt32LittleEndian(fileSystem.Head);
            ModSettings modSettings = overrideSettings ?? project.ModSettings;
            WriteLengthPrefixedString(modSettings.Title);
            WriteLengthPrefixedString(modSettings.Author);
            Write(byte.MaxValue);
            Write(Convert.ToByte(1));
            WriteLengthPrefixedString(string.Empty);
            WriteLengthPrefixedString(string.Empty);
            WriteLengthPrefixedString(modSettings.Version);
            WriteLengthPrefixedString(modSettings.Description);
            WriteLengthPrefixedString(string.Empty);
            WriteLengthPrefixedString(string.Empty);
            WriteLengthPrefixedString(string.Empty);
            WriteLengthPrefixedString(string.Empty);
            WriteLengthPrefixedString(string.Empty);
            WriteLengthPrefixedString(string.Empty);
            WriteLengthPrefixedString(string.Empty);
            WriteLengthPrefixedString(string.Empty);
            AddResource(new EmbeddedResource("Icon", modSettings.Icon, ResourceManifest));
            WriteUInt32LittleEndian(0);
            // locale ini
            Write7BitEncodedInt(AssetManager.Instance.LocaleINIMod.HasUserData ? 1 : 0);
            if (AssetManager.Instance.LocaleINIMod.HasUserData)
            {
                WriteLengthPrefixedString($"{modSettings.Title} Locale.ini");
                WriteLengthPrefixedString(Encoding.UTF8.GetString(AssetManager.Instance.LocaleINIMod.UserData));
            }

            // Remove Locale.Ini in InitFsManager to replace with Locale Ini Manager version
            if (AssetManager.Instance.LocaleINIMod.HasUserData)
            {
                if (AssetManager.Instance.InitFSManager.DataModifications.ContainsKey("Scripts/ini/data/locale.ini"))
                    AssetManager.Instance.InitFSManager.DataModifications.Remove("Scripts/ini/data/locale.ini");

                AssetManager.Instance.InitFSManager.DataModifications.Add("Scripts/ini/data/locale.ini", AssetManager.Instance.LocaleINIMod.UserData);
            }

            // Initfs
            //Write7BitEncodedInt(0);

            var initfsmodCount = AssetManager.Instance.InitFSManager.DataModifications.Count;
            Write7BitEncodedInt(initfsmodCount); // init fs
            foreach (var initfsmod in AssetManager.Instance.InitFSManager.DataModifications)
            {
                WriteLengthPrefixedString(initfsmod.Key);
                Write7BitEncodedInt(1);
                WriteLengthPrefixedString(initfsmod.Key);
                Write7BitEncodedInt(initfsmod.Value.Length);
                Write(initfsmod.Value);
            }



            Write7BitEncodedInt(0); // player lua
            Write7BitEncodedInt(0); // player kit lua
            //WritePlayerLuaMods(this, project.PlayerLuaMod);
            //WritePlayerKitLuaMods(this, project.PlayerKitLuaMod);
            positionOfEndOfHeader = base.Position;
            WriteUInt64LittleEndian(0uL);
            //Dictionary<EbxAssetEntry, List<(string, LegacyFileEntry.ChunkCollectorInstance)>> legacyCollectorsToModifiedEntries = new Dictionary<EbxAssetEntry, List<(string, LegacyFileEntry.ChunkCollectorInstance)>>();
            //foreach (LegacyFileEntry item2 in legacyFileManager.EnumerateAssets(modifiedOnly: true))
            //{
            //    foreach (LegacyFileEntry.ChunkCollectorInstance collectorInstance in item2.CollectorInstances)
            //    {
            //        if (!legacyCollectorsToModifiedEntries.TryGetValue(collectorInstance.Entry, out var list))
            //        {
            //            list = new List<(string, LegacyFileEntry.ChunkCollectorInstance)>();
            //            legacyCollectorsToModifiedEntries[collectorInstance.Entry] = list;
            //        }
            //        list.Add((item2.Name, collectorInstance));
            //    }
            //}
            foreach (BundleEntry item in assetManager.EnumerateBundles(BundleType.None, modifiedOnly: true))
            {

            }
            foreach (EbxAssetEntry ebxAssetEntry in assetManager.EnumerateEbx("", modifiedOnly: true))
            {
                if (ebxAssetEntry.HasModifiedData)
                {
                    EbxResource ebxResource = new EbxResource(ebxAssetEntry, ResourceManifest);
                    AddResource(ebxResource);
                }
            }
            //foreach (ResAssetEntry item3 in assetManager.EnumerateRes(0u, modifiedOnly: true))
            //{
            //    if (item3.HasModifiedData)
            //    {
            //        AddResource(new ResResource(item3, ResourceManifest));
            //    }
            //}
            //foreach (ChunkAssetEntry item4 in assetManager.EnumerateChunks(modifiedOnly: true))
            //{
            //    if (item4.HasModifiedData)
            //    {
            //        AddResource(new ChunkResource(item4, ResourceManifest));
            //    }
            //}
            //foreach (LegacyFileEntry lfe in AssetManager.Instance.EnumerateCustomAssets("legacy", true))
            //{
            //    AddResource(new FIFAEditorLegacyResource(lfe.Name, lfe.EbxAssetEntry != null ? lfe.EbxAssetEntry.Name : "", lfe.ModifiedEntry.Data, null, manifest));
            //}
            //foreach (KeyValuePair<EbxAssetEntry, List<(string, LegacyFileEntry.ChunkCollectorInstance)>> kvp in legacyCollectorsToModifiedEntries)
            {
                //dynamic rootObject = assetManager.GetEbx(kvp.Key).RootObject;
                //dynamic manifest = rootObject.Manifest;
                //ChunkAssetEntry chunkAssetEntry = assetManager.GetChunkEntry(manifest.ChunkId);
                //using RecyclableMemoryStream recyclableMemoryStream = new RecyclableMemoryStream(RecyclableMemoryStreamManagerSingleton.Instance);
                //FileWriter writer = new FileWriter(recyclableMemoryStream);
                //foreach (var (itemName, chunkCollector) in kvp.Value)
                //{
                //    writer.WriteInt32LittleEndian(Fnv1Hasher.HashString(itemName));
                //    writer.WriteLengthPrefixedString(itemName);
                //    writer.WriteGuid(chunkCollector.ModifiedEntry.ChunkId);
                //    writer.WriteInt64LittleEndian(chunkCollector.ModifiedEntry.Offset);
                //    writer.WriteInt64LittleEndian(chunkCollector.ModifiedEntry.CompressedStartOffset);
                //    writer.WriteInt64LittleEndian(chunkCollector.ModifiedEntry.CompressedEndOffset);
                //    writer.WriteInt64LittleEndian(chunkCollector.ModifiedEntry.Size);
                //}
                //AddResource(new LegacyResource(assetManager, chunkAssetEntry.Name, kvp.Key.Name, recyclableMemoryStream.ToArray(), chunkAssetEntry.EnumerateBundles(), ResourceManifest));
            }
            WriteInt32LittleEndian(resources.Count);
            foreach (EditorModResource resource in resources)
            {
                resource.Write(this, 28u);
            }
            long dataOffset = base.Position;
            uint dataCount = (uint)ResourceManifest.Count;
            ResourceManifest.Write(this);
            base.Position = positionOfPlaceholders;
            WriteInt64LittleEndian(positionOfEndOfHeader);
            //base.Position += 8L;
            Position = positionOfDataOffset;
            WriteInt64LittleEndian(dataOffset);
            WriteUInt32LittleEndian(dataCount);
            GenerateChecksums();
        }

        private void GenerateChecksums()
        {
            long offset = positionOfPlaceholders + 8 + 8;
            ulong headerChecksum = xxHash64.ComputeHash(new SubStream(base.BaseStream, offset, positionOfEndOfHeader - offset), 32768, 0uL);
            base.Position = offset - 8;
            WriteUInt64LittleEndian(headerChecksum);
            offset = positionOfEndOfHeader + 8;
            ulong dataChecksum = xxHash64.ComputeHash(new SubStream(base.BaseStream, offset, base.Length - offset), 32768, 0uL);
            base.Position = positionOfEndOfHeader;
            WriteUInt64LittleEndian(dataChecksum);
        }

        private class FIFAEditorLegacyResource : EditorModResource
        {
            public static uint Hash => 3181116261u;

            public override ModResourceType Type => ModResourceType.Chunk;

            public FIFAEditorLegacyResource(string inName, string ebxName, byte[] data, IEnumerable<int> bundles, Manifest manifest)
            {
                name = inName;
                sha1 = FMT.FileTools.Sha1.Create(data);
                resourceIndex = manifest.Add(data);
                size = data.Length;
                flags = 2;
                handlerHash = (int)Hash;
                UserData = "legacy;Collector (" + ebxName + ")";
                AddBundle("chunks", modify: true);
                //foreach (int bundle in bundles)
                //{
                //    BundleEntry bundleEntry = AssetManager.Instance.GetBundleEntry(bundle);
                //    AddBundle(bundleEntry.Name, modify: true);
                //}
            }

            public override void Write(NativeWriter writer, uint writerVersion = 4)
            {
                base.Write(writer, writerVersion);
                writer.WriteInt32LittleEndian(0);
                writer.WriteInt32LittleEndian(0);
                writer.WriteInt32LittleEndian(0);
                writer.WriteInt32LittleEndian(0);
                writer.WriteInt32LittleEndian(0);
                writer.WriteInt32LittleEndian(0);
            }
        }

    }


}
