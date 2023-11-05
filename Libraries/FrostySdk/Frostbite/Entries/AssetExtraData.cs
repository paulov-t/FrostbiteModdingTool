namespace FrostySdk.Managers
{
    public class AssetExtraData
    {
        //public Sha1 BaseSha1 { get; set; }

        //public Sha1 DeltaSha1 { get; set; }

        public uint DataOffset { get; set; }

        //public int SuperBundleId { get; set; }

        public bool IsPatch { get; set; }

        private string casPath;

        public string CasPath
        {
            get
            {
                if (!string.IsNullOrEmpty(casPath))
                    return casPath;

                if (Catalog.HasValue && Cas.HasValue)
                {
                    return FileSystem.Instance.GetFilePath(Catalog.Value, Cas.Value, IsPatch);
                }

                //            if (CasIndex.HasValue)
                //            {
                //	return FileSystem.Instance.GetFilePath(CasIndex.Value);
                //}

                return string.Empty;
            }
            set
            {
                casPath = value;
            }
        }

        public ushort? Catalog { get; set; }
        public ushort? Cas { get; set; }

        public int? CasIndex { get; set; }

        public byte Unk { get; set; }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(AssetExtraData))
            {
                var other = (AssetExtraData)obj;
                if (Cas.HasValue && Catalog.HasValue && DataOffset != 0)
                {
                    if(other.Cas.HasValue && other.Catalog.HasValue && other.DataOffset != 0)
                    { 
                        if(other.Cas == this.Cas
                            && other.Catalog == this.Catalog 
                            && other.IsPatch == this.IsPatch
                            && other.DataOffset == this.DataOffset)
                        {
                            return true;
                        }
                    }
                }
            }

            return base.Equals(obj);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(CasPath))
                return CasPath;

            return base.ToString();
        }
    }
}
