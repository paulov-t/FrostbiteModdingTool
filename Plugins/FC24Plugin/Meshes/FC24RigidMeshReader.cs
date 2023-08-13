using FMT.FileTools;
using FrostySdk.FrostySdk.Resources;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FC24Plugin.Meshes
{
    internal class FC24RigidMeshReader
    {
        public int MaxLodCount => (int)MeshLimits.MaxMeshLodCount;

        public void Read(NativeReader nativeReader, MeshSet meshSet)
        {
            nativeReader.Position = 0;

            meshSet.BoundingBox = nativeReader.ReadAxisAlignedBox();
            long[] lodOffsets = new long[MaxLodCount];
            for (int i2 = 0; i2 < MaxLodCount; i2++)
            {
                lodOffsets[i2] = nativeReader.ReadLong();
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
            var lodsCount = nativeReader.ReadUShort();
            meshSet.MeshCount = nativeReader.ReadUShort();

            for (var iL = 0; iL < lodsCount; iL++)
            {
                meshSet.PositionsOfLodMeshSet.Add(nativeReader.ReadUShort());
            }
            nativeReader.ReadBytes(10);

            for (int iL = 0; iL < lodsCount; iL++)
            {
                nativeReader.Position = lodOffsets[iL];
                MeshSetLod lod = new MeshSetLod(nativeReader, meshSet);
                lod.SetParts(meshSet.partTransforms, meshSet.partBoundingBoxes);
                meshSet.Lods.Add(lod);
            }
        }
    }
}
