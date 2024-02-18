using FMT.FileTools;
using FMT.Logging;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.IO._2022.Readers;
using FrostySdk.Managers;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using static PInvoke.BCrypt;


namespace FrostySdk.FrostySdk.IO
{
    public class EbxWriter2022 : EbxBaseWriter
    {
    

        protected readonly List<object> objsToProcess = new List<object>();

        private readonly List<Type> typesToProcess = new List<Type>();

        private readonly List<EbxFieldMetaAttribute> arrayTypes = new List<EbxFieldMetaAttribute>();

        private readonly List<object> objs = new List<object>();

        private List<object> sortedObjs { get; } = new List<object>();

        private readonly List<Guid> dependencies = new List<Guid>();

        /// <summary>
        /// Only used for Indexing. Otherwise redundant.
        /// </summary>
        //private  List<EbxClass> classTypes { get; set; } = new List<EbxClass>();

        private int ClassTypeCount = 0;

        private readonly List<Guid> classGuids = new List<Guid>();

        private readonly List<uint> typeInfoSignatures = new List<uint>();

        private readonly List<EbxField> fieldTypes = new List<EbxField>();

        private readonly List<string> typeNames = new List<string>();

        private readonly List<EbxImportReference> imports = new List<EbxImportReference>();

        private readonly List<EbxInstance> instances = new List<EbxInstance>();

        private List<EbxArray> arrays { get; } = new List<EbxArray>();

        private readonly List<byte[]> arrayData = new List<byte[]>();

        private readonly List<int> dataContainerOffsets = new List<int>();

        private readonly List<(int pointerOffset, int arrayIndex)> pointerOffsets = new List<(int, int)>();

        private readonly List<int> patchedPointerOffsets = new List<int>();

        private readonly List<(int resourceRefOffset, int arrayIndex)> resourceRefOffsets = new List<(int, int)>();

        private readonly List<int> patchedResourceRefOffsets = new List<int>();

        private readonly List<(int importOffset, int arrayIndex)> importOffsets = new List<(int, int)>();

        private readonly List<int> patchedImportOffsets = new List<int>();

        private readonly Dictionary<string, List<(int offset, int containingArrayIndex)>> stringsToCStringOffsets = new Dictionary<string, List<(int, int)>>();

        private readonly Dictionary<(int offset, int containingArrayIndex), int> pointerRefPositionToDataContainerIndex = new Dictionary<(int, int), int>();

        private List<(int arrayOffset, int arrayIndex, int containingArrayIndex, int dataContainerIndex, PropertyInfo property)> unpatchedArrayInfo { get; } = new();

        private readonly List<int> arrayIndicesMap = new List<int>();

        private List<EbxArray> originalAssetArrays;

        private HashSet<Type> uniqueTypes = new HashSet<Type>();

        private long payloadPosition;

        private int arraysPosition;

        private int boxedValuesPosition;

        private int stringTablePosition;

        private int currentArrayIndex = -1;

        private int currentArrayDepth;

        private byte[] mainData { get; set; }

        private ushort uniqueClassCount;

        private int exportedCount;

        public EbxWriter2022(Stream inStream, EbxWriteFlags inFlags = EbxWriteFlags.None, bool leaveOpen = false)
            : base(inStream, inFlags, true)
        {
        }

        public override void WriteAsset(EbxAsset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException("asset");
            }
            originalAssetArrays = asset.arrays;
            WriteEbxObjects(asset.RootObjects.Distinct(), asset.FileGuid);
        }

        public void WriteEbxObjects(IEnumerable<object> objects, Guid fileGuid)
        {
            // Check objects parameter
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));

            // Check fileGuid parameter
            if (fileGuid == Guid.Empty)
                throw new ArgumentNullException(nameof(fileGuid));

#if DEBUG
            if (objects.ToArray()[0].GetType().Name == "AttribSchema_gp_kickshot_shooting")
            {

            }
