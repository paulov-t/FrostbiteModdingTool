using FrostySdk.FrostbiteSdk.Managers;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace FrostySdk.Managers
{
    //public class ModifiedAssetEntry : AssetEntry
    public class ModifiedAssetEntry : AssetEntry, IModifiedAssetEntry
    {

        public ModifiedAssetEntry()
        {

        }

        public ModifiedAssetEntry(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; set; }

        /// <summary>
        /// If not using ModData folder, this is a save of the original data from the file
        /// </summary>
        public byte[] OriginalData { get; set; }


        public long? NewOffset
        {
            get; set;
        }

        private object dataObject;

        public virtual object DataObject
        {
            get
            {
                if (dataObject == null && Data != null && Data.Length > 0
                    &&
                    (
                        ResMeta == null
                        || (ResMeta != null && ResMeta.Length == 0)
                    )
                    &&
                    LogicalSize == 0
                    )
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(ProfileManager.LoadedProfile.EBXReader))
                        {
                            using (var ebxReader = (EbxReader)AssetManager.LoadTypeByName(
                                ProfileManager.LoadedProfile.EBXReader,
                                new MemoryStream(Data), false))
                                dataObject = ebxReader.ReadAsset();
                        }
                    }
                    catch
                    {

                    }
                }

                return dataObject;
            }
            set { dataObject = value; }
        }


        public long? CompressedOffset { get; set; }

        public long? CompressedOffsetEnd { get; set; }

        public byte[] ResMeta { get; set; }

        public uint LogicalOffset { get; set; }

        public uint LogicalSize { get; set; }

        public uint RangeStart { get; set; }

        public uint RangeEnd { get; set; }

        private int firstMip { get; set; } = -1;

        public int FirstMip
        {
            get { return firstMip; }
            set { firstMip = value; }
        }


        //public bool AddToChunkBundle { get; set; } = false;

        //public bool AddToTOCChunks { get; set; } = false;

        public bool IsTransientModified { get; set; }

        public int H32 { get; set; }

        public List<Guid> DependentAssets { get; set; } = new List<Guid>();

        public string UserData { get; set; } = "";

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

        /// <summary>
        /// Only relavant to FIFAMod
        /// </summary>
        public virtual string LegacyFullName
        {
            get
            {
                if (!string.IsNullOrEmpty(UserData))
                {
                    if (UserData.Contains(";"))
                    {
                        return UserData.Split(";")[1];
                    }
                }
                if (!string.IsNullOrEmpty(ExtraInformation))
                {
                    if (ExtraInformation.Contains(";"))
                    {
                        return ExtraInformation.Split(";")[1];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        //public byte[] CompressedDataOodle
        //      {
        //          get
        //          {
        //		if(Data != null)
        //              {
        //			return Utils.CompressFile(Data, null, ResourceType.Invalid, CompressionType.Oodle);

        //		}
        //		else
        //              {
        //			return null;
        //              }

        //          }
        //      }

        //public byte[] CompressedDataZstd
        //{
        //	get
        //	{
        //		if (Data != null)
        //		{
        //			return Utils.CompressFile(Data, null, ResourceType.Invalid, CompressionType.ZStd);

        //		}
        //		else
        //		{
        //			return null;
        //		}

        //	}
        //}

        //public byte[] CompressedDataNone
        //{
        //	get
        //	{
        //		if (Data != null)
        //		{
        //			return Utils.CompressFile(Data, null, ResourceType.Invalid, CompressionType.None);

        //		}
        //		else
        //		{
        //			return null;
        //		}

        //	}
        //}

        public Guid? ChunkId { get; set; }
    }
}
