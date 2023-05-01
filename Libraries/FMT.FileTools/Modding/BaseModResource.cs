using FrostySdk;
using System.Collections.Generic;
using System.Linq;

//namespace FrostySdk
namespace FMT.FileTools.Modding
{
    public class BaseModResource
    {
        protected int resourceIndex = -1;

        protected string name;

        protected Sha1 sha1;

        protected long size;

        protected byte flags;

        protected int handlerHash;

        protected List<int> bundlesToModify = new List<int>();

        protected List<int> bundlesToAdd = new List<int>();

        public virtual ModResourceType Type => ModResourceType.Invalid;

        public int ResourceIndex => resourceIndex;

        public string Name => name;

        public Sha1 Sha1 => sha1;

        public long Size => size;

        public int Handler => handlerHash;

        public string UserData { get; protected set; }

        public bool IsModified => bundlesToModify.Count != 0;

        public bool IsAdded => bundlesToAdd.Count != 0;

        public bool ShouldInline => (flags & 1) != 0;

        public bool IsTocChunk => (flags & 2) != 0;

        public bool HasHandler => handlerHash != 0;

        public IEnumerable<int> ModifiedBundles => bundlesToModify;

        public IEnumerable<int> AddedBundles => bundlesToAdd;

        public virtual bool IsDDS
        {
            get
            {
                return false;
            }
        }

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
                //if(!string.IsNullOrEmpty(Extr))
                return null;
            }
        }

        /// <summary>
        /// Only relavant to FIFAMod
        /// </summary>
        public virtual string LegacyName
        {
            get
            {
                if (!string.IsNullOrEmpty(LegacyFullName))
                {
                    if (LegacyFullName.Contains("/"))
                    {
                        var lastIndex = LegacyFullName.LastIndexOf("/");
                        if (lastIndex != -1)
                        {
                            var result = LegacyFullName.Substring(lastIndex + 1, LegacyFullName.Length - lastIndex - 1);
                            if (result.Contains("."))
                            {
                                lastIndex = result.LastIndexOf(".");
                                if (lastIndex != -1)
                                {
                                    result = result.Substring(0, lastIndex);
                                }
                            }
                            return result;
                        }
                    }
                }
                return null;
            }
        }

        public virtual string LegacyPath
        {
            get
            {
                if (!string.IsNullOrEmpty(LegacyFullName))
                {
                    if (LegacyFullName.Contains("/"))
                    {
                        var lastIndex = LegacyFullName.LastIndexOf("/");
                        if (lastIndex != -1)
                        {
                            return LegacyFullName.Substring(0, lastIndex) + "/";
                        }
                    }
                }
                return null;
            }
        }

        public string ReadString(NativeReader reader, uint modVersion)
        {
            // should handle FrostyEditor stuff? right?
            if(modVersion < 6)
            {
                return reader.ReadLengthPrefixedString();
            }

            if (modVersion < 27 || modVersion == IFrostbiteMod.CurrentVersion || modVersion == IFrostbiteMod.HashVersions[7] || modVersion == IFrostbiteMod.HashVersions_Pre2323[7])
            {
                return reader.ReadNullTerminatedString();
            }
            return reader.ReadLengthPrefixedString();
        }

        public virtual void Read(NativeReader reader, uint modVersion = 6u)
        {
            resourceIndex = reader.ReadInt();
            if (resourceIndex != -1)
            {
                name = ReadString(reader, modVersion);
                sha1 = reader.ReadSha1();
                size = reader.ReadLong();
                flags = reader.ReadByte();
                handlerHash = reader.ReadInt();
                UserData = ReadString(reader, modVersion);
                int num = reader.ReadInt();
                for (int i = 0; i < num; i++)
                {
                    bundlesToModify.Add(reader.ReadInt());
                }
                num = reader.ReadInt();
                for (int j = 0; j < num; j++)
                {
                    bundlesToAdd.Add(reader.ReadInt());
                }
            }
        }

        public virtual void FillAssetEntry(IAssetEntry entry)
        {
            entry.Name = name;
            entry.Sha1 = sha1;
            entry.OriginalSize = size;
            //entry.IsInline = ShouldInline;
            //       if (!string.IsNullOrEmpty(userData))
            //       {
            //           if (entry.ModifiedEntry == null) 
            //entry.ModifiedEntry = new ModifiedAssetEntry();

            //           entry.ModifiedEntry.UserData = userData;
            //       }
            entry.ExtraInformation = UserData;

            if (AddedBundles != null && AddedBundles.Any())
                entry.Bundles.AddRange(AddedBundles);
            if (ModifiedBundles != null && ModifiedBundles.Any())
                entry.Bundles.AddRange(ModifiedBundles);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(name))
                return name;

            return base.ToString();
        }
    }
}
