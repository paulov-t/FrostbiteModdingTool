using FMT.FileTools;
using FMT.FileTools.Modding;
using FMT.Logging;
using FrostbiteSdk;
using FrostbiteSdk.FrostbiteSdk.Managers;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.ModsAndProjects.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using v2k4FIFAModding.Frosty;
using Fnv1a = FMT.FileTools.Fnv1a;

namespace FrostySdk
{
    public class FrostbiteModWriter : NativeWriter
    {
        public class Manifest
        {
            private Dictionary<string, int> nameEntries = new Dictionary<string, int>();

            private Dictionary<FMT.FileTools.Sha1, int> sha1Entries = new Dictionary<FMT.FileTools.Sha1, int>();

            private List<object> objects = new List<object>();

            public int Count => objects.Count;

            public int Add(byte[] data)
            {
                objects.Add(data);
                return objects.Count - 1;
            }

            public int Add(FMT.FileTools.Sha1 sha1, byte[] data)
            {
                if (sha1Entries.ContainsKey(sha1))
                {
                    return sha1Entries[sha1];
                }
                objects.Add(data);
                sha1Entries.Add(sha1, objects.Count - 1);
                return objects.Count - 1;
            }

            public int Add(string name, byte[] data)
            {
                if (nameEntries.ContainsKey(name))
                {
                    return nameEntries[name];
                }
                objects.Add(data);
                nameEntries.Add(name, objects.Count - 1);
                return objects.Count - 1;
            }

            public void Write(NativeWriter writer)
            {
                long num = writer.BaseStream.Position + objects.Count * 16;
                long num2 = 0L;
                foreach (object @object in objects)
                {
                    writer.Write(num2);
                    long num3 = 0L;
                    long position = writer.BaseStream.Position;
                    writer.BaseStream.Position = num + num2;
                    byte[] array = (byte[])@object;
                    writer.Write(array);
                    num3 = array.Length;
                    writer.BaseStream.Position = position;
                    writer.Write(num3);
                    num2 += num3;
                }
            }
        }

        protected class EmbeddedResource : EditorModResource
        {
            public override ModResourceType Type => ModResourceType.Embedded;

            public EmbeddedResource(string inName, byte[] data, Manifest manifest)
            {
                name = inName;
                if (data != null)
                {
                    resourceIndex = manifest.Add(data);
                    size = data.Length;
                }
            }
        }

        protected class BundleResource : EditorModResource
        {
            private int superBundleName;

            public override ModResourceType Type => (ModResourceType)5;

            public BundleResource(BundleEntry entry, Manifest manifest)
            {
                name = entry.Name.ToLower();
                superBundleName = Fnv1a.HashString(AssetManager.Instance.GetSuperBundle(entry.SuperBundleId).Name.ToLower());
            }

            public override void Write(NativeWriter writer, uint writerVersion = 4)
            {
                base.Write(writer, writerVersion);
                if (writerVersion >= 28u)
                    writer.WriteLengthPrefixedString(name);
                else
                    writer.WriteNullTerminatedString(name);

                writer.Write(superBundleName);
            }
        }

        public class EbxResource : EditorModResource
        {
            private static List<string> _listOfEBXRawFilesToUse = new List<string>();
            public static List<string> ListOfEBXRawFilesToUse
            {
                get
                {
                    if (File.Exists("EBXRawFilesToUse.dat") && _listOfEBXRawFilesToUse.Count == 0)
                    {
                        _listOfEBXRawFilesToUse = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("EBXRawFilesToUse.dat"));
                    }

                    return _listOfEBXRawFilesToUse;
                }
                set
                {
                    _listOfEBXRawFilesToUse = value;

                    if (File.Exists("EBXRawFilesToUse.dat"))
                        File.Delete("EBXRawFilesToUse.dat");


                    File.WriteAllText("EBXRawFilesToUse.dat", JsonConvert.SerializeObject(_listOfEBXRawFilesToUse));

                }
            }

            public override ModResourceType Type => ModResourceType.Ebx;

