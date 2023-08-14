using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using SdkGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SDKGenerator
{
    public class ClassesSdkCreatorV2
    {
        public static long offset;

        private List<IClassInfo> classInfos = new List<IClassInfo>();

        private List<string> alreadyProcessedClasses = new List<string>();

        private Dictionary<long, IClassInfo> offsetClassInfoMapping = new Dictionary<long, IClassInfo>();

        private DbObject classList { get; set; }

        private DbObject classMetaList { get; set; }

        private SdkUpdateState state { get; set; }

        public ClassesSdkCreatorV2(SdkUpdateState inState)
        {
            state = inState;
        }

        public bool GatherTypeInfos(SdkUpdateTask task)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var names = executingAssembly.GetManifestResourceNames();

            if (!string.IsNullOrEmpty(ProfileManager.SDKClassesFile))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames().SingleOrDefault(x => x.EndsWith(ProfileManager.SDKClassesFile));
                System.IO.Stream classesStream = null;
                if (!string.IsNullOrEmpty(resourceName))
                {
                    classesStream = assembly.GetManifestResourceStream(resourceName);
                }
                else
                {
                    var classesTxtFilePath = AppContext.BaseDirectory + "/SdkGen/" + ProfileManager.SDKClassesFile;
                    classesStream = new FileStream(classesTxtFilePath, FileMode.Open);
                }
                if (classesStream != null)
                {
                    using (classesStream)
                        classMetaList = TypeLibrary.LoadClassesSDK(classesStream);

                }
            }
            else
            {
                throw new ArgumentNullException("SDK Classes File not provided in the Profile. Please set a file!");
            }

            classList = DumpClasses(task);
            if (classList != null)
            {
                Trace.WriteLine($"{classList.Count} Classes Dumped");
                Debug.WriteLine($"{classList.Count} Classes Dumped");
                Console.WriteLine($"{classList.Count} Classes Dumped");

                return classList.Count > 0;
            }
            return false;
        }

        public bool CrossReferenceAssets(SdkUpdateTask task)
        {
            // Data must be loaded and cached before SDK is built

            List<Guid> existingClasses = new List<Guid>();
            foreach (var f in FileSystem.Instance.memoryFs.Keys
                .Where(x => x.Contains("SharedTypeDescriptor", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Length)
                )
            {
                IEbxSharedTypeDescriptor std = null;
                if (!string.IsNullOrEmpty(ProfileManager.EBXTypeDescriptor))
                {
                    std = (IEbxSharedTypeDescriptor)AssetManager.LoadTypeByName(ProfileManager.EBXTypeDescriptor
                            , f, f.Contains("patch", StringComparison.OrdinalIgnoreCase));
                }
                std.ReflectionTypeDescripter = true;
                std.Read(FileSystem.Instance.GetFileFromMemoryFs(f), f.Contains("patch", StringComparison.OrdinalIgnoreCase));

                for (int k = 0; k < std.Classes.Count; k++)
                {
                    if (std.Classes[k].HasValue)
                    {
                        EbxClass stdClass = std.Classes[k].Value;
                        Guid guid = std.Guids[k];
                        DbObject dboClass = (DbObject)classList.list.SingleOrDefault(x => ((DbObject)x).GetValue<uint>("nameHash") == stdClass.NameHash);
                        if (dboClass == null)
                            continue;

                        if (!dboClass.HasValue("typeInfoGuid"))
                        {
                            dboClass.SetValue("typeInfoGuid", DbObject.CreateList());
                        }
                        if (dboClass.GetValue<DbObject>("typeInfoGuid").FindIndex((object a) => (Guid)a == guid) == -1)
                        {
                            dboClass.GetValue<DbObject>("typeInfoGuid").Add(guid);
                        }
                    }
                }
            }
            return true;
        }

        private DbObject DumpClasses(SdkUpdateTask task)
        {
            MemoryReader memoryReader = null;
            string typeStr = "SdkGenerator.BaseInfo.ClassInfo";
            if (!string.IsNullOrEmpty(ProfileManager.SDKGeneratorClassInfoType))
                typeStr = ProfileManager.SDKGeneratorClassInfoType;

            long typeInfoOffset = state.TypeInfoOffset;
            memoryReader = new MemoryReader(state.Process, typeInfoOffset);
            offsetClassInfoMapping.Clear();
            classInfos.Clear();
            alreadyProcessedClasses.Clear();
            offset = typeInfoOffset;
            int num = 0;

            while (offset != 0L)
            {
                task.StatusMessage = $"Found {++num} type(s)";
                memoryReader.Position = offset;
                var t = TypeLibrary.LoadTypeByName(typeStr);
                IClassInfo classInfo = (IClassInfo)t;
                classInfo.Read(memoryReader);

                if (!offsetClassInfoMapping.ContainsKey(typeInfoOffset))
                {
                    classInfos.Add(classInfo);
                    offsetClassInfoMapping.Add(typeInfoOffset, classInfo);
                }
                else
                    break;

                if (classInfo.nextOffset != 0L)
                {
                    offset = classInfo.nextOffset;
                    typeInfoOffset = offset;
                }
                else if (offset != 0)
                {
                    typeInfoOffset = offset;
                }
            }
            Debug.WriteLine(task.StatusMessage);

            memoryReader.Dispose();
            memoryReader = null;

            DbObject result = new DbObject(bObject: false);
            classInfos.Sort((IClassInfo a, IClassInfo b) => a.typeInfo.name.CompareTo(b.typeInfo.name));

            foreach (IClassInfo classInfo2 in classInfos)
            {
                if (classInfo2.typeInfo.name.Equals("RenderFormat", StringComparison.OrdinalIgnoreCase))
                {

                }

                if (classInfo2.typeInfo.Type == 2
                    || classInfo2.typeInfo.Type == 3
                    || classInfo2.typeInfo.Type == 8
                    || classInfo2.typeInfo.Type == 24
                    || classInfo2.typeInfo.Type == 27)
                {
                    if (classInfo2.typeInfo.Type == 27)
                    {
                        classInfo2.typeInfo.flags = 48;
                    }
                    CreateClassObject(classInfo2, ref result);
                }
                else if (classInfo2.typeInfo.Type != 4)
                {
                    CreateBasicClassObject(classInfo2, ref result);
                }
            }
            return result;
        }

        private void CreateBasicClassObject(IClassInfo classInfo, ref DbObject classList)
        {
            int alignment = classInfo.typeInfo.alignment;
            int size = (int)classInfo.typeInfo.size;
            DbObject dbObject = DbObject.CreateObject();
            dbObject.SetValue("name", classInfo.typeInfo.name);
            dbObject.SetValue("type", classInfo.typeInfo.Type);
            dbObject.SetValue("flags", (int)classInfo.typeInfo.flags);
            dbObject.SetValue("alignment", alignment);
            dbObject.SetValue("size", size);
            dbObject.SetValue("runtimeSize", size);
            if (classInfo.typeInfo.guid != Guid.Empty)
            {
                dbObject.SetValue("guid", classInfo.typeInfo.guid);
            }
            dbObject.SetValue("namespace", classInfo.typeInfo.nameSpace);
            dbObject.SetValue("fields", DbObject.CreateList());
            dbObject.SetValue("parent", "");
            dbObject.SetValue("basic", true);
            classInfo.typeInfo.Modify(dbObject);
            classList.Add(dbObject);
        }

        private void CreateClassObject(IClassInfo classInfo, ref DbObject classList)
        {
            if (!alreadyProcessedClasses.Contains(classInfo.typeInfo.name))
            {
                IClassInfo classInfo2 = offsetClassInfoMapping.ContainsKey(classInfo.parentClass) ? offsetClassInfoMapping[classInfo.parentClass] : null;
                if (classInfo2 != null)
                {
                    CreateClassObject(classInfo2, ref classList);
                }
                int alignment = classInfo.typeInfo.alignment;
                int size = (int)classInfo.typeInfo.size;
                DbObject dbObject = new DbObject();
                dbObject.AddValue("name", classInfo.typeInfo.name);
                dbObject.AddValue("nameHash", classInfo.typeInfo.nameHash);
                if (classInfo.typeInfo.name.Contains("actor_movement"))
                {

                }
                dbObject.AddValue("parent", (classInfo2 != null) ? classInfo2.typeInfo.name : "");
                dbObject.AddValue("type", classInfo.typeInfo.Type);
                dbObject.AddValue("flags", (int)classInfo.typeInfo.flags);
                dbObject.AddValue("alignment", alignment);
                dbObject.AddValue("size", size);
                dbObject.AddValue("runtimeSize", size);
                dbObject.AddValue("additional", (int)classInfo.isDataContainer);
                dbObject.AddValue("namespace", classInfo.typeInfo.nameSpace);
                if (classInfo.typeInfo.guid != Guid.Empty)
                {
                    dbObject.AddValue("guid", classInfo.typeInfo.guid);
                }
                classInfo.typeInfo.Modify(dbObject);
                DbObject dbObject2 = new DbObject(bObject: false);
                foreach (IFieldInfo field in classInfo.typeInfo.fields)
                {
                    DbObject dboField = new DbObject();
                    if (classInfo.typeInfo.Type == 8)
                    {
                        dboField.AddValue("name", field.name);
                        dboField.AddValue("value", (int)field.typeOffset);
                    }
                    else if (offsetClassInfoMapping.ContainsKey(field.typeOffset))
                    {
                        IClassInfo classInfo3 = offsetClassInfoMapping[field.typeOffset];
                        dboField.AddValue("name", field.name);
                        dboField.AddValue("nameHash", field.nameHash);
                        dboField.AddValue("type", classInfo3.typeInfo.Type);
                        dboField.AddValue("flags", (int)classInfo3.typeInfo.flags);
                        dboField.AddValue("offset", (int)field.offset);
                        dboField.AddValue("index", field.index);
                        if (classInfo3.typeInfo.Type == 3
                            || classInfo3.typeInfo.Type == 2
                            || classInfo3.typeInfo.Type == 8)
                        {
                            dboField.AddValue("baseType", classInfo3.typeInfo.name);
                        }
                        else if (classInfo3.typeInfo.Type == 4)
                        {
                            if (offsetClassInfoMapping.ContainsKey(classInfo3.parentClass))
                            {
                                classInfo3 = offsetClassInfoMapping[classInfo3.parentClass];
                                dboField.AddValue("isArray", true);
                                dboField.AddValue("baseType", classInfo3.typeInfo.name);
                                dboField.AddValue("arrayFlags", (int)classInfo3.typeInfo.flags);
                            }
                        }
                    }
                    field.Modify(dboField);
                    dbObject2.Add(dboField);
                }
                dbObject.AddValue("fields", dbObject2);
                classList.Add(dbObject);
                alreadyProcessedClasses.Add(classInfo.typeInfo.name);
            }
        }

        public bool CreateSDK()
        {
            Debug.WriteLine("Creating SDK");
            using (ModuleWriter moduleWriter = new ModuleWriter("EbxClasses.dll", classList))
            {
                moduleWriter.Write(FileSystem.Instance.Head);
            }
            if (File.Exists("EbxClasses.dll"))
            {
                FileInfo fileInfo = new FileInfo(".\\TmpProfiles\\" + ProfileManager.SDKFilename + ".dll");
                if (!fileInfo.Directory.Exists)
                {
                    Directory.CreateDirectory(fileInfo.Directory.FullName);
                }
                if (fileInfo.Exists)
                {
                    File.Delete(fileInfo.FullName);
                }
                File.Move("EbxClasses.dll", fileInfo.FullName);
                return true;
            }
            Console.WriteLine("Failed to produce SDK");
            return false;
        }
    }
}
