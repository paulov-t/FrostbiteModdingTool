using FMT.FileTools;
using FMT.FileTools.Modding;
using FrostySdk;
using FrostySdk.FrostySdk.Resources;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static FrostySdk.ProfileManager;

public class MeshSet
{
    private TangentSpaceCompressionType tangentSpaceCompressionType;

    private AxisAlignedBox boundingBox;

    private string fullName;

    private uint nameHash { get; set; }

    private uint headerSize { get; set; }

    private readonly List<uint> unknownUInts = new List<uint>();

    private ushort? unknownUShort { get; set; }

    private readonly List<ushort> unknownUShorts = new List<ushort>();

    private readonly List<long> unknownOffsets = new List<long>();

    private readonly List<byte[]> unknownBytes = new List<byte[]>();

    private ushort boneCount { get; set; }

    private ushort CullBoxCount { get; set; }

    private readonly List<ushort> boneIndices = new List<ushort>();

    private readonly List<AxisAlignedBox> boneBoundingBoxes = new List<AxisAlignedBox>();

    private readonly List<AxisAlignedBox> partBoundingBoxes = new List<AxisAlignedBox>();

    private readonly List<LinearTransform> partTransforms = new List<LinearTransform>();

    public TangentSpaceCompressionType TangentSpaceCompressionType
    {
        get
        {
            return tangentSpaceCompressionType;
        }
        set
        {
            tangentSpaceCompressionType = value;
            foreach (MeshSetLod lod in Lods)
            {
                foreach (MeshSetSection section in lod.Sections)
                {
                    section.TangentSpaceCompressionType = value;
                }
            }
        }
    }

    public AxisAlignedBox BoundingBox => boundingBox;

    public List<MeshSetLod> Lods { get; } = new List<MeshSetLod>();


    public MeshType Type { get; set; }

    public EMeshLayout MeshLayout { get; private set; }
    public MeshSetLayoutFlags MeshSetLayoutFlags { get; private set; }

    public string FullName
    {
        get
        {
            return fullName;
        }
        set
        {
            fullName = value.ToLower();
            if (nameHash == 0)
                nameHash = (uint)Fnv1.HashString(fullName);
            int num = fullName.LastIndexOf('/');
            Name = ((num != -1) ? fullName.Substring(num + 1) : string.Empty);
        }
    }

    public string Name { get; private set; }

    public int HeaderSize => BitConverter.ToUInt16(Meta, 12);

    public int MaxLodCount => (int)MeshLimits.MaxMeshLodCount;

    public byte[] Meta { get; } = new byte[16];

    public ushort MeshCount { get; set; }

    public MeshType FIFA23_Type2 { get; private set; }
    public byte[] FIFA23_TypeUnknownBytes { get; private set; }
    public byte[] FIFA23_SkinnedUnknownBytes { get; private set; }

    public long SkinnedBoneCountUnkLong1 { get; private set; }
    public long SkinnedBoneCountUnkLong2 { get; private set; }

    public ShaderDrawOrder ShaderDrawOrder { get; private set; }

    public ShaderDrawOrderUserSlot ShaderDrawOrderUserSlot { get; private set; }

    public ShaderDrawOrderSubOrder ShaderDrawOrderSubOrder { get; private set; }

    public long UnknownPostLODCount { get; private set; }

    public List<ushort> LodFade { get; } = new List<ushort>();

    public MeshSet(Stream stream)
    {
        if (stream == null)
            return;

        NativeWriter nativeWriterTest = new NativeWriter(new FileStream("_MeshSet.dat", FileMode.Create));
        nativeWriterTest.Write(((MemoryStream)stream).ToArray());
        nativeWriterTest.Close();
        nativeWriterTest.Dispose();

        FileReader nativeReader = new FileReader(stream);
        // useful for resetting when live debugging
        nativeReader.Position = 0;

        if (ProfileManager.Game == EGame.FIFA23)
            ReadFIFA23(nativeReader);
        else if (ProfileManager.Game == EGame.NFSUnbound)
            ReadNFSUnbound(nativeReader);
        else
            ReadFIFA23(nativeReader);

    }

    public List<ushort> PositionsOfLodMeshSet { get; } = new List<ushort>();

