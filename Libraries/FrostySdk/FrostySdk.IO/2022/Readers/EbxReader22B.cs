using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.FrostySdk.IO;
using FrostySdk.Managers;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FrostySdk.IO._2022.Readers
{
    public class EbxReader22B : EbxReader22A
    {
        //internal const int EbxExternalReferenceMask = 1;

        //internal static EbxSharedTypeDescriptorV2 std { get; private set; }

        //internal static EbxSharedTypeDescriptorV2 patchStd { get; private set; }

        public EbxReader22B(Stream ebxDataStream, bool inPatched)
            : base(ebxDataStream, passthru: true)
        {
            Position = 0;
            InitialRead(ebxDataStream, inPatched);

        }

        public override void InitialRead(Stream InStream, bool inPatched)
        {
            if (InStream == null)
            {
                throw new ArgumentNullException("ebxDataStream");
            }
            
            if (stream != InStream)
            {
                stream = InStream;
                stream.Position = 0;
            }
            Position = 0;

            this.patched = inPatched;
            base.magic = (EbxVersion)base.ReadUInt32LittleEndian();
            
            this.LoadRiffEbx();
            this.isValid = true;
            IsLoaded = true;

        }

        protected virtual void LoadRiffEbx()
        {
            classGuids.Clear();
            fieldTypes.Clear();
            classTypes.Clear();
            instances.Clear();
            imports.Clear();
            dependencies.Clear();
            objects.Clear();
            refCounts.Clear();
            arrays.Clear();
            boxedValues.Clear();
            boxedValueRefs.Clear();

            uint chunkSize = base.ReadUInt32LittleEndian();
            if (chunkSize == 0)
                return;
            long chunkSizeRelativeToPosition = base.Position;
            uint chunkName = base.ReadUInt32LittleEndian();
            if (chunkName == 0)
                return;

            if (chunkName != 5784133 && chunkName != 1398293061)
            {
#if DEBUG
                Position = 0;
                if (File.Exists($"ebx.BadRIFF.read.22.dat"))
                    File.Delete($"ebx.BadRIFF.read.22.dat");
                var fsDump = new FileStream($"ebx.BadRIFF.read.22.dat", FileMode.OpenOrCreate);
                base.stream.CopyTo(fsDump);
                fsDump.Close();
                fsDump.Dispose();
#endif
                //throw new InvalidDataException("Incorrectly formatted RIFF detected.");
                return;
            }
            chunkName = base.ReadUInt32LittleEndian();
            if (chunkName != 1146634821)
            {
                return;
                //throw new InvalidDataException("Incorrectly formatted RIFF detected. Expected EBXD.");
            }
            chunkSize = base.ReadUInt32LittleEndian();
            chunkSizeRelativeToPosition = base.Position;
            base.Pad(16);
            long payloadOffset = (this.payloadPosition = base.Position);
            base.Position = chunkSizeRelativeToPosition + chunkSize;
            _ = base.Position;
            base.Pad(2);
            chunkName = base.ReadUInt32LittleEndian();
            if (chunkName != 1481197125)
            {
                throw new InvalidDataException("Incorrectly formatted RIFF detected. Expected EFIX.");
            }
            chunkSize = base.ReadUInt32LittleEndian();
            chunkSizeRelativeToPosition = base.Position;
            Guid partitionGuid = (base.fileGuid = base.ReadGuid());
            uint guidCount = base.ReadUInt32LittleEndian();
            for (int i6 = 0; i6 < guidCount; i6++)
            {
                Guid guid = base.ReadGuid();
                this.classGuids.Add(guid);
            }
            uint signatureCount = base.ReadUInt32LittleEndian();
            List<uint> signatures = new List<uint>((int)signatureCount);
            for (int i5 = 0; i5 < signatureCount; i5++)
            {
                uint ebxSignature = base.ReadUInt32LittleEndian();
                signatures.Add(ebxSignature);
            }
            uint exportedInstancesCount = base.ReadUInt32LittleEndian();
            base.exportedCount = (ushort)exportedInstancesCount;
            uint dataContainerCount = base.ReadUInt32LittleEndian();
            List<uint> dataContainerOffsets = (this.dataContainerOffsets = new List<uint>((int)dataContainerCount));
            for (int i4 = 0; i4 < dataContainerCount; i4++)
            {
                uint dataContainerOffset = base.ReadUInt32LittleEndian();
                dataContainerOffsets.Add(dataContainerOffset);
            }
            uint pointerOffsetsCount = base.ReadUInt32LittleEndian();
            List<uint> pointerOffsets = new List<uint>((int)pointerOffsetsCount);
            for (int i3 = 0; i3 < pointerOffsetsCount; i3++)
            {
                uint pointerOffset = base.ReadUInt32LittleEndian();
                pointerOffsets.Add(pointerOffset);
            }
            uint resourceRefOffsetsCount = base.ReadUInt32LittleEndian();
            List<uint> resourceRefOffsets = new List<uint>((int)resourceRefOffsetsCount);
            for (int i2 = 0; i2 < resourceRefOffsetsCount; i2++)
            {
                uint resourceRefOffset = base.ReadUInt32LittleEndian();
                resourceRefOffsets.Add(resourceRefOffset);
            }
            uint importsCount = base.ReadUInt32LittleEndian();
            for (int n = 0; n < importsCount; n++)
            {
                EbxImportReference ebxImportReference2 = default(EbxImportReference);
                ebxImportReference2.FileGuid = base.ReadGuid();
                ebxImportReference2.ClassGuid = base.ReadGuid();
                EbxImportReference ebxImportReference = ebxImportReference2;
                base.imports.Add(ebxImportReference);
                if (!base.dependencies.Contains(ebxImportReference.FileGuid))
                {
                    base.dependencies.Add(ebxImportReference.FileGuid);
                }
            }
            uint importOffsetsCount = base.ReadUInt32LittleEndian();
            List<uint> importOffsets = (this.importOffsets = new List<uint>((int)importOffsetsCount));
            for (int m = 0; m < importOffsetsCount; m++)
            {
                uint importOffset = base.ReadUInt32LittleEndian();
                importOffsets.Add(importOffset);
            }
            uint typeInfoOffsetsCount = base.ReadUInt32LittleEndian();
            List<uint> typeInfoOffsets = new List<uint>((int)typeInfoOffsetsCount);
            for (int l = 0; l < typeInfoOffsetsCount; l++)
            {
                uint typeInfoOffset = base.ReadUInt32LittleEndian();
                typeInfoOffsets.Add(typeInfoOffset);
            }
            uint arrayOffset = base.ReadUInt32LittleEndian();
            this.arrayOffset = arrayOffset;
            base.ReadUInt32LittleEndian();
            uint stringTableOffset = base.ReadUInt32LittleEndian();
            base.stringsOffset = stringTableOffset + payloadOffset;
            chunkName = base.ReadUInt32LittleEndian();
            if (chunkName == 0 && base.ReadUInt32LittleEndian() != 1482179141)
                base.Position -= 4;
            
            chunkSize = base.ReadUInt32LittleEndian();
            if (chunkSize == 1482179141)
                chunkSize = base.ReadUInt32LittleEndian();

            chunkSizeRelativeToPosition = base.Position;
            uint arrayCount = base.ReadUInt32LittleEndian();
            uint boxedValueCount = base.ReadUInt32LittleEndian();
            for (int k = 0; k < arrayCount; k++)
            {
                uint offset = base.ReadUInt32LittleEndian();
                uint elementCount = base.ReadUInt32LittleEndian();
                uint pathDepth = base.ReadUInt32LittleEndian();
                ushort typeFlags = base.ReadUInt16LittleEndian();
                ushort typeId = base.ReadUInt16LittleEndian();
                base.arrays.Add(new EbxArray
                {
                    ClassRef = typeId,
                    Count = elementCount,
                    Offset = offset,
                    TypeFlags = typeFlags,
                    PathDepth = pathDepth
                });
            }
            for (int j = 0; j < boxedValueCount; j++)
            {
                //base.ReadUInt32LittleEndian();
                //base.ReadUInt32LittleEndian();
                //base.ReadUInt32LittleEndian();
                //base.ReadUInt16LittleEndian();
                //base.ReadUInt16LittleEndian();
                uint offset = ReadUInt();
                uint count = ReadUInt();
                uint hash = ReadUInt();
                ushort type = ReadUShort();
                ushort classRef = ReadUShort();

                boxedValues.Add
                (
                    new EbxBoxedValue
                    {
                        Offset = offset,
                        Type = type,
                        ClassRef = classRef
                    }
                );
            }
            _ = base.Position;
            _ = base.Length;
            foreach (uint dataContainerOffset2 in dataContainerOffsets)
            {
                base.Position = payloadOffset + dataContainerOffset2;
                uint typeInfoIndex = base.ReadUInt32LittleEndian();
                base.instances.Add(new EbxInstance
                {
                    ClassRef = (ushort)typeInfoIndex,
                    Count = 1,
                    IsExported = (base.instances.Count < exportedInstancesCount)
                });
            }
            _ = this.classGuids.Count;
            _ = signatures.Count;
            
            base.Position = payloadOffset;
            base.isValid = true;
            IsLoaded = true;


#if DEBUG

            var debuggingFileStreamDirectoryPath = Directory.CreateDirectory(AppContext.BaseDirectory + "\\Debugging\\EBX\\");

            var debugFileStreamPath = $"{debuggingFileStreamDirectoryPath.FullName}\\ebx.{RootType}.read.22.dat";
            if (File.Exists(debugFileStreamPath))
                File.Delete(debugFileStreamPath);

            if (RootType.Contains("Hotspot", StringComparison.OrdinalIgnoreCase))
            {
                Position = 0;
                var fsDump = new FileStream($"ebx.{RootType}.read.22.dat", FileMode.OpenOrCreate);
                base.stream.CopyTo(fsDump);
                fsDump.Close();
                fsDump.Dispose();
                Position = payloadOffset;
            }
            else if (RootType.Contains("SkinnedMeshAsset", StringComparison.OrdinalIgnoreCase))
            {
                Position = 0;
                var fsDump = new FileStream($"ebx.{RootType}.read.22.dat", FileMode.OpenOrCreate);
                base.stream.CopyTo(fsDump);
                fsDump.Close();
                fsDump.Dispose();
                Position = payloadOffset;
            }
            else if (RootType.Contains("AttribSchema_gp_positioning_zonal_defense_attribute", StringComparison.OrdinalIgnoreCase))
            {
                Position = 0;
                var fsDump = new FileStream($"ebx.{RootType}.read.22.dat", FileMode.OpenOrCreate);
                base.stream.CopyTo(fsDump);
                fsDump.Close();
                fsDump.Dispose();
                Position = payloadOffset;
            }
            else if (RootType.Contains("AttribSchema_gp_actor_movement", StringComparison.OrdinalIgnoreCase))
            {
                Position = 0;
                var fsDump = new FileStream($"ebx.{RootType}.read.22.dat", FileMode.OpenOrCreate);
                base.stream.CopyTo(fsDump);
                fsDump.Close();
                fsDump.Dispose();
                Position = payloadOffset;
            }
            else if (RootType.Contains("gp_cpuai_cpuaiballhandler", StringComparison.OrdinalIgnoreCase))
            {
                Position = 0;
                var fsDump = new FileStream($"ebx.{RootType}.read.22.dat", FileMode.OpenOrCreate);
                base.stream.CopyTo(fsDump);
                fsDump.Close();
                fsDump.Dispose();
                Position = payloadOffset;
            }
            else if (RootType.Contains("AttribSchema_gp_cpuai_cpuaithroughpass", StringComparison.OrdinalIgnoreCase))
            {
                Position = 0;
                var fsDump = new FileStream($"ebx.{RootType}.read.22.dat", FileMode.OpenOrCreate);
                base.stream.CopyTo(fsDump);
                fsDump.Close();
                fsDump.Dispose();
                Position = payloadOffset;
            }
            else
            {

                Position = 0;
                var fsDump = new FileStream(debugFileStreamPath, FileMode.OpenOrCreate);
                base.stream.CopyTo(fsDump);
                fsDump.Close();
                fsDump.Dispose();
                Position = payloadOffset;
            }

#endif
        }

        public override EbxAsset ReadAsset(EbxAssetEntry entry = null)
        {
            return ReadAsset<EbxAsset>();
        }

        public override T ReadAsset<T>()
        {
            T val = new T();
            this.InternalReadObjects();
            val.fileGuid = base.fileGuid;
            val.objects = base.objects;
            val.dependencies = base.dependencies;
            val.refCounts = base.refCounts;
            val.arrays = base.arrays;
            return val;
        }

        /// <summary>
        /// Reads objects to objects list by type from the EBX bytes
        /// </summary>
        public override void InternalReadObjects()
        {
            // ----------------------------------------------------------------------
            // Instantiates all objects before processing
            for (int i = 0; i < instances.Count; i++)
            {
                var ebxInstance2 = base.instances[i];
                Type objType = TypeLibrary.GetType(this.classGuids[ebxInstance2.ClassRef]);
                var instanceObj = TypeLibrary.CreateObject(objType);
                if (!objects.Contains(instanceObj))
                    objects.Add(instanceObj);
            }
            int num2 = 0;
            for (int i = 0; i < instances.Count; i++)
            {
                var ebxInstance2 = base.instances[i];
                Type objType = TypeLibrary.GetType(this.classGuids[ebxInstance2.ClassRef]);
                var align = objType.GetCustomAttribute<EbxClassMetaAttribute>().Alignment;
                var size = objType.GetCustomAttribute<EbxClassMetaAttribute>().Size;

                for (int j = 0; j < ebxInstance2.Count; j++)
                {
                    dynamic obj = base.objects[i];
                    EbxClass @class = this.GetClass(objType);
                    //base.Pad(@class.Alignment);
                    base.Pad(align);

                    Guid inGuid = Guid.Empty;
                    if (ebxInstance2.IsExported)
                    {
                        //base.Pad(@class.Alignment);
                        inGuid = base.ReadGuid();
                    }
                    long classPosition = base.Position;

                    //base.ReadInt32LittleEndian();
                    //base.Position += 12L;
                    _ = base.ReadGuid();

                    if (align != 4)
                        base.Position += 8L;

                    obj.SetInstanceGuid(new AssetClassGuid(inGuid, num2++));
                    long startOffset = (base.Position - 24);

                    try
                    {
                        this.ReadClass(@class, obj, startOffset);
                    }
                    catch
                    {

                    }

                    base.Position = classPosition + size;
                }
            }
        }

        public EbxClass GetClass(Type objType)
        {
            EbxClass? ebxClass = null;
            var nameHashAttribute = objType.GetCustomAttribute<HashAttribute>();
            var ebxclassmeta = objType.GetCustomAttribute<EbxClassMetaAttribute>();
            if (nameHashAttribute != null && ebxclassmeta != null)
            {
                if (EbxSharedTypeDescriptors.patchStd != null)
                {
                    var nHClass = EbxSharedTypeDescriptors.patchStd.Classes
                              .Union(EbxSharedTypeDescriptors.std.Classes).FirstOrDefault(x => x.HasValue && x.Value.NameHash == nameHashAttribute.Hash);
                    if (nHClass.HasValue)
                        return nHClass.Value;
                }
                else if (EbxSharedTypeDescriptors.std != null)
                {
                    var nHClass = EbxSharedTypeDescriptors.std.Classes.FirstOrDefault(x => x.HasValue && x.Value.NameHash == nameHashAttribute.Hash);
                    if (nHClass.HasValue)
                        return nHClass.Value;
                }
            }


            return ebxClass.HasValue ? ebxClass.Value : default(EbxClass);
        }

        public override PropertyInfo GetProperty(Type objType, EbxField field)
        {
            PropertyInfo[] properties = objType.GetProperties();
            PropertyInfo propertyInfo = properties.SingleOrDefault(x =>
                x.GetCustomAttribute<HashAttribute>() != null
                && x.GetCustomAttribute<HashAttribute>().Hash == field.NameHash);
           
            return propertyInfo;
        }

        public override object CreateObject(EbxClass classType)
        {
            if (classType.SecondSize == 1)
            {
                return TypeLibrary.CreateObject(EbxSharedTypeDescriptors.patchStd.GetGuid(classType).Value);
            }
            return TypeLibrary.CreateObject(EbxSharedTypeDescriptors.std.GetGuid(classType).Value);
        }

        public override object Read(Type type, long? readPosition = null, int? paddingAlignment = null)
        {
            switch (type.Name)
            {
                case "ResourceRef":
                    return this.ReadResourceRef();
                case "CString":
                    return base.ReadCString(base.ReadUInt32LittleEndian());
                case "FileRef":
                    return this.ReadFileRef();
                case "TypeRef":
                    return this.ReadTypeRef();
                case "BoxedValueRef":
                    //throw new NotImplementedException("BoxedValueRef has not been implemented!");
                    return this.ReadBoxedValueRef();
                case "Pointer":
                case "PointerRef":
                    int num = base.ReadInt32LittleEndian();
                    if (num == 0)
                    {
                        return default(PointerRef);
                    }
                    if ((num & 1) == 1)
                    {
                        var pointerRefImportIndex = num >> 1;
                        if (pointerRefImportIndex >= 0 && base.imports.Count > pointerRefImportIndex)
                            return new PointerRef(base.imports[pointerRefImportIndex]);

                        return default(PointerRef);
                    }
                    long offset = base.Position - 4 + num - this.payloadPosition;
                    int dc = this.dataContainerOffsets.IndexOf((uint)offset);
                    if (dc == -1)
                    {
                        return default(PointerRef);
                    }
                    if (dc > objects.Count - 1)
                        return default(PointerRef);

                    return new PointerRef(objects[dc]);
                default:
                    return base.Read(type, readPosition, paddingAlignment);
            }
        }

        //public override object ReadArray(object obj, PropertyInfo property)
        //{
        //    long position = base.Position;
        //    int arrayOffset = Read<int>(); // base.ReadInt32LittleEndian();
        //    var newPosition = base.Position + arrayOffset - 4;
        //    if (newPosition < 0)
        //        return null;
        //    if (newPosition > base.Length)
        //        return null;

        //    base.Position += arrayOffset - 4;
        //    base.Position -= 4L;
        //    uint arrayCount = Read<uint>();// base.ReadUInt32LittleEndian();
        //    if (arrayCount == 0)
        //        return obj;

        //    for (int i = 0; i < arrayCount; i++)
        //    {
        //        var genT = property.PropertyType;
        //        var genArg0 = genT.GetGenericArguments()[0];

        //        object obj2 = null;

        //        if (genArg0.Name == "PointerRef")
        //            obj2 = this.ReadField(default(EbxClass), EbxFieldType.Pointer, ushort.MaxValue, false);
        //        else if (genArg0.Name == "CString")
        //            obj2 = this.ReadField(default(EbxClass), EbxFieldType.CString, ushort.MaxValue, false);
        //        else
        //            obj2 = Read(genArg0);

        //        var addMethod = genT.GetMethod("Add");
        //        if (addMethod == null)
        //            continue;

        //        addMethod
        //            .Invoke(property.GetValue(obj), new object[1] { obj2 });

        //    }
        //    base.Position = position;
        //    return obj;
        //}


    }
}