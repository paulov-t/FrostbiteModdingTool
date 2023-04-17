using FMT.FileTools;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
//using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System.Linq;
using System.Text;

namespace SdkGenerator
{
    public class ModuleWriter : IDisposable
    {
        private DbObject classList;

        private string filename;

        private List<string> CreateOldFB3PFs()
        {
            return new List<string>
            {
                "RenderFormat_BC1_UNORM",
                "RenderFormat_BC1A_UNORM",
                "RenderFormat_BC2_UNORM",
                "RenderFormat_BC3_UNORM",
                "RenderFormat_BC3A_UNORM",
                "RenderFormat_DXN",
                "RenderFormat_BC7_UNORM",
                "RenderFormat_RGB565",
                "RenderFormat_RGB888",
                "RenderFormat_ARGB1555",
                "RenderFormat_ARGB4444",
                "RenderFormat_ARGB8888",
                "RenderFormat_L8",
                "RenderFormat_L16",
                "RenderFormat_ABGR16",
                "RenderFormat_ABGR16F",
                "RenderFormat_ABGR32F",
                "RenderFormat_R16F",
                "RenderFormat_R32F",
                "RenderFormat_NormalDXN",
                "RenderFormat_NormalDXT1",
                "RenderFormat_NormalDXT5",
                "RenderFormat_NormalDXT5RGA",
                "RenderFormat_RG8",
                "RenderFormat_GR16",
                "RenderFormat_GR16F",
                "RenderFormat_D16",
                "RenderFormat_D24",
                "RenderFormat_D24S8",
                "RenderFormat_D24FS8",
                "RenderFormat_D32F",
                "RenderFormat_D32FS8",
                "RenderFormat_S8",
                "RenderFormat_ABGR32",
                "RenderFormat_GR32F",
                "RenderFormat_A2R10G10B10",
                "RenderFormat_R11G11B10F",
                "RenderFormat_ABGR16_SNORM",
                "RenderFormat_ABGR16_UINT",
                "RenderFormat_L16_UINT",
                "RenderFormat_L32",
                "RenderFormat_GR16_UINT",
                "RenderFormat_GR32_UINT",
                "RenderFormat_ETC1",
                "RenderFormat_ETC2_RGB",
                "RenderFormat_ETC2_RGBA",
                "RenderFormat_ETC2_RGB_A1",
                "RenderFormat_PVRTC1_4BPP_RGBA",
                "RenderFormat_PVRTC1_4BPP_RGB",
                "RenderFormat_PVRTC1_2BPP_RGBA",
                "RenderFormat_PVRTC1_2BPP_RGB",
                "RenderFormat_PVRTC2_4BPP",
                "RenderFormat_PVRTC2_2BPP",
                "RenderFormat_R8",
                "RenderFormat_R9G9B9E5F",
                "RenderFormat_Unknown"
            };
        }

        public ModuleWriter(string inFilename, DbObject inList)
        {
            filename = inFilename;
            classList = inList;
        }

