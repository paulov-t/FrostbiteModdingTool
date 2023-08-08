using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using FrostySdk;
using System;
using System.Collections.Generic;

namespace SdkGenerator.Madden20
{
    public class TypeInfo : BaseInfo.TypeInfo, ITypeInfo
    {
        string ITypeInfo.name { get; set; }
        ushort ITypeInfo.flags { get; set; }
        uint ITypeInfo.size { get; set; }
        Guid ITypeInfo.guid { get; set; }
        ushort ITypeInfo.padding1 { get; set; }
        string ITypeInfo.nameSpace { get; set; }
        ushort ITypeInfo.alignment { get; set; }
        uint ITypeInfo.fieldCount { get; set; }
        uint ITypeInfo.padding3 { get; set; }
        long ITypeInfo.parentClass { get; set; }
        List<IFieldInfo> ITypeInfo.fields { get; set; }

        public override void Read(MemoryReader reader)
        {
            name = reader.ReadNullTerminatedString();
            nameHash = reader.ReadUInt();
            flags = reader.ReadUShort();
            flags >>= 1;
            size = reader.ReadUInt();
            reader.Position -= 4L;
            size = reader.ReadUShort();
            guid = reader.ReadGuid();
            long position = reader.ReadLong();
            reader.ReadLong();
            alignment = reader.ReadUShort();
            fieldCount = reader.ReadUShort();
            padding3 = reader.ReadUInt();
            long[] array = new long[7];
            for (int i = 0; i < 7; i++)
            {
                array[i] = reader.ReadLong();
            }
            reader.Position = position;
            nameSpace = reader.ReadNullTerminatedString();
            bool flag = false;
            parentClass = array[0];
            if (base.Type == 2)
            {
                reader.Position = array[6];
                flag = true;
            }
            else if (base.Type == 3)
            {
                reader.Position = array[1];
                flag = true;
            }
            else if (base.Type == 8)
            {
                parentClass = 0L;
                reader.Position = array[0];
                reader.Position = array[0];
                flag = true;
            }
            if (flag)
            {
                for (int j = 0; j < fieldCount; j++)
                {
                    FieldInfo fieldInfo = new FieldInfo();
                    fieldInfo.Read(reader);
                    fieldInfo.index = j;
                    fields.Add(fieldInfo);
                }
            }
        }

        public override void Modify(DbObject classObj)
        {
            classObj.SetValue("nameHash", nameHash);
        }
    }
}