    private void ReadNFSUnbound(FileReader nativeReader)
    {
        _ = nativeReader.Position;

        boundingBox = nativeReader.ReadAxisAlignedBox();
        long[] lodOffsets = new long[MaxLodCount];
        for (int i2 = 0; i2 < MaxLodCount; i2++)
        {
            lodOffsets[i2] = nativeReader.ReadLong();
        }
        UnknownPostLODCount = nativeReader.ReadLong();
        var nameLong = nativeReader.ReadNullTerminatedString(offset: nativeReader.ReadLong());
        var nameShort = nativeReader.ReadNullTerminatedString(offset: nativeReader.ReadLong());

        nameHash = nativeReader.ReadUInt();
        Type = (MeshType)nativeReader.ReadByte();
        unknownBytes.Add(nativeReader.ReadBytes(15));
        for (int n = 0; n < MaxLodCount * 2; n++)
        {
            LodFade.Add(nativeReader.ReadUInt16LittleEndian());
        }
        var preMeshSetFlags = nativeReader.Position;
        nativeReader.Position = preMeshSetFlags;
        MeshSetLayoutFlags = (MeshSetLayoutFlags)nativeReader.ReadUInt();
        var postMeshSetFlags = nativeReader.Position;
        nativeReader.Position = postMeshSetFlags;
        ShaderDrawOrder = (ShaderDrawOrder)nativeReader.ReadByte();
        ShaderDrawOrderUserSlot = (ShaderDrawOrderUserSlot)nativeReader.ReadByte();
        ShaderDrawOrderSubOrder = (ShaderDrawOrderSubOrder)nativeReader.ReadUShort();
        var lodsCount = nativeReader.ReadUShort();
        MeshCount = nativeReader.ReadUShort();

        for(var iL = 0; iL < lodsCount; iL++)
        {
            PositionsOfLodMeshSet.Add(nativeReader.ReadUShort());
        }
        nativeReader.ReadBytes(10);

        for (int iL = 0; iL < lodsCount; iL++)
        {
            nativeReader.Position = lodOffsets[iL];
            MeshSetLod lod = new MeshSetLod(nativeReader, this);
            lod.SetParts(partTransforms, partBoundingBoxes);
            Lods.Add(lod);
        }
    }

