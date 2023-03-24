using FMT.FileTools.Modding;
using FMT.FileTools;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FrostbiteSdk;
using System.Windows.Controls;
using System.Configuration;
using FrostySdk.Resources;
using System.Reflection.Metadata.Ecma335;
using FrostySdk.Frosty.FET;

namespace FrostySdk.ModsAndProjects.Projects
{
    public abstract class BaseProject : IProject
    {
        public virtual bool IsDirty { get; set; }
        public virtual string Filename { get; set; }

        public virtual string DisplayName { get; set; }

        public virtual AssetManager AssetManager => AssetManager.Instance;

        public virtual ModSettings ModSettings { get; protected set; } = new ModSettings();

        public virtual IEnumerable<AssetEntry> ModifiedAssetEntries
        {
            get
            {
                List<AssetEntry> entries = new List<AssetEntry>();
                entries.AddRange(AssetManager.EnumerateEbx(modifiedOnly: true));
                entries.AddRange(AssetManager.EnumerateRes(modifiedOnly: true));
                entries.AddRange(AssetManager.EnumerateChunks(modifiedOnly: true));
                entries.AddRange(AssetManager.EnumerateCustomAssets("legacy", modifiedOnly: true));
                return entries;
            }
        }

        public virtual bool Load(in FIFAModReader reader)
        {
            var resources = reader.ReadResources()
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Name);

            Dictionary<Guid, byte[]> rawChunkDatas = new Dictionary<Guid, byte[]>();

            foreach (BaseModResource r in resources)
            {
                IAssetEntry entry = new AssetEntry();
                var t = r.GetType().Name;
                switch (t)
                {
                    case "EbxResource":
                        entry = new EbxAssetEntry();
                        break;
                    case "ResResource":
                        entry = new ResAssetEntry();
                        break;
                    case "ChunkResource":
                        entry = new ChunkAssetEntry();
                        break;
                    default:
                        entry = null;
                        break;
                }

                if (entry != null)
                {
                    r.FillAssetEntry(entry);
                    var d = reader.GetResourceData(r);
                    using CasReader casReader = new CasReader(new MemoryStream(d));
                    var d2 = casReader.Read();
                    d = null;

                    if (r.IsLegacyFile)
                    {
                        if (r.LegacyFullName.Contains("CFC", StringComparison.OrdinalIgnoreCase)
                                 || r.LegacyFullName.Contains("Collector", StringComparison.OrdinalIgnoreCase)
                                 )
                            continue;

                        AssetManager.Instance.ModifyLegacyAsset(r.LegacyFullName, d2);
                    }
                    else
                    {
                        if (entry is ChunkAssetEntry)
                        {
                            rawChunkDatas.Add(Guid.Parse(entry.Name), d2);
                        }
                        AssetManager.Instance.ModifyEntry(entry, d2);
                    }
                }
            }

            var modifiedEntries = AssetManager.Instance.ModifiedEntries;
            LoadTexturesFromChunks(modifiedEntries, rawChunkDatas);
            return modifiedEntries.Any();
        }

        public virtual bool Load(in FrostbiteMod frostbiteMod)
        {
            var resources = frostbiteMod.Resources
                 .OrderBy(x => x.Name)
                 .ThenBy(x => x.Name);

            Dictionary<Guid, byte[]> rawChunkDatas = new Dictionary<Guid, byte[]>();

            foreach (BaseModResource r in resources)
            {
                IAssetEntry entry = new AssetEntry();
                var t = r.GetType().Name;
                switch (t)
                {
                    case "EbxResource":
                        entry = new EbxAssetEntry();
                        break;
                    case "ResResource":
                        entry = new ResAssetEntry();
                        break;
                    case "ChunkResource":
                        entry = new ChunkAssetEntry();
                        break;
                    case "LegacyResource":
                        entry = new LegacyFileEntry();
                        break;
                    default:
                        entry = null;
                        break;
                }

                if (entry != null)
                {
                    r.FillAssetEntry(entry);
                    var d = frostbiteMod.GetResourceData(r);
                    using (CasReader casReader = new CasReader(new MemoryStream(d)))
                    {
                        var d2 = casReader.Read();
                        if (entry is ChunkAssetEntry)
                        {
                            rawChunkDatas.Add(Guid.Parse(entry.Name), d2);

                        }
                        AssetManager.Instance.ModifyEntry(entry, d2);
                    }
                    d = null;
                }
            }

            var modifiedEntries = AssetManager.Instance.ModifiedEntries;
            LoadTexturesFromChunks(modifiedEntries, rawChunkDatas);

            return modifiedEntries.Any();
        }

