using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FrostySdk.Resources
{
    public class MeshSetLod
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct IndexBufferFormatStruct
        {
            [FieldOffset(0)]
            public int format;

            [FieldOffset(0)]
            public IndexBufferFormat formatEnum;
        }

        private readonly uint maxInstances;

        private IndexBufferFormatStruct indexBufferFormat;

        private readonly string shaderDebugName;

        private readonly string name;

        private readonly string shortName;

        private readonly uint nameHash;

        private List<AxisAlignedBox> partBoundingBoxes = new List<AxisAlignedBox>();

        private List<LinearTransform> partTransforms = new List<LinearTransform>();

        private byte[] inlineData;

        private uint inlineDataOffset { get; set; }

        private readonly byte[] adjacencyData;

        private readonly bool hasBoneShortNames;

        public MeshType Type { get; set; }

        public List<MeshSetSection> Sections { get; } = new List<MeshSetSection>();


        public EMeshLayout Flags { get; private set; }

        public int IndexUnitSize
        {
            get
            {
                int num = (int)Enum.Parse(TypeLibrary.GetType("RenderFormat"), "RenderFormat_R32_UINT");
                if (indexBufferFormat.format != num)
                {
                    return 16;
                }
                return 32;
            }
        }

        public uint IndexBufferSize { get; set; }

        public uint VertexBufferSize { get; set; }

        public uint AdjacencyBufferSize { get; }

        public Guid ChunkId { get; set; }

        public string String01 => shaderDebugName;

        public string String02 => name;

        public string String03 => shortName;

        public int BoneCount => BoneIndexArray.Count;

        public List<uint> BoneIndexArray { get; } = new List<uint>();


        public List<uint> BoneShortNameArray { get; } = new List<uint>();


        public int PartCount => partBoundingBoxes.Count;

        public List<AxisAlignedBox> PartBoundingBoxes => partBoundingBoxes;

        public List<LinearTransform> PartTransforms => partTransforms;

        public byte[] InlineData => inlineData;

        public List<List<byte>> CategorySubsetIndices { get; } = new List<List<byte>>();


        public int MaxCategories => 5;

        private bool m_HasAdjacencyInMesh;

        public bool HasAdjacencyInMesh
        {
            get
            {
                return m_HasAdjacencyInMesh || ProfileManager.IsLoaded(FMT.FileTools.Modding.EGame.StarWarsSquadrons);
                //{
                //    case (int)ProfileVersion.NeedForSpeedRivals:
                //    case (int)ProfileVersion.DragonAgeInquisition:
                //    case (int)ProfileVersion.Battlefield4:
                //    case (int)ProfileVersion.PlantsVsZombiesGardenWarfare:
                //    case (int)ProfileVersion.MirrorsEdgeCatalyst:
                //    case (int)ProfileVersion.Battlefield1:
                //    case (int)ProfileVersion.StarWarsBattlefrontII:
                //    case (int)ProfileVersion.Battlefield5:
                //    case (int)ProfileVersion.StarWarsSquadrons:
                //        return true;
                //    default:
                //        return false;
                //}
            }
            set
            {
                m_HasAdjacencyInMesh = value;
            }
        }


        public byte[] UnknownChunkPad;

        public long UnknownLongAfterNameHash { get; set; }

        public MeshSetLod(NativeReader reader, MeshSet meshSet)
        {
            var positionAtStart = reader.Position;
            reader.Position = positionAtStart;

            Type = (MeshType)reader.ReadUInt();
            maxInstances = reader.ReadUInt();
            uint sectionCount = reader.ReadUInt();
            var sectionOffset = reader.ReadLong();
            long categoryOffset = reader.Position;
            reader.Position = sectionOffset;
            for (uint sectionIndex = 0u; sectionIndex < sectionCount; sectionIndex++)
            {
                Sections.Add(new MeshSetSection(reader, (int)sectionIndex));
            }
            reader.Position = categoryOffset;
            for (int i = 0; i < MaxCategories; i++)
            {
                int subsetCategoryCount = reader.ReadInt32LittleEndian();
                var subsetCategoryOffset = reader.ReadInt64LittleEndian();

                var currentPosition = reader.Position;
                reader.Position = subsetCategoryOffset;

                CategorySubsetIndices.Add(new List<byte>());
                for (int j = 0; j < subsetCategoryCount; j++)
                {
                    CategorySubsetIndices[i].Add(reader.ReadByte());
                }

                reader.Position = currentPosition;
            }
            Flags = (EMeshLayout)reader.ReadUInt32LittleEndian();
            indexBufferFormat.format = reader.ReadInt32LittleEndian();
            IndexBufferSize = reader.ReadUInt32LittleEndian();
            VertexBufferSize = reader.ReadUInt32LittleEndian();
            if (HasAdjacencyInMesh)
            {
                AdjacencyBufferSize = reader.ReadUInt32LittleEndian();
                adjacencyData = new byte[AdjacencyBufferSize];
            }
            if (ProfileManager.IsLoaded(
                FMT.FileTools.Modding.EGame.FIFA23
                , FMT.FileTools.Modding.EGame.NFSUnbound
                , FMT.FileTools.Modding.EGame.DeadSpace))
            {
                UnknownChunkPad = reader.ReadBytes(8);
                if (ProfileManager.IsLoaded(FMT.FileTools.Modding.EGame.DeadSpace))
                    _ = reader.ReadBytes(4);
            }

            ChunkId = reader.ReadGuid();
            inlineDataOffset = reader.ReadUInt32LittleEndian();
            if (HasAdjacencyInMesh)
            {
                reader.ReadInt64LittleEndian();
            }
            long posShaderDebug = reader.ReadInt64LittleEndian();
            long posFullname = reader.ReadInt64LittleEndian();
            long posShortname = reader.ReadInt64LittleEndian();
            nameHash = reader.ReadUInt32LittleEndian();
            uint boneCount = 0u;
            long boneOffset = 0L;
            long num7 = 0L;
            long num8 = 0L;
            UnknownLongAfterNameHash = reader.ReadInt64LittleEndian();
            if (Type == MeshType.MeshType_Skinned)
            {
                boneCount = reader.ReadUInt32LittleEndian();
                boneOffset = reader.ReadInt64LittleEndian();
            }
            else if (Type == MeshType.MeshType_Composite)
            {
                num8 = reader.ReadInt64LittleEndian();
            }
            reader.Pad(16);
            long position4 = reader.Position;
            if (Type == MeshType.MeshType_Skinned)
            {
                reader.Position = boneOffset;
                for (int k = 0; k < boneCount; k++)
                {
                    BoneIndexArray.Add(reader.ReadUInt32LittleEndian());
                }
                if (num7 != 0L)
                {
                    reader.Position = num7;
                    for (int l = 0; l < boneCount; l++)
                    {
                        BoneShortNameArray.Add(reader.ReadUInt32LittleEndian());
                    }
                }
            }
            else if (Type == MeshType.MeshType_Composite)
            {
                if (boneOffset != 0L)
                {
                    reader.Position = boneOffset;
                    for (int m = 0; m < boneCount; m++)
                    {
                        partBoundingBoxes.Add(reader.ReadAxisAlignedBox());
                    }
                }
                if (num7 != 0L)
                {
                    reader.Position = num7;
                    for (int n = 0; n < boneCount; n++)
                    {
                        partTransforms.Add(reader.ReadLinearTransform());
                    }
                }
                if (num8 != 0L)
                {
                    reader.Position = num8;
                    List<int> list = new List<int>();
                    for (int num9 = 0; num9 < 24; num9++)
                    {
                        int num10 = reader.ReadByte();
                        for (int num2 = 0; num2 < 8; num2++)
                        {
                            if (((uint)num10 & (true ? 1u : 0u)) != 0)
                            {
                                list.Add(num9 * 8 + num2);
                            }
                            num10 >>= 1;
                        }
                    }
                }
            }
            reader.Position = posShaderDebug;
            shaderDebugName = reader.ReadNullTerminatedString();
            reader.Position = posFullname;
            name = reader.ReadNullTerminatedString();
            reader.Position = posShortname;
            shortName = reader.ReadNullTerminatedString();
            reader.Position = position4;
            hasBoneShortNames = BoneShortNameArray.Count > 0;
        }

        internal void PreProcess(MeshContainer meshContainer, ref uint inInlineDataOffset)
        {
            if (meshContainer == null)
            {
                throw new ArgumentNullException("meshContainer");
            }
            this.inlineDataOffset = uint.MaxValue;
            if (inlineData != null)
            {
                this.inlineDataOffset = inlineDataOffset;
                inlineDataOffset += (uint)inlineData.Length;
            }
            meshContainer.AddRelocArray("SECTION", Sections.Count, Sections);
            foreach (MeshSetSection section in Sections)
            {
                section.PreProcess(meshContainer);
            }
            foreach (List<byte> subsetCategory in CategorySubsetIndices)
            {
                meshContainer.AddRelocArray("SUBSET", subsetCategory.Count, subsetCategory);
            }
            if (HasAdjacencyInMesh && this.inlineDataOffset != uint.MaxValue)
            {
                meshContainer.AddRelocPtr("ADJACENCY", adjacencyData);
            }
            meshContainer.AddString(shaderDebugName, "Mesh:", ignoreNull: true);
            meshContainer.AddString(name, name.Replace(shortName, ""), ignoreNull: true);
            meshContainer.AddString(shortName, shortName);
            if (Type == MeshType.MeshType_Skinned)
            {
                meshContainer.AddRelocPtr("BONES", BoneIndexArray);
            }
            //else if (Type == MeshType.MeshType_Composite && partIndices.Count > 0)
            //{
            //	meshContainer.AddRelocPtr("PARTINDICES", partIndices);
            //}
        }

        public void ClearBones()
        {
            BoneIndexArray.Clear();
            BoneShortNameArray.Clear();
        }

        public bool IsSectionInCategory(MeshSetSection section, MeshSubsetCategory category)
        {
            if (section == null)
            {
                throw new ArgumentNullException("section");
            }
            int sectionIndex = GetSectionIndex(section);
            if ((int)category >= CategorySubsetIndices.Count)
            {
                return false;
            }
            return CategorySubsetIndices[(int)category].Contains((byte)sectionIndex);
        }

        public bool IsSectionRenderable(MeshSetSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException("section");
            }
            if (section.PrimitiveCount != 0)
            {
                if (!IsSectionInCategory(section, MeshSubsetCategory.MeshSubsetCategory_Opaque) && !IsSectionInCategory(section, MeshSubsetCategory.MeshSubsetCategory_Transparent))
                {
                    return IsSectionInCategory(section, MeshSubsetCategory.MeshSubsetCategory_TransparentDecal);
                }
                return true;
            }
            return false;
        }

        public void SetSectionCategory(MeshSetSection inSection, MeshSubsetCategory category)
        {
            if (inSection == null)
            {
                throw new ArgumentNullException("inSection");
            }
            byte b = (byte)GetSectionIndex(inSection);
            if (b != byte.MaxValue && !CategorySubsetIndices[(int)category].Contains(b))
            {
                CategorySubsetIndices[(int)category].Add(b);
            }
        }

        public void SetParts(List<LinearTransform> inPartTransforms, List<AxisAlignedBox> inPartBBoxes)
        {
            if (inPartTransforms == null)
            {
                throw new ArgumentNullException("inPartTransforms");
            }
            if (inPartBBoxes == null)
            {
                throw new ArgumentNullException("inPartBBoxes");
            }
            partTransforms = inPartTransforms;
            partBoundingBoxes = inPartBBoxes;
            if (partTransforms.Count == partBoundingBoxes.Count)
            {
                return;
            }
            for (int i = 0; i < partBoundingBoxes.Count; i++)
            {
                if (i >= partTransforms.Count)
                {
                    List<LinearTransform> list = partTransforms;
                    LinearTransform item = default(LinearTransform);
                    Vec3 vec2 = new Vec3
                    {
                        x = 1f,
                        y = 0f,
                        z = 0f
                    };
                    Vec3 vec = (item.right = vec2);
                    vec2 = new Vec3
                    {
                        x = 0f,
                        y = 1f,
                        z = 0f
                    };
                    vec = (item.up = vec2);
                    vec2 = new Vec3
                    {
                        x = 0f,
                        y = 0f,
                        z = 1f
                    };
                    vec = (item.forward = vec2);
                    vec2 = new Vec3
                    {
                        x = 0f,
                        y = 0f,
                        z = 0f
                    };
                    vec = (item.trans = vec2);
                    list.Add(item);
                }
            }
        }

        public void SetIndexBufferFormatSize(int newSize)
        {
            var v2 = Enum.Parse(TypeLibrary.GetType("RenderFormat"), "RenderFormat_R16_UINT");
            var v4 = Enum.Parse(TypeLibrary.GetType("RenderFormat"), "RenderFormat_R32_UINT");
            indexBufferFormat.format = (int)((newSize == 2) ? v2 : v4);
        }

        public void Write(NativeWriter writer, MeshContainer meshContainer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (meshContainer == null)
            {
                throw new ArgumentNullException("meshContainer");
            }
            writer.Write((int)Type);
            writer.Write(maxInstances);
            meshContainer.WriteRelocArray("SECTION", Sections, writer);
            foreach (List<byte> subsetCategory in CategorySubsetIndices)
            {
                meshContainer.WriteRelocArray("SUBSET", subsetCategory, writer);
            }
            writer.Write((int)Flags);
            writer.Write(indexBufferFormat.format);
            writer.Write(IndexBufferSize);
            writer.Write(VertexBufferSize);
            if (HasAdjacencyInMesh)
            {
                writer.Write(0);
            }
            if (ProfileManager.IsFIFA23DataVersion())
            {
                writer.Write(UnknownChunkPad);
            }
            writer.WriteGuid(ChunkId);

            writer.Write(inlineDataOffset);
            if (HasAdjacencyInMesh)
            {
                if (inlineDataOffset != uint.MaxValue)
                {
                    meshContainer.WriteRelocPtr("ADJACENCY", adjacencyData, writer);
                }
                else
                {
                    writer.WriteUInt64LittleEndian(0uL);
                }
            }
            meshContainer.WriteRelocPtr("STR", shaderDebugName, writer);
            meshContainer.WriteRelocPtr("STR", name, writer);
            meshContainer.WriteRelocPtr("STR", shortName, writer);
            writer.Write(nameHash);
            writer.WriteInt64LittleEndian(UnknownLongAfterNameHash);
            if (Type == MeshType.MeshType_Skinned)
            {
                writer.Write(BoneIndexArray.Count);
                meshContainer.WriteRelocPtr("BONES", BoneIndexArray, writer);
            }
            else if (Type == MeshType.MeshType_Composite)
            {
            }
            writer.WritePadding(16);
        }

        public MeshSubsetCategoryFlags GetSectionCategories(int index)
        {
            MeshSubsetCategoryFlags meshSubsetCategoryFlags = 0;
            for (int i = 0; i < CategorySubsetIndices.Count; i++)
            {
                if (CategorySubsetIndices[i].Contains((byte)index))
                {
                    meshSubsetCategoryFlags = (MeshSubsetCategoryFlags)((int)meshSubsetCategoryFlags | (1 << i));
                }
            }
            return meshSubsetCategoryFlags;
        }

        public void ClearCategories()
        {
            for (int i = 0; i < CategorySubsetIndices.Count; i++)
            {
                CategorySubsetIndices[i].Clear();
            }
        }

        public void ReadInlineData(NativeReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (ChunkId == Guid.Empty)
            {
                inlineData = reader.ReadBytes((int)(VertexBufferSize + IndexBufferSize));
                reader.Pad(16);
            }
        }

        public void SetInlineData(byte[] inBuffer)
        {
            inlineData = inBuffer;
            ChunkId = Guid.Empty;
            Flags = EMeshLayout.Inline;
        }

        private int GetSectionIndex(MeshSetSection inSection)
        {
            return Sections.IndexOf(inSection);
        }

        public void AddBones(IEnumerable<ushort> bones, IEnumerable<string> boneNames)
        {
            if (bones == null)
            {
                throw new ArgumentNullException("bones");
            }
            if (hasBoneShortNames && boneNames == null)
            {
                throw new ArgumentNullException("boneNames");
            }
            foreach (ushort bone in bones)
            {
                if (!BoneIndexArray.Contains(bone))
                {
                    BoneIndexArray.Add(bone);
                }
            }
            if (!hasBoneShortNames)
            {
                return;
            }
            foreach (string boneName in boneNames)
            {
                uint item = (uint)Fnv1.HashString(boneName.ToLower());
                if (!BoneShortNameArray.Contains(item))
                {
                    BoneShortNameArray.Add(item);
                }
            }
        }
    }

}