using FMT.FileTools;
using FrostySdk.Attributes;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FrostySdk.IO
{
    public class EbxSharedTypeDescriptors : IEbxSharedTypeDescriptor
    {
        public static IEbxSharedTypeDescriptor std { get; set; }

        public static IEbxSharedTypeDescriptor patchStd { get; set; }

        public static void LoadSharedTypeDescriptors()
        {
            if (EbxSharedTypeDescriptors.std == null)
            {
                
                if (FileSystem.Instance.HasFileInMemoryFs("SharedTypeDescriptors.ebx"))
                {
                    if (!string.IsNullOrEmpty(ProfileManager.LoadedProfile.EBXTypeDescriptor))
                        EbxSharedTypeDescriptors.std = (IEbxSharedTypeDescriptor)AssetManager.LoadTypeByName(ProfileManager.LoadedProfile.EBXTypeDescriptor, "SharedTypeDescriptors.ebx", false);
                    else
                        EbxSharedTypeDescriptors.std = new EbxSharedTypeDescriptors("SharedTypeDescriptors.ebx", false);
                }
                if (FileSystem.Instance.HasFileInMemoryFs("SharedTypeDescriptors_patch.ebx"))
                {
                    if (!string.IsNullOrEmpty(ProfileManager.LoadedProfile.EBXTypeDescriptor))
                        EbxSharedTypeDescriptors.patchStd = (IEbxSharedTypeDescriptor)AssetManager.LoadTypeByName(ProfileManager.LoadedProfile.EBXTypeDescriptor, "SharedTypeDescriptors.ebx", true);
                    else
                        EbxSharedTypeDescriptors.patchStd = new EbxSharedTypeDescriptors("SharedTypeDescriptors.ebx", true);
                }
                if (FileSystem.Instance.HasFileInMemoryFs("SharedTypeDescriptors_Patch.ebx"))
                {
                    if (!string.IsNullOrEmpty(ProfileManager.LoadedProfile.EBXTypeDescriptor))
                        EbxSharedTypeDescriptors.patchStd = (IEbxSharedTypeDescriptor)AssetManager.LoadTypeByName(ProfileManager.LoadedProfile.EBXTypeDescriptor, "SharedTypeDescriptors.ebx", true);
                    else
                        EbxSharedTypeDescriptors.patchStd = new EbxSharedTypeDescriptors("SharedTypeDescriptors.ebx", true);
                }
            }
        }


        private List<EbxClass?> classes = new List<EbxClass?>();
        public List<EbxClass?> Classes { get { return classes; } }

        public Dictionary<Guid, int> mapping = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> Mapping { get { return mapping; } }

        private List<EbxField> fields = new List<EbxField>();
        public List<EbxField> Fields { get { return fields; } }

        private List<Guid> guids = new List<Guid>();
        public List<Guid> Guids { get { return guids; } }
        public Dictionary<EbxClass, List<EbxField>> ClassFields { get; } = new Dictionary<EbxClass, List<EbxField>>();

        public bool ReflectionTypeDescripter { get; set; } = false;

        private static Assembly EbxClassesAssembly;
        private static Type[] EbxClassesTypes;

        public static string GetClassName(uint nameHash)
        {
            EbxClassesAssembly = AppDomain.CurrentDomain
                                    .GetAssemblies().FirstOrDefault(x => x.FullName.Contains("EbxClasses", StringComparison.OrdinalIgnoreCase));
            if (EbxClassesAssembly != null)
            {
                if (EbxClassesTypes == null)
                    EbxClassesTypes = EbxClassesAssembly.GetTypes();

                for (var i = 0; i < EbxClassesTypes.Length; i++)
                {
                    //var t = EbxClassesTypes.FirstOrDefault(t =>
                    //		t.GetCustomAttribute<HashAttribute>() != null
                    //		&& (uint)t.GetCustomAttribute<HashAttribute>().Hash == (uint)nameHash);
                    //if(t != null)
                    //               {
                    //	return t.Name;
                    //               }
                    var hash = EbxClassesTypes[i].GetCustomAttribute<HashAttribute>();
                    if (hash != null && (hash.Hash == nameHash))
                    {
                        return EbxClassesTypes[i].Name;
                    }
                }
            }

            return null;

        }

        public static string GetPropertyName(uint nameHash)
        {
            if (EbxClassesAssembly != null)
            {
                if (EbxClassesTypes == null)
                    EbxClassesTypes = EbxClassesAssembly.GetTypes();

                if (!EbxClassesTypes.Any())
                    return null;

                for (var i = 0; i < EbxClassesTypes.Length; i++)
                {
                    var t = EbxClassesTypes[i];
                    PropertyInfo[] properties = t.GetProperties();
                    var hashAttrbProps = properties.ToList().Where(x => x.GetCustomAttribute<HashAttribute>() != null).ToList();
                    foreach (PropertyInfo propertyInfo in hashAttrbProps)
                    {
                        HashAttribute customAttribute = propertyInfo.GetCustomAttribute<HashAttribute>();
                        if (customAttribute != null && customAttribute.Hash == nameHash)
                        {
                            return propertyInfo.Name;
                        }
                    }
                    return null;
                }
            }

            return null;

        }

        public readonly bool IsPatch;

        public EbxSharedTypeDescriptors(string name, bool patch)
        {
            IsPatch = patch;
            if (IsPatch)
            {

            }

            var ebxtys = FileSystem.Instance.GetFileFromMemoryFs(name);
            //         if (File.Exists("Debugging/" + name + ".dat"))
            //             File.Delete("Debugging/" + name + ".dat");

            //Directory.CreateDirectory(name);
            //         using (NativeWriter nativeWriter = new NativeWriter(new FileStream(name + "ebxDict.dat", FileMode.OpenOrCreate)))
            //         {
            //             nativeWriter.Write(ebxtys);
            //         }

            Read(ebxtys, patch);
        }

        public bool HasClass(Guid guid)
        {
            return Mapping.ContainsKey(guid);
        }

        public EbxClass? GetClass(Guid guid)
        {
            if (!Mapping.ContainsKey(guid))
            {
                return null;
            }
            var mappedGuid = Mapping[guid];
            EbxClass? c = Classes.ElementAt(mappedGuid);
            return c;
        }

        public EbxClass? GetClass(int index)
        {
            return Classes[index];
        }

        public Guid? GetGuid(EbxClass classType)
        {
            if (Guids.Count > classType.Index)
                return Guids[classType.Index];

            return null;
        }

        public Guid? GetGuid(int index)
        {
            if (Guids.Count > index)
                return Guids[index];

            return null;
        }

        public EbxField? GetField(int index)
        {
            if (Fields.Count > index)
                return Fields[index];

            return null;
        }

        public void Read(in byte[] data, in bool patch)
        {
            Guids.Clear();
            Mapping.Clear();
            Fields.Clear();
            Classes.Clear();
            using (NativeReader nativeReader = new NativeReader(new MemoryStream(data)))
            {
                nativeReader.Position = 0;

                var magic = nativeReader.ReadUInt();
                ushort stdClassCount = nativeReader.ReadUShort();
                ushort stdFieldCount = nativeReader.ReadUShort();
                for (int i = 0; i < stdFieldCount; i++)
                {
                    uint nameHash = nativeReader.ReadUInt();
                    //string actualfieldname = GetPropertyName(nameHash);
                    EbxField item = new EbxField
                    {
                        NameHash = nameHash,
                        Type = (ushort)(nativeReader.ReadUShort() >> 1),
                        ClassRef = nativeReader.ReadUShort(),
                        DataOffset = nativeReader.ReadUInt(),
                        SecondOffset = nativeReader.ReadUInt()
                    };
                    Fields.Add(item);
                }
                int totalFieldCount = 0;
                EbxClass lastValue = new EbxClass();
                for (int j = 0; j < stdClassCount; j++)
                {
                    Guid guid = nativeReader.ReadGuid(); // 16
                    if (guid.ToString() == "0ddd3260-f601-bf35-b5d8-5ddcfb1d3567")
                    {

                    }

                    Guid b = nativeReader.ReadGuid(); // 16
                    if (guid.Equals(b))
                    {
                        mapping.Add(guid, Classes.Count);
                        classes.Add(null);
                        guids.Add(guid);
                        continue;
                    }
                    else
                    {
                        nativeReader.Position -= 16L;
                        uint nameHash2 = nativeReader.ReadUInt(); // 4
                        uint unkuint2 = nativeReader.ReadUInt(); // 8
                        int fieldCount = nativeReader.ReadByte(); // 9
                        byte alignment = nativeReader.ReadByte(); // 10
                        ushort type = nativeReader.ReadUShort(); // 12
                        uint size = nativeReader.ReadUInt(); // 16
                        if ((alignment & 0x80) != 0)
                        {
                            fieldCount += 256;
                            alignment = (byte)(alignment & 0x7F);
                        }
                        EbxClass value = new EbxClass
                        {
                            //Name = GetClassName(nameHash2),
                            NameHash = nameHash2,
                            FieldIndex = totalFieldCount,
                            FieldCount = (byte)fieldCount,
                            Alignment = alignment,
                            Size = (ushort)size,
                            Type = (ushort)(type >> 1),
                            Index = j
                        };
                        lastValue = value;
                        if (patch)
                        {
                            value.SecondSize = 1;
                        }
                        mapping.Add(guid, Classes.Count);
                        classes.Add(value);
                        guids.Add(guid);
                        totalFieldCount += fieldCount;
                    }
                }
            }
        }
    }
}
