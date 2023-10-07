using FMT.FileTools;
using FMT.FileTools.Modding;
using FMT.Logging;
using FrostbiteSdk.FrostbiteSdk.Managers;
using FrostySdk.Frostbite.IO.Input;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.Frosty.FET;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.ModsAndProjects.FET;
using FrostySdk.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using v2k4FIFAModding.Frosty;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using static PInvoke.Kernel32;

namespace FrostySdk.ModsAndProjects.Projects
{
    public class FMTProject : BaseProject, IProject
    {
        public static int MasterProjectVersion { get; } = 2;
        private static string projectExtension { get; } = ".fmtproj";
        private static string projectFilter { get; } = $"FMT Project file (*{projectExtension})|*{projectExtension}";
        private string projectFilePath { get; set; }

        public int ProjectVersion { get; private set; } = 2;
        public uint GameDataVersion { get; private set; }

        public FileInfo projectFileInfo { get { return new FileInfo(projectFilePath); } }

        public override bool IsDirty 
        { 
            get 
            {

                foreach (var ebx in AssetManager.Instance.EnumerateEbx(modifiedOnly: true))
                {
                    if (ebx.IsDirty)
                        return true;
                }

                foreach (var res in AssetManager.Instance.EnumerateRes(modifiedOnly: true))
                {
                    if (res.IsDirty)
                        return true;
                }

                foreach (var chunk in AssetManager.Instance.EnumerateChunks(modifiedOnly: true))
                {
                    if (chunk.IsDirty)
                        return true;
                }

                return false;

            }
            set
            {
                // Not used
            }
        }

        public override string Filename => projectFilePath;

        public override string DisplayName
        {
            get
            {
                if (Filename == "")
                {
                    return "New Project.fbproject";
                }
                return new FileInfo(Filename).Name;
            }
        }

        public override AssetManager AssetManager => AssetManager.Instance;

        public override string FileExtension => ".fmtproj";

        //public override ModSettings ModSettings { get; private set; } = new ModSettings();

        public ModSettings GetModSettings()
        {
            return ProjectManagement.Instance.Project.ModSettings;
        }


        public FMTProject(string filePath)
        {
            if (!filePath.EndsWith(projectExtension, StringComparison.Ordinal))
                filePath += projectExtension;

            projectFilePath = filePath;
        }

        public static FMTProject Create(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new NullReferenceException(nameof(filePath));

            if (!filePath.EndsWith(projectExtension, StringComparison.Ordinal))
                filePath += projectExtension;

            if (File.Exists(filePath))
                File.Delete(filePath);

            FMTProject project = new FMTProject(filePath);// this;
            project.projectFilePath = filePath;
            if (!project.projectFileInfo.Directory.Exists)
                Directory.CreateDirectory(project.projectFileInfo.DirectoryName);

            project.Update();
            return project;
        }

        public static FMTProject Read(Stream stream, FMTProject project = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (project == null)
            {
                project = new FMTProject("EditorProject.fmtproj");
            }

            using (NativeReader nr = new NativeReader(stream))
            {
                project.ProjectVersion = nr.ReadInt();
                project.GameDataVersion = nr.ReadUInt();

                if (!ProfileManager.Initialize(project.GameDataVersion))
                {
                    return project;
                }

                if (project.GameDataVersion != ProfileManager.DataVersion)
                {
                    if (AssetManager.Instance != null && AssetManager.Instance.Logger != null)
                        AssetManager.Instance.Logger.LogWarning("You are loading a Project for a different game.");
                }
                project.ModSettings = JsonConvert.DeserializeObject<ModSettings>(nr.ReadLengthPrefixedString());

                var assetManagerPositions = new Dictionary<string, long>();
                var countOfAssetManagers = nr.ReadInt();
                for (var indexAM = 0; indexAM < countOfAssetManagers; indexAM++)
                {
                    assetManagerPositions.Add(nr.ReadLengthPrefixedString(), nr.ReadLong());
                }
                // Read Data
                nr.Position = assetManagerPositions["ebx"];
                project.EBXAssetsRead(nr);
                nr.Position = assetManagerPositions["res"];
                project.ResourceAssetsRead(nr, out var textureResources);
                nr.Position = assetManagerPositions["chunks"];
                project.ChunkAssetsRead(nr);
                nr.Position = assetManagerPositions["legacy"];
                project.LegacyFilesModifiedRead(nr);
                LegacyFilesAddedRead(nr);
                nr.Position = assetManagerPositions["embedded"];
                EmbeddedFilesRead(nr);
                nr.Position = assetManagerPositions["localeini"];
                LocaleINIRead(nr);
            }

            return project;
        }