            public EbxResource(EbxAssetEntry entry, Manifest manifest)
            {
                CompressionType compressionOverride = ProfileManager.GetCompressionType(ProfileManager.CompTypeArea.EBX);

                byte[] decompressedArray = null;
                byte[] compressedArray = null;
                name = entry.Name.ToLower();
                UserData = entry.ModifiedEntry.UserData;

                FileInfo fileInfo = new FileInfo(Path.Combine("EBX", entry.Filename.ToLower() + ".dat"));
                if (fileInfo.Exists)
                {
                    decompressedArray = File.ReadAllBytes(fileInfo.FullName);
                }
                else
                {
                    // -------------------------------------
                    // get via ebx writer
                    try
                    {
                        decompressedArray = EbxBaseWriter.GetEbxArrayDecompressed(entry);
                    }
                    catch(Exception ex) 
                    {
                        FileLogger.WriteLine($"Unable to Write EBX {ex.Message} to Mod:");
                        FileLogger.WriteLine($"{ex.ToString()}");
                    }
                    //
                    // -------------------------------------
                }

                if (decompressedArray == null || decompressedArray.Length == 0)
                    return;

                compressedArray = Utils.CompressFile(decompressedArray, null, ResourceType.Invalid, compressionOverride);

#if DEBUG
                var debuggingFileStreamDirectoryPath = Directory.CreateDirectory(AppContext.BaseDirectory + "\\Debugging\\EBX\\");
                //if (
                //    name.Contains("default/settings")
                //    || name.Contains("head_")
                //    || name.Contains("fifa/attribulator/gameplay/groups/gp_cpuai/gp_cpuai_crossingdecision_runtime")
                //    || name.Contains("fifa/attribulator/gameplay/groups/gp_actor/gp_actor_movement_runtime")
                //    || name.Contains("fifa/attribulator/gameplay/groups/gp_collision/gp_collision_playercollision_runtime")
                //    || name.Contains("splashscreen")
                //    )
                //{
                    //File.WriteAllBytes($"ebx.{entry.Filename.Replace("\\", "_")}.write.dat", decompressedArray);
                    DebugBytesToFileLogger.Instance.WriteAllBytes($"ebx.{entry.Filename.Replace("\\", "_")}.write.dat", decompressedArray, "EBX/Write");
                //}
#endif

                if (compressedArray.Length > 0)
                {
                    size = decompressedArray.LongLength;
                    resourceIndex = manifest.Add(compressedArray);
                    sha1 = entry.Sha1;// FMT.FileTools.Sha1.Create(compressedArray);
                }
                //}
                foreach (int bundle in entry.Bundles)
                {
                    BundleEntry bundleEntry = AssetManager.Instance.GetBundleEntry(bundle);
                    if (bundleEntry != null)
                        AddBundle(bundleEntry.Name, modify: true);
                }
                foreach (int item in entry.EnumerateBundles(addedOnly: true))
                {
                    BundleEntry bundleEntry2 = AssetManager.Instance.GetBundleEntry(item);
                    if (bundleEntry2 != null)
                        AddBundle(bundleEntry2.Name, modify: false);
                }
            }
        }

        protected class ResResource : EditorModResource
        {
            private uint resType;

            private ulong resRid;

            private byte[] resMeta;

            public override ModResourceType Type => ModResourceType.Res;

            public ResResource(ResAssetEntry entry, Manifest manifest)
            {
                name = entry.Name.ToLower();
                sha1 = entry.ModifiedEntry.Sha1;
                resourceIndex = manifest.Add(entry.ModifiedEntry.Sha1, entry.ModifiedEntry.Data);
                size = entry.ModifiedEntry.OriginalSize;
                resType = entry.ResType;
                resRid = entry.ResRid;
                resMeta = ((entry.ModifiedEntry.ResMeta != null) ? entry.ModifiedEntry.ResMeta : entry.ResMeta);
                UserData = entry.ModifiedEntry.UserData;
                flags = (byte)(entry.IsInline ? 1 : 0);
                foreach (int bundle in entry.Bundles)
                {
                    BundleEntry bundleEntry = AssetManager.Instance.GetBundleEntry(bundle);
                    if (bundleEntry != null)
                        AddBundle(bundleEntry.Name, modify: true);
                }
                foreach (int item in entry.EnumerateBundles(addedOnly: true))
                {
                    BundleEntry bundleEntry2 = AssetManager.Instance.GetBundleEntry(item);
                    if (bundleEntry2 != null)
                        AddBundle(bundleEntry2.Name, modify: false);
                }
            }

            public override void Write(NativeWriter writer, uint writerVersion = 4)
            {
                base.Write(writer, writerVersion);
                writer.Write(resType);
                writer.Write(resRid);
                writer.Write(resMeta.Length);
                writer.Write(resMeta);
            }
        }

