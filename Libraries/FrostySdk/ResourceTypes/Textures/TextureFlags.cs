using System;

namespace FrostySdk.Resources
{
    [Flags]
    public enum TextureFlags : ushort
    {
        Default = 0x0,
        Streaming = 0x1,
        SrgbGamma = 0x2,
        CpuResource = 0x4,
        OnDemandLoaded = 0x8,
        Mutable = 0x10,
        NoSkipmip = 0x20,
        XenonPackedMipmaps = 0x100,
        Ps3MemoryCell = 0x100,
        Ps3MemoryRsx = 0x200, // 512
        StreamingAlways = 0x400,
        Swizzle = 2048, // 2048
        Ps4Swizzle = 2049 // 2049
    }
}