        public void Write(uint version)
        {
            //if (File.Exists("temp.cs")) { 
            //	WriteFromTempCS(version);
            //	return;
            //}

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("using System;");
            stringBuilder.AppendLine("using System.Collections.Generic;");
            stringBuilder.AppendLine("using FMT.FileTools;");
            stringBuilder.AppendLine("using FrostySdk;");
            stringBuilder.AppendLine("using FrostySdk.Attributes;");
            stringBuilder.AppendLine("using FrostySdk.Managers;");
            stringBuilder.AppendLine("using System.Reflection;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("[assembly: SdkVersion(" + (int)version + ")]");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("namespace FrostySdk.Ebx");
            stringBuilder.AppendLine("{");

            if (ProfileManager.LoadedProfile.Name.Contains("Dead"))
            {
                stringBuilder.AppendLine("public class BoolChimeraValue");
                stringBuilder.AppendLine("{");
                stringBuilder.AppendLine("}");
                stringBuilder.AppendLine("public class Vec3ChimeraValue");
                stringBuilder.AppendLine("{");
                stringBuilder.AppendLine("}");
                stringBuilder.AppendLine("public class FloatChimeraValue");
                stringBuilder.AppendLine("{");
                stringBuilder.AppendLine("}");
            }


            foreach (DbObject @class in classList)
            {
                if (@class.GetValue<string>("name").Equals("RenderFormat", StringComparison.OrdinalIgnoreCase))
                {
                }
                if (@class.GetValue<string>("name").Equals("LinearTransform", StringComparison.OrdinalIgnoreCase))
                {
                }
                if (@class.GetValue<string>("name").Equals("Vec3", StringComparison.OrdinalIgnoreCase))
                {
                }
                EbxFieldType ebxFieldType = (EbxFieldType)@class.GetValue("type", 0);
                switch (ebxFieldType)
                {
                    case EbxFieldType.Enum:
                        stringBuilder.Append(WriteEnum(@class));
                        continue;
                    case EbxFieldType.Struct:
                    case EbxFieldType.Pointer:
                        stringBuilder.Append(WriteClass(@class));
                        continue;
                    default:
                        if (@class.HasValue("basic"))
                        {
                            stringBuilder.AppendLine("namespace Reflection\r\n{");
                            stringBuilder.Append(WriteClass(@class));
                            stringBuilder.AppendLine("}");
                            continue;
                        }
                        break;
                    case EbxFieldType.Array:
                    case EbxFieldType.Delegate:
                        break;
                }
                if (ebxFieldType == EbxFieldType.Delegate || ebxFieldType == (EbxFieldType)28)
                {
                    stringBuilder.AppendLine("namespace Reflection\r\n{");
                    stringBuilder.AppendLine("[" + typeof(DisplayNameAttribute).Name + "(\"" + @class.GetValue<string>("name") + "\")]");
                    stringBuilder.AppendLine("[GuidAttribute(\"" + @class.GetValue<Guid>("guid") + "\")]");

                    stringBuilder.AppendLine("public class Delegate_" + @class.GetValue<Guid>("guid").ToString().Replace('-', '_') + " { }\r\n}");
                }
            }
            if (ProfileManager.DataVersion == 20141118 || ProfileManager.DataVersion == 20141117 || ProfileManager.DataVersion == 20131115 || ProfileManager.DataVersion == 20140225
                 ||
                ProfileManager.IsBF4DataVersion()
                )
            {
                DbObject dbObject2 = DbObject.CreateObject();
                dbObject2.SetValue("name", "RenderFormat");
                List<string> list = CreateOldFB3PFs();
                DbObject dbObject3 = DbObject.CreateList();
                int num = 0;
                foreach (string item in list)
                {
                    DbObject dbObject4 = DbObject.CreateObject();
                    dbObject4.SetValue("name", item);
                    dbObject4.SetValue("value", num++);
                    dbObject3.Add(dbObject4);
                }
                dbObject2.SetValue("fields", dbObject3);
                stringBuilder.Append(WriteEnum(dbObject2));
            }
            stringBuilder.AppendLine("}");

            //if (File.Exists("temp.cs"))
            //	File.Delete("temp.cs");

            using (NativeWriter nativeWriter = new NativeWriter(new FileStream("temp.cs", FileMode.Create)))
            {
                nativeWriter.WriteLine(stringBuilder.ToString());
            }

            WriteFromTempCS(version);

            //File.Delete("temp.cs");
        }

        public void WriteFromTempCS(uint version)
        {
            var codeString = SourceText.From(File.ReadAllText("temp.cs"));
            var options = CSharpParseOptions.Default
                .WithLanguageVersion(LanguageVersion.LatestMajor);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var dotnetcore_directory = Directory.GetParent(typeof(object).Assembly.Location);

            var dotnetcore_files = Directory.GetFiles(dotnetcore_directory.FullName, "*.dll", SearchOption.AllDirectories);

            var objectAssemblyLocation = typeof(object).Assembly.Location;
            //var systemRuntimeAssemblyLocation = typeof(System.Runtime.Serialization.DataContractSerializer).Assembly.Location;


            var references = new MetadataReference[]
            {
                    MetadataReference.CreateFromFile(objectAssemblyLocation),
                    MetadataReference.CreateFromFile(dotnetcore_directory + @"\netstandard.dll"),
                    MetadataReference.CreateFromFile(dotnetcore_directory + @"\System.Runtime.dll"),
                    MetadataReference.CreateFromFile("FrostySdk.dll"),
                    MetadataReference.CreateFromFile("FMT.FileTools.dll"),
            };

            if (File.Exists("EbxClasses.dll"))
                File.Delete("EbxClasses.dll");
            using (FileStream stream = new FileStream(filename, FileMode.CreateNew))
            {
                var result = CSharpCompilation.Create(filename,
                    new[] { parsedSyntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Release,
                        assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                        allowUnsafe: true


                        )).Emit(stream);
                if (!result.Success)
                {
                    throw new Exception(result.Diagnostics.Length + " errors");
                }
                else
                {
                    if (File.Exists("temp.cs"))
                        File.Delete("temp.cs");
                }
                stream.Flush();
            }


        }
        public string GetAlphabets(int i)

        {

            //Declare string for alphabet

            string strAlpha = ((char)i + 65).ToString();


            return strAlpha;


        }

        private string WriteEnum(DbObject enumObj)
        {
            if (enumObj.GetValue<string>("name").Equals("RenderFormat", StringComparison.OrdinalIgnoreCase))
            {
            }

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(WriteClassAttributes(enumObj));

            stringBuilder.AppendLine("public enum " + enumObj.GetValue<string>("name") + " : int");
            stringBuilder.AppendLine("{");
            var index = 0;
            foreach (DbObject item in enumObj.GetValue<DbObject>("fields"))
            {
                var name = item.GetValue<string>("name");
                var value = item.GetValue("value", 0);
                if (!string.IsNullOrEmpty(name))
                {
                    stringBuilder.AppendLine(name + " = " + value + ",");
                }
                index++;
            }
            stringBuilder.AppendLine("}");
            return stringBuilder.ToString();
        }

        private string WriteClass(DbObject classObj)
        {
            if (classObj.GetValue<string>("name") == "char")
            {
                return "";
            }
            StringBuilder stringBuilder = new StringBuilder();
            string parentClassName = classObj.GetValue("parent", "").Replace(':', '_').Replace('-', '_').Replace("(", "").Replace(")", "").Replace(",", "");
            EbxFieldType ebxFieldType = (EbxFieldType)classObj.GetValue("type", 0);
            DbObject value = classObj.GetValue<DbObject>("meta");
            string className = classObj.GetValue<string>("name").Replace(':', '_').Replace('-', '_').Replace("(", "").Replace(")", "").Replace(",", "");

            if (string.IsNullOrEmpty(className))
                return string.Empty;

            if (className.EndsWith(">") || parentClassName.EndsWith(">"))
                return string.Empty;

            stringBuilder.Append(WriteClassAttributes(classObj));

            stringBuilder.AppendLine("public class " + className + ((parentClassName != "") ? (" : " + parentClassName) : ""));
            stringBuilder.AppendLine("{");
            if (ebxFieldType == EbxFieldType.Pointer)
            {
                if (parentClassName == "DataContainer")
                {
                    //stringBuilder.AppendLine("[" + typeof(IsTransientAttribute).Name + "]");
                    //if (!classObj.HasValue("isData"))
                    //{
                    //	stringBuilder.AppendLine("[" + typeof(IsHiddenAttribute).Name + "]");
                    //}
                    //stringBuilder.AppendLine("[" + typeof(DisplayNameAttribute).Name + "(\"Id\")]");
                    //stringBuilder.AppendLine("[" + typeof(CategoryAttribute).Name + "(\"Annotations\")]");
                    //stringBuilder.AppendLine("[" + typeof(EbxFieldMetaAttribute).Name + "(8310, 8u, null, false, 0)]");
                    //stringBuilder.AppendLine("[" + typeof(FieldIndexAttribute).Name + "(-2)]");
                    //stringBuilder.AppendLine("public CString __Id\r\n{\r\nget\r\n{\r\nreturn GetId();\r\n}\r\nset { __id = value; }\r\n}\r\n");
                    //stringBuilder.AppendLine("protected CString __id = new CString();");
                }
                if (parentClassName == "")
                {
                    stringBuilder.AppendLine("[" + typeof(IsTransientAttribute).Name + "]");
                    stringBuilder.AppendLine("[" + typeof(IsReadOnlyAttribute).Name + "]");
                    stringBuilder.AppendLine("[" + typeof(DisplayNameAttribute).Name + "(\"Guid\")]");
                    stringBuilder.AppendLine("[" + typeof(CategoryAttribute).Name + "(\"Annotations\")]");
                    //stringBuilder.AppendLine("[" + typeof(EditorAttribute).Name + "(\"Struct\")]");
                    stringBuilder.AppendLine("[" + typeof(EbxFieldMetaAttribute).Name + "(24918, 1, null, false, 0)]");
                    stringBuilder.AppendLine("[" + typeof(FieldIndexAttribute).Name + "(-1)]");
                    stringBuilder.AppendLine("public AssetClassGuid __InstanceGuid { get { return __Guid; } }");
                    stringBuilder.AppendLine("protected AssetClassGuid __Guid;");
                    stringBuilder.AppendLine("public AssetClassGuid GetInstanceGuid() { return __Guid; }");
                    stringBuilder.AppendLine("public void SetInstanceGuid(AssetClassGuid newGuid) { __Guid = newGuid; }");
                }
            }
            bool flag = classObj.GetValue<string>("name").Equals("Asset");
            bool flag2 = false;

            if (classObj.GetValue<string>("name") == "blocking")
            {
            }
            if (classObj.GetValue<string>("name") == "blocking")
            {
            }

            if (classObj.GetValue<string>("name") == "AgentsClassSettingsFillerData")
            {
            }

            if (classObj.GetValue<string>("name") == "LinearTransform")
            {
            }


            var class_fields = classObj.GetValue<DbObject>("fields").list.OrderBy(x => ((DbObject)x).GetValue<int>("offset"));
            foreach (DbObject item in class_fields)
            {
                if (!item.HasValue("name"))
                    continue;

                if (!item.HasValue("type"))
                    continue;

                stringBuilder.Append(WriteField(item));

                if (!flag && item.GetValue<string>("name").Equals("Name", StringComparison.OrdinalIgnoreCase) && ebxFieldType == EbxFieldType.Pointer && (byte)item.GetValue("type", 0) == 7)
                {
                    string name = typeof(EbxClassMetaAttribute).GetProperties()[4].Name;
                    string name2 = typeof(GlobalAttributes).GetFields()[0].Name;
                    Type typeFromHandle = typeof(CString);
                    string name3 = typeFromHandle.GetMethods()[0].Name;
                    _ = typeFromHandle.GetMethods()[3].Name;
                    if (classObj.HasValue("isData"))
                    {
                        string value2 = item.GetValue<string>("name");
                        //stringBuilder.AppendLine("protected virtual CString GetId()\r\n{");
                        //stringBuilder.AppendLine("if (__id != \"\") return __id;");
                        //                  //stringBuilder.AppendLine("if (_" + value2 + " != \"\") return _" + value2 + "." + name3 + "();");
                        //                  stringBuilder.AppendLine("if (_" + value2 + " != \"\") return _" + value2 + ".ToString();");
                        //                  stringBuilder.AppendLine("if (" + typeof(GlobalAttributes).Name + "." + name2 + ")\r\n{\r\n" + typeof(EbxClassMetaAttribute).Name + " attr = GetType().GetCustomAttribute<" + typeof(EbxClassMetaAttribute).Name + ">();\r\nif (attr != null && attr." + name + " != \"\")\r\nreturn attr." + name + " + \".\" + GetType().Name;\r\n}\r\nreturn GetType().Name;");
                        //stringBuilder.AppendLine("}");
                        flag2 = true;
                    }
                    else
                    {
                        string value3 = item.GetValue<string>("name");
                        //stringBuilder.AppendLine("protected override CString GetId()\r\n{");
                        //stringBuilder.AppendLine("if (__id != \"\") return __id;");
                        ////stringBuilder.AppendLine("if (_" + value3 + " != \"\") return _" + value3 + "." + name3 + "();\r\nreturn base.GetId();");
                        //stringBuilder.AppendLine("if (_" + value3 + " != \"\") return _" + value3 + ".ToString();\r\nreturn base.GetId();");
                        //stringBuilder.AppendLine("}");
                    }
                }
            }
            if (parentClassName == "DataContainer" && !flag2)
            {
                //string name4 = typeof(EbxClassMetaAttribute).GetProperties()[4].Name;
                //string name5 = typeof(GlobalAttributes).GetFields()[0].Name;
                //stringBuilder.AppendLine("protected virtual CString GetId()\r\n{");
                //stringBuilder.AppendLine("if (__id == \"\")\r\n{\r\nif (" + typeof(GlobalAttributes).Name + "." + name5 + ")\r\n{\r\n" + typeof(EbxClassMetaAttribute).Name + " attr = GetType().GetCustomAttribute<" + typeof(EbxClassMetaAttribute).Name + ">();\r\nif (attr != null && attr." + name4 + " != \"\")\r\nreturn attr." + name4 + " + \".\" + GetType().Name;\r\n}\r\nreturn GetType().Name;\r\n}\r\nreturn __id;");
                //stringBuilder.AppendLine("}");
            }
            if (value != null)
            {
                if (value.HasValue("constructor"))
                {
                    stringBuilder.AppendLine("public " + classObj.GetValue<string>("name").Replace(':', '_') + "()");
                    stringBuilder.AppendLine("{");
                    stringBuilder.AppendLine(value.GetValue<string>("constructor"));
                    stringBuilder.AppendLine("}");
                }
                stringBuilder.AppendLine(value.GetValue("functions", ""));
            }
            if (ebxFieldType == EbxFieldType.Struct && classObj.GetValue<DbObject>("fields").Count > 0)
            {
                //stringBuilder.AppendLine("public override bool Equals(object obj)\r\n{");
                //stringBuilder.AppendLine("if (obj == null || !(obj is " + className + "))\r\nreturn false;");
                //stringBuilder.AppendLine(className + " b = (" + className + ")obj;");
                //stringBuilder.Append("return ");
                //int num = 0;
                //foreach (DbObject fieldItem in classObj.GetValue<DbObject>("fields"))
                //{
                //    DbObject value4 = fieldItem.GetValue<DbObject>("meta");
                //    if (value4 == null || !value4.HasValue("hidden"))
                //    {
                //        string fieldName = fieldItem.GetValue<string>("name");
                //        if (string.IsNullOrEmpty(fieldName))
                //            continue;

                //        stringBuilder.AppendLine(((num++ != 0) ? "&& " : "") + fieldName + " == b." + fieldName);
                //    }
                //}
                //stringBuilder.AppendLine(";\r\n}");

                //stringBuilder.AppendLine("public override int GetHashCode()\r\n{\r\nunchecked {\r\nint hash = (int)2166136261;");
                //foreach (DbObject item3 in classObj.GetValue<DbObject>("fields"))
                //{
                //    DbObject value6 = item3.GetValue<DbObject>("meta");
                //    if (value6 == null || !value6.HasValue("hidden"))
                //    {
                //        string value7 = item3.GetValue<string>("name");
                //        stringBuilder.AppendLine("hash = (hash * 16777619) ^ " + value7 + ".GetHashCode();");
                //    }
                //}
                //stringBuilder.AppendLine("return hash;\r\n}\r\n}");
            }
            stringBuilder.AppendLine("}");
            return stringBuilder.ToString();
        }

        private string WriteField(DbObject fieldObj)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (!fieldObj.HasValue("name"))
                return string.Empty;

            string fieldName = fieldObj.GetValue<string>("name");
            EbxFieldType ebxFieldType = (EbxFieldType)fieldObj.GetValue("type", 0);
            string baseType = fieldObj.GetValue("baseType", "");
            DbObject meta = fieldObj.GetValue<DbObject>("meta");
            DbObject dbObject = meta?.GetValue<DbObject>("type");
            if (meta != null && meta.HasValue("version"))
            {
                bool flag = false;
                foreach (int item in meta.GetValue<DbObject>("version"))
                {
                    if (item == ProfileManager.DataVersion)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    return "";
                }
            }
            if (dbObject != null)
            {
                ebxFieldType = (EbxFieldType)dbObject.GetValue("flags", 0);
                if (dbObject.HasValue("baseType"))
                {
                    baseType = dbObject.GetValue<string>("baseType");
                }
            }
            string fieldType = "";
            bool flag2 = false;
            stringBuilder.Append(WriteFieldAttributes(fieldObj));
            if (ebxFieldType == EbxFieldType.Array)
            {
                EbxFieldType type = (EbxFieldType)((fieldObj.GetValue("arrayFlags", 0) >> 4) & 0x1F);
                if (dbObject != null)
                {
                    type = (EbxFieldType)dbObject.GetValue("arrayType", 0);
                }
                fieldType = "List<" + GetFieldType(type, baseType) + ">";
                flag2 = true;
            }
            else
            {
                fieldType = GetFieldType(ebxFieldType, baseType);
                flag2 = (ebxFieldType == EbxFieldType.ResourceRef || ebxFieldType == EbxFieldType.BoxedValueRef || ebxFieldType == EbxFieldType.CString || ebxFieldType == EbxFieldType.FileRef || ebxFieldType == EbxFieldType.TypeRef || ebxFieldType == EbxFieldType.Struct);
            }
            if (string.IsNullOrEmpty(fieldType))
                fieldType = "PointerRef";

            if (meta != null && meta.HasValue("accessor"))
            {
                stringBuilder.AppendLine("public " + fieldType + " " + fieldName + " { " + meta.GetValue<string>("accessor") + " }");
            }
            else
            {
                //stringBuilder.AppendLine("public " + fieldType + " " + fieldName + " { get { return _" + fieldName + "; } set { _" + fieldName + " = value; } }");
                stringBuilder.AppendLine("public " + fieldType + " " + fieldName + " { get; set; }");
            }
            //stringBuilder.AppendLine("protected " + fieldType + " _" + fieldName + (flag2 ? (" = new " + fieldType + "()") : "") + ";");
            return stringBuilder.ToString();
        }