        protected class ChunkResource : EditorModResource
        {
            private uint rangeStart;

            private uint rangeEnd;

            private uint logicalOffset;

            private uint logicalSize;

            private int h32;

            private int firstMip;

            public override ModResourceType Type => ModResourceType.Chunk;

            public ChunkResource(ChunkAssetEntry entry, Manifest manifest)
            {
                name = entry.Id.ToString();
                sha1 = entry.ModifiedEntry.Sha1;
                resourceIndex = manifest.Add(entry.ModifiedEntry.Sha1, entry.ModifiedEntry.Data);
                size = entry.ModifiedEntry.OriginalSize;
                rangeStart = entry.ModifiedEntry.RangeStart;
                rangeEnd = entry.ModifiedEntry.RangeEnd;
                logicalOffset = entry.ModifiedEntry.LogicalOffset;
                logicalSize = entry.ModifiedEntry.LogicalSize;
                h32 = entry.ModifiedEntry.H32;
                firstMip = entry.ModifiedEntry.FirstMip;
                UserData = entry.ModifiedEntry.UserData;
                flags = (byte)(entry.IsInline ? 1 : 0);
                //flags |= (byte)(entry.ModifiedEntry.AddToChunkBundle ? 2 : 0);
                //if (entry.ModifiedEntry.AddToChunkBundle)
                //{
                //	//if (ProfileManager.MustAddChunks || entry.IsAdded)
                //	//	AddBundle("chunks", modify: false);
                //	//else
                //		AddBundle("chunks", modify: true);
                //}

                foreach (int bundle in entry.Bundles)
                {
                    //bundlesToModify.Add(bundle);
                    BundleEntry bundleEntry = AssetManager.Instance.GetBundleEntry(bundle);
                    if (bundleEntry != null)
                        AddBundle(bundleEntry.Name, modify: true);
                }
                foreach (int bundle in entry.EnumerateBundles(addedOnly: true))
                {
                    //bundlesToAdd.Add(bundle);
                    BundleEntry bundleEntry = AssetManager.Instance.GetBundleEntry(bundle);
                    if (bundleEntry != null)
                        AddBundle(bundleEntry.Name, modify: false);
                }
            }

            public override void Write(NativeWriter writer, uint writerVersion = 4)
            {
                base.Write(writer, writerVersion);
                writer.Write(rangeStart);
                writer.Write(rangeEnd);
                writer.Write(logicalOffset);
                writer.Write(logicalSize);
                writer.Write(h32);
                writer.Write(firstMip);
            }
        }


        protected class LegacyFileResource : EditorModResource
        {
            public override ModResourceType Type => ModResourceType.Legacy;

            public LegacyFileResource(LegacyFileEntry entry, Manifest manifest)
            {
                name = entry.Name;
                size = entry.ModifiedEntry.Size;
                resourceIndex = manifest.Add(entry.Name, entry.ModifiedEntry.Data);
            }

            public override void Write(NativeWriter writer, uint writerVersion = 4)
            {
                base.Write(writer, writerVersion);
                writer.Write(name);
            }
        }

        protected class EmbeddedFileResource : EditorModResource
        {
            public override ModResourceType Type => ModResourceType.EmbeddedFile;

            public EmbeddedFileResource(EmbeddedFileEntry entry, Manifest manifest)
            {
                name = entry.ExportedRelativePath;
                size = entry.Data.Length;
                resourceIndex = manifest.Add(entry.Name, entry.Data);
            }

            public override void Write(NativeWriter writer, uint writerVersion = 4)
            {
                base.Write(writer, writerVersion);
                writer.Write(name);
            }
        }

        private ModSettings overrideSettings;

        protected Manifest manifest = new Manifest();

        protected List<BaseModResource> resources = new List<BaseModResource>();

        public Manifest ResourceManifest => manifest;

        public FrostbiteModWriter(Stream inStream, ModSettings inOverrideSettings = null)
            : base(inStream)
        {
            overrideSettings = inOverrideSettings;
        }