    public void ReadFIFA23(FileReader nativeReader)
    {
        boundingBox = nativeReader.ReadAxisAlignedBox();
        long[] array = new long[MaxLodCount];
        for (int i2 = 0; i2 < MaxLodCount; i2++)
        {
            array[i2] = nativeReader.ReadLong();
        }
        UnknownPostLODCount = nativeReader.ReadLong();
        long offsetNameLong = nativeReader.ReadLong();
        long offsetNameShort = nativeReader.ReadLong();
        nameHash = nativeReader.ReadUInt();
        Type = (MeshType)nativeReader.ReadUInt();
        if (ProfileManager.IsFIFA23DataVersion() || ProfileManager.Game == EGame.NFSUnbound)
        {
            nativeReader.Position -= 4;
            Type = (MeshType)nativeReader.ReadByte();
            // another type?
            FIFA23_Type2 = (MeshType)nativeReader.ReadByte();
            // lots of zeros?
            //FIFA23_TypeUnknownBytes = nativeReader.ReadBytes(128 - (int)nativeReader.BaseStream.Position);
            FIFA23_TypeUnknownBytes = nativeReader.ReadBytes(18);

            // we should be at 128 anyway?
            //nativeReader.Position = 128;
        }
        for (int n = 0; n < MaxLodCount * 2; n++)
        {
            LodFade.Add(nativeReader.ReadUInt16LittleEndian());
        }
        MeshLayout = (EMeshLayout)nativeReader.ReadByte();
        nativeReader.Position -= 1;
        var meshLayoutFlags = (MeshSetLayoutFlags)nativeReader.ReadByte();
        unknownUInts.Add(nativeReader.ReadUInt());
        unknownUInts.Add(nativeReader.ReadUInt());
        nativeReader.Position -= 1;
        ShaderDrawOrder = (ShaderDrawOrder)nativeReader.ReadByte();
        ShaderDrawOrderUserSlot = (ShaderDrawOrderUserSlot)nativeReader.ReadByte();
        ShaderDrawOrderSubOrder = (ShaderDrawOrderSubOrder)nativeReader.ReadUShort();
        //ShaderDrawOrder = (ShaderDrawOrder)nativeReader.ReadByte();
        //ShaderDrawOrderUserSlot = nativeReader.ReadByte();
        //ShaderDrawOrderSubOrder = nativeReader.ReadInt16LittleEndian();
        //      Flags = (MeshLayoutFlags)nativeReader.ReadUInt();
        //ReadUnknownUInts(nativeReader);
        //nativeReader.ReadBytes(16);

        ushort lodsCount = 0;
        if (
            ProfileManager.IsMadden21DataVersion(ProfileManager.Game)
            || ProfileManager.IsMadden22DataVersion(ProfileManager.Game)
            )
        {
            nativeReader.ReadUShort();
            lodsCount = nativeReader.ReadUShort();
            nativeReader.ReadUInt();
            nativeReader.ReadUShort();
        }
        else
        {
            lodsCount = nativeReader.ReadUShort();
            MeshCount = nativeReader.ReadUShort();
        }
        boneCount = 0;
        if (ProfileManager.IsMadden21DataVersion(ProfileManager.Game) || ProfileManager.IsMadden22DataVersion(ProfileManager.Game))
        {
            unknownBytes.Add(nativeReader.ReadBytes(8));
        }

        // useful for resetting when live debugging
        var positionBeforeMeshTypeRead = nativeReader.Position;
        nativeReader.Position = positionBeforeMeshTypeRead;

        if (Type == MeshType.MeshType_Skinned)
        {
            if (ProfileManager.IsFIFA23DataVersion())
            {
                // 12 bytes of unknowness
                FIFA23_SkinnedUnknownBytes = nativeReader.ReadBytes(12);
            }
            boneCount = nativeReader.ReadUInt16LittleEndian();
            CullBoxCount = nativeReader.ReadUInt16LittleEndian();
            if (CullBoxCount != 0)
            {
                long cullBoxBoneIndicesOffset = nativeReader.ReadInt64LittleEndian();
                long cullBoxBoundingBoxOffset = nativeReader.ReadInt64LittleEndian();
                long position = nativeReader.Position;
                if (cullBoxBoneIndicesOffset != 0L)
                {
                    nativeReader.Position = cullBoxBoneIndicesOffset;
                    for (int m = 0; m < CullBoxCount; m++)
                    {
                        boneIndices.Add(nativeReader.ReadUInt16LittleEndian());
                    }
                }
                if (cullBoxBoundingBoxOffset != 0L)
                {
                    nativeReader.Position = cullBoxBoundingBoxOffset;
                    for (int l = 0; l < CullBoxCount; l++)
                    {
                        boneBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
                    }
                }
                nativeReader.Position = position;
            }
        }
        else if (Type == MeshType.MeshType_Composite)
        {
            boneCount = nativeReader.ReadUShort();
            boneCount = nativeReader.ReadUShort();
            long num3 = nativeReader.ReadLong();
            long num4 = nativeReader.ReadLong();
            long position3 = nativeReader.Position;
            if (num3 != 0L)
            {
                nativeReader.Position = num3;
                for (int n2 = 0; n2 < boneCount; n2++)
                {
                    partTransforms.Add(nativeReader.ReadLinearTransform());
                }
            }
            if (num4 != 0L)
            {
                nativeReader.Position = num4;
                for (int num5 = 0; num5 < boneCount; num5++)
                {
                    partBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
                }
            }
            nativeReader.Position = position3;
        }
        nativeReader.Pad(16);
        headerSize = (uint)nativeReader.Position;
        for (int n = 0; n < lodsCount; n++)
        {
            Lods.Add(new MeshSetLod(nativeReader, this));
        }
        int sectionIndex = 0;
        foreach (MeshSetLod lod4 in Lods)
        {
            for (int m = 0; m < lod4.Sections.Count; m++)
            {
                lod4.Sections[m] = new MeshSetSection(nativeReader, sectionIndex++);
            }
        }
        nativeReader.Pad(16);
        nativeReader.Position = offsetNameLong;
        FullName = nativeReader.ReadNullTerminatedString();
        nativeReader.Position = offsetNameShort;
        Name = nativeReader.ReadNullTerminatedString();
        nativeReader.Pad(16);
        foreach (MeshSetLod lod3 in Lods)
        {
            for (int l = 0; l < lod3.CategorySubsetIndices.Count; l++)
            {
                for (int j2 = 0; j2 < lod3.CategorySubsetIndices[l].Count; j2++)
                {
                    lod3.CategorySubsetIndices[l][j2] = nativeReader.ReadByte();
                }
            }
        }
        nativeReader.Pad(16);
        foreach (MeshSetLod lod2 in Lods)
        {
            nativeReader.Position += lod2.AdjacencyBufferSize;
        }
        nativeReader.Pad(16);
        foreach (MeshSetLod lod in Lods)
        {
            if (lod.Type == MeshType.MeshType_Skinned)
            {
                nativeReader.Position += lod.BoneCount * 4;
            }
            else if (lod.Type == MeshType.MeshType_Composite)
            {
                nativeReader.Position += lod.Sections.Count * 24;
            }
        }
        //if (Type == MeshType.MeshType_Skinned)
        //{
        //    nativeReader.Pad(16);
        //    for (int k = 0; k < boneCount; k++)
        //    {
        //        boneIndices.Add(nativeReader.ReadUShort());
        //    }
        //    nativeReader.Pad(16);
        //    for (int j = 0; j < boneCount; j++)
        //    {
        //        boneBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
        //    }
        //}
        //else if (Type == MeshType.MeshType_Composite)
        //{
        //    nativeReader.Pad(16);
        //    for (int i = 0; i < boneCount; i++)
        //    {
        //        partBoundingBoxes.Add(nativeReader.ReadAxisAlignedBox());
        //    }
        //}
        nativeReader.Pad(16);
        foreach (MeshSetLod lod5 in Lods)
        {
            lod5.ReadInlineData(nativeReader);
        }
    }

