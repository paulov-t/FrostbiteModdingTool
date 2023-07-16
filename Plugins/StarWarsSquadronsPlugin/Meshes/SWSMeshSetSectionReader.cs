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
            nativeReader.Position = startPosition;
            section.SectionIndex = index;

            // Always 0
            section.Offset1 = nativeReader.ReadInt64LittleEndian();
            // Always 0
            section.Offset2 = nativeReader.ReadInt64LittleEndian();
            // Get the section name
            section.Name = nativeReader.ReadNullTerminatedString(offset: nativeReader.ReadInt64LittleEndian());

            _ = nativeReader.ReadUInt();

            section.MaterialId = nativeReader.ReadInt();
            section.PrimitiveCount = (uint)nativeReader.ReadUInt();
            section.StartIndex = nativeReader.ReadUInt();
            section.VertexOffset = nativeReader.ReadUInt();
            section.VertexCount = nativeReader.ReadUInt();
            section.VertexStride = nativeReader.ReadByte();
            section.PrimitiveType = (PrimitiveType)nativeReader.ReadByte();
            section.BonesPerVertex = (byte)nativeReader.ReadByte();
            section.BoneCount = nativeReader.ReadByte();

            /*
             *  Offset1 = reader.ReadInt64LittleEndian(); // 0
            long namePosition = reader.ReadInt64LittleEndian();
            long bonePositions = reader.ReadInt64LittleEndian();
            BoneCount = reader.ReadUInt16LittleEndian(); //438
            BonesPerVertex = (byte)reader.ReadByte(); // 8
            MaterialId = reader.ReadUShort(); // 28
            StartIndex = reader.ReadByte(); // 0 ? 
            VertexStride = reader.ReadByte(); // 68
            PrimitiveType = (PrimitiveType)reader.ReadByte(); // 3
            PrimitiveCount = (uint)reader.ReadUInt32LittleEndian();
            StartIndex = reader.ReadUInt32LittleEndian(); // 0
            VertexOffset = reader.ReadUInt32LittleEndian(); // 0
            VertexCount = (uint)reader.ReadUInt32LittleEndian(); // 3157
            UnknownInt = reader.ReadUInt(); // 0
            FIFA23_UnknownInt1 = reader.ReadUInt(); // 0 
            FIFA23_UnknownInt2 = reader.ReadUInt(); // 0
            for (int l = 0; l < 6; l++)
            {
                TextureCoordinateRatios.Add(reader.ReadSingleLittleEndian());
                // Texture Coords are usually a over 1 float (e.g. 2.11203742) and then 5 floats of exactly 1.0
            }
             */

            //long bonePositions = nativeReader.ReadInt64LittleEndian();
            //BoneCount = nativeReader.ReadUInt16LittleEndian(); //438
            //BonesPerVertex = (byte)nativeReader.ReadByte();
            //StartIndex = nativeReader.ReadByte(); // 0 ? 
            //VertexStride = nativeReader.ReadByte(); // 68
            //PrimitiveType = (PrimitiveType)nativeReader.ReadByte(); // 3
            //PrimitiveCount = (uint)nativeReader.ReadUInt32LittleEndian();
            //StartIndex = nativeReader.ReadUInt32LittleEndian();
            //VertexOffset = nativeReader.ReadUInt32LittleEndian();
            //VertexCount = (uint)nativeReader.ReadUInt32LittleEndian(); // 3157
            //section.VertexCount = nativeReader.ReadUInt();
            section.UnknownInt = nativeReader.ReadUInt();
            section.UnknownInt = nativeReader.ReadUInt();
            section.UnknownInt = nativeReader.ReadUInt();
            section.BonesPerVertex = nativeReader.ReadByte();
            nativeReader.ReadByte();
            section.BoneCount = nativeReader.ReadUShort();

            section.BoneListOffset = nativeReader.ReadLong();
            nativeReader.ReadULong();
            //for (int l = 0; l < 6; l++)
            //{
            //    TextureCoordinateRatios.Add(nativeReader.ReadSingleLittleEndian());
            //}

            //nativeReader.ReadUInt();
            //nativeReader.ReadUInt();
            //nativeReader.ReadUInt();

            //section.BonesPerVertex = nativeReader.ReadByte();
            //nativeReader.ReadByte();
            //section.BoneCount = nativeReader.ReadUShort();

            var positionBeforeGeoDeclDec = nativeReader.Position;
            nativeReader.Position = positionBeforeGeoDeclDec;

            for (int i = 0; i < 2; i++)
            {
                section.GeometryDeclDesc[i].Elements = new GeometryDeclarationDesc.Element[GeometryDeclarationDesc.MaxElements];
                section.GeometryDeclDesc[i].Streams = new GeometryDeclarationDesc.Stream[GeometryDeclarationDesc.MaxStreams];
                for (int j = 0; j < GeometryDeclarationDesc.MaxElements; j++)
                {
                    GeometryDeclarationDesc.Element element = new GeometryDeclarationDesc.Element
                    {
                        Usage = (VertexElementUsage)nativeReader.ReadByte(),
                        Format = (VertexElementFormat)nativeReader.ReadByte(),
                        Offset = nativeReader.ReadByte(),
                        StreamIndex = nativeReader.ReadByte()
                    };
                    section.GeometryDeclDesc[i].Elements[j] = element;
                }
                for (int k = 0; k < GeometryDeclarationDesc.MaxStreams; k++)
                {
                    GeometryDeclarationDesc.Stream stream = new GeometryDeclarationDesc.Stream
                    {
                        VertexStride = nativeReader.ReadByte(),
                        Classification = (VertexElementClassification)nativeReader.ReadByte()
                    };
                    section.GeometryDeclDesc[i].Streams[k] = stream;
                }
                section.GeometryDeclDesc[i].ElementCount = nativeReader.ReadByte();
                section.GeometryDeclDesc[i].StreamCount = nativeReader.ReadByte();
                nativeReader.ReadBytes(2);
            }

            var sizeOfGeo = nativeReader.Position - positionBeforeGeoDeclDec;
            _ = nativeReader.Position;

            for (int i = 0; i < 6; i++)
            {
                section.TextureCoordinateRatios.Add(nativeReader.ReadFloat());
            }


            section.UnknownData = nativeReader.ReadBytes(44);
            section.ReadBones(nativeReader, section.BoneListOffset);
        }
    }
}
