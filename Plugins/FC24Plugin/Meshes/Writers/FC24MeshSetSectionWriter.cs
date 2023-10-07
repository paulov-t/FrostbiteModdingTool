using FMT.FileTools;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Xml.Linq;

namespace FC24Plugin.Meshes.Writers
{
    public class FC24MeshSetSectionWriter : IMeshSetSectionWriter
    {
        public void Write(NativeWriter writer, MeshSetSection section, MeshContainer meshContainer)
        {
            // 0
            writer.Write(section.Offset1);

            // name
            meshContainer.WriteRelocPtr("STR", section.SectionIndex + ":" + section.Name, writer);
            
            // bones
            if (section.BoneList.Count > 0)
                meshContainer.WriteRelocPtr("BONELIST", section.BoneList, writer);
            else
                writer.WriteUInt64LittleEndian(0uL);

            // bone count
            writer.WriteUInt16LittleEndian((ushort)section.BoneList.Count);
            writer.Write((byte)section.BonesPerVertex);
            writer.WriteUInt16LittleEndian((ushort)section.MaterialId);
            writer.Write((byte)0);
            writer.Write((byte)section.VertexStride);
            writer.Write((byte)section.PrimitiveType);
            writer.WriteUInt32LittleEndian(section.PrimitiveCount);
            writer.WriteUInt32LittleEndian(section.StartIndex);
            writer.WriteUInt32LittleEndian(section.VertexOffset);
            writer.WriteUInt32LittleEndian(section.VertexCount);
            writer.WriteBytes(section.UnknownBytes[0]);

            for (int l = 0; l < 6; l++)
            {
                writer.WriteSingleLittleEndian(section.TextureCoordinateRatios[l]);
            }

            for (int i = 0; i < section.DeclCount; i++)
            {
                for (int j = 0; j < section.GeometryDeclDesc[i].Elements.Length; j++)
                {
                    writer.Write((byte)section.GeometryDeclDesc[i].Elements[j].Usage);
                    writer.Write((byte)section.GeometryDeclDesc[i].Elements[j].Format);
                    writer.Write(section.GeometryDeclDesc[i].Elements[j].Offset);
                    writer.Write(section.GeometryDeclDesc[i].Elements[j].StreamIndex);
                }
                for (int k = 0; k < section.GeometryDeclDesc[i].Streams.Length; k++)
                {
                    writer.Write(section.GeometryDeclDesc[i].Streams[k].VertexStride);
                    writer.Write((byte)section.GeometryDeclDesc[i].Streams[k].Classification);
                }
                writer.Write(section.GeometryDeclDesc[i].ElementCount);
                writer.Write(section.GeometryDeclDesc[i].StreamCount);
                writer.WriteUInt16LittleEndian(0);
            }

            writer.WriteBytes(section.UnknownBytes[1]);

        }
    }
}
