using DirectXTexNet;
using FMT.FileTools;
using FMT.Logging;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Frostbite.Textures
{
    public class TextureExporter
    {

        public virtual void Export(Texture textureAsset, string filename, string filterType)
        {
            byte[] array = WriteToDDS(textureAsset);
            if (filterType.ToLower() == "*.dds")
            {
                using (var fs = new FileStream(filename, FileMode.Create))
                using (var nativeWriter = new NativeWriter(fs))
                {
                    nativeWriter.Write(array);
                }
                return;
            }
            TextureUtils.ImageFormat format = TextureUtils.ImageFormat.DDS;
            switch (filterType.ToLower())
            {
                case "*.png":
                    format = TextureUtils.ImageFormat.PNG;
                    break;
                case "*.tga":
                    format = TextureUtils.ImageFormat.TGA;
                    break;
                case "*.hdr":
                    format = TextureUtils.ImageFormat.HDR;
                    break;
            }
            if (textureAsset.Type == TextureType.TT_2d)
            {
                using (var fs = new FileStream(filename, FileMode.Create))
                using (NativeWriter nativeWriter2 = new NativeWriter(fs))
                {
                    TextureUtils.BlobData pOutData = default(TextureUtils.BlobData);
                    TextureUtils.ConvertDDSToImage(array, array.Length, format, ref pOutData);
                    nativeWriter2.Write(pOutData.Data);
                    TextureUtils.ReleaseBlob(pOutData);
                }
                return;
            }
            int num = 0;
            string[] array2 = null;
            if (textureAsset.Type == TextureType.TT_Cube)
            {
                num = 6;
                array2 = new string[6]
                {
                    "px",
                    "nx",
                    "py",
                    "ny",
                    "pz",
                    "nz"
                };
                FileInfo fileInfo = new FileInfo(filename);
                for (int i = 0; i < num; i++)
                {
                    array2[i] = string.Format("{0}_{1}{2}", fileInfo.FullName.Replace(fileInfo.Extension, ""), array2[i], fileInfo.Extension);
                }
            }
            else
            {
                num = textureAsset.SliceCount;
                array2 = new string[num];
                FileInfo fileInfo2 = new FileInfo(filename);
                for (int j = 0; j < num; j++)
                {
                    array2[j] = string.Format("{0}_{1}{2}", fileInfo2.FullName.Replace(fileInfo2.Extension, ""), j.ToString("D3"), fileInfo2.Extension);
                }
            }
            TextureUtils.BlobData[] pOutDatas = new TextureUtils.BlobData[num];
            TextureUtils.ConvertDDSToImages(array, array.Length, format, ref pOutDatas, num);
            for (int k = 0; k < num; k++)
            {
                using (NativeWriter nativeWriter3 = new NativeWriter(new FileStream(array2[k], FileMode.Create)))
                {
                    nativeWriter3.Write(pOutDatas[k].Data);
                    TextureUtils.ReleaseBlob(pOutDatas[k]);
                }
            }
            array = null;
            array2 = null;
            GC.Collect();
        }

        public virtual Stream ExportToStream(Texture textureAsset)
        {
            return ExportToStream(textureAsset, TextureUtils.ImageFormat.PNG);
        }

        public virtual Stream ExportToStream(Texture textureAsset, TextureUtils.ImageFormat imageFormat)
        {

#if DEBUG
            DebugBytesToFileLogger.Instance.WriteAllBytes("DecompressedImageData.bin", ((MemoryStream)textureAsset.Data).ToArray());
#endif

            byte[] array = WriteToDDS(textureAsset);
            if (imageFormat == TextureUtils.ImageFormat.DDS)
                return new MemoryStream(array);

            if (textureAsset.Type == TextureType.TT_2d)
            {
                //TextureUtils.BlobData pOutData = default(TextureUtils.BlobData);
                if(!TextureUtils.TryConvertDDSToImage(array, imageFormat, out var outputData))
                {
                    return null;
                }

#if DEBUG
                DebugBytesToFileLogger.Instance.WriteAllBytes("DecompressedImageData_DDS.bin", outputData);
#endif
                return new MemoryStream(outputData);
            }

            return null;
        }

        public virtual byte[] WriteToDDS(Texture textureAsset)
        {
            TextureUtils.DDSHeader dDSHeader = new TextureUtils.DDSHeader();
            dDSHeader.dwHeight = textureAsset.Height;
            dDSHeader.dwWidth = textureAsset.Width;
            dDSHeader.dwPitchOrLinearSize = (int)textureAsset.MipSizes[0];
            dDSHeader.dwMipMapCount = textureAsset.MipCount;
            if (textureAsset.MipCount > 1)
            {
                dDSHeader.dwFlags |= TextureUtils.DDSFlags.MipMapCount;
                dDSHeader.dwCaps |= (TextureUtils.DDSCaps)4194312;
            }
            switch (textureAsset.Type)
            {
                case TextureType.TT_2d:
                    dDSHeader.ExtendedHeader.resourceDimension = (ResourceDimension)3;
                    dDSHeader.ExtendedHeader.arraySize = 1u;
                    break;
                case TextureType.TT_2dArray:
                    dDSHeader.ExtendedHeader.resourceDimension = (ResourceDimension)3;
                    dDSHeader.ExtendedHeader.arraySize = textureAsset.Depth;
                    break;
                case TextureType.TT_Cube:
                    dDSHeader.dwCaps2 = (TextureUtils.DDSCaps2)65024;
                    dDSHeader.ExtendedHeader.resourceDimension = (ResourceDimension)3;
                    dDSHeader.ExtendedHeader.arraySize = 1u;
                    dDSHeader.ExtendedHeader.miscFlag = 4u;
                    break;
                case TextureType.TT_3d:
                    dDSHeader.dwFlags |= TextureUtils.DDSFlags.Depth;
                    dDSHeader.dwCaps2 |= TextureUtils.DDSCaps2.Volume;
                    dDSHeader.dwDepth = textureAsset.Depth;
                    dDSHeader.ExtendedHeader.resourceDimension = (ResourceDimension)4;
                    dDSHeader.ExtendedHeader.arraySize = 1u;
                    break;
            }
            string pxFormat = textureAsset.PixelFormat;// ((Format)int.Parse(textureAsset.PixelFormat)).ToString();
            //Vortice.DXGI.Format vorticeFormat = (Vortice.DXGI.Format)textureAsset.PixelFormatNumber;
            if (pxFormat.StartsWith("BC") && (textureAsset.Flags.HasFlag(TextureFlags.SrgbGamma)))
            {
                pxFormat = pxFormat.Replace("UNORM", "SRGB");
            }

            if (pxFormat.StartsWith("BC") && textureAsset.Flags.HasFlag(TextureFlags.Ps4))
            {
                pxFormat = pxFormat.Replace("SRGB", "UNORM");
            }

            switch (pxFormat)
            {
                case "NormalDXT1":
                    dDSHeader.ddspf.dwFourCC = 827611204;
                    break;
                case "NormalDXN":
                    dDSHeader.ddspf.dwFourCC = 843666497;
                    break;
                case "BC1A_SRGB":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)72;
                    break;
                case "BC1A_UNORM":
                    dDSHeader.ddspf.dwFourCC = 827611204;
                    break;
                case "BC1_SRGB":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)72;
                    break;
                case "BC1_UNORM":
                    dDSHeader.ddspf.dwFourCC = 827611204;
                    break;
                case "BC2_SRGB":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)75;
                    break;
                case "BC3_SRGB":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)78;
                    break;
                case "BC3A_SRGB":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)78;
                    break;
                case "BC3_UNORM":
                    dDSHeader.ddspf.dwFourCC = 894720068;
                    break;
                case "BC3A_UNORM":
                    dDSHeader.ddspf.dwFourCC = 826889281;
                    break;
                case "BC4_UNORM":
                    dDSHeader.ddspf.dwFourCC = 826889281;
                    break;
                case "BC5_UNORM":
                    dDSHeader.ddspf.dwFourCC = 843666497;
                    break;
                case "BC6U_FLOAT":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)95;
                    break;
                case "BC7":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)98;
                    break;
                case "BC7_SRGB":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)99;
                    break;
                case "BC7_UNORM":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)98;
                    break;



                case "R8_UNORM":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)61;
                    break;
                case "R16G16B16A16_FLOAT":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)10;
                    break;
                case "ARGB32F":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)2;
                    break;
                case "R32G32B32A32_FLOAT":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)2;
                    break;
                case "R9G9B9E5F":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)67;
                    break;
                case "R9G9B9E5_FLOAT":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)67;
                    break;
                case "R8G8B8A8_UNORM":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)28;
                    break;
                case "R8G8B8A8_SRGB":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)29;
                    break;
                case "R10G10B10A2_UNORM":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)24;
                    break;
                case "L8":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)61;
                    break;
                case "L16":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)56;
                    break;
                case "ARGB8888":
                    dDSHeader.HasExtendedHeader = true;
                    dDSHeader.ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)28;
                    break;
                //default:
                //                dDSHeader.HasExtendedHeader = true;
                //	dDSHeader.ExtendedHeader.dxgiFormat = vorticeFormat;
                //                //dDSHeader.ddspf.dwFourCC = BitConverter.ToInt32(Encoding.UTF8.GetBytes("DX10"));
                //                //dDSHeader.ddspf.dwFourCC = 0;
                //                break;
                default:
                    dDSHeader.ddspf.dwFourCC = 0;
                    break;
            }

            //switch(vorticeFormat)
            //         {
            //	default:
            //		dDSHeader.HasExtendedHeader = true;
            //		dDSHeader.ExtendedHeader.dxgiFormat = vorticeFormat;
            //		dDSHeader.ddspf.dwFourCC = 808540228;
            //		break;
            //}

            if (dDSHeader.HasExtendedHeader && dDSHeader.ddspf.dwFourCC == 0)
            {
                dDSHeader.ddspf.dwFourCC = 808540228;
            }
            MemoryStream memoryStream = textureAsset.Data as MemoryStream;
            memoryStream.Position = 0L;
            //byte[] array = null;
            using (NativeWriter nativeWriter = new NativeWriter(new MemoryStream()))
            {
                dDSHeader.Write(nativeWriter);
                if (textureAsset.Type == TextureType.TT_Cube || textureAsset.Type == TextureType.TT_2dArray)
                {
                    int num = 6;
                    if (textureAsset.Type == TextureType.TT_2dArray)
                    {
                        num = textureAsset.Depth;
                    }
                    uint[] array2 = new uint[textureAsset.MipCount];
                    for (int i = 0; i < textureAsset.MipCount - 1; i++)
                    {
                        array2[i + 1] = (uint)((int)array2[i] + (int)(textureAsset.MipSizes[i] * num));
                    }
                    byte[] buffer = new byte[textureAsset.MipSizes[0]];
                    for (int j = 0; j < num; j++)
                    {
                        for (int k = 0; k < textureAsset.MipCount; k++)
                        {
                            int num2 = (int)textureAsset.MipSizes[k];
                            memoryStream.Position = array2[k] + num2 * j;
                            memoryStream.Read(buffer, 0, num2);
                            nativeWriter.Write(buffer, 0, num2);
                        }
                    }
                }
                else
                {
                    // If it is a PS4 texture. Make some changes.
                    if (textureAsset.Flags.HasFlag(TextureFlags.Ps4))
                    {
		                var arraySize = textureAsset.width * textureAsset.height * 4 / 8;
                        //byte[] array = new byte[arraySize];
                        byte[] array = new byte[textureAsset.Data.Length];

                        using (var nr = new NativeReader(new MemoryStream(memoryStream.ToArray())))
                        {
                            using (var nw = new NativeWriter(new MemoryStream()))
                            {
                                byte[] buf = new byte[16];
                                int h = textureAsset.height / 4;
                                int w = textureAsset.width / 4;
                                int num5 = 4;
                                for (int i = 0; i < (h + 7) / 8; i++)
                                {
                                    for (int j = 0; j < (w + 7) / 8; j++)
                                    {
                                        for (int k = 0; k < 64; k++)
                                        {
                                            int mortChange = morton(k, 8, 8);
                                            int num9 = mortChange / 8;
                                            int num10 = mortChange % 8;
                                            nr.Read(buf, 0, num5);
                                            if (j * 8 + num10 < w && i * 8 + num9 < h)
                                            {
                                                int destinationIndex = num5 * ((i * 8 + num9) * w + j * 8 + num10);
                                                Array.Copy(buf, 0, array, destinationIndex, num5);
                                            }
                                        }
                                    }
                                }
                                nw.Write(array, 0, (int)textureAsset.Data.Length);
                            }
                        }
                        nativeWriter.Write(array);

                    }
                    else 
                    {
                        nativeWriter.Write(memoryStream.ToArray());
                    }
                }
                var finalBuffer = ((MemoryStream)nativeWriter.BaseStream).GetBuffer();
                return finalBuffer;
            }
        }

        private static int morton(int t, int sx, int sy)
        {
            int num = 0;
            int num2 = 0;
            int num3;
            int num4 = (num3 = 1);
            int num5 = t;
            int num6 = sx;
            int num7 = sy;
            num = 0;
            num2 = 0;
            while (num6 > 1 || num7 > 1)
            {
                if (num6 > 1)
                {
                    num += num4 * (num5 & 1);
                    num5 >>= 1;
                    num4 *= 2;
                    num6 >>= 1;
                }
                if (num7 > 1)
                {
                    num2 += num3 * (num5 & 1);
                    num5 >>= 1;
                    num3 *= 2;
                    num7 >>= 1;
                }
            }
            return num2 * sx + num;
        }
    }
}


