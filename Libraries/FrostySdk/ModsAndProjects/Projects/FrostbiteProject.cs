using FMT.FileTools;
using FMT.FileTools.Modding;
using Frostbite.FileManagers;
using FrostbiteSdk;
using FrostbiteSdk.FrostbiteSdk.Managers;
using FrostySdk.Frosty.FET;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.ModsAndProjects.Projects;
using FrostySdk.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using v2k4FIFAModding.Frosty;

namespace FrostySdk
{

    public class FrostbiteProject : BaseProject, IProject
    {
        //private const uint FormatVersion = 12u;
        private uint CurrentFormatVersion { get; } = 23u;
        protected uint ProjectFormatVersion { get; set; } = 23u;

        private const ulong Magic = 98218709832262uL;

        private string filename;

        private DateTime creationDate;

        private DateTime modifiedDate;

        private uint gameVersion;

        private ModSettings modSettings;

        public override string DisplayName
        {
            get
            {
                if (filename == "")
                {
                    return "New Project.fbproject";
                }
                return new FileInfo(filename).Name;
            }
        }

        public override string Filename
        {
            get
            {
                return filename;
            }
            set
            {
                filename = value;
            }
        }

        public override bool IsDirty
        {
            get
            {
                if (AssetManager.GetDirtyCount() == 0)
                {
                    return modSettings.IsDirty;
                }
                return true;
            }
        }

        public FileSystem FileSystem { get { return AssetManager.Instance.FileSystem; } }

        public FrostbiteProject()
        {
            filename = "";
            creationDate = DateTime.Now;
            modifiedDate = DateTime.Now;
            gameVersion = 0u;
            modSettings = new ModSettings();
            modSettings.Author = "";
            modSettings.ClearDirtyFlag();
        }

        public FrostbiteProject(AssetManager assetManager, FileSystem fileSystem)
        {
            filename = "";
            creationDate = DateTime.Now;
            modifiedDate = DateTime.Now;
            gameVersion = 0u;
            modSettings = new ModSettings();
            modSettings.Author = "";
            modSettings.ClearDirtyFlag();
        }

        public override bool Load(in string inFilename)
        {
            filename = inFilename;
            return Load(new FileStream(inFilename, FileMode.Open, FileAccess.Read));
        }

        public override bool Load(in Stream inStream)
        {
            using (NativeReader nativeReader = new NativeReader(inStream))
            {
                return InternalLoad(nativeReader).Result;
            }
        }

        public override async Task<bool> LoadAsync(string inFilename, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!File.Exists(inFilename))
                return false;

            filename = inFilename;
            using (NativeReader nativeReader = new NativeReader(new FileStream(inFilename, FileMode.Open, FileAccess.Read)))
            {
                return await InternalLoad(nativeReader, cancellationToken);
            }

        }

        public static string LastFilePath;

        public override IEnumerable<AssetEntry> ModifiedAssetEntries
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

        public override string FileExtension => ".fbproject";

        public override async Task<bool> SaveAsync(string overrideFilename = "", bool updateDirtyState = true)
        {
            return await Task.Run(() => { return Save(overrideFilename, updateDirtyState); });
        }

