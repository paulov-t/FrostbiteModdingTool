using FMT.FileTools;
using System;

namespace FrostySdk.Managers
{
    public sealed class EbxAssetEntry : AssetEntry, IAssetEntry
    {
        public EbxAssetEntry(ModifiedAssetEntry modifiedAssetEntry = null) : base(modifiedAssetEntry)
        {
        }

        public override string AssetType => EAssetType.ebx.ToString();

        public bool IsBinary { get; internal set; }

        public override bool IsModified
        {
            get
            {
                if (!IsDirectlyModified)
                {
                    if (IsIndirectlyModified)
                        return true;

                    var resEntry = AssetManager.Instance.GetResEntry(Name);
                    if(resEntry != null)
                    {
                        return resEntry.IsModified;
                    }
                }
                return IsDirectlyModified;
            }
        }

        public override AssetEntry Clone()
        {
            return this.MemberwiseClone() as EbxAssetEntry;
        }
    }
}
