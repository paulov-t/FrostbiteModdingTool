using FMT.FileTools;
using FMT.Logging;
using FrostbiteSdk.Extras;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Frostbite.Compilers;
using FrostySdk.Frostbite.IO;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using ModdingSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ModdingSupport.ModExecutor;

namespace Madden21Plugin
{

    /// <summary>
    /// Currently. The Madden 21 Compiler does not work in game.
    /// </summary>
    public class Madden21AssetCompiler : Frostbite2022AssetCompiler, IAssetCompiler
    {
        /// <summary>
        /// Construct the Modded Bundles within CAS files
        /// </summary>
        /// <returns>cas to Tuple of Sha1, Name, Type, IsAdded</returns>
        private void ProcessLegacyFiles()
        {
            // Handle Legacy first to generate modified chunks
            if (ModExecuter.modifiedLegacy.Count > 0)
            {
                CacheManager buildCache = new CacheManager();
                buildCache.LoadData(ProfileManager.ProfileName, ModExecuter.GamePath, ModExecuter.Logger, false, true); ;

                ModExecuter.Logger.Log($"Legacy :: {ModExecuter.modifiedLegacy.Count} Legacy files found. Modifying associated chunks");

                Dictionary<string, byte[]> legacyData = new Dictionary<string, byte[]>();
                var countLegacyChunksModified = 0;
                foreach (var modLegacy in ModExecuter.modifiedLegacy)
                {
                    var originalEntry = AssetManager.Instance.GetCustomAssetEntry("legacy", modLegacy.Key);
                    var data = ModExecuter.archiveData[modLegacy.Value.Sha1].Data;
                    legacyData.Add(modLegacy.Key, data);

                }

                AssetManager.Instance.ModifyLegacyAssets(legacyData, true);

                var modifiedLegacyChunks = AssetManager.Instance.EnumerateChunks(true);
                foreach (var modLegChunk in modifiedLegacyChunks)
                {
                    modLegChunk.Sha1 = modLegChunk.ModifiedEntry.Sha1;
                    if (!ModExecuter.ModifiedChunks.ContainsKey(modLegChunk.Id))
                    {
                        ModExecuter.ModifiedChunks.Add(modLegChunk.Id, modLegChunk);
                    }
                    else
                    {
                        ModExecuter.ModifiedChunks[modLegChunk.Id] = modLegChunk;
                    }
                    countLegacyChunksModified++;
                }

                var modifiedChunks = AssetManager.Instance.EnumerateChunks(true);
                foreach (var chunk in modifiedChunks)
                {
                    if (ModExecuter.archiveData.ContainsKey(chunk.Sha1))
                        ModExecuter.archiveData[chunk.Sha1] = new ArchiveInfo() { Data = chunk.ModifiedEntry.Data };
                    else
                        ModExecuter.archiveData.TryAdd(chunk.Sha1, new ArchiveInfo() { Data = chunk.ModifiedEntry.Data });
                }
                ModExecuter.Logger.Log($"Legacy :: Modified {countLegacyChunksModified} associated chunks");
            }
            // ------ End of handling Legacy files ---------

        }

        ModExecutor parent => ModExecuter;

