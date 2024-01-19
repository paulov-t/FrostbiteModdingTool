#pragma warning disable SYSLIB0011 // Type or member is obsolete
#pragma warning disable SYSLIB0021 // Type or member is obsolete
using FMT.FileTools;
using FrostySdk.Ebx;
using FrostySdk.Managers;
using FrostySdk.Resources;
using FrostySdk.ThirdParty;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace FrostySdk
{
    public static class Utils
    {
        private static Dictionary<int, string> strings = new Dictionary<int, string>();

        public static int MaxBufferSize
        {
            get
            {
                return ProfileManager.LoadedProfile.MaxBufferSize.HasValue ? ProfileManager.LoadedProfile.MaxBufferSize.Value : 65536;
            }
        }

        public static Guid GenerateDeterministicGuid(IEnumerable<object> objects, string type, Guid fileGuid)
        {
            return GenerateDeterministicGuid(objects, TypeLibrary.GetType(type), fileGuid);
        }

        public static Guid GenerateDeterministicGuid(IEnumerable<object> objects, Type type, Guid fileGuid)
        {
            Guid empty = Guid.Empty;
            int num = 0;
            foreach (object @object in objects)
            {
                _ = @object;
                num++;
            }
            while (true)
            {
                using (NativeWriter nativeWriter = new NativeWriter(new MemoryStream()))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        new BinaryFormatter().Serialize(memoryStream, type);
                        nativeWriter.Write(fileGuid);
                        nativeWriter.Write(++num);
                        nativeWriter.Write(memoryStream.ToArray());
                    }
                    using (MD5 mD = new MD5CryptoServiceProvider())
                    {
                        empty = new Guid(mD.ComputeHash(((MemoryStream)nativeWriter.BaseStream).ToArray()));
                        bool flag = false;
                        foreach (dynamic object2 in objects)
                        {
                            if (((AssetClassGuid)object2.GetInstanceGuid()).ExportedGuid == empty)
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            return empty;
                        }
                    }
                }
            }
        }

        //public static Sha1 GenerateSha1(byte[] buffer)
        //{
        //	using (SHA1Managed sHA1Managed = new SHA1Managed())
        //	{
        //		return new FMT.FileTools.Sha1(sHA1Managed.ComputeHash(buffer));
        //	}
        //}

        public static ulong GenerateResourceId()
        {
            Random random = new Random();
            ulong num = 0uL;
            ulong num2 = (ulong)(-1L - (long)num);
            ulong num3;
            do
            {
                byte[] array = new byte[8];
                random.NextBytes(array);
                num3 = (ulong)BitConverter.ToInt64(array, 0);
            }
            while (num3 > (ulong)(-1L - (long)((ulong.MaxValue % num2 + 1) % num2)));
            return (num3 % num2 + num) | 1;
        }

        public static int HashString(string strToHash, bool lowercase = false)
        {
            if (lowercase)
            {
                strToHash = strToHash.ToLower();
            }
            return Fnv1.HashString(strToHash);
        }

        public static string GetString(int hash)
        {
            if (strings.Count == 0 && File.Exists("strings.txt"))
            {
                using (NativeReader nativeReader = new NativeReader(new FileStream("strings.txt", FileMode.Open, FileAccess.Read)))
                {
                    while (nativeReader.Position < nativeReader.Length)
                    {
                        string text = nativeReader.ReadLine();
                        int key = Fnv1.HashString(text);
                        if (!strings.ContainsKey(key))
                        {
                            strings.Add(key, text);
                        }
                    }
                }
            }
            if (!strings.ContainsKey(hash))
            {
                return "0x" + hash.ToString("x8");
            }
            return strings[hash];
        }

        public static string ReverseString(string str)
        {
            string text = "";
            foreach (char item in str.Reverse())
            {
                text += item.ToString();
            }
            return text;
        }

        public static byte[] CompressTexture(byte[] inData, Texture texture, CompressionType compressionOverride = CompressionType.Default)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                uint num = 0u;
                uint num2 = (uint)inData.Length;
                if (texture.MipCount > 1 && inData.Length > 65536)
                {
                    int num3 = 0;
                    while (num2 > 65536 && num3 < texture.FirstMip)
                    {
                        num += texture.MipSizes[num3];
                        num2 -= texture.MipSizes[num3++];
                    }
                }
                if (texture.LogicalOffset != num)
                {
                    texture.LogicalOffset = num;
                    texture.LogicalSize = num2;
                }
                byte[] array = null;
                if (num != 0)
                {
                    array = new byte[num];
                    Array.Copy(inData, array, num);
                    array = CompressFile(array, texture, ResourceType.Invalid, compressionOverride);
                    memoryStream.Write(array, 0, array.Length);
                    texture.RangeStart = (uint)memoryStream.Length;
                }
                array = new byte[num2];
                Array.Copy(inData, num, array, 0L, num2);
                array = CompressFile(array, texture, ResourceType.Invalid, compressionOverride, num);
                memoryStream.Write(array, 0, array.Length);
                texture.RangeEnd = (uint)memoryStream.Length;
                if (texture.RangeStart == 0)
                {
                    texture.RangeEnd = 0u;
                }
                return memoryStream.ToArray();
            }
        }
        /*
		public static byte[] CompressFile(byte[] inData, Texture texture = null, ResourceType resType = ResourceType.Invalid, CompressionType compressionOverride = CompressionType.Default, uint offset = 0u, uint oodleCO = 8u)
		{
			CompressionType compressionType = compressionOverride;
			if (resType == ResourceType.SwfMovie)
			{
				compressionType = CompressionType.None;
			}
			if (compressionOverride == CompressionType.Default)
			{
				compressionType = CompressionType.LZ4;
				if (ProfilesLibrary.DataVersion == 20190905)
				{
					compressionType = CompressionType.Oodle;
				}
				//else if (ProfilesLibrary.DataVersion == 20170321 || ProfilesLibrary.DataVersion == 20160927 || ProfilesLibrary.DataVersion == 20171117 || ProfilesLibrary.DataVersion == 20180807)
				//{
				//	compressionType = CompressionType.ZStd;
				//}
				//else if (ProfilesLibrary.DataVersion == 20141118 || ProfilesLibrary.DataVersion == 20141117)
				//{
				//	compressionType = CompressionType.ZLib;
				//}
				//else if (ProfilesLibrary.DataVersion == 20150223)
				//{
				//	compressionType = CompressionType.None;
				//	if (texture != null)
				//	{
				//		compressionType = CompressionType.LZ4;
				//	}
				//}
				else if (ProfilesLibrary.DataVersion == 20180914
					|| ProfilesLibrary.IsFIFA20DataVersion() // FIFA 20

					) 
				{
					compressionType = CompressionType.ZStd;
				}
				else if (ProfilesLibrary.DataVersion == 20180628)
				{
					compressionType = CompressionType.ZStd;
				}
				// Confirmed that EBX are ZStd
				else if (ProfilesLibrary.IsFIFA21DataVersion()) // FIFA 21
				{
                    compressionType = CompressionType.ZStd;
                }
				// Confirmed that EBX are ZStd
                //else if (ProfilesLibrary.IsMadden21DataVersion()) // MADDEN 21
                //{
                //    compressionType = CompressionType.ZStd;
                //}
            }
			MemoryStream memoryStream = new MemoryStream();
			NativeWriter nativeWriter = new NativeWriter(memoryStream);
			byte[] array = null;
			using (NativeReader nativeReader = new NativeReader(new MemoryStream(inData)))
			{
				nativeReader.Position = 0L;
				long num = nativeReader.Length - nativeReader.Position;
				long num2 = 0L;
				long num3 = 0L;
				bool uncompressed = false;
				while (num > 0)
				{
					int originalSize = (int)((num > MaxBufferSize) ? MaxBufferSize : num);
					array = nativeReader.ReadBytes(originalSize);
					ushort compressCode = 0;
					ulong compressedSize = 0uL;
					byte[] compBuffer = null;
					switch (compressionType)
					{
					case CompressionType.ZStd:
						compressedSize = CompressZStd(array, out compBuffer, out compressCode, ref uncompressed);
						break;
					case CompressionType.ZLib:
						compressedSize = CompressZlib(array, out compBuffer, out compressCode, ref uncompressed);
						break;
					case CompressionType.LZ4:
						compressedSize = CompressLZ4(array, out compBuffer, out compressCode, ref uncompressed);
						break;
					case CompressionType.None:
						compressedSize = CompressNone(array, out compBuffer, out compressCode);
						break;
					case CompressionType.Oodle:
						compressedSize = CompressOodle(array, out compBuffer, out compressCode, ref uncompressed, oodleCO);
						break;
					}
					if (uncompressed)
					{
						uncompressed = false;
						compressionType = CompressionType.None;
						nativeReader.Position = 0L;
						nativeWriter.BaseStream.Position = 0L;
						num = nativeReader.Length - nativeReader.Position;
						num2 = 0L;
						num3 = 0L;
					}
					else
					{
						compressCode = (ushort)(compressCode | (ushort)((compressedSize & 0xF0000) >> 16));
						nativeWriter.Write(originalSize, Endian.Big);
						nativeWriter.Write(compressCode, Endian.Big);
						nativeWriter.Write((ushort)compressedSize, Endian.Big);
						nativeWriter.Write(compBuffer, 0, (int)compressedSize);
						num -= originalSize;
						num2 += originalSize;
						num3 += (long)(compressedSize + 8);
						if (texture != null && texture.MipCount > 1)
						{
							if (num2 + offset == texture.MipSizes[0])
							{
								uint num8 = texture.FirstMipOffset = (texture.SecondMipOffset = (uint)num3);
							}
							else if (num2 + offset == texture.MipSizes[0] + texture.MipSizes[1])
							{
								texture.SecondMipOffset = (uint)num3;
							}
						}
					}
				}
			}
			return memoryStream.ToArray();
		}
		*/

        public static byte[] CompressFile(
            byte[] inData
            , Texture texture = null
            , ResourceType resType = ResourceType.Invalid
            , CompressionType compressionOverride = CompressionType.Default
            , uint offset = 0u
            , uint oodleCO = 8u
            )
        {
            int maxBufferSize = Utils.MaxBufferSize;
            CompressionType compressionType = compressionOverride;
            if (resType == ResourceType.SwfMovie)
            {
                compressionType = CompressionType.None;
            }
            if (compressionOverride == CompressionType.Default)
            {
                compressionType = CompressionType.Oodle;
            }
            MemoryStream memoryStream = new MemoryStream();
            FileWriter outputWriter = new FileWriter(memoryStream);
            FileReader inputReader = new FileReader(new MemoryStream(inData));
            long remainingByteCount = inputReader.Length - inputReader.Position;
            long totalBytesRead = 0L;
            long totalBytesWritten = 0L;
            bool uncompressed = false;
            while (remainingByteCount > 0)
            {
                int bufferSize = (int)((remainingByteCount > maxBufferSize) ? maxBufferSize : remainingByteCount);
                byte[] array = inputReader.ReadBytes(bufferSize);
                ushort compressCode = 0;
                ulong compressedSize = 0uL;
                byte[] compBuffer = null;
                bool pooledBuffer = false;
                try
                {
                    switch (compressionType)
                    {
                        case CompressionType.ZStd:
                            pooledBuffer = true;
                            compressedSize = Utils.CompressZStd(array, out compBuffer, out compressCode, ref uncompressed);
                            break;
                        case CompressionType.LZ4:
                            pooledBuffer = true;
                            compressedSize = Utils.CompressLZ4(array, out compBuffer, out compressCode, ref uncompressed);
                            break;
                        case CompressionType.None:
                            pooledBuffer = false;
                            compressedSize = Utils.CompressNone(array, out compBuffer, out compressCode);
                            break;
                        case CompressionType.Oodle:
                            pooledBuffer = true;
                            compressedSize = Oodle.CompressOodle(array, out compBuffer, out compressCode, ref uncompressed, oodleCO);
                            break;
                    }
                    if (uncompressed)
                    {
                        uncompressed = false;
                        compressionType = CompressionType.None;
                        inputReader.Position = 0L;
                        outputWriter.Position = 0L;
                        remainingByteCount = inputReader.Length - inputReader.Position;
                        totalBytesRead = 0L;
                        totalBytesWritten = 0L;
                        continue;
                    }
                    compressCode |= (ushort)((compressedSize & 0xF0000) >> 16);
                    outputWriter.WriteInt32BigEndian(bufferSize);
                    outputWriter.WriteUInt16BigEndian(compressCode);
                    outputWriter.WriteUInt16BigEndian((ushort)compressedSize);
                    outputWriter.BaseStream.Write(compBuffer, 0, (int)compressedSize);
                    remainingByteCount -= bufferSize;
                    totalBytesRead += bufferSize;
                    if (ProfileManager.IsLoaded(FMT.FileTools.Modding.EGame.MADDEN22, FMT.FileTools.Modding.EGame.MADDEN23, FMT.FileTools.Modding.EGame.MADDEN24))
                    {
                        totalBytesWritten += (long)(compressedSize + 8);
                    }
                    if (texture != null && texture.MipCount > 1)
                    {
                        if (totalBytesRead + offset == texture.MipSizes[0])
                        {
                            uint secondMipOffset = (texture.ThirdMipOffset = (uint)totalBytesWritten);
                            texture.SecondMipOffset = secondMipOffset;
                        }
                        else if (totalBytesRead + offset == texture.MipSizes[0] + texture.MipSizes[1])
                        {
                            texture.ThirdMipOffset = (uint)totalBytesWritten;
                        }
                    }
                    if (!ProfileManager.IsLoaded(FMT.FileTools.Modding.EGame.MADDEN22, FMT.FileTools.Modding.EGame.MADDEN23, FMT.FileTools.Modding.EGame.MADDEN24))
                    {
                        totalBytesWritten += (long)(compressedSize + 8);
                    }
                }
                finally
                {
                    if (compBuffer != null && pooledBuffer)
                    {
                        //ArrayPool<byte>.Shared.Return(compBuffer);
                        compBuffer = null;
                    }
                }
            }
            return memoryStream.ToArray();
        }



        private static ulong CompressLZ4(byte[] buffer, out byte[] compBuffer, out ushort compressCode, ref bool uncompressed)
        {
            compBuffer = new byte[LZ4.CompressBound(buffer.Length)];
            compressCode = 2416;
            GCHandle gCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            GCHandle gCHandle2 = GCHandle.Alloc(compBuffer, GCHandleType.Pinned);
            ulong num = (ulong)LZ4.Compress(gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), buffer.Length, compBuffer.Length);
            if (num > (ulong)MaxBufferSize || (uint)num > buffer.Length)
            {
                uncompressed = true;
                num = 0uL;
            }
            gCHandle.Free();
            gCHandle2.Free();
            return num;
        }

        private static ulong CompressZStd(byte[] buffer, out byte[] compBuffer, out ushort compressCode, ref bool uncompressed)
        {
            int bufferSize = (int)ZStd.CompressBound((ulong)buffer.Length);
            compBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            compressCode = 3952;
            GCHandle gCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                GCHandle gCHandle2 = GCHandle.Alloc(compBuffer, GCHandleType.Pinned);
                try
                {
                    ulong compressedSize = ZStd.Compress(gCHandle2.AddrOfPinnedObject(), (ulong)bufferSize, gCHandle.AddrOfPinnedObject(), (ulong)buffer.Length, 16);
                    if (compressedSize > (ulong)buffer.Length)
                    {
                        uncompressed = true;
                        compressedSize = 0uL;
                    }
                    return compressedSize;
                }
                finally
                {
                    gCHandle2.Free();
                }
            }
            finally
            {
                gCHandle.Free();
            }
        }

        private static ulong CompressZlib(byte[] buffer, out byte[] compBuffer, out ushort compressCode, ref bool uncompressed)
        {
            ulong num = 0uL;
            compBuffer = new byte[buffer.Length * 2];
            compressCode = 624;
            GCHandle gCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            GCHandle gCHandle2 = GCHandle.Alloc(compBuffer, GCHandleType.Pinned);
            ZLib.ZStream structure = default(ZLib.ZStream);
            IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            structure.avail_in = (uint)buffer.Length;
            structure.next_in = gCHandle.AddrOfPinnedObject();
            structure.avail_out = structure.avail_in * 2;
            structure.next_out = gCHandle2.AddrOfPinnedObject();
            Marshal.StructureToPtr(structure, intPtr, fDeleteOld: true);
            ZLib.DeflateInit(intPtr, 9, "1.2.11", Marshal.SizeOf<ZLib.ZStream>());
            ZLib.Deflate(intPtr, 4);
            structure = Marshal.PtrToStructure<ZLib.ZStream>(intPtr);
            num = structure.total_out;
            ZLib.DeflateEnd(intPtr);
            Marshal.FreeHGlobal(intPtr);
            if (num > (ulong)MaxBufferSize)
            {
                uncompressed = true;
                num = 0uL;
            }
            gCHandle.Free();
            gCHandle2.Free();
            return num;
        }

        private static ulong CompressNone(byte[] buffer, out byte[] compBuffer, out ushort compressCode)
        {
            compressCode = 112;
            compBuffer = buffer;
            return (ulong)buffer.Length;
        }

        static int GetCompressedBufferSizeNeeded(int size)
        {
            return size + 274 * ((size + 0x3FFFF) / 0x40000);
        }


        public static byte[] DecompressZLib(byte[] tmpBuffer, int decompressedSize)
        {
            GCHandle gCHandle = GCHandle.Alloc(tmpBuffer, GCHandleType.Pinned);
            byte[] array = new byte[decompressedSize];
            GCHandle gCHandle2 = GCHandle.Alloc(array, GCHandleType.Pinned);
            ZLib.ZStream structure = default(ZLib.ZStream);
            structure.avail_in = (uint)tmpBuffer.Length;
            structure.avail_out = (uint)array.Length;
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
            return array;
        }

        public static uint CalcFletcher32(GeometryDeclarationDesc geomDecl)
        {
            byte[] array = null;
            using (NativeWriter nativeWriter = new NativeWriter(new MemoryStream()))
            {
                GeometryDeclarationDesc.Element[] elements = geomDecl.Elements;
                for (int i = 0; i < elements.Length; i++)
                {
                    GeometryDeclarationDesc.Element element = elements[i];
                    nativeWriter.Write((byte)element.Usage);
                    nativeWriter.Write((byte)element.Format);
                    nativeWriter.Write(element.Offset);
                    nativeWriter.Write(element.StreamIndex);
                }
                GeometryDeclarationDesc.Stream[] streams = geomDecl.Streams;
                for (int i = 0; i < streams.Length; i++)
                {
                    GeometryDeclarationDesc.Stream stream = streams[i];
                    nativeWriter.Write(stream.VertexStride);
                    nativeWriter.Write((byte)stream.Classification);
                }
                nativeWriter.Write(geomDecl.ElementCount);
                nativeWriter.Write(geomDecl.StreamCount);
                nativeWriter.Write((ushort)0);
                array = ((MemoryStream)nativeWriter.BaseStream).ToArray();
            }
            return CalcFletcher32Internal(array);
        }

        private static uint CalcFletcher32Internal(byte[] array)
        {
            int num = array.Length;
            int num2 = num >> 1;
            int num3 = 0;
            int num4 = 0;
            uint num5 = 0u;
            uint num6 = 0u;
            int num7 = 0;
            if (num2 > 0)
            {
                do
                {
                    num3 = ((num2 - 360) & (num2 - 360 >> 31)) + 360;
                    num4 = num2 - num3;
                    do
                    {
                        ushort num8 = (ushort)((array[num7] << 8) | array[num7 + 1]);
                        num7 += 2;
                        num5 += num8;
                        num6 += num5;
                        num3--;
                    }
                    while (num3 > 0);
                    num5 = (ushort)((num5 >> 16) + num5);
                    num6 = (ushort)(num6 + (num6 >> 16));
                    num2 = num4;
                }
                while (num4 > 0);
            }
            if ((num & 1) != 0)
            {
                num5 += (ushort)(array[num7] << 8);
                num6 += num5;
            }
            return (uint)((((int)num5 & -65536) + (int)(num5 << 16)) | ((ushort)num6 + (num6 >> 16)));
        }
    }
}
