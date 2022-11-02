using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.IO;
using System.Linq;

namespace FrostySdk.Resources
{
	public class Texture : IDisposable
	{
		public uint[] mipOffsets = new uint[2];

        public TextureType type;

        public int pixelFormat;

        public uint unknown1;

        public uint unknown2 { get; set; }

        public int unknown4;

        public TextureFlags flags;

        public ushort width;

        public ushort height;

        public ushort depth;

        public ushort sliceCount;

        public byte mipCount;

        public byte firstMip;

        public Guid chunkId;

        public uint[] mipSizes = new uint[15];

        public uint chunkSize;

        public uint[] unknown3 = new uint[4];

        public uint assetNameHash;

        public string textureGroup;

        public Stream data;

        public uint logicalOffset;

        public uint logicalSize;

        public uint rangeStart;

        public uint rangeEnd;

        public uint FirstMipOffset
		{
			get
			{
				return mipOffsets[0];
			}
			set
			{
				mipOffsets[0] = value;
			}
		}

        public uint SecondMipOffset
		{
			get
			{
				return mipOffsets[1];
			}
			set
			{
				mipOffsets[1] = value;
			}
		}

        public int PixelFormatNumber
        {
			get
            {
				return pixelFormat;
            }
        }


        public string PixelFormat
		{
			get
			{
				string text = "RenderFormat";
				return Enum.Parse(TypeLibrary.GetType(text), pixelFormat.ToString()).ToString().Replace(text + "_", "");
			}
			
		}

        public TextureType Type => type;

        public TextureFlags Flags
		{
			get
			{
				return flags;
			}
			set
			{
				flags = value;
			}
		}

		public ushort Width => width;

		public ushort Height => height;

		public ushort SliceCount
		{
			get
			{
				return sliceCount;
			}
			set
			{
				sliceCount = value;
				if (type == TextureType.TT_2dArray || type == TextureType.TT_3d)
				{
					depth = sliceCount;
				}
			}
		}

		public ushort Depth => depth;

		public byte MipCount => mipCount;

		public byte FirstMip
		{
			get
			{
				return firstMip;
			}
			set
			{
				firstMip = value;
			}
		}

		public uint[] MipSizes => mipSizes;

		public string TextureGroup
		{
			get
			{
				return textureGroup;
			}
			set
			{
				textureGroup = value;
			}
		}

		public uint AssetNameHash
		{
			get
			{
				return assetNameHash;
			}
			set
			{
				assetNameHash = value;
			}
		}

		public Stream Data => data;

		public uint LogicalOffset
		{
			get
			{
				return logicalOffset;
			}
			set
			{
				logicalOffset = value;
			}
		}

		public uint LogicalSize
		{
			get
			{
				return logicalSize;
			}
			set
			{
				logicalSize = value;
			}
		}

		public uint RangeStart
		{
			get
			{
				return rangeStart;
			}
			set
			{
				rangeStart = value;
			}
		}

		public uint RangeEnd
		{
			get
			{
				return rangeEnd;
			}
			set
			{
				rangeEnd = value;
			}
		}

		public uint[] Unknown3 => unknown3;

		public Guid ChunkId
		{
			get
			{
				return chunkId;
			}
			set
			{
				chunkId = value;
			}
		}

        public ChunkAssetEntry ChunkEntry { get; set; }

        public uint ChunkSize => chunkSize;


		/// <summary>
		/// Blank Texture Object (rarely used)
		/// </summary>
		public Texture()
		{
		}

