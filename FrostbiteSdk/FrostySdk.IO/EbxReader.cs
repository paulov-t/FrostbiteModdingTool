using FrostySdk.Attributes;
using FrostySdk.Ebx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace FrostySdk.IO
{
	public class EbxReader : NativeReader
	{
		internal List<EbxField> fieldTypes = new List<EbxField>();

		private List<EbxClass> classTypes = new List<EbxClass>();

		internal List<EbxInstance> instances = new List<EbxInstance>();

		internal List<EbxArray> arrays = new List<EbxArray>();

		internal List<EbxImportReference> imports = new List<EbxImportReference>();

		internal List<Guid> dependencies = new List<Guid>();

		internal List<object> objects = new List<object>();

		internal List<int> refCounts = new List<int>();

		protected List<BoxedValueRef> boxedValueRefs = new List<BoxedValueRef>();

		internal Guid fileGuid;

		internal long arraysOffset;

		internal long stringsOffset;

		internal long stringsAndDataLen;

		internal uint guidCount;

		internal ushort instanceCount;

		internal ushort exportedCount;

		internal ushort uniqueClassCount;

		internal ushort classTypeCount;

		internal ushort fieldTypeCount;

		internal ushort typeNamesLen;

		internal uint stringsLen;

		internal uint arrayCount;

		internal uint dataLen;

		internal uint boxedValuesCount;

		internal long boxedValuesOffset;

		internal EbxVersion magic;

		internal bool isValid;

		public Guid FileGuid => fileGuid;

		public virtual string RootType => classTypes[instances[0].ClassRef].Name;

		public List<Guid> Dependencies => dependencies;

		public bool IsValid => isValid;

		public List<EbxField> FieldTypes => fieldTypes;

		public List<EbxClass> ClassTypes => classTypes;

		internal EbxReader(Stream inStream, bool passthru)
			: base(inStream)
		{
		}

		public EbxReader(Stream InStream)
			: base(InStream)
		{
			magic = (EbxVersion)ReadUInt();
			if (magic != EbxVersion.Version2 && magic != EbxVersion.Version4)
			{
				return;
			}
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
			fileGuid = ReadGuid();
			if (magic == EbxVersion.Version4)
			{
				boxedValuesCount = ReadUInt();
				boxedValuesOffset = ReadUInt();
				boxedValuesOffset += stringsOffset + stringsLen;
			}
			else
			{
				while (Position % 16 != 0L)
				{
					Position++;
				}
			}
			for (int i = 0; i < guidCount; i++)
			{
				EbxImportReference item = new EbxImportReference
				{
					FileGuid = ReadGuid(),
					ClassGuid = ReadGuid()
				};
				imports.Add(item);
				if (!dependencies.Contains(item.FileGuid))
				{
					dependencies.Add(item.FileGuid);
				}
			}
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			long position = Position;
			while (Position - position < typeNamesLen)
			{
				string text = ReadNullTerminatedString();
				int key = HashString(text);
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, text);
				}
			}
			for (int j = 0; j < fieldTypeCount; j++)
			{
				EbxField item2 = default(EbxField);
				int key2 = ReadInt();
				item2.Type = ((magic == EbxVersion.Version2) ? ReadUShort() : ((ushort)(ReadUShort() >> 1)));
				item2.ClassRef = ReadUShort();
				item2.DataOffset = ReadUInt();
				item2.SecondOffset = ReadUInt();
				item2.Name = dictionary[key2];
				fieldTypes.Add(item2);
			}
			for (int k = 0; k < classTypeCount; k++)
			{
				EbxClass item3 = default(EbxClass);
				int key3 = ReadInt();
				item3.FieldIndex = ReadInt();
				item3.FieldCount = ReadByte();
				item3.Alignment = ReadByte();
				item3.Type = ((magic == EbxVersion.Version2) ? ReadUShort() : ((ushort)(ReadUShort() >> 1)));
				item3.Size = ReadUShort();
				item3.SecondSize = ReadUShort();
				item3.Name = dictionary[key3];
				classTypes.Add(item3);
			}
			ushort num = exportedCount;
			for (int l = 0; l < instanceCount; l++)
			{
				EbxInstance item4 = new EbxInstance
				{
					ClassRef = ReadUShort(),
					Count = ReadUShort()
				};
				if (num != 0)
				{
					item4.IsExported = true;
					num = (ushort)(num - 1);
				}
				instances.Add(item4);
			}
			while (Position % 16 != 0L)
			{
				Position++;
			}
			for (int m = 0; m < arrayCount; m++)
			{
				EbxArray item5 = new EbxArray
				{
					Offset = ReadUInt(),
					Count = ReadUInt(),
					ClassRef = ReadInt()
				};
				arrays.Add(item5);
			}
			Position = stringsOffset + stringsLen;
			isValid = true;
		}

		public EbxAsset ReadAsset()
		{
			EbxAsset ebxAsset = new EbxAsset();
			InternalReadObjects();
			ebxAsset.fileGuid = fileGuid;
			ebxAsset.objects = objects;
			ebxAsset.dependencies = dependencies;
			ebxAsset.refCounts = refCounts;
			return ebxAsset;
		}

		public dynamic ReadObject()
		{
			InternalReadObjects();
			return objects[0];
		}

		public List<object> ReadObjects()
		{
			InternalReadObjects();
			return objects;
		}

		public List<object> GetUnreferencedObjects()
		{
			List<object> list = new List<object>();
			list.Add(objects[0]);
			for (int i = 1; i < objects.Count; i++)
			{
				if (refCounts[i] == 0)
				{
					list.Add(objects[i]);
				}
			}
			return list;
		}

		internal virtual void InternalReadObjects()
		{
			new List<Type>();
			foreach (EbxInstance instance in instances)
			{
				EbxClass @class = GetClass(null, instance.ClassRef);
				for (int i = 0; i < instance.Count; i++)
				{
					Type inType = ParseClass(@class);
					objects.Add(TypeLibrary.CreateObject(inType));
					refCounts.Add(0);
				}
			}
			int num = 0;
			int num2 = 0;
			foreach (EbxInstance instance2 in instances)
			{
				EbxClass class2 = GetClass(null, instance2.ClassRef);
				for (int j = 0; j < instance2.Count; j++)
				{
					while (Position % (long)class2.Alignment != 0L)
					{
						Position++;
					}
					Guid inGuid = Guid.Empty;
					if (instance2.IsExported)
					{
						inGuid = ReadGuid();
					}
					if (class2.Alignment != 4)
					{
						Position += 8L;
					}
					dynamic val = objects[num++];
					val.SetInstanceGuid(new AssetClassGuid(inGuid, num2++));
					this.ReadClass(class2, val, Position - 8);
				}
			}
			while (boxedValuesOffset % 16 != 0L)
			{
				boxedValuesOffset++;
			}
			foreach (BoxedValueRef boxedValueRef in boxedValueRefs)
			{
				if ((int)boxedValueRef != -1)
				{
					Position = boxedValuesOffset + 8 * (int)boxedValueRef;
					boxedValueRef.SetData(ReadBytes(8));
				}
			}
		}

		internal object ReadClass(EbxClass classType, object obj, long startOffset)
		{
			if (obj == null)
			{
				Position += classType.Size;
				while (Position % (long)classType.Alignment != 0L)
				{
					Position++;
				}
				return null;
			}
			Type type = obj.GetType();
			for (int i = 0; i < classType.FieldCount; i++)
			{
				EbxField field = GetField(classType, classType.FieldIndex + i);
				PropertyInfo property = GetProperty(type, field);
				IsReferenceAttribute isReferenceAttribute = (property != null) ? property.GetCustomAttribute<IsReferenceAttribute>() : null;
				if (field.DebugType == EbxFieldType.Inherited)
				{
					ReadClass(GetClass(classType, field.ClassRef), obj, startOffset);
					continue;
				}
				if (field.DebugType == EbxFieldType.ResourceRef || field.DebugType == EbxFieldType.TypeRef || field.DebugType == EbxFieldType.FileRef || field.DebugType == EbxFieldType.BoxedValueRef || field.DebugType == EbxFieldType.UInt64 || field.DebugType == EbxFieldType.Int64 || field.DebugType == EbxFieldType.Float64)
				{
					while (Position % 8 != 0L)
					{
						Position++;
					}
				}
				else if (field.DebugType == EbxFieldType.Array || field.DebugType == EbxFieldType.Pointer)
				{
					while (Position % 4 != 0L)
					{
						Position++;
					}
				}
				if (field.DebugType == EbxFieldType.Array)
				{
					EbxClass @class = GetClass(classType, field.ClassRef);
					int index = ReadInt();
					EbxArray ebxArray = arrays[index];
					long position = Position;
					Position = arraysOffset + ebxArray.Offset;
					for (int j = 0; j < ebxArray.Count; j++)
					{
						object obj2 = ReadField(@class, GetField(@class, @class.FieldIndex), isReferenceAttribute != null);
						if (property != null)
						{
							try
							{
								property.GetValue(obj).GetType().GetMethod("Add")
									.Invoke(property.GetValue(obj), new object[1]
									{
										obj2
									});
							}
							catch (Exception)
							{
							}
						}
					}
					if (Position > boxedValuesOffset)
					{
						boxedValuesOffset = Position;
					}
					Position = position;
				}
				else
				{
					object value = ReadField(classType, field, isReferenceAttribute != null);
					if (property != null)
					{
						try
						{
							property.SetValue(obj, value);
						}
						catch (Exception)
						{
						}
					}
				}
			}
			while (Position % (long)classType.Alignment != 0L)
			{
				Position++;
			}
			return null;
		}

		internal virtual PropertyInfo GetProperty(Type objType, EbxField field)
		{
			return objType.GetProperty(field.Name);
		}

		internal virtual EbxClass GetClass(EbxClass? parentClass, int index)
		{
			return classTypes[index];
		}

		internal virtual EbxField GetField(EbxClass classType, int index)
		{
			return fieldTypes[index];
		}

		internal virtual object CreateObject(EbxClass classType)
		{
			return TypeLibrary.CreateObject(classType.Name);
		}

		internal object ReadField(EbxClass parentClass, EbxField fieldType, bool dontRefCount = false)
		{
			switch (fieldType.DebugType)
			{
			case EbxFieldType.Boolean:
				return (ReadByte() > 0) ? true : false;
			case EbxFieldType.Int8:
				return (sbyte)ReadByte();
			case EbxFieldType.UInt8:
				return ReadByte();
			case EbxFieldType.Int16:
				return ReadShort();
			case EbxFieldType.UInt16:
				return ReadUShort();
			case EbxFieldType.Int32:
				return ReadInt();
			case EbxFieldType.UInt32:
				return ReadUInt();
			case EbxFieldType.Int64:
				return ReadLong();
			case EbxFieldType.UInt64:
				return ReadULong();
			case EbxFieldType.Float32:
				return ReadFloat();
			case EbxFieldType.Float64:
				return ReadDouble();
			case EbxFieldType.Guid:
				return ReadGuid();
			case EbxFieldType.ResourceRef:
				return ReadResourceRef();
			case EbxFieldType.Sha1:
				return ReadSha1();
			case EbxFieldType.String:
				return ReadSizedString(32);
			case EbxFieldType.CString:
				return ReadCString(ReadUInt());
			case EbxFieldType.FileRef:
				return ReadFileRef();
			case EbxFieldType.TypeRef:
				return ReadTypeRef();
			case EbxFieldType.BoxedValueRef:
				return ReadBoxedValueRef();
			case EbxFieldType.Struct:
			{
				EbxClass @class = GetClass(parentClass, fieldType.ClassRef);
				while (Position % (long)@class.Alignment != 0L)
				{
					Position++;
				}
				object obj = CreateObject(@class);
				ReadClass(@class, obj, Position);
				return obj;
			}
			case EbxFieldType.Enum:
				return ReadInt();
			case EbxFieldType.Pointer:
			{
				uint num = ReadUInt();
				if (num >> 31 == 1)
				{
					return new PointerRef(imports[(int)(num & int.MaxValue)]);
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
			case EbxFieldType.DbObject:
				throw new InvalidDataException("DbObject");
			default:
				throw new InvalidDataException("Unknown");
			}
		}

		internal Type ParseClass(EbxClass classType)
		{
			Type type = TypeLibrary.AddType(classType.Name);
			if (type != null)
			{
				return type;
			}
			List<FieldType> list = new List<FieldType>();
			Type parentType = null;
			for (int i = 0; i < classType.FieldCount; i++)
			{
				EbxField ebxField = fieldTypes[classType.FieldIndex + i];
				if (ebxField.DebugType == EbxFieldType.Inherited)
				{
					parentType = ParseClass(classTypes[ebxField.ClassRef]);
					continue;
				}
				Type typeFromEbxField = GetTypeFromEbxField(ebxField);
				list.Add(new FieldType(ebxField.Name, typeFromEbxField, null, ebxField, (ebxField.DebugType == EbxFieldType.Array) ? new EbxField?(fieldTypes[classTypes[ebxField.ClassRef].FieldIndex]) : null));
			}
			if (classType.DebugType == EbxFieldType.Struct)
			{
				return TypeLibrary.FinalizeStruct(classType.Name, list, classType);
			}
			return TypeLibrary.FinalizeClass(classType.Name, list, parentType, classType);
		}

		internal Type GetTypeFromEbxField(EbxField fieldType)
		{
			switch (fieldType.DebugType)
			{
			case EbxFieldType.Struct:
				return ParseClass(classTypes[fieldType.ClassRef]);
			case EbxFieldType.String:
				return typeof(string);
			case EbxFieldType.Int8:
				return typeof(sbyte);
			case EbxFieldType.UInt8:
				return typeof(byte);
			case EbxFieldType.Boolean:
				return typeof(bool);
			case EbxFieldType.UInt16:
				return typeof(ushort);
			case EbxFieldType.Int16:
				return typeof(short);
			case EbxFieldType.UInt32:
				return typeof(uint);
			case EbxFieldType.Int32:
				return typeof(int);
			case EbxFieldType.UInt64:
				return typeof(ulong);
			case EbxFieldType.Int64:
				return typeof(long);
			case EbxFieldType.Float32:
				return typeof(float);
			case EbxFieldType.Float64:
				return typeof(double);
			case EbxFieldType.Pointer:
				return typeof(PointerRef);
			case EbxFieldType.Guid:
				return typeof(Guid);
			case EbxFieldType.Sha1:
				return typeof(Sha1);
			case EbxFieldType.CString:
				return typeof(CString);
			case EbxFieldType.ResourceRef:
				return typeof(ResourceRef);
			case EbxFieldType.FileRef:
				return typeof(FileRef);
			case EbxFieldType.TypeRef:
				return typeof(TypeRef);
			case EbxFieldType.BoxedValueRef:
				return typeof(ulong);
			case EbxFieldType.Array:
			{
				EbxClass ebxClass = classTypes[fieldType.ClassRef];
				return typeof(List<>).MakeGenericType(GetTypeFromEbxField(fieldTypes[ebxClass.FieldIndex]));
			}
			case EbxFieldType.Enum:
			{
				EbxClass classInfo = classTypes[fieldType.ClassRef];
				List<Tuple<string, int>> list = new List<Tuple<string, int>>();
				for (int i = 0; i < classInfo.FieldCount; i++)
				{
					list.Add(new Tuple<string, int>(fieldTypes[classInfo.FieldIndex + i].Name, (int)fieldTypes[classInfo.FieldIndex + i].DataOffset));
				}
				return TypeLibrary.AddEnum(classInfo.Name, list, classInfo);
			}
			case EbxFieldType.DbObject:
				return null;
			default:
				return null;
			}
		}

		internal string ReadString(uint offset)
		{
			if (offset == uint.MaxValue)
			{
				return "";
			}
			long position = Position;
			Position = stringsOffset + offset;
			string result = ReadNullTerminatedString();
			Position = position;
			return result;
		}

		internal CString ReadCString(uint offset)
		{
			return new CString(ReadString(offset));
		}

		internal ResourceRef ReadResourceRef()
		{
			return new ResourceRef(ReadULong());
		}

		internal FileRef ReadFileRef()
		{
			uint offset = ReadUInt();
			Position += 4L;
			return new FileRef(ReadString(offset));
		}

		internal TypeRef ReadTypeRef()
		{
			string text = ReadString(ReadUInt());
			Position += 4L;
			if (text == "")
			{
				return new TypeRef(text);
			}
			Guid result = Guid.Empty;
			if (Guid.TryParse(text, out result) && result != Guid.Empty)
			{
				return new TypeRef(TypeLibrary.Reflection.LookupType(result));
			}
			return new TypeRef(text);
		}

		internal BoxedValueRef ReadBoxedValueRef()
		{
			int num = ReadInt();
			Position += 12L;
			long position = Position;
			Position = boxedValuesOffset + num * 8;
			byte[] inData = ReadBytes(8);
			Position = position;
			return new BoxedValueRef(num, inData);
		}

		internal int HashString(string strToHash)
		{
			int num = 5381;
			for (int i = 0; i < strToHash.Length; i++)
			{
				byte b = (byte)strToHash[i];
				num = ((num * 33) ^ b);
			}
			return num;
		}
	}
}