        public static FMTProject Read(string filePath)
        {
            if (!filePath.EndsWith(projectExtension, StringComparison.Ordinal))
                filePath += projectExtension;

            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            return Read(new FileStream(filePath, FileMode.OpenOrCreate), new FMTProject(filePath));
        }


        public FMTProject Update()
        {
            if (File.Exists(projectFilePath))
                File.Delete(projectFilePath);

            Dictionary<string, long> writePositions = new Dictionary<string, long>();

            //var entryExporter = new AssetEntryExporter();
            using (var ms = new MemoryStream())
            {
                using (var nw = new NativeWriter(ms, true))
                {
                    // Master Version
                    nw.Write(MasterProjectVersion);
                    // Profile Version
                    nw.Write(ProfileManager.DataVersion);
                    // Mod Settings Json
                    nw.WriteLengthPrefixedString(JsonConvert.SerializeObject(GetModSettings()));

                    // number of positional stuff. ebx, res, chunks + "custom" + embedded + localeini etc
                    nw.Write(3 + AssetManager.Instance.CustomAssetManagers.Count + 2);
                    // --------- TODO: Convert these to "Custom" Asset Managers -----------------
                    nw.WriteLengthPrefixedString("ebx"); // key of cam
                    var ebxWritePosition = nw.Position;
                    nw.WriteULong(0ul); // position of data

                    nw.WriteLengthPrefixedString("res"); // key of cam
                    var resWritePosition = nw.Position;
                    nw.WriteULong(0ul); // position of data

                    nw.WriteLengthPrefixedString("chunks"); // key of cam
                    var chunksWritePosition = nw.Position;
                    nw.WriteULong(0ul); // position of data

                    var startOfCAMPosition = nw.Position;
                    // Custom Asset Managers
                    foreach (var cam in AssetManager.Instance.CustomAssetManagers)
                    {
                        nw.WriteLengthPrefixedString(cam.Key); // key of cam
                        nw.WriteULong(0ul); // position of data
                    }

                    nw.WriteLengthPrefixedString("embedded"); // key of cam
                    var embeddedWritePosition = nw.Position;
                    nw.WriteULong(0ul); // position of data

                    nw.WriteLengthPrefixedString("localeini"); // key of cam
                    var localeIniWritePosition = nw.Position;
                    nw.WriteULong(0ul); // position of data

                    // EBX
                    writePositions.Add("ebx", nw.Position);
                    EBXAssetsWrite(nw);
                    // RES
                    writePositions.Add("res", nw.Position);
                    ResourceAssetsWrite(nw);
                    // CHUNK
                    writePositions.Add("chunks", nw.Position);
                    ChunkAssetsWrite(nw);

                    // Legacy
                    writePositions.Add("legacy", nw.Position);
                    LegacyFilesModifiedWrite(nw);
                    LegacyFilesAddedWrite(nw);
                    // -----------------------
                    // Embedded files
                    writePositions.Add("embedded", nw.Position);
                    EmbeddedFilesWrite(nw);
                    // ------------------------
                    // Locale.ini mod
                    writePositions.Add("localeini", nw.Position);
                    LocaleINIWrite(nw);

                    nw.Position = ebxWritePosition;
                    nw.Write(writePositions["ebx"]);
                    nw.Position = resWritePosition;
                    nw.Write(writePositions["res"]);
                    nw.Position = chunksWritePosition;
                    nw.Write(writePositions["chunks"]);

                    nw.Position = startOfCAMPosition;
                    // Custom Asset Managers
                    foreach (var cam in AssetManager.Instance.CustomAssetManagers)
                    {
                        nw.WriteLengthPrefixedString(cam.Key); // key of cam
                        nw.Write(writePositions[cam.Key]);
                    }

                    nw.Position = embeddedWritePosition;
                    nw.Write(writePositions["embedded"]);

                    nw.Position = localeIniWritePosition;
                    nw.Write(writePositions["localeini"]);
                }

                //var msComp = new MemoryStream();
                //new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionLevel.Optimal).CopyTo(msComp);
                File.WriteAllBytes(projectFilePath, ms.ToArray());
            }

            foreach(var ebx in AssetManager.Instance.EnumerateEbx(modifiedOnly: true))
                ebx.IsDirty = false;

            foreach (var res in AssetManager.Instance.EnumerateRes(modifiedOnly: true))
                res.IsDirty = false;

            foreach (var chunk in AssetManager.Instance.EnumerateChunks(modifiedOnly: true))
                chunk.IsDirty = false;

            return this;
        }

