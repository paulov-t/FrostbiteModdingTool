// Sdk.IO.EbxWriterRiff
using FrostySdk.IO;
using System.IO;


namespace FrostySdk.FrostySdk.IO
{
    public class EbxWriter2023 : EbxWriter2022
    {
        public EbxWriter2023(Stream inStream, EbxWriteFlags inFlags = EbxWriteFlags.None, bool leaveOpen = false)
            : base(inStream, inFlags, true)
        {
        }

    }
}