namespace Frostbite.Textures
{
    public static class TextureUtils
    {
        public enum DDSCaps
        {
            Complex = 8,
            MipMap = 0x400000,
            Texture = 0x1000
        }

        public enum DDSCaps2
        {
            CubeMap = 0x200,
            CubeMapPositiveX = 0x400,
            CubeMapNegativeX = 0x800,
            CubeMapPositiveY = 0x1000,
            CubeMapNegativeY = 0x2000,
            CubeMapPositiveZ = 0x4000,
            CubeMapNegativeZ = 0x8000,
            Volume = 0x200000,
            CubeMapAllFaces = 64512
        }

        public enum DDSFlags
        {
            Caps = 1,
            Height = 2,
            Width = 4,
            Pitch = 8,
            PixelFormat = 0x1000,
            MipMapCount = 0x20000,
            LinearSize = 0x80000,
            Depth = 0x800000,
            Required = 4103
        }

        public enum DDSPFFlags
        {
            AlphaPixels = 1,
            Alpha = 2,
            FourCC = 4,
            RGB = 0x40,
            YUV = 0x200,
            Luminance = 0x20000
        }

        public struct DDSHeaderDX10
        {
            public Vortice.DXGI.Format dxgiFormat;

            public ResourceDimension resourceDimension;