        private static void EBXAssetsWrite(NativeWriter nw)
        {

            var modifiedEbxAssets = AssetManager.Instance.EnumerateEbx("", modifiedOnly: true, includeLinked: true);
            // EBX Count
            nw.Write(modifiedEbxAssets != null ? modifiedEbxAssets.Count() : 0);
            foreach (var item in modifiedEbxAssets)
            {
                using AssetEntryExporter assetEntryExporter = new AssetEntryExporter(item);
                // Item Name
                nw.WriteLengthPrefixedString(item.Name);
                // EBX Stream to Json
                nw.WriteLengthPrefixedString(assetEntryExporter.ExportToJson());
            }
        }

        private void EBXAssetsRead(NativeReader nr)
        {
            // EBX Count
            var count = nr.ReadInt();
            for (var i = 0; i < count; i++)
            {
                // Item Name
                var assetName = nr.ReadLengthPrefixedString();
                // EBX Stream to Json
                var json = nr.ReadLengthPrefixedString();

                if (AssetManager.Instance == null)
                    continue;

                var ebxAssetEntry = AssetManager.Instance.GetEbxEntry(assetName);
                if (ebxAssetEntry == null)
                {
                    FileLogger.WriteLine($"FMTProject: Unable to find AssetEntry by the name {assetName}");
                    Debug.WriteLine($"FMTProject: Unable to find AssetEntry by the name {assetName}");
                    continue;
                }
                AssetEntryImporter assetEntryImporter = new AssetEntryImporter(ebxAssetEntry);
                try
                {
                    assetEntryImporter.ImportWithJSON(Encoding.UTF8.GetBytes(json));
                    ebxAssetEntry.IsDirty = false;
                }
                catch (Exception ex)
                {
                    FileLogger.WriteLine($"Failed to load {assetName} from Project with message {ex.Message}");
                    Debug.WriteLine(ex);    
                }
                assetEntryImporter = null;
            }
        }

        private static void ResourceAssetsWrite(NativeWriter nw)
        {
            var modifiedResourceAssets = AssetManager.Instance.EnumerateRes(modifiedOnly: true);
            // RES Count
            nw.Write(modifiedResourceAssets != null ? modifiedResourceAssets.Count() : 0);
            foreach (var item in modifiedResourceAssets)
            {
                // Item Name
                nw.WriteLengthPrefixedString(item.Name);
                // Item Data -- Need to decompress and export it
                //nw.WriteLengthPrefixedBytes(item.ModifiedEntry.Data);
                //using (CasReader reader = new CasReader(new MemoryStream(item.ModifiedEntry.Data)))
                //    nw.WriteLengthPrefixedBytes(reader.Read());
                nw.WriteLengthPrefixedBytes(item.ModifiedEntry.Data);
            }
        }

