using FMT.FileTools;
using FMT.FileTools.Modding;
using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.FrostySdk.Resources;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FIFA23Plugin.Meshes
{
    internal class FIFA23MeshReader : IMeshSetReader
    {
        public int MaxLodCount => (int)MeshLimits.MaxMeshLodCount;

        public void Read(NativeReader nativeReader, MeshSet meshSet)
        {
            meshSet.BoundingBox = nativeReader.ReadAxisAlignedBox();
            long[] array = new long[MaxLodCount];
            for (int i2 = 0; i2 < MaxLodCount; i2++)
            {
                array[i2] = nativeReader.ReadLong();
            }
            meshSet.UnknownPostLODCount = nativeReader.ReadLong();
            long offsetNameLong = nativeReader.ReadLong();
            long offsetNameShort = nativeReader.ReadLong();
            meshSet.nameHash = nativeReader.ReadUInt();
            meshSet.Type = (MeshType)nativeReader.ReadByte();
            meshSet.FIFA23_Type2 = (MeshType)nativeReader.ReadByte();
            meshSet.FIFA23_TypeUnknownBytes = nativeReader.ReadBytes(18);

            for (int n = 0; n < MaxLodCount * 2; n++)
            {
                meshSet.LodFade.Add(nativeReader.ReadUInt16LittleEndian());
            }
            meshSet.MeshLayout = (EMeshLayout)nativeReader.ReadByte();
            nativeReader.Position -= 1;
            var meshLayoutFlags = (MeshSetLayoutFlags)nativeReader.ReadByte();
            meshSet.unknownUInts.Add(nativeReader.ReadUInt());
            meshSet.unknownUInts.Add(nativeReader.ReadUInt());
            nativeReader.Position -= 1;
            meshSet.ShaderDrawOrder = (ShaderDrawOrder)nativeReader.ReadByte();
            meshSet.ShaderDrawOrderUserSlot = (ShaderDrawOrderUserSlot)nativeReader.ReadByte();
            meshSet.ShaderDrawOrderSubOrder = (ShaderDrawOrderSubOrder)nativeReader.ReadUShort();
            ushort lodsCount = 0;
            lodsCount = nativeReader.ReadUShort();
            meshSet.MeshCount = nativeReader.ReadUShort();
            meshSet.boneCount = 0;
            // useful for resetting when live debugging
            var positionBeforeMeshTypeRead = nativeReader.Position;
            nativeReader.Position = positionBeforeMeshTypeRead;

            if (meshSet.Type == MeshType.MeshType_Skinned)
            {

                meshSet.FIFA23_SkinnedUnknownBytes = nativeReader.ReadBytes(12);
                meshSet.boneCount = nativeReader.ReadUInt16LittleEndian();
                meshSet.CullBoxCount = nativeReader.ReadUInt16LittleEndian();
                if (meshSet.CullBoxCount != 0)
                {
                    long cullBoxBoneIndicesOffset = nativeReader.ReadInt64LittleEndian();
                    long cullBoxBoundingBoxOffset = nativeReader.ReadInt64LittleEndian();
                    long position = nativeReader.Position;
                    if (cullBoxBoneIndicesOffset != 0L)
                    {
                        nativeReader.Position = cullBoxBoneIndicesOffset;
                        for (int m = 0; m < meshSet.CullBoxCount; m++)
                        {
                            meshSet.boneIndices.Add(nativeReader.ReadUInt16LittleEndian());
                        }
                    }
                    if (cullBoxBoundingBoxOffset != 0L)
                    {
                        nativeReader.Position = cullBoxBoundingBoxOffset;
                        for (int l = 0; l < meshSet.CullBoxCount; l++)
                        {
                            meshSet.boneBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
                        }
                    }
                    nativeReader.Position = position;
                }
            }
            
            nativeReader.Pad(16);
            meshSet.headerSize = (uint)nativeReader.Position;
            for (int n = 0; n < lodsCount; n++)
            {
                meshSet.Lods.Add(new MeshSetLod(nativeReader, meshSet));
            }
            int sectionIndex = 0;
            foreach (MeshSetLod lod4 in meshSet.Lods)
            {
                for (int m = 0; m < lod4.Sections.Count; m++)
                {
                    lod4.Sections[m] = new MeshSetSection(nativeReader, sectionIndex++);
                }
            }
            nativeReader.Pad(16);
            nativeReader.Position = offsetNameLong;
            meshSet.FullName = nativeReader.ReadNullTerminatedString();
            nativeReader.Position = offsetNameShort;
            meshSet.Name = nativeReader.ReadNullTerminatedString();
            nativeReader.Pad(16);
            foreach (MeshSetLod lod3 in meshSet.Lods)
            {
                for (int l = 0; l < lod3.CategorySubsetIndices.Count; l++)
                {
                    for (int j2 = 0; j2 < lod3.CategorySubsetIndices[l].Count; j2++)
                    {
                        lod3.CategorySubsetIndices[l][j2] = nativeReader.ReadByte();
                    }
                }
            }
            nativeReader.Pad(16);
            foreach (MeshSetLod lod2 in meshSet.Lods)
            {
                nativeReader.Position += lod2.AdjacencyBufferSize;
            }
            nativeReader.Pad(16);
            foreach (MeshSetLod lod in meshSet.Lods)
            {
                if (lod.Type == MeshType.MeshType_Skinned)
                {
                    nativeReader.Position += lod.BoneCount * 4;
                }
                else if (lod.Type == MeshType.MeshType_Composite)
                {
                    nativeReader.Position += lod.Sections.Count * 24;
                }
            }
           
            nativeReader.Pad(16);
            foreach (MeshSetLod lod5 in meshSet.Lods)
            {
                lod5.ReadInlineData(nativeReader);
            }
        }
    }
}