		/// <summary>
		/// Build a Texture Object from a Stream
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="am"></param>
		public Texture(Stream stream, AssetManager am)
		{
			if(stream == null)
            {
				return;
            }
			using (NativeReader nativeReader = new NativeReader(stream))
			{
				stream.Position = 0;

				
					mipOffsets[0] = nativeReader.ReadUInt();
					mipOffsets[1] = nativeReader.ReadUInt();
					type = (TextureType)nativeReader.ReadUInt();
					pixelFormat = nativeReader.ReadInt();
					//if (ProfilesLibrary.DataVersion == 20170321
					//	|| ProfilesLibrary.DataVersion == 20160927
					//	|| ProfilesLibrary.DataVersion == 20171117
					//	|| ProfilesLibrary.DataVersion == 20170929
					//	|| ProfilesLibrary.DataVersion == 20171110
					//	|| ProfilesLibrary.DataVersion == 20180807
					//	|| ProfilesLibrary.DataVersion == 20180914
					//	|| ProfilesLibrary.DataVersion == 20181207 || ProfilesLibrary.DataVersion == 20180628
					//	|| ProfilesLibrary.IsMadden20DataVersion() // Madden 20
					//	|| ProfilesLibrary.IsFIFA20DataVersion()
					//	|| ProfilesLibrary.IsMadden21DataVersion()
					//	|| ProfilesLibrary.IsFIFA21DataVersion()
					//	)
					//{
						unknown1 = nativeReader.ReadUInt();
					//}
					flags = (TextureFlags)nativeReader.ReadUShort();
				width = nativeReader.ReadUShort();
				height = nativeReader.ReadUShort();
				depth = nativeReader.ReadUShort();
				sliceCount = nativeReader.ReadUShort();
				mipCount = nativeReader.ReadByte();
				firstMip = nativeReader.ReadByte();
				if(ProfileManager.IsFIFA23DataVersion())
                {
					unknown4 = nativeReader.ReadInt();
				}
				chunkId = nativeReader.ReadGuid();
				for (int i = 0; i < 15; i++)
				{
					mipSizes[i] = nativeReader.ReadUInt();
				}
				chunkSize = nativeReader.ReadUInt();
				//if (ProfilesLibrary.DataVersion == 20181207)
				//{
				//	for (int j = 0; j < 3; j++)
				//	{
				//		unknown3[j] = nativeReader.ReadUInt();
				//	}
				//}
				//else if (ProfilesLibrary.DataVersion == 20170321)
				//{
				//	for (int k = 0; k < 4; k++)
				//	{
				//		unknown3[k] = nativeReader.ReadUInt();
				//	}
				//}
				//else if (ProfilesLibrary.DataVersion == 20170929 || ProfilesLibrary.DataVersion == 20180807)
				//{
				//	unknown3[0] = nativeReader.ReadUInt();
				//}
				assetNameHash = nativeReader.ReadUInt();
				//if (ProfilesLibrary.DataVersion == 20150223)
				//{
				//	unknown3[0] = nativeReader.ReadUInt();
				//}
				textureGroup = nativeReader.ReadSizedString(16);
				//if (ProfilesLibrary.DataVersion == 20171117 || ProfilesLibrary.DataVersion == 20180628)
				//{
				//	unknown3[0] = nativeReader.ReadUInt();
				//}
				if (am != null)
				{
					//if (am.logger != null)
					//	am.logger.Log($"Texture: Loading ChunkId: {chunkId}");

					ChunkEntry = am.GetChunkEntry(chunkId);
					data = am.GetChunk(ChunkEntry);
				}
			}
		}

		public Stream ResStream { get; set; }

		/// <summary>
		/// Build a Texture Object from a Stream
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="am"></param>
		public Texture(ResAssetEntry resAssetEntry)
		{
			ResStream = AssetManager.Instance.GetRes(resAssetEntry);
			if (ResStream == null)
			{
				return;
			}
			if (resAssetEntry == null)
			{
				return;
			}
			using (NativeReader nativeReader = new NativeReader(ResStream))
			{
				ResStream.Position = 0;

				if (!Directory.Exists("Debugging"))
					Directory.CreateDirectory("Debugging");

				if (!Directory.Exists("Debugging\\Other\\"))
					Directory.CreateDirectory("Debugging\\Other\\");

				if (File.Exists("Debugging\\Other\\_TextureExport.dat"))
					File.Delete("Debugging\\Other\\_TextureExport.dat");

				using (FileStream fileStream = new FileStream("Debugging\\Other\\_TextureExport.dat", FileMode.OpenOrCreate))
				{
					ResStream.CopyTo(fileStream);
				}
				ResStream.Position = 0;

				mipOffsets[0] = nativeReader.ReadUInt();
				mipOffsets[1] = nativeReader.ReadUInt();
				type = (TextureType)nativeReader.ReadUInt();
				pixelFormat = nativeReader.ReadInt();
				unknown1 = nativeReader.ReadUInt();
				flags = (TextureFlags)nativeReader.ReadUShort();
				width = nativeReader.ReadUShort();
				height = nativeReader.ReadUShort();
				depth = nativeReader.ReadUShort();
				sliceCount = nativeReader.ReadUShort();
				mipCount = nativeReader.ReadByte();
				firstMip = nativeReader.ReadByte();
                if (ProfileManager.IsFIFA23DataVersion())
                {
					unknown4 = nativeReader.ReadInt();
                }
				chunkId = nativeReader.ReadGuid();
				for (int i = 0; i < 15; i++)
				{
					mipSizes[i] = nativeReader.ReadUInt();
				}
				chunkSize = nativeReader.ReadUInt();
				assetNameHash = nativeReader.ReadUInt();

				if(!ProfileManager.IsFIFA23DataVersion())
					unknown2 = nativeReader.ReadUInt();

				//textureGroup = nativeReader.ReadSizedString(16);
				textureGroup = nativeReader.ReadNullTerminatedString();
				if (AssetManager.Instance.logger != null)
					AssetManager.Instance.logger.Log($"Texture: Loading ChunkId: {chunkId}");

				ChunkEntry = AssetManager.Instance.GetChunkEntry(chunkId);
				//if (ChunkEntry == null)
				//{
				//	var n = resAssetEntry.BundleNames.Select(x => x.Split('/')[x.Split('/').Length - 1]);
				//	ChunkEntry = AssetManager.Instance.GetChunkEntry(chunkId, n.First());
				//	var ChunkEntry2 = AssetManager.Instance.GetChunkEntry(chunkId, n.Last());

				//	var cbEntry = AssetManager.Instance.BundleChunks.First(x => x.Key.Item2 == chunkId);
				//}
				data = AssetManager.Instance.GetChunk(ChunkEntry);
			}
		}

