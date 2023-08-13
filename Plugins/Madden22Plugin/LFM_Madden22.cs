using FMT.FileTools;
using FMT.Logging;
using Frostbite.FileManagers;
using FrostbiteSdk.Frostbite.FileManagers;
using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System.Collections.Generic;
using System.IO;

namespace FIFA22Plugin
{
    public class LFM_Madden22 : ChunkFileManager2022, IChunkFileManager, ICustomAssetManager
    {

        public override void Initialize(ILogger logger)
        {
            logger.Log("Loading legacy files");
            AddedFileEntries = new List<LegacyFileEntry>();

            //ChunkBatches = new List<ChunkBatch>();
            //LegacyEntries = new Dictionary<string, LegacyFileEntry>();

            //var chAttempt1 = AssetManager.Instance.GetChunkEntry(Guid.Parse("FA40D5B2-358C-B8BF-DACA-B7EA34EF1F45"));
            //var chAttempt2 = AssetManager.Instance.GetChunkEntry(Guid.Parse("FA40D5B2-358C-B8BF-DACA-B7EA34EF1F45"));

            foreach (EbxAssetEntry item in AssetManager.EnumerateEbx("ChunkFileCollector"))
            {
                GetChunkAssetForEbx(item, out ChunkAssetEntry chunkAssetEntry, out EbxAsset ebxAsset);
                if (chunkAssetEntry == null)
                {
                    //chunkAssetEntry = AssetManager.Instance.GetChunkEntry(Guid.Parse("FA40D5B2-358C-B8BF-DACA-B7EA34EF1F45"));
                    continue;
                }
                chunkAssetEntry.IsLegacy = true;

                MemoryStream chunk = new MemoryStream();
                AssetManager.Instance.GetChunk(chunkAssetEntry).CopyTo(chunk);
                if (chunk != null)
                {
                    using (NativeReader nativeReader = new NativeReader(chunk))
                    {
                        File.WriteAllBytes("_debug_legacy_" + item.Name.Replace(@"/", "_") + ".dat", chunk.ToArray());
                        nativeReader.Position = 0;

                        ChunkBatch chunkBatch = new ChunkBatch()
                        {
                            EbxAssetEntry = item,
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
                            legacyFileEntry.ChunkId = chunkId;

                            nativeReader.Position = legacyFileEntry.FileNameInBatchOffset;
                            string name = nativeReader.ReadNullTerminatedString();
                            if (nativeReader.Position > chunkBatch.EndOfStrings)
                                chunkBatch.EndOfStrings = (int)nativeReader.Position;

                            if (!LegacyEntries.ContainsKey(name))
                            {
                                legacyFileEntry.Name = name;
                                LegacyEntries.Add(name, legacyFileEntry);
                            }
                            else
                            {
                                legacyFileEntry = LegacyEntries[name];
                            }

                            //if (name.Contains("playervalues.ini", StringComparison.OrdinalIgnoreCase))
                            //{
                            //	var chunkStreamTestPV = GetChunkStream(legacyFileEntry);
                            //	var assetStreamTestPV = GetAsset(legacyFileEntry);

                            //}

                            nativeReader.Position = chunkBatch.Offset2_Files + ((8 + 8 + 8 + 8 + 8 + 16) * (index + 1));



                        }

                        if (ProfileManager.IsFIFA21DataVersion())
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
                                chunkBatch.BottomUnknownOffsets.Add(nativeReader.ReadInt());
                            }
                        }

                        ChunkBatches.Add(chunkBatch);


                    }
                }
            }

            logger.Log($"Loaded {LegacyEntries.Count} legacy files");
            ChunkFileManager.Instance = this;
        }
    }
}