        /// <summary>
        /// This is run AFTER the compilation of the fbmod into resource files ready for the Actions to TOC/SB/CAS to be taken
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="logger"></param>
        /// <param name="frostyModExecuter">Frosty Mod Executer object</param>
        /// <returns></returns>
        public bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            ModExecuter = modExecuter;
            // ------------------------------------------------------------------------------------------
            // You will need to change this to ProfilesLibrary.DataVersion if you change the Profile.json DataVersion field
            if (ProfileManager.IsMadden21DataVersion(ProfileManager.Game))
            {
                if (UseModData)
                {
                    Directory.CreateDirectory(fs.BasePath + ModDirectory + "\\Data");
                    Directory.CreateDirectory(fs.BasePath + ModDirectory + "\\Patch");

                    logger.Log($"Copying files from Data to {ModDirectory}/Data");
                    CopyDataFolder(fs.BasePath + "\\Data\\", fs.BasePath + ModDirectory + "\\Data\\", logger);
                    logger.Log($"Copying files from Patch to {ModDirectory}/Patch");
                    CopyDataFolder(fs.BasePath + PatchDirectory, fs.BasePath + ModDirectory + "\\" + PatchDirectory, logger);
                }
                else
                {
                    if (!ModExecuter.GameWasPatched)
                    {
                        ModExecuter.Logger.Log("Same Game Version detected. Using vanilla backups.");

                        MakeTOCOriginals(ModExecuter.GamePath + "\\Data\\");
                        MakeTOCOriginals(ModExecuter.GamePath + "\\Patch\\");

                    }
                    else
                    {
                        ModExecuter.Logger.Log("Game was patched. Creating backups.");

                        MakeTOCBackups(ModExecuter.GamePath + "\\Data\\");
                        MakeTOCBackups(ModExecuter.GamePath + "\\Patch\\");
                    }
                }

                //BuildCache buildCache = new BuildCache();
                //buildCache.LoadData(ProfilesLibrary.ProfileName, parent.GamePath, parent.Logger, false, true); ;

                ModExecuter.Logger.Log("Enumerating modified bundles.");

                ProcessLegacyFiles();
                ProcessBundles();

                return true;
            }
            return false;
        }

        private class BundleFileEntry
        {
            public int CasIndex;

            public int Offset;

            public int Size;

            public BundleFileEntry(int inCasIndex, int inOffset, int inSize)
            {
                CasIndex = inCasIndex;
                Offset = inOffset;
                Size = inSize;
            }
        }

        private static readonly object locker = new object();

        public static int CasFileCount = 0;

        private Dictionary<int, string> casFiles = new Dictionary<int, string>();

        public Dictionary<int, string> CasFiles => casFiles;


