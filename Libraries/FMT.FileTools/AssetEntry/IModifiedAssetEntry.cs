using FMT.FileTools;
using System;
using System.Collections.Generic;

namespace FrostySdk.FrostbiteSdk.Managers
{
    public interface IModifiedAssetEntry : IAssetEntry
    {
        public Sha1 BaseSha1 { get; set; }

        public long Size { get; set; }

        public byte[] Data { get; set; }

        public object DataObject { get; set; }

        public long? NewOffset
        {
            get; set;
        }

        public long? CompressedOffset { get; set; }

        public long? CompressedOffsetEnd { get; set; }

        public byte[] ResMeta { get; set; }

        public uint LogicalOffset { get; set; }

        public uint LogicalSize { get; set; }

        public uint RangeStart { get; set; }

        public uint RangeEnd { get; set; }

        public int FirstMip { get; set; }


        //public bool AddToChunkBundle { get; set; }

        //public bool AddToTOCChunks { get; set; }

        public bool IsTransientModified { get; set; }

        public int H32 { get; set; }

        public List<Guid> DependentAssets { get; set; }

        public string UserData { get; set; }

        /// <summary>
        /// Only related to *.fifamod
        /// </summary>
        public virtual bool IsLegacyFile
        {
            get
            {
                return LegacyFullName != null;
            }
        }
        public string LegacyFullName { get; }

        public Guid? ChunkId { get; set; }
    }
}
