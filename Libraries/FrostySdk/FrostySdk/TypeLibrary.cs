using FMT.FileTools;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace FrostySdk
{
    public static class TypeLibrary
    {
        public static class Reflection
        {
            public static Dictionary<Guid, string> typeInfos = new Dictionary<Guid, string>();

            public static Dictionary<string, dynamic> TypeData = new Dictionary<string, dynamic>();

            public static Dictionary<string, object> TypeObjects = new Dictionary<string, object>();

            public static IEnumerable<Type> ConcreteTypes = new List<Type>();

            public static Dictionary<uint, Type> typeInfosByHash = new Dictionary<uint, Type>();

            private static bool bInitialized = false;

            public static void LoadClassInfoAssets(AssetManager am)
            {
                if (!bInitialized && typeInfos.Count == 0)
                {
                    foreach (EbxAssetEntry item in am.EnumerateEbx("TypeInfoAsset"))
                    {
                        EbxAsset ebx = am.GetEbx(item);
                        if (ebx.RootInstanceGuid != null && !typeInfos.ContainsKey(ebx.RootInstanceGuid))
                        {
                            var typeName = ((dynamic)ebx.RootObject).TypeName;
                            typeInfos.Add(ebx.RootInstanceGuid, typeName);
                            //if (typeName.strValue != null)
                            //{
                            if (!TypeData.ContainsKey(typeName))
                            {
                                var tnAsString = typeName.ToString();
                                if (!string.IsNullOrEmpty(tnAsString))
                                {
                                    TypeData.Add(typeName, (dynamic)ebx.RootObject);
                                }
                            }
                            //}
                        }
                    }
                    ConcreteTypes = GetConcreteTypes();
                    if (ConcreteTypes != null)
                    {
                        foreach (Type type in ConcreteTypes)
                        {
                            GuidAttribute customAttribute = type.GetCustomAttribute<GuidAttribute>();
                            if (customAttribute != null)
                            {
                                string name = type.Name;
                                if (type.GetCustomAttribute<DisplayNameAttribute>() != null)
                                {
                                    name = type.GetCustomAttribute<DisplayNameAttribute>().Name;
                                }
                                if (!typeInfos.ContainsKey(customAttribute.Guid))
                                    typeInfos.Add(customAttribute.Guid, name);
                            }
                        }
                    }
                }
                bInitialized = true;
            }

            public static string LookupType(Guid guid)
            {
                if (!bInitialized)
                {
                    return "";
                }
                if (!typeInfos.ContainsKey(guid))
                {
                    return guid.ToString();
                }
                return typeInfos[guid];
            }

            public static Type LookupType(uint hash)
            {
                if (typeInfosByHash.Count == 0)
                {
                    Type[] concreteTypes = GetConcreteTypes();
                    foreach (Type type in concreteTypes)
                    {
                        var hashAttr = type.GetCustomAttribute<HashAttribute>();
                        if (hashAttr != null)
                        {
                            typeInfosByHash.Add(hashAttr.Hash, type);
                        }
                    }
                }
                if (!typeInfosByHash.ContainsKey(hash))
                {
                    return null;
                }
                return typeInfosByHash[hash];
            }

            public static Type LookupTypeByTypeCode(EbxFieldType typeCode)
            {
                string typeName = typeCode switch
                {
                    EbxFieldType.DbObject => "DbObject",
                    EbxFieldType.Pointer => "DataContainer",
                    EbxFieldType.CString => "CString",
                    EbxFieldType.Boolean => "Boolean",
                    EbxFieldType.Int8 => "Int8",
                    EbxFieldType.UInt8 => "Uint8",
                    EbxFieldType.Int16 => "Int16",
                    EbxFieldType.UInt16 => "Uint16",
                    EbxFieldType.Int32 => "Int32",
                    EbxFieldType.UInt32 => "Uint32",
                    EbxFieldType.Int64 => "Int64",
                    EbxFieldType.UInt64 => "Uint64",
                    EbxFieldType.Float32 => "Float32",
                    EbxFieldType.Float64 => "Float64",
                    EbxFieldType.Guid => "Guid",
                    EbxFieldType.Sha1 => "SHA1",
                    EbxFieldType.ResourceRef => "ResourceRef",
                    EbxFieldType.TypeRef => "TypeRef",
                    EbxFieldType.BoxedValueRef => "BoxedValueRef",
                    _ => null,
                };
                if (typeName == null)
                {
                    return null;
                }
                return LookupType((uint)Fnv1.HashString(typeName));
            }
        }

        private const string ModuleName = "EbxClasses";

        private static string Namespace { get; } = "FrostySdk.Ebx.";

        private static AssemblyBuilder assemblyBuilder { get; set; }

        private static ModuleBuilder moduleBuilder { get; set; }

        /// <summary>
        /// The Profile SDK Assembly
        /// </summary>
        public static Assembly ExistingAssembly { get; set; }

        /// <summary>
        /// The Profile SDK Assembly
        /// </summary>
        public static Assembly ProfileAssembly => ExistingAssembly;


        public static List<TypeBuilder> constructingTypes { get; } = new List<TypeBuilder>();

        public static List<ConstructorBuilder> constructors { get; } = new List<ConstructorBuilder>();

        private static List<Guid> constructingGuids { get; } = new List<Guid>();

        public static ConcurrentDictionary<Guid, Type> guidTypeMapping { get; } = new ConcurrentDictionary<Guid, Type>();

        private static string ProfileSDKLocation { get; } = "SDK/" + ProfileManager.SDKFilename + ".dll";

        public static bool RequestLoadSDK { get; set; } = false;
        public static bool Initialize(bool loadSDK = true)
        {
            RequestLoadSDK = loadSDK;
            try
            {
                FileInfo fileInfo = new FileInfo("TmpProfiles/" + ProfileManager.SDKFilename + ".dll");
                if (fileInfo.Exists)
                {
                    FileInfo fileInfo2 = new FileInfo(ProfileSDKLocation);
                    if (fileInfo2.Exists)
                    {
                        File.Delete(fileInfo2.FullName);
                    }
                    Directory.CreateDirectory("SDK");
                    File.Move(fileInfo.FullName, fileInfo2.FullName);
                    fileInfo.Delete();
                }

                if (loadSDK && ExistingAssembly == null)
                {
                    if (File.Exists(ProfileSDKLocation))
                    {
                        //existingAssembly = Assembly.LoadFrom("Profiles/" + ProfilesLibrary.SDKFilename + ".dll");
                        //existingAssembly = Assembly.UnsafeLoadFrom(ProfileSDKLocation);
                        ExistingAssembly = Assembly.LoadFrom(ProfileSDKLocation);
                    }
                    else
                    {
                        //throw new FileNotFoundException($"Unable to find SDK ({ProfilesLibrary.SDKFilename}.dll)");
                    }

                    AssemblyName name = new AssemblyName("EbxClasses");
                    //assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
                    assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule("EbxClasses");
                }
                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static uint GetSdkVersion()
        {
            if (ExistingAssembly == null)
            {
                return uint.MaxValue;
            }
            return (uint)(ExistingAssembly.GetCustomAttribute<SdkVersionAttribute>()?.Version ?? (-1));
        }

        public static DbObject LoadClassesSDK(Stream sdkStream)
        {
            DbObject dbObject = null;
            using (NativeReader nativeReader = new NativeReader(sdkStream))
            {
                dbObject = new DbObject(bObject: false);
                while (nativeReader.Position < nativeReader.Length)
                {
                    string text = nativeReader.ReadLine().Trim('\t', ' ');
                    if (text.StartsWith("BeginClass"))
                    {
                        string newValue = text.Split(' ')[1];
                        DbObject dbObject2 = new DbObject();
                        dbObject2.SetValue("name", newValue);
                        DbObject dbObject3 = new DbObject(bObject: false);
                        dbObject2.SetValue("fields", dbObject3);
                        while (!text.StartsWith("EndClass"))
                        {
                            text = nativeReader.ReadLine().Trim('\t', ' ');
                            if (!text.StartsWith("//"))
                            {
                                if (text.StartsWith("BeginFields"))
                                {
                                    _ = text.Split(' ')[1];
                                    while (!text.StartsWith("EndFields"))
                                    {
                                        text = nativeReader.ReadLine().Trim('\t', ' ');
                                        if (text.StartsWith("BeginField "))
                                        {
                                            string newValue2 = text.Split(' ')[1];
                                            DbObject dbObject4 = new DbObject();
                                            dbObject4.SetValue("name", newValue2);
                                            while (!text.EndsWith("EndField"))
                                            {
                                                text = nativeReader.ReadLine().Trim('\t', ' ');
                                                string[] array = SplitOnce(text);
                                                if (array[0].StartsWith("DisplayName"))
                                                {
                                                    dbObject4.SetValue("displayName", array[1]);
                                                }
                                                else if (array[0].StartsWith("Description"))
                                                {
                                                    dbObject4.SetValue("description", array[1]);
                                                }
                                                else if (array[0].StartsWith("Category"))
                                                {
                                                    dbObject4.SetValue("category", array[1]);
                                                }
                                                else if (array[0].StartsWith("Editor"))
                                                {
                                                    dbObject4.SetValue("editor", array[1]);
                                                }
                                                else if (array[0].StartsWith("Transient"))
                                                {
                                                    dbObject4.SetValue("transient", bool.Parse(array[1]));
                                                }
                                                else if (array[0].StartsWith("ReadOnly"))
                                                {
                                                    dbObject4.SetValue("readOnly", bool.Parse(array[1]));
                                                }
                                                else if (array[0].StartsWith("Reference"))
                                                {
                                                    dbObject4.SetValue("reference", bool.Parse(array[1]));
                                                }
                                                else if (array[0].StartsWith("Added"))
                                                {
                                                    dbObject4.SetValue("added", bool.Parse(array[1]));
                                                }
                                                else if (array[0].StartsWith("Hidden"))
                                                {
                                                    dbObject4.SetValue("hidden", bool.Parse(array[1]));
                                                }
                                                else if (array[0].StartsWith("Index"))
                                                {
                                                    dbObject4.SetValue("index", int.Parse(array[1]));
                                                }
                                                else if (array[0].StartsWith("HideChildren"))
                                                {
                                                    dbObject4.SetValue("hideChildren", bool.Parse(array[1]));
                                                }
                                                else if (array[0].StartsWith("InterfaceType"))
                                                {
                                                    switch (int.Parse(array[1]))
                                                    {
                                                        case 0:
                                                            dbObject4.SetValue("property", 0);
                                                            break;
                                                        case 1:
                                                            dbObject4.SetValue("property", 1);
                                                            break;
                                                        case 2:
                                                            dbObject4.SetValue("event", 0);
                                                            break;
                                                        case 3:
                                                            dbObject4.SetValue("event", 1);
                                                            break;
                                                        case 4:
                                                            dbObject4.SetValue("link", 0);
                                                            break;
                                                        case 5:
                                                            dbObject4.SetValue("link", 1);
                                                            break;
                                                    }
                                                }
                                                else if (array[0].StartsWith("Type"))
                                                {
                                                    DbObject dbObject5 = new DbObject();
                                                    string[] array2 = array[1].Split(',');
                                                    EbxFieldType ebxFieldType = (EbxFieldType)Enum.Parse(typeof(EbxFieldType), array2[0]);
                                                    dbObject5.SetValue("flags", (int)ebxFieldType);
                                                    if (array2.Length > 1 && array2[1] != "None")
                                                    {
                                                        dbObject5.SetValue("baseType", array2[1]);
                                                    }
                                                    if (array2.Length > 2)
                                                    {
                                                        EbxFieldType ebxFieldType2 = (EbxFieldType)Enum.Parse(typeof(EbxFieldType), array2[2]);
                                                        dbObject5.SetValue("arrayType", (int)ebxFieldType2);
                                                    }
                                                    dbObject4.SetValue("type", dbObject5);
                                                }
                                                else if (array[0].StartsWith("Version"))
                                                {
                                                    string[] array3 = array[1].Split(',');
                                                    DbObject dbObject6 = new DbObject(bObject: false);
                                                    string[] array4 = array3;
                                                    foreach (string s in array4)
                                                    {
                                                        dbObject6.Add(int.Parse(s));
                                                    }
                                                    dbObject4.SetValue("version", dbObject6);
                                                }
                                                else if (array[0].StartsWith("BeginAccessor"))
                                                {
                                                    string text2 = "";
                                                    while (!text.StartsWith("EndAccessor"))
                                                    {
                                                        text = nativeReader.ReadLine().Trim('\t', ' ');
                                                        if (text != "EndAccessor")
                                                        {
                                                            text2 = text2 + text + "\n";
                                                        }
                                                    }
                                                    dbObject4.SetValue("accessor", text2);
                                                }
                                            }
                                            dbObject3.Add(dbObject4);
                                        }
                                    }
                                }
                                else if (text.StartsWith("BeginFunctions"))
                                {
                                    string text3 = "";
                                    while (!text.StartsWith("EndFunctions"))
                                    {
                                        text = nativeReader.ReadLine().Trim('\t', ' ');
                                        if (text != "EndFunctions")
                                        {
                                            text3 = text3 + text + "\n";
                                        }
                                    }
                                    dbObject2.SetValue("functions", text3);
                                }
                                else if (text.StartsWith("BeginAttributes"))
                                {
                                    string text4 = "";
                                    while (!text.StartsWith("EndAttributes"))
                                    {
                                        text = nativeReader.ReadLine().Trim('\t', ' ');
                                        if (text != "EndAttributes")
                                        {
                                            text4 = text4 + text + "\n";
                                        }
                                    }
                                    dbObject2.SetValue("attributes", text4);
                                }
                                else if (text.StartsWith("BeginConstructor"))
                                {
                                    string text5 = "";
                                    while (!text.StartsWith("EndConstructor"))
                                    {
                                        text = nativeReader.ReadLine().Trim('\t', ' ');
                                        if (text != "EndConstructor")
                                        {
                                            text5 = text5 + text + "\n";
                                        }
                                    }
                                    dbObject2.SetValue("constructor", text5);
                                }
                                else
                                {
                                    string[] array5 = SplitOnce(text);
                                    if (array5[0].StartsWith("DisplayName"))
                                    {
                                        dbObject2.SetValue("displayName", array5[1]);
                                    }
                                    else if (array5[0].StartsWith("Description"))
                                    {
                                        dbObject2.SetValue("description", array5[1]);
                                    }
                                    else if (array5[0].StartsWith("ValueConverter"))
                                    {
                                        dbObject2.SetValue("valueConverter", array5[1]);
                                    }
                                    else if (array5[0].StartsWith("Alignment"))
                                    {
                                        dbObject2.SetValue("alignment", int.Parse(array5[1]));
                                    }
                                    else if (array5[0].StartsWith("Abstract"))
                                    {
                                        dbObject2.SetValue("abstract", bool.Parse(array5[1]));
                                    }
                                    else if (array5[0].StartsWith("Inline"))
                                    {
                                        dbObject2.SetValue("inline", bool.Parse(array5[1]));
                                    }
                                    else if (array5[0].StartsWith("Icon"))
                                    {
                                        dbObject2.SetValue("icon", array5[1]);
                                    }
                                    else if (array5[0].StartsWith("Realm"))
                                    {
                                        dbObject2.SetValue("realm", array5[1]);
                                    }
                                }
                            }
                        }
                        dbObject.Add(dbObject2);
                    }
                }
                return dbObject;
            }
        }

        private static string[] SplitOnce(string str)
        {
            int num = str.IndexOf('=');
            if (num != -1)
            {
                return new string[2]
                {
                    str.Remove(num),
                    str.Remove(0, num + 1)
                };
            }
            return new string[1]
            {
                str
            };
        }

        private static Type GetTypeFromEbxType(EbxFieldType inType, string baseType, int arrayType = -1)
        {
            Type result = null;
            switch (inType)
            {
                case EbxFieldType.Struct:
                    result = AddType(baseType);
                    break;
                case EbxFieldType.String:
                    result = typeof(string);
                    break;
                case EbxFieldType.Int8:
                    result = typeof(sbyte);
                    break;
                case EbxFieldType.UInt8:
                    result = typeof(byte);
                    break;
                case EbxFieldType.Boolean:
                    result = typeof(bool);
                    break;
                case EbxFieldType.UInt16:
                    result = typeof(ushort);
                    break;
                case EbxFieldType.Int16:
                    result = typeof(short);
                    break;
                case EbxFieldType.UInt32:
                    result = typeof(uint);
                    break;
                case EbxFieldType.Int32:
                    result = typeof(int);
                    break;
                case EbxFieldType.UInt64:
                    result = typeof(ulong);
                    break;
                case EbxFieldType.Int64:
                    result = typeof(long);
                    break;
                case EbxFieldType.Float32:
                    result = typeof(float);
                    break;
                case EbxFieldType.Float64:
                    result = typeof(double);
                    break;
                case EbxFieldType.Pointer:
                    result = typeof(PointerRef);
                    break;
                case EbxFieldType.Guid:
                    result = typeof(Guid);
                    break;
                case EbxFieldType.Sha1:
                    result = typeof(Sha1);
                    break;
                case EbxFieldType.CString:
                    result = typeof(CString);
                    break;
                case EbxFieldType.ResourceRef:
                    result = typeof(ResourceRef);
                    break;
                case EbxFieldType.FileRef:
                    result = typeof(FileRef);
                    break;
                case EbxFieldType.TypeRef:
                    result = typeof(TypeRef);
                    break;
                case EbxFieldType.Array:
                    result = typeof(List<>).MakeGenericType(GetTypeFromEbxType((EbxFieldType)((arrayType >> 4) & 0x1F), baseType));
                    break;
                case EbxFieldType.Enum:
                    result = AddType(baseType);
                    break;
                case EbxFieldType.DbObject:
                    result = null;
                    break;
                case EbxFieldType.BoxedValueRef:
                    result = typeof(BoxedValueRef);
                    break;
            }
            return result;
        }

        public static bool IsSubClassOf(object obj, string name)
        {
            return IsSubClassOf(obj.GetType(), name);
        }

        public static bool IsSubClassOf(Type type, string name)
        {
            Type type2 = GetType(name);
            if (type2 == null)
            {
                return false;
            }
            if (!type.IsSubclassOf(type2))
            {
                return type == type2;
            }
            return true;
        }

        public static bool IsSubClassOf(string type, string name)
        {
            Type type2 = GetType(type);
            if (type2 == null)
            {
                return false;
            }
            return IsSubClassOf(type2, name);
        }

        public static Type[] GetTypes(Type type)
        {
            return GetTypes(type.Name);
        }

        public static Type[] GetTypes(string name)
        {
            List<Type> list = new List<Type>();
            Type[] array = (ExistingAssembly != null) ? ExistingAssembly.GetTypes() : null;
            if (array != null)
            {
                Type[] array2 = array;
                foreach (Type type in array2)
                {
                    if (IsSubClassOf(type, name))
                    {
                        list.Add(type);
                    }
                }
            }
            array = moduleBuilder.Assembly.GetTypes();
            if (array != null)
            {
                Type[] array2 = array;
                foreach (Type type2 in array2)
                {
                    if (IsSubClassOf(type2, name))
                    {
                        list.Add(type2);
                    }
                }
            }
            return list.ToArray();
        }

        public static Type[] GetConcreteTypes()
        {
            if (ExistingAssembly == null)
            {
                return null;
            }
            return ExistingAssembly.GetTypes();
        }

        public static dynamic CreateObject(string name)
        {
            Type type = GetType(name);
            if (type == null)
            {
                return null;
            }
            return CreateObject(type);
        }

        public static dynamic CreateObject(Guid guid)
        {
            Type type = GetType(guid);
            if (type == null)
            {
                return null;
            }
            return CreateObject(type);
        }

        public static Type AddEnum(string name, List<Tuple<string, int>> values, EbxClass classInfo)
        {
            Type type = GetType(name);
            if (type != null)
            {
                return type;
            }
            EnumBuilder enumBuilder = moduleBuilder.DefineEnum("FrostySdk.Ebx." + name, TypeAttributes.Public, typeof(int));
            for (int i = 0; i < values.Count; i++)
            {
                enumBuilder.DefineLiteral(values[i].Item1, values[i].Item2);
            }
            AddEnumMeta(enumBuilder, classInfo.Type, classInfo.Alignment, classInfo.Size, classInfo.Namespace);
            return enumBuilder.CreateTypeInfo();// CreateType();
        }

        public static Type AddType(string name, Guid? guid = null)
        {
            if (name == "" || name == null)
            {
                return null;
            }
            return CreateType(name, guid);
        }

        public static Type FinalizeStruct(string name, List<FieldType> fields, EbxClass classInfo)
        {
            return FinalizeType(name, fields, typeof(object), classInfo);
        }

        public static Type FinalizeClass(string name, List<FieldType> fields, Type parentType, EbxClass classInfo, MetaDataType? metaData = null)
        {
            return FinalizeType(name, fields, parentType, classInfo, metaData);
        }

        public static Type GetType(string name)
        {
            Type type = (ExistingAssembly != null) ? ExistingAssembly.GetType("FrostySdk.Ebx." + name) : null;
            if (type != null)
            {
                return type;
            }
            return moduleBuilder.Assembly.GetType("FrostySdk.Ebx." + name);
        }


        public static Type GetType(Guid guid)
        {
            if (ExistingAssembly == null)
            {
                Initialize(true);
            }

            if (ExistingAssembly != null)
            {
                // FIFA23. Hard coding TextureAsset for testing until SDK Gen.
                //if (ProfilesLibrary.LoadedProfile.Name.Equals("FIFA23", StringComparison.OrdinalIgnoreCase)
                //	&& guid.ToString() == "cfb542e8-15ce-28c4-de4d-2f0810f998dc")
                //{
                //	return ExistingAssembly.GetExportedTypes().LastOrDefault(x => x.FullName.Equals("FrostySdk.Ebx.TextureAsset", StringComparison.OrdinalIgnoreCase));
                //}

                if (guidTypeMapping.TryGetValue(guid, out Type v))
                    return v;

                if (guidTypeMapping.Count == 0)
                {
                    var ExportedTypes = ExistingAssembly.GetExportedTypes().Where(x => x.GetCustomAttributes<TypeInfoGuidAttribute>().Any()).ToList();

                    foreach (Type type in ExportedTypes)
                    {
                        foreach (TypeInfoGuidAttribute customAttribute in type.GetCustomAttributes<TypeInfoGuidAttribute>())
                        {
                            guidTypeMapping.TryAdd(customAttribute.Guid, type);
                        }
                    }

                    var ExportedTypes2 = ExistingAssembly.GetExportedTypes().Where(x => x.GetCustomAttribute<GuidAttribute>() != null).ToList();

                    foreach (Type type in ExportedTypes)
                    {
                        guidTypeMapping.TryAdd(type.GetCustomAttribute<GuidAttribute>().Guid, type);
                    }
                }

                if (guidTypeMapping.TryGetValue(guid, out v))
                    return v;

            }
            return null;
        }

        public static Type GetType(uint hash)
        {
            return Reflection.LookupType(hash);
        }

        public static dynamic CreateObject(Type inType)
        {
            return Activator.CreateInstance(inType);
        }

        public static void InitializeArrays(object obj)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (
                    (propertyInfo.PropertyType.Namespace == "FrostySdk.Ebx" 
                    || propertyInfo.PropertyType.Namespace == "FrostySdk.Ebx")
                    && propertyInfo.PropertyType.BaseType != typeof(Enum))
                {
                    object value = propertyInfo.GetValue(obj);
                    InitializeArrays(value);
                    propertyInfo.SetValue(obj, value);
                }
                else if (propertyInfo.PropertyType.Name == "List`1")
                {
                    object value2 = propertyInfo.PropertyType.GetConstructor(Type.EmptyTypes).Invoke(null);
                    propertyInfo.SetValue(obj, value2);
                }
            }
        }

        private static Type CreateType(string name, Guid? guid = null)
        {
            Type type = GetType(name);
            if (type != null)
            {
                return type;
            }
            int num = constructingTypes.FindIndex((TypeBuilder a) => a.Name == name);
            if (num != -1)
            {
                return constructingTypes[num];
            }
            TypeBuilder typeBuilder = moduleBuilder.DefineType("FrostySdk.Ebx." + name, TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
            ConstructorBuilder item = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            constructingTypes.Add(typeBuilder);
            constructingGuids.Add(guid.HasValue ? guid.Value : Guid.Empty);
            constructors.Add(item);
            return null;
        }

        private static Type FinalizeType(string name, List<FieldType> fields, Type parentType, EbxClass classInfo, MetaDataType? metaData = null)
        {
            int index = constructingTypes.FindIndex((TypeBuilder a) => a.Name == name);
            TypeBuilder typeBuilder = constructingTypes[index];
            ConstructorBuilder constructorBuilder = constructors[index];
            Guid? guid = constructingGuids[index];
            constructingTypes.RemoveAt(index);
            constructors.RemoveAt(index);
            typeBuilder.SetParent(parentType);
            if (parentType == null)
            {
                FieldType ft = new FieldType("_Guid", typeof(Guid), null, null);
                CreateProperty(typeBuilder, ft, createGetter: false, createSetter: false);
                parentType = typeof(object);
            }
            ILGenerator iLGenerator = constructorBuilder.GetILGenerator();
            ConstructorInfo constructorInfo = null;
            if (parentType is TypeBuilder)
            {
                int index2 = constructingTypes.IndexOf((TypeBuilder)parentType);
                constructorInfo = constructors[index2];
            }
            else
            {
                constructorInfo = parentType.GetConstructor(Type.EmptyTypes);
            }
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Call, constructorInfo);
            iLGenerator.Emit(OpCodes.Nop);
            iLGenerator.Emit(OpCodes.Nop);
            foreach (FieldType field2 in fields)
            {
                if (!(parentType != null) || !(parentType.GetProperty(field2.Name) != null))
                {
                    FieldBuilder field = CreateProperty(typeBuilder, field2, createGetter: true, createSetter: true);
                    if (field2.Type.Name == "List`1")
                    {
                        Type obj = field2.Type.GenericTypeArguments[0];
                        ConstructorInfo constructorInfo2 = null;
                        constructorInfo2 = ((!(obj is TypeBuilder)) ? field2.Type.GetConstructor(Type.EmptyTypes) : TypeBuilder.GetConstructor(field2.Type, typeof(List<>).GetConstructor(Type.EmptyTypes)));
                        iLGenerator.Emit(OpCodes.Ldarg_0);
                        iLGenerator.Emit(OpCodes.Newobj, constructorInfo2);
                        iLGenerator.Emit(OpCodes.Stfld, field);
                    }
                    else if (field2.Type.Namespace == "FrostySdk.Ebx" && field2.Type.BaseType != typeof(Enum) && field2.Type.BaseType != typeof(ValueType))
                    {
                        ConstructorInfo constructorInfo3 = null;
                        if (field2.Type is TypeBuilder)
                        {
                            int index3 = constructingTypes.IndexOf((TypeBuilder)field2.Type);
                            constructorInfo3 = constructors[index3];
                        }
                        else
                        {
                            constructorInfo3 = field2.Type.GetConstructor(Type.EmptyTypes);
                        }
                        iLGenerator.Emit(OpCodes.Ldarg_0);
                        iLGenerator.Emit(OpCodes.Newobj, constructorInfo3);
                        iLGenerator.Emit(OpCodes.Stfld, field);
                    }
                }
            }
            iLGenerator.Emit(OpCodes.Ret);
            AddClassMeta(typeBuilder, classInfo.Type, classInfo.Alignment, classInfo.Size, classInfo.Namespace, guid);
            if (metaData.HasValue)
            {
                MetaDataType value = metaData.Value;
                if (value.ValueConverter != "")
                {
                    CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(typeof(ClassConverterAttribute).GetConstructor(new Type[1]
                    {
                        typeof(string)
                    }), new object[1]
                    {
                        value.ValueConverter
                    });
                    typeBuilder.SetCustomAttribute(customAttribute);
                }
                if (value.Description != "")
                {
                    CustomAttributeBuilder customAttribute2 = new CustomAttributeBuilder(typeof(DescriptionAttribute).GetConstructor(new Type[1]
                    {
                        typeof(string)
                    }), new object[1]
                    {
                        value.Description
                    });
                    typeBuilder.SetCustomAttribute(customAttribute2);
                }
                if (value.IsInline)
                {
                    CustomAttributeBuilder customAttribute3 = new CustomAttributeBuilder(typeof(IsInlineAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
                    typeBuilder.SetCustomAttribute(customAttribute3);
                }
                if (value.IsAbstract)
                {
                    CustomAttributeBuilder customAttribute4 = new CustomAttributeBuilder(typeof(IsAbstractAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
                    typeBuilder.SetCustomAttribute(customAttribute4);
                }
            }
            return typeBuilder.CreateType();
        }

        private static FieldBuilder CreateProperty(TypeBuilder tb, FieldType ft, bool createGetter, bool createSetter)
        {
            FieldBuilder fieldBuilder = tb.DefineField("_" + ft.Name, ft.Type, FieldAttributes.PrivateScope);
            if (!createGetter && !createSetter)
            {
                return fieldBuilder;
            }
            PropertyBuilder propertyBuilder = tb.DefineProperty(ft.Name, PropertyAttributes.HasDefault, ft.Type, null);
            if (ft.ArrayInfo.HasValue)
            {
                AddFieldMeta(propertyBuilder, ft.FieldInfo.Value.Type, ft.FieldInfo.Value.DataOffset, ft.BaseType, isArray: true, ft.ArrayInfo.Value.Type);
            }
            else if (ft.FieldInfo.HasValue)
            {
                AddFieldMeta(propertyBuilder, ft.FieldInfo.Value.Type, ft.FieldInfo.Value.DataOffset, ft.BaseType, isArray: false, 0);
            }
            if (createGetter)
            {
                MethodBuilder methodBuilder = tb.DefineMethod("get_" + ft.Name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName, ft.Type, Type.EmptyTypes);
                ILGenerator iLGenerator = methodBuilder.GetILGenerator();
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
                iLGenerator.Emit(OpCodes.Ret);
                propertyBuilder.SetGetMethod(methodBuilder);
            }
            if (createSetter)
            {
                MethodBuilder methodBuilder2 = tb.DefineMethod("set_" + ft.Name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName, null, new Type[1]
                {
                    ft.Type
                });
                ILGenerator iLGenerator2 = methodBuilder2.GetILGenerator();
                Label loc = iLGenerator2.DefineLabel();
                Label loc2 = iLGenerator2.DefineLabel();
                iLGenerator2.MarkLabel(loc);
                iLGenerator2.Emit(OpCodes.Ldarg_0);
                iLGenerator2.Emit(OpCodes.Ldarg_1);
                iLGenerator2.Emit(OpCodes.Stfld, fieldBuilder);
                iLGenerator2.Emit(OpCodes.Nop);
                iLGenerator2.MarkLabel(loc2);
                iLGenerator2.Emit(OpCodes.Ret);
                propertyBuilder.SetSetMethod(methodBuilder2);
            }
            if (ft.MetaData.HasValue)
            {
                MetaDataType value = ft.MetaData.Value;
                if (value.DisplayName != "")
                {
                    CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(typeof(DisplayNameAttribute).GetConstructor(new Type[1]
                    {
                        typeof(string)
                    }), new object[1]
                    {
                        value.DisplayName
                    });
                    propertyBuilder.SetCustomAttribute(customAttribute);
                }
                if (value.Description != "")
                {
                    CustomAttributeBuilder customAttribute2 = new CustomAttributeBuilder(typeof(DescriptionAttribute).GetConstructor(new Type[1]
                    {
                        typeof(string)
                    }), new object[1]
                    {
                        value.Description
                    });
                    propertyBuilder.SetCustomAttribute(customAttribute2);
                }
                if (value.Editor != "")
                {
                    CustomAttributeBuilder customAttribute3 = new CustomAttributeBuilder(typeof(EditorAttribute).GetConstructor(new Type[1]
                    {
                        typeof(string)
                    }), new object[1]
                    {
                        value.Editor
                    });
                    propertyBuilder.SetCustomAttribute(customAttribute3);
                }
                if (value.IsReadOnly)
                {
                    CustomAttributeBuilder customAttribute4 = new CustomAttributeBuilder(typeof(IsReadOnlyAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
                    propertyBuilder.SetCustomAttribute(customAttribute4);
                }
                if (value.IsReference)
                {
                    CustomAttributeBuilder customAttribute5 = new CustomAttributeBuilder(typeof(IsReferenceAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
                    propertyBuilder.SetCustomAttribute(customAttribute5);
                }
                if (value.IsProperty)
                {
                    CustomAttributeBuilder customAttribute6 = new CustomAttributeBuilder(typeof(IsPropertyAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
                    propertyBuilder.SetCustomAttribute(customAttribute6);
                }
            }
            return fieldBuilder;
        }

        private static void AddClassMeta(TypeBuilder tb, ushort flags, byte alignment, ushort size, string nameSpace, Guid? guid = null)
        {
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(typeof(EbxClassMetaAttribute).GetConstructor(new Type[4]
            {
                typeof(ushort),
                typeof(byte),
                typeof(ushort),
                typeof(string)
            }), new object[4]
            {
                flags,
                alignment,
                size,
                nameSpace
            });
            tb.SetCustomAttribute(customAttribute);
            if (guid.HasValue && guid != Guid.Empty)
            {
                customAttribute = new CustomAttributeBuilder(typeof(TypeInfoGuidAttribute).GetConstructor(new Type[1]
                {
                    typeof(Guid)
                }), new object[1]
                {
                    guid
                });
                tb.SetCustomAttribute(customAttribute);
            }
        }

        private static void AddEnumMeta(EnumBuilder eb, ushort flags, byte alignment, ushort size, string nameSpace)
        {
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(typeof(EbxClassMetaAttribute).GetConstructor(new Type[4]
            {
                typeof(ushort),
                typeof(byte),
                typeof(ushort),
                typeof(string)
            }), new object[4]
            {
                flags,
                alignment,
                size,
                nameSpace
            });
            eb.SetCustomAttribute(customAttribute);
        }

        private static void AddFieldMeta(PropertyBuilder pb, ushort flags, uint offset, Type baseType, bool isArray, ushort arrayFlags)
        {
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(typeof(EbxFieldMetaAttribute).GetConstructor(new Type[5]
            {
                typeof(ushort),
                typeof(uint),
                typeof(Type),
                typeof(bool),
                typeof(ushort)
            }), new object[5]
            {
                flags,
                offset,
                baseType,
                isArray,
                arrayFlags
            });
            pb.SetCustomAttribute(customAttribute);
        }

        public static Dictionary<string, Type> CachedTypes = new Dictionary<string, Type>();

        public static object LoadTypeByName(string className, params object[] args)
        {
            if (CachedTypes.Any() && CachedTypes.ContainsKey(className))
            {
                var cachedType = CachedTypes[className];
                return Activator.CreateInstance(type: cachedType, args: args);
            }

            if(AssetManager.Instance == null)
            {
                new AssetManager();
            }
            AssetManager.Instance.InitialisePlugins();
            
            
            var pluginInstance = AssetManager.LoadTypeFromPlugin2(className, args);
            if (pluginInstance != null)
            {
                CachedTypes.Add(className, pluginInstance.GetType());
            }
            else
            {
                IEnumerable<Assembly> currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                var assembly = currentAssemblies.FirstOrDefault(x => x.GetTypes().Any(x => x.FullName.Contains(className, StringComparison.OrdinalIgnoreCase)));
                var t = assembly.GetTypes().FirstOrDefault(x => x.FullName.Contains(className, StringComparison.OrdinalIgnoreCase));
                if (t != null)
                {
                    CachedTypes.Add(className, t);
                    return Activator.CreateInstance(type: t, args: args);
                }
            }

            throw new ArgumentNullException("Unable to find Class");
        }
    }
}
