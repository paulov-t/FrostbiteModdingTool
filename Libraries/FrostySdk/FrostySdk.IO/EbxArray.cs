using System.Security.Policy;

namespace FrostySdk.IO
{
    public struct EbxArray
    {
        public int ClassRef;

        public uint Offset;

        public uint Count;

        public uint PathDepth { get; set; }

        public int TypeFlags { get; set; }

        public uint Index { get; set; }

        public EbxClass ArrayClass { get; set; }

        public uint Hash { get; set; }
    }
}
