using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FC24Plugin
{

    public class FC24AssetLoader : IAssetLoader
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

        //public class BaseBundleInfo
        //{
        //	public static int BundleItemIndex = 0;

        //	public string Name { get; set; }

        //	public long Offset { get; set; }

        //	public long TOCSizePosition { get; set; }

        //	public long Size { get; set; }

        //	public long TocOffset { get; set; }

        //	public int CasIndex { get; internal set; }
        //	public int Unk { get; internal set; }

        //	public int TocBundleIndex { get; set; }

        //	public override string ToString()
        //          {
        //		return $"Offset:{Offset}-Size:{Size}-Index:{TocBundleIndex}";
        //	}

        //      }

        public void LoadData(AssetManager assetManager, BinarySbDataHelper helper, string folder = "native_data/")
        {
            //if (parent != null && parent.fs.Catalogs != null && parent.fs.Catalogs.Count() > 0)
            //{
            //	foreach (Catalog catalogInfoItem in parent.fs.EnumerateCatalogInfos().OrderBy(x=> x.PersistentIndex.HasValue ? x.PersistentIndex : 0))
            //	{
            //		foreach (string sbName in catalogInfoItem.SuperBundles.Where(x => !x.Value).Select(x => x.Key))
            //		{
            //			SuperBundleEntry superBundleEntry = parent.superBundles.Find((SuperBundleEntry a) => a.Name == sbName);
            //			int sbIndex = -1;
            //			if (superBundleEntry != null)
            //			{
            //				sbIndex = parent.superBundles.IndexOf(superBundleEntry);
            //			}
            //			else
            //			{
            //				parent.superBundles.Add(new SuperBundleEntry
            //				{
            //					Name = sbName
            //					,
            //					CatalogInfo = catalogInfoItem
            //				});
            //				sbIndex = parent.superBundles.Count - 1;
            //			}

            //			parent.Logger.Log($"Loading data ({sbName})");
            //			//string tocFile = sbName.Replace("win32", catalogInfoItem.Name);
            //			//if (parent.fs.ResolvePath(folder + tocFile + ".toc") == "")
            //			//{
            //			//	tocFile = sbName;
            //			//}
            //			string tocFile = sbName;
            //			var tocFileRAW = $"{folder}{tocFile}.toc";
            //			string tocFileLocation = parent.fs.ResolvePath(tocFileRAW);
            //			if (!string.IsNullOrEmpty(tocFileLocation) && File.Exists(tocFileLocation))
            //			{
            //				TocSbReader_FC24 tocSbReader = new TocSbReader_FC24();
            //				// TOCFile CasDataLoader automatically proceses data
            //				tocSbReader.Read(tocFileLocation, sbIndex, sbName, true, tocFileRAW);

            //			}
            //		}
            //	}
            //}

            if (assetManager == null || assetManager.FileSystem.SuperBundles.Count() == 0)
                return;

            int sbIndex = -1;

            foreach (var sbName in assetManager.FileSystem.SuperBundles)
            {
                var tocFileRAW = $"{folder}{sbName}.toc";
                string tocFileLocation = assetManager.FileSystem.ResolvePath(tocFileRAW);
                if (string.IsNullOrEmpty(tocFileLocation) || !File.Exists(tocFileLocation))
                    continue;

                if (sbName.Contains("presprematchsba"))
                    continue;

                if (sbName.Contains("storycharsb"))
                    continue;

                if (sbName.Contains("storyingamesba"))
                    continue;

                if (sbName.Contains("storysba"))
                    continue;

                if (tocFileRAW.Contains("/cs/"))
                    continue;

                assetManager.Logger.Log($"Loading data ({tocFileRAW})");
                using FC24TOCFile tocFile = new FC24TOCFile(tocFileRAW, true, true, false, sbIndex, false);
                sbIndex++;
            }
        }

        public void LoadPatch(AssetManager parent, BinarySbDataHelper helper)
        {
            LoadData(parent, helper, "native_patch/");
        }

        public void Load(AssetManager parent, BinarySbDataHelper helper)
        {
            LoadPatch(parent, helper);
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
