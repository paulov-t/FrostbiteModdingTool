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
            MemoryStream memoryStream = new MemoryStream();
            using (var nw = new NativeWriter(memoryStream))
            {
                nw.Write(texture.mipOffsets[0]);
                nw.Write(texture.mipOffsets[1]);
                nw.Write((uint)texture.Type);
                nw.Write(texture.pixelFormat);
                nw.Write(texture.unknown1);
                nw.Write((ushort)texture.flags);
                nw.Write(texture.width);
                nw.Write(texture.height);
                nw.Write(texture.depth);
                nw.Write(texture.sliceCount);
                nw.Write(texture.mipCount);
                nw.Write(texture.firstMip);
                nw.Write(texture.unknownBytes[0]);
                nw.Write(texture.chunkId);
                for (int i = 0; i < 15; i++)
                    nw.Write(texture.mipSizes[i]);

                nw.Write(texture.chunkSize);
                nw.Write(texture.assetNameHash);
                nw.WriteNullTerminatedString(texture.textureGroup);
                nw.Write(texture.unknownBytes[1]);
            }

            var arrayOfBytes = memoryStream.ToArray();
#if DEBUG
            Directory.CreateDirectory("Texture/FC24/");
            DebugBytesToFileLogger.Instance.WriteAllBytes("Texture_Write.bin", arrayOfBytes, "Texture/FC24/");
#endif

            return arrayOfBytes;
        }
    }
}
