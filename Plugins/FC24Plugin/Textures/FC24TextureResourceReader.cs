using FMT.FileTools;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PInvoke.BCrypt.BCRYPT_ALGORITHM_IDENTIFIER;
using System.Windows.Media.Media3D;

namespace FC24Plugin.Textures
{
    public class FC24TextureResourceReader : ITextureResourceReader
    {
        public void ReadInStream(NativeReader nativeReader, Texture texture)
        {
            texture.mipOffsets[0] = nativeReader.ReadUInt();
            texture.mipOffsets[1] = nativeReader.ReadUInt();
            texture.type = (TextureType)nativeReader.ReadUInt();
            texture.pixelFormat = nativeReader.ReadInt();
            texture.unknown1 = nativeReader.ReadUInt();
            texture.flags = (TextureFlags)nativeReader.ReadUShort();
            texture.width = nativeReader.ReadUShort();
            texture.height = nativeReader.ReadUShort();
            texture.depth = nativeReader.ReadUShort();
            texture.sliceCount = nativeReader.ReadUShort();
            texture.mipCount = nativeReader.ReadByte();
            texture.firstMip = nativeReader.ReadByte();
            texture.unknownBytes.Add(nativeReader.ReadBytes(8));
            texture.chunkId = nativeReader.ReadGuid();
            for (int i = 0; i < 15; i++)
                texture.mipSizes[i] = nativeReader.ReadUInt();

            texture.chunkSize = nativeReader.ReadUInt();
            texture.assetNameHash = nativeReader.ReadUInt();
            texture.TextureGroup = nativeReader.ReadSizedString(16);

            List<byte> lastBytes = new();
            while (nativeReader.Position != nativeReader.Length)
            {
                lastBytes.Add(nativeReader.ReadByte());
            }
            texture.unknownBytes.Add(lastBytes.ToArray());

            if (AssetManager.Instance.Logger != null)
                AssetManager.Instance.Logger.Log($"Texture: Loading ChunkId: {texture.chunkId}");

            texture.ChunkEntry = AssetManager.Instance.GetChunkEntry(texture.chunkId);
            texture.Data = AssetManager.Instance.GetChunk(texture.ChunkEntry);
        }
    }
}
