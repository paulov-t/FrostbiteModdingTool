using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using FrostySdk;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SdkGenerator.NFSUnbound
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

        private EbxFieldType FieldType => (EbxFieldType)Type;

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

            if(name.Equals("RaceVehicleBlueprint", System.StringComparison.OrdinalIgnoreCase))
            {

            }

            nameHash = reader.ReadUInt();
            flags = reader.ReadUShort();
            flags >>= 1;
            //size = reader.ReadUShort();
            size = reader.ReadUInt();
            reader.Position -= 4L;
            size = reader.ReadUShort();

            guid = reader.ReadGuid();
            if (!Regex.IsMatch(guid.ToString(), @"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$"))
            {
                throw new System.FormatException("Guid is not valid");
            }

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
                if (array[1] > 5000000000)
                    reader.Position = array[1];
                //else if (array[3] > 5000000000) // Caused hang
                //    reader.Position = array[3];
                //else if (array[0] > 5000000000) // Caused hang
                //    reader.Position = array[0];
                else if (array[6] > 5000000000)
                    reader.Position = array[6];
                else if (array[3] > 5000000000)
                    reader.Position = array[3];
                else
                    hasFields = false;
            }
            else if (Type == 8)
            {
                if (fieldCount > 0)
                {
                    parentClass = 0L;
                    if (array[0] > 5000000000)
                        reader.Position = array[0];
                    //else if (array[2] > 5000000000)  // Caused hang
                    //    reader.Position = array[2];
                    else if (array[4] > 5000000000)
                        reader.Position = array[4];
                    else
                        hasFields = false;
                }
            }
            else
            {
                //if (fieldCount > 0)
                //{
                //    reader.Position = array[5];
                //    hasFields = true;
                //}

                if (array[5] > 5000000000)
                    reader.Position = array[5];
                else if (array[3] > 5000000000)
                    reader.Position = array[3];
                else if (array[2] > 5000000000)
                    reader.Position = array[3];
                else
                    hasFields = false;
            }

            if (hasFields && reader.Position > 5000000000)
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


                foreach (var f in fields)
                {
                }
            }
        }


        public void Modify(DbObject classObj)
        {
            classObj.SetValue("nameHash", nameHash);
        }
    }
}
