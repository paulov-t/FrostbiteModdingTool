using FMT.FileTools;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;

namespace FrostySdk.Resources
{

    public class MeshSetSection
    {
        public TangentSpaceCompressionType TangentSpaceCompressionType;

        private long offset1;

        private uint unknownInt2;

        private ulong unknownLong;

        private byte vertexStride;

        private byte bonesPerVertex;

        private List<float> texCoordRatios = new List<float>();

        private byte[] unknownData;

        private int sectionIndex;

        public string Name { get; set; }

        public int MaterialId { get; set; }

        public uint UnknownInt { get; set; }

        public uint PrimitiveCount { get; set; }

        public uint StartIndex { get; set; }

        public uint VertexOffset { get; set; }

        public uint VertexCount { get; set; }

        public GeometryDeclarationDesc[] GeometryDeclDesc { get; } = new GeometryDeclarationDesc[2];


        public List<ushort> BoneList { get; set; } = new List<ushort>();


        public uint VertexStride => vertexStride;

        public PrimitiveType PrimitiveType { get; set; }

        public byte BonesPerVertex
        {
            get
            {
                return bonesPerVertex;
            }
            set
            {
                bonesPerVertex = value;
                if (bonesPerVertex > 8)
                {
                    bonesPerVertex = 8;
                }
            }
        }

        public int DeclCount => 1;

        public bool HasUnknown { get; }

        public bool HasUnknown2 { get; }

        public bool HasUnknown3 { get; }

        public ushort BoneCount { get; private set; }

        internal MeshSetSection()
        {
        }

        private void ReadBones(FileReader reader, long bonePositions)
        {
            long startPosition = reader.Position;
            reader.Position = bonePositions;
            for (int m = 0; m < BoneCount; m++)
            {
                BoneList.Add(reader.ReadUInt16LittleEndian());
            }
            reader.Position = startPosition;
        }

        public MeshSetSection(FileReader reader, int index)
        {
            switch(ProfileManager.Game)
            {
                case FMT.FileTools.Modding.EGame.FIFA23:
                    Read23(reader, index); break;
                case FMT.FileTools.Modding.EGame.FIFA22:
                case FMT.FileTools.Modding.EGame.MADDEN22:
                    Read22(reader, index); break;
                case FMT.FileTools.Modding.EGame.FIFA21:
                    Read21(reader, index); break;
                case FMT.FileTools.Modding.EGame.NFSUnbound:
                    ReadNFSU(reader, index); break;

            }

            //if (ProfileManager.IsFIFA23DataVersion())
            //{
            //    Read23(reader, index);
            //}
            //else if (ProfileManager.IsFIFA22DataVersion()
            //    || ProfileManager.IsMadden22DataVersion(ProfileManager.Game)
            //    )
            //{
            //    Read22(reader, index);
            //}
            //else
            //{
            //    Read21(reader, index);

            //}

        }

        private void ReadNFSU(FileReader reader, int index)
        {
            var startPosition = reader.Position;

            sectionIndex = index;
            offset1 = reader.ReadInt64LittleEndian();
            long namePosition = reader.ReadInt64LittleEndian();
            var preNamePosition = reader.Position;
            reader.Position = namePosition;
            Name = reader.ReadNullTerminatedString();
            reader.Position = preNamePosition;
            long bonePositions = reader.ReadInt64LittleEndian();
            BoneCount = reader.ReadUInt16LittleEndian(); //438
            BonesPerVertex = (byte)reader.ReadByte();
            MaterialId = reader.ReadUShort();
            StartIndex = reader.ReadByte(); // 0 ? 
            vertexStride = reader.ReadByte(); // 68
            PrimitiveType = (PrimitiveType)reader.ReadByte(); // 3
            PrimitiveCount = (uint)reader.ReadUInt32LittleEndian();
            StartIndex = reader.ReadUInt32LittleEndian();
            VertexOffset = reader.ReadUInt32LittleEndian();
            VertexCount = (uint)reader.ReadUInt32LittleEndian(); // 3157
            UnknownInt = reader.ReadUInt();
            UnknownInt = reader.ReadUInt();
            UnknownInt = reader.ReadUInt();

            for (int l = 0; l < 6; l++)
            {
                texCoordRatios.Add(reader.ReadSingleLittleEndian());
            }

            for (int i = 0; i < DeclCount; i++)
            {
                GeometryDeclDesc[i].Elements = new GeometryDeclarationDesc.Element[GeometryDeclarationDesc.MaxElements];
                GeometryDeclDesc[i].Streams = new GeometryDeclarationDesc.Stream[GeometryDeclarationDesc.MaxStreams];
                for (int j = 0; j < GeometryDeclarationDesc.MaxElements; j++)
                {
                    GeometryDeclarationDesc.Element element = new GeometryDeclarationDesc.Element
                    {
                        Usage = (VertexElementUsage)reader.ReadByte(),
                        Format = (VertexElementFormat)reader.ReadByte(),
                        Offset = reader.ReadByte(),
                        StreamIndex = reader.ReadByte()
                    };
                    GeometryDeclDesc[i].Elements[j] = element;
                }
                for (int k = 0; k < GeometryDeclarationDesc.MaxStreams; k++)
                {
                    GeometryDeclarationDesc.Stream stream = new GeometryDeclarationDesc.Stream
                    {
                        VertexStride = reader.ReadByte(),
                        Classification = (VertexElementClassification)reader.ReadByte()
                    };
                    GeometryDeclDesc[i].Streams[k] = stream;
                }
                GeometryDeclDesc[i].ElementCount = reader.ReadByte();
                GeometryDeclDesc[i].StreamCount = reader.ReadByte();
                reader.ReadBytes(2);
            }
            unknownData = reader.ReadBytes(56);
            ReadBones(reader, bonePositions);
        }

