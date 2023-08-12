using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Frostbite.FIFA
{
    /// <summary>
    /// An AttribDb Manager designed to read/write old FIFA Gameplay Dbs
    /// </summary>
    public class AttribdbManager
    {
        public AttribdbManager(byte[] vltBytes, byte[] binBytes) 
        {
            ReadVlt(vltBytes);
            ReadBin(vltBytes);
        }

        public void ReadVlt(byte[] vltBytes)
        {
            if (vltBytes == null)
                return;

            if (vltBytes.Length == 0)
                return;

            using NativeReader nr = new NativeReader(vltBytes);
        }

        public void ReadBin(byte[] binBytes)
        {
            if (binBytes == null)
                return;

            if (binBytes.Length == 0)
                return;

            using NativeReader nr = new NativeReader(binBytes);
        }
    }
}