        private void ResourceAssetsRead(NativeReader nr, out IList<Texture> textures)
        {
            textures = new List<Texture>();
            // RES Count
            var count = nr.ReadInt();
            for (var i = 0; i < count; i++)
            {
                // Item Name
                var assetName = nr.ReadLengthPrefixedString();
                // Item Data
                var data = nr.ReadLengthPrefixedBytes();

                if (AssetManager.Instance == null)
                    continue;

                var resAssetEntry = AssetManager.Instance.GetResEntry(assetName);
                if (resAssetEntry == null)
                    continue;

                AssetEntryImporter assetEntryImporter = new AssetEntryImporter(resAssetEntry);
                try
                {
                    assetEntryImporter.Import(data);
                    resAssetEntry = AssetManager.Instance.GetResEntry(assetName);

                    // Flag this as a texture!
                    bool isTexture = false;
                    if (AssetManager.Instance.EBX.ContainsKey(assetName))
                    {
                        var ebx = AssetManager.Instance.GetEbxEntry(assetName);
                        if (ebx != null)
                        {
                            isTexture = ebx.Type == "TextureAsset";
                            Texture texture = new Texture(resAssetEntry);
                            textures.Add(texture);
                        }
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.WriteLine($"Failed to load {assetName} from Project with message {ex.Message}");
                }
                assetEntryImporter = null;
            }
        }

        private void ChunkAssetsWrite(NativeWriter nw)
        {
            var modifiedChunkAssets = AssetManager.Instance.EnumerateChunks(modifiedOnly: true);
            // CHUNK Count
            nw.Write(modifiedChunkAssets != null ? modifiedChunkAssets.Count() : 0);
            foreach (var item in modifiedChunkAssets)
            {
                // Item Name
                nw.Write(item.Id);
                nw.Write(item.ModifiedEntry.LogicalOffset);
                nw.Write(item.ModifiedEntry.LogicalSize);
                nw.Write(item.ModifiedEntry.RangeStart);
                nw.Write(item.ModifiedEntry.RangeEnd);
                nw.Write(item.ModifiedEntry.FirstMip);
                nw.Write(item.ModifiedEntry.H32);
                nw.WriteLengthPrefixedString(item.ModifiedEntry.UserData);
                nw.WriteLengthPrefixedBytes(item.ModifiedEntry.Data);
                item.IsDirty = false;
            }
        }


        private void ChunkAssetsRead(NativeReader nr)
        {
            if (ProjectVersion < 2)
                return;

            // CHUNK Count
            var count = nr.ReadInt();
            for (var i = 0; i < count; i++)
            {
                // Item Name
                var assetName = nr.ReadGuid();
                uint logicalOffset = nr.ReadUInt();
                uint logicalSize = nr.ReadUInt();
                uint rangeStart = nr.ReadUInt();
                uint rangeEnd = nr.ReadUInt();
                int firstMip = nr.ReadInt();
                int h = nr.ReadInt();
                var userData = nr.ReadLengthPrefixedString();
                byte[] data = nr.ReadLengthPrefixedBytes();

                if (AssetManager.Instance == null)
                    continue;

                ChunkAssetEntry chunkAssetEntry = AssetManager.GetChunkEntry(assetName);
                if (chunkAssetEntry == null)
                    continue;

                chunkAssetEntry.ModifiedEntry = new ModifiedAssetEntry(data);
                chunkAssetEntry.ModifiedEntry.Sha1 = Sha1.Create(data);
                chunkAssetEntry.ModifiedEntry.LogicalOffset = logicalOffset;
                chunkAssetEntry.ModifiedEntry.LogicalSize = logicalSize;
                chunkAssetEntry.ModifiedEntry.RangeStart = rangeStart;
                chunkAssetEntry.ModifiedEntry.RangeEnd = rangeEnd;
                chunkAssetEntry.ModifiedEntry.FirstMip = firstMip;
                chunkAssetEntry.ModifiedEntry.H32 = h;
                chunkAssetEntry.ModifiedEntry.UserData = userData;
                chunkAssetEntry.ModifiedEntry.Data = data;
            }
        }

        private static void LegacyFilesAddedWrite(NativeWriter nw)
        {
            // -----------------------
            // Added Legacy Files
            nw.WriteLengthPrefixedString("legacy"); // CFC 
            nw.Write(AssetManager.Instance.CustomAssetManagers["legacy"].AddedFileEntries != null ? AssetManager.Instance.CustomAssetManagers["legacy"].AddedFileEntries.Count : 0); // Count Added
            if (AssetManager.Instance.CustomAssetManagers["legacy"].AddedFileEntries != null)
            {
                foreach (var lfe in AssetManager.Instance.CustomAssetManagers["legacy"].AddedFileEntries)
                {
                    nw.WriteLengthPrefixedString(JsonConvert.SerializeObject(lfe));
                }
            }
            
        }

        private static void LegacyFilesAddedRead(NativeReader nr)
        {
            // -----------------------
            // Added Legacy Files
            nr.ReadLengthPrefixedString(); // CFC 
            var countAdded = nr.ReadInt(); // Count Added
            for (var iCount = 0; iCount < countAdded; iCount++)
            {
                nr.ReadLengthPrefixedString();
            }
        }

        private static void LegacyFilesModifiedWrite(NativeWriter nw)
        {
            if (!AssetManager.Instance.CustomAssetManagers.ContainsKey("legacy"))
                return;

            nw.WriteLengthPrefixedString("legacy"); // CFC
            nw.Write(AssetManager.Instance.EnumerateCustomAssets("legacy", modifiedOnly: true).Count()); // Count Added
            foreach (LegacyFileEntry lfe in AssetManager.Instance.EnumerateCustomAssets("legacy", modifiedOnly: true))
            {
                if (lfe.Name != null)
                {
                    var serialisedLFE = JsonConvert.SerializeObject(lfe);
                    nw.WriteLengthPrefixedString(lfe.Name);
                    nw.WriteLengthPrefixedString(serialisedLFE);
                }
            }
        }

        private void LegacyFilesModifiedRead(NativeReader nr)
        {
            // -----------------------
            // Modified Legacy Files
            nr.ReadLengthPrefixedString(); // CFC 
            var count = nr.ReadInt(); // Count Added
            List<LegacyFileEntry> legacyFileEntries = new List<LegacyFileEntry>(count);
            for (var iCount = 0; iCount < count; iCount++)
            {
                var assetName = nr.ReadLengthPrefixedString();
                var assetSerializedData = nr.ReadLengthPrefixedString();
                dynamic lfeD = JsonConvert.DeserializeObject<dynamic>(assetSerializedData, new JsonSerializerSettings()
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

            if (AssetManager.Instance == null)
                return;

            if (!AssetManager.Instance.CustomAssetManagers.ContainsKey("legacy"))
                return;

            var legacyFileManager = AssetManager.Instance.CustomAssetManagers["legacy"];
            legacyFileManager.LoadEntriesModifiedFromProject(legacyFileEntries);
        }

        private static void LocaleINIWrite(NativeWriter nw)
        {
            var hasLocaleIniMod = AssetManager.Instance.LocaleINIMod.HasUserData;
            nw.Write(hasLocaleIniMod);
            if (hasLocaleIniMod)
            {
                nw.Write(FileSystem.Instance.LocaleIsEncrypted);
                nw.WriteLengthPrefixedBytes(AssetManager.Instance.LocaleINIMod.UserData);
            }
        }

        private static void LocaleINIRead(NativeReader nr)
        {
            var hasLocaleIniMod = nr.ReadBoolean();
            if (hasLocaleIniMod)
            {
                nr.ReadBoolean();
                //AssetManager.Instance.LocaleINIMod = new Frostbite.IO.LocaleINIMod(nr.ReadLengthPrefixedBytes());
                AssetManager.Instance.LocaleINIMod.Save(nr.ReadLengthPrefixedBytes());

            }
        }

        private static void EmbeddedFilesWrite(NativeWriter nw)
        {
            nw.Write(AssetManager.Instance.EmbeddedFileEntries.Count > 0);
            nw.Write(AssetManager.Instance.EmbeddedFileEntries.Count);
            foreach (EmbeddedFileEntry efe in AssetManager.Instance.EmbeddedFileEntries)
            {
                var serialisedEFE = JsonConvert.SerializeObject(efe);
                nw.WriteLengthPrefixedString(serialisedEFE);
            }
        }
        private static void EmbeddedFilesRead(NativeReader nr)
        {
            if (nr.ReadBoolean()) // nw.Write(AssetManager.Instance.EmbeddedFileEntries.Count > 0);
            {
                var embeddedFileCount = nr.ReadInt();
                AssetManager.Instance.EmbeddedFileEntries.Clear();
                for (int iItem = 0; iItem < embeddedFileCount; iItem++)
                {
                    var rawFile = nr.ReadLengthPrefixedString();
                    EmbeddedFileEntry efe = JsonConvert.DeserializeObject<EmbeddedFileEntry>(rawFile);
                    AssetManager.Instance.EmbeddedFileEntries.Add(efe);
                }
            }
        }

        //public override void WriteToMod(string filename, ModSettings overrideSettings = null)
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

        //public override void WriteToFIFAMod(string filename, ModSettings overrideSettings = null)
        //{
        //    if (overrideSettings == null)
        //        overrideSettings = ModSettings;

        //    if (File.Exists(filename))
        //        File.Delete(filename);

        //    using (var fs = new FileStream(filename, FileMode.Create))
        //    {
        //        FIFAModWriter frostyModWriter = new FIFAModWriter(ProfileManager.LoadedProfile.Name, AssetManager, FileSystem.Instance, fs, overrideSettings);
        //        frostyModWriter.WriteProject(this);
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Delete()
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> LoadAsync(string fileName, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                return FMTProject.Read(fileName) != null;
            });
        }

        public override bool Load(in string inFilename)
        {
            Read(inFilename); return true;
        }

        public override bool Load(in Stream inStream)
        {
            Read(inStream); return true;
        }

        public override async Task<bool> SaveAsync(string overrideFilename, bool updateDirtyState)
        {
            return await Task.Run(() =>
            {
                return Update() != null;
            });
        }
    }
}
