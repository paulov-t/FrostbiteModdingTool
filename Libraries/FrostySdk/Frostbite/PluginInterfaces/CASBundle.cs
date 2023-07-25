using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public class CASBundle// : IDisposable
    {
        public int unk1 { get; set; }
        public int unk2 { get; set; }
        public int HeaderSize { get; set; }
        public int unk4 { get; set; }
        public int unk5 { get; set; }

        // ---------------------------------------
        // Cas Identifiers

        public byte Unk { get; set; }
        public byte Catalog { get; set; }
        public byte Cas { get; set; }
        public bool Patch { get; set; }

        //
        // ---------------------------------------


        public int FlagsOffset { get; set; }

        public uint BundleOffset { get; set; }
        public uint BundleSize { get; set; }

        public List<uint> Sizes { get; private set; } = new List<uint>();

        public List<uint> Offsets { get; private set; } = new List<uint>();

        public List<CASBundleEntry> Entries { get; private set; } = new List<CASBundleEntry>();

        public long TotalSize
        {
            get
            {
                if (Sizes.Count == 0)
                {
                    return 0;
                }

                return Sizes.Sum(x => (int)x) + BundleSize;
            }
        }

        public int EntriesCount { get; set; }
        public int EntriesOffset { get; set; }
        public BundleEntry BaseEntry { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<long> TOCOffsets { get; private set; } = new List<long>();
        /// <summary>
        /// 
        /// </summary>
        public List<long> TOCSizes { get; private set; } = new List<long>();
        /// <summary>
        /// 
        /// </summary>
        public List<int> TOCCas { get; private set; } = new List<int>();
        /// <summary>
        /// 
        /// </summary>
        public List<int> TOCCatalog { get; private set; } = new List<int>();
        /// <summary>
        /// 
        /// </summary>
        public List<bool> TOCPatch { get; private set; } = new List<bool>();
        /// <summary>
        /// 
        /// </summary>
        public byte[] Flags;

        //public List<bool> Flags = new List<bool>();

        public override string ToString()
        {
            return $"Cas:{Cas}-Catalog:{Catalog}-BundleOffset:{BundleOffset}-Size:{TotalSize}";
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!disposedValue)
        //    {
        //        if (disposing)
        //        {
        //            // TODO: dispose managed state (managed objects)

        //        }

        //        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        //        // TODO: set large fields to null
        //        TOCOffsets.Clear();
        //        TOCOffsets = null;
        //        TOCSizes.Clear();
        //        TOCSizes = null;
        //        TOCCatalog.Clear();
        //        TOCCatalog = null;
        //        TOCPatch.Clear();
        //        TOCPatch = null;

        //        disposedValue = true;
        //    }
        //}

        //// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        //// ~CASBundle()
        //// {
        ////     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        ////     Dispose(disposing: false);
        //// }

        //public void Dispose()
        //{
        //    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //    Dispose(disposing: true);
        //    GC.SuppressFinalize(this);
        //}
    }

    public class CASBundleEntry
    {
        public int unk { get; set; }
        public bool isInPatch { get; set; }
        public int catalog { get; set; }
        public int cas { get; set; }
        public uint bundleSizeInCas { get; set; }
        public long locationOfSize { get; set; }
        public uint bundleOffsetInCas { get; set; }
        public long locationOfOffset { get; set; }
    }
}