            public uint miscFlag;

            public uint arraySize;

            public uint miscFlags2;
        }

        public struct DDSPixelFormat
        {
            public int dwSize;

            public DDSPFFlags dwFlags;

            public int dwFourCC;

            public int dwRGBBitCount;

            public uint dwRBitMask;

            public uint dwGBitMask;

            public uint dwBBitMask;

            public uint dwABitMask;
        }

        public class DDSHeader
        {
            public int dwMagic;

            public int dwSize;

            public DDSFlags dwFlags;

            public int dwHeight;

            public int dwWidth;

            public int dwPitchOrLinearSize;

            public int dwDepth;

            public int dwMipMapCount;

            public int[] dwReserved1;

            public DDSPixelFormat ddspf;

            public DDSCaps dwCaps;

            public DDSCaps2 dwCaps2;

            public int dwCaps3;

            public int dwCaps4;

            public int dwReserved2;

            public bool HasExtendedHeader;

            public DDSHeaderDX10 ExtendedHeader;

            public DDSHeader()
            {
                dwMagic = 542327876;
                dwSize = 124;
                dwFlags = DDSFlags.Required;
                dwDepth = 0;
                dwCaps = DDSCaps.Texture;
                dwCaps2 = 0;
                dwReserved1 = new int[11];
                ddspf.dwSize = 32;
                ddspf.dwFlags = DDSPFFlags.FourCC;
                HasExtendedHeader = false;
            }

