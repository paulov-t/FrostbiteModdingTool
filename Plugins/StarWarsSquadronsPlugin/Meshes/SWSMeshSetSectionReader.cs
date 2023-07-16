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

namespace StarWarsSquadronsPlugin.Meshes
{
    internal class SWSMeshSetSectionReader : IMeshSetSectionReader
    {
        public void Read(NativeReader nativeReader, MeshSetSection section, int index)
        {
            var startPosition = nativeReader.Position;

            section.SectionIndex = index;
            section.Offset1 = nativeReader.ReadInt64LittleEndian();
            section.Offset2 = nativeReader.ReadInt64LittleEndian();
            section.Name = nativeReader.ReadNullTerminatedString(offset: nativeReader.ReadInt64LittleEndian());

            //long bonePositions = nativeReader.ReadInt64LittleEndian();
            //BoneCount = nativeReader.ReadUInt16LittleEndian(); //438
            //BonesPerVertex = (byte)nativeReader.ReadByte();
            //MaterialId = nativeReader.ReadUShort();
            //StartIndex = nativeReader.ReadByte(); // 0 ? 
            //VertexStride = nativeReader.ReadByte(); // 68
            //PrimitiveType = (PrimitiveType)nativeReader.ReadByte(); // 3
            //PrimitiveCount = (uint)nativeReader.ReadUInt32LittleEndian();
            //StartIndex = nativeReader.ReadUInt32LittleEndian();
            //VertexOffset = nativeReader.ReadUInt32LittleEndian();
            //VertexCount = (uint)nativeReader.ReadUInt32LittleEndian(); // 3157
            //UnknownInt = nativeReader.ReadUInt();
            //UnknownInt = nativeReader.ReadUInt();
            //UnknownInt = nativeReader.ReadUInt();

            //for (int l = 0; l < 6; l++)
            //{
            //    TextureCoordinateRatios.Add(nativeReader.ReadSingleLittleEndian());
            //}

            //for (int i = 0; i < DeclCount; i++)
            //{
            //    GeometryDeclDesc[i].Elements = new GeometryDeclarationDesc.Element[GeometryDeclarationDesc.MaxElements];
            //    GeometryDeclDesc[i].Streams = new GeometryDeclarationDesc.Stream[GeometryDeclarationDesc.MaxStreams];
            //    for (int j = 0; j < GeometryDeclarationDesc.MaxElements; j++)
            //    {
            //        GeometryDeclarationDesc.Element element = new GeometryDeclarationDesc.Element
            //        {
            //            Usage = (VertexElementUsage)nativeReader.ReadByte(),
            //            Format = (VertexElementFormat)nativeReader.ReadByte(),
            //            Offset = nativeReader.ReadByte(),
            //            StreamIndex = nativeReader.ReadByte()
            //        };
            //        GeometryDeclDesc[i].Elements[j] = element;
            //    }
            //    for (int k = 0; k < GeometryDeclarationDesc.MaxStreams; k++)
            //    {
            //        GeometryDeclarationDesc.Stream stream = new GeometryDeclarationDesc.Stream
            //        {
            //            VertexStride = nativeReader.ReadByte(),
            //            Classification = (VertexElementClassification)nativeReader.ReadByte()
            //        };
            //        GeometryDeclDesc[i].Streams[k] = stream;
            //    }
            //    GeometryDeclDesc[i].ElementCount = nativeReader.ReadByte();
            //    GeometryDeclDesc[i].StreamCount = nativeReader.ReadByte();
            //    nativeReader.ReadBytes(2);
            //}
            //UnknownData = nativeReader.ReadBytes(56);
            //ReadBones(nativeReader, bonePositions);
        }
    }
}