        private string WriteClassAttributes(DbObject classObj)
        {
            StringBuilder stringBuilder = new StringBuilder();
            DbObject value = classObj.GetValue<DbObject>("meta");
            int value2 = classObj.GetValue("alignment", 0);
            if (value != null && value.HasValue("alignment"))
            {
                value2 = value.GetValue("alignment", 0);
            }
            stringBuilder.AppendLine("[" + typeof(EbxClassMetaAttribute).Name + "(" + classObj.GetValue("flags", 0) + ", " + value2 + ", " + classObj.GetValue("size", 0) + ", \"" + classObj.GetValue<string>("namespace") + "\")]");
            if (classObj.HasValue("guid"))
            {
                //stringBuilder.AppendLine("[" + typeof(GuidAttribute).Name + "(\"" + classObj.GetValue<Guid>("guid") + "\")]");
                stringBuilder.AppendLine("[GuidAttribute(\"" + classObj.GetValue<Guid>("guid") + "\")]");
            }
            if (classObj.HasValue("typeInfoGuid"))
            {
                foreach (Guid item in classObj.GetValue<DbObject>("typeInfoGuid"))
                {
                    stringBuilder.AppendLine("[" + typeof(TypeInfoGuidAttribute).Name + "(\"" + item + "\")]");
                }
            }
            if (classObj.HasValue("nameHash"))
            {
                stringBuilder.AppendLine("[" + typeof(HashAttribute).Name + "(" + classObj.GetValue<uint>("nameHash") + ")]");
            }
            if (ProfileManager.DataVersion == 20171117)// || ProfilesLibrary.IsFIFA21DataVersion())// || ProfilesLibrary.IsMadden21DataVersion())
            {
                stringBuilder.AppendLine("[" + typeof(RuntimeSizeAttribute).Name + "(" + classObj.GetValue("runtimeSize", 0) + ")]");
            }
            if (value != null)
            {
                if (value.HasValue("valueConverter"))
                {
                    stringBuilder.AppendLine("[" + typeof(ClassConverterAttribute).Name + "(\"" + value.GetValue<string>("valueConverter") + "\")]");
                }
                if (value.HasValue("description"))
                {
                    stringBuilder.AppendLine("// [" + typeof(DescriptionAttribute).Name + "(\"" + value.GetValue<string>("description") + "\")]");
                }
                if (value.HasValue("inline"))
                {
                    stringBuilder.AppendLine("[" + typeof(IsInlineAttribute).Name + "]");
                }
                if (value.HasValue("abstract"))
                {
                    stringBuilder.AppendLine("[" + typeof(IsAbstractAttribute).Name + "]");
                }
                if (value.HasValue("icon"))
                {
                    stringBuilder.AppendLine("[" + typeof(IconAttribute).Name + "(\"" + value.GetValue<string>("icon") + "\")]");
                }
            }
            return stringBuilder.ToString();
        }