#endif

            Queue<object> queue = new Queue<object>();
            foreach (object @object in objects)
            {
                queue.Enqueue(@object);
            }

            while (queue.Count > 0)
            {
                object obj = queue.Dequeue();
                foreach (object extractedObj in ExtractClass(obj.GetType(), obj))
                {
                    if (extractedObj == null)
                        continue;

                    queue.Enqueue(extractedObj);
                }
            }


            imports.Sort(delegate (EbxImportReference a, EbxImportReference b)
            {
                byte[] array = a.FileGuid.ToByteArray();
                byte[] array2 = b.FileGuid.ToByteArray();
                uint num = (uint)((array[0] << 24) | (array[1] << 16) | (array[2] << 8) | array[3]);
                uint num2 = (uint)((array2[0] << 24) | (array2[1] << 16) | (array2[2] << 8) | array2[3]);
                if (num != num2)
                {
                    return num.CompareTo(num2);
                }
                array = a.ClassGuid.ToByteArray();
                array2 = b.ClassGuid.ToByteArray();
                num = (uint)((array[0] << 24) | (array[1] << 16) | (array[2] << 8) | array[3]);
                num2 = (uint)((array2[0] << 24) | (array2[1] << 16) | (array2[2] << 8) | array2[3]);
                return num.CompareTo(num2);
            });
            WriteEbx(fileGuid);
        }

        private IEnumerable<object> ExtractClass(Type type, object obj, bool add = true)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (add)
            {
                if (objsToProcess.Contains(obj))
                {
                    return Enumerable.Empty<object>();
                }
                objsToProcess.Add(obj);
                objs.Add(obj);
            }
            //PropertyInfo[] properties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            Dictionary<PropertyInfo, EbxFieldMetaAttribute> properties = new Dictionary<PropertyInfo, EbxFieldMetaAttribute>();
            foreach (var prp in type.GetProperties())
            {
                var ebxfieldmeta = prp.GetCustomAttribute<EbxFieldMetaAttribute>();
                if (ebxfieldmeta != null)
                {
                    properties.Add(prp, ebxfieldmeta);
                }
            }

            var orderedProps = properties
                .Where(x => x.Key.GetCustomAttribute<IsTransientAttribute>() == null && x.Key.GetCustomAttribute<EbxFieldMetaAttribute>() != null)
                .OrderBy(x => x.Value.Offset)
                //.OrderBy(x => x.Key.GetCustomAttribute<FieldIndexAttribute>().Index)
                .ToArray();


            List<object> dataContainers = new List<object>();
            foreach (PropertyInfo propertyInfo in orderedProps.Select(x=>x.Key))
            {
                if (!flags.HasFlag(EbxWriteFlags.IncludeTransient) && propertyInfo.GetCustomAttribute<IsTransientAttribute>() != null)
                {
                    continue;
                }
                if (propertyInfo.PropertyType == typeof(PointerRef))
                {
                    var prInstanceObj = propertyInfo.GetValue(obj);
                    if (prInstanceObj == null)
                    {
                        WriteErrors.Add("COULD_NOT_WRITE_POINTERREF");
                        continue;
                    }
                    PointerRef pointerRef2 = (PointerRef)prInstanceObj;
                    if (pointerRef2 == default(PointerRef))
                    {
                        WriteErrors.Add("COULD_NOT_WRITE_POINTERREF");
                        continue;
                    }

                    if (pointerRef2.Type == PointerRefType.Internal)
                    {
                        if(pointerRef2.Internal == null)
                        {
                            WriteErrors.Add("COULD_NOT_WRITE_POINTERREF");
                            continue;
                        }

                        dataContainers.Add(pointerRef2.Internal);
                    }
                    else if (pointerRef2.Type == PointerRefType.External && !imports.Contains(pointerRef2.External))
                    {
                        imports.Add(pointerRef2.External);
                    }
                }
                else if (
                    (
                    // Old school "FrostySdk" namespace
                    propertyInfo.PropertyType.Namespace == "FrostySdk.Ebx" 
                    || 
                    // Sdk namespace, used by FIFAEditorTool
                    propertyInfo.PropertyType.Namespace == "Sdk.Ebx"
                    )
                    && propertyInfo.PropertyType.BaseType != typeof(Enum))
                {
                    object value = propertyInfo.GetValue(obj);
                    if (value == null)
                    {
                        WriteErrors.Add("COULD_NOT_WRITE_PROPERTY_OBJECT");
                        continue;
                    }
                    IEnumerable<object> extractedInstances = ExtractClass(value.GetType(), value, add: false);
                    if (extractedInstances == null || extractedInstances.Any(x => x == null))
                    {
                        WriteErrors.Add("COULD_NOT_WRITE_PROPERTY_OBJECT");
                        continue;
                    }
                    dataContainers.AddRange(extractedInstances);
                }
                else
                {
                    if (propertyInfo.PropertyType.Name != "List`1")
                    {
                        continue;
                    }
                    Type propertyType = propertyInfo.PropertyType;
                    IList typedArrayObject = (IList)propertyInfo.GetValue(obj);
                    if (typedArrayObject == null)
                        continue;

                    int arrayCount = typedArrayObject.Count;
                    if (arrayCount <= 0)
                        continue;

                    List<PointerRef> pointerRefArray = typedArrayObject as List<PointerRef>;
                    if (pointerRefArray != null)
                    {
                        for (int i = 0; i < arrayCount; i++)
                        {
                            PointerRef pointerRef = pointerRefArray[i];
                            if (pointerRef.Type == PointerRefType.Internal)
                            {
                                dataContainers.Add(pointerRef.Internal);
                            }
                            else if (pointerRef.Type == PointerRefType.External && !imports.Contains(pointerRef.External))
                            {
                                imports.Add(pointerRef.External);
                            }
                        }
                    }
                    else if (
                        
                        (propertyType.GenericTypeArguments[0].Namespace == "FrostySdk.Ebx" 
                        || propertyType.GenericTypeArguments[0].Namespace == "Sdk.Ebx")

                        && propertyType.GenericTypeArguments[0].BaseType != typeof(Enum))
                    {
                        for (int j = 0; j < arrayCount; j++)
                        {
                            object arrayElement = typedArrayObject[j];
                            dataContainers.AddRange(ExtractClass(arrayElement.GetType(), arrayElement, add: false));
                        }
                    }
                    else
                    {
                        // This is a standard array. Ignore.
                        //throw new NotImplementedException();
                    }
                }
            }
            if (type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
            {
                dataContainers.AddRange(ExtractClass(type.BaseType, obj, add: false));
            }
            return dataContainers;
        }

        private void WriteEbx(Guid fileGuid)
        {
            var distinctObjsToProcess = new HashSet<object>();
            foreach (object item in objsToProcess)
            {
                ProcessClass(item.GetType(), item);
                if (!distinctObjsToProcess.Any(x => x.GetType().Name == item.GetType().Name))
                    distinctObjsToProcess.Add(item);
            }
            for (int j = 0; j < typesToProcess.Count; j++)
            {
                ProcessType(j);
            }
            ProcessData();
            WriteInt32LittleEndian(1179011410); // RIFF
            long riffChunkDataLengthOffset = base.Position;
            WriteUInt32LittleEndian(0u); // RIFF Length
            WriteUInt32LittleEndian(5784133u); // EBX.
            WriteUInt32LittleEndian(1146634821u); // EBXD
            long ebxdChunkDataLengthOffset = base.Position;
            WriteUInt32LittleEndian(0u); // EBXD Length
            WritePadding(16);
            payloadPosition = base.Position;
            WriteBytes(mainData);
            long ebxdChunkLength = base.Position - ebxdChunkDataLengthOffset - 4;
            WritePadding(2);
            WriteUInt32LittleEndian(1481197125u); // EFIX
            long efixChunkDataLengthOffset = base.Position;
            WriteUInt32LittleEndian(0u); // EFIX Length
            WriteGuid(fileGuid);
            WriteInt32LittleEndian(classGuids.Count);
            //WriteInt32LittleEndian(uniqueTypes.Count);
            _ = classGuids;
            _ = uniqueTypes;
            var uniqueTypeGuids = uniqueTypes.Select(x => x.GetCustomAttribute<GuidAttribute>().Guid);
            List<int> usedIndex = new List<int>();
            foreach (Guid classGuid in classGuids)
            //foreach (Guid classGuid in uniqueTypes.Select(x => x.GetCustomAttribute<GuidAttribute>().Guid))
            {
                WriteGuid(classGuid);
                usedIndex.Add(classGuids.FindIndex(x => x == classGuid));
            }
            WriteInt32LittleEndian(typeInfoSignatures.Count);
            //WriteInt32LittleEndian(uniqueTypes.Count);
            foreach (uint signature in typeInfoSignatures)
            //foreach (var iSig in usedIndex)
            {
                WriteUInt32LittleEndian(signature);
                //WriteUInt32LittleEndian(typeInfoSignatures[iSig]);
            }
            WriteInt32LittleEndian(exportedCount);
            WriteInt32LittleEndian(instances.Count);
            for (int i = 0; i < instances.Count; i++)
            {
                WriteInt32LittleEndian(dataContainerOffsets[i]);
            }
            patchedPointerOffsets.Sort();
            WriteInt32LittleEndian(patchedPointerOffsets.Count);
            foreach (int pointerOffset in patchedPointerOffsets)
            {
                WriteInt32LittleEndian(pointerOffset);
            }
            WriteInt32LittleEndian(patchedResourceRefOffsets.Count);
            foreach (int resourceRefOffset in patchedResourceRefOffsets)
            {
                WriteInt32LittleEndian(resourceRefOffset);
            }
            WriteInt32LittleEndian(imports.Count);
            foreach (EbxImportReference import in imports)
            {
                WriteGuid(import.FileGuid);
                WriteGuid(import.ClassGuid);
            }
            WriteInt32LittleEndian(patchedImportOffsets.Count);
            foreach (int importOffset in patchedImportOffsets)
            {
                WriteInt32LittleEndian(importOffset);
            }
            WriteInt32LittleEndian(0);
            WriteInt32LittleEndian(arraysPosition);
            WriteInt32LittleEndian(boxedValuesPosition);
            WriteInt32LittleEndian(stringTablePosition);
            long efixChunkLength = base.Position - efixChunkDataLengthOffset;// - 4;

            Write(0u); // Seems to be some padding before EBXX :\

            WriteUInt32LittleEndian(1482179141u); // EBXX
            long ebxxChunkLengthOffset = base.Position;
            uint ebxxChunkLength = 8;
            Write(ebxxChunkLength);

            // -----------------------------------
            // Compare to Original? EBXX Stuff
            //
            List<EbxArray> origEbxArrays = new List<EbxArray>();
            List<EbxBoxedValue> origEbxBoxedValues = new List<EbxBoxedValue>();
            if (stringsToCStringOffsets.Any())
            {
                var oldEntry = AssetManager.Instance.GetEbxEntry(stringsToCStringOffsets.Keys.First());
                if (oldEntry != null)
                {
                    var oldEbxStream = AssetManager.Instance.GetEbxStream(oldEntry);
                    using (var reader = EbxReader.GetEbxReader(oldEbxStream, true)) // new EbxReader22B(oldEbxStream, true))
                    {
                        origEbxArrays.AddRange(reader.arrays.Where(x => x.Count > 0).OrderBy(x => x.Offset));
                        origEbxBoxedValues.AddRange(reader.boxedValues.Where(x => x.Offset > 0).OrderBy(x => x.Offset));
                    }
                }
            }
            var ebxxArrays = 
                arrays
                .Where(x => x.Count > 0)
                .OrderBy(x => x.Offset).ToArray();
            var ebxxArrayCount = ebxxArrays.Count();

            var ebxxBoxedValues = 
                boxedValues
                .Where(x => x.Offset > 0)
                .OrderBy(x => x.Offset).ToArray();
            var ebxxBoxedValueCount = ebxxBoxedValues.Count();
            
            Write(ebxxArrayCount);
            Write(ebxxBoxedValueCount);
            for (var iEbxxArray = 0; iEbxxArray < ebxxArrayCount; iEbxxArray++)
            {
                var arr = ebxxArrays[iEbxxArray];
                if (origEbxArrays.Count > iEbxxArray)
                {
                    Write(arr.Offset);
                    Write(arr.Count);
                    Write(origEbxArrays[iEbxxArray].PathDepth);
                    Write((ushort)origEbxArrays[iEbxxArray].TypeFlags);
                    Write((ushort)origEbxArrays[iEbxxArray].ClassRef);
                }
                else
                {
                    Write(0u);
                    Write(0u);
                    Write(0u);
                    WriteUInt16(0, Endian.Little);
                    WriteUInt16(0, Endian.Little);
                }
            }
            for (var iEbxBoxedValue = 0; iEbxBoxedValue < ebxxBoxedValueCount; iEbxBoxedValue++)
            {
                var boxedValue = ebxxBoxedValues[iEbxBoxedValue];
                Write(boxedValue.Offset);
                Write(1);
                Write(0x00);
                Write((ushort)origEbxBoxedValues[iEbxBoxedValue].Type);
                Write((ushort)boxedValue.ClassRef);
            }
            ebxxChunkLength = (uint)base.Position - (uint)ebxxChunkLengthOffset - 4u;
            // ----------------------------------

            long riffChunkLength = base.Position - riffChunkDataLengthOffset - 4;
            base.Position = riffChunkDataLengthOffset;
            WriteUInt32LittleEndian((uint)riffChunkLength);
            base.Position = ebxdChunkDataLengthOffset;
            WriteUInt32LittleEndian((uint)ebxdChunkLength);
            base.Position = efixChunkDataLengthOffset;
            WriteUInt32LittleEndian((uint)efixChunkLength);
            base.Position = ebxxChunkLengthOffset;
            Write(ebxxChunkLength);
        }

        private ushort ProcessClass(Type type, object obj, bool isBaseType = false)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (obj == null)
            {
                return 0;
            }
            if (type.BaseType!.Namespace == "FrostySdk.Ebx" || type.BaseType!.Namespace == "Sdk.Ebx")
            {
                ProcessClass(type.BaseType, obj, isBaseType: true);
            }
            int classIndex = FindExistingClass(type);
            if (classIndex != -1)
            {
                return (ushort)classIndex;
            }
            // Class Meta
            var classMeta = type.GetCustomAttribute<EbxClassMetaAttribute>();

            // Obtain and Order the Properties
            PropertyInfo[] properties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            List<PropertyInfo> propertiesToInclude = new List<PropertyInfo>();
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.GetCustomAttribute<IsTransientAttribute>() == null || flags.HasFlag(EbxWriteFlags.IncludeTransient))
                {
                    propertiesToInclude.Add(propertyInfo);
                }
            }
            propertiesToInclude = properties.OrderBy(x => x.GetCustomAttribute<EbxFieldMetaAttribute>()?.Offset).ToList();
            if (!isBaseType)
            {
                if (classMeta.Type == EbxFieldType.Pointer)
                    classIndex = AddClass(type.Name, type);
            }
            if (type.IsEnum)
                return (ushort)classIndex;

            foreach (PropertyInfo item in propertiesToInclude)
            {
                EbxFieldMetaAttribute ebxFieldMetaAttribute = item.GetCustomAttribute<EbxFieldMetaAttribute>();
                switch (ebxFieldMetaAttribute.Type)
                {
                    case EbxFieldType.Struct:
                        ProcessClass(item.PropertyType, item.GetValue(obj), isBaseType);
                        //ProcessClass(item.PropertyType, item.GetValue(obj), true);
                        break;
                    case EbxFieldType.Array:
                        {
                            Type elementType = item.PropertyType.GenericTypeArguments[0];

                            var genericTypeDef = item.PropertyType.GetGenericTypeDefinition();
                            var genericArgs = item.PropertyType.GetGenericArguments();
                            Type constructed = genericTypeDef.MakeGenericType(genericArgs);

                            if (FindExistingClass(elementType) != -1)
                                break;
                            
                            if (obj == null)
                                break;

                            var arrayValue = item.GetValue(obj);
                            

                            if (arrayValue == null)
                                arrayValue = Activator.CreateInstance(constructed);

                            IList typedArray = (IList)arrayValue;
                            if (typedArray.Count == 0)
                            {
                                break;
                            }
                            arrayTypes.Add(ebxFieldMetaAttribute);
                            if (ebxFieldMetaAttribute.ArrayType != EbxFieldType.Struct)
                            {
                                break;
                            }
                            foreach (object arrayItem in typedArray)
                            {
                                ProcessClass(elementType, arrayItem, isBaseType);
                            }
                            break;
                        }
                }
            }
            return (ushort)classIndex;
        }

        private void ProcessType(int index)
        {
            Type type = typesToProcess[index];
            //EbxClass ebxClass = classTypes[index];
            var classMetaAttribute = type.GetCustomAttribute<EbxClassMetaAttribute>();
            if (
                (type.Name == "List`1" || classMetaAttribute.Type == EbxFieldType.Array)
                && (arrayTypes.Count > 0)
                )
            {

                EbxFieldMetaAttribute ebxFieldMetaAttribute = arrayTypes[0];
                arrayTypes.RemoveAt(0);
                ushort classIndex = 0;
                if (type.GenericTypeArguments.Length > 0)
                {
                    classIndex = (ushort)FindExistingClass(type.GenericTypeArguments[0]);
                }
                else
                {
                    classIndex = (ushort)FindExistingClass(type);
                }
                if (classIndex == ushort.MaxValue)
                {
                    classIndex = 0;
                }
                AddField("member", ebxFieldMetaAttribute.ArrayFlags, classIndex, 0u, 0u);
                return;
            }
            if (classMetaAttribute.Type == EbxFieldType.Enum)
            {
                string[] enumNames = type.GetEnumNames();
                Array enumValues = type.GetEnumValues();
                for (int i = 0; i < enumNames.Length; i++)
                {
                    int enumValue = (int)enumValues.GetValue(i);
                    AddField(enumNames[i], 0, 0, (uint)enumValue, (uint)enumValue);
                }
                return;
            }
            if (type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
            {
                ushort classRef = (ushort)FindExistingClass(type.BaseType);
                AddField("$", 0, classRef, 8u, 0u);
            }
            foreach (PropertyInfo item in from property in type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                                          where (flags.HasFlag(EbxWriteFlags.IncludeTransient) || property.GetCustomAttribute<IsTransientAttribute>() == null) && !property.Name.Equals("__InstanceGuid", StringComparison.Ordinal)
                                          select property)
            {
                ProcessField(item);
            }
        }

        private void ProcessField(PropertyInfo pi)
        {
            if (pi == null)
            {
                throw new ArgumentNullException("pi");
            }
            EbxFieldMetaAttribute ebxFieldMetaAttribute = pi.GetCustomAttribute<EbxFieldMetaAttribute>();
            Type propType = pi.PropertyType;
            ushort classIndex = (ushort)typesToProcess.FindIndex((Type value) => value == propType);
            if (classIndex == ushort.MaxValue)
            {
                classIndex = 0;
            }
            AddField(pi.Name, ebxFieldMetaAttribute.Flags, classIndex, ebxFieldMetaAttribute.Offset, 0u);
        }

        protected virtual void ProcessData()
        {
            List<object> exportedInstances = new List<object>();
            List<object> nonExportedInstances = new List<object>();
            foreach (dynamic item in objs)
            {
                if (((AssetClassGuid)item.GetInstanceGuid()).IsExported)
                {
                    exportedInstances.Add(item);
                }
                else
                {
                    nonExportedInstances.Add(item);
                }
            }
            object primaryInstance = exportedInstances[0];
            exportedInstances.RemoveAt(0);
            //exportedInstances.Sort(delegate (dynamic a, dynamic b)
            //{
            //    AssetClassGuid assetClassGuid2 = a.GetInstanceGuid();
            //    AssetClassGuid assetClassGuid3 = b.GetInstanceGuid();
            //    byte[] array = assetClassGuid2.ExportedGuid.ToByteArray();
            //    byte[] array2 = assetClassGuid3.ExportedGuid.ToByteArray();
            //    uint num = (uint)((array[0] << 24) | (array[1] << 16) | (array[2] << 8) | array[3]);
            //    uint value3 = (uint)((array2[0] << 24) | (array2[1] << 16) | (array2[2] << 8) | array2[3]);
            //    return num.CompareTo(value3);
            //});
            nonExportedInstances.Sort((object a, object b) => string.CompareOrdinal(a.GetType().Name, b.GetType().Name));
            sortedObjs.Add(primaryInstance);
            sortedObjs.AddRange(exportedInstances);
            sortedObjs.AddRange(nonExportedInstances);
            MemoryStream memoryStream = new MemoryStream();
            _ = base.Position;
            FileWriter nativeWriter = new FileWriter(memoryStream);
            for (int dataContainerIndex = 0; dataContainerIndex < sortedObjs.Count; dataContainerIndex++)
            {
                AssetClassGuid assetClassGuid = ((dynamic)sortedObjs[dataContainerIndex]).GetInstanceGuid();
                Type type = sortedObjs[dataContainerIndex].GetType();
                var ebxClassMeta = type.GetCustomAttribute<EbxClassMetaAttribute>();
                int classIndex = FindExistingClass(type);
                //EbxClass ebxClass = classTypes[classIndex];
                if (!uniqueTypes.Contains(type))
                {
                    uniqueTypes.Add(type);
                }
                nativeWriter.WritePadding(ebxClassMeta.Alignment);
                if (assetClassGuid.IsExported)
                {
                    nativeWriter.WriteGuid(assetClassGuid.ExportedGuid);
                }

                dataContainerOffsets.Add((int)nativeWriter.Position);
                nativeWriter.Write((ulong)classIndex);
                //nativeWriter.WritePadding(8);
                if (ebxClassMeta.Alignment != 4)
                {
                    nativeWriter.WriteUInt64LittleEndian(0uL);
                }
                nativeWriter.WriteUInt32LittleEndian(2u);

                var uFlagExported = assetClassGuid.IsExported ? 45312u : 40960u;
                nativeWriter.Write(uFlagExported);

                WriteClass(sortedObjs[dataContainerIndex], type, nativeWriter, dataContainerIndex);
                EbxInstance ebxInstance = default(EbxInstance);
                ebxInstance.ClassRef = (ushort)classIndex;
                ebxInstance.Count = 1;
                ebxInstance.IsExported = assetClassGuid.IsExported;
                instances.Add(ebxInstance);
                exportedCount += (ushort)(ebxInstance.IsExported ? 1 : 0);
            }

            ProcessAndWriteDataArrayOutput(nativeWriter);
            nativeWriter.WritePadding(16);
            ProcessDataBoxedValues(nativeWriter);
            nativeWriter.WritePadding(16);

            stringTablePosition = (int)nativeWriter.Position;
            foreach (KeyValuePair<string, List<(int, int)>> stringsToCStringOffset in stringsToCStringOffsets)
            {
                string stringValue = stringsToCStringOffset.Key;
                List<(int, int)> list = stringsToCStringOffset.Value;
                long stringPosition = nativeWriter.Position;
                nativeWriter.WriteNullTerminatedString(stringValue);
                long afterStringPosition = nativeWriter.Position;
                foreach (var (cStringOffset, containingArrayIndex5) in list)
                {
                    if (containingArrayIndex5 == -1)
                    {
                        nativeWriter.Position = cStringOffset;
                    }
                    else
                    {
                        int realArrayIndex6 = arrayIndicesMap[containingArrayIndex5];
                        nativeWriter.Position = cStringOffset + arrays[realArrayIndex6].Offset;
                    }
                    int offset = (int)(stringPosition - nativeWriter.Position);
                    nativeWriter.WriteInt32LittleEndian(offset);
                }
                nativeWriter.Position = afterStringPosition;
            }
            var unpatchedArrayIndex = 0;
            var orderedArrays = unpatchedArrayInfo.OrderBy(x => x.property.GetCustomAttribute<FieldIndexAttribute>()?.Index);
            var orderedArrayNames = orderedArrays.Select(x=>x.property.Name).ToArray();
            foreach (var unpatchedArray in orderedArrays)
            //foreach (var unpatchedArray in unpatchedArrayInfo)
            {
                EbxArray ebxArray = arrays[unpatchedArray.arrayIndex];
                var (arrayPointerOffset, _, _, _, _) = unpatchedArray;
                if (unpatchedArray.containingArrayIndex > -1)
                {
                    int realArrayIndex5 = arrayIndicesMap[unpatchedArray.containingArrayIndex];
                    nativeWriter.Position = arrays[realArrayIndex5].Offset + arrayPointerOffset;
                }
                else
                {
                    nativeWriter.Position = arrayPointerOffset;
                }

                //int relativeOffset = (int)(ebxArray.Offset - 56);

                //nativeWriter.WriteInt32LittleEndian(relativeOffset);

                // first array is relative - container offset?
                if (unpatchedArrayIndex == 0)
                    nativeWriter.WriteInt32LittleEndian((int)(ebxArray.Offset - (int)nativeWriter.Position));
                // rest of the array is by the offset
                else
                    nativeWriter.WriteInt32LittleEndian((int)ebxArray.Offset - (int)nativeWriter.Position);

                unpatchedArrayIndex++;
            }
            foreach (KeyValuePair<(int, int), int> item2 in pointerRefPositionToDataContainerIndex)
            {
                item2.Deconstruct(out var key2, out var value2);
                (int, int) tuple3 = key2;
                int pointerRefPosition = tuple3.Item1;
                int containingArrayIndex4 = tuple3.Item2;
                int dataContainerIndex = value2;
                if (containingArrayIndex4 == -1)
                {
                    nativeWriter.Position = pointerRefPosition;
                }
                else
                {
                    int realArrayIndex4 = arrayIndicesMap[containingArrayIndex4];
                    nativeWriter.Position = pointerRefPosition + arrays[realArrayIndex4].Offset;
                }
                int relativeOffset = (int)(dataContainerOffsets[dataContainerIndex] - nativeWriter.Position);

                nativeWriter.WriteInt32LittleEndian(relativeOffset);
            }
            foreach (var (pointerOffset, containingArrayIndex3) in pointerOffsets)
            {
                if (containingArrayIndex3 == -1)
                {
                    patchedPointerOffsets.Add(pointerOffset);
                    continue;
                }
                int realArrayIndex3 = arrayIndicesMap[containingArrayIndex3];

                patchedPointerOffsets.Add((int)(pointerOffset + arrays[realArrayIndex3].Offset));
            }
            foreach (var (resourceRefOffset, containingArrayIndex2) in resourceRefOffsets)
            {
                if (containingArrayIndex2 == -1)
                {
                    patchedResourceRefOffsets.Add(resourceRefOffset);
                    continue;
                }
                int realArrayIndex2 = arrayIndicesMap[containingArrayIndex2];
                patchedResourceRefOffsets.Add((int)(resourceRefOffset + arrays[realArrayIndex2].Offset));
            }
            foreach (var (importOffset, containingArrayIndex) in importOffsets)
            {
                if (containingArrayIndex == -1)
                {
                    patchedImportOffsets.Add(importOffset);
                    continue;
                }
                int realArrayIndex = arrayIndicesMap[containingArrayIndex];

#if DEBUG
                if (arrays[realArrayIndex].Offset == 10564)
                {

                }
#endif

                patchedImportOffsets.Add((int)(importOffset + arrays[realArrayIndex].Offset));
            }
            mainData = memoryStream.ToArray();
            uniqueClassCount = (ushort)uniqueTypes.Count;
        }

        protected void ProcessAndWriteDataArrayOutput(FileWriter nativeWriter)
        {
            nativeWriter.WritePadding(16);

            // When checking this in HXD, remember real position is Position + 32
            arraysPosition = (int)nativeWriter.Position;
            nativeWriter.Position = arraysPosition;

            var realPosition = arraysPosition + 32;

            nativeWriter.WriteEmpty(32);

            realPosition = (int)nativeWriter.Position + 32;

            _ = this.arrays;
            _ = this.objs;
            _ = classGuids;

            // If the arrays are empty, return
            if (unpatchedArrayInfo.Count == 0 || arrayData.Count == 0)
                return;

            var orderedArrays = unpatchedArrayInfo.OrderBy(x => x.property.GetCustomAttribute<FieldIndexAttribute>()?.Index);
            var orderedArrayNames = orderedArrays.Select(x => x.property.Name).ToArray();
            foreach (var unpatchedArray in orderedArrays)
            {
                EbxArray arrayInfo = arrays[unpatchedArray.arrayIndex];
                byte[] arrayData = this.arrayData[unpatchedArray.arrayIndex];
                if (arrayInfo.Count == 0)
                {
                    uint offsetFromPayload = (arrayInfo.Offset = (uint)(arraysPosition + 16));
                }
                else
                {
                    long beforePaddingPosition = nativeWriter.Position;


                    nativeWriter.WritePadding(16);

                    nativeWriter.WriteUInt32LittleEndian(arrayInfo.Count);
                    int beforeArrayPosition = (int)nativeWriter.Position;

                    nativeWriter.WriteBytes(arrayData);
                    arrayInfo.Offset = (uint)beforeArrayPosition;
                    arrays[unpatchedArray.arrayIndex] = arrayInfo;
                }
                arrays[unpatchedArray.arrayIndex] = arrayInfo;
            }
        }


        private void ProcessDataBoxedValues(FileWriter nativeWriter)
        {
            nativeWriter.WritePadding(16);
            boxedValuesPosition = (int)nativeWriter.Position;

            for (int i = 0; i < boxedValues.Count; i++)
            {
                EbxBoxedValue boxedValue = boxedValues[i];
                if (boxedValue.Data == null)
                    continue;

                byte alignment = 4;
                if (boxedValue.ClassRef == 0xFFFF)
                {
                    EbxFieldType boxedValType = (EbxFieldType)((boxedValue.Type >> 5) & 0x1F);
                    switch (boxedValType)
                    {
                        case EbxFieldType.Boolean:
                            alignment = 1; break;
                        case EbxFieldType.Int8:
                        case EbxFieldType.UInt8:
                        case EbxFieldType.Int16:
                        case EbxFieldType.UInt16:
                            alignment = 2; break;
                        case EbxFieldType.Enum:
                        case EbxFieldType.TypeRef:
                        case EbxFieldType.String:
                        case EbxFieldType.Float32:
                        case EbxFieldType.Int32:
                        case EbxFieldType.UInt32:
                            alignment = 4; break;

                        case EbxFieldType.Float64:
                        case EbxFieldType.Int64:
                        case EbxFieldType.UInt64:
                        case EbxFieldType.CString:
                        case EbxFieldType.FileRef:
                        case EbxFieldType.Delegate:
                        case EbxFieldType.Pointer:
                        case EbxFieldType.ResourceRef:
                        case EbxFieldType.BoxedValueRef:
                            alignment = 8; break;
                        //case EbxFieldType.Guid: alignment = 16; break;

                        default: alignment = 4; break;
                    }
                }
                else
                {
                    //EbxClass boxedValClass = m_classTypes[boxedValue.ClassRef];
                    //switch (boxedValClass.DebugCategory)
                    //{
                    //    case EbxFieldCategory.EnumType:
                    //        alignment = 4; break;
                    //    case EbxFieldCategory.Pointer:
                    //    case EbxFieldCategory.ArrayType:
                    //    case EbxFieldCategory.DelegateType:
                    //        alignment = 8; break;
                    //    default: alignment = boxedValClass.Alignment; break;
                    //}
                }
                nativeWriter.WritePadding(alignment);
                boxedValue.Offset = (uint)nativeWriter.Position;
                boxedValues[i] = boxedValue;

                nativeWriter.Write(boxedValue.Data);
            }

            //nativeWriter.WritePadding(16);
        }


        protected virtual void WriteClass(object obj, Type objType, NativeWriter writer, int dataContainerIndex)
        {
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
            if (
                objType.BaseType!.Namespace == "FrostySdk.Ebx"
                || objType.BaseType!.Namespace == "Sdk.Ebx"
                || objType.BaseType!.Namespace == "FMTSdk.Ebx"
                )
            {
                WriteClass(obj, objType.BaseType, writer, dataContainerIndex);
            }

            PropertyInfo[] properties = objType
                .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
#if DEBUG
            var writtenProperties = new List<PropertyInfo>();
#endif

            var classMeta = objType.GetCustomAttribute<EbxClassMetaAttribute>();
            var propertiesOrderedByOffset = properties.OrderBy(x => x.GetCustomAttribute<EbxFieldMetaAttribute>().Offset).ToArray();
            var propertiesOrderedByIndex = properties.OrderBy(x => x.GetCustomAttribute<FieldIndexAttribute>().Index).ToArray();

            foreach (var propertyInfo in propertiesOrderedByOffset)
            //foreach (var propertyInfo in propertiesOrderedByIndex)
            {
                IsTransientAttribute isTransientAttribute = propertyInfo.GetCustomAttribute<IsTransientAttribute>();
                if (isTransientAttribute != null)
                {
#if DEBUG
                    writtenProperties.Add(propertyInfo);
#endif
                    continue;
                }

                EbxFieldMetaAttribute ebxFieldMetaAttribute = propertyInfo.GetCustomAttribute<EbxFieldMetaAttribute>();
                if (ebxFieldMetaAttribute == null || ebxFieldMetaAttribute.Type == EbxFieldType.Inherited)
                {
#if DEBUG
                    writtenProperties.Add(propertyInfo);
#endif
                    continue;
                }

                if (propertyInfo == null)
                {
#if DEBUG
                    Debug.WriteLine("There is a dodgy Property in here. How can there be a null property info in a list of property infos?");
                    FileLogger.WriteLine("There is a dodgy Property in here. How can there be a null property info in a list of property infos?");
#endif
                    continue;
                }
                else
                {
#if DEBUG
                    writtenProperties.Add(propertyInfo);

                    if (propertyInfo.Name == "SHOT_ShotSpeedCoe")
                    {

                    }
#endif
                    bool isReference = propertyInfo.GetCustomAttribute<IsReferenceAttribute>() != null;
                    if (ebxFieldMetaAttribute.IsArray)
                    {
                        uint fieldNameHash = propertyInfo.GetCustomAttribute<HashAttribute>()!.Hash;
                        //WriteArray(propertyInfo.GetValue(obj), ebxFieldMetaAttribute.ArrayType, fieldNameHash, classMeta.Alignment, writer, isReference);
                        WriteArray(propertyInfo.GetValue(obj), ebxFieldMetaAttribute, fieldNameHash, classMeta.Alignment, writer, isReference, propertyInfo);
                    }
                    else
                    {
                        WriteField(propertyInfo.GetValue(obj), ebxFieldMetaAttribute.Type, classMeta.Alignment, writer, isReference, dataContainerIndex);
                    }
                }
            }
#if DEBUG
            var unwrittenProperties = properties.Where(x => !writtenProperties.Any(y => y.Name == x.Name));
            if (unwrittenProperties.Any() && obj == objsToProcess[0])
            {
                throw new Exception("Some properties were not written");
            }
#endif

            writer.WritePadding(classMeta.Alignment);

        }

        protected void WriteField(object obj, EbxFieldType ebxType, byte classAlignment, NativeWriter writer, bool isReference, int dataContainerIndex)
        {
            switch (ebxType)
            {
                case EbxFieldType.TypeRef:
                    TypeRef typeRefObj = (TypeRef)obj;
                    WriteTypeRef(typeRefObj, false, writer);
                    break;
                case EbxFieldType.BoxedValueRef:
                    {
                        BoxedValueRef value = (BoxedValueRef)obj;
                        TypeRef typeRef = new TypeRef(value.TypeString);
                        TypeRefOutputStruct tiPair = WriteTypeRef(typeRef, true, writer);

                        int index = boxedValues.Count;
                        EbxBoxedValue boxedValue = new EbxBoxedValue()
                        {
                            Offset = 0,
                            Type = tiPair.Type,
                            ClassRef = tiPair.ClassRef
                        };
                        if (value.Value != null)
                        {
                            boxedValue.Data = WriteBoxedValueRef(value);
                        }
                        else
                        {
                            boxedValue.Data = null;
                        }
                        boxedValues.Add(boxedValue);

                        writer.Write((ulong)index);
                    }
                    break;
                case EbxFieldType.FileRef:
                    throw new NotSupportedException("The asset contains a FileRef field, which is not yet supported.");
                case EbxFieldType.CString:
                    pointerOffsets.Add(((int)writer.Position, currentArrayIndex));
                    AddString((CString)obj, (int)writer.Position);
                    writer.WriteUInt64LittleEndian(0uL);
                    break;
                case EbxFieldType.Pointer:
                    {
                        PointerRef pointer = (PointerRef)obj;
                        int pointerRefValue = 0;
                        if (pointer.Type == PointerRefType.External)
                        {
                            int importIndex = imports.FindIndex((EbxImportReference value) => value == pointer.External);
                            if (importIndex == -1)
                            {
                                throw new IndexOutOfRangeException("");
                            }
                            pointerRefValue = (importIndex << 1) | 1;
                            if (isReference && !dependencies.Contains(imports[importIndex].FileGuid))
                            {
                                dependencies.Add(imports[importIndex].FileGuid);
                            }
                            importOffsets.Add(((int)writer.Position, currentArrayIndex));
                        }
                        else if (pointer.Type == PointerRefType.Internal)
                        {
                            if (pointer.Internal == null)
                            {
                                Debug.WriteLine($"Error writing Pointer {obj} in {objs[0].GetType().FullName}");
                                WriteErrors.Add("COULD_NOT_WRITE_PROPERTY_POINTERREF_INTERNAL_IS_NULL");
                                break;
                            }

                            pointerRefValue = sortedObjs.FindIndex((object value) => value == pointer.Internal);
                            if (pointerRefValue == -1)
                            {
                                throw new IndexOutOfRangeException("");
                            }
                            pointerRefPositionToDataContainerIndex[((int)writer.Position, currentArrayIndex)] = pointerRefValue;
                            pointerOffsets.Add(((int)writer.Position, currentArrayIndex));
                        }
                        writer.WriteUInt64LittleEndian((uint)pointerRefValue);
                        break;
                    }
                case EbxFieldType.Struct:
                    {
                        Type structObjType = obj.GetType();
                        int existingClassIndex = FindExistingClass(structObjType);
                        byte alignment = ((existingClassIndex == -1) ? structObjType.GetCustomAttribute<EbxClassMetaAttribute>()!.Alignment : (byte)4);
                        writer.WritePadding(alignment);
                        WriteClass(obj, structObjType, writer, dataContainerIndex);
                        break;
                    }
                case EbxFieldType.Enum:
                    writer.WriteInt32LittleEndian((int)obj);
                    break;
                case EbxFieldType.Float32:
                    writer.WriteSingleLittleEndian((float)obj);
                    break;
                case EbxFieldType.Float64:
                    writer.WriteDoubleLittleEndian((double)obj);
                    break;
                case EbxFieldType.Boolean:
                    writer.Write((byte)(((bool)obj) ? 1u : 0u));
                    break;
                case EbxFieldType.Int8:
                    writer.Write((sbyte)obj);
                    break;
                case EbxFieldType.UInt8:
                    writer.Write((byte)obj);
                    break;
                case EbxFieldType.Int16:
                    writer.WriteInt16LittleEndian((short)obj);
                    break;
                case EbxFieldType.UInt16:
                    writer.WriteUInt16LittleEndian((ushort)obj);
                    break;
                case EbxFieldType.Int32:
                    writer.WriteInt32LittleEndian((int)obj);
                    break;
                case EbxFieldType.UInt32:
                    writer.WriteUInt32LittleEndian((uint)obj);
                    break;
                case EbxFieldType.Int64:
                    writer.WriteInt64LittleEndian((long)obj);
                    break;
                case EbxFieldType.UInt64:
                    writer.WriteUInt64LittleEndian((ulong)obj);
                    break;
                case EbxFieldType.Guid:
                    writer.WriteGuid((Guid)obj);
                    break;
                case EbxFieldType.Sha1:
                    writer.Write((Sha1)obj);
                    break;
                case EbxFieldType.String:
                    writer.WriteFixedSizedString((string)obj, 32);
                    break;
                case EbxFieldType.ResourceRef:
                    resourceRefOffsets.Add(((int)writer.Position, currentArrayIndex));
                    writer.WriteUInt64LittleEndian((ResourceRef)obj);
                    break;
              
                //case EbxFieldType.Delegate:
                //	throw new NotSupportedException("The asset contains a Delegate field, which is not yet supported.");
                //case EbxFieldType.Function:
                //	throw new NotSupportedException("The asset contains a Function field, which is not yet supported.");
                default:
                    throw new InvalidDataException($"Unknown field type {ebxType}");
            }
        }

        //protected void WriteArray(object obj, EbxFieldType elementFieldType, uint fieldNameHash, byte classAlignment, NativeWriter writer, bool isReference)
        protected void WriteArray(object arrayPropertyValue, EbxFieldMetaAttribute fieldMetaAttribute, uint fieldNameHash, byte classAlignment, NativeWriter mainWriter, bool isReference, PropertyInfo property, int dataContainerIndex = 0)
        {
            int classIndex = typesToProcess.FindIndex((Type item) => item == arrayPropertyValue.GetType().GetGenericArguments()[0]);
            if (classIndex == -1)
            {
                classIndex = 65535;
            }
            IList typedObj = (IList)arrayPropertyValue;
            int arrayCount = typedObj.Count;
            MemoryStream arrayMemoryStream = new MemoryStream();
            using FileWriter arrayWriter = new FileWriter(arrayMemoryStream);
            int previousArrayIndex = currentArrayIndex;
            int localArrayIndex = (currentArrayIndex = arrayIndicesMap.Count);
            arrayIndicesMap.Add(-1);
            currentArrayDepth++;
            long pointerPosition = mainWriter.Position;
            mainWriter.WriteInt32LittleEndian(0);
            mainWriter.WritePadding(8);
            for (int i = 0; i < arrayCount; i++)
            {
                bool has8bitAlignment;
                switch (fieldMetaAttribute.Type)
                {
                    case EbxFieldType.Pointer:
                    case EbxFieldType.CString:
                    case EbxFieldType.FileRef:
                    case EbxFieldType.Int64:
                    case EbxFieldType.UInt64:
                    case EbxFieldType.Float64:
                    case EbxFieldType.ResourceRef:
                    case EbxFieldType.TypeRef:
                    case EbxFieldType.BoxedValueRef:
                        has8bitAlignment = true;
                        break;
                    default:
                        has8bitAlignment = false;
                        break;
                }
                if (has8bitAlignment)
                {
                    arrayWriter.WritePadding(8);
                }
                else if (fieldMetaAttribute.Type == EbxFieldType.Array)
                {
                    arrayWriter.WritePadding(4);
                }
                object arrayElementObj = typedObj[i];
                WriteField(arrayElementObj, fieldMetaAttribute.ArrayType, classAlignment, arrayWriter, isReference, dataContainerIndex);
            }
            int arrayIndex = arrays.Count;
            arrays.Add(new EbxArray
            {
                Count = (uint)arrayCount,
                ClassRef = classIndex,
                PathDepth = fieldNameHash,
                TypeFlags = fieldMetaAttribute.ArrayFlags,
                Offset = 0u
            });
            arrayIndicesMap[localArrayIndex] = arrayIndex;
            unpatchedArrayInfo.Add(((int)pointerPosition, arrayIndex, previousArrayIndex, dataContainerIndex, property));
            pointerOffsets.Add(((int)pointerPosition, previousArrayIndex));
            arrayData.Add(arrayMemoryStream.ToArray());
            currentArrayDepth--;
            if (currentArrayDepth == 0)
            {
                currentArrayIndex = -1;
            }
            else
            {
                currentArrayIndex = previousArrayIndex;
            }
        }

        protected void AddString(string stringToAdd, int offset)
        {
            if (!stringsToCStringOffsets.TryGetValue(stringToAdd ?? string.Empty, out var offsets))
            {
                offsets = (stringsToCStringOffsets[stringToAdd ?? string.Empty] = new List<(int, int)>());
            }
            offsets.Add((offset, currentArrayIndex));
        }

        //private int AddClass(PropertyInfo pi, Type classType)
        //{
        //	GuidAttribute guidAttribute = pi.GetCustomAttribute<GuidAttribute>();
        //	Guid classGuid;
        //	EbxClass @class;
        //	if (guidAttribute != null)
        //	{
        //		classGuid = guidAttribute.Guid;
        //		@class = GetClass(classGuid);
        //	}
        //	else
        //	{
        //		EbxFieldMetaAttribute fieldMetaAttribute = pi.GetCustomAttribute<EbxFieldMetaAttribute>();
        //		@class = GetClass(fieldMetaAttribute.BaseType);
        //		classGuid = fieldMetaAttribute.BaseType.GetCustomAttribute<GuidAttribute>()!.Guid;
        //	}
        //	classTypes.Add(@class);
        //	typesToProcess.Add(classType);
        //	classGuids.Add(classGuid);
        //	return classTypes.Count - 1;
        //}

        private int AddClass(string name, Type classType)
        {
            Guid classGuid = classType.GetCustomAttribute<GuidAttribute>()!.Guid;
            classGuids.Add(classGuid);
            //EbxClass @class = GetClass(classType);
            //classTypes.Add(@class);
            ClassTypeCount++;
            Span<byte> typeInfoGuidBytes = stackalloc byte[16];
            var typeInfo = classType.GetCustomAttributes<TypeInfoGuidAttribute>().LastOrDefault();
            //GetTypeInfoGuid(@class).TryWriteBytes(typeInfoGuidBytes);
            typeInfo.Guid.TryWriteBytes(typeInfoGuidBytes);
            //List<uint> list = typeInfoSignatures;
            Span<byte> span = typeInfoGuidBytes;
            typeInfoSignatures.Add(BinaryPrimitives.ReadUInt32LittleEndian(span[12..]));
            AddTypeName(name);
            typesToProcess.Add(classType);
            //return classTypes.Count - 1;
            return ClassTypeCount - 1;
            //return typesToProcess.Count - 1;
        }

        private void AddField(string name, ushort type, ushort classRef, uint dataOffset, uint secondOffset)
        {
            AddTypeName(name);
        }

        private void AddTypeName(string inName)
        {
            if (!typeNames.Contains(inName))
            {
                typeNames.Add(inName);
            }
        }

        private int FindExistingClass(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            return typesToProcess.FindIndex((Type value) => value == type);
        }

        protected EbxClass GetClass(Type objType)
        {
            if (objType == null)
            {
                throw new ArgumentNullException("objType");
            }

            // FIFA23. Hard coding TextureAsset for testing until SDK Gen.
            //if (ProfilesLibrary.LoadedProfile.Name.Equals("FIFA23", StringComparison.OrdinalIgnoreCase)
            //	&& objType.Name.Equals("TextureAsset", StringComparison.OrdinalIgnoreCase)
            //	)
            //{
            //	return GetClass(Guid.Parse("cfb542e8-15ce-28c4-de4d-2f0810f998dc"));
            //}

            var tiGuid = objType.GetCustomAttributes<TypeInfoGuidAttribute>().Last().Guid;
            var @class = GetClass(tiGuid);
            @class.SecondSize = (ushort)objType.GetCustomAttributes<TypeInfoGuidAttribute>().Count() > 1 ? (ushort)objType.GetCustomAttributes<TypeInfoGuidAttribute>().Count() : (ushort)0u;
            return @class;
        }

        //protected Guid GetTypeInfoGuid(EbxClass classType)
        //{
        //    if (classType.SecondSize == 0)
        //    {
        //        return EbxReader22B.std.GetGuid(classType).Value;
        //    }
        //    return EbxReader22B.patchStd.GetGuid(classType).Value;
        //}

        protected EbxClass GetClass(Guid guid)
        {
            if (EbxSharedTypeDescriptors.patchStd != null && EbxSharedTypeDescriptors.patchStd.GetClass(guid).HasValue)
            {
                return EbxSharedTypeDescriptors.patchStd.GetClass(guid).Value;
            }
            return EbxSharedTypeDescriptors.std.GetClass(guid).Value;
        }

        protected EbxField GetField(EbxClass classType, int index)
        {
            if (classType.SecondSize >= 1 && EbxSharedTypeDescriptors.patchStd.GetField(index).HasValue)
            {
                return EbxSharedTypeDescriptors.patchStd.GetField(index).Value;
            }
            return EbxSharedTypeDescriptors.std.GetField(index).Value;
        }

        protected EbxClass GetClass(EbxClass parentClassType, EbxField field)
        {
            if (parentClassType.SecondSize >= 1 && EbxSharedTypeDescriptors.patchStd.GetClass(parentClassType.Index + (short)field.ClassRef).HasValue)
            {
                return EbxSharedTypeDescriptors.patchStd.GetClass(parentClassType.Index + (short)field.ClassRef).Value;
            }
            return EbxSharedTypeDescriptors.std.GetClass(parentClassType.Index + (short)field.ClassRef).Value;

        }

        private struct TypeRefOutputStruct
        {
            public ushort Type { get; set; }
            public ushort ClassRef { get; set; }

            public TypeRefOutputStruct(in ushort in_type, in ushort in_classref)
            {
                Type = in_type;
                ClassRef = in_classref; 
            }
        }

        private TypeRefOutputStruct WriteTypeRef(TypeRef typeRef, bool addSignature, NativeWriter writer)
        {
            if (typeRef.IsNull() || typeRef.Name == "0" || typeRef.Name == "Inherited")
            {
                writer.Write(0x00);
                writer.Write(0x00);
                return new TypeRefOutputStruct(0, 0);
            }

            Type typeRefType = typeRef.GetReferencedType();

            int typeIdx = FindExistingClass(typeRefType);
            EbxClassMetaAttribute cta = typeRefType.GetCustomAttribute<EbxClassMetaAttribute>();

            EbxFieldType type = EbxFieldType.Inherited;
            EbxFieldCategory category = EbxFieldCategory.NotApplicable;
            if (cta != null)
            {
                type = cta.Type;
                category = (EbxFieldCategory)(cta.Flags & 0xF);
            }
            else if (typeRefType.Name.StartsWith("Delegate"))
            {
                type = EbxFieldType.Delegate;
                category = EbxFieldCategory.Delegate;
            }

            TypeRefOutputStruct output = default(TypeRefOutputStruct);
            uint typeFlags = (uint)type << 5;
            typeFlags |= (uint)category << 1;
            typeFlags |= 1;

            output.Type = (ushort)typeFlags;
            if (category == EbxFieldCategory.Primitive)
            {
                typeFlags |= 0x80000000;
                output.ClassRef = (ushort)typeIdx;
            }
            else
            {
                if (typeIdx == -1)
                {
                    // boxed value type refs shouldn't end up here, as they're already handled when processing classes
                    //typeIdx = AddClass(typeRefType.Name, typeRefType, addSignature);
                }
                typeFlags = (uint)(typeIdx << 2);
                typeFlags |= 2;
                // boxed value info in the EBXX section needs the class index
                // the type ref just sets it to zero
                output.ClassRef = (ushort)typeIdx;
                typeIdx = 0;
            }
            writer.Write(typeFlags);
            writer.Write(typeIdx);
            return output;
        }

        protected new byte[] WriteBoxedValueRef(BoxedValueRef value, int objectIndex)
        {
            // @todo: Does not at all handle boxed value arrays
            using (NativeWriter writer = new NativeWriter(new MemoryStream()))
            {
                object obj = value.Value;
                switch (value.Type)
                {
                    case EbxFieldType.TypeRef:
                        {
                            TypeRef typeRefObj = (TypeRef)obj;
                            WriteTypeRef(typeRefObj, false, writer);
                        }
                        break;

                    case EbxFieldType.FileRef:
                        {
                            string str = (FileRef)obj;
                            writer.Write((long)AddString(str));
                        }
                        break;

                    case EbxFieldType.CString:
                        {
                            string str = (CString)obj;
                            writer.Write((long)AddString(str));
                        }
                        break;

                    case EbxFieldType.Pointer:
                        {
                            PointerRef pointer = (PointerRef)obj;
                            //WritePointer(pointer, false, writer);
                        }
                        break;

                    case EbxFieldType.Struct:
                        {
                            object structValue = obj;
                            Type structType = structValue.GetType();
                            EbxClassMetaAttribute cta = structType.GetCustomAttribute<EbxClassMetaAttribute>();

                            writer.WritePadding(cta.Alignment);
                            //WriteClass(structValue, structType, writer.Position, writer);
                            WriteClass(structValue, structType, writer, objectIndex);
                        }
                        break;

                    case EbxFieldType.Enum: writer.Write((int)obj); break;
                    case EbxFieldType.Float32: writer.Write((float)obj); break;
                    case EbxFieldType.Float64: writer.Write((double)obj); break;
                    case EbxFieldType.Boolean: writer.Write((byte)(((bool)obj) ? 0x01 : 0x00)); break;
                    case EbxFieldType.Int8: writer.Write((sbyte)obj); break;
                    case EbxFieldType.UInt8: writer.Write((byte)obj); break;
                    case EbxFieldType.Int16: writer.Write((short)obj); break;
                    case EbxFieldType.UInt16: writer.Write((ushort)obj); break;
                    case EbxFieldType.Int32: writer.Write((int)obj); break;
                    case EbxFieldType.UInt32: writer.Write((uint)obj); break;
                    case EbxFieldType.Int64: writer.Write((long)obj); break;
                    case EbxFieldType.UInt64: writer.Write((ulong)obj); break;
                    case EbxFieldType.Guid: writer.Write((Guid)obj); break;
                    case EbxFieldType.Sha1: writer.Write((Sha1)obj); break;
                    case EbxFieldType.String: writer.WriteFixedSizedString((string)obj, 32); break;
                    case EbxFieldType.ResourceRef: writer.Write((ResourceRef)obj); break;

                    default: throw new InvalidDataException($"Unhandled field type: {value.Type}");
                }

                return ((MemoryStream)writer.BaseStream).ToArray();
            }
        }

    }

}