            public void Write(NativeWriter writer)
            {
                writer.Write(dwMagic);
                writer.Write(dwSize);
                writer.Write((int)dwFlags);
                writer.Write(dwHeight);
                writer.Write(dwWidth);
                writer.Write(dwPitchOrLinearSize);
                writer.Write(dwDepth);
                writer.Write(dwMipMapCount);
                for (int i = 0; i < 11; i++)
                {
                    writer.Write(dwReserved1[i]);
                }
                writer.Write(ddspf.dwSize);
                writer.Write((int)ddspf.dwFlags);
                writer.Write(ddspf.dwFourCC);
                writer.Write(ddspf.dwRGBBitCount);
                writer.Write(ddspf.dwRBitMask);
                writer.Write(ddspf.dwGBitMask);
                writer.Write(ddspf.dwBBitMask);
                writer.Write(ddspf.dwABitMask);
                writer.Write((int)dwCaps);
                writer.Write((int)dwCaps2);
                writer.Write(dwCaps3);
                writer.Write(dwCaps4);
                writer.Write(dwReserved2);
                if (HasExtendedHeader)
                {
                    writer.Write((uint)(int)ExtendedHeader.dxgiFormat);
                    writer.Write((uint)(int)ExtendedHeader.resourceDimension);
                    writer.Write(ExtendedHeader.miscFlag);
                    writer.Write(ExtendedHeader.arraySize);
                    writer.Write(ExtendedHeader.miscFlags2);
                }
            }

            public bool Read(NativeReader reader)
            {
                dwMagic = reader.ReadInt();
                if (dwMagic != 542327876)
                {
                    return false;
                }
                dwSize = reader.ReadInt();
                if (dwSize != 124)
                {
                    return false;
                }
                dwFlags = (DDSFlags)reader.ReadInt();
                dwHeight = reader.ReadInt();
                dwWidth = reader.ReadInt();
                dwPitchOrLinearSize = reader.ReadInt();
                dwDepth = reader.ReadInt();
                dwReserved1 = new int[11];
                dwMipMapCount = reader.ReadInt();
                for (int i = 0; i < 11; i++)
                {
                    dwReserved1[i] = reader.ReadInt();
                }
                ddspf.dwSize = reader.ReadInt();
                ddspf.dwFlags = (DDSPFFlags)reader.ReadInt();
                ddspf.dwFourCC = reader.ReadInt();
                ddspf.dwRGBBitCount = reader.ReadInt();
                ddspf.dwRBitMask = reader.ReadUInt();
                ddspf.dwGBitMask = reader.ReadUInt();
                ddspf.dwBBitMask = reader.ReadUInt();
                ddspf.dwABitMask = reader.ReadUInt();
                dwCaps = (DDSCaps)reader.ReadInt();
                dwCaps2 = (DDSCaps2)reader.ReadInt();
                dwCaps3 = reader.ReadInt();
                dwCaps4 = reader.ReadInt();
                dwReserved2 = reader.ReadInt();
                if (ddspf.dwFourCC == 808540228)
                {
                    HasExtendedHeader = true;
                    ExtendedHeader.dxgiFormat = (Vortice.DXGI.Format)reader.ReadUInt();
                    ExtendedHeader.resourceDimension = (ResourceDimension)reader.ReadUInt();
                    ExtendedHeader.miscFlag = reader.ReadUInt();
                    ExtendedHeader.arraySize = reader.ReadUInt();
                    ExtendedHeader.miscFlags2 = reader.ReadUInt();
                }
                return true;
            }
        }

        public struct BlobData
        {
            private IntPtr data;

            private long size;

            public byte[] Data
            {
                get
                {
                    byte[] array = new byte[size];
                    Marshal.Copy(data, array, 0, (int)size);
                    return array;
                }
            }
        }

        public enum ImageFormat
        {
            PNG,
            TGA,
            HDR,
            DDS,
            JPG
        }

        public struct TextureImportOptions
        {
            public TextureType type;

            public Format format;

            public bool generateMipmaps;

            public int mipmapsFilter;

            public bool resizeTexture;

            public int resizeFilter;