    private void ReadUnknownUInts(NativeReader nativeReader)
    {
        switch (ProfileManager.DataVersion)
        {
            case 20160927:
                unknownUInts.Add(nativeReader.ReadUInt());
                unknownUInts.Add(nativeReader.ReadUInt());
                break;
            case 20171210:
                unknownUInts.Add(nativeReader.ReadUShort());
                break;
            case (int)EGame.MADDEN22:
            case (int)EGame.MADDEN21:
            case (int)EGame.FIFA23:
            case (int)EGame.FIFA22:
            case (int)EGame.FIFA21:
            case (int)EGame.FIFA20:
            case 20170929:
            case 20180807:
            case 20180914:
                {
                    for (int m = 0; m < 4; m++)
                    {
                        unknownUInts.Add(nativeReader.ReadUInt());
                    }
                    break;
                }
            case 20180628:
                {
                    for (int k = 0; k < 6; k++)
                    {
                        unknownUInts.Add(nativeReader.ReadUInt());
                    }
                    break;
                }
            case 20181207:
            case 20190905:
            case 20191101:
                {
                    for (int j = 0; j < 7; j++)
                    {
                        unknownUInts.Add(nativeReader.ReadUInt());
                    }
                    break;
                }

            case 20190729:
                {
                    for (int l = 0; l < 8; l++)
                    {
                        unknownUInts.Add(nativeReader.ReadUInt());
                    }
                    break;
                }
            default:
                unknownUInts.Add(nativeReader.ReadUInt());
                if (ProfileManager.DataVersion != 20170321)
                {
                    unknownUInts.Add(nativeReader.ReadUInt());
                    unknownUInts.Add(nativeReader.ReadUInt());
                    unknownUInts.Add(nativeReader.ReadUInt());
                    unknownUInts.Add(nativeReader.ReadUInt());
                    unknownUInts.Add(nativeReader.ReadUInt());
                    if (ProfileManager.DataVersion == 20171117 || ProfileManager.DataVersion == 20171110)
                    {
                        unknownUInts.Add(nativeReader.ReadUInt());
                    }
                }
                break;
            case 20131115:
            case 20140225:
            case 20141117:
            case 20141118:
            case 20150223:
            case 20151103:
                break;
        }
    }

    private void PreProcess(MeshContainer meshContainer)
    {
        if (meshContainer == null)
        {
            throw new ArgumentNullException("meshContainer");
        }
        uint inInlineDataOffset = 0u;
        foreach (MeshSetLod lod3 in Lods)
        {
            lod3.PreProcess(meshContainer, ref inInlineDataOffset);
        }
        foreach (MeshSetLod lod2 in Lods)
        {
            meshContainer.AddRelocPtr("LOD", lod2);
        }
        meshContainer.AddString(fullName, fullName.Replace(Name, ""), ignoreNull: true);
        meshContainer.AddString(Name, Name);
        if (Type == MeshType.MeshType_Skinned)
        {
            meshContainer.AddRelocPtr("BONEINDICES", boneIndices);
            meshContainer.AddRelocPtr("BONEBBOXES", boneBoundingBoxes);
        }
        else if (Type == MeshType.MeshType_Composite)
        {
            meshContainer.AddRelocPtr("PARTTRANSFORMS", partTransforms);
            meshContainer.AddRelocPtr("PARTBOUNDINGBOXES", partBoundingBoxes);
        }
    }

