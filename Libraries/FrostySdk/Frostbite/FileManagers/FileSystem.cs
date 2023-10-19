using FMT.FileTools;
using FMT.FileTools.Modding;
using FrostySdk.Frostbite.IO;
using FrostySdk.Frostbite.PluginInterfaces;
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
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;

namespace FrostySdk
{
    /// <summary>
    /// The FileSystem Class stores information about the physical files about the Game
    /// </summary>
    public class FileSystem : IDisposable
    {
        private List<string> paths { get; } = new List<string>();

        private HashSet<string> superBundles { get; } = new HashSet<string>();

        private List<string> splitSuperBundles { get; } = new List<string>();


        private List<Catalog> catalogs { get; } = new List<Catalog>();

        public Dictionary<string, byte[]> memoryFs { get; set; } = new Dictionary<string, byte[]>();

        public Dictionary<string, byte[]> MemoryFileSystem => memoryFs;

        private List<string> casFiles { get; } = new List<string>();

        private string cacheName;

        private Type deobfuscatorType;

        private uint baseNum;

        private uint headNum;

        /// <summary>
        /// A combination calculation of baseNum + headNum + lastAccessDate of the EXE
        /// </summary>
        public ulong SystemIteration;


        // ---------------- MANIFEST
        public List<ManifestBundleInfo> ManifestBundles { get; } = new();
        public List<ManifestFileInfo> ManifestFiles { get; } = new();
        public List<string> ManifestPaths { get; } = new();
        public Dictionary<string, long> ManifestPathOffsets { get; } = new();
        public List<ManifestChunkInfo> ManifestChunks { get; } = new();
        public Dictionary<string, List<ManifestChunkInfo>> ManifestPathsToChunks { get; } = new();

        // ---------------- MANIFEST

        public int SuperBundleCount => superBundles.Count;

