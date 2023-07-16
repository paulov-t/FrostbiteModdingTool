using FMT.FileTools;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.FrostySdk.Resources;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIFA23Plugin.Meshes
{
    public class SWSSkinnedMeshReader 
    {
        public int MaxLodCount => (int)MeshLimits.MaxMeshLodCount;

        public void Read(NativeReader nativeReader, MeshSet meshSet)
        {
            var positionToStart = nativeReader.Position;
            nativeReader.Position = positionToStart;
            for (int n = 0; n < MaxLodCount * 2; n++)
            {
                meshSet.LodFade.Add(nativeReader.ReadUInt16LittleEndian());
            }
            var meshLayoutFlags = (MeshSetLayoutFlags)nativeReader.ReadUInt();
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
                meshSet.boneCount = nativeReader.ReadUShort();
                meshSet.CullBoxCount = nativeReader.ReadUShort();
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
                nativeReader.Position = meshSet.LodOffsets[n];
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
            //nativeReader.Position = offsetNameLong;
            //meshSet.FullName = nativeReader.ReadNullTerminatedString();
            //nativeReader.Position = offsetNameShort;
            //meshSet.Name = nativeReader.ReadNullTerminatedString();

            // Get MeshSet Layout Size
            uint? meshSetLayoutSize = null;
            uint? meshSetVertexSize = null;
            var resEntry = AssetManager.Instance.GetResEntry(meshSet.FullName);
            if (resEntry != null)
            {
                meshSetLayoutSize = BinaryPrimitives.ReadUInt32LittleEndian(resEntry.ResMeta);
                meshSetVertexSize = BinaryPrimitives.ReadUInt32LittleEndian(resEntry.ResMeta.AsSpan(4));
            }
            nativeReader.Pad(16);
            foreach (MeshSetLod lod in meshSet.Lods)
            {
                for (int l = 0; l < lod.CategorySubsetIndices.Count; l++)
                {
                    for (int j2 = 0; j2 < lod.CategorySubsetIndices[l].Count; j2++)
                    {
                        lod.CategorySubsetIndices[l][j2] = nativeReader.ReadByte();
                    }
                }
            }
            nativeReader.Pad(16);
            foreach (MeshSetLod lod in meshSet.Lods)
            {
                nativeReader.Position += lod.AdjacencyBufferSize;
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
            //nativeReader.Pad(16);
            if (meshSetLayoutSize.HasValue && meshSetVertexSize.HasValue)
            { 
                nativeReader.Position = meshSetLayoutSize.Value;
                foreach (MeshSetLod lod in meshSet.Lods)
                {
                    lod.ReadInlineData(nativeReader);
                }
            }
        }
    }
}