        private string WriteFieldAttributes(DbObject fieldObj)
        {
            StringBuilder stringBuilder = new StringBuilder();
            DbObject value = fieldObj.GetValue<DbObject>("meta");
            EbxFieldType ebxFieldType = (EbxFieldType)fieldObj.GetValue("type", 0);
            int num = fieldObj.GetValue("arrayFlags", 0);
            string baseType = (ebxFieldType == EbxFieldType.Pointer || ebxFieldType == EbxFieldType.Array) ? fieldObj.GetValue("baseType", "null") : "null";
            int flags = fieldObj.GetValue("flags", 0);
            //if (ebxFieldType == EbxFieldType.Array && fieldObj.HasValue("guid"))
            if (fieldObj.HasValue("guid"))
            {
                //stringBuilder.AppendLine("[" + typeof(GuidAttribute) + "(\"" + fieldObj.GetValue<Guid>("guid").ToString() + "\")]");
                stringBuilder.AppendLine("[GuidAttribute(\"" + fieldObj.GetValue<Guid>("guid") + "\")]");
            }
            if (value != null)
            {
                DbObject value2 = value.GetValue<DbObject>("type");
                if (value2 != null)
                {
                    flags = value2.GetValue("flags", 0) << 4;
                    if (value2.HasValue("baseType"))
                    {
                        baseType = value2.GetValue<string>("baseType");
                    }
                    if (value2.HasValue("arrayType"))
                    {
                        num = value2.GetValue("arrayType", 0) << 4;
                    }
                }
                else if (value.HasValue("transient"))
                {
                    flags = 240;
                }
            }
            if (baseType != "null")
            {
                baseType = "typeof(" + baseType + ")";
            }

            baseType = baseType.Replace("Float32", "float", StringComparison.OrdinalIgnoreCase);
            baseType = baseType.Replace("UInt16", "ushort", StringComparison.OrdinalIgnoreCase);
            baseType = baseType.Replace("UInt32", "uint", StringComparison.OrdinalIgnoreCase);
            baseType = baseType.Replace("Int16", "short", StringComparison.OrdinalIgnoreCase);
            baseType = baseType.Replace("Int32", "int", StringComparison.OrdinalIgnoreCase);
            baseType = baseType.Replace("UInt64", "ulong", StringComparison.OrdinalIgnoreCase);
            baseType = baseType.Replace("Int64", "long", StringComparison.OrdinalIgnoreCase);
            baseType = baseType.Replace("UInt8", "sbyte", StringComparison.OrdinalIgnoreCase);
            baseType = baseType.Replace("Int8", "byte", StringComparison.OrdinalIgnoreCase);
            if (baseType.Contains("ulongprovider", StringComparison.OrdinalIgnoreCase))
                return String.Empty;

            int value3 = fieldObj.GetValue("index", 0);
            if (fieldObj.HasValue("nameHash"))
            {
                //stringBuilder.AppendLine("[" + typeof(HashAttribute).Name + "(" + fieldObj.GetValue("nameHash", 0ul) + ")]");
                //stringBuilder.AppendLine("[" + typeof(HashAttribute).Name + "(" + fieldObj.GetValue<uint>("nameHash", 0u) + ")]");
                stringBuilder.AppendLine("[" + typeof(HashAttribute).Name + "(" + fieldObj.GetValue<uint>("nameHash") + ")]");
            }
            // PG - Added this hack for a quick fix
            //if (fieldObj.GetValue("offset", 0) < 0)
            //{
            //	stringBuilder.AppendLine("[" + typeof(EbxFieldMetaAttribute).Name + "(" + num2 + ", " + 3 + ", " + baseType + ", " + (ebxFieldType == EbxFieldType.Array).ToString().ToLower() + ", " + num + ")]");
            //}
            //else
            //         {
            stringBuilder.AppendLine("[" + typeof(EbxFieldMetaAttribute).Name
                + "(" + flags + ", "
                + fieldObj.GetValue("offset", 0) + ", "
                + baseType + ", "
                + (ebxFieldType == EbxFieldType.Array).ToString().ToLowerInvariant() + ", "
                + num + ")]");
            //}
            if (value != null)
            {
                if (value.HasValue("displayName"))
                {
                    stringBuilder.AppendLine("[" + typeof(DisplayNameAttribute) + "(\"" + value.GetValue<string>("displayName") + "\")]");
                }
                if (value.HasValue("description"))
                {
                    stringBuilder.AppendLine("// [" + typeof(DescriptionAttribute) + "(\"" + value.GetValue<string>("description") + "\")]");
                }
                //            if (value.HasValue("editor"))
                //{
                //	//stringBuilder.AppendLine("[" + typeof(EditorAttribute).Name + "(\"" + value.GetValue<string>("editor") + "\")]");
                //}
                if (value.HasValue("readOnly"))
                {
                    stringBuilder.AppendLine("[" + typeof(IsReadOnlyAttribute).Name + "]");
                }
                if (value.HasValue("hidden"))
                {
                    stringBuilder.AppendLine("[" + typeof(IsHiddenAttribute).Name + "]");
                }
                if (value.HasValue("reference"))
                {
                    stringBuilder.AppendLine("[" + typeof(IsReferenceAttribute).Name + "]");
                }
                if (value.HasValue("transient"))
                {
                    stringBuilder.AppendLine("[" + typeof(IsTransientAttribute).Name + "]");
                }
                if (value.HasValue("category"))
                {
                    stringBuilder.AppendLine("[" + typeof(CategoryAttribute).Name + "(\"" + value.GetValue<string>("category") + "\")]");
                }
                if (value.HasValue("index"))
                {
                    value3 = value.GetValue("index", 0);
                }
                if (value.HasValue("hideChildren"))
                {
                    stringBuilder.AppendLine("[" + typeof(HideChildrentAttribute).Name + "]");
                }
            }
            stringBuilder.AppendLine("[" + typeof(FieldIndexAttribute).Name + "(" + value3 + ")]");
            return stringBuilder.ToString();
        }

