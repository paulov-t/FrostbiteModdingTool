using System;

namespace FrostySdk.Resources
{
    [Flags]
    public enum EMeshLayout : ulong
    {
        IsBaseLod = 1u,
        StreamInstancingEnable = 0x10u,
        StreamingEnable = 0x40u,
        VertexAnimationEnable = 0x80u,
        Deformation = 0x100u,
        MultiStreamEnable = 0x200u,
        SubsetSortingEnable = 0x400u,
        Inline = 0x800u,
        AlternateBatchSorting = 0x1000u,
        ProjectedDecalsEnable = 0x8000u,
        SrvEnable = 0x20000u,
        IsMeshFront = 0x10000000u,
        IsDataAvailable = 0x20000000u
    }

    [Flags]
    public enum MeshLayoutFlags : uint
    {
        //IsBaseLod = 1 << 0,
        //StreamInstancingEnable = 1 << 4,
        //StreamingEnable = 1 << 6,
        //VertexAnimationEnable = 1 << 7,
        //Deformation = 1 << 8,
        //MultiStreamEnable = 1 << 9,
        //SubsetSortingEnable = 1 << 10,
        //Inline = 1 << 11,
        //AlternateBatchSorting = 1 << 12,
        //ProjectedDecalsEnable = 1 << 15,
        //ClothEnabled = 1 << 16,
        //SrvEnable = 1 << 17,
        //IsMeshFront = 1 << 28,
        //IsDataAvailable = 1 << 29

        IsBaseLod = 1u,
        StreamInstancingEnable = 0x10u,
        StreamingEnable = 0x40u,
        VertexAnimationEnable = 0x80u,
        Deformation = 0x100u,
        MultiStreamEnable = 0x200u,
        SubsetSortingEnable = 0x400u,
        Inline = 0x800u,
        AlternateBatchSorting = 0x1000u,
        ProjectedDecalsEnable = 0x8000u,
        ClothEnabled = 1 << 16,
        SrvEnable = 0x20000u,
        IsMeshFront = 0x10000000u,
        IsDataAvailable = 0x20000000u
    }

    [Flags]
    public enum MeshSetLayoutFlags : ulong
    {
        StreamingEnable = 1uL,
        HalfResRenderEnable = 2uL,
        StreamInstancingEnable = 4uL,
        MovableParts = 8uL,
        DrawProcessEnable = 0x10uL,
        StreamingEnableAlways = 0x20uL,
        DeformationEnable = 0x40uL,
        UseLastLodForShadow = 0x100uL,
        CastShadowLowEnable = 0x200uL,
        CastShadowMediumEnable = 0x400uL,
        CastShadowHighEnable = 0x800uL,
        CastShadowUltraEnable = 0x1000uL,
        CastDynamicReflectionLowEnable = 0x2000uL,
        CastDynamicReflectionMediumEnable = 0x4000uL,
        CastDynamicReflectionHighEnable = 0x8000uL,
        CastDynamicReflectionUltraEnable = 0x10000uL,
        CastPlanarReflectionLowEnable = 0x20000uL,
        CastPlanarReflectionMediumEnable = 0x40000uL,
        CastPlanarReflectionHighEnable = 0x80000uL,
        CastPlanarReflectionUltraEnable = 0x100000uL,
        CastStaticReflectionLowEnable = 0x200000uL,
        CastStaticReflectionMediumEnable = 0x400000uL,
        CastStaticReflectionHighEnable = 0x800000uL,
        CastStaticReflectionUltraEnable = 0x1000000uL,
        SubsetSortingEnable = 0x4000000uL,
        LodFadeEnable = 0x8000000uL,
        ProjectedDecalsEnable = 0x10000000uL,
        ClothEnable = 0x20000000uL,
        ZPassEnable = 0x40000000uL,
        CastDistantShadowCache = 0x80000000uL,
        CastPlanarShadowLowEnable = 0x100000000uL,
        CastPlanarShadowMediumEnable = 0x200000000uL,
        CastPlanarShadowHighEnable = 0x400000000uL,
        CastPlanarShadowUltraEnable = 0x800000000uL,
        CastShadowInBakedLowEnable = 0x1000000000uL,
        CastShadowInBakedMediumEnable = 0x2000000000uL,
        CastShadowInBakedHighEnable = 0x4000000000uL,
        CastShadowInBakedUltraEnable = 0x8000000000uL,
        ForwardDepthPassEnable = 0x10000000000uL
    }
}