        private uint FIFA23_UnknownInt1;
        private uint FIFA23_UnknownInt2;

        public void Read23(FileReader reader, int index)
        {
            var startPosition = reader.Position;

            sectionIndex = index;
            offset1 = reader.ReadInt64LittleEndian();
            long namePosition = reader.ReadInt64LittleEndian();
            long bonePositions = reader.ReadInt64LittleEndian();
            BoneCount = reader.ReadUInt16LittleEndian(); //438
            BonesPerVertex = (byte)reader.ReadByte();
            MaterialId = reader.ReadUShort();
            StartIndex = reader.ReadByte(); // 0 ? 
            vertexStride = reader.ReadByte(); // 68
            PrimitiveType = (PrimitiveType)reader.ReadByte(); // 3
            PrimitiveCount = (uint)reader.ReadUInt32LittleEndian();
            StartIndex = reader.ReadUInt32LittleEndian();
            VertexOffset = reader.ReadUInt32LittleEndian();
            VertexCount = (uint)reader.ReadUInt32LittleEndian(); // 3157
            UnknownInt = reader.ReadUInt();
            FIFA23_UnknownInt1 = reader.ReadUInt(); // hmmm
            FIFA23_UnknownInt2 = reader.ReadUInt(); // hmmm more unknownness
            for (int l = 0; l < 6; l++)
            {
                texCoordRatios.Add(reader.ReadSingleLittleEndian());
            }

            for (int i = 0; i < DeclCount; i++)
            {
                GeometryDeclDesc[i].Elements = new GeometryDeclarationDesc.Element[GeometryDeclarationDesc.MaxElements];
                GeometryDeclDesc[i].Streams = new GeometryDeclarationDesc.Stream[GeometryDeclarationDesc.MaxStreams];
                for (int j = 0; j < GeometryDeclarationDesc.MaxElements; j++)
                {
                    GeometryDeclarationDesc.Element element = new GeometryDeclarationDesc.Element
                    {
                        Usage = (VertexElementUsage)reader.ReadByte(),
                        Format = (VertexElementFormat)reader.ReadByte(),
                        Offset = reader.ReadByte(),
                        StreamIndex = reader.ReadByte()
                    };
                    GeometryDeclDesc[i].Elements[j] = element;
                }
                for (int k = 0; k < GeometryDeclarationDesc.MaxStreams; k++)
                {
                    GeometryDeclarationDesc.Stream stream = new GeometryDeclarationDesc.Stream
                    {
                        VertexStride = reader.ReadByte(),
                        Classification = (VertexElementClassification)reader.ReadByte()
                    };
                    GeometryDeclDesc[i].Streams[k] = stream;
                }
                GeometryDeclDesc[i].ElementCount = reader.ReadByte();
                GeometryDeclDesc[i].StreamCount = reader.ReadByte();
                reader.ReadBytes(2);
            }
            //unknownInt2 = reader.ReadUInt32LittleEndian();
            unknownData = reader.ReadBytes(56);
            long position3 = reader.Position;
            reader.Position = bonePositions;
            for (int m = 0; m < BoneCount; m++)
            {
                BoneList.Add(reader.ReadUInt16LittleEndian());
            }
            //reader.Position = namePosition;
            Name = reader.ReadNullTerminatedString(false, offset: namePosition);
            reader.Position = position3;
        }

