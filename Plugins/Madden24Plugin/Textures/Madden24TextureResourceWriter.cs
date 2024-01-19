using FMT.FileTools;
using FMT.Logging;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Madden24Plugin.Textures
{
    public class Madden24TextureResourceWriter : ITextureResourceWriter
    {
        public byte[] ToBytes(Texture texture)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (NativeWriter nw = new NativeWriter(memoryStream))
            {
                //texture.mipOffsets[0] = nativeReader.ReadUInt();
                nw.Write(texture.mipOffsets[0]);
                //texture.mipOffsets[1] = nativeReader.ReadUInt();
                nw.Write(texture.mipOffsets[1]);
                //texture.type = (TextureType)nativeReader.ReadUInt();
                nw.Write((uint)texture.type);
                //texture.pixelFormat = nativeReader.ReadInt();
                nw.Write(texture.pixelFormat);
                //texture.unknown1 = nativeReader.ReadUInt();
                nw.Write(texture.poolId);
                //texture.flags = (TextureFlags)nativeReader.ReadUShort();
                nw.Write((ushort)texture.flags);
                //texture.width = nativeReader.ReadUShort();
                nw.Write((ushort)texture.width);
                //texture.height = nativeReader.ReadUShort();
                nw.Write((ushort)texture.height);
                //texture.depth = nativeReader.ReadUShort();
                nw.Write((ushort)texture.depth);
                //texture.sliceCount = nativeReader.ReadUShort();
                nw.Write((ushort)texture.sliceCount);
                //texture.mipCount = nativeReader.ReadByte();
                nw.Write((byte)texture.mipCount);
                //texture.firstMip = nativeReader.ReadByte();
                nw.Write((byte)texture.firstMip);
                //texture.unknownBytes.Add(nativeReader.ReadBytes(4));
                nw.Write(texture.unknownBytes[0]);
                //texture.chunkId = nativeReader.ReadGuid();
                nw.Write(texture.chunkId);
                for (int i = 0; i < 15; i++)
                {
                    //texture.mipSizes[i] = nativeReader.ReadUInt();
                    nw.Write((uint)texture.mipSizes[i]);
                }
                //texture.chunkSize = nativeReader.ReadUInt();
                nw.Write((uint)texture.chunkSize);
                //texture.assetNameHash = nativeReader.ReadUInt();
                nw.Write((uint)texture.assetNameHash);
                //texture.unknownBytes.Add(nativeReader.ReadBytes(4));
                nw.Write(texture.unknownBytes[1]);
                //texture.TextureGroup = nativeReader.ReadSizedString(16);
                nw.WriteFixedSizedString(texture.TextureGroup, 16);
                //texture.unknownBytes.Add(nativeReader.ReadBytes(8));
                nw.Write(texture.unknownBytes[2]);
            }

            var arrayOfBytes = memoryStream.ToArray();
#if DEBUG
            DebugBytesToFileLogger.Instance.WriteAllBytes("Texture_Write.bin", arrayOfBytes, "Texture/Madden24/");
#endif

            return arrayOfBytes;
        }
    }
}
