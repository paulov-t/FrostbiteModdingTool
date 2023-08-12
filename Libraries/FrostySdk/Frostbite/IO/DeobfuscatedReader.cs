using FMT.FileTools;
using FrostySdk.Interfaces;
using System;
using System.IO;

namespace FrostySdk.Frostbite.IO
{
    public class DeobfuscatedReader : NativeReader
    {
        /// <summary>
        /// This constructor ignores any attempt to Deobfuscate and passes straight into Native Reader
        /// </summary>
        /// <param name="stream"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public DeobfuscatedReader(Stream stream) : base(stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
        }

        public DeobfuscatedReader(Stream stream, IDeobfuscator deobfuscator) : base(stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            //if (deobfuscator == null)
            //    throw new ArgumentNullException("deobfuscator");
            if (deobfuscator == null)
                return;

            long num = deobfuscator.Initialize(this);
            if (num != -1)
            {
                //streamLength = num;
            }
        }
    }
}