        public void Read22(FileReader reader, int index)
        {
            var startPosition = reader.Position;

            sectionIndex = index;
            offset1 = reader.ReadInt64LittleEndian();
            long namePosition = reader.ReadInt64LittleEndian();
            long bonePositions = reader.ReadInt64LittleEndian();
            BoneCount = reader.ReadUInt16LittleEndian();
            BonesPerVertex = (byte)reader.ReadUShort();
            MaterialId = reader.ReadUInt16LittleEndian();
            vertexStride = reader.ReadByte();
            PrimitiveType = (PrimitiveType)reader.ReadByte();
            PrimitiveCount = (uint)reader.ReadUInt32LittleEndian();
            StartIndex = reader.ReadUInt32LittleEndian();
            VertexOffset = reader.ReadUInt32LittleEndian();
            VertexCount = (uint)reader.ReadUInt32LittleEndian();
            UnknownInt = reader.ReadUInt();
            for (int l = 0; l < 6; l++)
            {
                texCoordRatios.Add(reader.ReadSingleLittleEndian());
            }

            for (int i = 0; i < DeclCount; i++)
            {
                GeometryDeclDesc[i].Elements = new GeometryDeclarationDesc.Element[GeometryDeclarationDesc.MaxElements];
                GeometryDeclDesc[i].Streams = new GeometryDeclarationDesc.Stream[GeometryDeclarationDesc.MaxStreams];
                for (int j = 0; j < GeometryDeclarationDesc.MaxElements; j++)
                {
                    GeometryDeclarationDesc.Element element = new GeometryDeclarationDesc.Element
                    {
                        Usage = (VertexElementUsage)reader.ReadByte(),
                        Format = (VertexElementFormat)reader.ReadByte(),
                        Offset = reader.ReadByte(),
                        StreamIndex = reader.ReadByte()
                    };
                    GeometryDeclDesc[i].Elements[j] = element;
                }
                for (int k = 0; k < GeometryDeclarationDesc.MaxStreams; k++)
                {
                    GeometryDeclarationDesc.Stream stream = new GeometryDeclarationDesc.Stream
                    {
                        VertexStride = reader.ReadByte(),
                        Classification = (VertexElementClassification)reader.ReadByte()
                    };
                    GeometryDeclDesc[i].Streams[k] = stream;
                }
                GeometryDeclDesc[i].ElementCount = reader.ReadByte();
                GeometryDeclDesc[i].StreamCount = reader.ReadByte();
                reader.ReadBytes(2);
            }
            //unknownInt2 = reader.ReadUInt32LittleEndian();
            unknownData = reader.ReadBytes(48);
            long position3 = reader.Position;
            reader.Position = bonePositions;
            for (int m = 0; m < BoneCount; m++)
            {
                BoneList.Add(reader.ReadUInt16LittleEndian());
            }
            reader.Position = namePosition;
            Name = reader.ReadNullTerminatedString();
            reader.Position = position3;
        }

