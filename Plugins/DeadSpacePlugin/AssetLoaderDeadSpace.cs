﻿using Frosty.Hash;
using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using static FrostySdk.Managers.AssetManager;

namespace NFSUnboundPlugin
{
	public class AssetLoaderDeadSpace : IAssetLoader
	{
		public List<DbObject> AllDbObjects = new List<DbObject>();
		internal struct BundleFileInfo
		{
			public int Index;

			public int Offset;

			public int Size;

			public BundleFileInfo(int index, int offset, int size)
			{
				Index = index;
				Offset = offset;
				Size = size;
			}
		}

		public void LoadData(AssetManager assetManager, BinarySbDataHelper helper, string folder = "native_data/")
		{
			if (assetManager == null || assetManager.fs.SuperBundles.Count() == 0)
				return;

			int sbIndex = -1;

			foreach (var sbName in assetManager.fs.SuperBundles)
			{
				var tocFileRAW = $"{folder}{sbName}.toc";
				string tocFileLocation = assetManager.fs.ResolvePath(tocFileRAW);
                if (string.IsNullOrEmpty(tocFileLocation) || !File.Exists(tocFileLocation))
					continue;

				assetManager.Logger.Log($"Loading data ({tocFileRAW})");
				using TOCFile tocFile = new TOCFile(tocFileRAW, true, true, false, sbIndex, false);
                sbIndex++;
            }
        }

		public void LoadPatch(AssetManager parent, BinarySbDataHelper helper)
		{
			LoadData(parent, helper, "native_patch/");
		}

		public void Load(AssetManager parent, BinarySbDataHelper helper)
		{
			//LoadPatch(parent, helper);
			LoadData(parent, helper);
        }

		static public List<int> SearchBytePattern(byte[] pattern, byte[] bytes)
		{
			List<int> positions = new List<int>();
			int patternLength = pattern.Length;
			int totalLength = bytes.Length;
			byte firstMatchByte = pattern[0];
			for (int i = 0; i < totalLength; i++)
			{
				if (firstMatchByte == bytes[i] && totalLength - i >= patternLength)
				{
					byte[] match = new byte[patternLength];
					Array.Copy(bytes, i, match, 0, patternLength);
					if (match.SequenceEqual<byte>(pattern))
					{
						positions.Add(i);
						i += patternLength - 1;
					}
				}
			}
			return positions;
		}
	}


}