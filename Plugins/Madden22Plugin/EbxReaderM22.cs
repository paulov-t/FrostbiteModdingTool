using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Madden22Plugin
{

    public class EbxReaderM22 : EbxReader
    {
        public static IEbxSharedTypeDescriptor std;

        public static IEbxSharedTypeDescriptor patchStd;

        public List<Guid> classGuids = new List<Guid>();

        public bool patched;

        public Type EBXType = null;

        //public override string RootType => classGuids.Count > 0 && instances.Count > 0 
        //					? TypeLibrary.GetType(classGuids[instances[0].ClassRef])?.Name 
        //					: string.Empty;
        public override string RootType => EBXType != null ? EBXType.Name : string.Empty;

        public string fileName { get; set; }

        public EbxReaderM22(Stream InStream, bool inPatched)
            : base(InStream, passthru: true)
        {
            InStream.Position = 0;

            patched = inPatched;
            // RIFF
            var ebxVersion = (EbxVersion)ReadUInt();

            var fileLengthMinus4 = ReadUInt();

            var ebxHeaderName = ReadNullTerminatedString();
            if (!string.IsNullOrEmpty(ebxHeaderName) && ebxHeaderName != "EBX")
                return;

            var ebxHeaderEndBit = ReadUInt();

            // 
            var afterStringsOffsetMinus4 = ReadUInt();
            InStream.Position += 12;
            fileGuid = ReadGuid();
            var stdClassFile = std.GetClass(fileGuid);
            InStream.Position += 16;
            guidCount = ReadUInt();
            var unkh1 = ReadByte();
            var unkh2 = ReadByte();
            var unkh3 = ReadByte();
            var unkh4 = ReadByte();

            var fileItemStringOffsetFromThisPosition = ReadLong() + (InStream.Position - 8L);
            var unkh7 = ReadUInt();
            var unkh8 = ReadUInt();
            var unkh9 = ReadULong();
            var unkh10 = ReadULong();
            var unkh11 = ReadULong();
            var unkh12 = ReadULong();

            InStream.Position = fileItemStringOffsetFromThisPosition;
            fileName = ReadNullTerminatedString();
            if (string.IsNullOrEmpty(fileName))
                return;

            //var type_attempt_1 = TypeLibrary.GetType(fileGuid);

            if (!string.IsNullOrEmpty(fileName) && fileName.Equals("content/characters/player/bodies/body_0_normal", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists("ebxV4.dat")) File.Delete("ebxV4.dat");
                var sPositionBeforeDump = InStream.Position;
                InStream.Position = 0;
                using (var fsDump = new FileStream("ebxV4.dat", FileMode.OpenOrCreate))
                {
                    InStream.CopyTo(fsDump);
                }
                InStream.Position = sPositionBeforeDump;
            }

            //InStream.Position = afterStringsOffsetMinus4 + 4 + headerLength;
            while (InStream.Position % 2 != 0) InStream.Position++;
            // Read "EFIX"
            InStream.Position += 4;
            // Read string position
            //var stringPos = ReadUInt();
            //InStream.Position = stringPos + 32;
            //var name = ReadNullTerminatedString();
            var offset = ReadUInt();
            for (var i = 0; i < guidCount; i++)
            {
                var guid = ReadGuid();
                var stdClass = std.GetClass(guid);
                if (stdClass != null)
                {
                    EBXType = TypeLibrary.GetType(stdClass.Value.NameHash);
                }
                var count = ReadUInt();
                EbxImportReference ebxImportReference = new EbxImportReference
                {
                    FileGuid = guid,
                    ClassGuid = guid
                };
                imports.Add(ebxImportReference);
                if (!dependencies.Contains(ebxImportReference.FileGuid))
                {
                    dependencies.Add(ebxImportReference.FileGuid);
                }

                var type_attempt = TypeLibrary.GetType(guid);
                if (type_attempt != null)
                {

                }

                classGuids.Add(guid);
            }
            var unk1 = ReadUInt();
            var t = TypeLibrary.GetType(unk1);
            ReadUInt();
            ReadUInt();
            ReadUInt();
            ReadUInt();
            ReadUInt();
            ReadUInt();
            ReadUInt();
            Guid item1 = ReadGuid();
            classGuids.Add(item1);
            Guid item2 = ReadGuid();
            classGuids.Add(item2);

            var stdClassItem1 = std.GetClass(item1);
            var stdClassItem2 = std.GetClass(item2);


            var t2 = TypeLibrary.GetType(item1);
            var t3 = TypeLibrary.GetType(item2);
            if (t2 != null)
            {

            }
            if (t3 != null)
            {

            }

            /*

			stringsOffset = ReadUInt();
			stringsAndDataLen = ReadUInt();
			guidCount = ReadUInt();
			instanceCount = ReadUShort();
			exportedCount = ReadUShort();
			uniqueClassCount = ReadUShort();
			classTypeCount = ReadUShort();
			fieldTypeCount = ReadUShort();
			typeNamesLen = ReadUShort();
			stringsLen = ReadUInt();
			arrayCount = ReadUInt();
			dataLen = ReadUInt();
			arraysOffset = stringsOffset + stringsLen + dataLen;
			//fileGuid = ReadGuid();
			boxedValuesCount = ReadUInt();
			boxedValuesOffset = ReadUInt();
			boxedValuesOffset += stringsOffset + stringsLen;
			for (int num = 0; num < guidCount; num++)
			{
				EbxImportReference ebxImportReference = new EbxImportReference
				{
					FileGuid = ReadGuid(),
					ClassGuid = ReadGuid()
				};
				imports.Add(ebxImportReference);
				if (!dependencies.Contains(ebxImportReference.FileGuid))
				{
					dependencies.Add(ebxImportReference.FileGuid);
				}
			}
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			long position = base.Position;
			while (base.Position - position < typeNamesLen)
			{
				string text = ReadNullTerminatedString();
				int key = HashString(text);
				dictionary.TryAdd(key, text);
			}
			for (int i = 0; i < fieldTypeCount; i++)
			{
				EbxField item = default(EbxField);
				int key2 = ReadInt32LittleEndian();
				item.Type = ((magic == EbxVersion.Version2) ? ReadUShort() : ((ushort)(ReadUShort() >> 1)));
				item.ClassRef = ReadUShort();
				item.DataOffset = ReadUInt();
				item.SecondOffset = ReadUInt();
				item.Name = dictionary[key2];
				fieldTypes.Add(item);
			}
			for (int j = 0; j < classTypeCount; j++)
			{
				Guid item2 = ReadGuid();
				classGuids.Add(item2);
			}
			ushort num2 = exportedCount;
			for (int k = 0; k < instanceCount; k++)
			{
				EbxInstance item3 = new EbxInstance
				{
					ClassRef = ReadUShort(),
					Count = ReadUShort()
				};
				if (num2 != 0)
				{
					item3.IsExported = true;
					num2 = (ushort)(num2 - 1);
				}
				instances.Add(item3);
			}
			while (base.Position % 16 != 0L)
			{
				base.Position++;
			}
			for (int num3 = 0; num3 < arrayCount; num3++)
			{
				EbxArray item4 = new EbxArray
				{
					Offset = ReadUInt(),
					Count = ReadUInt(),
					ClassRef = ReadInt32LittleEndian()
				};
				arrays.Add(item4);
			}
			Pad(16);
			for (int l = 0; l < boxedValuesCount; l++)
			{
				EbxBoxedValue item5 = new EbxBoxedValue
				{
					Offset = ReadUInt(),
					ClassRef = ReadUShort(),
					Type = ReadUShort()
				};
				boxedValues.Add(item5);
			}
			base.Position = stringsOffset + stringsLen;
			isValid = true;
			*/
        }

        public override void InternalReadObjects()
        {
            foreach (EbxInstance ebxInstance in instances)
            {
                Type type = TypeLibrary.GetType(classGuids[ebxInstance.ClassRef]);
                for (int i = 0; i < ebxInstance.Count; i++)
                {
                    objects.Add(TypeLibrary.CreateObject(type));
                    refCounts.Add(0);
                }
            }
            int num = 0;
            int num2 = 0;
            foreach (EbxInstance ebxInstance2 in instances)
            {
                for (int j = 0; j < ebxInstance2.Count; j++)
                {
                    dynamic obj = objects[num++];
                    Type objType = obj.GetType();
                    EbxClass @class = GetClass(objType);
                    while (base.Position % @class.Alignment != 0L)
                    {
                        base.Position++;
                    }
                    Guid inGuid = Guid.Empty;
                    if (ebxInstance2.IsExported)
                    {
                        inGuid = ReadGuid();
                    }
                    if (@class.Alignment != 4)
                    {
                        base.Position += 8L;
                    }
                    obj.SetInstanceGuid(new AssetClassGuid(inGuid, num2++));
                    this.ReadClass(@class, obj, base.Position - 8);
                }
            }
        }

        public EbxClass GetClass(Type objType)
        {
            EbxClass? ebxClass = null;
            foreach (TypeInfoGuidAttribute typeInfoGuidAttribute in objType.GetCustomAttributes(typeof(TypeInfoGuidAttribute), inherit: true).Cast<TypeInfoGuidAttribute>())
            {
                if (classGuids.Contains(typeInfoGuidAttribute.Guid))
                {
                    if (patched && patchStd != null)
                    {
                        ebxClass = patchStd.GetClass(typeInfoGuidAttribute.Guid);
                    }
                    if (!ebxClass.HasValue)
                    {
                        ebxClass = std.GetClass(typeInfoGuidAttribute.Guid);
                    }
                    break;
                }
            }
            return ebxClass.Value;
        }

        public override PropertyInfo GetProperty(Type objType, EbxField field)
        {
            if (field.NameHash == 3109710567u)
            {
                return null;
            }
            PropertyInfo[] properties = objType.GetProperties();
            foreach (PropertyInfo propertyInfo in properties)
            {
                HashAttribute customAttribute = propertyInfo.GetCustomAttribute<HashAttribute>();
                if (customAttribute != null && customAttribute.Hash == (int)field.NameHash)
                {
                    return propertyInfo;
                }
            }
            return null;
        }

        public override EbxClass GetClass(EbxClass? classType, int index)
        {
            EbxClass? ebxClass = null;
            Guid? guid = null;
            int index2 = (short)index + (classType.HasValue ? classType.Value.Index : 0);
            guid = std.GetGuid(index2);
            if (classType.HasValue && classType.Value.SecondSize == 1)
            {
                guid = patchStd.GetGuid(index2);
                ebxClass = patchStd.GetClass(index2);
                if (!ebxClass.HasValue)
                {
                    ebxClass = std.GetClass(guid.Value);
                }
            }
            else
            {
                ebxClass = std.GetClass(index2);
            }
            if (ebxClass.HasValue)
            {
                TypeLibrary.AddType(ebxClass.Value.Name, guid);
            }
            return ebxClass.Value;
        }

        public override EbxField GetField(EbxClass classType, int index)
        {
            if (classType.SecondSize == 1)
            {
                return patchStd.GetField(index).Value;
            }
            return std.GetField(index).Value;
        }

        public override object CreateObject(EbxClass classType)
        {
            if (classType.SecondSize == 1)
            {
                return TypeLibrary.CreateObject(patchStd.GetGuid(classType).Value);
            }
            return TypeLibrary.CreateObject(std.GetGuid(classType).Value);
        }

        public object ReadClass(EbxClassMetaAttribute classMeta, object obj, Type objType, long startOffset)
        {
            if (obj == null)
            {
                base.Position += classMeta.Size;
                while (base.Position % classMeta.Alignment != 0L)
                {
                    base.Position++;
                }
                return null;
            }
            if (objType.BaseType != typeof(object))
            {
                ReadClass(objType.BaseType.GetCustomAttribute<EbxClassMetaAttribute>(), obj, objType.BaseType, startOffset);
            }
            PropertyInfo[] properties = objType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.GetCustomAttribute<IsTransientAttribute>() != null)
                {
                    continue;
                }
                IsReferenceAttribute customAttribute = propertyInfo.GetCustomAttribute<IsReferenceAttribute>();
                EbxFieldMetaAttribute customAttribute2 = propertyInfo.GetCustomAttribute<EbxFieldMetaAttribute>();
                base.Position = startOffset + customAttribute2.Offset;
                if (customAttribute2.Type == EbxFieldType.Array)
                {
                    int index = ReadInt32LittleEndian();
                    EbxArray ebxArray = arrays[index];
                    long position2 = base.Position;
                    base.Position = arraysOffset + ebxArray.Offset;
                    propertyInfo?.GetValue(obj).GetType().GetMethod("Clear")
                        .Invoke(propertyInfo.GetValue(obj), new object[0]);
                    for (int i = 0; i < ebxArray.Count; i++)
                    {
                        object obj2 = ReadField(customAttribute2.ArrayType, customAttribute2.BaseType, customAttribute != null);
                        propertyInfo?.GetValue(obj).GetType().GetMethod("Add")
                            .Invoke(propertyInfo.GetValue(obj), new object[1] { obj2 });
                    }
                    base.Position = position2;
                }
                else
                {
                    object value = ReadField(customAttribute2.Type, propertyInfo.PropertyType, customAttribute != null);
                    propertyInfo?.SetValue(obj, value);
                }
            }
            while (base.Position - startOffset != classMeta.Size)
            {
                base.Position++;
            }
            return null;
        }

        public object ReadField(EbxFieldType type, Type baseType, bool dontRefCount = false)
        {
            switch (type)
            {
                case EbxFieldType.DbObject:
                    throw new InvalidDataException("DbObject");
                case EbxFieldType.Struct:
                    {
                        object obj = TypeLibrary.CreateObject(baseType);
                        EbxClassMetaAttribute customAttribute = obj.GetType().GetCustomAttribute<EbxClassMetaAttribute>();
                        while (base.Position % customAttribute.Alignment != 0L)
                        {
                            base.Position++;
                        }
                        ReadClass(customAttribute, obj, obj.GetType(), base.Position);
                        return obj;
                    }
                case EbxFieldType.Pointer:
                    {
                        uint num = ReadUInt();
                        if (num >> 31 == 1)
                        {
                            EbxImportReference ebxImportReference = imports[(int)(num & 0x7FFFFFFF)];
                            if (dontRefCount && !dependencies.Contains(ebxImportReference.FileGuid))
                            {
                                dependencies.Add(ebxImportReference.FileGuid);
                            }
                            return new PointerRef(ebxImportReference);
                        }
                        if (num == 0)
                        {
                            return default(PointerRef);
                        }
                        if (!dontRefCount)
                        {
                            refCounts[(int)(num - 1)]++;
                        }
                        return new PointerRef(objects[(int)(num - 1)]);
                    }
                case EbxFieldType.String:
                    return ReadSizedString(32);
                case EbxFieldType.CString:
                    return ReadCString(ReadUInt());
                case EbxFieldType.Enum:
                    return ReadInt32LittleEndian();
                case EbxFieldType.FileRef:
                    return ReadFileRef();
                case EbxFieldType.Boolean:
                    return ReadByte() > 0;
                case EbxFieldType.Int8:
                    return (sbyte)ReadByte();
                case EbxFieldType.UInt8:
                    return ReadByte();
                case EbxFieldType.Int16:
                    return ReadInt16LittleEndian();
                case EbxFieldType.UInt16:
                    return ReadUShort();
                case EbxFieldType.Int32:
                    return ReadInt32LittleEndian();
                case EbxFieldType.UInt32:
                    return ReadUInt();
                case EbxFieldType.UInt64:
                    return ReadUInt64LittleEndian();
                case EbxFieldType.Int64:
                    return ReadInt64LittleEndian();
                case EbxFieldType.Float32:
                    return ReadSingleLittleEndian();
                case EbxFieldType.Float64:
                    return ReadDoubleLittleEndian();
                case EbxFieldType.Guid:
                    return ReadGuid();
                case EbxFieldType.Sha1:
                    return new FMT.FileTools.Sha1(ReadBytes(20));
                case EbxFieldType.ResourceRef:
                    return ReadResourceRef();
                case EbxFieldType.TypeRef:
                    return ReadTypeRef();
                case EbxFieldType.BoxedValueRef:
                    return ReadBoxedValueRef();
                default:
                    throw new InvalidDataException("Unknown");
            }
        }
    }

}
