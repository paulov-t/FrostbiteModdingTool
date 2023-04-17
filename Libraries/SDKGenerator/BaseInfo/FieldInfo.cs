using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using FrostySdk;

namespace SdkGenerator.BaseInfo
{
    public class FieldInfo : ISdkGenInfo, IFieldInfo
    {
        public uint nameHash { get; set; }
        public string name { get; set; }
        public ushort flags { get; set; }
        public uint offset { get; set; }
        public ushort padding1 { get; set; }
        public long typeOffset { get; set; }
        public int index { get; set; }

        //public string name;

        //public ushort flags;

        //public uint offset;

        //public ushort padding1;

        //public long typeOffset;

        //public int index;

        public virtual void Read(MemoryReader reader)
        {
            name = reader.ReadNullTerminatedString();
            flags = reader.ReadUShort();
            offset = reader.ReadUInt();
            padding1 = reader.ReadUShort();
            typeOffset = reader.ReadLong();
        }

        public virtual void Modify(DbObject fieldObj)
        {
        }
    }
}
