using FMT.FileTools;
using Frostbite.Textures;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.IO;

namespace FC24Plugin.Textures
{
    public class FC24TextureImporter : ITextureImporter
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
                AssetManager.Instance.ModifyRes(resRid, textureAsset.ToBytes());
                AssetManager.Instance.ModifyEbx(assetEntry.Name, ebxAsset);
                resEntry.LinkAsset(chunkEntry);
                assetEntry.LinkAsset(resEntry);
            }

        }

        public void DoImport(string path, AssetEntry assetEntry)
        {
            throw new NotImplementedException();
        }

    }
}