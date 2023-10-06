using System;

namespace FrostySdk.Resources
{
    [Flags]
    public enum TextureFlags : ushort
    {
        Streaming = 0x1,
        SrgbGamma = 0x2,
        CpuResource = 0x4,
        OnDemandLoaded = 0x8,
        Mutable = 0x10,
        NoSkipmip = 0x20,
        XenonPackedMipmaps = 0x100,
        Ps3MemoryCell = 0x100,
        Ps3MemoryRsx = 0x200,
        Ps4 = 2049
    }
}
