using FMT.FileTools;
using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Frostbite.Textures
{
    public class TextureImporter : ITextureImporter
    {
        public async Task<bool> ImportAsync(string path, EbxAssetEntry assetEntry, Texture textureAsset)
        {
            var ta = textureAsset;
            return await Task.Run(() => { return Import(path, assetEntry, ref ta); });
        }

        public bool Import(string path, EbxAssetEntry assetEntry, ref Texture textureAsset)
        {
            bool run = false;
            if (string.IsNullOrEmpty(ProfileManager.TextureImporter))
            {
                DoImport(path, assetEntry, ref textureAsset);
                run = true;
            }
            else
            {
                var textureImporter = AssetManager.Instance.LoadTypeFromPlugin(ProfileManager.TextureImporter) as ITextureImporter;
                if (textureImporter == null)
                {
                    return false;
                }
                textureImporter.DoImport(path, assetEntry, ref textureAsset);
                run = true;

                //            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                //{
                //	if (a.FullName.Contains("Plugin"))
                //	{
                //		var loadTypes = a.GetTypes();
                //		foreach (Type t in loadTypes)
                //		{
                //			if (t.GetInterface("ITextureImporter") != null)
                //			{
                //				try
                //				{
                //					if (t.Name == ProfileManager.TextureImporter)
                //					{
                //						((ITextureImporter)Activator.CreateInstance(t)).DoImport(path, assetEntry, ref textureAsset);
                //						run = true;
                //						break;
                //					}
                //				}
                //				catch
                //				{
                //				}
                //			}
                //		}
                //	}
                //}
            }
            return run;
        }

        public void DoImport(string path, AssetEntry assetEntry)
        {
            throw new Exception("The TextureImporter does not support this method call");
        }

        public virtual void DoImport(string path, EbxAssetEntry assetEntry, ref Texture textureAsset)
        {
            ImportTextureFromFileToTextureAsset_Original(path, assetEntry, assetManager: AssetManager.Instance, ref textureAsset, out string message);
        }

        public void DoImportFromPNGMemoryStream(MemoryStream stream, EbxAssetEntry assetEntry, ref Texture textureAsset)
        {
            TextureUtils.ImageFormat imageFormat = TextureUtils.ImageFormat.PNG;
            TextureUtils.BlobData pOutData = default(TextureUtils.BlobData);

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
                byte[] pngarray = stream.ToArray();
                TextureUtils.ConvertImageToDDS(pngarray, pngarray.Length, imageFormat, options, ref pOutData);
            }
            else
            {
                throw new NotImplementedException("Unable to process PNG into non-2D texture");
            }

            stream = new MemoryStream(pOutData.Data);
            stream.Position = 0;
            using (NativeReader nativeReader = new NativeReader(stream))
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


        public bool ImportFromPathIntoRes(string path, string resName)
        {
            var resEntry = AssetManager.Instance.GetResEntry(resName);
            if (resEntry != null)
            {
                Texture texture = new Texture(resEntry);
                EbxAssetEntry ebxAssetEntry = AssetManager.Instance.GetEbxEntry(resName);

                if (ebxAssetEntry != null)
                {
                    return (!this.Import(path, ebxAssetEntry, ref texture));
                }
            }
            return false;
        }

        public async Task<bool> ImportFromPathIntoResAsync(string path, string resName)
        {
            return await Task.FromResult(ImportFromPathIntoRes(path, resName));
        }

        public virtual TextureUtils.DDSHeader GetDDSHeaderFromBytes(byte[] bytes)
        {
            NativeReader nativeReader = new NativeReader(new MemoryStream(bytes));
            TextureUtils.DDSHeader dDSHeader = new TextureUtils.DDSHeader();
            if (dDSHeader.Read(nativeReader))
            {
                return dDSHeader;
            }

            return null;
        }

        [Obsolete("Not used in any useful capacity other than Testing")]
        public virtual NativeReader ImportTextureFromFileToReaderWithHeader(string path, out string message)
        {
            message = string.Empty;

            var extension = "DDS";
            var spl = path.Split('.');
            extension = spl[spl.Length - 1].ToUpper();

            var compatible_extensions = new List<string>() { "DDS", "PNG" };
            if (!compatible_extensions.Contains(extension))
            {
                message = "Non compatible image type used";
                throw new Exception(message);
            }

            TextureUtils.ImageFormat imageFormat = TextureUtils.ImageFormat.DDS;
            Enum.Parse(imageFormat.GetType(), extension);

            MemoryStream memoryStream = null;
            //TextureUtils.BlobData pOutData = default(TextureUtils.BlobData);
            if (extension == "DDS")
            {
                memoryStream = new MemoryStream(NativeReader.ReadInStream(new FileStream(path, FileMode.Open, FileAccess.Read)));
            }

            if (memoryStream != null)
            {
                NativeReader nativeReader = new NativeReader(memoryStream);
                TextureUtils.DDSHeader dDSHeader = new TextureUtils.DDSHeader();
                long sizeOfData = 0;
                if (dDSHeader.Read(nativeReader))
                {
                    sizeOfData = nativeReader.Length - nativeReader.Position;
                    // remove the header
                    byte[] ddsData = new byte[sizeOfData];
                    // read in the rest of the data
                    nativeReader.Read(ddsData, 0, (int)(sizeOfData));
                }
                return nativeReader;
            }
            return null;
        }

        [Obsolete("Not used in any useful capacity other than Testing")]
        public virtual NativeReader ImportTextureFromFileToReader(string path, out string message)
        {
            message = string.Empty;

            var extension = "DDS";
            var spl = path.Split('.');
            extension = spl[spl.Length - 1].ToUpper();

            var compatible_extensions = new List<string>() { "DDS", "PNG" };
            if (!compatible_extensions.Contains(extension))
            {
                message = "Non compatible image type used";
                throw new Exception(message);
            }

            TextureUtils.ImageFormat imageFormat = TextureUtils.ImageFormat.DDS;
            Enum.Parse(imageFormat.GetType(), extension);

            MemoryStream memoryStream = null;
            //TextureUtils.BlobData pOutData = default(TextureUtils.BlobData);
            if (extension == "DDS")
            {
                memoryStream = new MemoryStream(NativeReader.ReadInStream(new FileStream(path, FileMode.Open, FileAccess.Read)));
            }

            if (memoryStream != null)
            {
                NativeReader nativeReader = new NativeReader(memoryStream);
                TextureUtils.DDSHeader dDSHeader = new TextureUtils.DDSHeader();
                long sizeOfHeader = 0;
                long sizeOfData = 0;
                if (dDSHeader.Read(nativeReader))
                {
                    sizeOfHeader = nativeReader.Position;
                    sizeOfData = nativeReader.Length - nativeReader.Position;
                    // remove the header
                    byte[] ddsData = new byte[sizeOfData];
                    // read in the rest of the data
                    nativeReader.Read(ddsData, 0, (int)(sizeOfData));
                }
                var dataOfNativeReader = nativeReader.CreateViewStream(sizeOfHeader, sizeOfData);
                nativeReader.Dispose();
                return new NativeReader(dataOfNativeReader);
            }
            return null;
        }

        [Obsolete("ImportTextureFromFileToTextureAsset is deprecated, please use ImportTextureFromFileToTextureAsset_Original instead.")]
        public virtual void ImportTextureFromFileToTextureAsset(string path, ref Texture textureAsset, out string message)
        {
            message = string.Empty;

            var extension = "DDS";
            var spl = path.Split('.');
            extension = spl[spl.Length - 1].ToUpper();

            var compatible_extensions = new List<string>() { "DDS", "PNG" };
            if (!compatible_extensions.Contains(extension))
            {
                message = "Non compatible image type used";
                return;
            }

            TextureUtils.ImageFormat imageFormat = TextureUtils.ImageFormat.DDS;
            Enum.Parse(imageFormat.GetType(), extension);

            MemoryStream memoryStream = null;
            //TextureUtils.BlobData pOutData = default(TextureUtils.BlobData);
            if (extension == "DDS")
            {
                memoryStream = new MemoryStream(NativeReader.ReadInStream(new FileStream(path, FileMode.Open, FileAccess.Read)));
                ImportTextureFromStreamToTextureAsset(memoryStream, ref textureAsset, out message);
            }

        }

        public virtual void ImportTextureFromStreamToTextureAsset(MemoryStream memoryStream, ref Texture textureAsset, out string message)
        {
            if (textureAsset == null)
                textureAsset = new Texture();

            message = string.Empty;

            if (memoryStream != null)
            {
                using (NativeReader nativeReader = new NativeReader(memoryStream))
                {
                    TextureUtils.DDSHeader dDSHeader = new TextureUtils.DDSHeader();
                    if (dDSHeader.Read(nativeReader))
                    {
                        //TextureType textureType = TextureType.TT_2d;
                        //if (dDSHeader.HasExtendedHeader)
                        //{
                        //	if ((int)dDSHeader.ExtendedHeader.resourceDimension == 3)
                        //	{
                        //		if ((dDSHeader.ExtendedHeader.miscFlag & 4) != 0)
                        //		{
                        //			textureType = TextureType.TT_Cube;
                        //		}
                        //		else if (dDSHeader.ExtendedHeader.arraySize > 1)
                        //		{
                        //			textureType = TextureType.TT_2dArray;
                        //		}
                        //	}
                        //	else if ((int)dDSHeader.ExtendedHeader.resourceDimension == 4)
                        //	{
                        //		textureType = TextureType.TT_3d;
                        //	}
                        //}
                        //else if ((dDSHeader.dwCaps2 & TextureUtils.DDSCaps2.CubeMap) != 0)
                        //{
                        //	textureType = TextureType.TT_Cube;
                        //}
                        //else if ((dDSHeader.dwCaps2 & TextureUtils.DDSCaps2.Volume) != 0)
                        //{
                        //	textureType = TextureType.TT_3d;
                        //}
                        //if (textureType != textureAsset.Type)
                        //{
                        //	message = $"Imported texture must match original texture type. Original texture type is {textureAsset.Type}. Imported texture type is {textureType}";
                        //	return;
                        //}

                        //var texAssetType = TextureType.TT_2d;
                        //var mipMaps = 1;
                        //if (textureAsset.Type != TextureType.TT_2dArray && textureAsset.Type != TextureType.TT_3d)
                        //{
                        //	texAssetType = textureAsset.Type;
                        //	mipMaps = 1;
                        //}

                        string pixelFormat = "";
                        TextureFlags flags = 0;
                        GetPixelFormat(dDSHeader, textureAsset, out pixelFormat, out flags);
                        if (TextureUtils.IsCompressedFormat(pixelFormat) && textureAsset.MipCount > 1 && (dDSHeader.dwWidth % 4 != 0 || dDSHeader.dwHeight % 4 != 0))
                        {
                            message = "Texture width/height must be divisible by 4 for compressed formats requiring mip maps";
                            return;
                        }
                        byte[] array8 = new byte[nativeReader.Length - nativeReader.Position];
                        nativeReader.Read(array8, 0, (int)(nativeReader.Length - nativeReader.Position));
                        ushort inDepth = (ushort)((!dDSHeader.HasExtendedHeader || (int)dDSHeader.ExtendedHeader.resourceDimension != 3) ? 1 : ((ushort)dDSHeader.ExtendedHeader.arraySize));
                        if ((dDSHeader.dwCaps2 & TextureUtils.DDSCaps2.CubeMap) != 0)
                        {
                            inDepth = 6;
                        }
                        if ((dDSHeader.dwCaps2 & TextureUtils.DDSCaps2.Volume) != 0)
                        {
                            inDepth = (ushort)dDSHeader.dwDepth;
                        }

                        textureAsset.SetData(array8);
                        textureAsset.CalculateMipData((byte)dDSHeader.dwMipMapCount, TextureUtils.GetFormatBlockSize(pixelFormat), TextureUtils.IsCompressedFormat(pixelFormat), (uint)array8.Length);
                        textureAsset.Flags = flags;
                        TextureFlags textureFlags = textureAsset.Flags & ~TextureFlags.SrgbGamma;
                        textureAsset.Flags |= textureFlags;

                    }
                }
            }

        }

        /// <summary>
        /// This is the one thats used right now
        /// </summary>
        /// <param name="path"></param>
        /// <param name="assetEntry"></param>
        /// <param name="assetManager"></param>
        /// <param name="textureAsset"></param>
        /// <param name="message"></param>
        public virtual void ImportTextureFromFileToTextureAsset_Original(string path, EbxAssetEntry assetEntry, AssetManager assetManager, ref Texture textureAsset, out string message)
        {
            message = string.Empty;

            var extension = "DDS";
            var spl = path.Split('.');
            extension = spl[spl.Length - 1].ToUpper();

            var compatible_extensions = new List<string>() { "DDS", "PNG" };
            if (!compatible_extensions.Contains(extension))
            {
                message = "Non compatible image type used";
                return;
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
                options.resizeTexture = false;
                options.resizeFilter = 0;
                options.resizeHeight = 0;
                options.resizeWidth = 0;
                if (textureAsset.Type == TextureType.TT_2d)
                {
                    byte[] array3 = NativeReader.ReadInStream(new FileStream(path, FileMode.Open, FileAccess.Read));
                    TextureUtils.ConvertImageToDDS(array3, array3.Length, imageFormat, options, ref pOutData);
                }
                else
                {
                    //string[] array4 = new string[settings.Textures.Count];
                    //for (int k = 0; k < settings.Textures.Count; k++)
                    //{
                    //	array4[k] = settings.Textures[k].Filename;
                    //}
                    //byte[] array5 = new byte[0];
                    //long[] array6 = new long[array4.Length];
                    //for (int l = 0; l < array4.Length; l++)
                    //{
                    //	byte[] array7 = NativeReader.ReadInStream(new FileStream(array4[l], FileMode.Open, FileAccess.Read));
                    //	array6[l] = array7.Length;
                    //	Array.Resize(ref array5, array5.Length + array7.Length);
                    //	Array.Copy(array7, 0, array5, array5.Length - array7.Length, array7.Length);
                    //}
                    //TextureUtils.ConvertImagesToDDS(array5, array6, array6.Length, (TextureUtils.ImageFormat)(ofd.FilterIndex - 1), options, ref pOutData);
                }
                memoryStream = new MemoryStream(pOutData.Data);
            }
            using (NativeReader nativeReader = new NativeReader(memoryStream))
            {
                ulong resRid = ((dynamic)assetManager.GetEbx(assetEntry).RootObject).Resource;

                var bFailed = false;
                TextureUtils.DDSHeader dDSHeader = new TextureUtils.DDSHeader();
                if (dDSHeader.Read(nativeReader))
                {
                    TextureType textureType = TextureType.TT_2d;
                    if (dDSHeader.HasExtendedHeader)
                    {
                        if ((int)dDSHeader.ExtendedHeader.resourceDimension == 3)
                        {
                            if ((dDSHeader.ExtendedHeader.miscFlag & 4) != 0)
                            {
                                textureType = TextureType.TT_Cube;
                            }
                            else if (dDSHeader.ExtendedHeader.arraySize > 1)
                            {
                                textureType = TextureType.TT_2dArray;
                            }
                        }
                        else if ((int)dDSHeader.ExtendedHeader.resourceDimension == 4)
                        {
                            textureType = TextureType.TT_3d;
                        }
                    }
                    else if ((dDSHeader.dwCaps2 & TextureUtils.DDSCaps2.CubeMap) != 0)
                    {
                        textureType = TextureType.TT_Cube;
                    }
                    else if ((dDSHeader.dwCaps2 & TextureUtils.DDSCaps2.Volume) != 0)
                    {
                        textureType = TextureType.TT_3d;
                    }
                    if (textureType != textureAsset.Type)
                    {
                        message = $"Imported texture must match original texture type. Original texture type is {textureAsset.Type}. Imported texture type is {textureType}";
                        bFailed = true;
                    }
                    if (!bFailed)
                    {
                        if (textureAsset.Type != TextureType.TT_2dArray && textureAsset.Type != TextureType.TT_3d)
                        {
                            _ = textureAsset.Type;
                            _ = 1;
                        }
                        //if (!bFailed && textureIsSRGB && (!dDSHeader.HasExtendedHeader || !dDSHeader.ExtendedHeader.dxgiFormat.ToString().ToLower().Contains("srgb")))
                        //{
                        //	message = $"Format must be SRGB variant";
                        //	bFailed = true;
                        //}
                    }
                    string pixelFormat = "";
                    TextureFlags flags = 0;
                    GetPixelFormat(dDSHeader, textureAsset, out pixelFormat, out flags);
                    if (TextureUtils.IsCompressedFormat(pixelFormat) && textureAsset.MipCount > 1 && (dDSHeader.dwWidth % 4 != 0 || dDSHeader.dwHeight % 4 != 0))
                    {
                        message = "Texture width/height must be divisible by 4 for compressed formats requiring mip maps";
                        bFailed = true;
                    }
                    if (!bFailed)
                    {
                        ResAssetEntry resEntry = assetManager.GetResEntry(resRid);
                        ChunkAssetEntry chunkEntry = assetManager.GetChunkEntry(textureAsset.ChunkId);
                        byte[] array8 = new byte[nativeReader.Length - nativeReader.Position];
                        nativeReader.Read(array8, 0, (int)(nativeReader.Length - nativeReader.Position));
                        ushort inDepth = (ushort)((!dDSHeader.HasExtendedHeader || (int)dDSHeader.ExtendedHeader.resourceDimension != 3) ? 1 : ((ushort)dDSHeader.ExtendedHeader.arraySize));
                        if ((dDSHeader.dwCaps2 & TextureUtils.DDSCaps2.CubeMap) != 0)
                        {
                            inDepth = 6;
                        }
                        if ((dDSHeader.dwCaps2 & TextureUtils.DDSCaps2.Volume) != 0)
                        {
                            inDepth = (ushort)dDSHeader.dwDepth;
                        }
                        Texture texture = new Texture(textureAsset.Type, pixelFormat, (ushort)dDSHeader.dwWidth, (ushort)dDSHeader.dwHeight, inDepth)
                        {
                            FirstMip = textureAsset.FirstMip
                        };
                        if (dDSHeader.dwMipMapCount <= textureAsset.FirstMip)
                        {
                            texture.FirstMip = 0;
                        }
                        texture.TextureGroup = textureAsset.TextureGroup;
                        texture.CalculateMipData((byte)dDSHeader.dwMipMapCount, TextureUtils.GetFormatBlockSize(pixelFormat), TextureUtils.IsCompressedFormat(pixelFormat), (uint)array8.Length);
                        texture.Flags = flags;
                        TextureFlags textureFlags = textureAsset.Flags & ~TextureFlags.SrgbGamma;
                        texture.Flags |= textureFlags;
                        if (texture.Type == TextureType.TT_Cube || texture.Type == TextureType.TT_2dArray)
                        {
                            MemoryStream memoryStream2 = new MemoryStream(array8);
                            MemoryStream memoryStream3 = new MemoryStream();
                            int num2 = 6;
                            if (texture.Type == TextureType.TT_2dArray)
                            {
                                num2 = texture.Depth;
                            }
                            uint[] array9 = new uint[texture.MipCount];
                            for (int m = 0; m < texture.MipCount - 1; m++)
                            {
                                array9[m + 1] = (uint)((int)array9[m] + (int)(texture.MipSizes[m] * num2));
                            }
                            byte[] buffer = new byte[texture.MipSizes[0]];
                            for (int n = 0; n < num2; n++)
                            {
                                for (int num3 = 0; num3 < texture.MipCount; num3++)
                                {
                                    int num4 = (int)texture.MipSizes[num3];
                                    memoryStream2.Read(buffer, 0, num4);
                                    memoryStream3.Position = array9[num3] + num4 * n;
                                    memoryStream3.Write(buffer, 0, num4);
                                }
                            }
                            array8 = memoryStream3.ToArray();
                        }
                        if (ProfileManager.MustAddChunks && chunkEntry.Bundles.Count == 0 && !chunkEntry.IsAdded)
                        {
                            textureAsset.ChunkId = assetManager.AddChunk(array8, null, ((texture.Flags & TextureFlags.OnDemandLoaded) != 0) ? null : texture);
                            chunkEntry = assetManager.GetChunkEntry(textureAsset.ChunkId);
                        }
                        else
                        {
                            Texture t = ((texture.Flags & TextureFlags.OnDemandLoaded) != 0 || texture.Type != 0) ? null : texture;
                            assetManager.ModifyChunk(textureAsset.ChunkId, array8, t);
                        }
                        //for (int num5 = 0; num5 < 4; num5++)
                        //{
                        //	texture.Unknown3[num5] = textureAsset.Unknown3[num5];
                        //}
                        texture.SetData(textureAsset.ChunkId, assetManager);
                        texture.AssetNameHash = (uint)Fnv1.HashString(resEntry.Name);
                        texture.ChunkEntry = chunkEntry;
                        textureAsset.Dispose();
                        textureAsset = texture;
                        assetManager.ModifyRes(resRid, texture.ToBytes());
                        resEntry.LinkAsset(chunkEntry);
                        assetEntry.LinkAsset(resEntry);
                    }
                }
                else
                {
                    message = $"Invalid DDS format";
                    bFailed = true;
                }
            }
            TextureUtils.ReleaseBlob(pOutData);

        }

        public static Texture CreateLegacyTexture(AssetEntry assetEntry)
        {
            using (NativeReader nativeReader = new NativeReader(AssetManager.Instance.GetCustomAsset("legacy", assetEntry)))
            {
                var textureHeader = new TextureUtils.DDSHeader();
                if (textureHeader.Read(nativeReader))
                {
                    TextureType inType = TextureType.TT_2d;
                    if (textureHeader.HasExtendedHeader)
                    {
                        if ((int)textureHeader.ExtendedHeader.resourceDimension == 3)
                        {
                            if ((textureHeader.ExtendedHeader.miscFlag & 4u) != 0)
                            {
                                inType = TextureType.TT_Cube;
                            }
                            else if (textureHeader.ExtendedHeader.arraySize > 1)
                            {
                                inType = TextureType.TT_2dArray;
                            }
                        }
                        else if ((int)textureHeader.ExtendedHeader.resourceDimension == 4)
                        {
                            inType = TextureType.TT_3d;
                        }
                    }
                    else if ((textureHeader.dwCaps2 & TextureUtils.DDSCaps2.CubeMap) != 0)
                    {
                        inType = TextureType.TT_Cube;
                    }
                    else if ((textureHeader.dwCaps2 & TextureUtils.DDSCaps2.Volume) != 0)
                    {
                        inType = TextureType.TT_3d;
                    }
                    byte[] array = new byte[nativeReader.Length - nativeReader.Position];
                    nativeReader.Read(array, 0, (int)(nativeReader.Length - nativeReader.Position));
                    string pixelFormat = "";
                    TextureFlags flags = 0;
                    TextureImporter.GetPixelFormat(textureHeader, new Texture(), out pixelFormat, out flags);
                    ushort inDepth = (ushort)((!textureHeader.HasExtendedHeader || (int)textureHeader.ExtendedHeader.resourceDimension != 3) ? 1 : ((ushort)textureHeader.ExtendedHeader.arraySize));
                    byte inMipCount = (byte)((textureHeader.dwMipMapCount == 0) ? 1 : textureHeader.dwMipMapCount);
                    if ((textureHeader.dwCaps2 & TextureUtils.DDSCaps2.CubeMap) != 0)
                    {
                        inDepth = 6;
                    }
                    if ((textureHeader.dwCaps2 & TextureUtils.DDSCaps2.Volume) != 0)
                    {
                        inDepth = (ushort)textureHeader.dwDepth;
                    }
                    Texture texture = new Texture(inType, pixelFormat, (ushort)textureHeader.dwWidth, (ushort)textureHeader.dwHeight, inDepth);
                    texture.FirstMip = 0;
                    texture.TextureGroup = "";
                    texture.CalculateMipData(inMipCount, TextureUtils.GetFormatBlockSize(pixelFormat), TextureUtils.IsCompressedFormat(pixelFormat), (uint)array.Length);
                    texture.Flags = flags;
                    if (texture.Type == TextureType.TT_Cube || texture.Type == TextureType.TT_2dArray)
                    {
                        MemoryStream memoryStream = new MemoryStream(array);
                        MemoryStream memoryStream2 = new MemoryStream();
                        int num = 6;
                        if (texture.Type == TextureType.TT_2dArray)
                        {
                            num = texture.Depth;
                        }
                        uint[] array2 = new uint[texture.MipCount];
                        for (int i = 0; i < texture.MipCount - 1; i++)
                        {
                            array2[i + 1] = array2[i] + (uint)(int)(texture.MipSizes[i] * num);
                        }
                        byte[] buffer = new byte[texture.MipSizes[0]];
                        for (int j = 0; j < num; j++)
                        {
                            for (int k = 0; k < texture.MipCount; k++)
                            {
                                int num2 = (int)texture.MipSizes[k];
                                memoryStream.Read(buffer, 0, num2);
                                memoryStream2.Position = array2[k] + num2 * j;
                                memoryStream2.Write(buffer, 0, num2);
                            }
                        }
                        array = memoryStream2.ToArray();
                    }
                    //for (int l = 0; l < 4; l++)
                    //{
                    //	texture.Unknown3[l] = 0u;
                    //}
                    texture.SetData(array);
                    texture.AssetNameHash = 0u;
                    return texture;
                }
            }
            return null;
        }


        public static void GetPixelFormat(TextureUtils.DDSHeader header, Texture textureAsset, out string pixelFormat, out TextureFlags flags)
        {
            pixelFormat = "Unknown";
            flags = 0;
            if (header.ddspf.dwFourCC == 0)
            {
                if (header.ddspf.dwRBitMask == 255
                    && header.ddspf.dwGBitMask == 65280
                    && header.ddspf.dwBBitMask == 16711680
                    && header.ddspf.dwABitMask == 4278190080u)
                {
                    pixelFormat = "R8G8B8A8_UNORM";
                }
                else
                {
                    pixelFormat = "B8G8R8A8_UNorm";
                }
            }
            else if (header.ddspf.dwFourCC == 827611204)
            {
                pixelFormat = "BC1_UNORM";
                if (textureAsset.PixelFormat == "BC1A_UNORM")
                {
                    pixelFormat = "BC1A_UNORM";
                }
            }
            else if (header.ddspf.dwFourCC == 894720068)
            {
                pixelFormat = "BC3_UNORM";
            }
            else if (header.ddspf.dwFourCC == 826889281)
            {
                pixelFormat = "BC4_UNORM";
            }
            else if (header.ddspf.dwFourCC == 843666497 || header.ddspf.dwFourCC == 1429553986)
            {
                pixelFormat = "BC5_UNORM";
            }
            else
            {
                if (!header.HasExtendedHeader)
                {
                    return;
                }
                if ((int)header.ExtendedHeader.dxgiFormat == 71)
                {
                    pixelFormat = "BC1_UNORM";
                    if (textureAsset.PixelFormat == "BC1A_UNORM")
                    {
                        pixelFormat = "BC1A_UNORM";
                    }
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 77)
                {
                    pixelFormat = "BC3_UNORM";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 80)
                {
                    pixelFormat = "BC4_UNORM";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 83)
                {
                    pixelFormat = "BC5_UNORM";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 72 && textureAsset.PixelFormat == "BC1A_SRGB")
                {
                    pixelFormat = "BC1A_SRGB";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 72)
                {
                    pixelFormat = "BC1_SRGB";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 78)
                {
                    pixelFormat = "BC3_SRGB";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 95)
                {
                    pixelFormat = "BC6U_FLOAT";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 98)
                {
                    pixelFormat = "BC7_UNORM";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 99)
                {
                    pixelFormat = "BC7_SRGB";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 61)
                {
                    pixelFormat = "R8_UNORM";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 10)
                {
                    pixelFormat = "R16G16B16A16_FLOAT";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 2)
                {
                    pixelFormat = "R32G32B32A32_FLOAT";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 67)
                {
                    pixelFormat = "R9G9B9E5_FLOAT";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 28)
                {
                    pixelFormat = "R8G8B8A8_UNORM";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 87)
                {
                    pixelFormat = "B8G8R8A8_UNORM";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 29)
                {
                    pixelFormat = "R8G8B8A8_SRGB";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 24)
                {
                    pixelFormat = "R10G10B10A2_UNORM";
                }
                else if ((int)header.ExtendedHeader.dxgiFormat == 56)
                {
                    pixelFormat = "R16_UNORM";
                    if (textureAsset.PixelFormat == "D16_UNORM")
                    {
                        pixelFormat = "D16_UNORM";
                    }
                }
            }
        }

        private int[] swi { get; } = new int[32]
    {
        0, 4, 1, 5, 8, 12, 9, 13, 16, 20,
        17, 21, 24, 28, 25, 29, 2, 6, 3, 7,
        10, 14, 11, 15, 18, 22, 19, 23, 26, 30,
        27, 31
    };

        public byte[] ConvertToSwizzle(byte[] textureArray, Texture textureAsset)
        {
            var inputDataStream = new MemoryStream(textureArray);
            var outputDataStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(outputDataStream);
            int divisable = 4;
            var ddsSize = 4;
            int doubleDdsSize = ddsSize * 2;
            var length = textureAsset.Width * textureAsset.Height * ddsSize / 8;
            byte[] finalBuffer = new byte[length * 4];
            byte[] buffer16 = new byte[16];
            int heightByDiv = (int)textureAsset.Height / divisable;
            int widthByDiv = (int)textureAsset.Width / divisable;
            int swizzlerIndex2 = 0;
            int swizzlerIndex1 = 0;
            int heightByDivByDiv8 = heightByDiv / 8;
            if (heightByDivByDiv8 > 16)
            {
                heightByDivByDiv8 = 16;
            }
            int i5max = 1;
            if (doubleDdsSize == 16)
            {
                i5max = 1;
            }
            if (doubleDdsSize == 8)
            {
                i5max = 2;
            }
            if (doubleDdsSize == 4)
            {
                i5max = 4;
            }
            for (int i1 = 0; i1 < heightByDiv / 8 / heightByDivByDiv8; i1++)
            {
                for (int i2 = 0; i2 < widthByDiv / 4 / i5max; i2++)
                {
                    for (int i3 = 0; i3 < heightByDivByDiv8; i3++)
                    {
                        for (int i4 = 0; i4 < 32; i4++)
                        {
                            for (int i5 = 0; i5 < i5max; i5++)
                            {
                                int swizzlerIndex = swi[i4];
                                swizzlerIndex1 = swizzlerIndex / 4;
                                swizzlerIndex2 = swizzlerIndex % 4;
                                inputDataStream.Read(buffer16, 0, doubleDdsSize);

                                int index1 = (i1 * heightByDivByDiv8 + i3) * 8 + swizzlerIndex1;
                                int index2 = (i2 * 4 + swizzlerIndex2) * i5max + i5;
                                int destinationIndex4 = doubleDdsSize * (index1 * widthByDiv + index2);
                                Array.Copy(buffer16, 0, finalBuffer, destinationIndex4, doubleDdsSize);
                            }
                        }
                    }
                }
            }

            outputDataStream.Write(finalBuffer, 0, (int)length);

            var arrayBytes = outputDataStream.ToArray();
            outputDataStream.Close();
            outputDataStream.Dispose();

            return arrayBytes;
        }

        public byte[] ConvertToSwizzlePS4(byte[] textureArray, Texture textureAsset)
        {
            return textureArray;
        }
    }
}