        Catalog catalogInfo;
        public void ProcessBundles()
        {
            //foreach (Catalog ci in AssetManager.Instance.fs.EnumerateCatalogInfos())
            foreach (Catalog ci in FileSystem.Instance.EnumerateCatalogInfos())
            {
                catalogInfo = ci;

                NativeWriter writer_new_cas_file = null;
                int casFileIndex = 0;
                byte[] key_2_from_key_manager = KeyManager.Instance.GetKey("Key2");
                foreach (string key3 in catalogInfo.SuperBundles.Keys)
                {
                    string arg = key3;
                    if (catalogInfo.SuperBundles[key3])
                    {
                        arg = key3.Replace("win32", catalogInfo.Name);
                    }
                    string location_toc_file = ModExecuter.fs.ResolvePath($"{arg}.toc").ToLower();
                    if (location_toc_file != "")
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        uint orig_toc_file_num1 = 0u;
                        uint tocchunkposition = 0u;
                        byte[] byte_array_of_original_toc_file = null;

                        if (!ModExecutor.UseModData)
                        {
                            if (File.Exists(location_toc_file + ".bak"))
                                File.Copy(location_toc_file + ".bak", location_toc_file, true);

                            if (!File.Exists(location_toc_file + ".bak") || ModExecuter.GameWasPatched)
                                File.Copy(location_toc_file, location_toc_file + ".bak");
                        }

                        using (NativeReader reader_original_toc_file = new DeobfuscatedReader(new FileStream(location_toc_file, FileMode.Open, FileAccess.Read), ModExecuter.fs.CreateDeobfuscator()))
                        {
                            uint orig_toc_file_num = reader_original_toc_file.ReadUInt();
                            orig_toc_file_num1 = reader_original_toc_file.ReadUInt();
                            tocchunkposition = reader_original_toc_file.ReadUInt();
                            byte_array_of_original_toc_file = reader_original_toc_file.ReadToEnd();
                        }
                        string location_toc_file_mod_data = location_toc_file.Replace("\\\\", "\\").Replace("patch\\win32", $"{ModDirectory}\\patch\\win32", StringComparison.OrdinalIgnoreCase);

                        FileInfo fi_toc_file_mod_data = new FileInfo(location_toc_file_mod_data);
                        if (!Directory.Exists(fi_toc_file_mod_data.DirectoryName))
                        {
                            Directory.CreateDirectory(fi_toc_file_mod_data.DirectoryName);
                        }
                        using (NativeWriter writer_new_toc_file_mod_data = new NativeWriter(new FileStream(location_toc_file_mod_data, FileMode.OpenOrCreate)))
                        {
                            writer_new_toc_file_mod_data.Write(30331136);
                            writer_new_toc_file_mod_data.Write(new byte[552]);
                            long position = writer_new_toc_file_mod_data.BaseStream.Position;
                            writer_new_toc_file_mod_data.Write(3280507699u);
                            long newBundlePosition = 4294967295L;
                            long newTocChunkPosition = 4294967295L;
                            if (byte_array_of_original_toc_file.Length != 0)
                            {
                                writer_new_toc_file_mod_data.Write(3735928559u);
                                writer_new_toc_file_mod_data.Write(3735928559u);
                                using (NativeReader reader_of_original_toc_file_array = new NativeReader(new MemoryStream(byte_array_of_original_toc_file)))
                                {
                                    if (orig_toc_file_num1 != uint.MaxValue)
                                    {
                                        reader_of_original_toc_file_array.Position = orig_toc_file_num1 - 12;
                                        int bundleCount = reader_of_original_toc_file_array.ReadInt();
                                        List<int> list = new List<int>();
                                        for (int i = 0; i < bundleCount; i++)
                                        {
                                            list.Add(reader_of_original_toc_file_array.ReadInt());
                                        }
                                        List<int> list2 = new List<int>();
                                        for (int j = 0; j < bundleCount; j++)
                                        {
                                            int num7 = reader_of_original_toc_file_array.ReadInt() - 12;
                                            long position2 = reader_of_original_toc_file_array.Position;
                                            reader_of_original_toc_file_array.Position = num7;
                                            int num8 = reader_of_original_toc_file_array.ReadInt() - 1;
                                            List<BundleFileEntry> listOfBundleFileEntries = new List<BundleFileEntry>();
                                            int num9;
                                            do
                                            {
                                                num9 = reader_of_original_toc_file_array.ReadInt();
                                                int inOffset = reader_of_original_toc_file_array.ReadInt();
                                                int inSize = reader_of_original_toc_file_array.ReadInt();
                                                listOfBundleFileEntries.Add(new BundleFileEntry(num9 & int.MaxValue, inOffset, inSize));
                                            }
                                            while ((num9 & 2147483648u) != 0L);
                                            reader_of_original_toc_file_array.Position = num8 - 12;
                                            int num10 = 0;
                                            string bundleName = "";
                                            do
                                            {
                                                string str = reader_of_original_toc_file_array.ReadNullTerminatedString();
                                                num10 = reader_of_original_toc_file_array.ReadInt() - 1;
                                                bundleName = Utils.ReverseString(str) + bundleName;
                                                if (num10 != -1)
                                                {
                                                    reader_of_original_toc_file_array.Position = num10 - 12;
                                                }
                                            }
                                            while (num10 != -1);
                                            reader_of_original_toc_file_array.Position = position2;
                                            int bundleKey = Fnv1.HashString(bundleName.ToLower());
                                            if (ModExecuter.modifiedBundles.ContainsKey(bundleKey))
                                            {
                                                ModExecuter.Logger.Log("Modifying Bundle: " + bundleName);

                                                ModBundleInfo modBundleInfo = ModExecuter.modifiedBundles[bundleKey];
                                                //MemoryStream memoryStream = new MemoryStream();
                                                MemoryUtils memoryUtils = new MemoryUtils();
                                                foreach (BundleFileEntry item in listOfBundleFileEntries)
                                                {
                                                    using (NativeReader nativeReader3 = new NativeReader(new FileStream(ModExecuter.fs.ResolvePath(ModExecuter.fs.GetFilePath(item.CasIndex)), FileMode.Open, FileAccess.Read)))
                                                    {
                                                        nativeReader3.Position = item.Offset;
                                                        //memoryStream.Write(nativeReader3.ReadBytes(item.Size), 0, item.Size);
                                                        memoryUtils.Write(nativeReader3.ReadBytes(item.Size));
                                                    }
                                                }
                                                DbObject dbObject = null;
                                                //using (BinarySbReaderV2 binarySbReader = new BinarySbReaderV2(memoryStream, 0L, ModExecuter.fs.CreateDeobfuscator()))
                                                using (BinarySbReaderV2 binarySbReader = new BinarySbReaderV2(memoryUtils.GetMemoryStream(), 0L, ModExecuter.fs.CreateDeobfuscator()))
                                                {
                                                    dbObject = binarySbReader.ReadDbObject();
                                                    foreach (DbObject ebxItem in dbObject.GetValue<DbObject>("ebx"))
                                                    {
                                                        ebxItem.GetValue("size", 0);
                                                        long value = ebxItem.GetValue("offset", 0L);
                                                        long num11 = 0L;
                                                        foreach (BundleFileEntry item3 in listOfBundleFileEntries)
                                                        {
                                                            if (value < num11 + item3.Size)
                                                            {
                                                                value -= num11;
                                                                value += item3.Offset;
                                                                ebxItem.SetValue("offset", value);
                                                                ebxItem.SetValue("cas", item3.CasIndex);
                                                                break;
                                                            }
                                                            num11 += item3.Size;
                                                        }
                                                    }
                                                    foreach (DbObject item4 in dbObject.GetValue<DbObject>("res"))
                                                    {
                                                        item4.GetValue("size", 0);
                                                        long value2 = item4.GetValue("offset", 0L);
                                                        long num12 = 0L;
                                                        foreach (BundleFileEntry item5 in listOfBundleFileEntries)
                                                        {
                                                            if (value2 < num12 + item5.Size)
                                                            {
                                                                value2 -= num12;
                                                                value2 += item5.Offset;
                                                                item4.SetValue("offset", value2);
                                                                item4.SetValue("cas", item5.CasIndex);
                                                                break;
                                                            }
                                                            num12 += item5.Size;
                                                        }
                                                    }
                                                    foreach (DbObject item6 in dbObject.GetValue<DbObject>("chunks"))
                                                    {
                                                        item6.GetValue("size", 0);
                                                        long value3 = item6.GetValue("offset", 0L);
                                                        long num13 = 0L;
                                                        foreach (BundleFileEntry item7 in listOfBundleFileEntries)
                                                        {
                                                            if (value3 < num13 + item7.Size)
                                                            {
                                                                value3 -= num13;
                                                                value3 += item7.Offset;
                                                                item6.SetValue("offset", value3);
                                                                item6.SetValue("cas", item7.CasIndex);
                                                                break;
                                                            }
                                                            num13 += item7.Size;
                                                        }
                                                    }
                                                }
                                                foreach (DbObject ebx in dbObject.GetValue<DbObject>("ebx"))
                                                {
                                                    var ebxName = ebx.GetValue<string>("name");

                                                    int num14 = modBundleInfo.Modify.Ebx.FindIndex((string a) => a.Equals(ebx.GetValue<string>("name")));
                                                    if (num14 != -1)
                                                    //if (ModExecuter.modifiedEbx.ContainsKey(ebxName))
                                                    {
                                                        //EbxAssetEntry ebxAssetEntry = parent.modifiedEbx[modBundleInfo.Modify.Ebx[num14]];
                                                        EbxAssetEntry ebxAssetEntry = ModExecuter.modifiedEbx[ebxName];
                                                        //if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + ModExecuter.archiveData[ebxAssetEntry.Sha1].Data.Length > 1073741824)
                                                        if (writer_new_cas_file == null)
                                                        {
                                                            writer_new_cas_file?.Close();
                                                            writer_new_cas_file = GetNextCas(out casFileIndex);
                                                        }
                                                        ebx.SetValue("originalSize", ebxAssetEntry.OriginalSize);
                                                        //ebx.SetValue("size", ebxAssetEntry.Size);
                                                        ebx.SetValue("size", ModExecuter.archiveData[ebxAssetEntry.Sha1].Data.Length);
                                                        ebx.SetValue("cas", casFileIndex);
                                                        ebx.SetValue("offset", (int)writer_new_cas_file.BaseStream.Position);
                                                        var ebxData = ModExecuter.archiveData[ebxAssetEntry.Sha1].Data;

                                                        writer_new_cas_file.Write(ebxData);
                                                    }
                                                }
                                                foreach (DbObject res in dbObject.GetValue<DbObject>("res"))
                                                {
                                                    var resName = res.GetValue<string>("name");
                                                    int num14 = modBundleInfo.Modify.Res.FindIndex((string a) => a.Equals(res.GetValue<string>("name")));
                                                    if (num14 != -1)
                                                    {
                                                        //if (ModExecuter.modifiedRes.ContainsKey(resName))
                                                        //{
                                                        //ResAssetEntry resAssetEntry = parent.modifiedRes[modBundleInfo.Modify.Res[num15]];
                                                        ResAssetEntry resAssetEntry = ModExecuter.modifiedRes[resName];
                                                        //if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + ModExecuter.archiveData[resAssetEntry.Sha1].Data.Length > 1073741824)
                                                        if (writer_new_cas_file == null)
                                                        {
                                                            writer_new_cas_file?.Close();
                                                            writer_new_cas_file = GetNextCas(out casFileIndex);
                                                        }
                                                        res.SetValue("originalSize", resAssetEntry.OriginalSize);
                                                        res.SetValue("size", ModExecuter.archiveData[resAssetEntry.Sha1].Data.Length);
                                                        //res.SetValue("size", resAssetEntry.Size);
                                                        res.SetValue("cas", casFileIndex);
                                                        res.SetValue("offset", (uint)writer_new_cas_file.BaseStream.Position);
                                                        res.SetValue("resRid", resAssetEntry.ResRid);
                                                        res.SetValue("resMeta", resAssetEntry.ResMeta);
                                                        res.SetValue("resType", resAssetEntry.ResType);
                                                        writer_new_cas_file.Write(ModExecuter.archiveData[resAssetEntry.Sha1].Data);
                                                    }
                                                }
                                                //foreach (DbObject chunk in dbObject.GetValue<DbObject>("chunks"))
                                                //{
                                                //    int num16 = modBundleInfo.Modify.Chunks.FindIndex((Guid a) => a == chunk.GetValue<Guid>("id"));
                                                //    if (num16 != -1)
                                                //    {
                                                //        ChunkAssetEntry chunkAssetEntry = parent.modifiedChunks[modBundleInfo.Modify.Chunks[num16]];
                                                //        if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + parent.archiveData[chunkAssetEntry.Sha1].Data.Length > 1073741824)
                                                //        {
                                                //            writer_new_cas_file?.Close();
                                                //            writer_new_cas_file = GetNextCas(out casFileIndex);
                                                //        }
                                                //        chunk.SetValue("originalSize", chunkAssetEntry.OriginalSize);
                                                //        //chunk.SetValue("data", parent.archiveData[chunkAssetEntry.Sha1].Data);
                                                //        chunk.SetValue("size", parent.archiveData[chunkAssetEntry.Sha1].Data.Length);
                                                //        //chunk.SetValue("size", chunkAssetEntry.Size);
                                                //        chunk.SetValue("cas", casFileIndex);
                                                //        chunk.SetValue("offset", (int)writer_new_cas_file.BaseStream.Position);
                                                //        //chunk.SetValue("logicalOffset", chunkAssetEntry.LogicalOffset);
                                                //        //chunk.SetValue("logicalSize", chunkAssetEntry.LogicalSize);
                                                //        writer_new_cas_file.Write(parent.archiveData[chunkAssetEntry.Sha1].Data);
                                                //    }
                                                //}
                                                foreach (DbObject chunk in dbObject.GetValue<DbObject>("chunks"))
                                                {
                                                    var chunkId = chunk.GetValue<Guid>("id");
                                                    int num16 = modBundleInfo.Modify.Chunks.FindIndex((Guid a) => a == chunk.GetValue<Guid>("id"));
                                                    if (num16 != -1)
                                                    {
                                                        //if (ModExecuter.ModifiedChunks.ContainsKey(chunkId))
                                                        //{
                                                        ChunkAssetEntry chunkAssetEntry = parent.ModifiedChunks[modBundleInfo.Modify.Chunks[num16]];  // ModExecuter.ModifiedChunks[chunkId];
                                                        //if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + ModExecuter.archiveData[chunkAssetEntry.Sha1].Data.Length > 1073741824)
                                                        if (writer_new_cas_file == null)
                                                        {
                                                            writer_new_cas_file?.Close();
                                                            writer_new_cas_file = GetNextCas(out casFileIndex);
                                                        }
                                                        chunk.SetValue("originalSize", chunkAssetEntry.OriginalSize);
                                                        //chunk.SetValue("data", parent.archiveData[chunkAssetEntry.Sha1].Data);
                                                        chunk.SetValue("size", ModExecuter.archiveData[chunkAssetEntry.Sha1].Data.Length);
                                                        //chunk.SetValue("size", chunkAssetEntry.Size);
                                                        chunk.SetValue("cas", casFileIndex);
                                                        chunk.SetValue("offset", (int)writer_new_cas_file.BaseStream.Position);
                                                        chunk.SetValue("logicalOffset", chunkAssetEntry.LogicalOffset);
                                                        chunk.SetValue("logicalSize", chunkAssetEntry.LogicalSize);

                                                        //chunk.SetValue("logicalOffset", 0);
                                                        //chunk.SetValue("logicalSize", 0);

                                                        writer_new_cas_file.Write(ModExecuter.archiveData[chunkAssetEntry.Sha1].Data);
                                                    }
                                                    //}
                                                }
                                                // --------------- //
                                                // Gets the header //
                                                BundleFileEntry bundleFileEntry = listOfBundleFileEntries[0];
                                                listOfBundleFileEntries.Clear();
                                                // Re-Add just the header
                                                listOfBundleFileEntries.Add(bundleFileEntry);

                                                // ---------------------------------------------------------------------
                                                // Recreate the rest of the bundles
                                                foreach (DbObject dboEbx in dbObject.GetValue<DbObject>("ebx"))
                                                {
                                                    var newBfe = new BundleFileEntry(dboEbx.GetValue("cas", 0), dboEbx.GetValue("offset", 0), dboEbx.GetValue("size", 0));
                                                    listOfBundleFileEntries.Add(newBfe);
                                                }
                                                foreach (DbObject dboRes in dbObject.GetValue<DbObject>("res"))
                                                {
                                                    var newBfe = new BundleFileEntry(dboRes.GetValue("cas", 0), dboRes.GetValue("offset", 0), dboRes.GetValue("size", 0));
                                                    listOfBundleFileEntries.Add(newBfe);
                                                }
                                                foreach (DbObject dboChunk in dbObject.GetValue<DbObject>("chunks"))
                                                {
                                                    var newBfe = new BundleFileEntry(dboChunk.GetValue("cas", 0), dboChunk.GetValue("offset", 0), dboChunk.GetValue("size", 0));
                                                    listOfBundleFileEntries.Add(newBfe);
                                                }
                                                int countEbx = dbObject.GetValue<DbObject>("ebx").Count;
                                                int countRes = dbObject.GetValue<DbObject>("res").Count;
                                                int countChunks = dbObject.GetValue<DbObject>("chunks").Count;
                                                using (NativeWriter nwCas2 = new NativeWriter(new MemoryStream()))
                                                {
                                                    var bwBytes = new BinarySbWriter_M21().WriteToBytes(dbObject, false);
                                                    nwCas2.WriteBytes(bwBytes);

                                                    if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + nwCas2.BaseStream.Length > 1073741824)
                                                    {
                                                        writer_new_cas_file?.Close();
                                                        writer_new_cas_file = GetNextCas(out casFileIndex);
                                                    }
                                                    bundleFileEntry.CasIndex = casFileIndex;
                                                    bundleFileEntry.Offset = (int)writer_new_cas_file.BaseStream.Position;
                                                    bundleFileEntry.Size = bwBytes.Length;
                                                    writer_new_cas_file.WriteBytes(bwBytes);
                                                }

                                                memoryUtils.Dispose();
                                            }
                                            list2.Add((int)(writer_new_toc_file_mod_data.BaseStream.Position - position));
                                            long unkTocOffset = (writer_new_toc_file_mod_data.BaseStream.Position - position + listOfBundleFileEntries.Count * 3 * 4 + 5);
                                            writer_new_toc_file_mod_data.Write((int)unkTocOffset);
                                            for (int k = 0; k < listOfBundleFileEntries.Count; k++)
                                            {
                                                uint indexFixed = (uint)listOfBundleFileEntries[k].CasIndex;
                                                if (k != listOfBundleFileEntries.Count - 1)
                                                {
                                                    indexFixed = (uint)((int)indexFixed | int.MinValue);
                                                }
                                                writer_new_toc_file_mod_data.Write(indexFixed);
                                                writer_new_toc_file_mod_data.Write(listOfBundleFileEntries[k].Offset);
                                                writer_new_toc_file_mod_data.Write(listOfBundleFileEntries[k].Size);
                                            }
                                            writer_new_toc_file_mod_data.WriteNullTerminatedString(new string(bundleName.Reverse().ToArray()));
                                            writer_new_toc_file_mod_data.Write(0);

                                        }
                                        newBundlePosition = writer_new_toc_file_mod_data.BaseStream.Position - position;
                                        writer_new_toc_file_mod_data.Write(bundleCount);
                                        foreach (int item19 in list)
                                        {
                                            writer_new_toc_file_mod_data.Write(item19);
                                        }
                                        foreach (int item20 in list2)
                                        {
                                            writer_new_toc_file_mod_data.Write(item20);
                                        }
                                    }
                                    // -------------------------------------------------------------
                                    // Handle TOC Chunks
                                    //
                                    if (tocchunkposition != uint.MaxValue)
                                    {
                                        TOCFile tocFile = new TOCFile(location_toc_file);
                                        //tocFile.ReadHeader(null);
                                        //tocFile.ReadTOCChunks(null, false);
                                        tocFile.Read(location_toc_file, AssetManager.Instance, new BinarySbDataHelper(AssetManager.Instance), 0, false, false);

                                        Dictionary<Guid, int> chunkPositions = new Dictionary<Guid, int>();
                                        foreach (ChunkAssetEntry chunk in tocFile.TocChunks)
                                        {
                                            chunkPositions.Add(chunk.Id, Convert.ToInt32(writer_new_toc_file_mod_data.Position - position));
                                            writer_new_toc_file_mod_data.Write(chunk.Id);
                                            if (ModExecuter.ModifiedChunks.ContainsKey(chunk.Id))
                                            {
                                                var modifiedChunk = ModExecuter.ModifiedChunks[chunk.Id];
                                                var modifiedChunkData = ModExecuter.archiveData[modifiedChunk.Sha1].Data;

                                                if (writer_new_cas_file == null)
                                                {
                                                    writer_new_cas_file?.Close();
                                                    writer_new_cas_file = GetNextCas(out casFileIndex);
                                                }
                                                chunk.ExtraData.CasIndex = casFileIndex;

                                                var newOffset = writer_new_cas_file.Position;
                                                writer_new_cas_file.Write(modifiedChunkData);
                                                chunk.ExtraData.DataOffset = (uint)newOffset;
                                                chunk.Size = modifiedChunkData.Length;

                                            }
                                            if (ModExecuter.AddedChunks.ContainsKey(chunk.Id))
                                            {

                                            }
                                            writer_new_toc_file_mod_data.Write(chunk.ExtraData.CasIndex.Value);
                                            writer_new_toc_file_mod_data.Write(chunk.ExtraData.DataOffset);
                                            writer_new_toc_file_mod_data.Write(chunk.Size);
                                        }
                                        newTocChunkPosition = Convert.ToInt32(writer_new_toc_file_mod_data.Position - position);

                                        writer_new_toc_file_mod_data.Write(tocFile.TocChunks.Count);
                                        foreach (int bundleIndex in tocFile.TOCChunkBundleIndexes)
                                        {
                                            writer_new_toc_file_mod_data.Write(bundleIndex);
                                        }
                                        foreach (int chunkPosition in chunkPositions.Values)
                                        {
                                            writer_new_toc_file_mod_data.Write(chunkPosition);
                                        }
                                    }
                                    writer_new_toc_file_mod_data.BaseStream.Position = position + 4;
                                    writer_new_toc_file_mod_data.Write((int)newBundlePosition);
                                    writer_new_toc_file_mod_data.Write((int)newTocChunkPosition);
                                }
                            }
                            else
                            {
                                writer_new_toc_file_mod_data.Write(uint.MaxValue);
                                writer_new_toc_file_mod_data.Write(uint.MaxValue);
                            }
                        }

                        //TOCFile tocFile = new TOCFile();
                        //tocFile.Read(location_toc_file, AssetManager.Instance, new BinarySbDataHelper(AssetManager.Instance), 0);

                    }
                }
                if (writer_new_cas_file != null)
                {
                    writer_new_cas_file?.Close();
                }


            }
        }

        DbObject layoutToc = null;

        private NativeWriter GetNextCas(out int casFileIndex)
        {

            if (layoutToc == null)
            {
                using (DbReader dbReaderOfLayoutTOC = new DbReader(new FileStream(FileSystem.Instance.BasePath + "/Patch/layout.toc", FileMode.Open, FileAccess.Read), FileSystem.Instance.CreateDeobfuscator()))
                {
                    layoutToc = dbReaderOfLayoutTOC.ReadDbObject();
                }
            }
            DbObject installManifest = layoutToc["installManifest"] as DbObject;
            DbObject installChunks = installManifest["installChunks"] as DbObject;
            DbObject installChunk = installChunks.List.FirstOrDefault(x => catalogInfo.Name.Contains(x.ToString())) as DbObject;
            DbObject installChunkFiles = installChunk["files"] as DbObject;
            //DbObject firstCas = installChunkFiles.List.First() as DbObject;
            //casFileIndex = firstCas.GetValue<int>("id");
            //string path = firstCas.GetValue<string>("path");
            DbObject lastCas = installChunkFiles.List.Last() as DbObject;
            casFileIndex = lastCas.GetValue<int>("id");
            string path = lastCas.GetValue<string>("path");

            string text = ModExecuter.fs.BasePath;
            if (path.Contains("/native_data/"))
                text += path.Replace("/native_data/", $"{ModDirectory}\\", StringComparison.OrdinalIgnoreCase);
            if (path.Contains("/native_patch/"))
                text += path.Replace("/native_patch/", $"{ModDirectory}\\", StringComparison.OrdinalIgnoreCase);
            text = text.Replace("/", "\\");

            var nw = new NativeWriter(new FileStream(text, FileMode.OpenOrCreate));
            nw.Position = nw.BaseStream.Length;
            return nw;
        }

        private uint HashString(string strToHash, uint initial = 0u)
        {
            uint num = 2166136261u;
            if (initial != 0)
            {
                num = initial;
            }
            for (int i = 0; i < strToHash.Length; i++)
            {
                num = (strToHash[i] ^ (16777619 * num));
            }
            return num;
        }

        private static uint HashData(byte[] b, uint initial = 0u)
        {
            uint num = (uint)((sbyte)b[0] ^ 0x50C5D1F);
            int num2 = 1;
            if (initial != 0)
            {
                num = initial;
                num2 = 0;
            }
            for (int i = num2; i < b.Length; i++)
            {
                num = (uint)((sbyte)b[i] ^ (int)(16777619 * num));
            }
            return num;
        }



    }
}