        private string GetFieldType(EbxFieldType type, string baseType)
        {
            switch (type)
            {
                case EbxFieldType.Boolean:
                    return "bool";
                case EbxFieldType.BoxedValueRef:
                    return "BoxedValueRef";
                case EbxFieldType.CString:
                    return "CString";
                case EbxFieldType.FileRef:
                    return "FileRef";
                case EbxFieldType.Float32:
                    return "float";
                case EbxFieldType.Float64:
                    return "double";
                case EbxFieldType.Guid:
                    return "Guid";
                case EbxFieldType.Int16:
                    return "short";
                case EbxFieldType.Int32:
                    return "int";
                case EbxFieldType.Int64:
                    return "long";
                case EbxFieldType.Int8:
                    return "sbyte";
                case EbxFieldType.ResourceRef:
                    return "ResourceRef";
                case EbxFieldType.Sha1:
                    return "Sha1";
                case EbxFieldType.String:
                    return "string";
                case EbxFieldType.TypeRef:
                    return "TypeRef";
                case EbxFieldType.UInt16:
                    return "ushort";
                case EbxFieldType.UInt32:
                    return "uint";
                case EbxFieldType.UInt64:
                    return "ulong";
                case EbxFieldType.UInt8:
                    return "byte";
                case EbxFieldType.Pointer:
                    return "PointerRef";
                case EbxFieldType.Enum:
                    return baseType;
                case EbxFieldType.Struct:
                    return baseType;
                // there is something very likely wrong if its doing this
                case EbxFieldType.DbObject:
                    return baseType;
                default:
                    //return "";
                    return baseType;
            }
        }

        public void Dispose()
        {
        }
    }
}
