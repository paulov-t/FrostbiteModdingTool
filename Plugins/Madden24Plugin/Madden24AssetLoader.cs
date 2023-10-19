using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Madden24Plugin
{

    public class Madden24AssetLoader : IAssetLoader
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
            if (assetManager == null || assetManager.FileSystem.SuperBundles.Count() == 0)
                return;

            int sbIndex = -1;

            foreach (var cat in assetManager.FileSystem.CatalogObjects)
            {
                foreach (var sbName in cat.SuperBundles.Keys)
                {
                    var tocFileRAW = $"{folder}{sbName}.toc";
                    string tocFileLocation = assetManager.FileSystem.ResolvePath(tocFileRAW);
                    if (string.IsNullOrEmpty(tocFileLocation) || !File.Exists(tocFileLocation))
                        continue;

                    assetManager.Logger.Log($"Loading data ({tocFileRAW})");
                    using TOCFile tocFile = new TOCFile(tocFileRAW, true, true, false, sbIndex, false);
                    sbIndex++;
                }
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