		public byte[] Write()
        {
			return ToBytes();
        }

        public byte[] ToBytes()
        {
            MemoryStream memoryStream = new MemoryStream();
            using (NativeWriter nativeWriter = new NativeWriter(memoryStream))
            {
                nativeWriter.Write(mipOffsets[0]);
                nativeWriter.Write(mipOffsets[1]);
                nativeWriter.Write((uint)type);
                nativeWriter.Write(pixelFormat);

                nativeWriter.Write(unknown1);

                nativeWriter.Write((ushort)flags);
                nativeWriter.Write(width);
                nativeWriter.Write(height);
                nativeWriter.Write(depth);
                nativeWriter.Write(sliceCount);
                nativeWriter.Write(mipCount);
                nativeWriter.Write(firstMip);
                if (ProfileManager.IsFIFA23DataVersion())
                {
                    nativeWriter.Write(unknown4);
                }
                nativeWriter.Write(chunkId);
                for (int i = 0; i < 15; i++)
                {
                    nativeWriter.Write(mipSizes[i]);
                }
                nativeWriter.Write(chunkSize);
                if (ProfileManager.DataVersion == 20170321)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        nativeWriter.Write(unknown3[j]);
                    }
                }
                if (ProfileManager.DataVersion == 20170929
                    || ProfileManager.DataVersion == 20180807

                    )
                {
                    nativeWriter.Write(unknown3[0]);
                }
                nativeWriter.Write(assetNameHash);
                if (ProfileManager.DataVersion == 20150223)
                {
                    nativeWriter.Write(unknown3[0]);
                }
                if (ProfileManager.IsMadden21DataVersion())
                {
                    nativeWriter.Write(unknown2);
                    nativeWriter.WriteNullTerminatedString(textureGroup);
                }
                else
                {
                    nativeWriter.WriteFixedSizedString(textureGroup, 16);
                }
                if (ProfileManager.DataVersion == 20171117 || ProfileManager.DataVersion == 20180628)
                {
                    nativeWriter.Write(unknown3[0]);
                }
            }
            return memoryStream.ToArray();
        }

        public Texture(TextureType inType, string inFormat, ushort inWidth, ushort inHeight, ushort inDepth = 1)
		{
			type = inType;
			pixelFormat = getTextureFormat(inFormat);
			width = inWidth;
			height = inHeight;
			depth = inDepth;
			sliceCount = inDepth;
			unknown1 = 0u;
			flags = (TextureFlags)0;
			unknown3[0] = uint.MaxValue;
			unknown3[1] = uint.MaxValue;
			unknown3[2] = uint.MaxValue;
			unknown3[3] = uint.MaxValue;
		}

		~Texture()
		{
			Dispose(disposing: false);
		}

		public void SetData(Guid newChunkId, AssetManager am)
		{
			data = am.GetChunk(am.GetChunkEntry(newChunkId));
			chunkId = newChunkId;
			chunkSize = (uint)data.Length;
		}

		public void SetData(byte[] inData)
		{
			data = new MemoryStream(inData);
			// PG I have removed these
			//chunkId = Guid.Empty;
			//chunkSize = (uint)data.Length;
		}

		public void CalculateMipData(byte inMipCount, int blockSize, bool isCompressed, uint dataSize)
		{
			if (isCompressed)
			{
				blockSize /= 4;
			}
			mipCount = inMipCount;
			int num = width;
			int num2 = height;
			int num3 = depth;
			int num4 = (!isCompressed) ? 1 : 4;
			for (int i = 0; i < mipCount; i++)
			{
				int num5 = isCompressed ? (Math.Max(1, (num + 3) / 4) * blockSize) : ((num * blockSize + 7) / 8);
				mipSizes[i] = (uint)(num5 * num2);
				if (type == TextureType.TT_3d)
				{
					mipSizes[i] *= (uint)num3;
				}
				num >>= 1;
				num2 >>= 1;
				num2 = ((num2 < num4) ? num4 : num2);
				num = ((num < num4) ? num4 : num);
			}
			if (mipCount == 1)
			{
				logicalOffset = 0u;
				logicalSize = dataSize;
				flags = (TextureFlags)0;
				return;
			}
			logicalOffset = 0u;
			for (int j = 0; j < mipCount - firstMip; j++)
			{
				logicalSize |= (uint)(3 << j * 2);
			}
			logicalOffset = (dataSize & ~logicalSize);
			logicalSize = (dataSize & logicalSize);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				data.Dispose();
			}
		}

		private int getTextureFormat(string format)
		{
			string text = "RenderFormat";
			return (int)Enum.Parse(TypeLibrary.GetType(text), text + "_" + format);
		}
	}
}