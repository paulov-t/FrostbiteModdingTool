namespace FrostySdk
{
    public class ManifestFileInfo
    {
        public ManifestFileRef file { get; set; }
        public ManifestFileRef FileReference { get { return file; } set { file = FileReference; } }

        public uint offset { get; set; }

        public uint Offset { get { return offset; } set { offset = value; } }

        public long size { get; set; }

        public long Size { get { return size; } set { size = value; } }


        public bool isChunk { get; set; }

        public bool IsChunk { get { return isChunk; } set { isChunk = value; } }

        public long OffsetPosition { get; set; }

        public long? ModifiedOffset { get; set; }
        public long? ModifiedSize { get; set; }
    }
}
