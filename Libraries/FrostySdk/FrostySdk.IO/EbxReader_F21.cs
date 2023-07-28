using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.Managers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FrostySdk.IO
{
    public class EbxReader_F21 : EbxReader2021
    {
        public string CasPath { get; set; }

        public override string RootType
        {
            get
            {
                if (classGuids.Count > 0 && instances.Count > 0)
                {
                    if (classGuids.Count > instances[0].ClassRef)
                    {
                        Type type = TypeLibrary.GetType(classGuids[instances[0].ClassRef]);
                        if (type != null)
                        {
                            return type.Name;
                        }
                    }
                }
                return string.Empty;
                //return ClassTypes[instances[0].ClassRef].Name;
            }
        }

        protected string InBoundName { get; private set; }
        protected Type ConcreteTypeOfEbx { get; private set; }
        protected Object ConcreteObjectOfEbx { get; private set; }
        public long CasOffset { get; internal set; }

        public EbxReader_F21(Stream InStream, bool inPatched, string name = "FromCas")
            : base(InStream, inPatched)
        {
            InBoundName = name;

            if (InBoundName.Contains("gp_") || InBoundName.Contains("ballrules", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists($"Debugging/EBX/{InBoundName}.dat"))
                    File.Delete($"Debugging/EBX/{InBoundName}.dat");

                FileStream fileStreamFromCas = new FileStream($"Debugging/EBX/{InBoundName}.dat", FileMode.OpenOrCreate);
                var pos = InStream.Position;
                InStream.Position = 0;
                InStream.CopyTo(fileStreamFromCas);
                InStream.Position = pos;
                fileStreamFromCas.Close();
                fileStreamFromCas.Dispose();
            }


            if (TypeLibrary.Reflection.ConcreteTypes == null && ProfileManager.IsMadden21DataVersion(ProfileManager.Game))
            {
                TypeLibrary.Reflection.LoadClassInfoAssets(AssetManager.Instance);
            }

            if (TypeLibrary.Reflection.ConcreteTypes != null && TypeLibrary.Reflection.ConcreteTypes.Any(x => x.Name == InBoundName))
            {
                ConcreteTypeOfEbx = TypeLibrary.Reflection.ConcreteTypes.FirstOrDefault(x => x.Name == InBoundName);
                ConcreteObjectOfEbx = TypeLibrary.CreateObject(ConcreteTypeOfEbx);
            }

            //InStream.Position = 0;
            //InitialRead(InStream, fs, inPatched);
        }

        public override void InitialRead(Stream InStream, bool inPatched)
        {
            //if (!string.IsNullOrEmpty(InBoundName) && InBoundName.Contains("gp_"))
            //{
            //}

            //if (std == null || patchStd == null)
            //{
            //	std = new EbxSharedTypeDescriptors(AssetManager.Instance.fs, "SharedTypeDescriptors.ebx", patch: false);
            //	if (AssetManager.Instance.fs.HasFileInMemoryFs("SharedTypeDescriptors_patch.ebx"))
            //	{
            //		patchStd = new EbxSharedTypeDescriptors(AssetManager.Instance.fs, "SharedTypeDescriptors_patch.ebx", patch: true);
            //	}
            //	else if (AssetManager.Instance.fs.HasFileInMemoryFs("SharedTypeDescriptors_Patch.ebx"))
            //	{
            //		patchStd = new EbxSharedTypeDescriptors(AssetManager.Instance.fs, "SharedTypeDescriptors_Patch.ebx", patch: true);
            //	}
            //}
            patched = inPatched;
            ebxVersion = (EbxVersion)ReadUInt();
            if (ebxVersion != EbxVersion.Version2 && ebxVersion != EbxVersion.Version4)
            {
                //throw new Exception("Magic is not found");
                Debug.WriteLine("-- Magic is not found");
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
            boxedValuesCount = ReadUInt();
            boxedValuesOffset = ReadUInt();
            boxedValuesOffset += stringsOffset + stringsLen;
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
                item2.Type = ((ebxVersion == EbxVersion.Version2) ? ReadUShort() : ((ushort)(ReadUShort() >> 1)));
                item2.ClassRef = ReadUShort();
                item2.DataOffset = ReadUInt();
                item2.SecondOffset = ReadUInt();
                item2.Name = dictionary[key2];
                fieldTypes.Add(item2);
            }
            for (int k = 0; k < classTypeCount; k++)
            {
                Guid item3 = ReadGuid();
                classGuids.Add(item3);
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
            Position = stringsOffset;
            InBoundName = ReadNullTerminatedString();

            //FileStream fileStreamFromCas = new FileStream($"Debugging/EBX/{InBoundName.Split('/')[InBoundName.Split('/').Length-1]}.dat", FileMode.OpenOrCreate);
            //var pos = Position;
            //Position = 0;
            //InStream.CopyTo(fileStreamFromCas);
            //Position = pos;
            //fileStreamFromCas.Close();
            //fileStreamFromCas.Dispose();

            Position = stringsOffset + stringsLen;


            isValid = true;

        }


        public override void InternalReadObjects()
        {
            Position = stringsOffset + stringsLen;
            //#if DEBUG
            //            var pos = Position;
            //            if (File.Exists($"Debugging/EBX/_DebugCurrentEBX.dat"))
            //                File.Delete($"Debugging/EBX/_DebugCurrentEBX.dat");

            //            using (FileStream fileStreamFromCas = new FileStream($"Debugging/EBX/_DebugCurrentEBX.dat", FileMode.OpenOrCreate))
            //            {
            //                Position = 0;
            //                BaseStream.CopyTo(fileStreamFromCas);
            //                Position = pos;
            //            }
            //#endif
            //

            foreach (EbxInstance instance in instances)
            {
                //try
                //{
                Type type = TypeLibrary.GetType(classGuids[instance.ClassRef]);
                if (type != null)
                {
                    for (int i = 0; i < instance.Count; i++)
                    {
                        objects.Add(TypeLibrary.CreateObject(type));
                        refCounts.Add(0);
                    }
                }
                //}
                //catch(Exception e)
                //            {
                //	AssetManager.Instance.logger.LogError("Error in EbxReader: " + e.Message);
                //            }
            }
            int num = 0;
            int num2 = 0;
            foreach (EbxInstance instance2 in instances)
            {
                for (int j = 0; j < instance2.Count; j++)
                {
                    //if (objects.Count > num)
                    //{
                    dynamic obj = objects[num++];
                    Type objType = obj.GetType();
                    EbxClass @class = GetClass(objType);
                    while (base.Position % @class.Alignment != 0L)
                    {
                        base.Position++;
                    }
                    Guid inGuid = Guid.Empty;
                    if (instance2.IsExported)
                    {
                        inGuid = ReadGuid();
                    }
                    if (@class.Alignment != 4)
                    {
                        base.Position += 8L;
                    }
                    obj.SetInstanceGuid(new AssetClassGuid(inGuid, num2++));
                    this.ReadClass(@class, obj, base.Position - 8);
                    //}
                }
            }
        }

        public override EbxField GetField(EbxClass classType, int index)
        {
            if (classType.SecondSize == 1)
            {
                return patchStd.GetField(index).Value;
            }
            return std.GetField(index).Value;
        }

        public new object ReadClass(EbxClassMetaAttribute classMeta, object obj, Type objType, long startOffset)
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
                    int index = ReadInt();
                    EbxArray ebxArray = arrays[index];
                    long position2 = base.Position;
                    base.Position = arraysOffset + ebxArray.Offset;
                    propertyInfo?.GetValue(obj).GetType().GetMethod("Clear")
                        .Invoke(propertyInfo.GetValue(obj), new object[0]);
                    for (int i = 0; i < ebxArray.Count; i++)
                    {
                        object obj2 = ReadField(customAttribute2.ArrayType, customAttribute2.BaseType, customAttribute != null);
                        propertyInfo?.GetValue(obj).GetType().GetMethod("Add")
                            .Invoke(propertyInfo.GetValue(obj), new object[1]
                            {
                            obj2
                            });
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

        private Dictionary<EbxField, PropertyInfo> GetFieldListFromClass(EbxClass classType, object obj)
        {
            Type type = obj.GetType();
            Dictionary<EbxField, PropertyInfo> fieldList = new Dictionary<EbxField, PropertyInfo>();
            for (int i = 0; i < classType.FieldCount; i++)
            {
                EbxField field = GetField(classType, classType.FieldIndex + i);
                if (field.DebugType != EbxFieldType.Inherited)
                {
                    var propInfo = GetProperty(type, field);
                    if (propInfo != null)
                    {
                        field.Name = propInfo.Name;
                        fieldList.Add(field, propInfo);
                    }
                    else
                    {
                        Debug.WriteLine("[ERROR] Unable to find Property " + field.ClassRef + " for " + InBoundName);
                    }
                }
                else
                    fieldList.Add(field, null);
            }

            return fieldList;
        }

        internal new object ReadField(EbxFieldType type, Type baseType, bool dontRefCount = false)
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
                    return ReadInt();
                case EbxFieldType.FileRef:
                    return ReadFileRef();
                case EbxFieldType.Boolean:
                    return ReadByte() > 0;
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
                case EbxFieldType.UInt64:
                    return ReadULong();
                case EbxFieldType.Int64:
                    return ReadLong();
                case EbxFieldType.Float32:
                    return ReadFloat();
                case EbxFieldType.Float64:
                    return ReadFloat();
                case EbxFieldType.Guid:
                    return ReadGuid();
                case EbxFieldType.Sha1:
                    throw new NotImplementedException("Sha1 not supported!");
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

    public class PropertyLocationAndValue
    {
        public string CasFileLocation;
        public long Offset;
        public string ObjectName;
        public string PropertyName;
        public object PropertyValue;

        public override bool Equals(object obj)
        {
            var other = obj as PropertyLocationAndValue;
            if (other != null)
            {
                if (other.PropertyName == this.PropertyName
                    && other.ObjectName == this.ObjectName)
                {
                    return true;
                }
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            //stringBuilder.Append(" File: " + CasFileLocation);
            stringBuilder.Append(" Object: " + ObjectName);
            stringBuilder.Append(" Property: " + PropertyName);
            stringBuilder.Append(" Value: " + PropertyValue.ToString());
            return base.ToString();
        }



    }




}
/*
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FrostySdk.IO
{
	public class EbxReader_F21 : EbxReaderV2
	{
		public string CasPath { get; set; }

		public override string RootType
		{
			get
			{
				if (classGuids.Count > 0 && instances.Count > 0)
				{
					Type type = TypeLibrary.GetType(classGuids[instances[0].ClassRef]);
					if (type != null)
					{
						return type.Name;
					}
				}
				return string.Empty;
				//return ClassTypes[instances[0].ClassRef].Name;
			}
		}

		protected string InBoundName { get; private set; }
		protected Type ConcreteTypeOfEbx { get; private set; }
		protected Object ConcreteObjectOfEbx { get; private set; }
        public long CasOffset { get; internal set; }

        public EbxReader_F21(Stream InStream, FileSystem fs, bool inPatched, string name = "FromCas")
			: base(InStream, fs, inPatched)
		{
			InBoundName = name;

			if (InBoundName.Contains("gp_"))
			{
				if (File.Exists($"Debugging/EBX/{InBoundName}.dat"))
					File.Delete($"Debugging/EBX/{InBoundName}.dat");

				FileStream fileStreamFromCas = new FileStream($"Debugging/EBX/{InBoundName}.dat", FileMode.OpenOrCreate);
				var pos = InStream.Position;
				InStream.Position = 0;
				InStream.CopyTo(fileStreamFromCas);
				InStream.Position = pos;
				fileStreamFromCas.Close();
				fileStreamFromCas.Dispose();
			}

			if (TypeLibrary.Reflection.ConcreteTypes != null && TypeLibrary.Reflection.ConcreteTypes.Any(x => x.Name == InBoundName))
			{
				ConcreteTypeOfEbx = TypeLibrary.Reflection.ConcreteTypes.FirstOrDefault(x => x.Name == InBoundName);
				ConcreteObjectOfEbx = TypeLibrary.CreateObject(ConcreteTypeOfEbx);
			}

			//InStream.Position = 0;
			//InitialRead(InStream, fs, inPatched);
		}

        public override void InitialRead(Stream InStream, FileSystem fs, bool inPatched)
        {
			if (!string.IsNullOrEmpty(InBoundName) && InBoundName.Contains("gp_"))
			{
			}

			if (std == null)
			{
				std = new EbxSharedTypeDescriptors(fs, "SharedTypeDescriptors.ebx", patch: false);
				if (fs.HasFileInMemoryFs("SharedTypeDescriptors_patch.ebx"))
				{
					patchStd = new EbxSharedTypeDescriptors(fs, "SharedTypeDescriptors_patch.ebx", patch: true);
				}
			}
			patched = inPatched;
			magic = (EbxVersion)ReadUInt();
			if (magic != EbxVersion.Version2 && magic != EbxVersion.Version4)
			{
				//throw new Exception("Magic is not found");
				Debug.WriteLine("-- Magic is not found");
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
			boxedValuesCount = ReadUInt();
			boxedValuesOffset = ReadUInt();
			boxedValuesOffset += stringsOffset + stringsLen;
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
				Guid item3 = ReadGuid();
				classGuids.Add(item3);
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
                    //Count = ReadUInt(),
                    //Offset = ReadUInt(),
                    //ClassRef = ReadInt(),
                };
				arrays.Add(item5);
			}
			Position = stringsOffset;
			InBoundName = ReadNullTerminatedString();

			//FileStream fileStreamFromCas = new FileStream($"Debugging/EBX/{InBoundName.Split('/')[InBoundName.Split('/').Length-1]}.dat", FileMode.OpenOrCreate);
			//var pos = Position;
			//Position = 0;
			//InStream.CopyTo(fileStreamFromCas);
			//Position = pos;
			//fileStreamFromCas.Close();
			//fileStreamFromCas.Dispose();

			Position = stringsOffset + stringsLen;


			isValid = true;

		}


        public override void InternalReadObjects()
        {
			Position = stringsOffset + stringsLen;
#if DEBUG 
			var pos = Position;
			if (File.Exists($"Debugging/EBX/_DebugCurrentEBX.dat"))
				File.Delete($"Debugging/EBX/_DebugCurrentEBX.dat");

			using (FileStream fileStreamFromCas = new FileStream($"Debugging/EBX/_DebugCurrentEBX.dat", FileMode.OpenOrCreate))
			{
				Position = 0;
				BaseStream.CopyTo(fileStreamFromCas);
				Position = pos;
			}
#endif
			//
			if(InBoundName == "SM_FocusCharacterScale_PedestalOffset_Expression")
            {

            }
			foreach (EbxInstance instance in instances)
			{
				Type type = TypeLibrary.GetType(classGuids[instance.ClassRef]);
				for (int i = 0; i < instance.Count; i++)
				{
					objects.Add(TypeLibrary.CreateObject(type));
					refCounts.Add(0);
				}
			}
			int num = 0;
			int num2 = 0;
			foreach (EbxInstance instance2 in instances)
			{
				for (int j = 0; j < instance2.Count; j++)
				{
					dynamic val = objects[num++];
					Type objType = val.GetType();
					EbxClass @class = GetClass(objType);
					while (Position % (long)@class.Alignment != 0L)
					{
						Position++;
					}
					Guid inGuid = Guid.Empty;
					if (instance2.IsExported)
					{
						inGuid = ReadGuid();
					}
                    StartingDataPosition = Position;
					val.SetInstanceGuid(new AssetClassGuid(inGuid, num2++));
					this.ReadClass(@class, val, Position);
				}
			}
        }

		public long StartingDataPosition = 0;

		internal override EbxField GetField(EbxClass classType, int index)
		{
            if (classType.SecondSize == 1)
            {
                return patchStd.GetField(index);
            }
            return std.GetField(index);
		}
		
		internal override object ReadClass(EbxClass classType, object obj, long startOffset)
		{
			try
            {
                Position = startOffset;

                Dictionary<EbxField, PropertyInfo> fieldList = GetFieldListFromClass(classType, obj);

                foreach (var field in fieldList.Keys)
                {
                    PropertyInfo property = fieldList[field];
					//Position = StartingDataPosition + field.DataOffset;

					IsReferenceAttribute isReferenceAttribute = (property != null) ? property.GetCustomAttribute<IsReferenceAttribute>() : null;
                    if (field.DebugType == EbxFieldType.Inherited)
                    {
						//if (InBoundName == "SM_FocusCharacterScale_PedestalOffset_Expression")
						//{

						//}
						//var inheritedClass = GetClass(classType, field.ClassRef);
      //                  //ReadClass(inheritedClass, obj, StartingDataPosition);
                        ReadClass(GetClass(classType, field.ClassRef), obj, Position);
                        continue;
                    }
                    //              if (field.DebugType == EbxFieldType.Array)
                    //              {
                    //Position = startOffset + field.DataOffset;
                    //              }
                    if (field.DebugType == EbxFieldType.ResourceRef || field.DebugType == EbxFieldType.TypeRef || field.DebugType == EbxFieldType.FileRef || field.DebugType == EbxFieldType.BoxedValueRef || field.DebugType == EbxFieldType.UInt64 || field.DebugType == EbxFieldType.Int64 || field.DebugType == EbxFieldType.Float64)
                    {
                        while (Position % (long)classType.Alignment != 0L)
                        {

                            Position++;
                        }
                    }
					
					if (field.DebugType == EbxFieldType.Array || field.DebugType == EbxFieldType.Pointer)
					{
						while (Position % (long)classType.Alignment != 0L)
						{
							Position++;
						}
					}

                    if (field.DebugType == EbxFieldType.Array)
                    {
                        EbxClass @class = GetClass(classType, field.ClassRef);
                        int index = 0;

                        do
                        {
                            index = ReadInt();
                        }
                        while (index > arrays.Count-1 || index < 0);

                        if (index > arrays.Count - 1 || index < 0)
                        {
                            Debug.WriteLine($"[ERROR] EBXReader::ReadClass:: Index {index} out of array list for {InBoundName}.{property.Name} at Position {Position - 4}");



                            index = 0;
                        }

                        EbxArray ebxArray = arrays[index];

                        long position = Position;
                        Position = arraysOffset + ebxArray.Offset;
                        for (int j = 0; j < ebxArray.Count; j++)
                        {
                            PropertyLocationAndValue propertyLocationAndValue = new PropertyLocationAndValue();
                            propertyLocationAndValue.Offset = Position;
                            propertyLocationAndValue.ObjectName = InBoundName;
                            var f = GetField(@class, @class.FieldIndex);

                            object obj2 = ReadField(@class, f, isReferenceAttribute != null);

                            if (property != null)
                            {
                                //try
                                //{
                                propertyLocationAndValue.PropertyName = property.Name;

                                property.GetValue(obj).GetType().GetMethod("Add")
                                    .Invoke(property.GetValue(obj), new object[1]
                                    {
                                        obj2
                                    });

                                propertyLocationAndValue.PropertyValue = obj2;

                                //}
                                //catch (Exception)
                                //{
                                //}
                                SavePropertyLocationToFileCache(propertyLocationAndValue);
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
                        var pos = Position;
                        if (property != null)
                        {
                            PropertyLocationAndValue propertyLocationAndValue = new PropertyLocationAndValue();
                            //propertyLocationAndValue.Offset = pos;
                            FileStream fileStream = BaseStream as FileStream;
                            if (fileStream != null)
                            {
                                propertyLocationAndValue.CasFileLocation = fileStream.Name;
                            }

                            if (!string.IsNullOrEmpty(CasPath))
                                propertyLocationAndValue.CasFileLocation = CasPath;

                            propertyLocationAndValue.Offset = pos;
                            propertyLocationAndValue.ObjectName = InBoundName;
                            propertyLocationAndValue.PropertyName = property.Name;

                            object v = ReadField(classType, field, isReferenceAttribute != null);

                            try
                            {
                                property.SetValue(obj, v);
                                propertyLocationAndValue.PropertyValue = v;
                            }
                            catch (Exception)
                            {
                            }

                            SavePropertyLocationToFileCache(propertyLocationAndValue);

                        }

                    }
                }
                //while (Position % 4 != 0L)
                while (Position % (long)classType.Alignment != 0L)
                {
                    Position++;
                }
            }
            catch
            {

            }
			return null;
		}

        private Dictionary<EbxField, PropertyInfo> GetFieldListFromClass(EbxClass classType, object obj)
        {
            Type type = obj.GetType();
            Dictionary<EbxField, PropertyInfo> fieldList = new Dictionary<EbxField, PropertyInfo>();
            for (int i = 0; i < classType.FieldCount; i++)
            {
                EbxField field = GetField(classType, classType.FieldIndex + i);
                if (field.DebugType != EbxFieldType.Inherited)
                {
                    var propInfo = GetProperty(type, field);
					if (propInfo != null)
					{
						field.Name = propInfo.Name;
						fieldList.Add(field, propInfo);
					}
					else
                    {
						Debug.WriteLine("[ERROR] Unable to find Property " + field.ClassRef + " for " + InBoundName);
                    }
                }
                else
                    fieldList.Add(field, null);
            }

            return fieldList;
        }

        public void SavePropertyLocationToFileCache(PropertyLocationAndValue value)
		{
			return;

			if(!Directory.Exists("_EBXEditingFolder"))
            {
				Directory.CreateDirectory("_EBXEditingFolder");
                if (!Directory.Exists("_EBXEditingFolder/EBXOffsets/"))
                {
					Directory.CreateDirectory("_EBXEditingFolder/EBXOffsets/");
                }
			}

			var fileToSave = "EBXLocationList.dat";
			if(!string.IsNullOrEmpty(InBoundName))
            {
				fileToSave = "_EBXEditingFolder/EBXOffsets/" + InBoundName + ".dat";
            }

			if (value.ObjectName.Contains("gp_"))
			{
				//fileToSave = "GPEBXLocationList.dat";

				List<PropertyLocationAndValue> propertyLocationAndValues = new List<PropertyLocationAndValue>();
				if (File.Exists(fileToSave))
				{
					propertyLocationAndValues = JsonConvert.DeserializeObject<
						List<PropertyLocationAndValue>>(File.ReadAllText(fileToSave));
				}

				if (propertyLocationAndValues.Contains(value))
					propertyLocationAndValues.Remove(value);

				propertyLocationAndValues.Add(value);

				if (File.Exists(fileToSave))
				{
					File.Delete(fileToSave);
				}

				File.WriteAllText(fileToSave, JsonConvert.SerializeObject(propertyLocationAndValues));
			}
		}
	}

	public class PropertyLocationAndValue
    {
		public string CasFileLocation;
		public long Offset;
		public string ObjectName;
		public string PropertyName;
		public object PropertyValue;

        public override bool Equals(object obj)
        {
			var other = obj as PropertyLocationAndValue;
			if(other != null)
            {
				if(other.PropertyName == this.PropertyName
					&& other.ObjectName == this.ObjectName)
                {
					return true;
                }
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override string ToString()
        {
			StringBuilder stringBuilder = new StringBuilder();
			//stringBuilder.Append(" File: " + CasFileLocation);
			stringBuilder.Append(" Object: " + ObjectName);
			stringBuilder.Append(" Property: " + PropertyName);
			stringBuilder.Append(" Value: " + PropertyValue.ToString());
            return base.ToString();
        }
    }


	

}
*/