        public virtual void WriteProject(IProject project)
        {
            Write(FrostbiteMod.MagicFMT);
            Write(FrostbiteMod.CurrentVersion);
            Write(16045690984833335023uL);
            Write(3735928559u);
            Write(ProfileManager.ProfileName);
            Write(FileSystem.Instance.Head);
            ModSettings modSettings = overrideSettings;
            if (modSettings == null)
            {
                modSettings = project.ModSettings;
            }
            WriteLengthPrefixedString(modSettings.Title);
            WriteLengthPrefixedString(modSettings.Author);
            WriteLengthPrefixedString(modSettings.Category);
            WriteLengthPrefixedString(modSettings.Version);
            WriteLengthPrefixedString(modSettings.Description);

            // -----------------------------------------------------
            // Embedded Files
            // --------------------------------------------------
            // Convert Locale.Ini mod to EmbeddedFileEntry
            if (AssetManager.Instance.LocaleINIMod.HasUserData)
            {
                AssetManager.Instance.EmbeddedFileEntries.RemoveAll(x
                    => x.ImportedFileLocation.Contains("Locale.ini", StringComparison.OrdinalIgnoreCase)
                    || x.ExportedRelativePath.Contains("Locale.ini", StringComparison.OrdinalIgnoreCase)
                    );
                AssetManager.Instance.EmbeddedFileEntries.Add(new EmbeddedFileEntry()
                {
                    Name = "Locale.ini",
                    ImportedFileLocation = "PROJECT",
                    ExportedRelativePath = "Data\\Locale.ini",
                    Data = AssetManager.Instance.LocaleINIMod.UserDataEncrypted
                });
            }
            // 5 = Icon and Screenshots
            // The count of embedded files is added
            Write(5 + AssetManager.Instance.EmbeddedFileEntries.Count);
            AddResource(new EmbeddedResource("Icon;", modSettings.Icon, manifest));
            for (int i = 0; i < 4; i++)
            {
                AddResource(new EmbeddedResource("Screenshot;" + i.ToString(), modSettings.GetScreenshot(i), manifest));
            }
            for (int i = 0; i < AssetManager.Instance.EmbeddedFileEntries.Count; i++)
            {
                var efe = AssetManager.Instance.EmbeddedFileEntries[i];
                AddResource(new EmbeddedResource("efe;" + efe.ExportedRelativePath, efe.Data, manifest));
            }
            // end of embedded
            // ----------------------------------------------------

            foreach (BundleEntry bundleEntry in AssetManager.Instance.EnumerateBundles(BundleType.None, modifiedOnly: true))
            {
                if (bundleEntry.Added)
                {
                    AddResource(new BundleResource(bundleEntry, manifest));
                }
            }
            foreach (EbxAssetEntry ebxAsset in AssetManager.Instance.EnumerateEbx("", modifiedOnly: true))
            {
                if (!ebxAsset.ModifiedEntry.IsTransientModified && ebxAsset.HasModifiedData)
                {
                    AddResource(new EbxResource(ebxAsset, manifest));
                }
            }
            foreach (ResAssetEntry resAsset in AssetManager.Instance.EnumerateRes(0u, modifiedOnly: true))
            {
                if (resAsset.HasModifiedData)
                {
                    AddResource(new ResResource(resAsset, manifest));
                }
            }
            foreach (ChunkAssetEntry chunkEntry in AssetManager.Instance.EnumerateChunks(modifiedOnly: true))
            {
                if (chunkEntry.HasModifiedData)
                {
                    AddResource(new ChunkResource(chunkEntry, manifest));
                }
            }
            // Write Legacy stuff
            foreach (LegacyFileEntry lfe in AssetManager.Instance.EnumerateCustomAssets("legacy", true))
            {
                if (lfe.HasModifiedData)
                {
                    AddResource(new LegacyFileResource(lfe, manifest));
                }
            }
            // Write Embedded stuff
            foreach (EmbeddedFileEntry efe in AssetManager.Instance.EmbeddedFileEntries)
            {
                AddResource(new EmbeddedFileResource(efe, manifest));
            }
            Write(resources.Count);
            foreach (EditorModResource resource in resources)
            {
                resource.Write(this);
            }


            long manifestDataPosition = BaseStream.Position;
            manifest.Write(this);
            long legacyFilePosition = BaseStream.Position;
            BaseStream.Position = 12L;
            Write(manifestDataPosition);
            Write(manifest.Count);



        }

        public virtual void WriteProject()
        {
            WriteProject(ProjectManagement.Instance != null ? ProjectManagement.Instance.Project : null);
        }

        public void AddResource(BaseModResource resource)
        {
            resources.Add(resource);
        }
    }
}