        public void Read21(FileReader reader, int index)
        {
            sectionIndex = index;
            offset1 = reader.ReadInt64LittleEndian();
            long position = reader.ReadInt64LittleEndian();
            MaterialId = reader.ReadInt32LittleEndian();
            UnknownInt = reader.ReadUInt32LittleEndian();
            for (int l = 0; l < 6; l++)
            {
                texCoordRatios.Add(reader.ReadSingleLittleEndian());
            }
            PrimitiveCount = reader.ReadUInt32LittleEndian();  //6014
            StartIndex = reader.ReadUInt32LittleEndian(); // 0
            VertexOffset = reader.ReadUInt32LittleEndian(); // 0
            VertexCount = reader.ReadUInt32LittleEndian(); // 3157
            unknownData = reader.ReadBytes(32);
            vertexStride = reader.ReadByte(); // 68
            PrimitiveType = (PrimitiveType)reader.ReadByte(); // 3
            reader.ReadUInt16LittleEndian();
            bonesPerVertex = (byte)reader.ReadUInt16LittleEndian(); // 6
            BoneCount = reader.ReadUInt16LittleEndian(); // 438
            long position2 = reader.ReadInt64LittleEndian(); // 1824
            unknownLong = reader.ReadUInt64LittleEndian(); // 13113655001588707511
            for (int i = 0; i < DeclCount; i++)
            {
                GeometryDeclDesc[i].Elements = new GeometryDeclarationDesc.Element[GeometryDeclarationDesc.MaxElements];
                GeometryDeclDesc[i].Streams = new GeometryDeclarationDesc.Stream[GeometryDeclarationDesc.MaxStreams];
                for (int j = 0; j < GeometryDeclarationDesc.MaxElements; j++)
                {
                    GeometryDeclarationDesc.Element element = new GeometryDeclarationDesc.Element
                    {
                        Usage = (VertexElementUsage)reader.ReadByte(),
                        Format = (VertexElementFormat)reader.ReadByte(),
                        Offset = reader.ReadByte(),
                        StreamIndex = reader.ReadByte()
                    };
                    GeometryDeclDesc[i].Elements[j] = element;
                }
                for (int k = 0; k < GeometryDeclarationDesc.MaxStreams; k++)
                {
                    GeometryDeclarationDesc.Stream stream = new GeometryDeclarationDesc.Stream
                    {
                        VertexStride = reader.ReadByte(),
                        Classification = (VertexElementClassification)reader.ReadByte()
                    };
                    GeometryDeclDesc[i].Streams[k] = stream;
                }
                GeometryDeclDesc[i].ElementCount = reader.ReadByte();
                GeometryDeclDesc[i].StreamCount = reader.ReadByte();
                reader.ReadBytes(2);
            }
            unknownInt2 = reader.ReadUInt32LittleEndian();
            long position3 = reader.Position;
            reader.Position = position2;
            for (int m = 0; m < BoneCount; m++)
            {
                BoneList.Add(reader.ReadUInt16LittleEndian());
            }
            reader.Position = position;
            Name = reader.ReadNullTerminatedString();
            reader.Position = position3;
        }

        public void SetBones(IEnumerable<ushort> bones)
        {
            if (bones == null)
            {
                throw new ArgumentNullException("bones");
            }
            BoneList.Clear();
            foreach (ushort bone in bones)
            {
                BoneList.Add(bone);
            }
        }

        internal void PreProcess(MeshContainer meshContainer)
        {
            if (meshContainer == null)
            {
                throw new ArgumentNullException("meshContainer");
            }
            meshContainer.AddString(sectionIndex + ":" + Name, Name);
            if (BoneList.Count > 0)
            {
                meshContainer.AddRelocPtr("BONELIST", BoneList);
            }
        }

        internal void Process(NativeWriter writer, MeshContainer meshContainer)
        {
            if (ProfileManager.IsFIFA23DataVersion())
            {
                Process23(writer, meshContainer);
                return;
            }
            writer.WriteInt64LittleEndian(offset1);
            meshContainer.WriteRelocPtr("STR", sectionIndex + ":" + Name, writer);
            writer.WriteInt32LittleEndian(MaterialId);
            writer.WriteUInt32LittleEndian(UnknownInt);
            for (int l = 0; l < 6; l++)
            {
                writer.WriteSingleLittleEndian(texCoordRatios[l]);
            }
            writer.WriteUInt32LittleEndian(PrimitiveCount);
            writer.WriteUInt32LittleEndian(StartIndex);
            writer.WriteUInt32LittleEndian(VertexOffset);
            writer.WriteUInt32LittleEndian(VertexCount);
            writer.WriteBytes(unknownData);
            writer.Write(vertexStride);
            writer.Write((byte)PrimitiveType);
            writer.WriteUInt16LittleEndian(0);
            writer.WriteUInt16LittleEndian(bonesPerVertex);
            writer.WriteUInt16LittleEndian((ushort)BoneList.Count);
            if (BoneList.Count > 0)
            {
                meshContainer.WriteRelocPtr("BONELIST", BoneList, writer);
            }
            else
            {
                writer.WriteUInt64LittleEndian(0uL);
            }
            writer.WriteUInt64LittleEndian(unknownLong);
            for (int i = 0; i < DeclCount; i++)
            {
                for (int j = 0; j < GeometryDeclDesc[i].Elements.Length; j++)
                {
                    writer.Write((byte)GeometryDeclDesc[i].Elements[j].Usage);
                    writer.Write((byte)GeometryDeclDesc[i].Elements[j].Format);
                    writer.Write(GeometryDeclDesc[i].Elements[j].Offset);
                    writer.Write(GeometryDeclDesc[i].Elements[j].StreamIndex);
                }
                for (int k = 0; k < GeometryDeclDesc[i].Streams.Length; k++)
                {
                    writer.Write(GeometryDeclDesc[i].Streams[k].VertexStride);
                    writer.Write((byte)GeometryDeclDesc[i].Streams[k].Classification);
                }
                writer.Write(GeometryDeclDesc[i].ElementCount);
                writer.Write(GeometryDeclDesc[i].StreamCount);
                writer.WriteUInt16LittleEndian(0);
            }
            writer.WriteUInt32LittleEndian(unknownInt2);
        }


