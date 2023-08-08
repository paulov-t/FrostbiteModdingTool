using FrostbiteSdk;
using FrostySdk;

namespace SdkGenerator.FIFA18
{
    public class FieldInfo : BaseInfo.FieldInfo
    {
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
    }
}
