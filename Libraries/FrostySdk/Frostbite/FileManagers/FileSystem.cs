using FMT.FileTools;
using FMT.FileTools.Modding;
using FrostySdk.Frostbite.IO;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.ThirdParty;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace FrostySdk
{
    public class FileSystem
    {
        private List<string> paths { get; } = new List<string>();

        private List<string> superBundles { get; } = new List<string>();

        private List<string> splitSuperBundles { get; } = new List<string>();

        private List<Catalog> catalogs { get; } = new List<Catalog>();

        public Dictionary<string, byte[]> memoryFs { get; set; } = new Dictionary<string, byte[]>();

        public Dictionary<string, byte[]> MemoryFileSystem => memoryFs;

        public Dictionary<string, byte[]> MemoryFileSystemModifiedItems { get; set; } = new Dictionary<string, byte[]>();

        private List<string> casFiles { get; } = new List<string>();

        private string basePath;

        private string cacheName;

        private Type deobfuscatorType;

        private uint baseNum;

        private uint headNum;

        /// <summary>
        /// A combination calculation of baseNum + headNum + lastAccessDate of the EXE
        /// </summary>
        public ulong SystemIteration;

        private List<ManifestBundleInfo> manifestBundles { get; } = new List<ManifestBundleInfo>();

        private List<ManifestChunkInfo> manifestChunks { get; } = new List<ManifestChunkInfo>();

        public int SuperBundleCount => superBundles.Count;

        public IEnumerable<string> SuperBundles
        {
            get
            {
                for (int i = 0; i < superBundles.Count; i++)
                {
                    yield return superBundles[i];
                }
            }
        }

        public IEnumerable<string> SplitSuperBundles
        {
            get
            {
                for (int i = 0; i < splitSuperBundles.Count; i++)
                {
                    yield return splitSuperBundles[i];
                }
            }
        }

        public int CatalogCount => catalogs.Count;

        public IEnumerable<string> Catalogs
        {
            get
            {
                for (int i = 0; i < catalogs.Count; i++)
                {
                    yield return catalogs[i].Name;
                }
            }
        }
        public IEnumerable<Catalog> CatalogObjects
        {
            get
            {
                for (int i = 0; i < catalogs.Count; i++)
                {
                    yield return catalogs[i];
                }
            }
        }

        public int CasFileCount => casFiles.Count;

        public uint Base => baseNum;

        public uint Head => headNum;

        public string CacheName => cacheName;

        public string BasePath => basePath;

        public static FileSystem Instance;

        public string LocaleIniPath => ResolvePath("native_data/locale.ini");

        public LiveTuningUpdate LiveTuningUpdate { get; } = new LiveTuningUpdate();

        public FileSystem(string inBasePath)
        {
            if (Instance != null)
                throw new Exception("FileSystem Instance already exists");

            if (string.IsNullOrEmpty(inBasePath))
                throw new Exception("Base Path is empty!");

            if (!Directory.Exists(inBasePath))
                throw new DirectoryNotFoundException(inBasePath + " doesn't exist");

            if (inBasePath.EndsWith(@"\"))
                inBasePath = inBasePath.Substring(0, inBasePath.Length - 1);

            basePath = inBasePath;
            if (!basePath.EndsWith(@"\") && !basePath.EndsWith("/"))
            {
                basePath += @"\";
            }
            cacheName = ProfileManager.CacheName;
            deobfuscatorType = ProfileManager.Deobfuscator;

            Instance = this;
            LiveTuningUpdate.ReadFIFALiveTuningUpdate();

            if (paths.Count == 0)
            {
                bool patched = false;
                foreach (FileSystemSource source in ProfileManager.Sources)
                {
                    FileSystem.Instance.AddSource(source.Path, source.SubDirs);
                    if (source.Path.ToLower().Contains("patch"))
                        patched = true;
                }

                Initialize(KeyManager.Instance.GetKey("Key1"), patched);
            }
        }



        public byte[] LoadKey()
        {
            KeyManager.ReadInKeys();
            byte[] key = KeyManager.Instance.GetKey("Key1");
            return key;
            
        }

        /// <summary>
        /// Needs to first 16 bytes of the key
        /// </summary>
        /// <param name="key"></param>
        public void Initialize(byte[] key = null, bool patched = true)
        {
            ProcessLayouts();

            if (key == null)
                key = LoadKey();

            ReadInitfs(key, patched);

            ZStd.Bind();
            Oodle.Bind(BasePath);

            LoadCatalogs();
        }

        public Dictionary<Sha1, CatResourceEntry> CatResourceEntries { get; } = new Dictionary<Sha1, CatResourceEntry>();
        public Dictionary<Sha1, CatPatchEntry> CatPatchEntries { get; } = new Dictionary<Sha1, CatPatchEntry>();
        public Dictionary<int, string> CasFiles { get; } = new Dictionary<int, string>();

        /// <summary>
        /// Attempts to load in .CAT files
        /// </summary>
        private void LoadCatalogs()
        {
            foreach (string catalogName in Catalogs)
            {
                LoadCatalog("native_data/" + catalogName + "/cas.cat");
                LoadCatalog("native_patch/" + catalogName + "/cas.cat");
            }
        }

        /// <summary>
        /// Attempts to load in .CAT files (if they exist)
        /// </summary>
        /// <param name="filename"></param>
        private void LoadCatalog(string filename)
        {
            string fullPath = ResolvePath(filename);
            if (!File.Exists(fullPath))
            {
                return;
            }

            using (CatReader reader = new CatReader(new FileStream(fullPath, FileMode.Open, FileAccess.Read), CreateDeobfuscator()))
            {
                for (int i = 0; i < reader.ResourceCount; i++)
                {
                    CatResourceEntry entry = reader.ReadResourceEntry();
                    entry.ArchiveIndex = AddCas(filename, entry.ArchiveIndex);

                    if (entry.LogicalOffset == 0 && !CatResourceEntries.ContainsKey(entry.Sha1))
                    {
                        CatResourceEntries.Add(entry.Sha1, entry);
                    }
                }

                for (int i = 0; i < reader.EncryptedCount; i++)
                {
                    CatResourceEntry entry = reader.ReadEncryptedEntry();
                    entry.ArchiveIndex = AddCas(filename, entry.ArchiveIndex);

                    if (entry.LogicalOffset == 0 && !CatResourceEntries.ContainsKey(entry.Sha1))
                    {
                        CatResourceEntries.Add(entry.Sha1, entry);
                    }
                }

                for (int i = 0; i < reader.PatchCount; i++)
                {
                    CatPatchEntry entry = reader.ReadPatchEntry();
                    if (!CatPatchEntries.ContainsKey(entry.Sha1))
                    {
                        CatPatchEntries.Add(entry.Sha1, entry);
                    }
                }
            }
        }

        private int AddCas(string catPath, int archiveIndex)
        {
            string casFilename = catPath.Substring(0, catPath.Length - 7) + "cas_" + archiveIndex.ToString("d2") + ".cas";
            int hash = Fnv1.HashString(casFilename);

            if (!CasFiles.ContainsKey(hash))
            {
                CasFiles.Add(hash, ResolvePath(casFilename));
            }

            return hash;
        }

        public IDeobfuscator CreateDeobfuscator()
        {
            return (IDeobfuscator)Activator.CreateInstance(deobfuscatorType);
        }

        public void AddSource(string path, bool iterateSubPaths = false)
        {
            if (Directory.Exists(basePath + path))
            {
                if (iterateSubPaths)
                {
                    foreach (string item in Directory.EnumerateDirectories(basePath + path, "*", SearchOption.AllDirectories))
                    {
                        if (!item.ToLower().Contains("\\patch") && File.Exists(item + "\\package.mft"))
                        {
                            //paths.Add("\\" + item.Replace(basePath, "").ToLower() + "\\data\\");

                            string addPath = !basePath.EndsWith(@"\") ? @"\" + path.ToLower() + @"\" : path.ToLower() + @"\";
                            paths.Add(addPath);
                        }
                    }
                }
                else
                {
                    string addPath = @"\" + path.ToLower() + @"\";
                    paths.Add(addPath);
                }
            }
            else
            {
                Debug.WriteLine(basePath + path + " doesn't exist");
                throw new DirectoryNotFoundException(basePath + path + " doesn't exist");
            }
        }

        /// <summary>
        /// Resolves native_data or native_patch into the correct path
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="checkModData"></param>
        /// <returns></returns>
        public string ResolvePath(string filename, bool checkModData = false, bool created = false)
        {
            filename = filename.Trim('/');
            filename = filename.Replace("/", "\\");
            filename = filename.Replace("_debug_", "", StringComparison.OrdinalIgnoreCase); // BF4

            if (filename.StartsWith("win32"))
            {
                filename = filename.Replace("win32", "native_patch\\win32");
            }

            var resolvedPath = string.Empty;
            if (!filename.Contains("native_data") && !filename.Contains("native_patch"))
                throw new ArgumentOutOfRangeException("Incorrect input filename given, expecting native_data or native patch but got " + filename);

            if (filename.Contains("native_data"))
                resolvedPath = basePath + (checkModData ? "ModData\\" : "") + filename.Replace("native_data", "Data\\");

            if (filename.Contains("native_patch"))
                resolvedPath = basePath + (checkModData ? "ModData\\" : "") + filename.Replace("native_patch", "Patch\\");

            if (
                (ProfileManager.IsBF4DataVersion() || ProfileManager.IsGameVersion(EGame.FIFA17))
                && !Directory.Exists(Directory.GetParent(resolvedPath).FullName) && filename.Contains("native_patch"))
            {
                resolvedPath = basePath + (checkModData ? "ModData\\" : "") + filename.Replace("native_patch", "Update\\Patch\\Data\\");
            }

            if (!File.Exists(resolvedPath) && !created)
                return string.Empty;

            return resolvedPath;
        }

        public IEnumerable<string> ResolvePaths(string filename)
        {
            //Debug.WriteLine(JsonConvert.SerializeObject(paths));
            if (filename.StartsWith("native_patch/") && paths.Count == 1)
            {
                yield return "";
            }
            int num = 0;
            int num2 = paths.Count;
            if (filename.StartsWith("native_data/") && paths.Count > 1)
            {
                num = 1;
            }
            else if (filename.StartsWith("native_patch/"))
            {
                num2 = 1;
            }
            filename = filename.Replace("native_data/", "");
            filename = filename.Replace("native_patch/", "");
            filename = filename.Trim('/');
            if (paths.Count > 0)
            {
                for (int i = num; i < num2; i++)
                {
                    if (File.Exists(basePath + paths[i] + filename) || Directory.Exists(basePath + paths[i] + filename))
                    {
                        yield return basePath + paths[i] + filename;
                    }
                }
            }
            yield return "";
        }

        public string ResolvePath(ManifestFileRef fileRef)
        {
            string filename = (fileRef.IsInPatch ? "native_patch/" : "native_data/") + catalogs[fileRef.CatalogIndex].Name + "/cas_" + fileRef.CasIndex.ToString("D2") + ".cas";
            if (string.IsNullOrEmpty(ResolvePath(filename)))
                filename = "native_data/" + catalogs[fileRef.CatalogIndex].Name + "/cas_" + fileRef.CasIndex.ToString("D2") + ".cas";

            return ResolvePath(filename);
        }

        public string GetCatalogFromSuperBundle(string sbName)
        {
            foreach (Catalog catalog in catalogs)
            {
                if (catalog.SuperBundles.ContainsKey(sbName))
                {
                    return catalog.Name;
                }
            }

            foreach (Catalog catalog3 in catalogs)
            {
                if (catalog3.SuperBundles.Count != 0)
                {
                    return catalog3.Name;
                }
            }
            return catalogs[0].Name;
        }

        public Catalog GetCatalogObjectFromSuperBundle(string superBundle)
        {
            if (superBundle == null)
            {
                throw new ArgumentNullException("superBundle");
            }
            foreach (Catalog catalog2 in CatalogObjects)
            {
                if (catalog2.HasSuperBundle(superBundle))
                {
                    return catalog2;
                }
            }
            foreach (Catalog catalog in CatalogObjects)
            {
                if (catalog.SuperBundles.Any())
                {
                    return catalog;
                }
            }
            return catalogs[0];
        }

        public string GetCatalog(ManifestFileRef fileRef)
        {
            return catalogs[fileRef.CatalogIndex].Name;
        }

        public IEnumerable<Catalog> EnumerateCatalogInfos()
        {
            foreach (Catalog catalog in catalogs)
            {
                yield return catalog;
            }
        }

        public ManifestFileRef GetFileRef(string path)
        {
            path = path.Replace(BasePath, "");
            foreach (string path2 in paths)
            {
                path = path.Replace(path2, "");
            }
            if (path.EndsWith("cat"))
            {
                path = path.Remove(path.Length - 8);
            }
            else if (path.EndsWith("cas"))
            {
                path = path.Remove(path.Length - 11);
            }
            foreach (Catalog catalog in catalogs)
            {
                if (catalog.Name.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return new ManifestFileRef(catalogs.IndexOf(catalog), inPatch: false, 0);
                }
            }
            return default(ManifestFileRef);
        }

        public bool HasFileInMemoryFs(string name)
        {
            return memoryFs.ContainsKey(name);
        }

        public byte[] GetFileFromMemoryFs(string name)
        {
            if (!memoryFs.ContainsKey(name))
            {
                return null;
            }
            return memoryFs[name];
        }

        public string GetFilePath(int index)
        {
            if (index >= 0 && index < casFiles.Count)
            {
                return casFiles[index];
            }

            return "";
        }

        public string GetCasFilePathFromIndex(int index)
        {
            return GetFilePath(index);
        }

        public string GetFilePath(int catalog, int cas, bool patch)
        {
            Catalog catalogInfo = catalogs[catalog];
            var result = (patch ? "native_patch/" : "native_data/") + catalogInfo.Name + "/cas_" + cas.ToString("D2") + ".cas";
            if (new ReadOnlySpan<char>(ResolvePath(result).ToArray()).IsEmpty)
            {

            }
            return result;
        }
        public string GetCasFilePath(int catalog, int cas, bool patch)
        {
            return GetFilePath(catalog, cas, patch);
        }

        byte[] lKeyABC = new byte[] { 0x27, 0x0E, 0xCC, 0xA9, 0x96, 0x7E, 0x96, 0xBA, 0x35, 0x7E, 0x90, 0x90, 0xE6, 0x29, 0x9D, 0x36, 0x9D, 0xF8, 0x42, 0xA3, 0x3E, 0xBB, 0x08, 0xFB, 0x67, 0x85, 0x07, 0xA7, 0x80, 0x0A, 0xBA, 0x11, 0xA0, 0x51, 0x02, 0xF5, 0x40, 0xE4, 0x12, 0x91, 0x27, 0x89, 0x3D, 0x15, 0xF4, 0x50, 0x7A, 0x8E };
        public bool LocaleIsEncrypted;

        public byte[] ReadLocaleIni()
        {
            if (!string.IsNullOrEmpty(LocaleIniPath))
            {
                var data = File.ReadAllBytes(LocaleIniPath);
                if (!File.ReadAllText(LocaleIniPath).StartsWith("[LOCALE]"))
                {
                    if (File.Exists(LocaleIniPath + ".bak"))
                    {
                        Debug.WriteLine("ReadLocaleIni: Found backup Encrypted Locale.ini. Reading that.");
                        data = File.ReadAllBytes(LocaleIniPath + ".bak");
                    }
                    LocaleIsEncrypted = true;

                    var key1 = KeyManager.Instance.GetKey("Key1");
                    var key2 = KeyManager.Instance.GetKey("Key2");
                    var key3 = KeyManager.Instance.GetKey("Key3");
                    var key = lKeyABC;
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = key.AsSpan(0, 0x20).ToArray();
                        aes.IV = key.AsSpan(32, 16).ToArray();
                        ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
                        MemoryStream msDecrypted = new MemoryStream();
                        using (MemoryStream stream = new MemoryStream(data))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read))
                            {
                                //cryptoStream.Read(data, 0, data.Length);
                                cryptoStream.CopyTo(msDecrypted);
                            }
                        }
                        data = msDecrypted.ToArray();

                        if (File.Exists("locale_decrypt.ini"))
                            File.Delete("locale_decrypt.ini");

                        File.WriteAllBytes("locale_decrypt.ini", data);
                    }
                }

                return data;
            }

            return null;
        }

        public byte[] WriteLocaleIni(byte[] data, bool writeToFile = false)
        {
            if (!string.IsNullOrEmpty(LocaleIniPath))
            {
                if (!File.Exists(LocaleIniPath + ".bak"))
                    File.Copy(LocaleIniPath, LocaleIniPath + ".bak");

                if (!File.ReadAllText(LocaleIniPath).StartsWith("[LOCALE]"))
                {
                    var key = lKeyABC;
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = key.AsSpan(0, 0x20).ToArray();
                        aes.IV = key.AsSpan(32, 16).ToArray();
                        ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
                        MemoryStream msEncrypted = new MemoryStream();
                        using (MemoryStream stream = new MemoryStream(data))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read))
                            {
                                cryptoStream.CopyTo(msEncrypted);
                            }
                        }
                        data = msEncrypted.ToArray();

                        if (File.Exists("locale_encrypt.ini"))
                            File.Delete("locale_encrypt.ini");

                        File.WriteAllBytes("locale_encrypt.ini", data);

                        if (writeToFile)
                            File.WriteAllBytes(LocaleIniPath, data);
                    }
                }

            }

            return data;

        }

        public DbObject ReadInitfs(byte[] key, bool patched = true)
        {
            DbObject dbObject = null;

            string initfsFilePath = ResolvePath((patched ? "native_patch/" : "native_data/") + "initfs_win32");
            if (initfsFilePath == "")
            {
                return dbObject;
            }

            var fsInitfs = new FileReader(new FileStream(initfsFilePath, FileMode.Open, FileAccess.Read));
            MemoryStream msInitFs = new MemoryStream(fsInitfs.ReadToEnd());
            fsInitfs.Dispose();

            // Go down to 556 (like TOC) using Deobfuscator
            //using (DbReader dbReader = new DbReader(msInitFs, CreateDeobfuscator()))
            using (DbReader dbReader = DbReader.GetDbReader(msInitFs, true))
            {
                // Read the Object (encrypted)
                dbObject = dbReader.ReadDbObject();

                byte[] encryptedData = dbObject.GetValue<byte[]>("encrypted");
                if (encryptedData != null)
                {
                    if (key == null)
                    {
                        Debug.WriteLine("[DEBUG] LoadInitfs()::Key is not available");
                        return dbObject;
                    }

                    MemoryStream decryptedData = new MemoryStream(encryptedData);
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = key;
                        aes.IV = key;
                        ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
                        using (MemoryStream stream = new MemoryStream(encryptedData))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read))
                            {
                                cryptoStream.CopyTo(decryptedData);
                                //cryptoStream.Read(encryptedData, 0, encryptedData.Length);
                            }
                        }
                    }
                    using (DbReader dbReader2 = DbReader.GetDbReader(decryptedData, false)) // new DbReader(decryptedData, CreateDeobfuscator()))
                    {
                        dbObject = dbReader2.ReadDbObject();
                    }
                }
                //}
                foreach (DbObject item in dbObject)
                {
                    DbObject fileItem = item.GetValue<DbObject>("$file");
                    //string payload = System.Text.Encoding.Default.GetString(value2.GetValue<byte[]>("payload")); 
                    string nameOfItem = fileItem.GetValue<string>("name");
                    if (!memoryFs.ContainsKey(nameOfItem))
                    {
                        var nameOfFile = nameOfItem;
                        if (nameOfFile.Contains("/"))
                            nameOfFile = nameOfItem.Split('/')[nameOfItem.Split('/').Length - 1];

                        var payloadOfBytes = fileItem.GetValue<byte[]>("payload");
                        //using (NativeWriter nativeWriter = new NativeWriter(new FileStream("Debugging/" + nameOfFile, FileMode.OpenOrCreate)))
                        //{
                        //	nativeWriter.Write(payloadOfBytes);
                        //}


                        memoryFs.Add(nameOfItem, payloadOfBytes);
                    }
                }

                //using (DbWriter dbWriter = new DbWriter(new FileStream("decrypted_initfs_" + (patched ? "patch" : "data"), FileMode.Create), inWriteHeader: true))
                //{
                //	dbWriter.Write(dbObject);
                //}


            }
            if (memoryFs.ContainsKey("__fsinternal__"))
            {
                DbObject dbObject2 = null;
                using (DbReader dbReader3 = new DbReader(new MemoryStream(memoryFs["__fsinternal__"]), null))
                {
                    dbObject2 = dbReader3.ReadDbObject();
                }
                memoryFs.Remove("__fsinternal__");
                if (dbObject2.GetValue("inheritContent", defaultValue: false))
                {
                    ReadInitfs(key, patched: false);
                }
            }

            return dbObject;
        }

        public void WriteInitfs(Stream outStream, bool patched = true)
        {
            string initfsPath = ResolvePath((patched ? "native_patch/" : "native_data/") + "initfs_win32");
            if (initfsPath == "")
            {
                return;
            }
            var encryptionKey = LoadKey();
            MemoryStream unencryptedStream = new MemoryStream();
            MemoryStream encryptedStream = new MemoryStream();
            DbWriter dbWriter = new DbWriter(unencryptedStream, leaveOpen: true);
            DbObject unencryptedDb = new DbObject(new List<object>());
            string key;
            byte[] value;
            foreach (KeyValuePair<string, byte[]> memoryF in memoryFs)
            {
                memoryF.Deconstruct(out key, out value);
                string name2 = key;
                byte[] data2 = value;
                DbObject fileToc2 = new DbObject(new Dictionary<string, object>());
                fileToc2.AddValue("name", name2);
                fileToc2.AddValue("payload", data2);
                DbObject listEntryToc2 = new DbObject(new Dictionary<string, object>());
                listEntryToc2.AddValue("$file", fileToc2);
                unencryptedDb.List.Add(listEntryToc2);
            }
            dbWriter.Write(unencryptedDb);
            unencryptedStream.Position = 0L;
            using (Aes aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.IV = encryptionKey;
                ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
                using (CryptoStream cryptoStream = new CryptoStream(encryptedStream, transform, CryptoStreamMode.Write))
                {
                    unencryptedStream.CopyTo(cryptoStream);
                }
            }
            byte[] encryptedDbEntry = encryptedStream.ToArray();
            DbObject newEncryptEntry = new DbObject(new Dictionary<string, object> { { "encrypted", encryptedDbEntry } });
            DbWriter dbWriterEncrypted = new DbWriter(outStream, leaveOpen: true);
            dbWriterEncrypted.Write(newEncryptEntry);
            unencryptedStream.Close();
            unencryptedStream.Dispose();
            //tocWriter.Write(outStream, newTocEntry, writeHeader: true);

        }

        private void ProcessLayouts()
        {
            string dataPath = ResolvePath("native_data/layout.toc");
            string patchPath = ResolvePath("native_patch/layout.toc");


            DbObject dataLayoutTOC = null;
            using (DbReader dbReader = new DbReader(new FileStream(dataPath, FileMode.Open, FileAccess.Read), CreateDeobfuscator()))
            {
                dataLayoutTOC = dbReader.ReadDbObject();
            }
            foreach (DbObject dboSuperBundle in dataLayoutTOC.GetValue<DbObject>("superBundles"))
            {
                superBundles.Add(dboSuperBundle.GetValue<string>("name").ToLower());
            }

            if (patchPath != "")
            {
                DbObject patchLayoutTOC = null;
                using (DbReader dbReader2 = new DbReader(new FileStream(patchPath, FileMode.Open, FileAccess.Read), CreateDeobfuscator()))
                {
                    patchLayoutTOC = dbReader2.ReadDbObject();
                }
                foreach (DbObject sbItem in patchLayoutTOC.GetValue<DbObject>("superBundles"))
                {
                    string item = sbItem.GetValue<string>("name").ToLower();
                    if (!superBundles.Contains(item))
                    {
                        superBundles.Add(item);
                    }
                }
                baseNum = patchLayoutTOC.GetValue("base", 0u);
                headNum = patchLayoutTOC.GetValue("head", 0u);
                ProcessCatalogs(patchLayoutTOC);
                ProcessManifest(patchLayoutTOC);
            }
            else
            {
                baseNum = dataLayoutTOC.GetValue("base", 0u);
                headNum = dataLayoutTOC.GetValue("head", 0u);
                ProcessCatalogs(dataLayoutTOC);
                ProcessManifest(dataLayoutTOC);
            }

            SystemIteration = baseNum + headNum + (ulong)new FileInfo(Directory.GetFiles(basePath, "*.exe").First()).LastWriteTime.Ticks;
        }

        private void ProcessCatalogs(DbObject patchLayout)
        {
            DbObject value = patchLayout.GetValue<DbObject>("installManifest");
            if (value != null)
            {
                foreach (DbObject item in value.GetValue<DbObject>("installChunks"))
                {
                    if (item.GetValue("testDLC", defaultValue: false))
                        continue;

                    //{
                    bool alwaysInstalled = item.GetValue("alwaysInstalled", defaultValue: false);
                    string path = "win32/" + item.GetValue<string>("name");

                    if (ProfileManager.IsLoaded(EGame.StarWarsSquadrons, EGame.BFV))
                    {
                        if (path == "win32/installation/default")
                        {
                            continue;
                        }
                    }


                    //if (
                    //	(ProfilesLibrary.DataVersion != 20180628 
                    //		|| !(text == "win32/installation/default")) 
                    //		&& (File.Exists(ResolvePath(text + "/cas.cat"))
                    //		|| (item.HasValue("files") && item.GetValue<DbObject>("files").Count != 0
                    //	) 
                    //	|| ProfilesLibrary.DataVersion == 20181207 || ProfilesLibrary.DataVersion == 20190905 || ProfilesLibrary.DataVersion == 20180628)

                    //	|| ProfilesLibrary.IsFIFA21DataVersion()
                    //	)
                    //{
                    Catalog catalogInfo = null;
                    Guid catalogId = item.GetValue<Guid>("id");
                    catalogInfo = catalogs.Find((Catalog ci) => ci.Id == catalogId);
                    if (catalogInfo == null)
                    {
                        catalogInfo = new Catalog();
                        catalogInfo.Id = item.GetValue<Guid>("id");
                        catalogInfo.Name = path;
                        catalogInfo.AlwaysInstalled = alwaysInstalled;

                        if (item.HasValue("PersistentIndex"))
                        {
                            catalogInfo.PersistentIndex = item.GetValue<int>("PersistentIndex");
                        }
                        foreach (string tocBundle in item.GetValue<DbObject>("superBundles"))
                        {
                            catalogInfo.SuperBundles.Add(tocBundle.ToLower(), value: false);
                        }
                    }
                    if (item.HasValue("files"))
                    {
                        foreach (DbObject file in item.GetValue<DbObject>("files"))
                        {
                            int value3 = file.GetValue("id", 0);
                            while (casFiles.Count <= value3)
                            {
                                casFiles.Add("");
                            }
                            string text3 = file.GetValue<string>("path").Trim('/');
                            text3 = text3.Replace("native_data/Data", "native_data");
                            text3 = text3.Replace("native_data/Patch", "native_patch");
                            casFiles[value3] = text3;
                        }
                    }
                    if (item.HasValue("splitSuperBundles"))
                    {
                        foreach (DbObject tocsbBundle in item.GetValue<DbObject>("splitSuperBundles"))
                        {
                            string sbKey = tocsbBundle.GetValue<string>("superBundle").ToLower();
                            if (!catalogInfo.SuperBundles.ContainsKey(sbKey))
                            {
                                catalogInfo.SuperBundles.Add(sbKey, value: true);
                            }
                        }
                    }
                    if (item.HasValue("splitTocs"))
                    {
                        foreach (DbObject item5 in item.GetValue<DbObject>("splitTocs"))
                        {
                            string key2 = "win32/" + item5.GetValue<string>("superbundle").ToLower();
                            if (!catalogInfo.SuperBundles.ContainsKey(key2))
                            {
                                catalogInfo.SuperBundles.Add(key2, value: true);
                            }
                        }
                    }
                    catalogs.Add(catalogInfo);
                    //}
                    //}
                }
                return;
            }
            Catalog catalogInfo2 = new Catalog
            {
                Name = ""
            };
            foreach (string superBundle in superBundles)
            {
                catalogInfo2.SuperBundles.Add(superBundle, value: false);
            }
            catalogs.Add(catalogInfo2);
        }

        public IEnumerable<DbObject> EnumerateBundles()
        {
            foreach (ManifestBundleInfo manifestBundle in manifestBundles)
            {
                ManifestFileInfo manifestFileInfo = manifestBundle.files[0];
                Catalog catalogInfo = catalogs[manifestFileInfo.file.CatalogIndex];
                string path = ResolvePath(manifestFileInfo.file);
                if (File.Exists(path))
                {
                    using (NativeReader reader = new NativeReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
                    {
                        using (BinarySbReader sbReader = new BinarySbReader(reader.CreateViewStream(manifestFileInfo.offset, manifestFileInfo.size), 0L, null))
                        {
                            DbObject dbObject = sbReader.ReadDbObject();
                            string newValue = manifestBundle.hash.ToString("x8");
                            if (ProfileManager.SharedBundles.ContainsKey(manifestBundle.hash))
                            {
                                newValue = ProfileManager.SharedBundles[manifestBundle.hash];
                            }
                            dbObject.SetValue("name", newValue);
                            dbObject.SetValue("catalog", catalogInfo.Name);
                            yield return dbObject;
                        }
                    }
                }
            }
        }

        public List<ChunkAssetEntry> ProcessManifestChunks()
        {
            List<ChunkAssetEntry> list = new List<ChunkAssetEntry>();
            foreach (ManifestChunkInfo manifestChunk in manifestChunks)
            {
                ManifestFileInfo file = manifestChunk.file;
                ChunkAssetEntry chunkAssetEntry = new ChunkAssetEntry();
                chunkAssetEntry.Id = manifestChunk.guid;
                string casPath = (file.file.IsInPatch ? "native_patch/" : "native_data/") + catalogs[file.file.CatalogIndex].Name + "/cas_" + file.file.CasIndex.ToString("D2") + ".cas";
                chunkAssetEntry.Location = AssetDataLocation.CasNonIndexed;
                chunkAssetEntry.Size = file.size;
                chunkAssetEntry.ExtraData = new AssetExtraData();
                chunkAssetEntry.ExtraData.DataOffset = file.offset;
                chunkAssetEntry.ExtraData.CasPath = casPath;
                list.Add(chunkAssetEntry);
            }
            return list;
        }

        public ManifestBundleInfo GetManifestBundle(string name)
        {
            int result = 0;
            if (name.Length != 8 || !int.TryParse(name, NumberStyles.HexNumber, null, out result))
            {
                result = Fnv1.HashString(name);
            }
            foreach (ManifestBundleInfo manifestBundle in manifestBundles)
            {
                if (manifestBundle.hash == result)
                {
                    return manifestBundle;
                }
            }
            return null;
        }

        public ManifestBundleInfo GetManifestBundle(int nameHash)
        {
            foreach (ManifestBundleInfo manifestBundle in manifestBundles)
            {
                if (manifestBundle.hash == nameHash)
                {
                    return manifestBundle;
                }
            }
            return null;
        }

        public ManifestChunkInfo GetManifestChunk(Guid id)
        {
            return manifestChunks.Find((ManifestChunkInfo a) => a.guid == id);
        }

        public void AddManifestBundle(ManifestBundleInfo bi)
        {
            manifestBundles.Add(bi);
        }

        public void AddManifestChunk(ManifestChunkInfo ci)
        {
            manifestChunks.Add(ci);
        }

        public void ResetManifest()
        {
            manifestBundles.Clear();
            manifestChunks.Clear();
            catalogs.Clear();
            superBundles.Clear();
            ProcessLayouts();
        }

        public byte[] WriteManifest()
        {
            using (NativeWriter nativeWriter = new NativeWriter(new MemoryStream()))
            {
                List<ManifestFileInfo> list = new List<ManifestFileInfo>();
                foreach (ManifestBundleInfo manifestBundle in manifestBundles)
                {
                    for (int i = 0; i < manifestBundle.files.Count; i++)
                    {
                        ManifestFileInfo item = manifestBundle.files[i];
                        list.Add(item);
                    }
                }
                foreach (ManifestChunkInfo manifestChunk in manifestChunks)
                {
                    list.Add(manifestChunk.file);
                    manifestChunk.fileIndex = list.Count - 1;
                }
                nativeWriter.Write(list.Count);
                nativeWriter.Write(manifestBundles.Count);
                nativeWriter.Write(manifestChunks.Count);
                foreach (ManifestFileInfo item2 in list)
                {
                    nativeWriter.Write(item2.file);
                    nativeWriter.Write(item2.offset);
                    nativeWriter.Write(item2.size);
                }
                foreach (ManifestBundleInfo manifestBundle2 in manifestBundles)
                {
                    nativeWriter.Write(manifestBundle2.hash);
                    nativeWriter.Write(list.IndexOf(manifestBundle2.files[0]));
                    nativeWriter.Write(manifestBundle2.files.Count);
                    nativeWriter.Write(0);
                    nativeWriter.Write(0);
                }
                foreach (ManifestChunkInfo manifestChunk2 in manifestChunks)
                {
                    nativeWriter.Write(manifestChunk2.guid);
                    nativeWriter.Write(manifestChunk2.fileIndex);
                }
                return ((MemoryStream)nativeWriter.BaseStream).ToArray();
            }
        }

        private void ProcessManifest(DbObject patchLayout)
        {
            DbObject manifest = patchLayout.GetValue<DbObject>("manifest");
            if (manifest == null)
                return;

            List<ManifestFileInfo> manifestFiles = new List<ManifestFileInfo>();
            ManifestFileRef fileRef = manifest.GetValue("file", 0);
            _ = catalogs[fileRef.CatalogIndex];
            var fsPath = ResolvePath(fileRef);
            if (string.IsNullOrEmpty(fsPath))
                return;

            using (NativeReader reader = new NativeReader(new FileStream(fsPath, FileMode.Open, FileAccess.Read)))
            {
                long manifestOffset = manifest.GetValue<int>("offset");
                long manifestSize = manifest.GetValue<int>("size");

                reader.Position = manifestOffset;

                uint fileCount = reader.ReadUInt();
                uint bundleCount = reader.ReadUInt();
                uint chunksCount = reader.ReadUInt();

                // files
                for (uint i = 0; i < fileCount; i++)
                {
                    ManifestFileInfo fi = new ManifestFileInfo()
                    {
                        file = reader.ReadInt(),
                        offset = reader.ReadUInt(),
                        size = reader.ReadLong(),
                        isChunk = false
                    };
                    manifestFiles.Add(fi);
                }

                // bundles
                for (uint i = 0; i < bundleCount; i++)
                {
                    ManifestBundleInfo bi = new ManifestBundleInfo { hash = reader.ReadInt() };

                    int startIndex = reader.ReadInt();
                    int count = reader.ReadInt();

                    int unk1 = reader.ReadInt();
                    int unk2 = reader.ReadInt();

                    for (int j = 0; j < count; j++)
                        bi.files.Add(manifestFiles[startIndex + j]);

                    manifestBundles.Add(bi);
                }

                // chunks
                for (uint i = 0; i < chunksCount; i++)
                {
                    ManifestChunkInfo ci = new ManifestChunkInfo
                    {
                        guid = reader.ReadGuid(),
                        fileIndex = reader.ReadInt()
                    };
                    ci.file = manifestFiles[ci.fileIndex];
                    ci.file.isChunk = true;
                    manifestChunks.Add(ci);
                }
            }
        }

        public static string LastPatchedVersionPath
        {
            get
            {
                return AppContext.BaseDirectory + "\\" + "LastPatchedVersion.json";
            }
        }

        public static bool GetGameWasPatched()
        {
            var gameWasPatched = false;
            var lastHead = 0u;
            var LastHeadData = new Dictionary<string, uint>();
            if (File.Exists(LastPatchedVersionPath))
            {
                LastHeadData = JsonConvert.DeserializeObject<Dictionary<string, uint>>(File.ReadAllText(LastPatchedVersionPath));
                if (LastHeadData.ContainsKey(Instance.BasePath))
                {
                    lastHead = LastHeadData[Instance.BasePath];
                }
            }

            //// Notify if new Patch detected
            if (FileSystem.Instance.Head != lastHead)
            {
                gameWasPatched = true;
            }
            return gameWasPatched;
        }

        public static void MakeTOCOriginals(string dir)
        {
            var enumerationOptions = new EnumerationOptions() { RecurseSubdirectories = true, MaxRecursionDepth = 10 };
            foreach (var tFile in Directory.EnumerateFiles(dir, "*.toc", enumerationOptions))
            {
                if (File.Exists(tFile + ".bak"))
                    File.Copy(tFile + ".bak", tFile, true);
            }

            foreach (var tFile in Directory.EnumerateFiles(dir, "*.sb", enumerationOptions))
            {
                if (File.Exists(tFile + ".bak"))
                    File.Copy(tFile + ".bak", tFile, true);
            }

            //foreach (var tFile in Directory.EnumerateFiles(dir, "*.cas", enumerationOptions))
            //{
            //    if (File.Exists(tFile + ".bak"))
            //        File.Copy(tFile + ".bak", tFile, true);
            //}

        }

        public static void MakeGameDataBackup(string dir)
        {
            var backupDataPath = FileSystem.Instance.BasePath + "\\BackupData.bak";
            var backupPatchPath = FileSystem.Instance.BasePath + "\\BackupPatch.bak";

            var gameWasPatched = GetGameWasPatched();
            var enumerationOptions = new EnumerationOptions() { RecurseSubdirectories = true, MaxRecursionDepth = 10 };

            var hasBackupFiles = Directory.EnumerateFiles(dir, "*.toc.bak", enumerationOptions).Any(); //File.Exists(backupDataPath) && File.Exists(backupPatchPath);
            if (gameWasPatched || !hasBackupFiles)
            {
                foreach (var tFile in Directory.EnumerateFiles(dir, "*.toc", enumerationOptions))
                {
                    if (File.Exists(tFile))
                        File.Copy(tFile, tFile + ".bak", true);
                }

                foreach (var tFile in Directory.EnumerateFiles(dir, "*.sb", enumerationOptions))
                {
                    if (File.Exists(tFile))
                        File.Copy(tFile, tFile + ".bak", true);
                }
                //ZipFile.CreateFromDirectory(FileSystem.Instance.BasePath + "\\Data", backupDataPath);
                //ZipFile.CreateFromDirectory(FileSystem.Instance.BasePath + "\\Patch", backupPatchPath);
            }
        }


    }
}
