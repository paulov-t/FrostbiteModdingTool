using FMT.FileTools;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace FrostySdk.IO
{
    public class BinarySbReader : DbReader
    {
        public const uint BinarySbMagic = 2641989333u;

        private uint totalCount;

        private uint ebxCount;

        private uint resCount;

        private uint chunkCount;

        private uint stringsOffset;

        private uint metaOffset;

        private uint metaSize;

        private List<FMT.FileTools.Sha1> sha1 = new List<FMT.FileTools.Sha1>();

        private bool containsUncompressedData;

        private long bundleOffset;

        public uint TotalCount => totalCount;

        public BinarySbReader(Stream inStream, long inBundleOffset, IDeobfuscator inDeobfuscator)
            : base(inStream, inDeobfuscator)
        {
            bundleOffset = inBundleOffset;
        }

        public BinarySbReader(Stream inBaseStream, Stream inDeltaStream, IDeobfuscator inDeobfuscator)
            : base(inBaseStream, inDeobfuscator)
        {
            if (inDeltaStream == null)
            {
                return;
            }
            Stream stream = PatchStream(inDeltaStream);
            inDeltaStream.Dispose();
            inDeltaStream = null;
            if (stream != null)
            {
                if (base.stream != null)
                {
                    base.stream.Dispose();
                }
                base.stream = stream;
                //streamLength = base.stream.Length;
            }
            bundleOffset = 0L;
        }

        public override DbObject ReadDbObject()
        {
            uint size = ReadUInt(Endian.Big) + 4;
            uint num2 = ReadUInt(Endian.Big) ^ 0x7065636E;
            bool flag = true;
            if (num2 == 3280507699u || num2 == 3286619587u)
            {
                flag = false;
            }
            totalCount = ReadUInt(Endian.Big);
            ebxCount = ReadUInt(Endian.Big);
            resCount = ReadUInt(Endian.Big);
            chunkCount = ReadUInt(Endian.Big);
            stringsOffset = ReadUInt(Endian.Big) - 36;
            metaOffset = ReadUInt(Endian.Big) - 36;
            metaSize = ReadUInt(Endian.Big);
            byte[] array = (ProfileManager.DataVersion == 20181207 || ProfileManager.DataVersion == 20190905) ? ReadToEnd() : ReadBytes((int)(size - Position));
            switch (num2)
            {
                case 3280507699u:
                    flag = false;
                    break;
                case 3286619587u:
                    {
                        flag = false;
                        byte[] key = KeyManager.Instance.GetKey("Key2");
                        using (Aes aes = Aes.Create())
                        {
                            aes.Key = key;
                            aes.IV = key;
                            aes.Padding = PaddingMode.None;
                            ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
                            using (MemoryStream stream = new MemoryStream(array))
                            {
                                using (CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read))
                                {
                                    cryptoStream.Read(array, 0, array.Length);
                                }
                            }
                        }
                        break;
                    }
            }
            DbObject dbObject = new DbObject(new Dictionary<string, object>());
            using (DbReader dbReader = new DbReader(new MemoryStream(array), null))
            {
                for (int i = 0; i < totalCount; i++)
                {
                    sha1.Add(flag ? dbReader.ReadSha1() : Sha1.Zero);
                }
                dbObject.AddValue("ebx", new DbObject(ReadEbx(dbReader)));
                dbObject.AddValue("res", new DbObject(ReadRes(dbReader)));
                dbObject.AddValue("chunks", new DbObject(ReadChunks(dbReader)));
                dbObject.AddValue("dataOffset", (int)(size - 4));
                if (chunkCount != 0)
                {
                    dbReader.Position = metaOffset + 4;
                    dbObject.AddValue("chunkMeta", dbReader.ReadDbObject());
                }
            }
            Position = size;
            if (Position == Length)
            {
                return dbObject;
            }
            if (num2 == 3978096056u)
            {
                return dbObject;
            }
            ReadDataBlock(dbObject.GetValue<DbObject>("ebx"));
            ReadDataBlock(dbObject.GetValue<DbObject>("res"));
            ReadDataBlock(dbObject.GetValue<DbObject>("chunks"));
            return dbObject;
        }

        private Stream PatchStream(Stream deltaStream)
        {
            deltaStream.Position += 8L;
            MemoryStream memoryStream = new MemoryStream();
            using (NativeReader nativeReader = new NativeReader(deltaStream))
            {
                uint num = nativeReader.ReadUInt(Endian.Big);
                nativeReader.ReadUInt(Endian.Big);
                memoryStream.Write(nativeReader.ReadBytes(4), 0, 4);
                if (stream != null)
                {
                    ReadUInt(Endian.Big);
                }
                while (nativeReader.Position <= num + 15)
                {
                    uint num2 = nativeReader.ReadUInt(Endian.Big);
                    uint num3 = (uint)((int)num2 & -16777216) >> 24;
                    int num4 = (int)(num2 & 0xFFFFFF);
                    switch (num3)
                    {
                        case 0u:
                            memoryStream.Write(ReadBytes(num4), 0, num4);
                            break;
                        case 64u:
                            Position += num4;
                            break;
                        case 128u:
                            memoryStream.Write(nativeReader.ReadBytes(num4), 0, num4);
                            break;
                    }
                }
                using (CasReader casReader = new CasReader(stream, deltaStream))
                {
                    byte[] array = casReader.Read();
                    memoryStream.Write(array, 0, array.Length);
                }
            }
            memoryStream.Position = 0L;
            containsUncompressedData = true;
            return memoryStream;
        }

        private void ReadDataBlock(DbObject list)
        {
            foreach (DbObject item in list)
            {
                item.AddValue("offset", bundleOffset + Position);
                long num = item.GetValue("originalSize", 0L);
                long num2 = 0L;
                if (containsUncompressedData)
                {
                    num2 = num;
                    item.AddValue("data", ReadBytes((int)num));
                }
                else
                {
                    while (num > 0)
                    {
                        int num3 = ReadInt(Endian.Big);
                        ushort num4 = ReadUShort();
                        int num5 = ReadUShort(Endian.Big);
                        int num6 = (num4 & 0xFF00) >> 8;
                        if ((num6 & 0xF) != 0)
                        {
                            num5 = ((num6 & 0xF) << 16) + num5;
                        }
                        if ((num3 & 4278190080u) != 0L)
                        {
                            num3 &= 0xFFFFFF;
                        }
                        num -= num3;
                        if ((ushort)(num4 & 0x7F) == 0)
                        {
                            num5 = num3;
                        }
                        num2 += num5 + 8;
                        Position += num5;
                    }
                }
                item.AddValue("size", num2);
                item.AddValue("sb", true);
            }
        }

        private List<object> ReadEbx(NativeReader reader)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < ebxCount; i++)
            {
                DbObject dbObject = new DbObject(new Dictionary<string, object>());
                uint num = reader.ReadUInt(Endian.Big);
                uint num2 = reader.ReadUInt(Endian.Big);
                long position = reader.Position;
                reader.Position = 4 + stringsOffset + num;
                dbObject.AddValue("sha1", sha1[i]);
                dbObject.AddValue("name", reader.ReadNullTerminatedString());
                dbObject.AddValue("nameHash", Fnv1.HashString(dbObject.GetValue<string>("name")));
                dbObject.AddValue("originalSize", num2);
                list.Add(dbObject);
                reader.Position = position;
            }
            return list;
        }

        private List<object> ReadRes(NativeReader reader)
        {
            List<object> list = new List<object>();
            int num = (int)ebxCount;
            for (int i = 0; i < resCount; i++)
            {
                DbObject dbObject = new DbObject(new Dictionary<string, object>());
                uint num2 = reader.ReadUInt(Endian.Big);
                uint num3 = reader.ReadUInt(Endian.Big);
                long position = reader.Position;
                reader.Position = 4 + stringsOffset + num2;
                dbObject.AddValue("sha1", sha1[num++]);
                dbObject.AddValue("name", reader.ReadNullTerminatedString());
                dbObject.AddValue("nameHash", Fnv1.HashString(dbObject.GetValue<string>("name")));
                dbObject.AddValue("originalSize", num3);
                list.Add(dbObject);
                reader.Position = position;
            }
            foreach (DbObject item in list)
            {
                item.AddValue("resType", reader.ReadUInt(Endian.Big));
            }
            foreach (DbObject item2 in list)
            {
                item2.AddValue("resMeta", reader.ReadBytes(16));
            }
            foreach (DbObject item3 in list)
            {
                //item3.AddValue("resRid", reader.ReadLong(Endian.Big));
                item3.AddValue("resRid", reader.ReadULong(Endian.Big));
            }
            return list;
        }

        private List<object> ReadChunks(NativeReader reader)
        {
            List<object> list = new List<object>();
            int num = (int)(ebxCount + resCount);
            for (int i = 0; i < chunkCount; i++)
            {
                DbObject dbObject = new DbObject(new Dictionary<string, object>());
                Guid guid = reader.ReadGuid(Endian.Big);
                uint num2 = reader.ReadUInt(Endian.Big);
                uint num3 = reader.ReadUInt(Endian.Big);
                long num4 = (num2 & 0xFFFF) | num3;
                dbObject.AddValue("id", guid);
                dbObject.AddValue("sha1", sha1[num + i]);
                dbObject.AddValue("logicalOffset", num2);
                dbObject.AddValue("logicalSize", num3);
                dbObject.AddValue("originalSize", num4);
                list.Add(dbObject);
            }
            return list;
        }
    }
}
