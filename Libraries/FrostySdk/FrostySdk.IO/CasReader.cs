using FMT.FileTools;
using FrostySdk.Managers;
using FrostySdk.ThirdParty;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace FrostySdk.IO
{
    public class CasReader : NativeReader
    {
        private Stream deltaStream;

        public AssetEntry AssociatedAssetEntry = null;

        public CasReader(Stream inBaseStream, Stream inDeltaStream = null)
            : base(inBaseStream)
        {
            deltaStream = inDeltaStream;
        }

        public CasReader(Stream inStream, byte[] inEncryptionKey, uint actualSize)
            : base(DecryptStream(inStream, inEncryptionKey, actualSize))
        {
        }

        public CasReader(Stream inBaseStream, byte[] inBaseEncryptionKey, uint actualBaseSize, Stream inDeltaStream, byte[] inDeltaEncryptionKey, uint actualPatchSize)
            : this(DecryptStream(inBaseStream, inBaseEncryptionKey, actualBaseSize), DecryptStream(inDeltaStream, inDeltaEncryptionKey, actualPatchSize))
        {
        }

        public CasReader(Stream inBaseStream, byte[] inBaseEncryptionKey, uint actualBaseSize, Stream inDeltaStream)
            : this(DecryptStream(inBaseStream, inBaseEncryptionKey, actualBaseSize), inDeltaStream)
        {
        }

        public byte[] Read()
        {
            MemoryStream memoryStream = new MemoryStream();
            if (deltaStream != null)
            {
                byte[] array = ReadPatched();
                memoryStream.Write(array, 0, array.Length);
            }
            if (stream != null)
            {
                while (Position < Length)
                {
                    byte[] array2 = ReadBlock();
                    if (array2 == null)
                    {
                        break;
                    }
                    memoryStream.Write(array2, 0, array2.Length);
                }
            }
            return memoryStream.ToArray();
        }

        public int LastCompressedBufferSize { get; set; }
        public int LastCompressedSize { get; set; }

        public byte[] ReadBlock()
        {
            int originalSize = ReadInt(Endian.Big);
            // OODLE FIFA21 = 28953 / 28687 = 25
            // OODLE FIFA23 = 28697
            ushort compressTypeBig = ReadUShort(Endian.Big);
            Position -= 2;
            ushort compressType = ReadUShort();
            LastCompressedBufferSize = ReadUShort(Endian.Big);
            int num4 = (compressType & 0xFF00) >> 8;
            LastCompressedSize = num4;
            bool useDictionary = false;
            byte[] result = null;
            if ((num4 & 0xF) != 0)
            {
                LastCompressedBufferSize = ((num4 & 0xF) << 16) + LastCompressedBufferSize;
            }
            if ((originalSize & 4278190080u) != 0L)
            {
                originalSize &= 0xFFFFFF;
                useDictionary = true;
            }
            bool unobfuscateCode = ((compressType >> 7) & 1) != 0;
            bool unobfuscate = unobfuscateCode && ProfileManager.DataVersion == 20180914;
            //switch (compressTypeBig)
            //{
            //	case 3952:
            //		if (AssociatedAssetEntry != null) AssociatedAssetEntry.OriginalCompressionType = CompressionType.Oodle;
            //		return result = DecompressOodle(LastCompressedBufferSize, originalSize, unobfuscate);
            //}

            var sw = (ushort)(compressType & 0x7F);
            switch (sw)
            {
                case 0:
                    result = ReadUncompressed(LastCompressedBufferSize, unobfuscate);
                    break;
                case 2:
                    result = DecompressBlockZLib(LastCompressedBufferSize, originalSize);
                    break;
                case 9:
                    if (AssociatedAssetEntry != null) AssociatedAssetEntry.OriginalCompressionType = CompressionType.LZ4;
                    result = DecompressBlockLZ4(LastCompressedBufferSize, originalSize);
                    break;
                case 15:
                    if (AssociatedAssetEntry != null) AssociatedAssetEntry.OriginalCompressionType = CompressionType.ZStd;
                    result = DecompressBlockZStd(LastCompressedBufferSize, originalSize, useDictionary, unobfuscate);
                    break;
                // OODLE
                case 17:
                case 21:
                case 25:
                    if (AssociatedAssetEntry != null) AssociatedAssetEntry.OriginalCompressionType = CompressionType.Oodle;
                    result = DecompressOodle(LastCompressedBufferSize, originalSize, unobfuscate);
                    break;
                default:
                    if (AssociatedAssetEntry != null) AssociatedAssetEntry.OriginalCompressionType = CompressionType.Oodle;
                    result = DecompressOodle(LastCompressedBufferSize, originalSize, unobfuscate);
                    break;
            }
            return result;
        }

        private byte[] ReadUncompressed(int bufferSize, bool unobfuscate)
        {
            byte[] array = ReadBytes(bufferSize);
            if (unobfuscate)
            {
                byte[] key = KeyManager.Instance.GetKey("Key3");
                for (int i = 0; i < bufferSize; i++)
                {
                    array[i] ^= key[i & 0x3FFF];
                }
            }
            return array;
        }

        private byte[] DecompressOodle(int bufferSize, int decompressedSize, bool unobfuscate)
        {
            byte[] array = ReadBytes(bufferSize);
            if (unobfuscate)
            {
                byte[] key = KeyManager.Instance.GetKey("Key3");
                for (int i = 0; i < bufferSize; i++)
                {
                    array[i] ^= key[i & 0x3FFF];
                }
            }
            GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            byte[] array2 = new byte[decompressedSize];
            GCHandle gCHandle2 = GCHandle.Alloc(array2, GCHandleType.Pinned);
            Oodle.Decompress(gCHandle.AddrOfPinnedObject(), array.Length, gCHandle2.AddrOfPinnedObject(), array2.Length, 0, 0, 0L, 0L, 0L, 0L, 0L, 0L, 0L);
            gCHandle.Free();
            gCHandle2.Free();
            return array2;
        }

        private byte[] DecompressBlockZStd(int bufferSize, int decompressedSize, bool useDictionary, bool unobfuscate)
        {
            ZStd.Bind();


            byte[] array = ReadBytes(bufferSize);
            if (unobfuscate)
            {
                byte[] key = KeyManager.Instance.GetKey("Key3");
                for (int i = 0; i < bufferSize; i++)
                {
                    array[i] ^= key[i & 0x3FFF];
                }
            }
            GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            byte[] array2 = new byte[decompressedSize];
            GCHandle gCHandle2 = GCHandle.Alloc(array2, GCHandleType.Pinned);
            ulong num = 0uL;
            if (useDictionary)
            {
                byte[] dictionary = ZStd.GetDictionary();
                IntPtr intPtr = ZStd.Create();
                GCHandle gCHandle3 = GCHandle.Alloc(dictionary, GCHandleType.Pinned);
                if (gCHandle3 != null)
                {
                    IntPtr dict = ZStd.CreateDigestedDict(gCHandle3.AddrOfPinnedObject(), dictionary.Length);
                    num = ZStd.DecompressUsingDict(intPtr, gCHandle2.AddrOfPinnedObject(), (ulong)array2.Length, gCHandle.AddrOfPinnedObject(), (ulong)array.Length, dict);
                    gCHandle3.Free();
                    ZStd.FreeDigestedDict(dict);
                }
                ZStd.Free(intPtr);
            }
            else
            {
                num = ZStd.Decompress(gCHandle2.AddrOfPinnedObject(), (ulong)array2.Length, gCHandle.AddrOfPinnedObject(), (ulong)array.Length);
            }
            //if (ZStd.IsError(num))
            //{
            //	throw new InvalidDataException(Marshal.PtrToStringAnsi(ZStd.GetErrorName(num)));
            //}
            gCHandle.Free();
            gCHandle2.Free();
            return array2;
        }

        private byte[] DecompressBlockZLib(int bufferSize, int decompressedSize)
        {
            byte[] array = ReadBytes(bufferSize);
            GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            byte[] array2 = new byte[decompressedSize];
            GCHandle gCHandle2 = GCHandle.Alloc(array2, GCHandleType.Pinned);
            ZLib.ZStream structure = default(ZLib.ZStream);
            structure.avail_in = (uint)array.Length;
            structure.avail_out = (uint)array2.Length;
            structure.next_in = gCHandle.AddrOfPinnedObject();
            structure.next_out = gCHandle2.AddrOfPinnedObject();
            IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            Marshal.StructureToPtr(structure, intPtr, fDeleteOld: true);
            ZLib.InflateInit(intPtr, "1.2.11", Marshal.SizeOf<ZLib.ZStream>());
            ZLib.Inflate(intPtr, 0);
            ZLib.InflateEnd(intPtr);
            gCHandle.Free();
            gCHandle2.Free();
            Marshal.FreeHGlobal(intPtr);
            return array2;
        }

        private byte[] DecompressBlockLZ4(int bufferSize, int decompressedSize)
        {
            GCHandle gCHandle = GCHandle.Alloc(ReadBytes(bufferSize), GCHandleType.Pinned);
            byte[] array = new byte[decompressedSize];
            GCHandle gCHandle2 = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                LZ4.Decompress(gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), array.Length);
            }
            catch
            {

            }
            gCHandle.Free();
            gCHandle2.Free();
            return array;
        }

        private byte[] ReadPatched()
        {
            MemoryStream memoryStream = new MemoryStream();
            using (CasReader casReader = new CasReader(deltaStream))
            {
                while (casReader.Position < casReader.Length)
                {
                    uint num = casReader.ReadUInt(Endian.Big);
                    switch (((int)num & -268435456) >> 28)
                    {
                        case 0:
                            {
                                int num8 = (int)(num & 0xFFFFFFF);
                                while (num8-- > 0)
                                {
                                    byte[] array3 = ReadBlock();
                                    memoryStream.Write(array3, 0, array3.Length);
                                }
                                break;
                            }
                        case 1:
                            {
                                int num3 = (int)(num & 0xFFFFFFF);
                                using (MemoryStream memoryStream2 = new MemoryStream(ReadBlock()))
                                {
                                    byte[] array = null;
                                    while (num3-- > 0)
                                    {
                                        ushort num4 = casReader.ReadUShort(Endian.Big);
                                        int num5 = casReader.ReadUShort(Endian.Big);
                                        int num6 = (int)(num4 - memoryStream2.Position);
                                        array = new byte[num6];
                                        memoryStream2.Read(array, 0, num6);
                                        memoryStream.Write(array, 0, num6);
                                        array = casReader.ReadBlock();
                                        if (array != null)
                                        {
                                            memoryStream.Write(array, 0, array.Length);
                                        }
                                        memoryStream2.Position += num5;
                                    }
                                    array = new byte[(int)(memoryStream2.Length - memoryStream2.Position)];
                                    memoryStream2.Read(array, 0, array.Length);
                                    memoryStream.Write(array, 0, array.Length);
                                }
                                break;
                            }
                        case 2:
                            {
                                using (MemoryStream memoryStream3 = new MemoryStream(ReadBlock()))
                                {
                                    int num9 = (int)(num & 0xFFFFFFF);
                                    int num10 = casReader.ReadUShort(Endian.Big) + 1;
                                    byte[] array4 = new byte[num10];
                                    long position = casReader.Position;
                                    int num11 = 0;
                                    while (casReader.Position - position < num9)
                                    {
                                        ushort num12 = casReader.ReadUShort(Endian.Big);
                                        int num13 = casReader.ReadByte();
                                        int numBytes = casReader.ReadByte();
                                        num11 += memoryStream3.Read(array4, num11, (int)(num12 - memoryStream3.Position));
                                        memoryStream3.Position += num13;
                                        num11 += casReader.Read(array4, num11, numBytes);
                                    }
                                    memoryStream3.Read(array4, num11, num10 - num11);
                                    memoryStream.Write(array4, 0, array4.Length);
                                }
                                break;
                            }
                        case 3:
                            {
                                int num7 = (int)(num & 0xFFFFFFF);
                                while (num7-- > 0)
                                {
                                    byte[] array2 = casReader.ReadBlock();
                                    memoryStream.Write(array2, 0, array2.Length);
                                }
                                break;
                            }
                        case 4:
                            {
                                int num2 = (int)(num & 0xFFFFFFF);
                                while (num2-- > 0)
                                {
                                    ReadBlock();
                                }
                                break;
                            }
                    }
                }
            }
            deltaStream = null;
            return memoryStream.ToArray();
        }

        private static Stream DecryptStream(Stream inStream, byte[] encryptionKey, uint actualSize)
        {
            if (encryptionKey == null)
            {
                return inStream;
            }
            int num = (int)inStream.Length;
            byte[] array = new byte[num];
            inStream.Read(array, 0, num);
            using (Aes aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.IV = encryptionKey;
                ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
                using (MemoryStream stream = new MemoryStream(array))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read))
                    {
                        cryptoStream.Read(array, 0, num);
                    }
                }
            }
            inStream.Dispose();
            Array.Resize(ref array, (int)actualSize);
            return new MemoryStream(array);
        }
    }
}
