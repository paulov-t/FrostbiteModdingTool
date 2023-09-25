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

namespace FC24Plugin.Meshes
{
    internal class FC24MeshSetReader : IMeshSetReader
    {
        private FC24SkinnedMeshReader SkinnedMeshReader { get; } = new FC24SkinnedMeshReader();
        private FC24CompositeMeshReader CompositeMeshReader { get; } = new FC24CompositeMeshReader();
        private FC24RigidMeshReader RigidMeshReader { get; } = new FC24RigidMeshReader();

        public int MaxLodCount => (int)MeshLimits.MaxMeshLodCount;

        public void Read(NativeReader nativeReader, MeshSet meshSet)
        {
            meshSet.BoundingBox = nativeReader.ReadAxisAlignedBox();
            for (int i2 = 0; i2 < MaxLodCount; i2++)
            {
                meshSet.LodOffsets.Add(nativeReader.ReadLong());
            }
            meshSet.UnknownPostLODCount = nativeReader.ReadLong();
            _ = nativeReader.ReadLong();
            _ = nativeReader.ReadLong();
            meshSet.nameHash = nativeReader.ReadUInt();
            meshSet.Type = (MeshType)nativeReader.ReadByte();
            if(meshSet.Type == MeshType.MeshType_Skinned)
            {
                SkinnedMeshReader.Read(nativeReader, meshSet);
                return;
            }
            else if (meshSet.Type == MeshType.MeshType_Composite)
            {
                CompositeMeshReader.Read(nativeReader, meshSet);
                return;
            }
            else
            {
                RigidMeshReader.Read(nativeReader, meshSet);
                return;
            }
        }
    }
}
