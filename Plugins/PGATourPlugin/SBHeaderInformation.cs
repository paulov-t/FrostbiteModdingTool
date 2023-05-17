using FMT.FileTools;
using System.IO;

namespace FIFA23Plugin
{
    public class SBHeaderInformation
    {
        public int size;
        public uint magicStuff;
        public int totalCount;
        public int ebxCount;
        public int resCount;
        public int chunkCount;
        public int stringOffset;
        public int metaOffset;
        public int metaSize;


        public bool HasStrings
        {
            get
            {
                return stringOffset > AdditionalHeaderLength;
            }
        }

        public bool HasMeta
        {
            get
            {
                return metaSize > AdditionalHeaderLength && metaOffset > AdditionalHeaderLength;
            }
        }

        public bool SuccessfullyRead = true;

        int AdditionalHeaderLength = 32;

        //public int shaCount;

        int attempts = 0;

        public SBHeaderInformation(NativeReader nr, int additionalHeaderLength = 36)
        {
            //startOfSBHI:

            AdditionalHeaderLength = additionalHeaderLength;
            var pos = nr.Position;

            size = nr.ReadInt(Endian.Big) + AdditionalHeaderLength;
            magicStuff = nr.ReadUInt(Endian.Big);
            if (magicStuff != 3599661469)
            {
                //if (attempts == 0)
                //{
                //    nr.Position = 0;
                //    attempts++;
                //    goto startOfSBHI;
                //}

                //Debug.WriteLine("Magic/Hash is not right, expecting 3599661469");
                SuccessfullyRead = false;
                return;
            }

            totalCount = nr.ReadInt(Endian.Little);
            ebxCount = nr.ReadInt(Endian.Little);
            resCount = nr.ReadInt(Endian.Little);
            chunkCount = nr.ReadInt(Endian.Little);
            stringOffset = nr.ReadInt(Endian.Little) + AdditionalHeaderLength + 4;
            metaOffset = nr.ReadInt(Endian.Little) + AdditionalHeaderLength + 4;
            metaSize = nr.ReadInt(Endian.Little) + AdditionalHeaderLength + 4;
        }

        public byte[] Write()
        {
            MemoryStream memoryStream = new MemoryStream();
            NativeWriter nw = new NativeWriter(memoryStream);
            nw.Write(size, Endian.Big);
            nw.Write(3599661469, Endian.Big);
            nw.Write(totalCount, Endian.Little);
            nw.Write(ebxCount, Endian.Little);
            nw.Write(resCount, Endian.Little);
            nw.Write(chunkCount, Endian.Little);
            nw.Write(stringOffset - AdditionalHeaderLength, Endian.Little);
            nw.Write(metaOffset - AdditionalHeaderLength, Endian.Little);
            nw.Write(metaSize - AdditionalHeaderLength, Endian.Little);
            return memoryStream.ToArray();
        }
    }
}
