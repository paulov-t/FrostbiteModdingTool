using FMT.FileTools;
using FMT.Logging;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FC24Plugin.Textures
{
    public class FC24TextureResourceWriter : ITextureResourceWriter
    {
        public byte[] ToBytes(Texture texture)
        {
            // Define new memory stream with a capacity of 132 bytes
            MemoryStream memoryStream = new MemoryStream(132);
            using (var nw = new NativeWriter(memoryStream))
            {
                nw.Write(texture.mipOffsets[0]);
                nw.Write(texture.mipOffsets[1]);
                nw.Write((uint)texture.Type);
                nw.Write((int)texture.pixelFormat);
                nw.Write((uint)texture.poolId);
                nw.Write((ushort)texture.flags);
                nw.Write((ushort)texture.width);
                nw.Write((ushort)texture.height);
                nw.Write((ushort)texture.depth);
                nw.Write((ushort)texture.sliceCount);
                nw.Write((byte)texture.mipCount);
                nw.Write((byte)texture.firstMip);
                nw.Write(texture.unknownBytes[0]);
                nw.Write(texture.chunkId);
                for (int i = 0; i < 15; i++)
                    nw.Write((uint)texture.mipSizes[i]);

                nw.Write((uint)texture.chunkSize);
                nw.Write((uint)texture.assetNameHash);
                nw.WriteFixedSizedString(texture.textureGroup, 16);
                nw.Write(texture.unknownBytes[1]);
            }

            var arrayOfBytes = memoryStream.ToArray();
#if DEBUG
            DebugBytesToFileLogger.Instance.WriteAllBytes("Texture_Write.bin", arrayOfBytes, "Texture/FC24/");
#endif

            return arrayOfBytes;
        }
    }
}
