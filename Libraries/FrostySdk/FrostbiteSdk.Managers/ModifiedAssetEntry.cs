using System;
using System.Collections.Generic;

namespace FrostySdk.Managers
{
	//public class ModifiedAssetEntry : AssetEntry
	public class ModifiedAssetEntry : AssetEntry
	{
		public byte[] Data { get; set; }

		public long? NewOffset
        {
			get;set;
        }

		public object DataObject { get; set; }

		public byte[] ResMeta { get; set; }

		public uint LogicalOffset { get; set; }

		public uint LogicalSize { get; set; }

		public uint RangeStart { get; set; }

		public uint RangeEnd { get; set; }

        private int firstMip = -1;

        public int FirstMip
        {
            get { return firstMip; }
            set { firstMip = value; }
        }


		public bool AddToChunkBundle = true;

		public bool IsTransientModified { get; set; }

		public int H32;

		public List<Guid> DependentAssets = new List<Guid>();

		public string UserData = "";

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
				return null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public byte[] CompressedData
        {
            get
            {
				if(Data != null)
                {
					return Utils.CompressFile(Data, null, ResourceType.Invalid, CompressionType.Oodle);

				}
				else
                {
					return null;
                }

            }
        }

		public byte[] CompressedDataZstd
		{
			get
			{
				if (Data != null)
				{
					return Utils.CompressFile(Data, null, ResourceType.Invalid, CompressionType.ZStd);

				}
				else
				{
					return null;
				}

			}
		}
	}
}
