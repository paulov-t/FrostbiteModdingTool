using FMT.FileTools;
using FrostySdk.Attributes;
using FrostySdk.FrostySdk.IO._2022.Readers;
using FrostySdk.IO;
using System;
using System.IO;
using static FrostySdk.IO.EbxXmlWriter;
using System.Linq;
using System.Reflection;
using FrostySdk.Managers;

namespace FrostySdk.FrostySdk.IO
{
    public class EbxWriterRiff : EbxWriter2023
    {
        public EbxWriterRiff(Stream inStream, EbxWriteFlags inFlags = EbxWriteFlags.None, bool leaveOpen = false)
           : base(inStream, inFlags, true)
        {
        }

        IEbxSharedTypeDescriptor std = null;

        internal EbxClass GetClassUnpatched(Type objType)
        {
            if (objType == null)
            {
                throw new ArgumentNullException("objType");
            }
            foreach (TypeInfoGuidAttribute typeInfoGuidAttribute in objType.GetCustomAttributes<TypeInfoGuidAttribute>())
            {
                if(std == null)
                    std = (IEbxSharedTypeDescriptor)AssetManager.LoadTypeByName(ProfileManager.EBXTypeDescriptor
                            , AssetManager.Instance.FileSystem, "SharedTypeDescriptors.ebx", false);

                EbxClass? ebxClass = std.GetClass(typeInfoGuidAttribute.Guid);
                if (ebxClass.HasValue)
                {
                    return ebxClass.Value;
                }
            }
            throw new ArgumentException("Class not found.");
        }
    }
}
