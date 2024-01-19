using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using FrostySdk;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace SdkGenerator.FC24
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

            var typeInfoNamePosition = reader.Position;
            
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

            if (name.Equals("RaceVehicleBlueprint", System.StringComparison.OrdinalIgnoreCase))
            {

            }

            if (name.Equals("ShaderDrawOrder", System.StringComparison.OrdinalIgnoreCase)) 
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
            while(reader.Position % 8 != 0)
            {
                reader.Position++;
            }



            array = new long[32];
            for (int i = 0; i < 7; i++)
            //for (int i = 0; i < 32; i++)
            {
                array[i] = reader.ReadLong();
            }
            reader.Position = namespaceNamePosition;
            nameSpace = reader.ReadNullTerminatedString();
            bool hasFields = fieldCount > 0;

            if(FieldType != EbxFieldType.Enum)
                parentClass = array[0];
            else
                parentClass = 0L;

            if (name.Equals("LinearTransform", System.StringComparison.OrdinalIgnoreCase))
            {

            }

            if (name.Equals("ShaderDrawOrder", System.StringComparison.OrdinalIgnoreCase))
            {

            }

            

            if (hasFields)
            {
                var fieldsPosition = 0L;
                if (Type == 2)
                {
                    fieldsPosition = array[6];
                }
                else if (Type == 3)
                {
                    fieldsPosition = array[1];
                }
                else if (Type == 8 || FieldType == EbxFieldType.Enum)
                {
                    parentClass = 0L;
                    //fieldsPosition = array[0];
                    fieldsPosition = array[1];
                }
                else
                {
                    fieldsPosition = array[5];
                }

                if (FieldType == EbxFieldType.Enum)
                {
                    // TODO when full release
                    //Console.WriteLine($"{name} is an Enum, reading char*");
                    //reader.ReadNullTerminatedStringAtCurPosition();
                    //throw new Exception("TODO");
                }

                var resultOfFieldRead = ReadFieldsAtPosition(fieldsPosition);
                if (!resultOfFieldRead)
                {
                    Debug.WriteLine($"Unable to read Initially read Fields at position {fieldsPosition} for {FieldType}:{this.name}");
                    for (var i = 0; i < array.Length; i++)
                    {
                        if (array[i] == 0L)
                            continue;

                        if (ReadFieldsAtPosition(array[i]))
                            break;
                    }
                }

            }

            bool ReadFieldsAtPosition(long position)
            {
                if (position == 0L)
                    return true;

                var tmpFields = new IFieldInfo[fieldCount];

                reader.Position = position;

                bool success = true;
                for (int j = 0; j < fieldCount; j++)
                {
                    FieldInfo fieldInfo = new FieldInfo(this);

                    fieldInfo.Read(reader);
                    fieldInfo.index = j;
                    if (fieldInfo.name.Contains("UnkField"))
                        fieldInfo.name = this.name + "_" + j;
                    if (!fieldInfo.ReadSuccessfully)
                    {
                        Debug.WriteLine($"Unable to read {FieldType}:{this.name}:{j}");
                        success = false;
                    }

                    if (FieldType == EbxFieldType.Function)
                    {
                        reader.Position += 8;
                        while (reader.Position % 8 != 0)
                            reader.Position++;
                    }

                    tmpFields[j] = fieldInfo;

                }


                fields = tmpFields.ToList();
                return success;
            }
        }


        public void Modify(DbObject classObj)
        {
            classObj.SetValue("nameHash", nameHash);
        }
    }
}
