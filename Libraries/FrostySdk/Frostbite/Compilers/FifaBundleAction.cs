using FMT.FileTools;
using FrostySdk.Frostbite.IO;
using FrostySdk.IO;
using FrostySdk.Managers;
using ModdingSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using static ModdingSupport.ModExecutor;

namespace FrostySdk.Frostbite.Compilers
{
    /// <summary>
    /// Used by FIFA 19, FIFA 20, Madden 20 and above
    /// </summary>
    public class FifaBundleAction
    {
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

        private Exception errorException;

        private ManualResetEvent doneEvent;

        private ModExecutor parent;

        private Catalog catalogInfo;

        private Dictionary<int, string> casFiles = new Dictionary<int, string>();

        public Catalog CatalogInfo => catalogInfo;

        public Dictionary<int, string> CasFiles => casFiles;

        public bool HasErrored => errorException != null;

        public Exception Exception => errorException;

        public FifaBundleAction(Catalog inCatalogInfo, ManualResetEvent inDoneEvent, ModExecutor inParent)
        {
            catalogInfo = inCatalogInfo;
            parent = inParent;
            doneEvent = inDoneEvent;
        }

        public void Run()
        {
            try
            {
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
                    string location_toc_file = parent.fs.ResolvePath($"{arg}.toc").ToLower();
                    if (location_toc_file != "")
                    {
                        uint orig_toc_file_num1 = 0u;
                        uint tocchunkposition = 0u;
                        byte[] byte_array_of_original_toc_file = null;
                        using (NativeReader reader_original_toc_file = new DeobfuscatedReader(new FileStream(location_toc_file, FileMode.Open, FileAccess.Read), parent.fs.CreateDeobfuscator()))
                        {
                            uint orig_toc_file_num = reader_original_toc_file.ReadUInt();
                            orig_toc_file_num1 = reader_original_toc_file.ReadUInt();
                            tocchunkposition = reader_original_toc_file.ReadUInt();
                            byte_array_of_original_toc_file = reader_original_toc_file.ReadToEnd();
                            if (orig_toc_file_num == 3286619587u)
                            {
                                using (Aes aes = Aes.Create())
                                {
                                    aes.Key = key_2_from_key_manager;
                                    aes.IV = key_2_from_key_manager;
                                    aes.Padding = PaddingMode.None;
                                    ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
                                    using (MemoryStream stream = new MemoryStream(byte_array_of_original_toc_file))
                                    {
                                        using (CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read))
                                        {
                                            cryptoStream.Read(byte_array_of_original_toc_file, 0, byte_array_of_original_toc_file.Length);
                                        }
                                    }
                                }
                            }
                        }
                        string location_toc_file_mod_data = location_toc_file.Replace("patch\\win32", "moddata\\patch\\win32");
                        FileInfo fi_toc_file_mod_data = new FileInfo(location_toc_file_mod_data);
                        if (!Directory.Exists(fi_toc_file_mod_data.DirectoryName))
                        {
                            Directory.CreateDirectory(fi_toc_file_mod_data.DirectoryName);
                        }
                        using (NativeWriter writer_new_toc_file_mod_data = new NativeWriter(new FileStream(location_toc_file_mod_data, FileMode.Create)))
                        {
                            writer_new_toc_file_mod_data.Write(30331136);
                            writer_new_toc_file_mod_data.Write(new byte[552]);
                            long position = writer_new_toc_file_mod_data.BaseStream.Position;
                            writer_new_toc_file_mod_data.Write(3280507699u);
                            long num4 = 4294967295L;
                            long num5 = 4294967295L;
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
                                            List<BundleFileEntry> lstBundleFiles3 = new List<BundleFileEntry>();
                                            int num9;
                                            do
                                            {
                                                num9 = reader_of_original_toc_file_array.ReadInt();
                                                int inOffset = reader_of_original_toc_file_array.ReadInt();
                                                int inSize = reader_of_original_toc_file_array.ReadInt();
                                                lstBundleFiles3.Add(new BundleFileEntry(num9 & int.MaxValue, inOffset, inSize));
                                            }
                                            while ((num9 & 2147483648u) != 0L);
                                            reader_of_original_toc_file_array.Position = num8 - 12;
                                            int num10 = 0;
                                            string text3 = "";
                                            do
                                            {
                                                string str = reader_of_original_toc_file_array.ReadNullTerminatedString();
                                                num10 = reader_of_original_toc_file_array.ReadInt() - 1;
                                                text3 = Utils.ReverseString(str) + text3;
                                                if (num10 != -1)
                                                {
                                                    reader_of_original_toc_file_array.Position = num10 - 12;
                                                }
                                            }
                                            while (num10 != -1);
                                            reader_of_original_toc_file_array.Position = position2;
                                            int key2 = Fnv1.HashString(text3.ToLower());
                                            if (parent.modifiedBundles.ContainsKey(key2))
                                            {
                                                ModBundleInfo modBundleInfo = parent.modifiedBundles[key2];
                                                MemoryStream memoryStream = new MemoryStream();
                                                foreach (BundleFileEntry item in lstBundleFiles3)
                                                {
                                                    using (NativeReader nativeReader3 = new NativeReader(new FileStream(parent.fs.ResolvePath(parent.fs.GetFilePath(item.CasIndex)), FileMode.Open, FileAccess.Read)))
                                                    {
                                                        nativeReader3.Position = item.Offset;
                                                        memoryStream.Write(nativeReader3.ReadBytes(item.Size), 0, item.Size);
                                                    }
                                                }
                                                DbObject dbObject = null;
                                                using (BinarySbReader binarySbReader = new BinarySbReader(memoryStream, 0L, parent.fs.CreateDeobfuscator()))
                                                {
                                                    dbObject = binarySbReader.ReadDbObject();
                                                    foreach (DbObject ebxItem in dbObject.GetValue<DbObject>("ebx"))
                                                    {
                                                        ebxItem.GetValue("size", 0);
                                                        long value = ebxItem.GetValue("offset", 0L);
                                                        long num11 = 0L;
                                                        foreach (BundleFileEntry item3 in lstBundleFiles3)
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
                                                        foreach (BundleFileEntry item5 in lstBundleFiles3)
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
                                                        foreach (BundleFileEntry item7 in lstBundleFiles3)
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
                                                    int num14 = modBundleInfo.Modify.Ebx.FindIndex((string a) => a.Equals(ebx.GetValue<string>("name")));
                                                    if (num14 != -1)
                                                    {
                                                        EbxAssetEntry ebxAssetEntry = parent.modifiedEbx[modBundleInfo.Modify.Ebx[num14]];
                                                        if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + parent.archiveData[ebxAssetEntry.Sha1].Data.Length > 1073741824)
                                                        {
                                                            writer_new_cas_file?.Close();
                                                            writer_new_cas_file = GetNextCas(out casFileIndex);
                                                        }
                                                        ebx.SetValue("originalSize", ebxAssetEntry.OriginalSize);
                                                        ebx.SetValue("size", ebxAssetEntry.Size);
                                                        ebx.SetValue("cas", casFileIndex);
                                                        ebx.SetValue("offset", (int)writer_new_cas_file.BaseStream.Position);
                                                        var ebxData = parent.archiveData[ebxAssetEntry.Sha1].Data;
                                                        //using (FileStream fileStream = new FileStream("test.xmx", FileMode.OpenOrCreate))
                                                        //{
                                                        //    fileStream.Write(ebxData, 0, ebxData.Length);
                                                        //}

                                                        writer_new_cas_file.Write(ebxData);
                                                    }
                                                }
                                                foreach (string item8 in modBundleInfo.Add.Ebx)
                                                {
                                                    EbxAssetEntry ebxAssetEntry2 = parent.modifiedEbx[item8];
                                                    if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + parent.archiveData[ebxAssetEntry2.Sha1].Data.Length > 1073741824)
                                                    {
                                                        writer_new_cas_file?.Close();
                                                        writer_new_cas_file = GetNextCas(out casFileIndex);
                                                    }
                                                    DbObject dbObject5 = DbObject.CreateObject();
                                                    dbObject5.SetValue("name", ebxAssetEntry2.Name);
                                                    dbObject5.SetValue("originalSize", ebxAssetEntry2.OriginalSize);
                                                    dbObject5.SetValue("size", ebxAssetEntry2.Size);
                                                    dbObject5.SetValue("cas", casFileIndex);
                                                    dbObject5.SetValue("offset", (int)writer_new_cas_file.BaseStream.Position);
                                                    dbObject.GetValue<DbObject>("ebx").Add(dbObject5);
                                                    writer_new_cas_file.Write(parent.archiveData[ebxAssetEntry2.Sha1].Data);
                                                }
                                                foreach (DbObject res in dbObject.GetValue<DbObject>("res"))
                                                {
                                                    int num15 = modBundleInfo.Modify.Res.FindIndex((string a) => a.Equals(res.GetValue<string>("name")));
                                                    if (num15 != -1)
                                                    {
                                                        ResAssetEntry resAssetEntry = parent.modifiedRes[modBundleInfo.Modify.Res[num15]];
                                                        if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + parent.archiveData[resAssetEntry.Sha1].Data.Length > 1073741824)
                                                        {
                                                            writer_new_cas_file?.Close();
                                                            writer_new_cas_file = GetNextCas(out casFileIndex);
                                                        }
                                                        res.SetValue("originalSize", resAssetEntry.OriginalSize);
                                                        res.SetValue("size", resAssetEntry.Size);
                                                        res.SetValue("cas", casFileIndex);
                                                        res.SetValue("offset", (int)writer_new_cas_file.BaseStream.Position);
                                                        res.SetValue("resRid", (long)resAssetEntry.ResRid);
                                                        res.SetValue("resMeta", resAssetEntry.ResMeta);
                                                        res.SetValue("resType", resAssetEntry.ResType);
                                                        writer_new_cas_file.Write(parent.archiveData[resAssetEntry.Sha1].Data);
                                                    }
                                                }
                                                foreach (string re in modBundleInfo.Add.Res)
                                                {
                                                    ResAssetEntry resAssetEntry2 = parent.modifiedRes[re];
                                                    if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + parent.archiveData[resAssetEntry2.Sha1].Data.Length > 1073741824)
                                                    {
                                                        writer_new_cas_file?.Close();
                                                        writer_new_cas_file = GetNextCas(out casFileIndex);
                                                    }
                                                    DbObject dbObject6 = DbObject.CreateObject();
                                                    dbObject6.SetValue("name", resAssetEntry2.Name);
                                                    dbObject6.SetValue("originalSize", resAssetEntry2.OriginalSize);
                                                    dbObject6.SetValue("size", resAssetEntry2.Size);
                                                    dbObject6.SetValue("cas", casFileIndex);
                                                    dbObject6.SetValue("offset", (int)writer_new_cas_file.BaseStream.Position);
                                                    dbObject6.SetValue("resRid", (long)resAssetEntry2.ResRid);
                                                    dbObject6.SetValue("resMeta", resAssetEntry2.ResMeta);
                                                    dbObject6.SetValue("resType", resAssetEntry2.ResType);
                                                    dbObject.GetValue<DbObject>("res").Add(dbObject6);
                                                    writer_new_cas_file.Write(parent.archiveData[resAssetEntry2.Sha1].Data);
                                                }
                                                foreach (DbObject chunk in dbObject.GetValue<DbObject>("chunks"))
                                                {
                                                    int num16 = modBundleInfo.Modify.Chunks.FindIndex((Guid a) => a == chunk.GetValue<Guid>("id"));
                                                    if (num16 != -1)
                                                    {
                                                        ChunkAssetEntry chunkAssetEntry = parent.ModifiedChunks[modBundleInfo.Modify.Chunks[num16]];
                                                        if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + parent.archiveData[chunkAssetEntry.Sha1].Data.Length > 1073741824)
                                                        {
                                                            writer_new_cas_file?.Close();
                                                            writer_new_cas_file = GetNextCas(out casFileIndex);
                                                        }
                                                        chunk.SetValue("originalSize", chunkAssetEntry.OriginalSize);
                                                        chunk.SetValue("size", chunkAssetEntry.Size);
                                                        chunk.SetValue("cas", casFileIndex);
                                                        chunk.SetValue("offset", (int)writer_new_cas_file.BaseStream.Position);
                                                        chunk.SetValue("logicalOffset", chunkAssetEntry.LogicalOffset);
                                                        chunk.SetValue("logicalSize", chunkAssetEntry.LogicalSize);
                                                        writer_new_cas_file.Write(parent.archiveData[chunkAssetEntry.Sha1].Data);
                                                    }
                                                }
                                                foreach (Guid chunk2 in modBundleInfo.Add.Chunks)
                                                {
                                                    ChunkAssetEntry chunkAssetEntry2 = parent.ModifiedChunks[chunk2];
                                                    if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + parent.archiveData[chunkAssetEntry2.Sha1].Data.Length > 1073741824)
                                                    {
                                                        writer_new_cas_file?.Close();
                                                        writer_new_cas_file = GetNextCas(out casFileIndex);
                                                    }
                                                    DbObject dbObject7 = DbObject.CreateObject();
                                                    dbObject7.SetValue("id", chunkAssetEntry2.Id);
                                                    dbObject7.SetValue("originalSize", chunkAssetEntry2.OriginalSize);
                                                    dbObject7.SetValue("size", chunkAssetEntry2.Size);
                                                    dbObject7.SetValue("cas", casFileIndex);
                                                    dbObject7.SetValue("offset", (int)writer_new_cas_file.BaseStream.Position);
                                                    dbObject7.SetValue("logicalOffset", chunkAssetEntry2.LogicalOffset);
                                                    dbObject7.SetValue("logicalSize", chunkAssetEntry2.LogicalSize);
                                                    dbObject.GetValue<DbObject>("chunks").Add(dbObject7);
                                                    DbObject dbObject8 = DbObject.CreateObject();
                                                    dbObject8.SetValue("h32", chunkAssetEntry2.H32);
                                                    DbObject dbObject9 = DbObject.CreateObject();
                                                    if (chunkAssetEntry2.FirstMip != -1)
                                                    {
                                                        dbObject9.SetValue("firstMip", chunkAssetEntry2.FirstMip);
                                                    }
                                                    dbObject.GetValue<DbObject>("chunkMeta").Add(dbObject8);
                                                    writer_new_cas_file.Write(parent.archiveData[chunkAssetEntry2.Sha1].Data);
                                                }
                                                BundleFileEntry bundleFileEntry = lstBundleFiles3[0];
                                                lstBundleFiles3.Clear();
                                                lstBundleFiles3.Add(bundleFileEntry);
                                                foreach (DbObject item9 in dbObject.GetValue<DbObject>("ebx"))
                                                {
                                                    lstBundleFiles3.Add(new BundleFileEntry(item9.GetValue("cas", 0), item9.GetValue("offset", 0), item9.GetValue("size", 0)));
                                                }
                                                foreach (DbObject item10 in dbObject.GetValue<DbObject>("res"))
                                                {
                                                    lstBundleFiles3.Add(new BundleFileEntry(item10.GetValue("cas", 0), item10.GetValue("offset", 0), item10.GetValue("size", 0)));
                                                }
                                                foreach (DbObject item11 in dbObject.GetValue<DbObject>("chunks"))
                                                {
                                                    lstBundleFiles3.Add(new BundleFileEntry(item11.GetValue("cas", 0), item11.GetValue("offset", 0), item11.GetValue("size", 0)));
                                                }
                                                int count = dbObject.GetValue<DbObject>("ebx").Count;
                                                int count2 = dbObject.GetValue<DbObject>("res").Count;
                                                int count3 = dbObject.GetValue<DbObject>("chunks").Count;
                                                using (NativeWriter nwCas2 = new NativeWriter(new MemoryStream()))
                                                {
                                                    nwCas2.Write(3735927486u, Endian.Big);
                                                    nwCas2.Write(3018715229u, Endian.Big);
                                                    nwCas2.Write(count + count2 + count3, Endian.Big);
                                                    nwCas2.Write(count, Endian.Big);
                                                    nwCas2.Write(count2, Endian.Big);
                                                    nwCas2.Write(count3, Endian.Big);
                                                    nwCas2.Write(3735927486u, Endian.Big);
                                                    nwCas2.Write(3735927486u, Endian.Big);
                                                    nwCas2.Write(3735927486u, Endian.Big);
                                                    long num17 = 0L;
                                                    new Dictionary<uint, long>();
                                                    List<string> list4 = new List<string>();
                                                    foreach (DbObject item12 in dbObject.GetValue<DbObject>("ebx"))
                                                    {
                                                        Fnv1.HashString(item12.GetValue<string>("name"));
                                                        nwCas2.Write((uint)num17, Endian.Big);
                                                        list4.Add(item12.GetValue<string>("name"));
                                                        num17 += item12.GetValue<string>("name").Length + 1;
                                                        nwCas2.Write(item12.GetValue("originalSize", 0), Endian.Big);
                                                    }
                                                    foreach (DbObject item13 in dbObject.GetValue<DbObject>("res"))
                                                    {
                                                        Fnv1.HashString(item13.GetValue<string>("name"));
                                                        nwCas2.Write((uint)num17, Endian.Big);
                                                        list4.Add(item13.GetValue<string>("name"));
                                                        num17 += item13.GetValue<string>("name").Length + 1;
                                                        nwCas2.Write(item13.GetValue("originalSize", 0), Endian.Big);
                                                    }
                                                    foreach (DbObject item14 in dbObject.GetValue<DbObject>("res"))
                                                    {
                                                        nwCas2.Write((uint)item14.GetValue("resType", 0L), Endian.Big);
                                                    }
                                                    foreach (DbObject item15 in dbObject.GetValue<DbObject>("res"))
                                                    {
                                                        nwCas2.Write(item15.GetValue<byte[]>("resMeta"));
                                                    }
                                                    foreach (DbObject item16 in dbObject.GetValue<DbObject>("res"))
                                                    {
                                                        nwCas2.Write(item16.GetValue("resRid", 0L), Endian.Big);
                                                    }
                                                    foreach (DbObject item17 in dbObject.GetValue<DbObject>("chunks"))
                                                    {
                                                        nwCas2.Write(item17.GetValue<Guid>("id"), Endian.Big);
                                                        nwCas2.Write(item17.GetValue("logicalOffset", 0), Endian.Big);
                                                        nwCas2.Write(item17.GetValue("logicalSize", 0), Endian.Big);
                                                    }
                                                    long position3 = nwCas2.BaseStream.Position;
                                                    foreach (string item18 in list4)
                                                    {
                                                        nwCas2.WriteNullTerminatedString(item18);
                                                    }
                                                    long num18 = 0L;
                                                    long num19 = 0L;
                                                    if (dbObject.GetValue<DbObject>("chunks").Count > 0)
                                                    {
                                                        DbObject value4 = dbObject.GetValue<DbObject>("chunkMeta");
                                                        num18 = nwCas2.BaseStream.Position;
                                                        using (DbWriter dbWriter = new DbWriter(new MemoryStream()))
                                                        {
                                                            nwCas2.Write(dbWriter.WriteDbObject("chunkMeta", value4));
                                                        }
                                                        num19 = nwCas2.BaseStream.Position - num18;
                                                    }
                                                    long num20 = nwCas2.BaseStream.Position - 4;
                                                    nwCas2.BaseStream.Position = 24L;
                                                    nwCas2.Write((uint)(position3 - 4), Endian.Big);
                                                    nwCas2.Write((uint)(num18 - 4), Endian.Big);
                                                    nwCas2.Write((uint)num19, Endian.Big);
                                                    nwCas2.BaseStream.Position = 0L;
                                                    nwCas2.Write((uint)num20, Endian.Big);
                                                    if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + nwCas2.BaseStream.Length > 1073741824)
                                                    {
                                                        writer_new_cas_file?.Close();
                                                        writer_new_cas_file = GetNextCas(out casFileIndex);
                                                    }
                                                    bundleFileEntry.CasIndex = casFileIndex;
                                                    bundleFileEntry.Offset = (int)writer_new_cas_file.BaseStream.Position;
                                                    bundleFileEntry.Size = (int)(num20 + 4);
                                                    writer_new_cas_file.Write(((MemoryStream)nwCas2.BaseStream).ToArray());
                                                }
                                            }
                                            list2.Add((int)(writer_new_toc_file_mod_data.BaseStream.Position - position));
                                            writer_new_toc_file_mod_data.Write((int)(writer_new_toc_file_mod_data.BaseStream.Position - position + lstBundleFiles3.Count * 3 * 4 + 5));
                                            for (int k = 0; k < lstBundleFiles3.Count; k++)
                                            {
                                                uint num21 = (uint)lstBundleFiles3[k].CasIndex;
                                                if (k != lstBundleFiles3.Count - 1)
                                                {
                                                    num21 = (uint)((int)num21 | int.MinValue);
                                                }
                                                writer_new_toc_file_mod_data.Write(num21);
                                                writer_new_toc_file_mod_data.Write(lstBundleFiles3[k].Offset);
                                                writer_new_toc_file_mod_data.Write(lstBundleFiles3[k].Size);
                                            }
                                            writer_new_toc_file_mod_data.WriteNullTerminatedString(new string(text3.Reverse().ToArray()));
                                            writer_new_toc_file_mod_data.Write(0);
                                            int num22 = text3.Length + 5;
                                            for (int l = 0; l < 16 - num22 % 16; l++)
                                            {
                                                writer_new_toc_file_mod_data.Write((byte)0);
                                            }
                                        }
                                        num4 = writer_new_toc_file_mod_data.BaseStream.Position - position;
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
                                    List<int> list5 = new List<int>();
                                    List<uint> list6 = new List<uint>();
                                    List<Guid> list7 = new List<Guid>();
                                    List<List<Tuple<Guid, int>>> list8 = new List<List<Tuple<Guid, int>>>();
                                    int num23 = 0;
                                    if (tocchunkposition != uint.MaxValue)
                                    {
                                        _ = writer_new_toc_file_mod_data.BaseStream.Position;
                                        _ = reader_of_original_toc_file_array.Position;
                                        reader_of_original_toc_file_array.Position = tocchunkposition - 12;
                                        num23 = reader_of_original_toc_file_array.ReadInt();
                                        for (int m = 0; m < num23; m++)
                                        {
                                            list5.Add(reader_of_original_toc_file_array.ReadInt());
                                            list8.Add(new List<Tuple<Guid, int>>());
                                        }
                                        for (int n = 0; n < num23; n++)
                                        {
                                            uint num24 = reader_of_original_toc_file_array.ReadUInt();
                                            long position4 = reader_of_original_toc_file_array.Position;
                                            reader_of_original_toc_file_array.Position = num24 - 12;
                                            Guid guid = reader_of_original_toc_file_array.ReadGuid();
                                            int num25 = reader_of_original_toc_file_array.ReadInt();
                                            int num26 = reader_of_original_toc_file_array.ReadInt();
                                            int num27 = reader_of_original_toc_file_array.ReadInt();
                                            reader_of_original_toc_file_array.Position = position4;
                                            list6.Add((uint)(writer_new_toc_file_mod_data.BaseStream.Position - position));
                                            if (parent.modifiedBundles.ContainsKey(chunksBundleHash))
                                            {
                                                ModBundleInfo modBundleInfo2 = parent.modifiedBundles[chunksBundleHash];
                                                int num28 = modBundleInfo2.Modify.Chunks.FindIndex((Guid g) => g == guid);
                                                if (num28 != -1)
                                                {
                                                    ChunkAssetEntry chunkAssetEntry3 = parent.ModifiedChunks[modBundleInfo2.Modify.Chunks[num28]];
                                                    byte[] outData = null;
                                                    if (chunkAssetEntry3.ExtraData != null)
                                                    {
                                                        HandlerExtraData handlerExtraData = (HandlerExtraData)chunkAssetEntry3.ExtraData;
                                                        Stream resourceData = AssetManager.Instance.GetResourceData(parent.fs.GetFilePath(num25), num26, num27);
                                                        chunkAssetEntry3 = (ChunkAssetEntry)handlerExtraData.Handler.Modify(chunkAssetEntry3, resourceData, handlerExtraData.Data, out outData);
                                                    }
                                                    else
                                                    {
                                                        outData = parent.archiveData[chunkAssetEntry3.Sha1].Data;
                                                    }
                                                    if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + outData.Length > 1073741824)
                                                    {
                                                        writer_new_cas_file?.Close();
                                                        writer_new_cas_file = GetNextCas(out casFileIndex);
                                                    }
                                                    num25 = casFileIndex;
                                                    num26 = (int)writer_new_cas_file.BaseStream.Position;
                                                    num27 = (int)chunkAssetEntry3.Size;
                                                    writer_new_cas_file.Write(outData);
                                                }
                                            }
                                            writer_new_toc_file_mod_data.Write(guid);
                                            writer_new_toc_file_mod_data.Write(num25);
                                            writer_new_toc_file_mod_data.Write(num26);
                                            writer_new_toc_file_mod_data.Write(num27);
                                            list7.Add(guid);
                                        }
                                    }
                                    if (parent.modifiedBundles.ContainsKey(chunksBundleHash) && location_toc_file.ToLower().Contains("globals.toc"))
                                    {
                                        foreach (Guid chunk3 in parent.modifiedBundles[chunksBundleHash].Add.Chunks)
                                        {
                                            ChunkAssetEntry chunkAssetEntry4 = parent.ModifiedChunks[chunk3];
                                            if (writer_new_cas_file == null || writer_new_cas_file.BaseStream.Length + parent.archiveData[chunkAssetEntry4.Sha1].Data.Length > 1073741824)
                                            {
                                                writer_new_cas_file?.Close();
                                                writer_new_cas_file = GetNextCas(out casFileIndex);
                                            }
                                            int value5 = casFileIndex;
                                            int value6 = (int)writer_new_cas_file.BaseStream.Position;
                                            int value7 = (int)chunkAssetEntry4.Size;
                                            list6.Add((uint)(writer_new_toc_file_mod_data.BaseStream.Position - position));
                                            list8.Add(new List<Tuple<Guid, int>>());
                                            list5.Add(-1);
                                            num23++;
                                            list7.Add(chunk3);
                                            writer_new_cas_file.Write(parent.archiveData[chunkAssetEntry4.Sha1].Data);
                                            writer_new_toc_file_mod_data.Write(chunk3);
                                            writer_new_toc_file_mod_data.Write(value5);
                                            writer_new_toc_file_mod_data.Write(value6);
                                            writer_new_toc_file_mod_data.Write(value7);
                                        }
                                    }
                                    if (num23 > 0)
                                    {
                                        num5 = writer_new_toc_file_mod_data.BaseStream.Position - position;
                                        int num29 = 0;
                                        List<int> list9 = new List<int>();
                                        for (int num30 = 0; num30 < num23; num30++)
                                        {
                                            list9.Add(-1);
                                            list5[num30] = -1;
                                            int index = (int)((uint)((int)HashData(list7[num30].ToByteArray()) % 16777619) % num23);
                                            list8[index].Add(new Tuple<Guid, int>(list7[num30], (int)list6[num30]));
                                        }
                                        for (int num31 = 0; num31 < list8.Count; num31++)
                                        {
                                            List<Tuple<Guid, int>> list10 = list8[num31];
                                            if (list10.Count > 1)
                                            {
                                                uint num32 = 1u;
                                                List<int> list11 = new List<int>();
                                                while (true)
                                                {
                                                    bool flag = true;
                                                    for (int num33 = 0; num33 < list10.Count; num33++)
                                                    {
                                                        int num34 = (int)((uint)((int)HashData(list10[num33].Item1.ToByteArray(), num32) % 16777619) % num23);
                                                        if (list9[num34] != -1 || list11.Contains(num34))
                                                        {
                                                            flag = false;
                                                            break;
                                                        }
                                                        list11.Add(num34);
                                                    }
                                                    if (flag)
                                                    {
                                                        break;
                                                    }
                                                    num32++;
                                                    list11.Clear();
                                                }
                                                for (int num35 = 0; num35 < list10.Count; num35++)
                                                {
                                                    list9[list11[num35]] = list10[num35].Item2;
                                                }
                                                list5[num31] = (int)num32;
                                            }
                                        }
                                        for (int num36 = 0; num36 < list8.Count; num36++)
                                        {
                                            if (list8[num36].Count == 1)
                                            {
                                                for (; list9[num29] != -1; num29++)
                                                {
                                                }
                                                list5[num36] = -1 - num29;
                                                list9[num29] = list8[num36][0].Item2;
                                            }
                                        }
                                        writer_new_toc_file_mod_data.Write(num23);
                                        for (int num37 = 0; num37 < num23; num37++)
                                        {
                                            writer_new_toc_file_mod_data.Write(list5[num37]);
                                        }
                                        for (int num38 = 0; num38 < num23; num38++)
                                        {
                                            writer_new_toc_file_mod_data.Write(list9[num38]);
                                        }
                                    }
                                    writer_new_toc_file_mod_data.BaseStream.Position = position + 4;
                                    writer_new_toc_file_mod_data.Write((int)num4);
                                    writer_new_toc_file_mod_data.Write((int)num5);
                                }
                            }
                            else
                            {
                                writer_new_toc_file_mod_data.Write(uint.MaxValue);
                                writer_new_toc_file_mod_data.Write(uint.MaxValue);
                            }
                        }
                    }
                }
                if (writer_new_cas_file != null)
                {
                    writer_new_cas_file?.Close();
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = errorException = ex;
            }
        }

        private NativeWriter GetNextCas(out int casFileIndex)
        {
            int num = 1;
            string text = parent.fs.BasePath + "ModData\\patch\\" + catalogInfo.Name + "\\cas_" + num.ToString("D2") + ".cas";
            while (File.Exists(text))
            {
                num++;
                text = parent.fs.BasePath + "ModData\\patch\\" + catalogInfo.Name + "\\cas_" + num.ToString("D2") + ".cas";
            }
            lock (locker)
            {
                casFiles.Add(++CasFileCount, "/native_data/Patch/" + catalogInfo.Name + "/cas_" + num.ToString("D2") + ".cas");
                casFileIndex = CasFileCount;
            }
            FileInfo fileInfo = new FileInfo(text);
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }
            return new NativeWriter(new FileStream(text, FileMode.Create));
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

        public void ThreadPoolCallback(object threadContext)
        {
            Run();
            if (Interlocked.Decrement(ref parent.numTasks) == 0)
            {
                doneEvent.Set();
            }
        }
    }
}
