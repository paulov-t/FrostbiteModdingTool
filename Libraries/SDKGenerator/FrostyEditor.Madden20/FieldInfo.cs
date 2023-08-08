using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using FrostySdk;

namespace SdkGenerator.Madden20
{
    public class FieldInfo : BaseInfo.FieldInfo, IFieldInfo
    {
        string IFieldInfo.name { get; set; }
        ushort IFieldInfo.flags { get; set; }
        uint IFieldInfo.offset { get; set; }
        ushort IFieldInfo.padding1 { get; set; }
        long IFieldInfo.typeOffset { get; set; }
        int IFieldInfo.index { get; set; }

        public override void Read(MemoryReader reader)
        {
            name = reader.ReadNullTerminatedString();
            nameHash = reader.ReadUInt();
            flags = reader.ReadUShort();
            offset = reader.ReadUShort();
            typeOffset = reader.ReadLong();
        }

        public override void Modify(DbObject fieldObj)
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