            public int resizeWidth;

            public int resizeHeight;
        }

        [DllImport("thirdparty/dxtex.dll")]
        public static extern void ConvertDDSToImage(byte[] pData, long iDataSize, ImageFormat format, ref BlobData pOutData);

        [DllImport("thirdparty/dxtex.dll")]
        public static extern void ConvertDDSToImages(byte[] pData, long iDataSize, ImageFormat format, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] ref BlobData[] pOutDatas, int pOutCount);

        [DllImport("thirdparty/dxtex.dll")]
        public static extern void ConvertImageToDDS(byte[] pData, long iDataSize, ImageFormat origFormat, TextureImportOptions options, ref BlobData pOutData);

        [DllImport("thirdparty/dxtex.dll")]
        public static extern void ConvertImagesToDDS(byte[] pData, long[] iDataSize, long iCount, ImageFormat origFormat, TextureImportOptions options, ref BlobData pOutData);

        [DllImport("thirdparty/dxtex.dll")]
        public static extern void ReleaseBlob(BlobData pData);

        public static byte[] ConvertPNGtoDDS(byte[] inData, AssetEntry originalEntry)
        {
            //Texture texture = new Texture()

            //BlobData blobData = new BlobData();
            //TextureUtils.TextureImportOptions options = default(TextureUtils.TextureImportOptions);
            //options.type = TextureType.TT_2d;
            //options.format = Format.TextureUtils.ToShaderFormat(textureAsset.PixelFormat, (textureAsset.Flags & TextureFlags.SrgbGamma) != 0);
            //options.generateMipmaps = (textureAsset.MipCount > 1);
            //options.mipmapsFilter = 0;
            //options.resizeTexture = false;
            //options.resizeFilter = 0;
            //options.resizeHeight = 0;
            //options.resizeWidth = 0;
            //TextureUtils.ConvertImageToDDS(inData, inData.Length, imageFormat, options, ref blobData);

            //return blobData.Data;
            return null;
        }