    public byte[] ToBytes()
    {
        MeshContainer meshContainer = new MeshContainer();
        PreProcess(meshContainer);
        using FileWriter nativeWriter = new FileWriter(new MemoryStream());
        Write(nativeWriter, meshContainer);
        uint resSize = (uint)nativeWriter.BaseStream.Position;
        uint num2 = 0u;
        uint num3 = 0u;
        foreach (MeshSetLod lod in Lods)
        {
            if (lod.ChunkId == Guid.Empty)
            {
                nativeWriter.WriteBytes(lod.InlineData);
                nativeWriter.WritePadding(16);
            }
        }
        num2 = (uint)(nativeWriter.BaseStream.Position - resSize);
        meshContainer.WriteRelocPtrs(nativeWriter);
        meshContainer.WriteRelocTable(nativeWriter);
        num3 = (uint)(nativeWriter.BaseStream.Position - resSize - num2);
        BitConverter.TryWriteBytes(Meta, resSize);
        BitConverter.TryWriteBytes(Meta.AsSpan(4), num2);
        BitConverter.TryWriteBytes(Meta.AsSpan(8), num3);
        BitConverter.TryWriteBytes(Meta.AsSpan(12), headerSize);
        var array = ((MemoryStream)nativeWriter.BaseStream).ToArray();
#if DEBUG
        if (File.Exists("_MeshSetNEW.dat")) File.Delete("_MeshSetNEW.dat");
        File.WriteAllBytes("_MeshSetNEW.dat", array);
#endif
        return array;
    }

    private void Write(NativeWriter writer, MeshContainer meshContainer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }
        if (meshContainer == null)
        {
            throw new ArgumentNullException("meshContainer");
        }
        writer.WriteAxisAlignedBox(boundingBox);
        for (int i = 0; i < MaxLodCount; i++)
        {
            if (i < Lods.Count)
            {
                meshContainer.WriteRelocPtr("LOD", Lods[i], writer);
            }
            else
            {
                writer.WriteUInt64LittleEndian(0uL);
            }
        }
        //UnknownPostLODCount = nativeReader.ReadLong();
        writer.Write(UnknownPostLODCount);

        meshContainer.WriteRelocPtr("STR", fullName, writer);
        meshContainer.WriteRelocPtr("STR", Name, writer);
        writer.Write(nameHash);

        if (ProfileManager.IsFIFA23DataVersion())
        {
            writer.Write((byte)Type);
            writer.Write((byte)FIFA23_Type2);
            writer.Write(FIFA23_TypeUnknownBytes);
        }
        else
        {
            writer.Write((uint)Type);
        }
        for (int n = 0; n < MaxLodCount * 2; n++)
        {
            //LodFade.Add(nativeReader.ReadUInt16LittleEndian());
            writer.Write((ushort)LodFade[n]);
        }
        //MeshLayout = (EMeshLayout)nativeReader.ReadByte();
        writer.Write((byte)MeshLayout);
        //unknownUInts.Add(nativeReader.ReadUInt());
        writer.Write((uint)unknownUInts[0]);
        writer.Write((uint)unknownUInts[1]);
        writer.Position -= 1;
        //ShaderDrawOrder = (ShaderDrawOrder)nativeReader.ReadByte();
        writer.Write((byte)ShaderDrawOrder);
        //ShaderDrawOrderUserSlot = (ShaderDrawOrderUserSlot)nativeReader.ReadByte();
        writer.Write((byte)ShaderDrawOrderUserSlot);
        //ShaderDrawOrderSubOrder = (ShaderDrawOrderSubOrder)nativeReader.ReadUShort();
        writer.Write((ushort)ShaderDrawOrderSubOrder);

        //writer.Write((uint)MeshLayout);

