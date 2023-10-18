using FMT.FileTools;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using FrostySdk.Resources;

namespace Madden24Plugin.Textures
{
    public class Madden24TextureResourceReader : ITextureResourceReader
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
            texture.unknownBytes.Add(nativeReader.ReadBytes(4));
            texture.chunkId = nativeReader.ReadGuid();
            for (int i = 0; i < 15; i++)
            {
                texture.mipSizes[i] = nativeReader.ReadUInt();
            }
            texture.chunkSize = nativeReader.ReadUInt();
            texture.assetNameHash = nativeReader.ReadUInt();
            texture.unknownBytes.Add(nativeReader.ReadBytes(4));
            texture.TextureGroup = nativeReader.ReadSizedString(16);
            texture.unknownBytes.Add(nativeReader.ReadBytes(8));

            if (AssetManager.Instance.Logger != null)
                AssetManager.Instance.Logger.Log($"Texture: Loading ChunkId: {texture.chunkId}");

            texture.ChunkEntry = AssetManager.Instance.GetChunkEntry(texture.chunkId);
            texture.Data = AssetManager.Instance.GetChunk(texture.ChunkEntry);
        }
    }
}
