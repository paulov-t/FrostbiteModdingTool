using FMT.FileTools;
using FrostbiteSdk;
using FrostbiteSdk.SdkGenerator;
using FrostySdk;
using System;
using System.IO;

namespace SdkGenerator.BaseInfo
{

    public class ClassInfo : ISdkGenInfo, IClassInfo
    {
        public ITypeInfo typeInfo { get; set; }

        public ushort id { get; set; }

        public ushort isDataContainer { get; set; }

        public byte[] padding { get; set; }

        public long parentClass { get; set; }

        public long nextOffset { get; set; } = 0;

        public virtual void Read(MemoryReader reader)
        {
            long position = reader.Position;
            using (NativeWriter nativeWriter = new NativeWriter(new FileStream("DebugClassInfo.dat", FileMode.Create)))
            {
                nativeWriter.Write(reader.ReadBytes(100));
                reader.Position = position;
            }

            long position2 = reader.ReadLong();
            ClassesSdkCreator.offset = reader.ReadLong();
            Guid guid = Guid.Empty;
            if (
                ProfileManager.IsFIFA19DataVersion() ||
                ProfileManager.DataVersion == 20171117 || ProfileManager.DataVersion == 20171110 || ProfileManager.DataVersion == 20180807 || ProfileManager.DataVersion == 20170929 || ProfileManager.DataVersion == 20180628)
            {
                guid = reader.ReadGuid();
            }

            id = reader.ReadUShort();

            //if (ProfilesLibrary.IsFIFA19DataVersion())
            //    reader.ReadUShort();

            if (ProfileManager.IsFIFA19DataVersion())
            {
                reader.ReadByte();
            }

            //isDataContainer = reader.ReadUShort();
            isDataContainer = ProfileManager.IsFIFA19DataVersion() ? reader.ReadByte() : reader.ReadUShort();
            padding = new byte[4]
            {
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte()
            };
            parentClass = reader.ReadLong();
            reader.Position = position2;
            typeInfo = new TypeInfo();
            typeInfo.Read(reader);
            if (ProfileManager.DataVersion == 20171117 || ProfileManager.DataVersion == 20171110 || ProfileManager.DataVersion == 20180807 || ProfileManager.DataVersion == 20170929 || ProfileManager.DataVersion == 20180628)
            {
                typeInfo.guid = guid;
            }
            if (typeInfo.parentClass != 0L)
            {
                parentClass = typeInfo.parentClass;
            }
            reader.Position = parentClass;
            if (reader.Position == position)
            {
                parentClass = 0L;
            }


        }
    }

}
