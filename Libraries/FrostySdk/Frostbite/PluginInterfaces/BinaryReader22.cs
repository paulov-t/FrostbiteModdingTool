using FMT.FileTools;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public class BinaryReader22
    {
        public int SBInitialHeaderLength = 32;

        //public int SBInformationHeaderLength = 36;
        public int SBInformationHeaderLength = 32;

        List<long> Sha1Positions { get; } = new List<long>();
        List<byte[]> Sha1 { get; } = new List<byte[]>();

        public long StartOfBundleOffset = 0;


        public SBHeaderInformation BinaryRead(
            int baseBundleOffset
            , ref DbObject dbObject
            , NativeReader binarySbReader2
            , bool IncludeAdditionalHeaderLength
            )
        {
            StartOfBundleOffset = binarySbReader2.Position;
            // Read out the Header Info
            var SBHeaderInformation = new SBHeaderInformation(binarySbReader2
                //, IncludeAdditionalHeaderLength ? SBInformationHeaderLength : 4
                , IncludeAdditionalHeaderLength ? SBInformationHeaderLength : 0
                );
            if (!SBHeaderInformation.SuccessfullyRead)
                return null;

            //dbObject.AddValue("totalCount", SBHeaderInformation.totalCount);
            //dbObject.AddValue("ebxCount", SBHeaderInformation.ebxCount);
            //dbObject.AddValue("resCount", SBHeaderInformation.resCount);
            //dbObject.AddValue("chunkCount", SBHeaderInformation.chunkCount);

            //
            for (int i = 0; i < SBHeaderInformation.totalCount; i++)
            {
                Sha1Positions.Add(binarySbReader2.Position + baseBundleOffset);
                //Sha1.Add(binarySbReader2.ReadSha1());
                Sha1.Add(binarySbReader2.ReadBytes(20));
            }
            //dbObject.AddValue("sha1s", Sha1);

            dbObject.AddValue("ebx", new DbObject(ReadEbx(SBHeaderInformation, binarySbReader2, baseBundleOffset)));
            dbObject.AddValue("res", new DbObject(ReadRes(SBHeaderInformation, binarySbReader2, baseBundleOffset)));
            dbObject.AddValue("chunks", new DbObject(ReadChunks(SBHeaderInformation, binarySbReader2, baseBundleOffset)));
            //dbObject.AddValue("dataOffset", (int)(SBHeaderInformation.size));
            //dbObject.AddValue("stringsOffset", (int)(SBHeaderInformation.stringOffset));
            //dbObject.AddValue("metaOffset", (int)(SBHeaderInformation.metaOffset));
            //dbObject.AddValue("metaSize", (int)(SBHeaderInformation.metaSize));

            if (SBHeaderInformation.chunkCount != 0)
            {
                var metaOffset = StartOfBundleOffset + SBHeaderInformation.metaOffset;
                binarySbReader2.Position = metaOffset;
                //using (DbReader dbReader = new DbReader(
                //    binarySbReader2.CreateViewStream(
                //        metaOffset
                //        , binarySbReader2.Length 

                //        - SBHeaderInformation.metaOffset)
                //    , new NullDeobfuscator()))
                //{
                var o = new DbReader(binarySbReader2.BaseStream).ReadDbObject();
                if (o != null)
                    dbObject.AddValue("chunkMeta", o);
                //}
            }

            return SBHeaderInformation;
        }



        private List<object> ReadEbx(SBHeaderInformation information, NativeReader reader, int baseBundleOffset = 0)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < information.ebxCount; i++)
            {
                DbObject dbObject = new DbObject(new Dictionary<string, object>());
                uint sOffset = reader.ReadUInt(Endian.Little);

                dbObject.AddValue("SB_OriginalSize_Position", reader.Position + baseBundleOffset);
                dbObject.AddValue("SBOSizePos", reader.Position + baseBundleOffset);
                uint originalSize = reader.ReadUInt(Endian.Little);

                long positionBeforeStringRead = reader.Position;
                reader.Position = StartOfBundleOffset + information.stringOffset + sOffset;
                dbObject.AddValue("SB_StringOffsetPosition", reader.Position + baseBundleOffset);
                var name = reader.ReadNullTerminatedString();

                dbObject.AddValue("SB_Sha1_Position", Sha1Positions[i]);
                dbObject.AddValue("sha1", Sha1[i]);

                dbObject.AddValue("name", name);
                //dbObject.AddValue("nameHash", Fnv1.HashString(dbObject.GetValue<string>("name")));
                dbObject.AddValue("originalSize", originalSize);
                dbObject.AddValue("ebx", true);
                list.Add(dbObject);
                reader.Position = positionBeforeStringRead;
            }
            return list;
        }
        private List<object> ReadRes(SBHeaderInformation information, NativeReader reader, int baseBundleOffset = 0)
        {
            List<object> resObjects = new List<object>();
            int shaCount = information.ebxCount;
            for (int i = 0; i < information.resCount; i++)
            {
                DbObject dbObject = new DbObject(new Dictionary<string, object>());
                uint stringPosition = reader.ReadUInt(Endian.Little);

                dbObject.AddValue("SB_OriginalSize_Position", reader.Position + baseBundleOffset);
                uint originalSize = reader.ReadUInt(Endian.Little);

                long position = reader.Position;

                reader.Position = StartOfBundleOffset + information.stringOffset + stringPosition;
                dbObject.AddValue("SB_StringOffsetPosition", reader.Position + baseBundleOffset);
                var name = reader.ReadNullTerminatedString();

                dbObject.AddValue("sha1", Sha1[shaCount + i]);
                //dbObject.AddValue("SB_Sha1_Position", Sha1Positions[information.ebxCount + i]);
                dbObject.AddValue("SB_Sha1_Position", Sha1Positions[shaCount + i]);

                //System.Diagnostics.Debug.WriteLine("RES:: " + name);
                dbObject.AddValue("name", name);
                dbObject.AddValue("nameHash", Fnv1.HashString(name));
                dbObject.AddValue("originalSize", originalSize);
                dbObject.AddValue("res", true);

                resObjects.Add(dbObject);
                reader.Position = position;
            }
            foreach (DbObject item in resObjects)
            {
                //var type = reader.ReadUInt(Endian.Big);
                var type = reader.ReadUInt(Endian.Little);
                var resType = (ResourceType)type;
                item.AddValue("resType", type);
                item.SetValue("actualResType", resType);
            }
            foreach (DbObject item2 in resObjects)
            {
                item2.AddValue("SB_ResMeta_Position", reader.Position + baseBundleOffset);
                var resMeta = reader.ReadBytes(16);
                item2.AddValue("resMeta", resMeta);
            }
            foreach (DbObject item3 in resObjects)
            {
                // var resRid = reader.ReadLong(Endian.Little);
                item3.AddValue("SB_ReRid_Position", reader.Position + baseBundleOffset);

                var resRid = reader.ReadULong(Endian.Little);
                item3.SetValue("resRid", resRid);
            }
            return resObjects;
        }
        private List<object> ReadChunks(SBHeaderInformation information, NativeReader reader, int baseBundleOffset = 0)
        {
            List<object> list = new List<object>();
            int shaCount = information.ebxCount + information.resCount;
            for (int i = 0; i < information.chunkCount; i++)
            {
                DbObject dbObject = new DbObject(new Dictionary<string, object>());
                dbObject.AddValue("SB_Guid_Position", reader.Position + baseBundleOffset);
                Guid guid = reader.ReadGuid(Endian.Little);
                dbObject.AddValue("SB_LogicalOffset_Position", reader.Position + baseBundleOffset);
                uint logicalOffset = reader.ReadUInt(Endian.Little);
                dbObject.AddValue("SB_OriginalSize_Position", reader.Position + baseBundleOffset);
                uint chunkLogicalSize = reader.ReadUInt(Endian.Little);
                long chunkOriginalSize = (logicalOffset & 0xFFFF) | chunkLogicalSize;
                dbObject.AddValue("id", guid);
                dbObject.AddValue("SB_Sha1_Position", Sha1Positions[information.ebxCount + information.resCount + i]);
                dbObject.AddValue("sha1", Sha1[shaCount + i]);
                dbObject.AddValue("logicalOffset", logicalOffset);
                dbObject.AddValue("logicalSize", chunkLogicalSize);
                dbObject.AddValue("originalSize", chunkOriginalSize);
                dbObject.AddValue("chunk", true);

                list.Add(dbObject);
            }
            return list;
        }

    }
}
