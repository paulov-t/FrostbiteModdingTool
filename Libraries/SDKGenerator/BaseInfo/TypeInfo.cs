using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using FrostySdk;
using System;
using System.Collections.Generic;

namespace SdkGenerator.BaseInfo
{
    public class TypeInfo : ISdkGenInfo, ITypeInfo
    {
        //public string name;

        //public ushort flags;

        //public uint size;

        //public Guid guid;

        //public ushort padding1;

        //public string nameSpace;

        //public ushort alignment;

        //public uint fieldCount;

        //public uint padding3;

        //public long parentClass;

        //public List<FieldInfo> fields = new List<FieldInfo>();

        public int Type => (flags >> 4) & 0x1F;

        public uint nameHash { get; set; }
        public string name { get; set; }
        public ushort flags { get; set; }
        public uint size { get; set; }
        public Guid guid { get; set; }
        public ushort padding1 { get; set; }
        public string nameSpace { get; set; }
        public ushort alignment { get; set; }
        public uint fieldCount { get; set; }
        public uint padding3 { get; set; }
        public long parentClass { get; set; }
        public List<IFieldInfo> fields { get; set; } = new List<IFieldInfo>();

        public virtual void Read(MemoryReader reader)
        {
            bool flag = ProfileManager.DataVersion == 20131115 || ProfileManager.DataVersion == 20141118 || ProfileManager.DataVersion == 20140225;
            name = reader.ReadNullTerminatedString();
            if (!string.IsNullOrEmpty(name) && name.Length > 0)
            {
                flags = reader.ReadUShort();
                if (ProfileManager.DataVersion == 20170929 || ProfileManager.DataVersion == 20171117 || ProfileManager.DataVersion == 20171110 || ProfileManager.DataVersion == 20180807 || ProfileManager.DataVersion == 20180914 || ProfileManager.IsMadden20DataVersion() || ProfileManager.DataVersion == 20180628)
                {
                    flags >>= 1;
                }
                size = reader.ReadUInt();
                if (ProfileManager.DataVersion == 20180914 || ProfileManager.IsMadden20DataVersion())
                {
                    reader.Position -= 4L;
                    size = reader.ReadUShort();
                    guid = reader.ReadGuid();
                    reader.ReadUShort();
                }
                padding1 = reader.ReadUShort();
                long position = reader.ReadLong();
                if (ProfileManager.DataVersion == 20170321 || ProfileManager.DataVersion == 20160927 || ProfileManager.DataVersion == 20170929 || ProfileManager.DataVersion == 20171110 || ProfileManager.DataVersion == 20180807 || ProfileManager.DataVersion == 20180914 || ProfileManager.DataVersion == 20171117 || ProfileManager.IsMadden20DataVersion() || ProfileManager.DataVersion == 20180628)
                {
                    reader.ReadLong();
                }
                alignment = (flag ? reader.ReadByte() : reader.ReadUShort());
                fieldCount = (flag ? reader.ReadByte() : reader.ReadUShort());
                if (flag)
                {
                    padding3 = reader.ReadUShort();
                }
                padding3 = reader.ReadUInt();
                long[] array = new long[7];
                for (int i = 0; i < 7; i++)
                {
                    array[i] = reader.ReadLong();
                }
                reader.Position = position;
                nameSpace = reader.ReadNullTerminatedString();
                bool flag2 = false;
                if (ProfileManager.DataVersion == 20170929
                    || ProfileManager.DataVersion == 20171117
                    || ProfileManager.DataVersion == 20171110
                    || ProfileManager.DataVersion == 20180807
                    || ProfileManager.DataVersion == 20180628
                    || ProfileManager.IsFIFA19DataVersion()
                    )
                {
                    parentClass = array[0];
                    if (Type == 2 && !ProfileManager.IsFIFA19DataVersion())
                    {
                        reader.Position = array[5];
                        flag2 = true;
                    }
                    else if (Type == 3)
                    {
                        reader.Position = array[1];
                        flag2 = true;
                    }
                    else if (Type == 8 && !ProfileManager.IsFIFA19DataVersion())
                    {
                        reader.Position = array[0];
                        flag2 = true;
                        parentClass = 0L;
                    }
                }
                else if (Type == 2)
                {
                    reader.Position = array[1];
                    flag2 = true;
                }
                else if (Type == 3)
                {
                    reader.Position = array[2];
                    flag2 = true;
                }
                else if (Type == 8)
                {
                    reader.Position = array[0];
                    flag2 = true;
                    parentClass = 0L;
                }
                else if (Type == 4)
                {
                    parentClass = array[0];
                }
                if (flag2)
                {
                    for (int j = 0; j < fieldCount; j++)
                    {
                        FieldInfo fieldInfo2 = new FieldInfo();
                        fieldInfo2.Read(reader);
                        fieldInfo2.index = j;
                        if (fieldInfo2.name == "")
                        {

                        }
                        fields.Add(fieldInfo2);
                    }
                }
            }
        }

        public virtual void Modify(DbObject classObj)
        {
        }
    }
}
