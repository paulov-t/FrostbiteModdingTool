using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using FrostySdk;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SdkGenerator.StarWarsSquadronsPlugin
{
    public class TypeInfo : ITypeInfo
    {
        public uint nameHash { get; set; }

        public long[] array;

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


        public int Type => (flags >> 4) & 0x1F;

        public List<IFieldInfo> fields { get; set; }

        public void Read(MemoryReader reader)
        {
            fields = new List<IFieldInfo>();
            name = reader.ReadNullTerminatedString();
            if (name.Equals("TextureAsset", System.StringComparison.OrdinalIgnoreCase))
            {

            }

            if (name.Equals("RenderFormat", System.StringComparison.OrdinalIgnoreCase))
            {

            }

            if (name.Equals("AttribSchema_gp_rules_foul_playercontactscore", System.StringComparison.OrdinalIgnoreCase))
            {

            }

            if (name.Equals("LinearTransform", System.StringComparison.OrdinalIgnoreCase))
            {

            }
            flags = reader.ReadUShort();
            flags >>= 1;
            size = reader.ReadUInt();
            //size = reader.ReadUShort();
            //size = reader.ReadUInt();
            //reader.Position -= 4L;
            //size = reader.ReadUShort();

            //guid = reader.ReadGuid();
            //if (!Regex.IsMatch(guid.ToString(), @"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$"))
            //{
            //    throw new System.FormatException("Guid is not valid");
            //}

            //padding3 = reader.ReadUInt();
            fieldCount = reader.ReadUShort();

            // Module 
            long namespaceNamePosition = reader.ReadLong();

            // Unknown ?
            long nextTypeInfo = reader.ReadLong();

            // 
            alignment = reader.ReadUShort();
            fieldCount = reader.ReadUShort();

            // Padding
            padding3 = reader.ReadUInt();

            array = new long[7];
            for (int i = 0; i < 7; i++)
            {
                array[i] = reader.ReadLong();
            }
            reader.Position = namespaceNamePosition;
            nameSpace = reader.ReadNullTerminatedString();
            bool hasFields = fieldCount > 0;

            reader.Position = nextTypeInfo;

            parentClass = array[0];

            if (name.Equals("LinearTransform", System.StringComparison.OrdinalIgnoreCase))
            {

            }

            if (Type == 2)
            {
                reader.Position = array[6];
            }
            else if (Type == 3)
            {
                reader.Position = array[1];
            }
            else if (Type == 8)
            {
                if (fieldCount > 0)
                {
                    parentClass = 0L;
                    reader.Position = array[0];
                }
            }
            else
            {
                if (fieldCount > 0)
                {
                    reader.Position = array[5];
                    hasFields = true;
                }
            }

            if (hasFields)
            {
                for (int j = 0; j < fieldCount; j++)
                {
                    FieldInfo fieldInfo = new FieldInfo(this);
                    fieldInfo.Read(reader);
                    fieldInfo.index = j;
                    if (fieldInfo.ReadSuccessfully)
                    {
                        fields.Add(fieldInfo);
                    }


                }
            }
        }


        public void Modify(DbObject classObj)
        {
            classObj.SetValue("nameHash", nameHash);
        }
    }
}
