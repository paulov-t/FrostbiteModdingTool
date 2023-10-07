using FMT.FileTools;
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

namespace FC24Plugin.Meshes.Writers
{
    public class FC24MeshSetWriter : IMeshSetWriter
    {
        public void Write(NativeWriter writer, MeshSet meshSet, MeshContainer meshContainer)
        {
            writer.WriteAxisAlignedBox(meshSet.BoundingBox);
            for (int i = 0; i < meshSet.MaxLodCount; i++)
            {
                if (i < meshSet.Lods.Count)
                {
                    meshContainer.WriteRelocPtr("LOD", meshSet.Lods[i], writer);
                }
                else
                {
                    writer.WriteUInt64LittleEndian(0uL);
                }
            }
            writer.Write(meshSet.UnknownPostLODCount);
            meshContainer.WriteRelocPtr("STR", meshSet.fullName, writer);
            meshContainer.WriteRelocPtr("STR", meshSet.Name, writer);
            writer.Write(meshSet.nameHash);
            writer.Write((byte)meshSet.Type);
            writer.Write((byte)meshSet.FIFA23_Type2);
            writer.Write(meshSet.UnknownBytes[0]);
            for (int n = 0; n < meshSet.MaxLodCount * 2; n++)
            {
                writer.Write((ushort)meshSet.LodFade[n]);
            }
            writer.Write((byte)meshSet.MeshSetLayoutFlags);
            writer.Write(meshSet.unknownUInts[0]);
            writer.Write(meshSet.unknownUInts[1]);
            writer.Position -= 1;
            writer.Write((byte)meshSet.ShaderDrawOrder);
            writer.Write((byte)meshSet.ShaderDrawOrderUserSlot);
            writer.Write((ushort)meshSet.ShaderDrawOrderSubOrder);

            var sumOfLOD = (ushort)(meshSet.Lods.Sum(x => x.Sections.Count));
            writer.WriteUInt16LittleEndian((ushort)meshSet.Lods.Count);
            writer.WriteUInt16LittleEndian(meshSet.MeshCount);

            if (meshSet.Type == MeshType.MeshType_Skinned)
            {
                writer.Write(meshSet.UnknownBytes[1]);
                writer.WriteUInt16LittleEndian((ushort)meshSet.boneCount);
                writer.WriteUInt16LittleEndian((ushort)meshSet.CullBoxCount);
                if (meshSet.CullBoxCount > 0)
                {
                    meshContainer.WriteRelocPtr("BONEINDICES", meshSet.boneIndices, writer);
                    meshContainer.WriteRelocPtr("BONEBBOXES", meshSet.boneBoundingBoxes, writer);
                }
            }

            else if (meshSet.Type == MeshType.MeshType_Composite)
            {
                writer.WriteUInt16LittleEndian((ushort)meshSet.boneIndices.Count);
                writer.WriteUInt16LittleEndian(0);
                meshContainer.WriteRelocPtr("BONEINDICES", meshSet.boneIndices, writer);
                meshContainer.WriteRelocPtr("BONEBBOXES", meshSet.boneBoundingBoxes, writer);
            }
            writer.WritePadding(16);
            foreach (MeshSetLod lod2 in meshSet.Lods)
            {
                meshContainer.AddOffset("LOD", lod2, writer);
                lod2.Write(writer, meshContainer);
            }
            foreach (MeshSetLod lod3 in meshSet.Lods)
            {
                meshContainer.AddOffset("SECTION", lod3.Sections, writer);
                foreach (MeshSetSection section3 in lod3.Sections)
                {
                    section3.Process(writer, meshContainer);
                }
            }
            writer.WritePadding(16);
            foreach (MeshSetLod lod5 in meshSet.Lods)
            {
                foreach (MeshSetSection section2 in lod5.Sections)
                {
                    if (section2.BoneList.Count == 0)
                    {
                        continue;
                    }
                    meshContainer.AddOffset("BONELIST", section2.BoneList, writer);
                    foreach (ushort bone in section2.BoneList)
                    {
                        writer.WriteUInt16LittleEndian(bone);
                    }
                }
            }
            writer.WritePadding(16);
            meshContainer.WriteStrings(writer);
            writer.WritePadding(16);
            foreach (MeshSetLod lod6 in meshSet.Lods)
            {
                foreach (List<byte> categorySubsetIndex in lod6.CategorySubsetIndices)
                {
                    meshContainer.AddOffset("SUBSET", categorySubsetIndex, writer);
                    writer.WriteBytes(categorySubsetIndex.ToArray());
                }
            }
            writer.WritePadding(16);
            if (meshSet.Type != MeshType.MeshType_Skinned)
            {
                return;
            }
            foreach (MeshSetLod lod4 in meshSet.Lods)
            {
                meshContainer.AddOffset("BONES", lod4.BoneIndexArray, writer);
                foreach (uint item in lod4.BoneIndexArray)
                {
                    writer.Write((uint)item);
                }
            }
            writer.WritePadding(16);
            meshContainer.AddOffset("BONEINDICES", meshSet.boneIndices, writer);
            //foreach (ushort boneIndex in CullBoxCount)
            for (var iCB = 0; iCB < meshSet.CullBoxCount; iCB++)
            {
                //writer.WriteUInt16LittleEndian(boneIndex);
                writer.WriteUInt16LittleEndian(meshSet.boneIndices[iCB]);
            }
            writer.WritePadding(16);
            meshContainer.AddOffset("BONEBBOXES", meshSet.boneBoundingBoxes, writer);
            for (var iCB = 0; iCB < meshSet.CullBoxCount; iCB++)
            //foreach (AxisAlignedBox boneBoundingBox in boneBoundingBoxes)
            {
                writer.WriteAxisAlignedBox(meshSet.boneBoundingBoxes[iCB]);
            }
            writer.WritePadding(16);
        }
    }
}
