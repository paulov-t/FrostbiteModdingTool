namespace FrostySdk.FrostySdk.IO
{
    public struct EbxBoxedValue
    {
        public uint Hash { get; set; }

        public uint Offset { get; set; }

        public ushort ClassRef { get; set; }

        public ushort Type { get; set; }

        public byte[] Data { get; set; }
    }
}
