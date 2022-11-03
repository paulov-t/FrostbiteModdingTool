using System;
using System.IO;
using System.Reflection;
using FrostySdk.IO;
using Modding.Categories;
using FrostySdk;
using FrostySdk.Managers;
using Standart.Hash.xxHash;
using FrostySdk.Frosty.FET;
using FrostbiteSdk.Frosty.Abstract;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Linq;
using FrostySdk.IO._2022.Readers;

namespace FrostySdk
{
	public class FIFAModReader : BaseModReader
	{
		private class BundleResource : BaseModResource
		{
			private int superBundleName;

			public override ModResourceType Type => ModResourceType.Bundle;

            

            public override void Read(NativeReader reader, uint modVersion = 6u)
			{
				base.Read(reader, modVersion);
				name = ReadString(reader, modVersion);
				superBundleName = reader.ReadInt32LittleEndian();
			}

			public override void FillAssetEntry(object entry)
			{
				BundleEntry obj = (BundleEntry)entry;
				obj.Name = name;
				obj.SuperBundleId = superBundleName;
			}
		}

		private class ResResource : BaseModResource
		{
			private uint resType;

			private ulong resRid;

			private byte[] resMeta;

			public override ModResourceType Type => ModResourceType.Res;

            

			//public void Read(NativeReader reader, )
			public override void Read(NativeReader reader, uint modVersion = 6)
			{
				base.Read(reader, modVersion);
				//base.Read(reader);
				//resourceIndex = reader.ReadInt32LittleEndian();
				//if (resourceIndex != -1)
				//{
				//    name = ReadString(reader, modVersion);
				//    sha1 = reader.ReadSha1();
				//    size = reader.ReadInt64LittleEndian();
				//    flags = reader.ReadByte();
				//    handlerHash = reader.ReadInt32LittleEndian();
				//    userData = "";
				//    if (modVersion >= 3)
				//    {
				//        userData = ReadString(reader, modVersion);
				//    }
				//    int num = reader.ReadInt32LittleEndian();
				//    for (int i = 0; i < num; i++)
				//    {
				//        bundlesToModify.Add(reader.ReadInt32LittleEndian());
				//    }
				//    num = reader.ReadInt32LittleEndian();
				//    for (int j = 0; j < num; j++)
				//    {
				//        bundlesToAdd.Add(reader.ReadInt32LittleEndian());
				//    }
				//}
				resType = reader.ReadUInt32LittleEndian();
				resRid = reader.ReadUInt64LittleEndian();
				resMeta = reader.ReadBytes(reader.ReadInt32LittleEndian());
			}

			public override void FillAssetEntry(object entry)
			{
				base.FillAssetEntry(entry);
				ResAssetEntry obj = (ResAssetEntry)entry;
				obj.ResType = resType;
				obj.ResRid = resRid;
				obj.ResMeta = resMeta;
			}
		}

		private class ChunkResource : BaseModResource
		{
			private uint rangeStart;

			private uint rangeEnd;

			private uint logicalOffset;

			private uint logicalSize;

			private int h32;

			private int firstMip;

			public override ModResourceType Type => ModResourceType.Chunk;

			public override void Read(NativeReader reader, uint modVersion = 6u)
			{
				base.Read(reader, modVersion);
				rangeStart = reader.ReadUInt32LittleEndian();
				rangeEnd = reader.ReadUInt32LittleEndian();
				logicalOffset = reader.ReadUInt32LittleEndian();
				logicalSize = reader.ReadUInt32LittleEndian();
				h32 = reader.ReadInt32LittleEndian();
				firstMip = reader.ReadInt32LittleEndian();
			}

			public override void FillAssetEntry(object entry)
			{
				base.FillAssetEntry(entry);
				ChunkAssetEntry chunkAssetEntry = (ChunkAssetEntry)entry;
				chunkAssetEntry.Id = new Guid(name);
				chunkAssetEntry.RangeStart = rangeStart;
				chunkAssetEntry.RangeEnd = rangeEnd;
				chunkAssetEntry.LogicalOffset = logicalOffset;
				chunkAssetEntry.LogicalSize = logicalSize;
				chunkAssetEntry.H32 = h32;
				chunkAssetEntry.FirstMip = firstMip;
				chunkAssetEntry.IsTocChunk = base.IsTocChunk;
				if (chunkAssetEntry.FirstMip == -1 && chunkAssetEntry.RangeStart != 0)
				{
					chunkAssetEntry.FirstMip = 0;
				}
			}
		}

