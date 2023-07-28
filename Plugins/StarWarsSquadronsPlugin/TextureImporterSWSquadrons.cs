using FMT.FileTools;
using FMT.FileTools.Modding;
using Frostbite.Textures;
using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using static PInvoke.BCrypt.BCRYPT_ALGORITHM_IDENTIFIER;
using System.Windows.Media.Media3D;

namespace StarWarsSquadronsPlugin.Textures
{
    public class TextureImporterSWSquadrons : ITextureImporter
    {
        public void DoImport(string path, EbxAssetEntry assetEntry, ref Texture textureAsset)
        {
            var extension = "DDS";
            var spl = path.Split('.');
            extension = spl[spl.Length - 1].ToUpper();

            var compatible_extensions = new List<string>() { "DDS", "PNG" };
            if (!compatible_extensions.Contains(extension))
            {
                throw new NotImplementedException("Incorrect file type used in Texture Importer");
            }

            TextureUtils.ImageFormat imageFormat = TextureUtils.ImageFormat.DDS;
            imageFormat = (TextureUtils.ImageFormat)Enum.Parse(imageFormat.GetType(), extension);


            MemoryStream memoryStream = null;
            TextureUtils.BlobData pOutData = default(TextureUtils.BlobData);
            if (imageFormat == TextureUtils.ImageFormat.DDS)
            {
                memoryStream = new MemoryStream(NativeReader.ReadInStream(new FileStream(path, FileMode.Open, FileAccess.Read)));
            }
            else
            {
                TextureUtils.TextureImportOptions options = default(TextureUtils.TextureImportOptions);
                options.type = textureAsset.Type;
                options.format = TextureUtils.ToShaderFormat(textureAsset.PixelFormat, (textureAsset.Flags & TextureFlags.SrgbGamma) != 0);
                options.generateMipmaps = (textureAsset.MipCount > 1);
                options.mipmapsFilter = 0;
                options.resizeTexture = true;
                options.resizeFilter = 0;
                options.resizeHeight = textureAsset.Height;
                options.resizeWidth = textureAsset.Width;
                if (textureAsset.Type == TextureType.TT_2d)
                {
                    byte[] pngarray = NativeReader.ReadInStream(new FileStream(path, FileMode.Open, FileAccess.Read));
                    TextureUtils.ConvertImageToDDS(pngarray, pngarray.Length, imageFormat, options, ref pOutData);
                }
                else
                {
                    throw new NotImplementedException("Unable to process PNG into non-2D texture");
                }
            }

            if (imageFormat != TextureUtils.ImageFormat.DDS)
            {
                memoryStream = new MemoryStream(pOutData.Data);
            }

            //if (!Directory.Exists("Debugging"))
            //	Directory.CreateDirectory("Debugging");

            //if (!Directory.Exists("Debugging\\Other\\"))
            //	Directory.CreateDirectory("Debugging\\Other\\");

            //using (FileStream fileStream = new FileStream("Debugging\\Other\\_TextureImport.dat", FileMode.OpenOrCreate))
            //{
            //	memoryStream.CopyTo(fileStream);
            //	fileStream.Flush();
            //}
            memoryStream.Position = 0;


            using (NativeReader nativeReader = new NativeReader(memoryStream))
            {
                TextureUtils.DDSHeader dDSHeader = new TextureUtils.DDSHeader();
                if (dDSHeader.Read(nativeReader))
                {

                }

                var ebxAsset = AssetManager.Instance.GetEbx(assetEntry);
                ulong resRid = ((dynamic)ebxAsset.RootObject).Resource;
                ResAssetEntry resEntry = AssetManager.Instance.GetResEntry(resRid);
                ChunkAssetEntry chunkEntry = AssetManager.Instance.GetChunkEntry(textureAsset.ChunkId);
                byte[] textureArray = new byte[nativeReader.Length - nativeReader.Position];
                nativeReader.Read(textureArray, 0, (int)(nativeReader.Length - nativeReader.Position));
                AssetManager.Instance.ModifyChunk(textureAsset.ChunkId, textureArray, textureAsset);
                AssetManager.Instance.ModifyRes(resRid, ToSWSBytes(textureAsset));
                AssetManager.Instance.ModifyEbx(assetEntry.Name, ebxAsset);
                resEntry.LinkAsset(chunkEntry);
                assetEntry.LinkAsset(resEntry);
            }

        }

        public void DoImport(string path, AssetEntry assetEntry)
        {
            throw new NotImplementedException();
        }

        public static byte[] ToSWSBytes(Texture texture)
        {
            byte[] finalArray = null;
            using (var nw = new NativeWriter(new MemoryStream()))
            {
                //mipOffsets[0] = nativeReader.ReadUInt();
                nw.Write((uint)texture.mipOffsets[0]);
                //mipOffsets[1] = nativeReader.ReadUInt();
                nw.Write((uint)texture.mipOffsets[1]);
                //type = (TextureType)nativeReader.ReadUInt();
                nw.Write((uint)texture.type);
                //pixelFormat = nativeReader.ReadInt();
                nw.Write(texture.pixelFormat);
                //unknown1 = nativeReader.ReadUInt();
                nw.Write((uint)texture.unknown1);
                //flags = (TextureFlags)(Version >= 11 ? nativeReader.ReadUShort() : nativeReader.ReadUInt());
                nw.Write((ushort)texture.flags);
                //width = nativeReader.ReadUShort();
                nw.Write(texture.width);
                //height = nativeReader.ReadUShort();
                nw.Write(texture.height);
                //depth = nativeReader.ReadUShort();
                nw.Write(texture.depth);
                //sliceCount = nativeReader.ReadUShort();
                nw.Write(texture.sliceCount);
                //mipCount = nativeReader.ReadByte();
                nw.Write(texture.MipCount);
                //firstMip = nativeReader.ReadByte();
                nw.Write(texture.firstMip);
                //chunkId = nativeReader.ReadGuid();
                nw.Write(texture.chunkId);
                //for (int i = 0; i < 15; i++)
                //{
                //    mipSizes[i] = nativeReader.ReadUInt();
                //}
                for (int i = 0; i < 15; i++)
                {
                    nw.Write(texture.mipSizes[i]);
                }
                //chunkSize = nativeReader.ReadUInt();
                nw.Write(texture.chunkSize);
                //assetNameHash = nativeReader.ReadUInt();
                nw.Write(texture.AssetNameHash);

                //TextureGroup = nativeReader.ReadSizedString(16);
                nw.WriteFixedSizedString(texture.textureGroup, 16);

                //if (ProfileManager.IsLoaded(EGame.BFV, EGame.StarWarsSquadrons))
                //{
                //    _ = nativeReader.ReadUInt();
                //}
                nw.Write(0u);

                finalArray = ((MemoryStream)nw.BaseStream).ToArray();
            }
            return finalArray;
        }
    }
}