        public virtual bool LoadTexturesFromChunks(IEnumerable<IAssetEntry> modifiedEntries, Dictionary<Guid, byte[]> rawChunkDatas)
        {
            // Sort out textures
            foreach (var entry in modifiedEntries)
            {
                if (entry is ResAssetEntry resAsset)
                {
                    if (resAsset.Type == "Texture")
                    {
                        Texture texture = new Texture(resAsset);
                        if (!rawChunkDatas.Any(x => x.Key == texture.ChunkId))
                            continue;

                        var rawChunkData = rawChunkDatas.Single(x => x.Key == texture.ChunkId);

                        var chunkEntry = AssetManager.Instance.GetChunkEntry(texture.chunkId);
                        if (chunkEntry == null)
                            continue;

                        AssetManager.Instance.ModifyChunk(chunkEntry, rawChunkData.Value, texture);
                    }
                }
            }
            return true;
        }

        public virtual bool Load(in string inFilename)
        {
            throw new NotImplementedException();
        }

        public virtual bool Load(in Stream inStream)
        {
            throw new NotImplementedException();
        }

       

        public virtual Task<bool> LoadAsync(string fileName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> SaveAsync(string overrideFilename, bool updateDirtyState)
        {
            throw new NotImplementedException();
        }

        //public virtual void WriteToMod(string filename, ModSettings overrideSettings = null)
        //{
        //    byte[] projectbytes;

        //    if (File.Exists(filename))
        //        File.Delete(filename);

        //    var memoryStream = new MemoryStream();
        //    FrostbiteModWriter frostyModWriter = new FrostbiteModWriter(memoryStream, overrideSettings);
        //    frostyModWriter.WriteProject();

        //    memoryStream.Position = 0;
        //    projectbytes = new NativeReader(memoryStream).ReadToEnd();
        //    using NativeWriter nwFinal = new NativeWriter(new FileStream(filename, FileMode.CreateNew));
        //    nwFinal.Write(projectbytes);

        //}

        //public virtual void WriteToFIFAMod(string filename, ModSettings overrideSettings = null)
        //{
        //    if (File.Exists(filename))
        //        File.Delete(filename);
        //    using (var fs = new FileStream(filename, FileMode.Create))
        //    {
        //        FIFAModWriter frostyModWriter = new FIFAModWriter(ProfileManager.LoadedProfile.Name, AssetManager, FileSystem.Instance
        //            , fs
        //            , overrideSettings);
        //        frostyModWriter.WriteProject();
        //    }
        //}

        public virtual void WriteToMod(string filename, ModSettings overrideSettings = null)
        {
            byte[] projectbytes;

            if (File.Exists(filename))
                File.Delete(filename);

            var memoryStream = new MemoryStream();
            FrostbiteModWriter frostyModWriter = new FrostbiteModWriter(memoryStream, overrideSettings);
            frostyModWriter.WriteProject();

            memoryStream.Position = 0;
            projectbytes = new NativeReader(memoryStream).ReadToEnd();
            using NativeWriter nwFinal = new NativeWriter(new FileStream(filename, FileMode.CreateNew));
            nwFinal.Write(projectbytes);

        }

        public virtual void WriteToFIFAMod(string filename, ModSettings overrideSettings = null)
        {
            if (overrideSettings == null)
                overrideSettings = ModSettings;

            if (File.Exists(filename))
                File.Delete(filename);

            using (var fs = new FileStream(filename, FileMode.Create))
            {
                FIFAModWriter frostyModWriter = new FIFAModWriter(ProfileManager.LoadedProfile.Name, AssetManager, FileSystem.Instance, fs, overrideSettings);
                frostyModWriter.WriteProject(this);
            }
        }
    }
}
