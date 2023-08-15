using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using FrostySdk;
using System;

namespace SdkGenerator.FC24
{
    public class FieldInfo : IFieldInfo
    {
        public uint nameHash { get; set; }

        public static Random RandomEmpty = new Random();

        public bool ReadSuccessfully = false;

        public string name { get; set; }

        public ushort flags { get; set; }

        public uint offset { get; set; }

        public ushort padding1 { get; set; }

        public long typeOffset { get; set; }

        public int index { get; set; }


        private ITypeInfo parentTypeInfo { get; }
        public FieldInfo(ITypeInfo parentType)
        {
            parentTypeInfo = parentType;

        }

        public void Read(MemoryReader reader)
        {
            ReadSuccessfully = true;
            int maxLength = 999;
            name = reader.ReadNullTerminatedString(maxLength);
            if (string.IsNullOrEmpty(name) || name.Length >= maxLength - 1)
            {
                name = parentTypeInfo.name + "_UnkField_" + RandomEmpty.Next().ToString();
                ReadSuccessfully = false;
            }
            nameHash = reader.ReadUInt();
            flags = reader.ReadUShort();
            offset = reader.ReadUShort();
            typeOffset = reader.ReadLong();
        }


        //public override void Read(MemoryReader reader)
        //{
        //	name = reader.ReadNullTerminatedString();
        //	nameHash = reader.ReadUInt();
        //	flags = reader.ReadUShort();
        //	offset = reader.ReadUShort();
        //	typeOffset = reader.ReadLong();
        //}

        public void Modify(DbObject fieldObj)
        {
            fieldObj.SetValue("nameHash", nameHash);
        }

        public override string ToString()
        {
            return name;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
