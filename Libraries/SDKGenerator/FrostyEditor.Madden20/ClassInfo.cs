using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using System;

namespace SdkGenerator.Madden20
{
    public class ClassInfo : BaseInfo.ClassInfo, IClassInfo
    {
        ITypeInfo IClassInfo.typeInfo { get; set; }
        ushort IClassInfo.id { get; set; }
        ushort IClassInfo.isDataContainer { get; set; }
        byte[] IClassInfo.padding { get; set; }
        long IClassInfo.parentClass { get; set; }

        public override void Read(MemoryReader reader)
        {
            long position = reader.Position;
            long position2 = reader.ReadLong();
            ClassesSdkCreator.offset = reader.ReadLong();
            _ = Guid.Empty;
            id = reader.ReadUShort();
            isDataContainer = reader.ReadUShort();
            padding = new byte[4]
            {
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte()
            };
            parentClass = reader.ReadLong();
            reader.Position = position2;
            typeInfo = new TypeInfo();
            typeInfo.Read(reader);
            if (typeInfo.parentClass != 0L)
            {
                parentClass = typeInfo.parentClass;
            }
            if (parentClass == position)
            {
                parentClass = 0L;
            }
        }
    }
}
