using FMT.FileTools;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using FrostySdk.Resources;
using ModdingSupport;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public interface Importer
    {
        public void DoImport(string path, AssetEntry assetEntry);

    }

    public interface ITextureImporter : Importer
    {
        public void DoImport(string path, EbxAssetEntry assetEntry, ref Texture textureAsset);

    }

    public interface ICacheWriter
    {
        void Write();
        void WriteEbxEntry(NativeWriter nativeWriter, EbxAssetEntry ebxEntry);
        void WriteResEntry(NativeWriter nativeWriter, ResAssetEntry resEntry);
        void WriteChunkEntry(NativeWriter nativeWriter, ChunkAssetEntry chunkEntry);
    }

    public interface ICacheReader
    {
        public ulong EbxDataOffset { get; set; }
        public ulong ResDataOffset { get; set; }
        public ulong ChunkDataOffset { get; set; }
        public ulong NameToPositionOffset { get; set; }

        public bool Read();

        public EbxAssetEntry ReadEbxAssetEntry(NativeReader nativeReader);
    }

    public interface IAssetLoader
    {
        void Load(AssetManager parent, BinarySbDataHelper helper);
    }

    public interface IAssetCompiler
    {
        public string ModDirectory { get; }

        bool PreCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter);

        bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter);

        bool PostCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter);

        bool RunGame(FileSystem fs, ILogger logger, ModExecutor modExecuter);

        bool Cleanup(FileSystem fs, ILogger logger, ModExecutor modExecuter);
    }
}
