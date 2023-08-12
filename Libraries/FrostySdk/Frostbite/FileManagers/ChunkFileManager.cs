using FMT.FileTools;
using Frostbite.FileManagers;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Frostbite.FileManagers.ChunkFileManager2022;
using System.Reflection;

namespace FrostbiteSdk.Frostbite.FileManagers
{
    public class ChunkFileManager : ICustomAssetManager, IChunkFileManager
    {
        public static IEnumerable<ChunkAssetEntry> LegacyChunks
        {
            get
            {
                return AssetManager.Instance.EnumerateChunks().Where(x => x.IsLegacy);
            }
        }

        public List<LegacyFileEntry> AddedFileEntries { get; set; }

        public static IChunkFileManager Instance = null;

        private Dictionary<string, LegacyFileEntry> legacyEntries = new Dictionary<string, LegacyFileEntry>();

        private Dictionary<Guid, byte[]> cachedChunks = new Dictionary<Guid, byte[]>();

        private bool cacheMode;
        public AssetManager AssetManager;

        public ChunkFileManager()
        {
            this.AssetManager = AssetManager.Instance;
            Instance = this;
        }

        public ChunkFileManager(AssetManager assetManager)
        {
            this.AssetManager = assetManager;
            Instance = this;
        }

        public virtual void Initialize(ILogger logger)
        {
            logger.Log("Loading legacy files");

            foreach (EbxAssetEntry ebxEntry in AssetManager.EnumerateEbx("ChunkFileCollector"))
            {
                EbxAsset ebx = AssetManager.GetEbx(ebxEntry);
                if (ebx != null)
                {
                    dynamic rootObject = ebx.RootObject;
                    dynamic val = rootObject.Manifest;
                    if (!Utilities.PropertyExists(val, "ChunkId"))
                        continue;

                    ChunkAssetEntry chunkAssetEntry = AssetManager.GetChunkEntry(val.ChunkId);
                    if (chunkAssetEntry != null)
                    {
                        Stream chunk = AssetManager.GetChunk(chunkAssetEntry);
                        if (chunk != null)
                        {
                            using (NativeReader nativeReader = new NativeReader(chunk))
                            {
                                uint chunkFileCollectorCount = nativeReader.ReadUInt();
                                long chunkFileCollectorPosition = nativeReader.Position = nativeReader.ReadLong();
                                //for (uint chunkFileIndex = 0u; chunkFileIndex < chunkFileCollectorCount; chunkFileIndex++)
                                //{
                                //    long fileNamePosition = nativeReader.ReadLong();
                                //    long currentPosition = nativeReader.Position;
                                //    nativeReader.Position = fileNamePosition;
                                //    string text = nativeReader.ReadNullTerminatedString();
                                //    nativeReader.Position = currentPosition;
                                //    LegacyFileEntry legacyFileEntry = null;
                                //    if (!legacyEntries.ContainsKey(text))
                                //    {
                                //        legacyFileEntry = new LegacyFileEntry();
                                //        legacyFileEntry.Name = text;
                                //        legacyEntries.Add(text, legacyFileEntry);
                                //    }
                                //    else
                                //    {
                                //        legacyFileEntry = legacyEntries[text];
                                //    }
                                //    LegacyFileEntry.ChunkCollectorInstance chunkCollectorInstance = new LegacyFileEntry.ChunkCollectorInstance();
                                //    chunkCollectorInstance.CompressedOffset = legacyFileEntry.CompressedOffsetStart = nativeReader.ReadLong();
                                //    chunkCollectorInstance.CompressedSize = legacyFileEntry.CompressedSize = nativeReader.ReadLong();
                                //    chunkCollectorInstance.Offset  = nativeReader.ReadLong();
                                //    chunkCollectorInstance.Size = legacyFileEntry.Size = nativeReader.ReadLong();
                                //    chunkCollectorInstance.ChunkId = nativeReader.ReadGuid();
                                //    chunkCollectorInstance.Entry = item;
                                //    legacyFileEntry.CollectorInstances.Add(chunkCollectorInstance);
                                //    legacyFileEntry.ChunkId = chunkCollectorInstance.ChunkId;
                                //    if (legacyFileEntry.ExtraData == null)
                                //        legacyFileEntry.ExtraData = new AssetExtraData();

                                //    legacyFileEntry.ExtraData.DataOffset = (uint)chunkCollectorInstance.Offset;

                                //}
                                for (uint chunkFileIndex = 0u; chunkFileIndex < chunkFileCollectorCount; chunkFileIndex++)
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

                                        if (!legacyEntries.ContainsKey(name))
                                        {
                                            legacyFileEntry.Name = name;
                                            legacyEntries.Add(name, legacyFileEntry);
                                        }
                                        else
                                        {
                                            legacyFileEntry = legacyEntries[name];
                                        }
                                    }
                                    nativeReader.Position = chunkFileCollectorPosition + ((8 + 8 + 8 + 8 + 8 + 16) * (chunkFileIndex + 1));

                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetCacheModeEnabled(bool enabled)
        {
            cacheMode = enabled;
        }

        public void FlushCache()
        {
            cachedChunks.Clear();
        }

        public virtual IEnumerable<AssetEntry> EnumerateAssets(bool modifiedOnly)
        {
            if (legacyEntries.Count == 0)
                Initialize(new NullLogger());

            var lE = legacyEntries
                .Select(x => x.Value)
                .Where(x =>
                !modifiedOnly
                || x.HasModifiedData);
            return lE;
        }

        public AssetEntry GetAssetEntry(string key)
        {
            if (legacyEntries.ContainsKey(key))
            {
                return legacyEntries[key];
            }
            return null;
        }

        public LegacyFileEntry GetLFEntry(string key)
        {
            if (legacyEntries.ContainsKey(key))
            {
                return legacyEntries[key];
            }
            return null;
        }

        public Stream GetAsset(AssetEntry entry)
        {
            LegacyFileEntry legacyFileEntry = entry as LegacyFileEntry;
            Stream chunkStream = GetChunkStream(legacyFileEntry);
            if (chunkStream == null)
            {
                return null;
            }
            using (NativeReader nativeReader = new NativeReader(chunkStream))
            {
                if (legacyFileEntry.CollectorInstances == null || legacyFileEntry.CollectorInstances.Count == 0)
                    return new MemoryStream(GetAssetAsSpan(entry).ToArray());

                LegacyFileEntry.ChunkCollectorInstance chunkCollectorInstance = legacyFileEntry.IsModified ? legacyFileEntry.CollectorInstances[0].ModifiedEntry : legacyFileEntry.CollectorInstances[0];
                nativeReader.Position = chunkCollectorInstance.Offset;
                return new MemoryStream(nativeReader.ReadBytes((int)chunkCollectorInstance.Size));
            }
        }

        public ReadOnlySpan<byte> GetAssetAsSpan(AssetEntry entry)
        {
            LegacyFileEntry legacyFileEntry = (LegacyFileEntry)GetAssetEntry(entry.Name);

            if (legacyFileEntry == null)
                return null;

            if (legacyFileEntry.ModifiedEntry != null && legacyFileEntry.ModifiedEntry.Data != null)
            {
                return legacyFileEntry.ModifiedEntry.Data;
            }

            return GetChunkData(legacyFileEntry);
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

        public void ModifyAsset(string key, byte[] data)
        {
            if (legacyEntries.ContainsKey(key))
            {
                LegacyFileEntry legacyFileEntry = legacyEntries[key];
                MemoryStream memoryStream = new MemoryStream();
                using (NativeWriter nativeWriter = new NativeWriter(memoryStream, leaveOpen: true))
                {
                    nativeWriter.Write(data);
                }
                AssetManager.RevertAsset(legacyFileEntry);
                Guid guid = AssetManager.AddChunk(data, GenerateDeterministicGuid(legacyFileEntry), null);
                foreach (LegacyFileEntry.ChunkCollectorInstance collectorInstance in legacyFileEntry.CollectorInstances)
                {
                    collectorInstance.ModifiedEntry = new LegacyFileEntry.ChunkCollectorInstance();
                    ChunkAssetEntry chunkEntry = AssetManager.GetChunkEntry(guid);
                    collectorInstance.ModifiedEntry.ChunkId = guid;
                    collectorInstance.ModifiedEntry.Offset = 0L;
                    collectorInstance.ModifiedEntry.CompressedOffset = 0L;
                    collectorInstance.ModifiedEntry.Size = data.Length;
                    collectorInstance.ModifiedEntry.CompressedSize = chunkEntry.ModifiedEntry.Data.Length;
                    //chunkEntry.ModifiedEntry.AddToChunkBundle = true;
                    chunkEntry.ModifiedEntry.UserData = "legacy;" + legacyFileEntry.Name;
                    legacyFileEntry.LinkAsset(chunkEntry);
                    collectorInstance.Entry.LinkAsset(legacyFileEntry);
                }
                legacyFileEntry.IsDirty = true;
                memoryStream.Dispose();
            }
        }

        public void ModifyAsset(string key, byte[] data, bool rebuildChunk = false)
        {
            ModifyAsset(key, data);
        }
        private Stream GetChunkStream(LegacyFileEntry lfe)
        {
            if (cacheMode)
            {
                if (!cachedChunks.ContainsKey(lfe.ChunkId))
                {
                    using (Stream stream = AssetManager.GetChunk(AssetManager.GetChunkEntry(lfe.ChunkId)))
                    {
                        if (stream == null)
                        {
                            return null;
                        }
                        cachedChunks.Add(lfe.ChunkId, ((MemoryStream)stream).ToArray());
                    }
                }
                return new MemoryStream(cachedChunks[lfe.ChunkId]);
            }
            return AssetManager.GetChunk(AssetManager.GetChunkEntry(lfe.ChunkId));
        }

        public void OnCommand(string command, params object[] value)
        {
            if (!(command == "SetCacheModeEnabled"))
            {
                if (command == "FlushCache")
                {
                    FlushCache();
                }
            }
            else
            {
                SetCacheModeEnabled((bool)value[0]);
            }
        }

        public Guid GenerateDeterministicGuid(LegacyFileEntry lfe)
        {
            ulong num = Murmur2.HashString64(lfe.Filename, 18532uL);
            ulong value = Murmur2.HashString64(lfe.Path, 18532uL);
            int num2 = 1;
            Guid guid = Guid.Empty;
            do
            {
                using (NativeWriter nativeWriter = new NativeWriter(new MemoryStream()))
                {
                    nativeWriter.Write(value);
                    nativeWriter.Write((ulong)((long)num ^ num2));
                    byte[] array = ((MemoryStream)nativeWriter.BaseStream).ToArray();
                    array[15] = 1;
                    guid = new Guid(array);
                }
                num2++;
            }
            while (AssetManager.GetChunkEntry(guid) != null);
            return guid;
        }

        public void AddAsset(string key, LegacyFileEntry lfe)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
        }

        public void ResetAndDispose()
        {
        }

        public List<LegacyFileEntry> ModifyAssets(Dictionary<string, byte[]> data, bool rebuildChunk)
        {
            throw new NotImplementedException();
        }

        public void DuplicateAsset(string name, LegacyFileEntry originalAsset)
        {
            throw new NotImplementedException();
        }

        public void RevertAsset(AssetEntry assetEntry)
        {
            //throw new NotImplementedException();
        }

        public void LoadEntriesModifiedFromProject(List<LegacyFileEntry> entries)
        {
            //throw new NotImplementedException();
        }
    }
}
