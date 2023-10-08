using FMT.FileTools;
using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Resources;
using System.Collections.Generic;

namespace FC24Plugin.Meshes.Writers
{
    public class FC24MeshSetLodWriter : IMeshSetLodWriter
    {
        public void Write(NativeWriter writer, MeshContainer meshContainer, MeshSetLod meshSetLod)
        {
            writer.Write((int)meshSetLod.Type);
            writer.Write(meshSetLod.maxInstances);
            meshContainer.WriteRelocArray("SECTION", meshSetLod.Sections, writer);
            foreach (List<byte> subsetCategory in meshSetLod.CategorySubsetIndices)
            {
                meshContainer.WriteRelocArray("SUBSET", subsetCategory, writer);
            }
            writer.Write((int)meshSetLod.Flags);
            writer.Write(meshSetLod.indexBufferFormat.format);
            writer.Write(meshSetLod.IndexBufferSize);
            writer.Write(meshSetLod.VertexBufferSize);
            if (meshSetLod.HasAdjacencyInMesh)
            {
                writer.Write(0);
            }
            writer.Write(meshSetLod.UnknownChunkPad);
            writer.WriteGuid(meshSetLod.ChunkId);

            writer.Write(meshSetLod.inlineDataOffset);
            if (meshSetLod.HasAdjacencyInMesh)
            {
                if (meshSetLod.inlineDataOffset != uint.MaxValue)
                {
                    meshContainer.WriteRelocPtr("ADJACENCY", meshSetLod.adjacencyData, writer);
                }
                else
                {
                    writer.WriteUInt64LittleEndian(0uL);
                }
            }
            meshContainer.WriteRelocPtr("STR", meshSetLod.shaderDebugName, writer);
            meshContainer.WriteRelocPtr("STR", meshSetLod.name, writer);
            meshContainer.WriteRelocPtr("STR", meshSetLod.shortName, writer);
            writer.Write(meshSetLod.nameHash);
            writer.WriteInt64LittleEndian(meshSetLod.UnknownLongAfterNameHash);
            if (meshSetLod.Type == MeshType.MeshType_Skinned)
            {
                writer.Write(meshSetLod.BoneIndexArray.Count);
                meshContainer.WriteRelocPtr("BONES", meshSetLod.BoneIndexArray, writer);
            }
            else if (meshSetLod.Type == MeshType.MeshType_Composite)
            {
            }
            writer.WritePadding(16);
        }
    }
}
