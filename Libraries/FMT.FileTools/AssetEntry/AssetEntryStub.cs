using FrostySdk.FrostbiteSdk.Managers;
using System.Collections.Generic;

namespace FMT.FileTools.AssetEntry
{
    public record AssetEntryStub : IAssetEntry
    {
        public string Filename { get; set; } = null;

        public string Path { get; set; } = null;

        public string Name { get; set; } = null;

        public string DisplayName { get; set; } = null;

        public bool IsModified { get; set; }

        public Sha1 Sha1 { get; set; }

        public IModifiedAssetEntry ModifiedEntry { get; set; } = null;

        public enum EntryStubType
        {
            Frostbite_Ebx = 1,
            Frostbite_Resource = 2,
            Frostbite_Chunk = 3,
            Frostbite_ChunkFile = 4,
            Frostbite_File = 5,
            Frostbite_LTU = 6,
        }

        public EntryStubType StubType { get; set; }

        public long OriginalSize { get; set; }

        public List<int> Bundles { get; set; }
        public string ExtraInformation { get; set; }

        public AssetEntryStub(
            EntryStubType stubType
            , string fn
            , string p
            , string n
            , string dn
            , Sha1 sha
            , long? originalSize = null
            , List<int> bundles = null
            , IModifiedAssetEntry modifiedAssetEntry = null
            , string extraInfo = null)
        {
            StubType = stubType;
            Filename = fn;
            Path = p;
            Name = n;
            DisplayName = dn;
            Sha1 = sha;
            OriginalSize = originalSize.HasValue ? originalSize.Value : 0;
            Bundles = bundles;
            ModifiedEntry = modifiedAssetEntry;
            ExtraInformation = extraInfo;
        }
    }
}