        public IEnumerable<string> SuperBundles
        {
            get
            {
                return superBundles;
                //var arrayOfSb = superBundles.ToArray();
                //for (int i = 0; i < arrayOfSb.Length; i++)
                //{
                //    yield return arrayOfSb[i];
                //}
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

        public class SuperBundleInfo
        {
            public string Name { get; set; }
            public List<string> SplitSuperBundles { get; set; }

            public SuperBundleInfo(string inName)
            {
                Name = inName;
                SplitSuperBundles = new List<string>();
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

        public Dictionary<int, Catalog> CatalogsIndexed { get; } = new Dictionary<int, Catalog>();


        public int CasFileCount => casFiles.Count;

        public uint Base => baseNum;

        public uint Head => headNum;

        public string CacheName => cacheName;

        public string BasePath { get; private set; }

        public static FileSystem Instance { get; private set; }

        public string LocaleIniPath => ResolvePath("native_data/locale.ini");
        public Dictionary<Sha1, CatResourceEntry> CatResourceEntries { get; } = new Dictionary<Sha1, CatResourceEntry>();
        public Dictionary<Sha1, CatPatchEntry> CatPatchEntries { get; } = new Dictionary<Sha1, CatPatchEntry>();
        public Dictionary<int, string> CasFiles { get; } = new Dictionary<int, string>();

        /// <summary>
        /// The TOC File Type used to load TOC files (Is Public and Can be overridden). Default is TOCFile.
        /// </summary>
        public Type TOCFileType { get; set; } = typeof(TOCFile);

        /// <summary>
        /// If the Game has Live Tuning Updates, load the LTU from the File.
        /// </summary>
        public LiveTuningUpdate LiveTuningUpdate { get; } = new LiveTuningUpdate();

        /// <summary>
        /// Creates the <class cref="FileSystem">FileSystem</class> with the Directory of the Game Path provided
        /// </summary>
        /// <param name="inBasePath"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="AccessViolationException">If FMT doesn't have sufficient access. Throw an Exception and ask for Admin Access.</exception>
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

            BasePath = inBasePath;
            if (!BasePath.EndsWith(@"\") && !BasePath.EndsWith("/"))
            {
                BasePath += @"\";
            }

            if(!FileSystem.DirectoryHasPermission(BasePath, FileSystemRights.Write | FileSystemRights.Read | FileSystemRights.WriteData | FileSystemRights.Delete))
            {
                throw new AccessViolationException("You are unable to read & write to the Game Folder. Please restart the Application as Administrator.");
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
        /// Initializes against the Loaded game. Expects ProfileManager & BasePaths to be set.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="patched"></param>
        public void Initialize(byte[] key = null, bool patched = true)
        {
            ProcessLayouts();

            if (key == null)
                key = LoadKey();

            ReadInitfs(key, patched);
            EbxSharedTypeDescriptors.LoadSharedTypeDescriptors();

            ZStd.Bind();

            if (ProfileManager.IsLoaded(EGame.NHL23PS4))
            {
                Oodle.Bind(BasePath, 6);
            }

            Oodle.Bind(BasePath);

            LoadCatalogs();
        }

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
            if (Directory.Exists(BasePath + path))
            {
                if (iterateSubPaths)
                {
                    foreach (string item in Directory.EnumerateDirectories(BasePath + path, "*", SearchOption.AllDirectories))
                    {
                        if (!item.ToLower().Contains("\\patch") && File.Exists(item + "\\package.mft"))
                        {
                            //paths.Add("\\" + item.Replace(BasePath, "").ToLower() + "\\data\\");

                            string addPath = !BasePath.EndsWith(@"\") ? @"\" + path.ToLower() + @"\" : path.ToLower() + @"\";
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
                Debug.WriteLine(BasePath + path + " doesn't exist");
                throw new DirectoryNotFoundException(BasePath + path + " doesn't exist");
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
                resolvedPath = BasePath + (checkModData ? "ModData\\" : "") + filename.Replace("native_data", "Data\\");

            if (filename.Contains("native_patch"))
                resolvedPath = BasePath + (checkModData ? "ModData\\" : "") + filename.Replace("native_patch", "Patch\\");

            if (
                (ProfileManager.IsBF4DataVersion() || ProfileManager.IsGameVersion(EGame.FIFA17))
                && !Directory.Exists(Directory.GetParent(resolvedPath).FullName) && filename.Contains("native_patch"))
            {
                resolvedPath = BasePath + (checkModData ? "ModData\\" : "") + filename.Replace("native_patch", "Update\\Patch\\Data\\");
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
                    if (File.Exists(BasePath + paths[i] + filename) || Directory.Exists(BasePath + paths[i] + filename))
                    {
                        yield return BasePath + paths[i] + filename;
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

        public string GetFilePathByCatalogIndex(int catalogIndex, int cas, bool patch)
        {
            Catalog catalogInfo = CatalogsIndexed[catalogIndex];
            var result = (patch ? "native_patch/" : "native_data/") + catalogInfo.Name + "/cas_" + cas.ToString("D2") + ".cas";
            if (new ReadOnlySpan<char>(ResolvePath(result).ToArray()).IsEmpty)
            {
                throw new FileNotFoundException(result);
            }
            return result;
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

                        //if (File.Exists("locale_decrypt.ini"))
                        //    File.Delete("locale_decrypt.ini");

                        //File.WriteAllBytes("locale_decrypt.ini", data);
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

            var initfsFiles = Directory.GetFiles(Path.Combine(BasePath, ((patched ? "Patch" : "Data"))), "initfs_*", SearchOption.TopDirectoryOnly);

            string initfsFilePath = initfsFiles.First();// ResolvePath((patched ? "native_patch/" : "native_data/") + "initfs_win32");
            if (initfsFilePath == "")
            {
                return dbObject;
            }

            var fsInitfs = new FileReader(new FileStream(initfsFilePath, FileMode.Open, FileAccess.Read));
            MemoryStream msInitFs = new MemoryStream(fsInitfs.ReadToEnd());
            fsInitfs.Dispose();

            dbObject = ReadInitfs(msInitFs, key);

            
            if (memoryFs.ContainsKey("__fsinternal__"))
            {
                DbObject dbObject2 = null;
                using (DbReader dbReader3 = new DbReader(new MemoryStream(memoryFs["__fsinternal__"]), null))
                {
                    dbObject2 = dbReader3.ReadDbObject();
                }
                memoryFs.Remove("__fsinternal__");
                if (ProfileManager.Sources.Any(x => x.Path == "Data"))
                {
                    if (dbObject2.GetValue("inheritContent", defaultValue: false))
                    {
                        ReadInitfs(key, false);
                    }
                }
            }

            return dbObject;

        }

        public DbObject ReadInitfs(Stream initfsStream, byte[] key)
        {
            DbObject dbObject = null;

            // Go down to 556 (like TOC) using Deobfuscator
            //using (DbReader dbReader = new DbReader(msInitFs, CreateDeobfuscator()))
            using (DbReader dbReader = DbReader.GetDbReader(initfsStream, true))
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
                        using (MemoryStream encStream = new MemoryStream(encryptedData))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(encStream, transform, CryptoStreamMode.Read))
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
                    string nameOfItem = fileItem.GetValue<string>("name");
                    if (!memoryFs.ContainsKey(nameOfItem))
                    {
                        var nameOfFile = nameOfItem;
                        if (nameOfFile.Contains("/"))
                            nameOfFile = nameOfItem.Split('/')[nameOfItem.Split('/').Length - 1];

                        var payloadOfBytes = fileItem.GetValue<byte[]>("payload");
                        memoryFs.Add(nameOfItem, payloadOfBytes);
                    }
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
            var layoutFiles = Directory.GetFiles(this.BasePath, "*layout.toc", new EnumerationOptions() { RecurseSubdirectories = true }).ToList();
            layoutFiles = layoutFiles.Where(x => !x.Contains("ModData")).ToList();

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

            SystemIteration = baseNum + headNum + (ulong)new FileInfo(Directory.GetFiles(BasePath, "*.exe").First()).LastWriteTime.Ticks;
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
                    string path = item.HasValue("installBundle") ? item.GetValue<string>("installBundle") : "win32/" + item.GetValue<string>("name");

                    if (ProfileManager.IsLoaded(EGame.StarWarsSquadrons, EGame.BFV))
                    {
                        if (path == "win32/installation/default")
                        {
                            continue;
                        }
                    }

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

                    if (catalogInfo.PersistentIndex.HasValue && !CatalogsIndexed.ContainsKey(catalogInfo.PersistentIndex.Value))
                        CatalogsIndexed.Add(catalogInfo.PersistentIndex.Value, catalogInfo);

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

        public IEnumerable<DbObject> EnumerateManifestBundles()
        {
            foreach (ManifestBundleInfo manifestBundle in ManifestBundles)
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
            List<ChunkAssetEntry> chunks = new List<ChunkAssetEntry>();
            foreach (ManifestChunkInfo manifestChunk in ManifestChunks)
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
                chunks.Add(chunkAssetEntry);
            }
            return chunks;
        }

        public ManifestBundleInfo GetManifestBundle(string name)
        {
            int result = 0;
            if (name.Length != 8 || !int.TryParse(name, NumberStyles.HexNumber, null, out result))
            {
                result = Fnv1.HashString(name);
            }
            foreach (ManifestBundleInfo manifestBundle in ManifestBundles)
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
            foreach (ManifestBundleInfo manifestBundle in ManifestBundles)
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
            return ManifestChunks.Find((ManifestChunkInfo a) => a.guid == id);
        }

        public void AddManifestBundle(ManifestBundleInfo bi)
        {
            ManifestBundles.Add(bi);
        }

        public void AddManifestChunk(ManifestChunkInfo ci)
        {
            ManifestChunks.Add(ci);
        }

        public void ResetManifest()
        {
            ManifestBundles.Clear();
            ManifestChunks.Clear();
            catalogs.Clear();
            superBundles.Clear();
            ProcessLayouts();
        }

        public byte[] WriteManifest()
        {
            using (NativeWriter nativeWriter = new NativeWriter(new MemoryStream()))
            {
                List<ManifestFileInfo> manifestFiles = new List<ManifestFileInfo>();
                foreach (ManifestBundleInfo manifestBundle in ManifestBundles)
                {
                    for (int i = 0; i < manifestBundle.files.Count; i++)
                    {
                        ManifestFileInfo item = manifestBundle.files[i];
                        manifestFiles.Add(item);
                    }
                }
                foreach (ManifestChunkInfo manifestChunk in ManifestChunks)
                {
                    manifestFiles.Add(manifestChunk.file);
                    manifestChunk.fileIndex = manifestFiles.Count - 1;
                }
                nativeWriter.Write(manifestFiles.Count);
                nativeWriter.Write(ManifestBundles.Count);
                nativeWriter.Write(ManifestChunks.Count);
                foreach (ManifestFileInfo fI in manifestFiles)
                {
                    nativeWriter.Write(fI.file);
                    nativeWriter.Write(fI.offset);
                    nativeWriter.Write(fI.size);
                }
                foreach (ManifestBundleInfo bundle in ManifestBundles)
                {
                    nativeWriter.Write(bundle.hash);
                    nativeWriter.Write(manifestFiles.IndexOf(bundle.files[0]));
                    nativeWriter.Write(bundle.files.Count);
                    nativeWriter.Write(0);
                    nativeWriter.Write(0);
                }
                foreach (ManifestChunkInfo manifestChunk in ManifestChunks)
                {
                    nativeWriter.Write(manifestChunk.guid);
                    nativeWriter.Write(manifestChunk.fileIndex);
                }
                return ((MemoryStream)nativeWriter.BaseStream).ToArray();
            }
        }

        private void ProcessManifest(DbObject layout)
        {
            DbObject manifest = layout.GetValue<DbObject>("manifest");
            if (manifest == null)
                return;

            ManifestFileRef fileRef = manifest.GetValue("file", 0);
            _ = catalogs[fileRef.CatalogIndex];
            var fsPath = ResolvePath(fileRef);
            if (string.IsNullOrEmpty(fsPath))
                return;

            ManifestPaths.Add(fsPath);
            if (!ManifestPathsToChunks.ContainsKey(fsPath))
                ManifestPathsToChunks.Add(fsPath, new List<ManifestChunkInfo>());

            using (NativeReader reader = new NativeReader(new FileStream(fsPath, FileMode.Open, FileAccess.Read)))
            {
                long manifestOffset = manifest.GetValue<int>("offset");
                if(!ManifestPathOffsets.ContainsKey(fsPath))
                    ManifestPathOffsets.Add(fsPath, manifestOffset);
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
                        OffsetPosition = reader.Position,
                        offset = reader.ReadUInt(),
                        size = reader.ReadLong(),
                        isChunk = false
                    };
                    ManifestFiles.Add(fi);
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
                        bi.files.Add(ManifestFiles[startIndex + j]);

                    if(!ManifestBundles.Contains(bi))
                        ManifestBundles.Add(bi);
                }

                // chunks
                for (uint i = 0; i < chunksCount; i++)
                {
                    ManifestChunkInfo ci = new ManifestChunkInfo
                    {
                        guid = reader.ReadGuid(),
                        fileIndex = reader.ReadInt()
                    };
                    ci.file = ManifestFiles[ci.fileIndex];
                    ci.file.isChunk = true;

                    ManifestChunks.Add(ci);
                    ManifestPathsToChunks[fsPath].Add(ci);
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

        /// <summary>
        /// Test a directory for create file access permissions
        /// </summary>
        /// <param name="directoryPath">Full path to directory </param>
        /// <param name="accessRight">File System right tested</param>
        /// <returns>State [bool]</returns>
        public static bool DirectoryHasPermission(string directoryPath, FileSystemRights accessRight)
        {
            if (string.IsNullOrEmpty(directoryPath)) 
                return false;

            var directoryInfo = new DirectoryInfo(directoryPath);
            if (!directoryInfo.Exists)
                return false;

            try
            {
                AuthorizationRuleCollection rules = directoryInfo.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                WindowsIdentity identity = WindowsIdentity.GetCurrent();

                foreach (FileSystemAccessRule rule in rules)
                {
                    if (identity.Groups.Contains(rule.IdentityReference))
                    {
                        if ((accessRight & rule.FileSystemRights) == accessRight)
                        {
                            if (rule.AccessControlType == AccessControlType.Allow)
                                return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public void Dispose()
        {
        }
    }
}
