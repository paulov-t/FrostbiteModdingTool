using FrostySdk.FrostbiteSdk.Managers;
using System.Collections.Generic;

namespace FMT.FileTools
{
    public interface IAssetEntry
    {
        public string Filename { get; }

        public string Path { get; }

        public string Name { get; set; }

        public string DisplayName { get; }

        public bool IsModified { get; }

        public Sha1 Sha1 { get; set; }

        public IModifiedAssetEntry ModifiedEntry { get; set; }

        public long OriginalSize { get; set; }

        public List<int> Bundles { get; set; }

        public string ExtraInformation { get; set; }

    }
}
