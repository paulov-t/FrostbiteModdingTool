﻿using Frosty.Hash;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Frostbite.IO;
using FrostySdk.IO;
using FrostySdk.Managers;
using paulv2k4ModdingExecuter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using static paulv2k4ModdingExecuter.FrostyModExecutor;

namespace FIFA21Plugin
{

    public class FIFA21BundleAction
    {
        public class BundleFileEntry
        {
            public int CasIndex;

            public int Offset;

            public int Size;

            public string Name;

            public BundleFileEntry(int inCasIndex, int inOffset, int inSize, string inName = null)
            {
                CasIndex = inCasIndex;
                Offset = inOffset;
                Size = inSize;
                Name = inName;
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                return $"CASIdx({CasIndex}), Offset({Offset}), Size({Size}), Name({Name})";

            }
        }

        private static readonly object locker = new object();

        public static int CasFileCount = 0;

        private Exception errorException;

        private ManualResetEvent doneEvent;

        private FrostyModExecutor parent;

        private CatalogInfo catalogInfo;

        private Dictionary<int, string> casFiles = new Dictionary<int, string>();

        public CatalogInfo CatalogInfo => catalogInfo;

        public Dictionary<int, string> CasFiles => casFiles;

        public bool HasErrored => errorException != null;

        public Exception Exception => errorException;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inCatalogInfo"></param>
        /// <param name="inDoneEvent"></param>
        /// <param name="inParent"></param>
        public FIFA21BundleAction(CatalogInfo inCatalogInfo, FrostyModExecutor inParent)
        {
            catalogInfo = inCatalogInfo;
            parent = inParent;
        }

        public FIFA21BundleAction(FrostyModExecutor inParent)
        {
            parent = inParent;
        }

        private bool CheckTocCasReadCorrectly(string tocPath)
        {
            TocCasReader_M21 tocCasReader_M21 = new TocCasReader_M21();
            tocCasReader_M21.Read(tocPath, 0, new BinarySbDataHelper(AssetManager.Instance));
            return true;
        }

        private enum ModType
        {
            EBX,
            RES,
            CHUNK
        }

        private Dictionary<string, List<Tuple<Sha1, string, ModType>>> GetModdedCasFiles()
        {
            Dictionary<string, List<Tuple<Sha1, string, ModType>>> casToMods = new Dictionary<string, List<Tuple<Sha1, string, ModType>>>();
            foreach (var modEBX in parent.modifiedEbx)
            {
                var originalEntry = AssetManager.Instance.GetEbxEntry(modEBX.Value.Name);
                if (originalEntry != null)
                {
                    var casPath = originalEntry.ExtraData.CasPath;
                    if (!casToMods.ContainsKey(casPath))
                    {
                        casToMods.Add(casPath, new List<Tuple<Sha1, string, ModType>>() { new Tuple<Sha1, string, ModType>(modEBX.Value.Sha1, modEBX.Value.Name, ModType.EBX) });
                    }
                    else
                    {
                        casToMods[casPath].Add(new Tuple<Sha1, string, ModType>(modEBX.Value.Sha1, modEBX.Value.Name, ModType.EBX));
                    }
                }
            }
            foreach (var modRES in parent.modifiedRes)
            {
                var originalEntry = AssetManager.Instance.GetEbxEntry(modRES.Value.Name);
                if (originalEntry != null)
                {
                    var casPath = originalEntry.ExtraData.CasPath;
                    if (!casToMods.ContainsKey(casPath))
                    {
                        casToMods.Add(casPath, new List<Tuple<Sha1, string, ModType>>() { new Tuple<Sha1, string, ModType>(modRES.Value.Sha1, modRES.Value.Name, ModType.RES) });
                    }
                    else
                    {
                        casToMods[casPath].Add(new Tuple<Sha1, string, ModType>(modRES.Value.Sha1, modRES.Value.Name, ModType.RES));
                    }
                }
            }
            foreach (var modChunks in parent.modifiedChunks)
            {
                var originalEntry = AssetManager.Instance.GetChunkEntry(modChunks.Value.Id);
                if (originalEntry != null)
                {
                    var casPath = originalEntry.ExtraData.CasPath;
                    if (!casToMods.ContainsKey(casPath))
                    {
                        casToMods.Add(casPath, new List<Tuple<Sha1, string, ModType>>() { new Tuple<Sha1, string, ModType>(modChunks.Value.Sha1, modChunks.Value.Id.ToString(), ModType.CHUNK) });
                    }
                    else
                    {
                        casToMods[casPath].Add(new Tuple<Sha1, string, ModType>(modChunks.Value.Sha1, modChunks.Value.Id.ToString(), ModType.CHUNK));
                    }
                }
            }

            return casToMods;
        }

