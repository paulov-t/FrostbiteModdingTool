using FMT.FileTools;
using FrostySdk.Attributes;
using FrostySdk.FrostySdk.IO._2022.Readers;
using FrostySdk.IO;
using System;
using System.IO;
using static FrostySdk.IO.EbxXmlWriter;
using System.Linq;
using System.Reflection;
using FrostySdk.Managers;

namespace FrostySdk.FrostySdk.IO
{
    public class EbxWriterRiff : EbxWriter2023
    {
        public EbxWriterRiff(Stream inStream, EbxWriteFlags inFlags = EbxWriteFlags.None, bool leaveOpen = false)
           : base(inStream, inFlags, true)
        {
        }

        protected override void WriteClass(object obj, Type objType, NativeWriter writer)
        {
            var startOfDataContainer = writer.Position;
            //var dataContainerIndex = 0;
                if (obj == null)
                {
                    throw new ArgumentNullException("obj");
                }
                if (objType == null)
                {
                    throw new ArgumentNullException("objType");
                }
                if (writer == null)
                {
                    throw new ArgumentNullException("writer");
                }
                if (objType.BaseType.Namespace == "Sdk.Ebx")
                {
                    WriteClass(obj, objType.BaseType, writer);
                }
                PropertyInfo[] properties = objType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                Array.Sort(properties, new PropertyComparer());
                EbxClass classType = GetClass(objType);

            var classMeta = objType.GetCustomAttribute<EbxClassMetaAttribute>();

            foreach (var item in from index in Enumerable.Range(0, classType.FieldCount)
                                     select (index, GetField(classType, classType.FieldIndex + index)) into f
                                     orderby f.Item2.DataOffset
                                     select f)
                {
                    var (fieldIndex, field) = item;
                    if (field.DebugType == EbxFieldType.Inherited)
                    {
                        continue;
                    }
                    int propertyInfoIndex = Array.FindIndex(properties, (PropertyInfo p) => p.GetCustomAttribute<HashAttribute>()?.Hash == (int?)field.NameHash);
                    PropertyInfo propertyInfo = ((propertyInfoIndex == -1) ? null : properties[propertyInfoIndex]);
                    long currentOffset = writer.Position - startOfDataContainer;
                    if (currentOffset < 0)
                    {
                    }
                    else if (currentOffset > field.DataOffset)
                    {
                    }
                    else if (currentOffset < field.DataOffset)
                    {
                        int adjustment = (int)(field.DataOffset - currentOffset);
                        int adjustmentByPaddingTo8 = (int)(8 - currentOffset % 8);
                        if (adjustment != adjustmentByPaddingTo8)
                        {
                        }
                        writer.WriteEmpty(adjustment);
                    }
                    if (propertyInfo == null)
                    {
                        EbxClass unpatchedClassType = GetClassUnpatched(objType);
                        for (int i = 0; i < unpatchedClassType.FieldCount; i++)
                        {
                            EbxField unpatchedField = GetField(unpatchedClassType, unpatchedClassType.FieldIndex + i);
                            if (unpatchedField.DataOffset == field.DataOffset && unpatchedField.DebugType == field.DebugType)
                            {
                                propertyInfoIndex = Array.FindIndex(properties, (PropertyInfo p) => p.GetCustomAttribute<HashAttribute>()?.Hash == (int?)unpatchedField.NameHash);
                                propertyInfo = ((propertyInfoIndex == -1) ? null : properties[propertyInfoIndex]);
                                if (propertyInfoIndex == -1)
                                {
                                }
                                break;
                            }
                        }
                    }
                    if (propertyInfo == null)
                    {
                        switch (field.DebugType)
                        {
                            case EbxFieldType.TypeRef:
                                writer.WriteUInt64LittleEndian(0uL);
                                break;
                            case EbxFieldType.FileRef:
                                writer.WriteUInt64LittleEndian(0uL);
                                break;
                            case EbxFieldType.CString:
                                writer.WriteInt32LittleEndian(0);
                                break;
                            case EbxFieldType.Pointer:
                                writer.WriteInt32LittleEndian(0);
                                break;
                            case EbxFieldType.Struct:
                                {
                                    EbxClass value = GetClass(classType, field);
                                    writer.WritePadding(value.Alignment);
                                    writer.WriteEmpty(value.Size);
                                    break;
                                }
                            case EbxFieldType.Array:
                                writer.WriteInt32LittleEndian(0);
                                break;
                            case EbxFieldType.Enum:
                                writer.WriteInt32LittleEndian(0);
                                break;
                            case EbxFieldType.Float32:
                                writer.WriteSingleLittleEndian(0f);
                                break;
                            case EbxFieldType.Float64:
                                writer.WriteDoubleLittleEndian(0.0);
                                break;
                            case EbxFieldType.Boolean:
                                writer.Write((byte)0);
                                break;
                            case EbxFieldType.Int8:
                                writer.Write(0);
                                break;
                            case EbxFieldType.UInt8:
                                writer.Write((byte)0);
                                break;
                            case EbxFieldType.Int16:
                                writer.WriteInt16LittleEndian(0);
                                break;
                            case EbxFieldType.UInt16:
                                writer.WriteUInt16LittleEndian(0);
                                break;
                            case EbxFieldType.Int32:
                                writer.WriteInt32LittleEndian(0);
                                break;
                            case EbxFieldType.UInt32:
                                writer.WriteUInt32LittleEndian(0u);
                                break;
                            case EbxFieldType.Int64:
                                writer.WriteInt64LittleEndian(0L);
                                break;
                            case EbxFieldType.UInt64:
                                writer.WriteUInt64LittleEndian(0uL);
                                break;
                            case EbxFieldType.Guid:
                                writer.WriteGuid(Guid.Empty);
                                break;
                            case EbxFieldType.Sha1:
                                writer.Write(Sha1.Zero);
                                break;
                            case EbxFieldType.String:
                                writer.WriteFixedSizedString(string.Empty, 32);
                                break;
                            case EbxFieldType.ResourceRef:
                                writer.WriteUInt64LittleEndian(0uL);
                                break;
                            case EbxFieldType.BoxedValueRef:
                                writer.WriteGuid(Guid.Empty);
                                break;
                        }
                    }
                    else
                    {
                        bool isArray = GetField(classType, classType.FieldIndex + fieldIndex).TypeCategory == EbxTypeCategory.ArrayType;
                        bool isReference = propertyInfo.GetCustomAttribute<IsReferenceAttribute>() != null;
                        EbxFieldMetaAttribute ebxFieldMetaAttribute = propertyInfo.GetCustomAttribute<EbxFieldMetaAttribute>();
                        if (ebxFieldMetaAttribute == null || ebxFieldMetaAttribute.Type == EbxFieldType.Inherited)
                            continue;

                        if (isArray)
                        {
                            uint fieldNameHash = propertyInfo.GetCustomAttribute<HashAttribute>()!.Hash;
                            //WriteArray(propertyInfo.GetValue(obj), ebxFieldMetaAttribute.ArrayType, fieldNameHash, classMeta.Alignment, writer, isReference);
                            WriteArray(propertyInfo.GetValue(obj), ebxFieldMetaAttribute, fieldNameHash, classMeta.Alignment, writer, isReference);
                        }
                        else
                        {
                            WriteField(propertyInfo.GetValue(obj), ebxFieldMetaAttribute.Type, classMeta.Alignment, writer, isReference);
                        }
                    }
                }
                writer.WritePadding(classType.Alignment);

        }

        IEbxSharedTypeDescriptor std = null;

        internal EbxClass GetClassUnpatched(Type objType)
        {
            if (objType == null)
            {
                throw new ArgumentNullException("objType");
            }
            foreach (TypeInfoGuidAttribute typeInfoGuidAttribute in objType.GetCustomAttributes<TypeInfoGuidAttribute>())
            {
                if(std == null)
                    std = (IEbxSharedTypeDescriptor)AssetManager.LoadTypeByName(ProfileManager.EBXTypeDescriptor
                            , AssetManager.Instance.FileSystem, "SharedTypeDescriptors.ebx", false);

                EbxClass? ebxClass = std.GetClass(typeInfoGuidAttribute.Guid);
                if (ebxClass.HasValue)
                {
                    return ebxClass.Value;
                }
            }
            throw new ArgumentException("Class not found.");
        }
    }
}