        public bool Save(string overrideFilename = "", bool updateDirtyState = true)
        {
            string fileName = filename;
            if (!string.IsNullOrEmpty(overrideFilename))
            {
                fileName = overrideFilename;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }
            LastFilePath = fileName;

            modifiedDate = DateTime.Now;
            gameVersion = FileSystem.Head;
            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }
            string text = fileInfo.FullName + ".tmp";
            using (NativeWriter nativeWriter = new NativeWriter(new FileStream(text, FileMode.Create)))
            {
                nativeWriter.Write(98218709832262uL);
                nativeWriter.Write(CurrentFormatVersion);
                nativeWriter.WriteNullTerminatedString(ProfileManager.ProfileName);
                nativeWriter.Write(creationDate.Ticks);
                nativeWriter.Write(modifiedDate.Ticks);
                nativeWriter.Write(gameVersion);
                nativeWriter.WriteNullTerminatedString(modSettings.Title);
                nativeWriter.WriteNullTerminatedString(modSettings.Author);
                nativeWriter.WriteNullTerminatedString(modSettings.Category);
                nativeWriter.WriteNullTerminatedString(modSettings.Version);
                nativeWriter.WriteNullTerminatedString(modSettings.Description);
                if (modSettings.Icon != null && modSettings.Icon.Length != 0)
                {
                    nativeWriter.Write(modSettings.Icon.Length);
                    nativeWriter.Write(modSettings.Icon);
                }
                else
                {
                    nativeWriter.Write(0);
                }
                for (int i = 0; i < 4; i++)
                {
                    byte[] screenshot = modSettings.GetScreenshot(i);
                    if (screenshot != null && screenshot.Length != 0)
                    {
                        nativeWriter.Write(screenshot.Length);
                        nativeWriter.Write(screenshot);
                    }
                    else
                    {
                        nativeWriter.Write(0);
                    }
                }
                nativeWriter.Write(0);
                long position = nativeWriter.BaseStream.Position;
                nativeWriter.Write(3735928559u);
                int num = 0;
                foreach (BundleEntry item in AssetManager.EnumerateBundles(BundleType.None, modifiedOnly: true))
                {
                    if (item.Added)
                    {
                        nativeWriter.WriteNullTerminatedString(item.Name);
                        nativeWriter.WriteNullTerminatedString(AssetManager.GetSuperBundle(item.SuperBundleId).Name);
                        nativeWriter.Write((int)item.Type);
                        num++;
                    }
                }
                nativeWriter.BaseStream.Position = position;
                nativeWriter.Write(num);
                nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
                position = nativeWriter.BaseStream.Position;
                nativeWriter.Write(3735928559u);
                num = 0;
                foreach (EbxAssetEntry item2 in AssetManager.EnumerateEbx("", modifiedOnly: true))
                {
                    if (item2.IsAdded)
                    {
                        nativeWriter.WriteNullTerminatedString(item2.Name);
                        nativeWriter.Write(item2.Guid.Value);
                        num++;
                    }
                }
                nativeWriter.BaseStream.Position = position;
                nativeWriter.Write(num);
                nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
                position = nativeWriter.BaseStream.Position;
                nativeWriter.Write(3735928559u);
                num = 0;
                foreach (ResAssetEntry item3 in AssetManager.EnumerateRes(0u, modifiedOnly: true))
                {
                    if (item3.IsAdded)
                    {
                        nativeWriter.WriteNullTerminatedString(item3.Name);
                        nativeWriter.Write(item3.ResRid);
                        nativeWriter.Write(item3.ResType);
                        nativeWriter.Write(item3.ResMeta);
                        num++;
                    }
                }
                nativeWriter.BaseStream.Position = position;
                nativeWriter.Write(num);
                nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
                position = nativeWriter.BaseStream.Position;
                nativeWriter.Write(3735928559u);
                num = 0;
                foreach (ChunkAssetEntry item4 in AssetManager.EnumerateChunks(modifiedOnly: true))
                {
                    if (item4.IsAdded)
                    {
                        nativeWriter.Write(item4.Id);
                        nativeWriter.Write(item4.H32);
                        num++;
                    }
                }
                nativeWriter.BaseStream.Position = position;
                nativeWriter.Write(num);
                nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
                position = nativeWriter.BaseStream.Position;
                var modifiedEbxAssets = AssetManager.EnumerateEbx("", modifiedOnly: true, includeLinked: true);

                nativeWriter.Write(modifiedEbxAssets.Count());
                num = 0;
                foreach (EbxAssetEntry modifiedEbx in modifiedEbxAssets)
                {
                    SaveModifedEbxV23(nativeWriter, modifiedEbx);
                    WriteModifedEbxV24(nativeWriter, modifiedEbx);
                    num++;
                }
                nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
                position = nativeWriter.BaseStream.Position;
                var modifiedResAssets = AssetManager.EnumerateRes(0u, modifiedOnly: true);
                nativeWriter.Write(modifiedResAssets.Count());
                num = 0;
                foreach (ResAssetEntry modifiedRes in modifiedResAssets)
                {
                    nativeWriter.WriteNullTerminatedString(modifiedRes.Name);
                    SaveLinkedAssets(modifiedRes, nativeWriter);
                    nativeWriter.Write(modifiedRes.IsDirectlyModified);
                    if (modifiedRes.IsDirectlyModified)
                    {
                        nativeWriter.Write(modifiedRes.ModifiedEntry.Sha1);
                        nativeWriter.Write(modifiedRes.ModifiedEntry.OriginalSize);
                        if (modifiedRes.ModifiedEntry.ResMeta != null)
                        {
                            nativeWriter.Write(modifiedRes.ModifiedEntry.ResMeta.Length);
                            nativeWriter.Write(modifiedRes.ModifiedEntry.ResMeta);
                        }
                        else
                        {
                            nativeWriter.Write(0);
                        }
                        nativeWriter.WriteNullTerminatedString(modifiedRes.ModifiedEntry.UserData);
                        nativeWriter.Write(modifiedRes.AddBundles.Count);
                        foreach (int addBundle2 in modifiedRes.AddBundles)
                        {
                            nativeWriter.WriteNullTerminatedString(AssetManager.GetBundleEntry(addBundle2).Name);
                        }
                        byte[] array2 = modifiedRes.ModifiedEntry.Data;
                        //if (item6.ModifiedEntry.DataObject != null)
                        //{
                        //	array2 = (item6.ModifiedEntry.DataObject as ModifiedResource).Save();
                        //}
                        nativeWriter.Write(array2.Length);
                        nativeWriter.Write(array2);
                        if (updateDirtyState)
                        {
                            modifiedRes.IsDirty = false;
                        }
                    }
                    num++;
                }
                nativeWriter.BaseStream.Position = position;
                nativeWriter.Write(num);
                nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
                position = nativeWriter.BaseStream.Position;
                var modifiedChunks = AssetManager.EnumerateChunks(modifiedOnly: true);
                nativeWriter.Write((uint)modifiedChunks.Count());
                num = 0;
                foreach (ChunkAssetEntry modifiedChunk in modifiedChunks)
                {
                    nativeWriter.Write(modifiedChunk.Id);
                    nativeWriter.Write(modifiedChunk.ModifiedEntry.Sha1);
                    nativeWriter.Write(modifiedChunk.ModifiedEntry.LogicalOffset);
                    nativeWriter.Write(modifiedChunk.ModifiedEntry.LogicalSize);
                    nativeWriter.Write(modifiedChunk.ModifiedEntry.RangeStart);
                    nativeWriter.Write(modifiedChunk.ModifiedEntry.RangeEnd);
                    nativeWriter.Write(modifiedChunk.ModifiedEntry.FirstMip);
                    nativeWriter.Write(modifiedChunk.ModifiedEntry.H32);
                    //nativeWriter.Write(modifiedChunk.ModifiedEntry.AddToChunkBundle);
                    nativeWriter.Write(false);
                    nativeWriter.WriteNullTerminatedString(modifiedChunk.ModifiedEntry.UserData);
                    nativeWriter.Write(modifiedChunk.AddBundles.Count);
                    foreach (int addBundle3 in modifiedChunk.AddBundles)
                    {
                        nativeWriter.WriteNullTerminatedString(AssetManager.GetBundleEntry(addBundle3).Name);
                    }
                    nativeWriter.Write(modifiedChunk.ModifiedEntry.Data.Length);
                    nativeWriter.Write(modifiedChunk.ModifiedEntry.Data);
                    if (updateDirtyState)
                    {
                        modifiedChunk.IsDirty = false;
                    }
                }
                nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
                position = nativeWriter.BaseStream.Position;

                // --------------------
                // Legacy Files
                nativeWriter.Write(AssetManager.EnumerateCustomAssets("legacy", modifiedOnly: true).Count());
                foreach (LegacyFileEntry lfe in AssetManager.EnumerateCustomAssets("legacy", modifiedOnly: true))
                {
                    if (lfe.Name != null)
                    {
                        var serialisedLFE = JsonConvert.SerializeObject(lfe);
                        nativeWriter.WriteLengthPrefixedString(serialisedLFE);
                    }
                }

                // -----------------------
                // Added Legacy Files
                var hasAddedLegacyFiles = AssetManager.Instance.CustomAssetManagers["legacy"].AddedFileEntries.Count > 0;
                nativeWriter.Write(hasAddedLegacyFiles);
                if (hasAddedLegacyFiles)
                {
                    nativeWriter.Write(AssetManager.Instance.CustomAssetManagers["legacy"].AddedFileEntries.Count);
                    foreach (var lfe in AssetManager.Instance.CustomAssetManagers["legacy"].AddedFileEntries)
                    {
                        nativeWriter.WriteLengthPrefixedString(JsonConvert.SerializeObject(lfe));
                    }
                }

                // -----------------------
                // Embedded files
                nativeWriter.Write(AssetManager.EmbeddedFileEntries.Count > 0);
                nativeWriter.Write(AssetManager.EmbeddedFileEntries.Count);
                foreach (EmbeddedFileEntry efe in AssetManager.EmbeddedFileEntries)
                {
                    var serialisedEFE = JsonConvert.SerializeObject(efe);
                    nativeWriter.WriteLengthPrefixedString(serialisedEFE);
                }

                // ------------------------
                // Locale.ini mod
                var hasLocaleIniMod = AssetManager.LocaleINIMod.HasUserData;
                nativeWriter.Write(hasLocaleIniMod);
                if (hasLocaleIniMod)
                {
                    nativeWriter.Write(FileSystem.LocaleIsEncrypted);
                    nativeWriter.Write(AssetManager.LocaleINIMod.UserData.Length);
                    nativeWriter.Write(AssetManager.LocaleINIMod.UserData);
                }

                if (updateDirtyState)
                {
                    modSettings.ClearDirtyFlag();
                }
            }
            if (File.Exists(text))
            {
                bool flag = false;
                using (FileStream fileStream = new FileStream(text, FileMode.Open, FileAccess.Read))
                {
                    if (fileStream.Length > 0)
                    {
                        flag = true;
                    }
                }
                if (flag)
                {
                    File.Delete(fileInfo.FullName);
                    File.Move(text, fileInfo.FullName);
                }
            }

