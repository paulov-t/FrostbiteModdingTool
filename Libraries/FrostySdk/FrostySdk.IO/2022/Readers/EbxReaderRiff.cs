using FrostySdk.IO._2022.Readers;
using System.IO;

namespace FrostySdk.FrostySdk.IO.Readers
{
    public class EbxReaderRiff : EbxReader22B
    {

        public EbxReaderRiff(Stream ebxDataStream, bool inPatched, bool onlyType = false)
            : base(ebxDataStream, inPatched, onlyType)
        {
        }
    }
}

