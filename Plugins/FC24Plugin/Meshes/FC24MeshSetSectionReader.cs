using FMT.FileTools;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace FC24Plugin.Meshes
{
    internal class FC24MeshSetSectionReader : IMeshSetSectionReader
    {
        
        public void Read(NativeReader nativeReader, MeshSetSection section, int index)
        {
            var startPosition = nativeReader.Position;
            nativeReader.Position = startPosition;

            section.SectionIndex = index;
            section.Offset1 = nativeReader.ReadInt64LittleEndian();
            section.Name = nativeReader.ReadNullTerminatedString(offset: nativeReader.ReadInt64LittleEndian());
            ReadBonesAtPosition(nativeReader, nativeReader.ReadInt64LittleEndian());
            //long namePosition = nativeReader.ReadInt64LittleEndian();
            section.BoneCount = nativeReader.ReadUInt16LittleEndian();
            section.BonesPerVertex = (byte)nativeReader.ReadByte();
            _ = nativeReader.ReadByte();
            section.MaterialId = nativeReader.ReadUShort();
            section.VertexStride = nativeReader.ReadByte(); 
            section.PrimitiveType = (PrimitiveType)nativeReader.ReadByte(); 
            section.PrimitiveCount = (uint)nativeReader.ReadUInt32LittleEndian();
            section.StartIndex = nativeReader.ReadUInt32LittleEndian();
            section.VertexOffset = nativeReader.ReadUInt32LittleEndian();
            section.VertexCount = (uint)nativeReader.ReadUInt32LittleEndian(); 
            section.UnknownInt = nativeReader.ReadUInt();

            for (int i = 0; i < 6; i++)
            {
                section.TextureCoordinateRatios.Add(nativeReader.ReadFloat());
            }

            for (int geomDeclId = 0; geomDeclId < section.DeclCount; geomDeclId++)
            {
                section.GeometryDeclDesc[geomDeclId].Elements = new GeometryDeclarationDesc.Element[GeometryDeclarationDesc.MaxElements];
                section.GeometryDeclDesc[geomDeclId].Streams = new GeometryDeclarationDesc.Stream[GeometryDeclarationDesc.MaxStreams];

                for (int i = 0; i < GeometryDeclarationDesc.MaxElements; i++)
                {
                    GeometryDeclarationDesc.Element elem = new GeometryDeclarationDesc.Element
                    {
                        Usage = (VertexElementUsage)nativeReader.ReadByte(),
                        Format = (VertexElementFormat)nativeReader.ReadByte(),
                        Offset = nativeReader.ReadByte(),
                        StreamIndex = nativeReader.ReadByte()
                    };

                    section.GeometryDeclDesc[geomDeclId].Elements[i] = elem;
                }
                for (int i = 0; i < GeometryDeclarationDesc.MaxStreams; i++)
                {
                    GeometryDeclarationDesc.Stream stream = new GeometryDeclarationDesc.Stream
                    {
                        VertexStride = nativeReader.ReadByte(),
                        Classification = (VertexElementClassification)nativeReader.ReadByte()
                    };

                    section.GeometryDeclDesc[geomDeclId].Streams[i] = stream;
                }

                section.GeometryDeclDesc[geomDeclId].ElementCount = nativeReader.ReadByte();
                section.GeometryDeclDesc[geomDeclId].StreamCount = nativeReader.ReadByte();
                nativeReader.ReadBytes(2); // padding
            }

            section.UnknownData = nativeReader.ReadBytes(48);
        }

        private void ReadBonesAtPosition(NativeReader nativeReader, long position)
        {
            var startPosition = nativeReader.Position;
            nativeReader.Position = position;



            nativeReader.Position = startPosition;
        }
    }
}