            return true;
        }

        /// <summary>
        /// Writes using the EbxWriter method to create a byte array to save to the file. Will only write if Current Format Version is 24.
        /// </summary>
        /// <param name="nativeWriter"></param>
        /// <param name="modifiedEbx"></param>
        private void SaveModifedEbxV23(NativeWriter nativeWriter, EbxAssetEntry modifiedEbx)
        {
            if (CurrentFormatVersion > 23)
                return;

            byte[] decompressedArrayOfModified = null;
            if (modifiedEbx.IsDirectlyModified)
            {
                decompressedArrayOfModified = EbxBaseWriter.GetEbxArrayDecompressed(modifiedEbx);
                if (decompressedArrayOfModified == null || decompressedArrayOfModified.Length == 0)
                    modifiedEbx.ModifiedEntry = null;
            }

            nativeWriter.WriteNullTerminatedString(modifiedEbx.Name);
            SaveLinkedAssets(modifiedEbx, nativeWriter);
            nativeWriter.Write(modifiedEbx.IsDirectlyModified);

            if (modifiedEbx.IsDirectlyModified)
            {
                nativeWriter.Write(modifiedEbx.ModifiedEntry.IsTransientModified);
                nativeWriter.WriteNullTerminatedString(modifiedEbx.ModifiedEntry.UserData);
                nativeWriter.Write(modifiedEbx.AddBundles.Count);
                foreach (int addBundle in modifiedEbx.AddBundles)
                {
                    nativeWriter.WriteNullTerminatedString(AssetManager.GetBundleEntry(addBundle).Name);
                }

                nativeWriter.Write(decompressedArrayOfModified.Length);
                nativeWriter.Write(decompressedArrayOfModified);
                modifiedEbx.IsDirty = false;
            }
        }

        /// <summary>
        /// Writes into Serialized JSON Objects. Will only write if Current Format Version is 24.
        /// </summary>
        /// <param name="nativeWriter"></param>
        /// <param name="modifiedEbx"></param>
        private void WriteModifedEbxV24(NativeWriter nativeWriter, EbxAssetEntry modifiedEbx)
        {
            if (CurrentFormatVersion <= 23)
                return;

            var ebx = AssetManager.Instance.GetEbx(modifiedEbx);
            if (ebx == null || ebx.RootObject == null)
            {
                modifiedEbx.ModifiedEntry = null;
            }

            nativeWriter.WriteLengthPrefixedString(modifiedEbx.Name);
            SaveLinkedAssets(modifiedEbx, nativeWriter);
            nativeWriter.Write(modifiedEbx.IsDirectlyModified);

            if (modifiedEbx.IsDirectlyModified)
            {
                nativeWriter.Write(modifiedEbx.ModifiedEntry.IsTransientModified);
                nativeWriter.WriteLengthPrefixedString(modifiedEbx.ModifiedEntry.UserData);
                nativeWriter.Write(modifiedEbx.AddBundles.Count);
                foreach (int addBundle in modifiedEbx.AddBundles)
                {
                    nativeWriter.WriteLengthPrefixedString(AssetManager.GetBundleEntry(addBundle).Name);
                }

                var ebxRootObject = ebx.RootObject;
                foreach (var prop in ebxRootObject.GetProperties())
                {
                    nativeWriter.WriteLengthPrefixedString(prop.Name);
                    var propValue = prop.GetValue(ebxRootObject);
                    nativeWriter.WriteLengthPrefixedString(JsonConvert.SerializeObject(propValue));
                }

                modifiedEbx.IsDirty = false;
            }
        }

        private void ReadModifiedEbxV24(NativeReader reader, EbxAssetEntry modifiedEbx)
        {

        }

        public ModSettings GetModSettings()
        {
            return modSettings;
        }

        //public override void WriteToMod(string filename, ModSettings overrideSettings = null)
        //{
        //    if (overrideSettings == null)
        //        overrideSettings = ModSettings;

        //    byte[] projectbytes;

        //    if (File.Exists(filename))
        //        File.Delete(filename);

        //    using var memoryStream = new MemoryStream();

        //    FrostbiteModWriter frostyModWriter = new FrostbiteModWriter(memoryStream, overrideSettings);
        //    frostyModWriter.WriteProject(this);

        //    memoryStream.Position = 0;
        //    using (var nr = new NativeReader(memoryStream))
        //    projectbytes = nr.ReadToEnd();
        //    using NativeWriter nwFinal = new NativeWriter(new FileStream(filename, FileMode.CreateNew));
        //    nwFinal.Write(projectbytes);

        //}

        //public override void WriteToFIFAMod(string filename, ModSettings overrideSettings = null)
        //{
        //    if (overrideSettings == null)
        //        overrideSettings = ModSettings;

        //    if (File.Exists(filename))
        //        File.Delete(filename);
        //    using (var fs = new FileStream(filename, FileMode.Create))
        //    {
        //        FIFAModWriter frostyModWriter = new FIFAModWriter(ProfileManager.LoadedProfile.Name, AssetManager, FileSystem
        //            , fs
        //            , overrideSettings);
        //        frostyModWriter.WriteProject(this);
        //    }
        //}

        public static void SaveLinkedAssets(AssetEntry entry, NativeWriter writer)
        {
            writer.Write(entry.LinkedAssets.Count);
            foreach (AssetEntry linkedAsset in entry.LinkedAssets)
            {
                writer.WriteNullTerminatedString(linkedAsset.AssetType);
                if (linkedAsset is ChunkAssetEntry)
                {
                    writer.Write(((ChunkAssetEntry)linkedAsset).Id);
                }
                else
                {
                    writer.WriteNullTerminatedString(linkedAsset.Name);
                }
            }
        }

        public List<AssetEntry> LoadLinkedAssets(NativeReader reader)
        {
            int num = reader.ReadInt();
            List<AssetEntry> list = new List<AssetEntry>();
            for (int i = 0; i < num; i++)
            {
                string text = reader.ReadNullTerminatedString();
                if (text == "ebx")
                {
                    string name = reader.ReadNullTerminatedString();
                    EbxAssetEntry ebxEntry = AssetManager.GetEbxEntry(name);
                    if (ebxEntry != null)
                    {
                        list.Add(ebxEntry);
                    }
                }
                else if (text == "res")
                {
                    string name2 = reader.ReadNullTerminatedString();
                    ResAssetEntry resEntry = AssetManager.GetResEntry(name2);
                    if (resEntry != null)
                    {
                        list.Add(resEntry);
                    }
                }
                else if (text == "chunk")
                {
                    Guid id = reader.ReadGuid();
                    ChunkAssetEntry chunkEntry = AssetManager.GetChunkEntry(id);
                    if (chunkEntry != null)
                    {
                        list.Add(chunkEntry);
                    }
                }
                else
                {
                    string key = reader.ReadNullTerminatedString();
                    AssetEntry customAssetEntry = AssetManager.GetCustomAssetEntry(text, key);
                    if (customAssetEntry != null)
                    {
                        list.Add(customAssetEntry);
                    }
                }
            }
            return list;
        }

        public void LoadLinkedAssets(DbObject asset, AssetEntry entry, uint version)
        {
            if (version == 2)
            {
                string value = asset.GetValue<string>("linkedAssetType");
                if (value == "res")
                {
                    string value2 = asset.GetValue<string>("linkedAssetId");
                    entry.LinkedAssets.Add(AssetManager.GetResEntry(value2));
                }
                else if (value == "chunk")
                {
                    Guid value3 = asset.GetValue<Guid>("linkedAssetId");
                    entry.LinkedAssets.Add(AssetManager.GetChunkEntry(value3));
                }
            }
            else
            {
                foreach (DbObject item in asset.GetValue<DbObject>("linkedAssets"))
                {
                    string value4 = item.GetValue<string>("type");
                    if (value4 == "ebx")
                    {
                        string value5 = item.GetValue<string>("id");
                        EbxAssetEntry ebxEntry = AssetManager.GetEbxEntry(value5);
                        if (ebxEntry != null)
                        {
                            entry.LinkedAssets.Add(ebxEntry);
                        }
                    }
                    else if (value4 == "res")
                    {
                        string value6 = item.GetValue<string>("id");
                        ResAssetEntry resEntry = AssetManager.GetResEntry(value6);
                        if (resEntry != null)
                        {
                            entry.LinkedAssets.Add(resEntry);
                        }
                    }
                    else if (value4 == "chunk")
                    {
                        Guid value7 = item.GetValue<Guid>("id");
                        ChunkAssetEntry chunkEntry = AssetManager.GetChunkEntry(value7);
                        if (chunkEntry != null)
                        {
                            entry.LinkedAssets.Add(chunkEntry);
                        }
                    }
                    else
                    {
                        string value8 = item.GetValue<string>("id");
                        AssetEntry customAssetEntry = AssetManager.GetCustomAssetEntry(value4, value8);
                        if (customAssetEntry != null)
                        {
                            entry.LinkedAssets.Add(customAssetEntry);
                        }
                    }
                }
            }
        }

        public int EBXCount;
        public int RESCount;
        public int ChunkCount;
        public int LegacyCount;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<bool> InternalLoad(NativeReader reader, CancellationToken cancellationToken = default(CancellationToken))
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (reader.ReadULong() != 98218709832262L)
                return false;

            cancellationToken.ThrowIfCancellationRequested();

            List<Task> tasks = new List<Task>();
            ProjectFormatVersion = reader.ReadUInt();

            // We dont support stupidly old Frosty Editor projects
            if (ProjectFormatVersion <= 11u)
                return false;

            // Automatically convert projects to new Format Version
            if (ProfileManager.Game > EGame.FIFA21 && ProjectFormatVersion == 12u)
                ProjectFormatVersion = 23u;

            if (reader.ReadNullTerminatedString() != ProfileManager.ProfileName)
            {
                return false;
            }
            Dictionary<int, AssetEntry> dictionary = new Dictionary<int, AssetEntry>();
            creationDate = new DateTime(reader.ReadLong());
            modifiedDate = new DateTime(reader.ReadLong());
            gameVersion = reader.ReadUInt();
            modSettings.Title = reader.ReadNullTerminatedString();
            modSettings.Author = reader.ReadNullTerminatedString();
            modSettings.Category = reader.ReadNullTerminatedString();
            modSettings.Version = reader.ReadNullTerminatedString();
            modSettings.Description = reader.ReadNullTerminatedString();
            int iconAndSSLength = reader.ReadInt();
            if (iconAndSSLength > 0)
            {
                modSettings.Icon = reader.ReadBytes(iconAndSSLength);
            }
            for (int i = 0; i < 4; i++)
            {
                iconAndSSLength = reader.ReadInt();
                if (iconAndSSLength > 0)
                {
                    modSettings.SetScreenshot(i, reader.ReadBytes(iconAndSSLength));
                }
            }
            modSettings.ClearDirtyFlag();
            int count = reader.ReadInt();
            count = reader.ReadInt();
            for (int j = 0; j < count; j++)
            {
                string name = reader.ReadNullTerminatedString();
                string sbname = reader.ReadNullTerminatedString();
                BundleType type = (BundleType)reader.ReadInt();
                AssetManager.AddBundle(name, type, AssetManager.GetSuperBundleId(sbname));
            }
            count = reader.ReadInt();
            for (int k = 0; k < count; k++)
            {
                EbxAssetEntry ebxAssetEntry = new EbxAssetEntry();
                ebxAssetEntry.Name = reader.ReadNullTerminatedString();
                ebxAssetEntry.Id = reader.ReadGuid();
                AssetManager.AddEbx(ebxAssetEntry);
            }
            count = reader.ReadInt();
            for (int l = 0; l < count; l++)
            {
                ResAssetEntry resAssetEntry = new ResAssetEntry();
                resAssetEntry.Name = reader.ReadNullTerminatedString();
                resAssetEntry.ResRid = reader.ReadULong();
                resAssetEntry.ResType = reader.ReadUInt();
                resAssetEntry.ResMeta = reader.ReadBytes(16);
                AssetManager.AddRes(resAssetEntry);
            }
            count = reader.ReadInt();
            for (int m = 0; m < count; m++)
            {
                ChunkAssetEntry chunkAssetEntry = new ChunkAssetEntry();
                chunkAssetEntry.Id = reader.ReadGuid();
                chunkAssetEntry.H32 = reader.ReadInt();
                //AssetManager.ForceChunkRemoval = true;
                //AssetManager.AddChunk(chunkAssetEntry);
                //AssetManager.ForceChunkRemoval = false;
            }
            count = reader.ReadInt();
            EBXCount = count;
            for (int n = 0; n < count; n++)
            {
                ReadModifiedEbxV23(reader);

            }
            count = reader.ReadInt();
            RESCount = count;

            for (int num6 = 0; num6 < count; num6++)
            {
                string text = reader.ReadNullTerminatedString();
                List<AssetEntry> collection2 = LoadLinkedAssets(reader);
                bool flag2 = reader.ReadBoolean();
                FMT.FileTools.Sha1 sha = FMT.FileTools.Sha1.Zero;
                long originalSize = 0L;
                List<int> list2 = new List<int>();
                byte[] resMeta = null;
                byte[] array2 = null;
                string userData2 = "";
                if (flag2)
                {
                    sha = reader.ReadSha1();
                    originalSize = reader.ReadLong();
                    int num7 = reader.ReadInt();
                    if (num7 > 0)
                    {
                        resMeta = reader.ReadBytes(num7);
                    }
                    userData2 = reader.ReadNullTerminatedString();
                    num7 = reader.ReadInt();
                    for (int num8 = 0; num8 < num7; num8++)
                    {
                        string name4 = reader.ReadNullTerminatedString();
                        int bundleId2 = AssetManager.GetBundleId(name4);
                        if (bundleId2 != -1)
                        {
                            list2.Add(bundleId2);
                        }
                    }
                    array2 = reader.ReadBytes(reader.ReadInt());
                }
                ResAssetEntry resEntry = AssetManager.GetResEntry(text);
                if (resEntry == null)
                {
                    continue;
                }
                resEntry.LinkedAssets.AddRange(collection2);
                resEntry.AddBundles.AddRange(list2);
                if (flag2)
                {
                    resEntry.ModifiedEntry = new ModifiedAssetEntry();
                    resEntry.ModifiedEntry.Sha1 = sha;
                    resEntry.ModifiedEntry.OriginalSize = originalSize;
                    resEntry.ModifiedEntry.ResMeta = resMeta;
                    resEntry.ModifiedEntry.UserData = userData2;
                    if (sha == Sha1.Zero)
                    {
                        ((ModifiedAssetEntry)resEntry.ModifiedEntry).DataObject = ModifiedResource.Read(array2);
                    }
                    else
                    {
                        resEntry.ModifiedEntry.Data = array2;
                    }
                }
                int key2 = Fnv1.HashString(resEntry.Name);
                if (!dictionary.ContainsKey(key2))
                {
                    dictionary.Add(key2, resEntry);
                }
            }
            count = reader.ReadInt();
            ChunkCount = count;

            for (int iModifiedChunk = 0; iModifiedChunk < count; iModifiedChunk++)
            {
                Guid id = reader.ReadGuid();
                FMT.FileTools.Sha1 sha2 = reader.ReadSha1();
                uint logicalOffset = reader.ReadUInt();
                uint logicalSize = reader.ReadUInt();
                uint rangeStart = reader.ReadUInt();
                uint rangeEnd = reader.ReadUInt();
                int firstMip = reader.ReadInt();
                int h = reader.ReadInt();
                bool addToChunkBundle = reader.ReadBoolean();
                string userData3 = "";
                userData3 = reader.ReadNullTerminatedString();
                List<int> list3 = new List<int>();
                int num12 = reader.ReadInt();
                for (int num13 = 0; num13 < num12; num13++)
                {
                    string name6 = reader.ReadNullTerminatedString();
                    int bundleId3 = AssetManager.GetBundleId(name6);
                    if (bundleId3 != -1)
                    {
                        list3.Add(bundleId3);
                    }
                }
                byte[] data = reader.ReadBytes(reader.ReadInt());
                ChunkAssetEntry chunkAssetEntry2 = AssetManager.GetChunkEntry(id);
                if (chunkAssetEntry2 == null)
                {
                    ChunkAssetEntry chunkAssetEntry3 = new ChunkAssetEntry();
                    chunkAssetEntry3.Id = id;
                    chunkAssetEntry3.H32 = h;
                    AssetManager.AddChunk(chunkAssetEntry3);
                    if (dictionary.ContainsKey(chunkAssetEntry3.H32))
                    {
                        foreach (int bundle in dictionary[chunkAssetEntry3.H32].Bundles)
                        {
                            chunkAssetEntry3.AddToBundle(bundle);
                        }
                    }
                    chunkAssetEntry2 = chunkAssetEntry3;
                }
                chunkAssetEntry2.AddBundles.AddRange(list3);
                chunkAssetEntry2.ModifiedEntry = new ModifiedAssetEntry();
                chunkAssetEntry2.ModifiedEntry.Sha1 = sha2;
                chunkAssetEntry2.ModifiedEntry.LogicalOffset = logicalOffset;
                chunkAssetEntry2.ModifiedEntry.LogicalSize = logicalSize;
                chunkAssetEntry2.ModifiedEntry.RangeStart = rangeStart;
                chunkAssetEntry2.ModifiedEntry.RangeEnd = rangeEnd;
                chunkAssetEntry2.ModifiedEntry.FirstMip = firstMip;
                chunkAssetEntry2.ModifiedEntry.H32 = h;
                //chunkAssetEntry2.ModifiedEntry.AddToChunkBundle = addToChunkBundle;
                chunkAssetEntry2.ModifiedEntry.UserData = userData3;
                chunkAssetEntry2.ModifiedEntry.Data = data;
            }

            // ----------------------------------------------------------------------------------------------------
            // LEGACY FILE HANDLING
            //
            //await Task.FromResult(() =>
            //{

                // Count of Modified Legacy Files
                var legacyFileManager = AssetManager.Instance.CustomAssetManagers["legacy"] as ChunkFileManager2022;
                if (reader.Length > reader.Position)
                {
                    count = reader.ReadInt();
                    LegacyCount = count;

                    if (legacyFileManager != null)
                    {
                        List<LegacyFileEntry> legacyFileEntries = new List<LegacyFileEntry>(count);
                        for (int iItem = 0; iItem < count; iItem++)
                        {
                            var rawFile = reader.ReadLengthPrefixedString();
                            if (legacyFileManager != null)
                            {
                                dynamic lfeD = JsonConvert.DeserializeObject<dynamic>(rawFile, new JsonSerializerSettings()
                                {
                                    TypeNameHandling = TypeNameHandling.All
                                });
                                LegacyFileEntry lfe = new LegacyFileEntry(new ModifiedAssetEntry()
                                {
                                    Data = lfeD.ModifiedEntry.Data
                                })
                                {
                                    Name = lfeD.Name
                                };
                                legacyFileEntries.Add(lfe);
                            }
                        }
                        legacyFileManager.LoadEntriesModifiedFromProject(legacyFileEntries);
                    }
                }

                // Count of Added Legacy Files
                if (reader.Length > reader.Position)
                {
                    bool addedLegacyFiles = reader.ReadBoolean();
                    if (addedLegacyFiles)
                    {
                        count = reader.ReadInt();
                        LegacyCount += count;

                        if (legacyFileManager != null)
                        {
                            List<LegacyFileEntry> legacyFileEntries = new List<LegacyFileEntry>(count);
                            for (int iItem = 0; iItem < count; iItem++)
                            {
                                var rawFile = reader.ReadLengthPrefixedString();
                                if (legacyFileManager != null)
                                {
                                    LegacyFileEntry lfe = JsonConvert.DeserializeObject<LegacyFileEntry>(rawFile);
                                    legacyFileEntries.Add(lfe);
                                }
                            }
                            legacyFileManager.LoadEntriesAddedFromProject(legacyFileEntries);
                        }
                    }
                }
            //});

            if (reader.Length > reader.Position)
            {
                bool hasEmbeddedFiles = reader.ReadBoolean();
                int embeddedFileCount = reader.ReadInt();
                if (hasEmbeddedFiles)
                {
                    AssetManager.Instance.EmbeddedFileEntries.Clear();
                    for (int iItem = 0; iItem < embeddedFileCount; iItem++)
                    {
                        var rawFile = reader.ReadLengthPrefixedString();
                        EmbeddedFileEntry efe = JsonConvert.DeserializeObject<EmbeddedFileEntry>(rawFile);
                        AssetManager.Instance.EmbeddedFileEntries.Add(efe);
                    }
                }
            }

            //
            //
            // ----------------------------------------------------------------------------------------------------

            // ------------------------
            // Locale.ini mod
            if (reader.Length > reader.Position)
            {
                var hasLocaleIniMod = reader.ReadBoolean();
                if (hasLocaleIniMod)
                {
                    var localeIsEncrypted = reader.ReadBoolean();
                    var localeiniSize = reader.ReadInt();
                    AssetManager.Instance.LocaleINIMod.Save(reader.ReadBytes(localeiniSize));
                    //AssetManager.Instance.LocaleINIMod = new Frostbite.IO.LocaleINIMod(reader.ReadBytes(localeiniSize));
                }
            }

            //await Task.WhenAll(tasks.ToArray());
            Task.WaitAll(tasks.ToArray());
            return true;
        }

        private void ReadModifiedEbxV23(NativeReader reader)
        {
            if (ProjectFormatVersion > 23u)
                return;

            string ebxName = reader.ReadNullTerminatedString();

            List<AssetEntry> collection = LoadLinkedAssets(reader);
            bool flag = reader.ReadBoolean();
            bool isTransientModified = false;
            string userData = "";
            List<int> list = new List<int>();
            byte[] ebxAssetBytes = null;
            if (flag)
            {
                isTransientModified = reader.ReadBoolean();
                userData = reader.ReadNullTerminatedString();
                int num4 = reader.ReadInt();
                for (int num5 = 0; num5 < num4; num5++)
                {
                    string name3 = reader.ReadNullTerminatedString();
                    int bundleId = AssetManager.GetBundleId(name3);
                    if (bundleId != -1)
                    {
                        list.Add(bundleId);
                    }
                }
                ebxAssetBytes = reader.ReadBytes(reader.ReadInt());
            }
            var readerPositionBeforeEbx = reader.Position;
            var readerPositionAfterEbx = reader.Position + (ebxAssetBytes != null ? ebxAssetBytes.Length : 0);
            EbxAssetEntry ebxEntry = AssetManager.GetEbxEntry(ebxName);
            if (ebxEntry != null)
            {
                ebxEntry.LinkedAssets.AddRange(collection);
                ebxEntry.AddBundles.AddRange(list);
                if (flag)
                {
                    ebxEntry.ModifiedEntry = new ModifiedAssetEntry();
                    ebxEntry.ModifiedEntry.IsTransientModified = isTransientModified;
                    ebxEntry.ModifiedEntry.UserData = userData;
                    ebxEntry.ModifiedEntry.Data = ebxAssetBytes;

                    if (ProfileManager.IsFIFA21DataVersion())
                    {
                        EbxReader ebxReader = new EbxReader(new MemoryStream(ebxAssetBytes));
                        EbxAsset ebxAsset = ebxReader.ReadAsset();
                        ebxEntry.ModifiedEntry.DataObject = ebxAsset;
                    }

                }

            }
            reader.Position = readerPositionBeforeEbx;
        }
    }

    public class ModSettings
    {
        private string title = "";

        private string author = "";

        private string category = "";

        private string version = "";

        private string description = "";

        private byte[] iconData;

        private byte[][] screenshotData;

        private bool isDirty;

        public bool IsDirty => isDirty;

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                if (!title.Equals(value))
                {
                    title = value;
                    isDirty = true;
                }
            }
        }

        public string Author
        {
            get
            {
                return author;
            }
            set
            {
                if (!author.Equals(value))
                {
                    author = value;
                    isDirty = true;
                }
            }
        }

        public string Category
        {
            get
            {
                return category;
            }
            set
            {
                if (!category.Equals(value))
                {
                    category = value;
                    isDirty = true;
                }
            }
        }

        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                if (!version.Equals(value))
                {
                    version = value;
                    isDirty = true;
                }
            }
        }


        /// <summary>
        /// Description of the mod. Is usually a Multi-Line description of the entire mod.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                if (!description.Equals(value))
                {
                    description = value;
                    isDirty = true;
                }

                if (!description.Contains(mergedModListDescription))
                {
                    description += Environment.NewLine;
                    description += "Included Mods: " + Environment.NewLine;
                    description += mergedModListDescription;
                }
            }
        }

        public byte[] Icon
        {
            get
            {
                return iconData;
            }
            set
            {
                iconData = value;
                isDirty = true;
            }
        }

        public List<ModSettings> MergedModList { get; set; } = new List<ModSettings>();

        private string mergedModListDescription 
        { 
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach(var mod in MergedModList)
                {
                    stringBuilder.AppendLine(mod.Author + "'s " + mod.Title);
                }
                return stringBuilder.ToString();
            } 
        }

        public ModSettings()
        {
            screenshotData = new byte[4][];
        }

        public void SetScreenshot(int index, byte[] buffer)
        {
            screenshotData[index] = buffer;
            isDirty = true;
        }

        public byte[] GetScreenshot(int index)
        {
            return screenshotData[index];
        }

        public void ClearDirtyFlag()
        {
            isDirty = false;
        }

        public ModSettings UpdateFromOtherModSettings(ModSettings otherModSettings)
        {
            Author = otherModSettings.Author;
            Description = otherModSettings.Description;
            Title = otherModSettings.Title;
            Category = otherModSettings.Category;
            Version = otherModSettings.Version;
            return this;
        }
    }


}