        internal void Process23(NativeWriter writer, MeshContainer meshContainer)
        {
            /*
			 * offset1 = reader.ReadInt64LittleEndian();
            long namePosition = reader.ReadInt64LittleEndian();
            long bonePositions = reader.ReadInt64LittleEndian();
            ushort boneCount = reader.ReadUInt16LittleEndian(); //438
            BonesPerVertex = (byte)reader.ReadByte();
            MaterialId = reader.ReadUShort();
            StartIndex = reader.ReadByte(); // 0 ? 
            vertexStride = reader.ReadByte(); // 68
            PrimitiveType = (PrimitiveType)reader.ReadByte(); // 3
            PrimitiveCount = (uint)reader.ReadUInt32LittleEndian();
            StartIndex = reader.ReadUInt32LittleEndian();
            VertexOffset = reader.ReadUInt32LittleEndian();
            VertexCount = (uint)reader.ReadUInt32LittleEndian(); // 3157
            UnknownInt = reader.ReadUInt();
            FIFA23_UnknownInt1 = reader.ReadUInt(); // hmmm
            FIFA23_UnknownInt2 = reader.ReadUInt(); // hmmm more unknownness
            for (int l = 0; l < 6; l++)
            {
                texCoordRatios.Add(reader.ReadSingleLittleEndian());
            }

			 */
            writer.WriteInt64LittleEndian(offset1);
            meshContainer.WriteRelocPtr("STR", sectionIndex + ":" + Name, writer);
            if (BoneList.Count > 0)
            {
                meshContainer.WriteRelocPtr("BONELIST", BoneList, writer);
            }
            else
            {
                writer.WriteUInt64LittleEndian(0uL);
            }
            writer.WriteUInt16LittleEndian((ushort)BoneList.Count);
            writer.Write((byte)bonesPerVertex);
            writer.WriteUInt16LittleEndian((ushort)MaterialId);
            writer.Write((byte)0);
            writer.Write((byte)vertexStride);
            writer.Write((byte)PrimitiveType);
            writer.WriteUInt32LittleEndian(PrimitiveCount);
            writer.WriteUInt32LittleEndian(StartIndex);
            writer.WriteUInt32LittleEndian(VertexOffset);
            writer.WriteUInt32LittleEndian(VertexCount);
            writer.WriteUInt32LittleEndian(UnknownInt);
            writer.WriteUInt32LittleEndian(FIFA23_UnknownInt1);
            writer.WriteUInt32LittleEndian(FIFA23_UnknownInt2);
            for (int l = 0; l < 6; l++)
            {
                writer.WriteSingleLittleEndian(texCoordRatios[l]);
            }
            for (int i = 0; i < DeclCount; i++)
            {
                for (int j = 0; j < GeometryDeclDesc[i].Elements.Length; j++)
                {
                    writer.Write((byte)GeometryDeclDesc[i].Elements[j].Usage);
                    writer.Write((byte)GeometryDeclDesc[i].Elements[j].Format);
                    writer.Write(GeometryDeclDesc[i].Elements[j].Offset);
                    writer.Write(GeometryDeclDesc[i].Elements[j].StreamIndex);
                }
                for (int k = 0; k < GeometryDeclDesc[i].Streams.Length; k++)
                {
                    writer.Write(GeometryDeclDesc[i].Streams[k].VertexStride);
                    writer.Write((byte)GeometryDeclDesc[i].Streams[k].Classification);
                }
                writer.Write(GeometryDeclDesc[i].ElementCount);
                writer.Write(GeometryDeclDesc[i].StreamCount);
                writer.WriteUInt16LittleEndian(0);
            }
            writer.Write(unknownData);

        }
    }

}