        private static ScratchImage LoadScratchImage(byte[] data, TextureUtils.ImageFormat dataFormat)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            GCHandle pinnedInputData = default(GCHandle);
            try
            {
                pinnedInputData = GCHandle.Alloc(data, GCHandleType.Pinned);
                return dataFormat switch
                {
                    TextureUtils.ImageFormat.PNG => TexHelper.Instance.LoadFromWICMemory(pinnedInputData.AddrOfPinnedObject(), data.Length, WIC_FLAGS.NONE),
                    TextureUtils.ImageFormat.HDR => TexHelper.Instance.LoadFromHDRMemory(pinnedInputData.AddrOfPinnedObject(), data.Length),
                    TextureUtils.ImageFormat.DDS => TexHelper.Instance.LoadFromDDSMemory(pinnedInputData.AddrOfPinnedObject(), data.Length, DDS_FLAGS.NONE),
                    _ => throw new ArgumentException("Unhandled input data format.", "dataFormat"),
                };
            }
            finally
            {
                if (pinnedInputData.IsAllocated)
                {
                    pinnedInputData.Free();
                }
            }
        }

        public static bool TryConvertDDSToImage(byte[] inputData, TextureUtils.ImageFormat outputFormat, out byte[] outputData)
        {
            if (inputData == null)
            {
                throw new ArgumentNullException("inputData");
            }
            outputData = null;

            ScratchImage image = null;
            ScratchImage tmpScratchImage = null;
            try
            {
                image = LoadScratchImage(inputData, TextureUtils.ImageFormat.DDS);
                TexMetadata meta = image.GetMetadata();
                Lazy<bool> lazyIsOpaqueAlpha = new Lazy<bool>(delegate
                {
                    DDSHeader dDSHeader = new DDSHeader();
                    using (var nr = new NativeReader(new MemoryStream(inputData)))
                        dDSHeader.Read(nr);
                    return dDSHeader.HasExtendedHeader && dDSHeader.ExtendedHeader.miscFlags2 == 3;
                });
                if (TexHelper.Instance.IsCompressed(meta.Format))
                {
                    tmpScratchImage = image.Decompress(0, DXGI_FORMAT.UNKNOWN);
                    image.Dispose();
                    image = tmpScratchImage;
                }
                DXGI_FORMAT dXGI_FORMAT = ((outputFormat == TextureUtils.ImageFormat.HDR) ? DXGI_FORMAT.R32G32B32A32_FLOAT : ((!lazyIsOpaqueAlpha.Value) ? DXGI_FORMAT.R8G8B8A8_UNORM_SRGB : DXGI_FORMAT.B8G8R8X8_UNORM_SRGB));
                DXGI_FORMAT dxgiOutputFormat = dXGI_FORMAT;
                if (image.GetMetadata().Format != dxgiOutputFormat)
                {
                    tmpScratchImage = image.Convert(dxgiOutputFormat, TEX_FILTER_FLAGS.SRGB_IN, 0.5f);
                    image.Dispose();
                    image = tmpScratchImage;
                }
                using UnmanagedMemoryStream unmanagedOutputStream = outputFormat switch
                {
                    TextureUtils.ImageFormat.TGA => image.SaveToTGAMemory(0),
                    TextureUtils.ImageFormat.HDR => image.SaveToHDRMemory(0),
                    TextureUtils.ImageFormat.PNG => image.SaveToWICMemory(0, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(WICCodecs.PNG)),
                    TextureUtils.ImageFormat.JPG => image.SaveToJPGMemory(0, 1f),
                    _ => throw new ArgumentException("Unsupported output format.", "outputFormat"),
                };
                outputData = new byte[unmanagedOutputStream.Length];
                unmanagedOutputStream.Read(outputData);
                image.Dispose();
                return true;
            }
            catch (Exception exception)
            {
                FileLogger.WriteLine(exception.ToString());
            }
            finally
            {
                image?.Dispose();
                tmpScratchImage?.Dispose();
            }
            return false;
        }

        public static bool TryConvertImageToDDS(byte[] inputData, TextureUtils.ImageFormat inputDataFormat, DXGI_FORMAT outputFormat, TextureType textureType, out byte[] outputData)
        {
            if (inputData == null)
            {
                throw new ArgumentNullException("inputData");
            }
            if (textureType != TextureType.TT_1d && textureType != 0)
            {
                throw new ArgumentException("Only 1D and 2D textures are supported by this method. Use ConvertMultiImagesToDDS for other texture types.", "textureType");
            }
            //if (options == null)
            //{
            //    options = ConversionOptions.Default;
            //}
            ScratchImage image = null;
            ScratchImage tmpScratchImage = null;
            try
            {
                image = LoadScratchImage(inputData, inputDataFormat);
                TexMetadata meta = image.GetMetadata();
                if (TexHelper.Instance.IsCompressed(meta.Format))
                {
                    tmpScratchImage = image.Decompress(DXGI_FORMAT.UNKNOWN);
                    image.Dispose();
                    image = tmpScratchImage;
                }
                //if (options.Resize)
                //{
                //    int width = options.ResizeWidth;
                //    int height = options.ResizeHeight;
                //    if (width == 0 && height == 0)
                //    {
                //        throw new ArgumentException("ResizeWidth or ResizeHeight must be non-zero when Resize is enabled.", "options");
                //    }
                //    if (width == 0)
                //    {
                //        width = (int)((float)height / (float)meta.Height) * meta.Width;
                //    }
                //    else if (height == 0)
                //    {
                //        height = (int)((float)width / (float)meta.Width) * meta.Height;
                //    }
                //    if (meta.Height != height || meta.Width != width)
                //    {
                //        tmpScratchImage = image.Resize(width, height, TEX_FILTER_FLAGS.DEFAULT);
                //        image.Dispose();
                //        image = tmpScratchImage;
                //    }
                //}
                if (textureType == TextureType.TT_1d && image.GetMetadata().Height != 1)
                {
                    throw new InvalidDataException("The source image height must be 1 for 1D textures.");
                }
                if (TexHelper.Instance.IsCompressed(outputFormat) && image.GetMetadata().Width % 4 != 0)
                {
                    throw new InvalidDataException("The source image width must be a multiple of 4 for block-compressed textures.");
                }
                if (TexHelper.Instance.IsCompressed(outputFormat) && image.GetMetadata().Height % 4 != 0)
                {
                    throw new InvalidDataException("The source image height must be a multiple of 4 for block-compressed textures.");
                }
                //if (options.GenerateMipMaps)
                //{
                //    if (meta.Dimension == TEX_DIMENSION.TEXTURE3D)
                //    {
                //        tmpScratchImage = image.GenerateMipMaps3D(TEX_FILTER_FLAGS.DEFAULT, 0);
                //        image.Dispose();
                //        image = tmpScratchImage;
                //    }
                //    else
                //    {
                //        tmpScratchImage = image.GenerateMipMaps(TEX_FILTER_FLAGS.DEFAULT, 0);
                //        image.Dispose();
                //        image = tmpScratchImage;
                //    }
                //}
                int mipLevels = image.GetMetadata().MipLevels;
                if (TexHelper.Instance.IsCompressed(outputFormat))
                {
                    TEX_COMPRESS_FLAGS srgbCompressFlags = ((inputDataFormat == TextureUtils.ImageFormat.HDR) ? TEX_COMPRESS_FLAGS.SRGB_OUT : TEX_COMPRESS_FLAGS.SRGB);
                    //TEX_COMPRESS_FLAGS ditherFlags = (options.DitherRGB ? TEX_COMPRESS_FLAGS.RGB_DITHER : TEX_COMPRESS_FLAGS.DEFAULT) | (options.DitherAlpha ? TEX_COMPRESS_FLAGS.A_DITHER : TEX_COMPRESS_FLAGS.DEFAULT);
                    TEX_COMPRESS_FLAGS ditherFlags = TEX_COMPRESS_FLAGS.DEFAULT;
                    if (outputFormat == DXGI_FORMAT.BC6H_UF16 || outputFormat == DXGI_FORMAT.BC6H_SF16 || outputFormat == DXGI_FORMAT.BC7_UNORM || outputFormat == DXGI_FORMAT.BC7_UNORM_SRGB)
                    {
                        var flags = Vortice.Direct3D11.DeviceCreationFlags.BgraSupport;
                        int adapterIndex = 0;
                        using IDXGIFactory1 factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
                        using IDXGIAdapter adapter = factory.GetAdapter(adapterIndex);
                        ID3D11Device device;
                        var result = D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, flags, new FeatureLevel[1] { FeatureLevel.Level_11_0 }, out device);
                        try
                        {
                            result.CheckError();
                            tmpScratchImage = image.Compress(device.NativePointer, outputFormat, TEX_COMPRESS_FLAGS.PARALLEL | srgbCompressFlags | ditherFlags, 1f);
                            image.Dispose();
                            image = tmpScratchImage;
                        }
                        finally
                        {
                            device?.Dispose();
                        }
                    }
                    else
                    {
                        tmpScratchImage = image.Compress(outputFormat, TEX_COMPRESS_FLAGS.PARALLEL | srgbCompressFlags | ditherFlags, 0.5f);
                        image.Dispose();
                        image = tmpScratchImage;
                    }
                }
                else if (image.GetMetadata().Format != outputFormat)
                {
                    TEX_FILTER_FLAGS srgbFilterFlags = ((inputDataFormat == TextureUtils.ImageFormat.HDR) ? TEX_FILTER_FLAGS.SRGB_OUT : TEX_FILTER_FLAGS.SRGB);
                    tmpScratchImage = image.Convert(outputFormat, srgbFilterFlags, 0.5f);
                    image.Dispose();
                    image = tmpScratchImage;
                }
                TexMetadata outputMetadata = textureType switch
                {
                    TextureType.TT_1d => new TexMetadata(image.GetMetadata().Width, 1, 1, 1, mipLevels, (TEX_MISC_FLAG)0, (TEX_MISC_FLAG2)0, outputFormat, TEX_DIMENSION.TEXTURE1D),
                    TextureType.TT_2d => new TexMetadata(image.GetMetadata().Width, image.GetMetadata().Height, 1, 1, mipLevels, (TEX_MISC_FLAG)0, (TEX_MISC_FLAG2)0, outputFormat, TEX_DIMENSION.TEXTURE2D),
                    _ => throw new ArgumentException($"Unsupported texture type {textureType}", "textureType"),
                };
                using ScratchImage finalImage = TexHelper.Instance.InitializeTemporary((from index in Enumerable.Range(0, image.GetImageCount())
                                                                                        select image.GetImage(index)).ToArray(), outputMetadata);
                //using UnmanagedMemoryStream unmanagedOutputStream = finalImage.SaveToDDSMemory(options.OutputFlags);
                using UnmanagedMemoryStream unmanagedOutputStream = finalImage.SaveToDDSMemory(DDS_FLAGS.FORCE_DX9_LEGACY);
                outputData = new byte[unmanagedOutputStream.Length];
                unmanagedOutputStream.Read(outputData);
                return true;
            }
            catch (Exception ex)
            {
                FileLogger.WriteLine("ConvertImageToDDS failed!");
                FileLogger.WriteLine(ex.ToString());
                outputData = null;
            }
            finally
            {
                image?.Dispose();
                tmpScratchImage?.Dispose();
            }
            return false;
        }



        public static Format ToTextureFormat(string pixelFormat)
        {
            switch (pixelFormat)
            {
                case "NormalDXT1":
                    return (Format)70;
                case "NormalDXN":
                    return (Format)82;
                case "BC1A_SRGB":
                    return (Format)70;
                case "BC1A_UNORM":
                    return (Format)70;
                case "BC1_SRGB":
                    return (Format)70;
                case "BC1_UNORM":
                    return (Format)70;
                case "BC2_UNORM":
                    return (Format)73;
                case "BC3_SRGB":
                    return (Format)76;
                case "BC3_UNORM":
                    return (Format)76;
                case "BC3A_UNORM":
                    return (Format)76;
                case "BC3A_SRGB":
                    return (Format)76;
                case "BC4_UNORM":
                    return (Format)79;
                case "BC5_UNORM":
                    return (Format)82;
                case "BC6U_FLOAT":
                    return (Format)95;
                case "BC7":
                    return (Format)97;
                case "BC7_SRGB":
                    return (Format)97;
                case "BC7_UNORM":
                    return (Format)97;
                case "R8_UNORM":
                    return (Format)60;
                case "R16G16B16A16_FLOAT":
                    return (Format)10;
                case "ARGB32F":
                    return (Format)2;
                case "R32G32B32A32_FLOAT":
                    return (Format)2;
                case "R9G9B9E5F":
                    return (Format)67;
                case "R9G9B9E5_FLOAT":
                    return (Format)67;
                case "R8G8B8A8_UNORM":
                    return (Format)27;
                case "R8G8B8A8_SRGB":
                    return (Format)27;
                //case "B8G8R8A8_UNORM":
                //	//return (Format)90;
                //	return Format.B8G8R8A8_UNorm;
                case "R10G10B10A2_UNORM":
                    return (Format)23;
                case "L8":
                    return (Format)60;
                case "L16":
                    return (Format)53;
                case "ARGB8888":
                    return (Format)27;
                case "R16G16_UNORM":
                    return (Format)33;
                case "D16_UNORM":
                    return (Format)56;


                //            case "B8G8R8A8_UNorm":
                //                return Format.B8G8R8A8_UNorm;
                //case "B8G8R8A8_SRGB":
                //	return Format.B8G8R8A8_UNorm_SRgb;
                default:
                    return (Format)Enum.Parse(typeof(Format), pixelFormat);
            }
        }

        //public static Format ToShaderFormatFromPfim(string format1, string format2)
        //{
        //	var format = (format1 + format2).ToUpper();
        //	var result = string.Empty;
        //	switch (format)
        //	{
        //              //case "PFIM.DXT5DDSRGB32":
        //              //	result = "BC3_SRGB";
        //              //	break;
        //              //case "PFIM.DXT5DDSRGBA32":
        //              //	result = "BC3A_SRGB";
        //              //	break;
        //              case "PFIM.DXT3DDSRGBA32":
        //                  result = "BC2_UNORM";
        //                  break;
        //              default:
        //			result = "BC3_UNORM";
        //			break;
        //	}

        //	return ToShaderFormat(result);
        //}

        public static Format ToShaderFormat(string pixelFormat, bool bLegacySrgb = false)
        {
            pixelFormat = pixelFormat.ToUpper();
            if (bLegacySrgb && pixelFormat.StartsWith("BC"))
            {
                pixelFormat = pixelFormat.Replace("UNORM", "SRGB");
            }
            switch (pixelFormat)
            {
                case "NormalDXT1":
                    return (Format)71;
                case "NormalDXN":
                    return (Format)83;
                case "BC1A_SRGB":
                    return (Format)72;
                case "BC1A_UNORM":
                    return (Format)71;
                case "BC1_SRGB":
                    return (Format)72;
                case "BC1_UNORM":
                    return (Format)71;
                case "BC2_UNORM":
                    return (Format)74;
                case "BC3_SRGB":
                    return (Format)78;
                case "BC3_UNORM":
                    return (Format)77;
                case "BC3A_UNORM":
                    return (Format)77;
                case "BC3A_SRGB":
                    return (Format)78;
                case "BC4_UNORM":
                    return (Format)80;
                case "BC5_UNORM":
                    return (Format)83;
                case "BC6U_FLOAT":
                    return (Format)95;
                case "BC7":
                    return (Format)98;
                case "BC7_SRGB":
                    return (Format)99;
                case "BC7_UNORM":
                    return (Format)98;
                case "R8_UNORM":
                    return (Format)61;
                case "R16G16B16A16_FLOAT":
                    return (Format)10;
                case "ARGB32F":
                    return (Format)2;
                case "R32G32B32A32_FLOAT":
                    return (Format)2;
                case "R9G9B9E5F":
                    return (Format)67;
                case "R9G9B9E5_FLOAT":
                    return (Format)67;
                case "R8G8B8A8_UNORM":
                    return (Format)28;
                case "R8G8B8A8_SRGB":
                    return (Format)29;
                case "B8G8R8A8_UNORM":
                    return (Format)87;
                case "R10G10B10A2_UNORM":
                    return (Format)24;
                case "L8":
                    return (Format)61;
                case "L16":
                    return (Format)56;
                case "ARGB8888":
                    return (Format)28;
                case "R16G16_UNORM":
                    return (Format)35;
                case "D16_UNORM":
                    return (Format)56;
                default:
                    return 0;
            }
        }

        public static bool IsCompressedFormat(string pixelFormat)
        {
            bool result = true;
            switch (pixelFormat)
            {
                case "R8_UNORM":
                case "R16G16B16A16_FLOAT":
                case "R32G32B32A32_FLOAT":
                case "R9G9B9E5_FLOAT":
                case "R8G8B8A8_UNORM":
                case "R8G8B8A8_SRGB":
                case "B8G8R8A8_UNORM":
                case "R10G10B10A2_UNORM":
                case "ARGB32F":
                case "R9G9B9E5F":
                case "L8":
                case "L16":
                case "ARGB8888":
                case "D16_UNORM":
                    result = false;
                    break;
            }
            return result;
        }

        public static int GetFormatBlockSize(string pixelFormat)
        {
            int result = 8;
            switch (pixelFormat)
            {
                case "L8":
                    result = 8;
                    break;
                case "BC3_UNORM":
                case "BC3_SRGB":
                case "BC5_UNORM":
                case "BC5_SRGB":
                case "BC6U_FLOAT":
                case "BC7_UNORM":
                case "BC7_SRGB":
                case "NormalDXN":
                case "BC2_UNORM":
                case "BC3A_UNORM":
                case "L16":
                case "D16_UNORM":
                    result = 16;
                    break;
                case "R9G9B9E5_FLOAT":
                case "R8G8B8A8_UNORM":
                case "R8G8B8A8_SRGB":
                case "B8G8R8A8_UNORM":
                case "R10G10B10A2_UNORM":
                case "R9G9B9E5F":
                case "ARGB8888":
                    result = 32;
                    break;
                case "R16G16B16A16_FLOAT":
                    result = 64;
                    break;
                case "R32G32B32A32_FLOAT":
                case "ARGB32F":
                    result = 128;
                    break;
            }
            return result;
        }
    }
}


