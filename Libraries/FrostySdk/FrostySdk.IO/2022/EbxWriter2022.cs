using FMT.FileTools;
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


namespace FrostySdk.FrostySdk.IO
{
    public class EbxWriter2022 : EbxBaseWriter
    {
        protected readonly List<object> objsToProcess = new List<object>();

        private readonly List<Type> typesToProcess = new List<Type>();

        private readonly List<EbxFieldMetaAttribute> arrayTypes = new List<EbxFieldMetaAttribute>();

        private readonly List<object> objs = new List<object>();

        private readonly List<object> sortedObjs = new List<object>();

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

        private readonly List<EbxArray> arrays = new List<EbxArray>();

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

        private List<(int arrayOffset, int arrayIndex, int containingArrayIndex)> unpatchedArrayInfo { get; } = new List<(int, int, int)>();

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
            if (objects == null)
            {
                throw new ArgumentNullException("objects");
            }
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
            PropertyInfo[] properties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            List<object> dataContainers = new List<object>();
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (!flags.HasFlag(EbxWriteFlags.IncludeTransient) && propertyInfo.GetCustomAttribute<IsTransientAttribute>() != null)
                {
                    continue;
                }
                if (propertyInfo.PropertyType == typeof(PointerRef))
                {
                    PointerRef pointerRef2 = (PointerRef)propertyInfo.GetValue(obj);
                    if (pointerRef2.Type == PointerRefType.Internal)
                    {
                        dataContainers.Add(pointerRef2.Internal);
                    }
                    else if (pointerRef2.Type == PointerRefType.External && !imports.Contains(pointerRef2.External))
                    {
                        imports.Add(pointerRef2.External);
                    }
                }
                else if (propertyInfo.PropertyType.Namespace == "FrostySdk.Ebx" && propertyInfo.PropertyType.BaseType != typeof(Enum))
                {
                    object value = propertyInfo.GetValue(obj);
                    dataContainers.AddRange(ExtractClass(value.GetType(), value, add: false));
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
            if (stringsToCStringOffsets.Any())
            {
                var oldEntry = AssetManager.Instance.GetEbxEntry(stringsToCStringOffsets.Keys.First());
                var oldEbxStream = AssetManager.Instance.GetEbxStream(oldEntry);
                using (var reader = new EbxReader22B(oldEbxStream, true))
                {
                    origEbxArrays.AddRange(reader.arrays.Where(x => x.Count > 0).OrderBy(x => x.Offset));
                }
            }
            var ebxxArrays = arrays.Where(x => x.Count > 0).OrderBy(x => x.Offset).ToArray();
            var ebxxArrayCount = ebxxArrays.Count();
            //if (origEbxArrays.Count() <= ebxxArrayCount)
            //{
            //             Write(0u);
            //             Write(0u);
            //         }
            //else
            //{
            Write(ebxxArrayCount);
            //Write(0u);
            Write(0u);
            for (var iEbxxArray = 0; iEbxxArray < ebxxArrayCount; iEbxxArray++)
            {
                var arr = ebxxArrays[iEbxxArray];
                Write(arr.Offset);
                Write(arr.Count);
                if(origEbxArrays.Count - 1 > iEbxxArray)
                    Write(origEbxArrays[iEbxxArray].PathDepth);
                else
                    Write(0x00);
                Write((ushort)origEbxArrays[iEbxxArray].TypeFlags);
                Write((ushort)arr.ClassRef);
            }
            //}
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
            if (type.BaseType!.Namespace == "FrostySdk.Ebx")
            {
                ProcessClass(type.BaseType, obj, isBaseType: true);
            }
            int classIndex = FindExistingClass(type);
            if (classIndex != -1)
            {
                return (ushort)classIndex;
            }
            type.GetCustomAttribute<EbxClassMetaAttribute>();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            List<PropertyInfo> propertiesToInclude = new List<PropertyInfo>();
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.GetCustomAttribute<IsTransientAttribute>() == null || flags.HasFlag(EbxWriteFlags.IncludeTransient))
                {
                    propertiesToInclude.Add(propertyInfo);
                }
            }
            if (!isBaseType)
            {
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
            exportedInstances.Sort(delegate (dynamic a, dynamic b)
            {
                AssetClassGuid assetClassGuid2 = a.GetInstanceGuid();
                AssetClassGuid assetClassGuid3 = b.GetInstanceGuid();
                byte[] array = assetClassGuid2.ExportedGuid.ToByteArray();
                byte[] array2 = assetClassGuid3.ExportedGuid.ToByteArray();
                uint num = (uint)((array[0] << 24) | (array[1] << 16) | (array[2] << 8) | array[3]);
                uint value3 = (uint)((array2[0] << 24) | (array2[1] << 16) | (array2[2] << 8) | array2[3]);
                return num.CompareTo(value3);
            });
            nonExportedInstances.Sort((object a, object b) => string.CompareOrdinal(a.GetType().Name, b.GetType().Name));
            sortedObjs.Add(primaryInstance);
            sortedObjs.AddRange(exportedInstances);
            sortedObjs.AddRange(nonExportedInstances);
            MemoryStream memoryStream = new MemoryStream();
            _ = base.Position;
            FileWriter nativeWriter = new FileWriter(memoryStream);
            for (int i = 0; i < sortedObjs.Count; i++)
            {
                AssetClassGuid assetClassGuid = ((dynamic)sortedObjs[i]).GetInstanceGuid();
                Type type = sortedObjs[i].GetType();
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

                WriteClass(sortedObjs[i], type, nativeWriter);
                EbxInstance ebxInstance = default(EbxInstance);
                ebxInstance.ClassRef = (ushort)classIndex;
                ebxInstance.Count = 1;
                ebxInstance.IsExported = assetClassGuid.IsExported;
                instances.Add(ebxInstance);
                exportedCount += (ushort)(ebxInstance.IsExported ? 1 : 0);
            }
            nativeWriter.WritePadding(16);
            arraysPosition = (int)nativeWriter.Position;
            nativeWriter.WriteEmpty(32);
            foreach (var unpatchedArray2 in unpatchedArrayInfo)
            {
                EbxArray arrayInfo = arrays[unpatchedArray2.arrayIndex];
                byte[] arrayData = this.arrayData[unpatchedArray2.arrayIndex];
                if (arrayInfo.Count == 0)
                {
                    uint offsetFromPayload = (arrayInfo.Offset = (uint)(arraysPosition + 16));
                }
                else
                {
                    long beforePaddingPosition = nativeWriter.Position;
                    //nativeWriter.WritePadding(16);
                    //if ((nativeWriter.Position - beforePaddingPosition) < 4)
                    //               {
                    //	nativeWriter.WriteEmpty(16);
                    //}
                    ////nativeWriter.Position -= 12L;
                    //nativeWriter.Position -= 4L;
                    nativeWriter.WriteUInt32LittleEndian(arrayInfo.Count);
                    long beforeArrayPosition = nativeWriter.Position;
                    nativeWriter.WriteBytes(arrayData);
                    arrayInfo.Offset = (uint)beforeArrayPosition;
                    nativeWriter.WritePadding(16);
                }
                arrays[unpatchedArray2.arrayIndex] = arrayInfo;
            }
            nativeWriter.WritePadding(16);
            boxedValuesPosition = (int)nativeWriter.Position;
            nativeWriter.WritePadding(16);
            stringTablePosition = (int)nativeWriter.Position;
            foreach (KeyValuePair<string, List<(int, int)>> stringsToCStringOffset in stringsToCStringOffsets)
            {
                stringsToCStringOffset.Deconstruct(out var key, out var value);
                string stringValue = key;
                List<(int, int)> list = value;
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
            foreach (var unpatchedArray in unpatchedArrayInfo)
            {
                EbxArray ebxArray = arrays[unpatchedArray.arrayIndex];
                var (arrayPointerOffset, _, _) = unpatchedArray;
                if (unpatchedArray.containingArrayIndex > -1)
                {
                    int realArrayIndex5 = arrayIndicesMap[unpatchedArray.containingArrayIndex];
                    nativeWriter.Position = arrays[realArrayIndex5].Offset + arrayPointerOffset;
                }
                else
                {
                    nativeWriter.Position = arrayPointerOffset;
                }
                int relativeOffset = (int)(ebxArray.Offset - nativeWriter.Position);
                nativeWriter.WriteInt32LittleEndian(relativeOffset);
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
                nativeWriter.WriteInt32LittleEndian((int)(dataContainerOffsets[dataContainerIndex] - nativeWriter.Position));
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
                patchedImportOffsets.Add((int)(importOffset + arrays[realArrayIndex].Offset));
            }
            mainData = memoryStream.ToArray();
            uniqueClassCount = (ushort)uniqueTypes.Count;
        }

        protected virtual void WriteClass(object obj, Type objType, NativeWriter writer)
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
            if (objType.BaseType!.Namespace == "FrostySdk.Ebx")
            {
                WriteClass(obj, objType.BaseType, writer);
            }

            PropertyInfo[] properties = objType
                .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
#if DEBUG
            var writtenProperties = new List<PropertyInfo>();
#endif

            var classMeta = objType.GetCustomAttribute<EbxClassMetaAttribute>();

            //foreach (var propertyInfo in properties.OrderBy(x => x.GetCustomAttribute<FieldIndexAttribute>().Index))
            foreach (var propertyInfo in properties.OrderBy(x => x.GetCustomAttribute<EbxFieldMetaAttribute>().Offset))
            {
                IsTransientAttribute isTransientAttribute = propertyInfo.GetCustomAttribute<IsTransientAttribute>();
                if (isTransientAttribute != null)
                    continue;

                EbxFieldMetaAttribute ebxFieldMetaAttribute = propertyInfo.GetCustomAttribute<EbxFieldMetaAttribute>();
                if (ebxFieldMetaAttribute == null || ebxFieldMetaAttribute.Type == EbxFieldType.Inherited)
                    continue;

                if (propertyInfo == null)
                {
#if DEBUG
                    Debug.WriteLine("There is a dodgy Property in here. How can there be a null property info in a list of property infos?");
#endif
                    continue;
                }
                else
                {
#if DEBUG
                    writtenProperties.Add(propertyInfo);
#endif
                    bool isReference = propertyInfo.GetCustomAttribute<IsReferenceAttribute>() != null;
                    if (ebxFieldMetaAttribute.IsArray)
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
#if DEBUG
            var unwrittenProperties = properties.Where(x => !writtenProperties.Any(y => y.Name == x.Name));
            if (unwrittenProperties.Any() && obj == objsToProcess[0])
            {

            }
#endif
            writer.WritePadding(classMeta.Alignment);
        }

        protected void WriteField(object obj, EbxFieldType ebxType, byte classAlignment, NativeWriter writer, bool isReference)
        {
            switch (ebxType)
            {
                case EbxFieldType.TypeRef:
                    throw new NotSupportedException("The asset contains a TypeRef field, which is not yet supported.");
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
                            pointerRefValue = sortedObjs.FindIndex((object value) => value == pointer.Internal);
                            if (pointerRefValue == -1)
                            {
                                throw new IndexOutOfRangeException("");
                            }
                            pointerRefPositionToDataContainerIndex[((int)writer.Position, currentArrayIndex)] = pointerRefValue;
                            //pointerRefPositionToDataContainerIndex.Add(((int)writer.Position, currentArrayIndex), pointerRefValue);
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
                        WriteClass(obj, structObjType, writer);
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
                //case EbxFieldType.BoxedValueRef:
                //	throw new NotSupportedException("The asset contains a BoxedValueRef field, which is not yet supported.");
                //case EbxFieldType.Delegate:
                //	throw new NotSupportedException("The asset contains a Delegate field, which is not yet supported.");
                //case EbxFieldType.Function:
                //	throw new NotSupportedException("The asset contains a Function field, which is not yet supported.");
                default:
                    throw new InvalidDataException($"Unknown field type {ebxType}");
            }
        }

        //protected void WriteArray(object obj, EbxFieldType elementFieldType, uint fieldNameHash, byte classAlignment, NativeWriter writer, bool isReference)
        protected void WriteArray(object obj, EbxFieldMetaAttribute fieldMetaAttribute, uint fieldNameHash, byte classAlignment, NativeWriter writer, bool isReference)
        {
            int classIndex = typesToProcess.FindIndex((Type item) => item == obj.GetType().GetGenericArguments()[0]);
            if (classIndex == -1)
            {
                classIndex = 65535;
            }
            IList typedObj = (IList)obj;
            int arrayCount = typedObj.Count;
            MemoryStream arrayMemoryStream = new MemoryStream();
            FileWriter arrayWriter = new FileWriter(arrayMemoryStream);
            int previousArrayIndex = currentArrayIndex;
            int localArrayIndex = (currentArrayIndex = arrayIndicesMap.Count);
            arrayIndicesMap.Add(-1);
            currentArrayDepth++;
            long pointerPosition = writer.Position;
            writer.WriteInt64LittleEndian(0L);
            for (int i = 0; i < arrayCount; i++)
            {
                object arrayElementObj = typedObj[i];
                //WriteField(arrayElementObj, elementFieldType, classAlignment, arrayWriter, isReference);
                WriteField(arrayElementObj, fieldMetaAttribute.ArrayType, classAlignment, arrayWriter, isReference);
            }
            int arrayIndex = arrays.Count;
            arrays.Add(new EbxArray
            {
                Count = (uint)arrayCount,
                ClassRef = classIndex,
                PathDepth = fieldNameHash,
                //TypeFlags = (int)elementFieldType,
                TypeFlags = fieldMetaAttribute.ArrayFlags,
                Offset = 0u
            });
            arrayIndicesMap[localArrayIndex] = arrayIndex;
            unpatchedArrayInfo.Add(((int)pointerPosition, arrayIndex, previousArrayIndex));
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

        protected Guid GetTypeInfoGuid(EbxClass classType)
        {
            if (classType.SecondSize == 0)
            {
                return EbxReader22B.std.GetGuid(classType).Value;
            }
            return EbxReader22B.patchStd.GetGuid(classType).Value;
        }

        protected EbxClass GetClass(Guid guid)
        {
            if (EbxReader22B.patchStd != null && EbxReader22B.patchStd.GetClass(guid).HasValue)
            {
                return EbxReader22B.patchStd.GetClass(guid).Value;
            }
            return EbxReader22B.std.GetClass(guid).Value;
        }

        protected EbxField GetField(EbxClass classType, int index)
        {
            if (classType.SecondSize >= 1 && EbxReader22B.patchStd.GetField(index).HasValue)
            {
                return EbxReader22B.patchStd.GetField(index).Value;
            }
            return EbxReader22B.std.GetField(index).Value;
        }

        protected EbxClass GetClass(EbxClass parentClassType, EbxField field)
        {
            if (parentClassType.SecondSize >= 1 && EbxReader22B.patchStd.GetClass(parentClassType.Index + (short)field.ClassRef).HasValue)
            {
                return EbxReader22B.patchStd.GetClass(parentClassType.Index + (short)field.ClassRef).Value;
            }
            return EbxReader22B.std.GetClass(parentClassType.Index + (short)field.ClassRef).Value;

        }
    }

}