		public const int MinSupportedVersion = 4;

		private readonly long dataOffset;

		private readonly int dataCount;

		private readonly long startOfHeaderChecksumDataPosition;

		private readonly long endOfHeaderOffset;

		private readonly ulong headerChecksum;

		public bool HasChecksums { get; }

		public FIFAModReader(Stream stream)
			: base(stream)
		{
			try
			{
				ulong header = ReadUInt64LittleEndian();
				if (header != 72155812747760198L && header != 5498700893333637446L)
				{
					return;
				}
				Version = ReadUInt32LittleEndian();
				//if (version <= 11 && version >= 4)
				//{
				if (Version <= 28 && Version >= 4)
				{
					if (Version >= 10)
					{
						HasChecksums = ReadBoolean();
						endOfHeaderOffset = ReadInt64LittleEndian();
						headerChecksum = ReadUInt64LittleEndian();
						startOfHeaderChecksumDataPosition = base.Position;
					}
					dataOffset = ReadInt64LittleEndian();
					dataCount = ReadInt32LittleEndian();
					GameName = ReadLengthPrefixedString();
					var G = Game;
					GameVersion = ReadInt32LittleEndian();
					IsValid = true;
				}
			}
			catch(Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
		}

		public bool CheckChecksums()
		{
			if (!HasChecksums)
			{
				throw new InvalidOperationException("The mod does not contain checksums.");
			}
			long startingPosition = base.Position;
			try
			{
				ulong headerChecksum = xxHash64.ComputeHash(new SubStream(base.BaseStream, startOfHeaderChecksumDataPosition, endOfHeaderOffset - startOfHeaderChecksumDataPosition), 32768, 0uL);
				if (this.headerChecksum != headerChecksum)
				{
					return false;
				}
				base.Position = endOfHeaderOffset;
				ulong num = ReadUInt64LittleEndian();
				long offset = endOfHeaderOffset + 8;
				ulong calculatedDataChecksum = xxHash64.ComputeHash(new SubStream(base.BaseStream, offset, base.Length - offset), 32768, 0uL);
				return num == calculatedDataChecksum;
			}
			finally
			{
				base.Position = startingPosition;
			}
		}

		public FIFAModDetails ReadModDetails()
		{
			string inTitle = ReadLengthPrefixedString();
			string author = ReadLengthPrefixedString();
			ModMainCategory mainCategory = ModMainCategory.Custom;
			byte subCategoryId = 0;
			if (Version >= 6)
			{
				mainCategory = (ModMainCategory)ReadByte();
				subCategoryId = ReadByte();
			}
			string customCategory = ReadLengthPrefixedString();
			string customSubCategory = null;
			if (Version >= 7)
			{
				customSubCategory = ReadLengthPrefixedString();
			}
			if (Version < 6)
			{
				if (Enum.TryParse<ModMainCategory>(customCategory, ignoreCase: true, out var parsedMainCategory))
				{
					mainCategory = parsedMainCategory;
					subCategoryId = 0;
				}
				else
				{
					mainCategory = ModMainCategory.Custom;
					subCategoryId = 0;
				}
			}
			Enum subCategory = ModCustomSubCategory.Custom;
			string userVersion = ReadLengthPrefixedString();
			string description = ReadLengthPrefixedString();
			string outOfDateModWebsiteLink = ((Version >= 8) ? ReadLengthPrefixedString() : string.Empty);
			string discordServerLink = ((Version >= 8) ? ReadLengthPrefixedString() : string.Empty);
			string patreonLink = ((Version >= 8) ? ReadLengthPrefixedString() : string.Empty);
			string twitterLink = ((Version >= 8) ? ReadLengthPrefixedString() : string.Empty);
			string youTubeLink = ((Version >= 8) ? ReadLengthPrefixedString() : string.Empty);
			string instagramLink = ((Version >= 8) ? ReadLengthPrefixedString() : string.Empty);
			string facebookLink = ((Version >= 8) ? ReadLengthPrefixedString() : string.Empty);
			string customLink = ((Version >= 8) ? ReadLengthPrefixedString() : string.Empty);
			uint screenshotsCount = ((Version < 5) ? 4u : ReadUInt32LittleEndian());
			return new FIFAModDetails(inTitle: inTitle, inAuthor: author, mainCategory: mainCategory, subCategory: subCategory, customCategory: customCategory, secondCustomCategory: customSubCategory, inVersion: userVersion, inDescription: description)
			{
				OutOfDateModWebsiteLink = outOfDateModWebsiteLink,
				DiscordServerLink = discordServerLink,
				PatreonLink = patreonLink,
				TwitterLink = twitterLink,
				YouTubeLink = youTubeLink,
				InstagramLink = instagramLink,
				FacebookLink = facebookLink,
				CustomLink = customLink,
				ScreenshotsCount = screenshotsCount
			};
		}

		//private LocaleIniSettings ReadLocaleIniSettings()
		//{
		//	if (Version < 9)
		//	{
		//		return new LocaleIniSettings();
		//	}
		//	LocaleIniSettings settings = new LocaleIniSettings();
		//	int count = Read7BitEncodedInt();
		//	for (int i = 0; i < count; i++)
		//	{
		//		string description = ReadLengthPrefixedString();
		//		string contents = ReadLengthPrefixedString();
		//		settings.AddLocaleIniFile(new LocaleIniFile
		//		{
		//			Description = description,
		//			Contents = contents
		//		});
		//	}
		//	return settings;
		//}

		//private InitFsSettings ReadInitFsModifications()
		//{
		//	if (Version < 11)
		//	{
		//		return new InitFsSettings();
		//	}
		//	InitFsSettings settings = new InitFsSettings();
		//	int count = Read7BitEncodedInt();
		//	for (int i = 0; i < count; i++)
		//	{
		//		string description = ReadLengthPrefixedString();
		//		int filesInModification = Read7BitEncodedInt();
		//		InitFSModification modification = new InitFSModification
		//		{
		//			Description = description
		//		};
		//		settings.AddInitFsModification(modification);
		//		for (int j = 0; j < filesInModification; j++)
		//		{
		//			string fileName = ReadLengthPrefixedString();
		//			int fileLength = Read7BitEncodedInt();
		//			byte[] fileData = ReadBytes(fileLength);
		//			modification.ModifyFile(fileName, fileData);
		//		}
		//		modification.ClearDirtyFlag();
		//	}
		//	settings.ClearDirtyFlag();
		//	return settings;
		//}

		public BaseModResource[] ReadResources()
		{
			if (Version >= 10)
			{
				base.Position = endOfHeaderOffset + 8;
			}
			int num = ReadInt32LittleEndian();
			BaseModResource[] array = new BaseModResource[num];
			for (int i = 0; i < num; i++)
			{
				var s = ReadByte();
                switch (s)
				{
					case 0:
						array[i] = new EmbeddedResource();
						break;
					case 1:
						array[i] = new EbxResource();
						break;
					case 2:
						array[i] = new ResResource();
						break;
					case 3:
						array[i] = new ChunkResource();
						break;
					case 5:
						array[i] = new BundleResource();
						break;
				}
				try
				{
					var r = array[i];
					if(r != null)
						r.Read(this, Version);
					else
					{
						break;
					}
				}
				catch
				{

				}
			}
			return array.Where(x => x != null).ToArray();
		}

		public byte[] GetResourceData(BaseModResource resource)
		{
			if (resource == null)
			{
				throw new ArgumentNullException("resource");
			}
			if (resource.ResourceIndex == -1)
			{
				return null;
			}
			base.Position = dataOffset + resource.ResourceIndex * 16;
			long num = ReadInt64LittleEndian();
			long num2 = ReadInt64LittleEndian();
			base.Position = dataOffset + dataCount * 16 + num;
			var b = ReadBytes((int)num2);
			//if (resource.GetType().Name.Contains("ebx", StringComparison.OrdinalIgnoreCase))
			//{
			//	var db = new CasReader(new MemoryStream(b)).Read();
			//	EbxReader22B ebxReader = new EbxReader22B(new MemoryStream(db), false);
			//	ebxReader.ReadObject();
			//}
			return b;
		}

		public (long position, long length) GetResourceDataOffset(BaseModResource resource)
		{
			if (resource == null)
			{
				throw new ArgumentNullException("resource");
			}
			if (resource.ResourceIndex == -1)
			{
				return (0L, 0L);
			}
			base.Position = dataOffset + resource.ResourceIndex * 16;
			long resourceOffset = ReadInt64LittleEndian();
			long resourceLength = ReadInt64LittleEndian();
			return (dataOffset + dataCount * 16 + resourceOffset, resourceLength);
		}
	}
}