        //foreach (uint unknownUInt in unknownUInts)
        //{
        //	writer.Write((uint)unknownUInt);
        //}
        var sumOfLOD = (ushort)(Lods.Sum(x => x.Sections.Count));
        if (ProfileManager.IsMadden21DataVersion(ProfileManager.Game) || ProfileManager.IsMadden22DataVersion(ProfileManager.Game))
        {
            writer.WriteUInt16LittleEndian(0);
            writer.Write((ushort)Lods.Count);
            writer.WriteUInt32LittleEndian(sumOfLOD);
            writer.Write((ushort)Lods.Count);
            writer.Write(unknownBytes[0]);
        }
        else
        {
            writer.WriteUInt16LittleEndian((ushort)Lods.Count);
            //writer.WriteUInt16LittleEndian(sumOfLOD);
            writer.WriteUInt16LittleEndian(MeshCount);
        }

        //// Madden 21 Ushort
        //if (unknownUShort.HasValue)
        //          writer.Write((ushort)unknownUShort);

        //      writer.Write((ushort)Lods.Count);
        //ushort num = 0;
        //foreach (MeshSetLod lod in Lods)
        //{
        //	num = (ushort)(num + (ushort)lod.Sections.Count);
        //}
        //writer.Write(num);
        if (Type == MeshType.MeshType_Skinned)
        {
            if (ProfileManager.IsFIFA23DataVersion())
            {
                writer.Write(FIFA23_SkinnedUnknownBytes);
            }

            writer.WriteUInt16LittleEndian((ushort)boneCount);
            if (ProfileManager.IsMadden21DataVersion(ProfileManager.Game) || ProfileManager.IsMadden22DataVersion(ProfileManager.Game))
            {
                writer.WriteUInt32LittleEndian((uint)CullBoxCount);
            }
            else
            {
                writer.WriteUInt16LittleEndian((ushort)CullBoxCount);
            }
            //writer.WriteUInt16LittleEndian(boneCount);
            //writer.WriteUInt16LittleEndian((ushort)boneIndices.Count);
            if (CullBoxCount > 0)
            {
                meshContainer.WriteRelocPtr("BONEINDICES", boneIndices, writer);
                meshContainer.WriteRelocPtr("BONEBBOXES", boneBoundingBoxes, writer);
            }
        }
        else if (Type == MeshType.MeshType_Composite)
        {
            writer.WriteUInt16LittleEndian((ushort)boneIndices.Count);
            writer.WriteUInt16LittleEndian(0);
            meshContainer.WriteRelocPtr("BONEINDICES", boneIndices, writer);
            meshContainer.WriteRelocPtr("BONEBBOXES", boneBoundingBoxes, writer);
        }
        writer.WritePadding(16);
        foreach (MeshSetLod lod2 in Lods)
        {
            meshContainer.AddOffset("LOD", lod2, writer);
            lod2.Write(writer, meshContainer);
        }
        foreach (MeshSetLod lod3 in Lods)
        {
            meshContainer.AddOffset("SECTION", lod3.Sections, writer);
            foreach (MeshSetSection section3 in lod3.Sections)
            {
                section3.Process(writer, meshContainer);
            }
        }
        writer.WritePadding(16);
        foreach (MeshSetLod lod5 in Lods)
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
        foreach (MeshSetLod lod6 in Lods)
        {
            foreach (List<byte> categorySubsetIndex in lod6.CategorySubsetIndices)
            {
                meshContainer.AddOffset("SUBSET", categorySubsetIndex, writer);
                writer.WriteBytes(categorySubsetIndex.ToArray());
            }
        }
        writer.WritePadding(16);
        if (Type != MeshType.MeshType_Skinned)
        {
            return;
        }
        foreach (MeshSetLod lod4 in Lods)
        {
            meshContainer.AddOffset("BONES", lod4.BoneIndexArray, writer);
            foreach (uint item in lod4.BoneIndexArray)
            {
                writer.Write((uint)item);
            }
        }
        writer.WritePadding(16);
        meshContainer.AddOffset("BONEINDICES", boneIndices, writer);
        //foreach (ushort boneIndex in CullBoxCount)
        for (var iCB = 0; iCB < CullBoxCount; iCB++)
        {
            //writer.WriteUInt16LittleEndian(boneIndex);
            writer.WriteUInt16LittleEndian(boneIndices[iCB]);
        }
        writer.WritePadding(16);
        meshContainer.AddOffset("BONEBBOXES", boneBoundingBoxes, writer);
        for (var iCB = 0; iCB < CullBoxCount; iCB++)
        //foreach (AxisAlignedBox boneBoundingBox in boneBoundingBoxes)
        {
            writer.WriteAxisAlignedBox(boneBoundingBoxes[iCB]);
        }
        writer.WritePadding(16);
    }
}
