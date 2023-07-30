using FMT.FileTools;
using FMT.FileTools.Modding;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace FrostySdk.IO._2022.Readers
{
    public class EbxReader22A : EbxReader
    {
        protected List<EbxClass> classTypes { get; set; } = new List<EbxClass>();

        internal const int EbxExternalReferenceMask = 1;

        //internal static EbxSharedTypeDescriptorV2 std { get; private set; }

        //internal static EbxSharedTypeDescriptorV2 patchStd { get; private set; }

        public List<Guid> classGuids { get; } = new List<Guid>();

        //private readonly List<Guid> typeInfoGuids = new List<Guid>();

        public bool patched;

        public Guid unkGuid;

        public long payloadPosition;

        public long arrayOffset;

        public List<uint> importOffsets { get; set; } = new List<uint>();

        public List<uint> dataContainerOffsets { get; set; } = new List<uint>();

        public override string RootType
        {
            get
            {


                //if (this.typeInfoGuids.Count > 0)
                //{
                //	Type type = TypeLibrary.GetType(this.typeInfoGuids[0]);

                //	return type?.Name ?? "UnknownType";
                //}
                if (base.instances.Count == 0)
                {
                    return string.Empty;
                }
                if (this.classGuids.Count <= base.instances[0].ClassRef)
                {
                    return string.Empty;
                }
                return TypeLibrary.GetType(this.classGuids[base.instances[0].ClassRef])?.Name ?? string.Empty;
            }
        }

        internal byte[] boxedValueBuffer;


        internal EbxReader22A(Stream inStream, bool passthru)
            : base(inStream)
        {
            Position = 0;
            if (inStream == null)
            {
                throw new ArgumentNullException("inStream");
            }
        }

        public override EbxAsset ReadAsset(EbxAssetEntry entry = null)
        {
            EbxAsset ebxAsset = new EbxAsset();
            ebxAsset.ParentEntry = entry;
            this.InternalReadObjects();
            ebxAsset.fileGuid = fileGuid;
            ebxAsset.objects = objects;
            ebxAsset.dependencies = dependencies;
            ebxAsset.refCounts = refCounts;
            return ebxAsset;
        }

        public virtual T ReadAsset<T>() where T : EbxAsset, new()
        {
            T val = new T();
            this.InternalReadObjects();
            val.fileGuid = this.fileGuid;
            val.objects = this.objects;
            val.dependencies = this.dependencies;
            val.refCounts = this.refCounts;
            return val;
        }

        public new dynamic ReadObject()
        {
            this.InternalReadObjects();
            return this.objects[0];
        }

        public override List<object> ReadObjects()
        {
            this.InternalReadObjects();
            return this.objects;
        }

        public override List<object> GetUnreferencedObjects()
        {
            List<object> list = new List<object> { this.objects[0] };
            for (int i = 1; i < this.objects.Count; i++)
            {
                if (this.refCounts[i] == 0)
                {
                    list.Add(this.objects[i]);
                }
            }
            return list;
        }

        private static IEnumerable<EbxField> _AllEbxFields;
        public static IEnumerable<EbxField> AllEbxFields
        {
            get
            {
                if (_AllEbxFields == null)
                    _AllEbxFields = EbxSharedTypeDescriptors.patchStd.Fields.Union(EbxSharedTypeDescriptors.std.Fields);

                return _AllEbxFields;
            }
        }

        private static Dictionary<uint, EbxField> NameHashToEbxField { get; } = new Dictionary<uint, EbxField>();

        public static EbxField GetEbxFieldByNameHash(uint nameHash)
        {
            if (NameHashToEbxField.ContainsKey(nameHash))
                return NameHashToEbxField[nameHash];

            var field = AllEbxFields.Single(x => x.NameHash == nameHash);
            NameHashToEbxField.Add(nameHash, field);
            return field;
        }

        public static EbxField GetEbxFieldByProperty(EbxClass classType, PropertyInfo property)
        {
            var fieldIndex = property.GetCustomAttribute<FieldIndexAttribute>().Index;
            var nameHash = property.GetCustomAttribute<HashAttribute>().Hash;
            var ebxfieldmeta = property.GetCustomAttribute<EbxFieldMetaAttribute>();
            EbxFieldType fieldType = (EbxFieldType)((ebxfieldmeta.Flags >> 4) & 0x1Fu);

            EbxField field = default(EbxField);
            if (EbxSharedTypeDescriptors.std == null)
            {
                field.Type = ebxfieldmeta.Flags;
                field.NameHash = nameHash;
                return field;
            }

            var allFields = EbxSharedTypeDescriptors.std.Fields;
            List<EbxField> classFields = EbxSharedTypeDescriptors.std.ClassFields.ContainsKey(classType) ? EbxSharedTypeDescriptors.std.ClassFields[classType] : null;
            if (classType.SecondSize >= 1 && EbxSharedTypeDescriptors.patchStd != null)
            {
                classFields = EbxSharedTypeDescriptors.patchStd.ClassFields[classType];
                allFields = allFields.Union(EbxSharedTypeDescriptors.patchStd.Fields).ToList();
            }

            if (classFields != null)
            {
                var nameHashFields = classFields.Where(x => x.NameHash == nameHash);
                field = nameHashFields.FirstOrDefault(x => (x.DebugType == fieldType || (ebxfieldmeta.IsArray && x.DebugType == ebxfieldmeta.ArrayType)));
            }
            if (field.Equals(default(EbxField)))
                field = allFields.FirstOrDefault(x => x.NameHash == nameHash && (x.DebugType == fieldType || (ebxfieldmeta.IsArray && x.DebugType == ebxfieldmeta.ArrayType)));

            return field;
        }

        private static IEnumerable<EbxClass> _AllEbxClasses;
        public static IEnumerable<EbxClass> AllEbxClasses
        {
            get
            {
                if (_AllEbxClasses == null)
                    _AllEbxClasses = EbxSharedTypeDescriptors.patchStd.Classes.Union(EbxSharedTypeDescriptors.std.Classes).Where(x => x.HasValue).Select(x => x.Value);

                return _AllEbxClasses;
            }
        }

        private static Dictionary<uint, EbxClass> NameHashToEbxClass { get; } = new Dictionary<uint, EbxClass>();

        public static EbxClass GetEbxClassByNameHash(uint nameHash)
        {
            if (NameHashToEbxClass.ContainsKey(nameHash))
                return NameHashToEbxClass[nameHash];

            var @class = AllEbxClasses.Single(x => x.NameHash == nameHash);
            NameHashToEbxClass.Add(nameHash, @class);
            return @class;
        }



        public override object ReadClass(EbxClass classType, object obj, long startOffset)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
                //base.Position += classType.Size;
                //base.Pad(classType.Alignment);
                //return null;
            }

            base.Position = startOffset;

            Type type = obj.GetType();
            var ebxClassMeta = type.GetCustomAttribute<EbxClassMetaAttribute>();

#if DEBUG
            if (type.Name.Equals("SkinnedMeshAsset"))
            {

            }

            if (type.Name.Contains("MeshMaterial"))
            {

            }

            if (type.Name.Contains("SkeletonAsset"))
            {

            }

            if (type.Name.Contains("LinearTransform"))
            {

            }

            if (type.Name.Equals("RaceVehicleBlueprint"))
            {

            }

            if (type.Name.Equals("CompositeMeshAsset"))
            {

            }

#endif

            Dictionary<PropertyInfo, EbxFieldMetaAttribute> properties = new Dictionary<PropertyInfo, EbxFieldMetaAttribute>();
            foreach (var prp in obj.GetType().GetProperties())
            {
                var ebxfieldmeta = prp.GetCustomAttribute<EbxFieldMetaAttribute>();
                if (ebxfieldmeta != null)
                {
                    properties.Add(prp, ebxfieldmeta);
                }
            }

            var orderedProps = properties
                .Where(x => x.Key.GetCustomAttribute<IsTransientAttribute>() == null && x.Key.GetCustomAttribute<FieldIndexAttribute>() != null)
                .OrderBy(x => x.Value.Offset)
                //.OrderBy(x => x.Key.GetCustomAttribute<FieldIndexAttribute>().Index)
                .ToArray();

#if DEBUG

            if (type.Name.Contains("MeshMaterial"))
            {

            }
#endif

            foreach (var property in orderedProps)
            {
                base.Position = property.Value.Offset + startOffset;

                var fieldMetaAttribute = property.Key.GetCustomAttribute<EbxFieldMetaAttribute>();
                var propNameHash = property.Key.GetCustomAttribute<HashAttribute>();
                EbxField field = default(EbxField);
                EbxFieldType debugType = (EbxFieldType)((property.Value.Flags >> 4) & 0x1Fu);
                if (propNameHash != null)
                {
                    field = GetEbxFieldByProperty(classType, property.Key);
                }


                if (debugType == EbxFieldType.Inherited)
                {
                    ReadClass(default(EbxClass), obj, base.Position);
                    continue;
                }

                //base.Position = property.Value.Offset + startOffset;

                if (debugType == EbxFieldType.Array)
                {
                    ReadArray(obj, property.Key, classType, field, false);
                    continue;
                }
                object value = Read(property.Key.PropertyType, property.Value.Offset + startOffset, ebxClassMeta.Alignment);
                property.Key.SetValue(obj, value);
            }

            if (ebxClassMeta != null)
            {
                base.Position = startOffset + ebxClassMeta.Size;
                base.Pad(ebxClassMeta.Alignment);
            }
            else
            {
                base.Position = startOffset + classType.Size;
                base.Pad(classType.Alignment);
            }
            return obj;
        }

        protected void ReadArray(object obj, PropertyInfo property, EbxClass classType, EbxField field, bool isReference)
        {
            var hashAttribute = property.GetCustomAttribute<HashAttribute>();
            var ebxFieldMetaAttribute = property.GetCustomAttribute<EbxFieldMetaAttribute>();
            var arrayBaseType = ebxFieldMetaAttribute.BaseType;

            long position = base.Position;
            base.Position = position;

            int arrayOffset = Read<int>(); 
            var newPosition = base.Position + arrayOffset - 4;


            var ebxArray = arrays.Find((EbxArray a) => a.Offset == newPosition - payloadPosition);
            
            // EbxArray Offset & Payload Position (Payload seems to be always Header = 32)
            base.Position = ebxArray.Offset + payloadPosition;
            // Minus 4 to get the Count from the Ebx Bytes
            base.Position -= 4;


            var position2 = payloadPosition + ebxFieldMetaAttribute.Offset + ebxArray.Offset;

            if (newPosition < 0 || newPosition > base.Length)
                return;

            if (base.Position < 0)
                return;

            if (base.Position > base.Length)
                return;

            uint arrayCount = Read<uint>();
            if (arrayCount < 0)
                return;

            if (arrayCount >= 999)
                return;

            for (int i = 0; i < arrayCount; i++)
            {
                object obj2 = this.ReadField(classType, field.InternalType, field.ClassRef, isReference);
                if (property != null)
                {
                    var propValue = property.GetValue(obj);
                    if (propValue == null)
                    {

                        var genericTypeDef = property.PropertyType.GetGenericTypeDefinition();
                        var genericArgs = property.PropertyType.GetGenericArguments();
                        Type constructed = genericTypeDef.MakeGenericType(genericArgs);
                        propValue = Activator.CreateInstance(constructed);
                    }

                    var propValueType = property.PropertyType;
                    if (propValueType == null)
                        continue;

                    var addMethod = propValueType.GetMethod("Add");
                    if (addMethod == null)
                        continue;

                    addMethod.Invoke(propValue, new object[1] { obj2 });
                    property.SetValue(obj, propValue);
                }
                else
                {

                }
            }
        }

        protected void ReadArray(object obj, PropertyInfo property, bool isReference)
        {
            var hashAttribute = property.GetCustomAttribute<HashAttribute>();
            var ebxFieldMetaAttribute = property.GetCustomAttribute<EbxFieldMetaAttribute>();
            var arrayBaseType = ebxFieldMetaAttribute.BaseType;

            long position = base.Position;
            base.Position = position;

            int arrayOffset = Read<int>();
            var newPosition = base.Position + arrayOffset - 4;


            var ebxArray = arrays.Find((EbxArray a) => a.Offset == newPosition - payloadPosition);

            // EbxArray Offset & Payload Position (Payload seems to be always Header = 32)
            base.Position = ebxArray.Offset + payloadPosition;
            // Minus 4 to get the Count from the Ebx Bytes
            base.Position -= 4;


            var position2 = payloadPosition + ebxFieldMetaAttribute.Offset + ebxArray.Offset;

            if (newPosition < 0 || newPosition > base.Length)
                return;

            if (base.Position < 0)
                return;

            if (base.Position > base.Length)
                return;

            uint arrayCount = Read<uint>();
            if (arrayCount < 0)
                return;

            if (arrayCount >= 999)
                return;

            for (int i = 0; i < arrayCount; i++)
            {
                object obj2 = this.Read(arrayBaseType);
                if (property != null)
                {
                    //try
                    //{
                    var propValue = property.GetValue(obj);
                    if (propValue == null)
                    {

                        var genericTypeDef = property.PropertyType.GetGenericTypeDefinition();
                        var genericArgs = property.PropertyType.GetGenericArguments();
                        Type constructed = genericTypeDef.MakeGenericType(genericArgs);
                        propValue = Activator.CreateInstance(constructed);


                    }

                    var propValueType = property.PropertyType;
                    if (propValueType == null)
                        continue;

                    var addMethod = propValueType.GetMethod("Add");
                    if (addMethod == null)
                        continue;

                    addMethod.Invoke(propValue, new object[1] { obj2 });
                    property.SetValue(obj, propValue);
                    //}
                    //catch (Exception)
                    //{
                    //}
                }
                else
                {

                }
                //EbxFieldType debugType = field.DebugType;
                //if (debugType == EbxFieldType.Pointer || debugType == EbxFieldType.CString)
                //{
                //    base.Pad(8);
                //}
            }
            //base.Position = position;

            if (obj == null)
            {

            }
        }

        //      protected bool IsFieldInClassAnArray(EbxClass @class, EbxField field)
        //{
        //	return field.TypeCategory == EbxTypeCategory.ArrayType
        //		|| field.DebugType == EbxFieldType.Array;
        //		//|| field.DebugType22 == EbxFieldType22.ArrayOfStructs;
        //}


        //  public object ReadProperty(PropertyInfo property, EbxFieldType fieldType, int classSize)
        //  {
        //      if (buffer == null || BaseStream == null)
        //          return null;

        //      switch (fieldType)
        //      {
        //          case EbxFieldType.Boolean:
        //              return base.ReadByte() > 0;
        //          case EbxFieldType.Int8:
        //              return (sbyte)base.ReadByte();
        //          case EbxFieldType.UInt8:
        //              return base.ReadByte();
        //          case EbxFieldType.Int16:
        //              return base.ReadInt16LittleEndian();
        //          case EbxFieldType.UInt16:
        //              return base.ReadUInt16LittleEndian();
        //          case EbxFieldType.Int32:
        //              return base.ReadInt32LittleEndian();
        //          case EbxFieldType.UInt32:
        //              return base.ReadUInt32LittleEndian();
        //          case EbxFieldType.Int64:
        //              return base.ReadInt64LittleEndian();
        //          case EbxFieldType.UInt64:
        //              return base.ReadUInt64LittleEndian();
        //          case EbxFieldType.Float32:
        //              return base.ReadSingleLittleEndian();
        //          case EbxFieldType.Float64:
        //              return base.ReadDoubleLittleEndian();
        //          case EbxFieldType.Guid:
        //              return base.ReadGuid();
        //          case EbxFieldType.ResourceRef:
        //              return this.ReadResourceRef();
        //          case EbxFieldType.Sha1:
        //              return base.ReadSha1();
        //          case EbxFieldType.String:
        //              return base.ReadSizedString(32);
        //          case EbxFieldType.CString:
        //              return this.ReadCString(base.ReadUInt32LittleEndian());
        //          case EbxFieldType.FileRef:
        //              return this.ReadFileRef();
        //          case EbxFieldType.TypeRef:
        //              return this.ReadTypeRef();
        //          case EbxFieldType.BoxedValueRef:
        //              return this.ReadBoxedValueRef();
        //          case EbxFieldType.Struct:
        //              {
        //                  var positionBeforeRead = base.Position;
        //var strObj = Activator.CreateInstance(property.PropertyType);
        //this.ReadClass(default(EbxClass), strObj, base.Position);
        //                  //EbxClass @class = GetClass(parentClass, fieldClassRef);
        //                  //base.Pad(@class.Alignment);
        //                  //object obj = CreateObject(@class);
        //                  //this.ReadClass(@class, obj, base.Position);
        //                  base.Position = positionBeforeRead + classSize;
        //                  //return obj;
        //                  return strObj;
        //              }
        //          case EbxFieldType.Enum:
        //              return base.ReadInt32LittleEndian();
        //          case EbxFieldType.Pointer:
        //              {
        //                  int num = base.ReadInt32LittleEndian();
        //                  if (num == 0)
        //                  {
        //                      return default(PointerRef);
        //                  }
        //                  if ((num & 1) == 1)
        //                  {
        //                      return new PointerRef(base.imports[num >> 1]);
        //                  }
        //                  long offset = base.Position - 4 + num - this.payloadPosition;
        //                  int dc = this.dataContainerOffsets.IndexOf((uint)offset);
        //                  if (dc == -1)
        //                  {
        //                      return default(PointerRef);
        //                  }
        //                  return new PointerRef(objects[dc]);
        //              }
        //          case EbxFieldType.DbObject:
        //              throw new InvalidDataException("DbObject");
        //          case EbxFieldType.Inherited:
        //              {
        //                  return null;
        //              }
        //          default:
        //              {
        //                  throw new InvalidDataException("Unknown Field Type");
        //              }
        //      }
        //  }


        public override PropertyInfo GetProperty(Type objType, EbxField field)
        {
            return objType.GetProperty(field.Name);
        }


        public override EbxClass GetClass(EbxClass? classType, int index)
        {
            Guid? guid;
            EbxClass? ebxClass;
            if (!classType.HasValue)
            {
                guid = this.classGuids[index];
                ebxClass = EbxSharedTypeDescriptors.patchStd?.GetClass(guid.Value) ?? EbxSharedTypeDescriptors.std.GetClass(guid.Value);
            }
            else
            {
                int index2 = ((base.magic != EbxVersion.Riff) ? ((short)index + (classType?.Index ?? 0)) : index);
                guid = EbxSharedTypeDescriptors.std.GetGuid(index2);
                if (classType.Value.SecondSize >= 1)
                {
                    guid = EbxSharedTypeDescriptors.patchStd.GetGuid(index2);
                    ebxClass = EbxSharedTypeDescriptors.patchStd.GetClass(index2) ?? EbxSharedTypeDescriptors.std.GetClass(guid.Value);
                }
                else
                {
                    ebxClass = EbxSharedTypeDescriptors.std.GetClass(index2);
                }
            }
            if (ebxClass.HasValue)
            {
                TypeLibrary.AddType(ebxClass.Value.Name, guid);
            }
            return ebxClass.HasValue ? ebxClass.Value : default(EbxClass);
        }


        public override EbxField GetField(EbxClass classType, int index)
        {
            if (classType.SecondSize >= 1)
            {
                return EbxSharedTypeDescriptors.patchStd.GetField(index).Value;
            }
            return EbxSharedTypeDescriptors.std.GetField(index).Value;
        }

        public override object CreateObject(EbxClass classType)
        {
            return TypeLibrary.CreateObject(classType.Name);
        }

        //public virtual Type GetType(EbxClass classType)
        //{
        //	return TypeLibrary.GetType(classType.Name);
        //}

        public override object ReadField(EbxClass? parentClass, EbxFieldType fieldType, ushort fieldClassRef, bool dontRefCount = false)
        {
            if (buffer == null || BaseStream == null)
                return null;

            switch (fieldType)
            {
                case EbxFieldType.Boolean:
                    return base.ReadByte() > 0;
                case EbxFieldType.Int8:
                    return (sbyte)base.ReadByte();
                case EbxFieldType.UInt8:
                    return base.ReadByte();
                case EbxFieldType.Int16:
                    return base.ReadInt16LittleEndian();
                case EbxFieldType.UInt16:
                    return base.ReadUInt16LittleEndian();
                case EbxFieldType.Int32:
                    return base.ReadInt32LittleEndian();
                case EbxFieldType.UInt32:
                    return base.ReadUInt32LittleEndian();
                case EbxFieldType.Int64:
                    return base.ReadInt64LittleEndian();
                case EbxFieldType.UInt64:
                    return base.ReadUInt64LittleEndian();
                case EbxFieldType.Float32:
                    return base.ReadSingleLittleEndian();
                case EbxFieldType.Float64:
                    return base.ReadDoubleLittleEndian();
                case EbxFieldType.Guid:
                    return base.ReadGuid();
                case EbxFieldType.ResourceRef:
                    return this.ReadResourceRef();
                case EbxFieldType.Sha1:
                    //return base.ReadSha1();
                    throw new NotImplementedException("Sha1 not supported!");
                case EbxFieldType.String:
                    return base.ReadSizedString(32);
                case EbxFieldType.CString:
                    return this.ReadCString(base.ReadUInt32LittleEndian());
                case EbxFieldType.FileRef:
                    return this.ReadFileRef();
                case EbxFieldType.TypeRef:
                    return this.ReadTypeRef();
                case EbxFieldType.BoxedValueRef:
                    return this.ReadBoxedValueRef();
                case EbxFieldType.Struct:
                    {
                        var positionBeforeRead = base.Position;
                        EbxClass @class = GetClass(parentClass, fieldClassRef);
                        base.Pad(@class.Alignment);
                        object obj = CreateObject(@class);
                        this.ReadClass(@class, obj, base.Position);
                        base.Position = positionBeforeRead + @class.Size;
                        return obj;
                    }
                case EbxFieldType.Enum:
                    return base.ReadInt32LittleEndian();
                case EbxFieldType.Pointer:
                    {
                        int num = base.ReadInt32LittleEndian();
                        var mutatedNum = (num & 1);
                        var mutatedNum2 = num >> 1;
                        if (num == 0)
                        {
                            return default(PointerRef);
                        }
                        if (mutatedNum == 1 && mutatedNum2 > -1 && mutatedNum2 < base.imports.Count - 1)
                        {
                            return new PointerRef(base.imports[mutatedNum2]);
                        }
                        long offset = base.Position - 4 + num - this.payloadPosition;

                        int dc = this.dataContainerOffsets.IndexOf((uint)offset);
                        if (dc == -1)
                            return default(PointerRef);

                        if (dc > objects.Count - 1)
                            return default(PointerRef);

                        //if (!dontRefCount)
                        //{
                        //	base.refCounts[dc]++;
                        //}
                        return new PointerRef(objects[dc]);
                    }
                case EbxFieldType.DbObject:
                    throw new InvalidDataException("DbObject");
                case EbxFieldType.Inherited:
                    {
                        return null;
                    }
                default:
                    {
                        DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 1);
                        defaultInterpolatedStringHandler.AppendLiteral("Unknown field type ");
                        defaultInterpolatedStringHandler.AppendFormatted(fieldType);
                        throw new InvalidDataException(defaultInterpolatedStringHandler.ToStringAndClear());
                    }
            }
        }

        //internal Type ParseClass(EbxClass classType)
        //{
        //	Type type = TypeLibrary.AddType(classType.Name);
        //	if (type != null)
        //	{
        //		return type;
        //	}
        //	List<FieldType> list = new List<FieldType>();
        //	Type parentType = null;
        //	for (int i = 0; i < classType.FieldCount; i++)
        //	{
        //		EbxField ebxField = this.fieldTypes[classType.FieldIndex + i];
        //		if (ebxField.DebugType == EbxFieldType.Inherited)
        //		{
        //			parentType = this.ParseClass(this.classTypes[ebxField.ClassRef]);
        //			continue;
        //		}
        //		Type typeFromEbxField = this.GetTypeFromEbxField(ebxField);
        //		list.Add(new FieldType(ebxField.Name, typeFromEbxField, null, ebxField, (ebxField.DebugType == EbxFieldType.Array) ? new EbxField?(this.fieldTypes[this.classTypes[ebxField.ClassRef].FieldIndex]) : null));
        //	}
        //	if (classType.DebugType == EbxFieldType.Struct)
        //	{
        //		return TypeLibrary.FinalizeStruct(classType.Name, list, classType);
        //	}
        //	return TypeLibrary.FinalizeClass(classType.Name, list, parentType, classType);
        //}

        internal new Type GetTypeFromEbxField(EbxField fieldType)
        {
            switch (fieldType.DebugType)
            {
                case EbxFieldType.DbObject:
                    return null;
                case EbxFieldType.Struct:
                    return this.ParseClass(this.classTypes[fieldType.ClassRef]);
                case EbxFieldType.Pointer:
                    return typeof(PointerRef);
                case EbxFieldType.Array:
                    {
                        EbxClass ebxClass = this.classTypes[fieldType.ClassRef];
                        return typeof(List<>).MakeGenericType(this.GetTypeFromEbxField(this.fieldTypes[ebxClass.FieldIndex]));
                    }
                case EbxFieldType.String:
                    return typeof(string);
                case EbxFieldType.CString:
                    return typeof(CString);
                case EbxFieldType.Enum:
                    {
                        EbxClass classInfo = this.classTypes[fieldType.ClassRef];
                        List<Tuple<string, int>> list = new List<Tuple<string, int>>();
                        for (int i = 0; i < classInfo.FieldCount; i++)
                        {
                            list.Add(new Tuple<string, int>(this.fieldTypes[classInfo.FieldIndex + i].Name, (int)this.fieldTypes[classInfo.FieldIndex + i].DataOffset));
                        }
                        return TypeLibrary.AddEnum(classInfo.Name, list, classInfo);
                    }
                case EbxFieldType.FileRef:
                    return typeof(FileRef);
                case EbxFieldType.Boolean:
                    return typeof(bool);
                case EbxFieldType.Int8:
                    return typeof(sbyte);
                case EbxFieldType.UInt8:
                    return typeof(byte);
                case EbxFieldType.Int16:
                    return typeof(short);
                case EbxFieldType.UInt16:
                    return typeof(ushort);
                case EbxFieldType.Int32:
                    return typeof(int);
                case EbxFieldType.UInt32:
                    return typeof(uint);
                case EbxFieldType.UInt64:
                    return typeof(ulong);
                case EbxFieldType.Int64:
                    return typeof(long);
                case EbxFieldType.Float32:
                    return typeof(float);
                case EbxFieldType.Float64:
                    return typeof(double);
                case EbxFieldType.Guid:
                    return typeof(Guid);
                case EbxFieldType.Sha1:
                    return typeof(Sha1);
                case EbxFieldType.ResourceRef:
                    return typeof(ResourceRef);
                case EbxFieldType.TypeRef:
                    return typeof(TypeRef);
                case EbxFieldType.BoxedValueRef:
                    return typeof(ulong);
                default:
                    return null;
            }
        }

        internal new string ReadString(uint offset)
        {
            if (offset == uint.MaxValue)
            {
                return string.Empty;
            }
            long position = base.Position;
            if (this.magic == EbxVersion.Riff)
            {
                if (offset > base.Length)
                    return string.Empty;

                if (position + offset - 4 > base.Length)
                    return string.Empty;

                base.Position = position + offset - 4;
            }
            else
            {
                base.Position = this.stringsOffset + offset;
            }
            string result = base.ReadNullTerminatedString();
            base.Position = position;
            return result;
        }

        internal new CString ReadCString(uint offset)
        {
            return new CString(this.ReadString(offset));
        }

        internal new ResourceRef ReadResourceRef()
        {
            return new ResourceRef(base.ReadUInt64LittleEndian());
        }

        internal new FileRef ReadFileRef()
        {
            uint offset = base.ReadUInt32LittleEndian();
            base.Position += 4L;
            return new FileRef(this.ReadString(offset));
        }

        internal new TypeRef ReadTypeRef()
        {
            return new TypeRef(base.ReadUInt32LittleEndian().ToString(CultureInfo.InvariantCulture));
        }

        internal new BoxedValueRef ReadBoxedValueRef()
        {
            uint value = base.ReadUInt32LittleEndian();
            int unk = base.ReadInt32LittleEndian();
            long offset = base.ReadInt64LittleEndian();
            long restorePosition = base.Position;
            try
            {
                _ = -1;
                if ((value & 0x80000000u) == 2147483648u)
                {
                    value &= 0x7FFFFFFFu;
                    EbxFieldType typeCode = (EbxFieldType)((value >> 5) & 0x1Fu);
                    base.Position += offset - 8;
                    return new BoxedValueRef(this.ReadField(null, typeCode, ushort.MaxValue), typeCode);
                }
                return new BoxedValueRef();
            }
            finally
            {
                base.Position = restorePosition;
            }
        }

        internal new int HashString(string strToHash)
        {
            int num = 5381;
            for (int i = 0; i < strToHash.Length; i++)
            {
                byte b = (byte)strToHash[i];
                num = (num * 33) ^ b;
            }
            return num;
        }
    }
}