using FMT.FileTools;
using FrostySdk.FrostbiteSdk.Managers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Fnv1a = FMT.FileTools.Fnv1a;

namespace FrostySdk.Managers
{
    //[Serializable]
    public abstract class AssetEntry : IAssetEntry, INotifyPropertyChanged, IDisposable
    {
        public AssetEntry(ModifiedAssetEntry modifiedAssetEntry = null)
        {
            ModifiedEntry = modifiedAssetEntry;
        }

        public void Dispose()
        {
            Dispose(true);
            // any other managed resource cleanups you can do here
            GC.SuppressFinalize(this);
        }

        ~AssetEntry()      // finalizer
        {
            Dispose(false);
        }

        bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }

                _disposed = true;
            }
        }

        public CompressionType OriginalCompressionType { get; set; } = CompressionType.Default;

        public Guid Id { get; set; }

        public virtual FMT.FileTools.Sha1 Sha1 { get; set; }

        public virtual FMT.FileTools.Sha1 BaseSha1 { get; set; }

        private long _size;

        public long Size
        {
            get
            {

                return _size;
            }
            set
            {
                _size = value;
            }
        }

        public long OriginalSize { get; set; }

        public bool IsInline { get; set; }

        public AssetDataLocation Location { get; set; }

        public AssetExtraData ExtraData { get; set; }

        private List<int> listBundles = new List<int>();

        public List<int> Bundles
        {
            get { return listBundles; }
            set { listBundles = value; }
        }

        //public IEnumerable<string> BundleNames
        //{
        //	get
        //	{
        //		List<string> bNames = new List<string>();
        //		foreach (var i in Bundles)
        //		{
        //			bNames.Add(AssetManager.Instance.bundles[i].Name);
        //		}
        //		return bNames;
        //	}
        //}


        public List<int> AddBundles { get; } = new List<int>();

        public List<int> RemBundles { get; } = new List<int>();

        //private IModifiedAssetEntry modifiedAsset;
        //public virtual IModifiedAssetEntry ModifiedEntry { get; set; }

        private IModifiedAssetEntry modifiedAsset;

        public IModifiedAssetEntry ModifiedEntry
        {
            get
            {
                return modifiedAsset;
            }
            set
            {
                modifiedAsset = value;
            }
        }


        private List<AssetEntry> linkedAssets = new List<AssetEntry>();

        public List<AssetEntry> LinkedAssets
        {
            get { return linkedAssets; }
            set { linkedAssets = value; }
        }

        private bool dirty;

        /// <summary>
        /// Name is the Full Path
        /// </summary>
        public virtual string Name
        {
            get;
            set;
        }

        public virtual string DuplicatedFromName
        {
            get;
            set;
        }

        public string FullPath
        {
            get
            {
                return Name;
            }
        }

        public virtual string Type
        {
            get;
            set;
        }

        public virtual string AssetType
        {
            get;
        }

        public virtual string DisplayName => Filename + (IsDirty ? "*" : "");


        /// <summary>
        /// The Actual FileName?
        /// </summary>
        public virtual string Filename
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    int num = Name.LastIndexOf('/');
                    if (num == -1)
                    {
                        return Name;
                    }
                    return Name.Substring(num + 1);
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// The Virtual Path to the File
        /// </summary>
        public virtual string Path
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    int num = Name.LastIndexOf('/');
                    if (num == -1)
                    {
                        return "";
                    }
                    return Name.Substring(0, num);
                }
                return string.Empty;
            }
        }

        public bool IsAdded
        {
            get;
            set;
        }

        public virtual bool IsModified
        {
            get
            {
                if (!IsDirectlyModified)
                {
                    return IsIndirectlyModified;
                }
                return true;
            }
        }

        public bool IsDirectlyModified
        {
            get
            {
                if (ModifiedEntry == null && AddBundles.Count == 0)
                {
                    return RemBundles.Count != 0;
                }
                return true;
            }
        }

        public virtual bool HasModifiedData
        {
            get
            {
                if (ModifiedEntry != null)
                {
                    if (ModifiedEntry.Data == null)
                    {
                        return ModifiedEntry.DataObject != null;
                    }
                    return ModifiedEntry.Data != null;
                }
                return false;
            }
        }

        public bool IsIndirectlyModified
        {
            get
            {
                foreach (AssetEntry linkedAsset in LinkedAssets)
                {
                    if (linkedAsset.IsModified)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public virtual bool IsDirty
        {
            get
            {
                if (dirty)
                {
                    return true;
                }
                foreach (AssetEntry linkedAsset in LinkedAssets)
                {
                    if (linkedAsset.IsDirty)
                    {
                        return true;
                    }
                }
                return false;
            }
            set
            {
                if (dirty != value)
                {
                    dirty = value;
                    //if (dirty)
                    //{
                    //    OnModified();
                    //}
                }
            }
        }

        // ---- -----------------------------------------------------------------
        // v2k4 helpers for different approach to mod data generation

        private string _casfilelocation;
        public string CASFileLocation
        {
            get
            {
                if (string.IsNullOrEmpty(_casfilelocation) && ExtraData != null)
                    _casfilelocation = ExtraData.CasPath;

                return _casfilelocation;
            }
            set { _casfilelocation = value; }
        }
        public bool IsInPatch
        {
            get
            {
                if (ExtraData != null)
                    return ExtraData.IsPatch;

                return false;
            }
        }

        //public string Bundle { get; set; }

        private string sbFileLocation;
        public string SBFileLocation
        {
            get
            {
                if (string.IsNullOrEmpty(sbFileLocation) && SBFileLocations != null && SBFileLocations.Count > 0)
                    sbFileLocation = SBFileLocations.Last();

                return sbFileLocation;
            }
            set { sbFileLocation = value; }
        }
        public HashSet<string> SBFileLocations => new HashSet<string>();


        private string tocFileLocation;

        public string TOCFileLocation
        {
            get
            {
                if (string.IsNullOrEmpty(tocFileLocation) && TOCFileLocations != null && TOCFileLocations.Count > 0)
                    tocFileLocation = TOCFileLocations.Last();

                return tocFileLocation;
            }
            set { tocFileLocation = value; }
        }

        public HashSet<string> TOCFileLocations { get; } = new HashSet<string>();

        //[Obsolete]
        public int SB_CAS_Offset_Position { get; set; }
        //[Obsolete]
        public int SB_CAS_Size_Position { get; set; }
        //[Obsolete]
        public int SB_Sha1_Position { get; set; }
        //[Obsolete]
        public int SB_OriginalSize_Position { get; set; }

        public string ExtraInformation { get; set; }

        //public int ParentBundleOffset { get; set; }
        //public int ParentBundleSize { get; set; }


        public bool IsLegacy = false;
        //private bool disposedValue;


        // ---- -----------------------------------------------------------------
        // ---- -----------------------------------------------------------------

        //public event EventHandler AssetModified;
        public event PropertyChangedEventHandler PropertyChanged;

        public void LinkAsset(AssetEntry assetToLink)
        {
            if (!LinkedAssets.Contains(assetToLink))
            {
                LinkedAssets.Add(assetToLink);
            }
            if (assetToLink is ChunkAssetEntry)
            {
                if (assetToLink.HasModifiedData)
                {
                    (assetToLink as ChunkAssetEntry).ModifiedEntry.H32 = Fnv1a.HashString(Name);
                }
                else
                {
                    (assetToLink as ChunkAssetEntry).H32 = Fnv1a.HashString(Name);
                }
            }
        }

        public void AddToBundle(int bid)
        {
            AddBundles.Add(bid);
            IsDirty = true;
        }

        //public bool AddToBundles(IEnumerable<int> bundles)
        //{
        //	bool result = false;
        //	foreach (int bundle in bundles)
        //	{
        //		if (!Bundles.Contains(bundle) && !AddBundles.Contains(bundle))
        //		{
        //                  Bundles.Add(bundle);
        //			AddBundles.Add(bundle);
        //                  IsDirty = true;
        //			result = true;
        //		}
        //	}
        //	return result;
        //}

        public IEnumerable<int> EnumerateBundles(bool addedOnly = false)
        {
            if (!addedOnly)
            {
                for (int j = 0; j < Bundles.Count; j++)
                {
                    if (!RemBundles.Contains(Bundles[j]))
                    {
                        yield return Bundles[j];
                    }
                }
            }
            for (int j = 0; j < AddBundles.Count; j++)
            {
                yield return AddBundles[j];
            }
        }

        public virtual void ClearModifications()
        {
            if(ModifiedEntry != null && !ProfileManager.Instance.CanUseModData)
            {
                if(ModifiedEntry.OriginalData != null)
                {
                    AssetManager.Instance.ModifyEntry(this, ModifiedEntry.OriginalData);
                    return;
                }
            }

            ModifiedEntry = null;
        }

        //public void OnModified()
        //{
        //    this.AssetModified?.Invoke(this, new EventArgs());
        //}

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var other = obj as AssetEntry;

            if (obj is ChunkAssetEntry chunkAssetEntry && this is ChunkAssetEntry)
            {
                if (chunkAssetEntry.FullPath == this.FullPath
                    && !string.IsNullOrEmpty(chunkAssetEntry.Type) && !string.IsNullOrEmpty(this.Type) && chunkAssetEntry.Type == this.Type
                    && chunkAssetEntry.IsTocChunk == ((ChunkAssetEntry)this).IsTocChunk
                    )
                    return true;
            }

            else if (this is ResAssetEntry && obj is ResAssetEntry resAssetEntry)
            {
                if (resAssetEntry.ToString() == this.ToString())
                    return true;
            }

            else if (this.GetType() == obj.GetType())
            {
                var tocsEqual = true;
                if (!string.IsNullOrEmpty(other.TOCFileLocation) && !string.IsNullOrEmpty(this.TOCFileLocation))
                {
                    tocsEqual = other.TOCFileLocation == this.TOCFileLocation;
                }

                if (tocsEqual
                    && other.ExtraData != null && this.ExtraData != null && other.ExtraData.Equals(this.ExtraData)
                    && other.Sha1 == this.Sha1
                    && other.ToString() == this.ToString()
                        )
                    return true;
                else
                    return false;

                //return
                //    other.Sha1 == this.Sha1 && other.ToString() == this.ToString()
                //    &&
                //    (
                //        (other.ExtraData == null || this.ExtraData == null)
                //        || (
                //            other.ExtraData != null && this.ExtraData != null
                //            && other.ExtraData.Equals(this.ExtraData)
                //            )
                //    );

                //if (other.FullPath == this.FullPath 
                //    && !string.IsNullOrEmpty(other.Type) && !string.IsNullOrEmpty(this.Type) && other.Type == this.Type)
                //    return true;

                //if (other.FullPath == this.FullPath && other.Sha1 == this.Sha1)
                //    return true;
            }

            //if (obj is IAssetEntry iOther)
            //{
            //    if (iOther.Sha1 == this.Sha1)
            //        return true;

            //    if (iOther.Name == this.Name)
            //        return true;
            //}

            return base.Equals(obj);
        }

      
        public override int GetHashCode()
        {
            return BitConverter.ToInt32(Sha1.ToByteArray());
        }

        public virtual AssetEntry Clone()
        {
            return this.MemberwiseClone() as AssetEntry;
        }

        public virtual T Clone<T>(bool deep = true)
        {
            //return (T)this.MemberwiseClone();
            if(deep)
                return (T)this.MemberwiseClone();
            else
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings() { MaxDepth = 0 }));

        }

        public bool IsOfType(string type, bool acceptSubType = true)
        {
            if (!Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                if (acceptSubType)
                {
                    return TypeLibrary.IsSubClassOf(Type, type);
                }
                return false;
            }
            return true;
        }

        public bool IsOfType(bool acceptSubType, params string[] types)
        {
            foreach (string type in types)
            {
                if (IsOfType(type, acceptSubType))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsOfType(params string[] types)
        {
            return IsOfType(acceptSubType: true, types);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return $"[{GetType().Name}]({Name})";
            }
            return base.ToString();
        }

        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!disposedValue)
        //    {
        //        if (disposing)
        //        {
        //            // TODO: dispose managed state (managed objects)
        //            ModifiedEntry = null;

        //            TOCFileLocations.Clear();
        //            SBFileLocations.Clear();

        //        }

        //        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        //        // TODO: set large fields to null
        //        TOCFileLocation = null;
        //        SBFileLocation = null;
        //        disposedValue = true;
        //    }
        //}

        //// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        //// ~AssetEntry()
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

    public enum EAssetType
    {
        ebx,
        res,
        chunk
    }
}