        public void Run()
        {
            try
            {
                parent.Logger.Log("Loading files to know what to change.");

                //FIFA21AssetLoader assetLoader = new FIFA21AssetLoader();
                //assetLoader.Load(AssetManager.Instance, new BinarySbDataHelper(AssetManager.Instance));
                BuildCache buildCache = new BuildCache();
                buildCache.LoadData(ProfilesLibrary.ProfileName, parent.GamePath, parent.Logger, false, true); ;


                parent.Logger.Log("Finished loading files. Enumerating modified bundles.");

                var dictOfModsToCas = GetModdedCasFiles();
                if(dictOfModsToCas != null && dictOfModsToCas.Count > 0)
                {
                    foreach (var item in dictOfModsToCas) 
                    {
                        var casPath = item.Key.Replace("native_data"
                                , AssetManager.Instance.fs.BasePath + "ModData\\Data");
                        casPath = casPath.Replace("native_patch"
                            , AssetManager.Instance.fs.BasePath + "ModData\\Patch");

                        byte[] originalCASArray = null;
                        using (NativeReader readerOfCas = new NativeReader(new FileStream(casPath, FileMode.Open)))
                        {
                            originalCASArray = readerOfCas.ReadToEnd();
                        }

                        var positionOfNewData = 0;
                        using (NativeWriter nwCas = new NativeWriter(new FileStream(casPath, FileMode.Open)))
                        {
                            nwCas.Write(originalCASArray);
                            foreach (var modItem in item.Value)
                            {
                                byte[] data = new byte[0];
                                AssetEntry originalEntry = null;
                                switch(modItem.Item3)
                                {
                                    case ModType.EBX:
                                        originalEntry = AssetManager.Instance.GetEbxEntry(modItem.Item2);
                                        break;
                                    case ModType.RES:
                                        originalEntry = AssetManager.Instance.GetResEntry(modItem.Item2);
                                        break;
                                    case ModType.CHUNK:
                                        originalEntry = AssetManager.Instance.GetChunkEntry(Guid.Parse(modItem.Item2));
                                        break;
                                }
                                if (originalEntry != null)
                                {
                                    data = parent.archiveData[modItem.Item1].Data;
                                }
                               
                                if (data.Length > 0)
                                {
                                    // write the new data to end of the file (this should be fine)
                                    positionOfNewData = (int)nwCas.BaseStream.Position;
                                    nwCas.Write(data);

                                    parent.Logger.Log("Writing new asset entry for (" + originalEntry.Name + ")");
                                    Debug.WriteLine("Writing new asset entry for (" + originalEntry.Name + ")");

                                    var sb_cas_offset_position = originalEntry.SB_CAS_Offset_Position;
                                    var sb_sha1_position = originalEntry.SB_Sha1_Position;
                                    var sb_original_size_position = originalEntry.SB_OriginalSize_Position;// ebxObject.GetValue<int>("SB_OriginalSize_Position");

                                    var sbpath = parent.fs.ResolvePath(originalEntry.SBFileLocation);// ebxObject.GetValue<string>("SBFileLocation");
                                    sbpath = sbpath.Replace("\\patch", "\\ModData\\Patch");
                                    byte[] arrayOfSB = null;
                                    using (NativeReader nativeReader = new NativeReader(new FileStream(sbpath, FileMode.Open)))
                                    {
                                        arrayOfSB = nativeReader.ReadToEnd();
                                    }
                                    File.Delete(sbpath);
                                    using (NativeWriter nw_sb = new NativeWriter(new FileStream(sbpath, FileMode.OpenOrCreate)))
                                    {
                                        nw_sb.Write(arrayOfSB);
                                        nw_sb.BaseStream.Position = sb_cas_offset_position;
                                        nw_sb.Write((uint)positionOfNewData, Endian.Big);
                                        nw_sb.Flush();

                                        nw_sb.Write((uint)data.Length, Endian.Big);
                                        nw_sb.Flush();

                                        nw_sb.BaseStream.Position = sb_sha1_position;
                                        nw_sb.Write(modItem.Item1);
                                        nw_sb.Flush();

                                        //nativeWriter.BaseStream.Position = sb_original_size_position;
                                        //nativeWriter.Write(Convert.ToUInt32(modEBX.Value.OriginalSize), Endian.Little);
                                        //nativeWriter.Flush();


                                    }
                                }
                            }
                            
                        }
                    }
                }
                /*
                foreach (var modEBX in parent.modifiedEbx)
                {
                    var originalEntry = AssetManager.Instance.GetEbxEntry(modEBX.Value.Name);
                    if(originalEntry != null)
                    {
                        var ebxData = parent.archiveData[modEBX.Value.Sha1].Data;
                        var casPath = originalEntry.ExtraData.CasPath.Replace("native_data"
                            , AssetManager.Instance.fs.BasePath + "ModData\\Data");
                        casPath = casPath.Replace("native_patch"
                            , AssetManager.Instance.fs.BasePath + "ModData\\Patch");

                        byte[] originalCASArray = null;
                        using (NativeReader readerOfCas = new NativeReader(new FileStream(casPath, FileMode.Open)))
                        {
                            originalCASArray = readerOfCas.ReadToEnd();
                        }

                        var positionOfNewData = 0;
                        using (NativeWriter nativeWriter = new NativeWriter(new FileStream(casPath, FileMode.Open)))
                        {
                            nativeWriter.Write(originalCASArray);
                            // write the new data to end of the file (this should be fine)
                            positionOfNewData = (int)nativeWriter.BaseStream.Position;
                            nativeWriter.Write(ebxData);
                        }

                        using (var f = new FileStream(casPath, FileMode.Open, FileAccess.Read))
                        {
                            using (NativeReader reader = new NativeReader(f))
                            {
                                byte[] array = null;
                                using (CasReader casReader = new CasReader(reader.CreateViewStream(positionOfNewData, ebxData.Length)))
                                {
                                    array = casReader.Read();
                                }
                                if (array == null)
                                {
                                    throw new Exception("Array not found in CAS");
                                }
                            }
                        }

                        parent.Logger.Log("Writing new asset entry for EBX (" + modEBX.Key + ")");
                        Debug.WriteLine("Writing new asset entry for EBX (" + modEBX.Key + ")");

                        var sb_cas_offset_position = originalEntry.SB_CAS_Offset_Position;
                        var sb_sha1_position = originalEntry.SB_Sha1_Position;
                        var sb_original_size_position = originalEntry.SB_OriginalSize_Position;// ebxObject.GetValue<int>("SB_OriginalSize_Position");

                        var sbpath = parent.fs.ResolvePath(originalEntry.SBFileLocation);// ebxObject.GetValue<string>("SBFileLocation");
                        sbpath = sbpath.Replace("\\patch", "\\ModData\\Patch");
                        byte[] arrayOfSB = null;
                        using (NativeReader nativeReader = new NativeReader(new FileStream(sbpath, FileMode.Open)))
                        {
                            arrayOfSB = nativeReader.ReadToEnd();
                        }
                        File.Delete(sbpath);
                        using (NativeWriter nativeWriter = new NativeWriter(new FileStream(sbpath, FileMode.OpenOrCreate)))
                        {
                            nativeWriter.Write(arrayOfSB);
                            nativeWriter.BaseStream.Position = sb_cas_offset_position;
                            nativeWriter.Write((uint)positionOfNewData, Endian.Big);
                            nativeWriter.Flush();

                            nativeWriter.Write((uint)ebxData.Length, Endian.Big);
                            nativeWriter.Flush();

                            nativeWriter.BaseStream.Position = sb_sha1_position;
                            nativeWriter.Write(modEBX.Value.Sha1);
                            nativeWriter.Flush();

                            //nativeWriter.BaseStream.Position = sb_original_size_position;
                            //nativeWriter.Write(Convert.ToUInt32(modEBX.Value.OriginalSize), Endian.Little);
                            //nativeWriter.Flush();


                        }
                    }
                }
                foreach (var modRES in parent.modifiedRes)
                {
                    var originalEntry = AssetManager.Instance.GetResEntry(modRES.Value.Name);
                    if (originalEntry != null)
                    {
                        //var resData = originalEntry.ModifiedEntry.Data;
                        var resData = parent.archiveData[modRES.Value.Sha1].Data;

                        var casPath = originalEntry.ExtraData.CasPath.Replace("native_data"
                            , AssetManager.Instance.fs.BasePath + "ModData\\Data");
                        casPath = casPath.Replace("native_patch"
                            , AssetManager.Instance.fs.BasePath + "ModData\\Patch");

                        byte[] originalCASArray = null;
                        using (NativeReader readerOfCas = new NativeReader(new FileStream(casPath, FileMode.Open)))
                        {
                            originalCASArray = readerOfCas.ReadToEnd();
                        }

                        var positionOfNewData = 0;
                        using (NativeWriter nativeWriter = new NativeWriter(new FileStream(casPath, FileMode.Open)))
                        {
                            nativeWriter.Write(originalCASArray);
                            // write the new data to end of the file (this should be fine)
                            positionOfNewData = (int)nativeWriter.BaseStream.Position;
                            nativeWriter.Write(resData);
                        }

                        using (var f = new FileStream(casPath, FileMode.Open, FileAccess.Read))
                        {
                            using (NativeReader reader = new NativeReader(f))
                            {
                                byte[] array = null;
                                using (CasReader casReader = new CasReader(reader.CreateViewStream(positionOfNewData, resData.Length)))
                                {
                                    array = casReader.Read();
                                }
                                if (array == null)
                                {
                                    throw new Exception("Array not found in CAS");
                                }
                            }
                        }

                        parent.Logger.Log("Writing new asset entry for RES (" + modRES.Key + ")");
                        Debug.WriteLine("Writing new asset entry for RES (" + modRES.Key + ")");

                        var sb_cas_offset_position = originalEntry.SB_CAS_Offset_Position;
                        var sb_sha1_position = originalEntry.SB_Sha1_Position;
                        var sb_original_size_position = originalEntry.SB_OriginalSize_Position;

                        var sbpath = parent.fs.ResolvePath(originalEntry.SBFileLocation);
                        sbpath = sbpath.Replace("\\patch", "\\ModData\\Patch");
                        byte[] arrayOfSB = null;
                        using (NativeReader nativeReader = new NativeReader(new FileStream(sbpath, FileMode.Open)))
                        {
                            arrayOfSB = nativeReader.ReadToEnd();
                        }
                        File.Delete(sbpath);
                        using (NativeWriter nativeWriter = new NativeWriter(new FileStream(sbpath, FileMode.OpenOrCreate)))
                        {
                            nativeWriter.Write(arrayOfSB);
                            nativeWriter.BaseStream.Position = sb_cas_offset_position;
                            nativeWriter.Write((uint)positionOfNewData, Endian.Big);
                            nativeWriter.Flush();

                            nativeWriter.Write((uint)resData.Length, Endian.Big);
                            nativeWriter.Flush();

                            nativeWriter.BaseStream.Position = sb_sha1_position;
                            nativeWriter.Write(modRES.Value.Sha1);
                            nativeWriter.Flush();

                            nativeWriter.BaseStream.Position = sb_original_size_position;
                            nativeWriter.Write(Convert.ToUInt32(modRES.Value.OriginalSize), Endian.Little);
                            nativeWriter.Flush();
                        }

                    }
                }
                foreach (var modChunk in parent.modifiedChunks)
                {
                    var originalEntry = AssetManager.Instance.GetChunkEntry(modChunk.Value.Id);
                    if (originalEntry != null)
                    {
                        var originalDataLength = AssetManager.Instance.GetChunk(originalEntry).Length;
                        var data = parent.archiveData[modChunk.Value.Sha1].Data;

                        var casPath = originalEntry.ExtraData.CasPath.Replace("native_data"
                            , AssetManager.Instance.fs.BasePath + "ModData\\Data");
                        casPath = casPath.Replace("native_patch"
                            , AssetManager.Instance.fs.BasePath + "ModData\\Patch");


                        parent.Logger.Log("Writing new asset entry for Chunk (" + modChunk.Key + ")");
                        Debug.WriteLine("Writing new asset entry for Chunk (" + modChunk.Key + ")");

                        byte[] originalCASArray = null;
                        using (NativeReader readerOfCas = new NativeReader(new FileStream(casPath, FileMode.Open)))
                        {
                            originalCASArray = readerOfCas.ReadToEnd();
                        }

                        var new_position = 0;
                        using (NativeWriter nativeWriter = new NativeWriter(new FileStream(casPath, FileMode.Open)))
                        {
                            nativeWriter.Write(originalCASArray);
                            new_position = (int)nativeWriter.BaseStream.Position;
                            nativeWriter.Write(data);

                        }

                        var sbpath = parent.fs.ResolvePath(originalEntry.SBFileLocation);
                        sbpath = sbpath.Replace("\\patch", "\\ModData\\Patch");
                        byte[] arrayOfSB = null;
                        using (NativeReader nativeReader = new NativeReader(new FileStream(sbpath, FileMode.Open)))
                        {
                            arrayOfSB = nativeReader.ReadToEnd();
                        }
                        File.Delete(sbpath);
                        using (NativeWriter nativeWriter = new NativeWriter(new FileStream(sbpath, FileMode.OpenOrCreate)))
                        {
                            nativeWriter.Write(arrayOfSB);

                            nativeWriter.BaseStream.Position = originalEntry.SB_CAS_Offset_Position;
                            nativeWriter.Write((uint)new_position, Endian.Big);
                            nativeWriter.Flush();

                            nativeWriter.BaseStream.Position = originalEntry.SB_CAS_Size_Position;
                            nativeWriter.Write((uint)data.Length, Endian.Big);
                            nativeWriter.Flush();

                            nativeWriter.BaseStream.Position = originalEntry.SB_Sha1_Position;
                            nativeWriter.Write(modChunk.Value.Sha1);
                            nativeWriter.Flush();

                            //nativeWriter.BaseStream.Position = originalEntry.SB_OriginalSize_Position - 4;
                            //nativeWriter.Write((uint)data.Length, Endian.Little);
                            //nativeWriter.Flush();

                            //nativeWriter.BaseStream.Position = originalEntry.SB_OriginalSize_Position;
                            //nativeWriter.Write(Convert.ToUInt32(modChunk.Value.Size), Endian.Little);
                            //nativeWriter.Flush();
                        }
            }
                }
                */
            }
            catch (Exception e)
            {
                throw e;
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
                AssetManager.Instance.ModCASFiles.Add(CasFileCount, "/native_data/Patch/" + catalogInfo.Name + "/cas_" + num.ToString("D2") + ".cas");
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
                num = (uint)((int)(sbyte)b[i] ^ (int)(16777619 * num));
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
