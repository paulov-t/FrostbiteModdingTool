﻿using FMT.FileTools;
using FrostySdk.IO;
using System;
using System.IO;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public class BinarySbWriter : DbWriter
    {
        private Endian endian;
        private BaseBinarySbWriter binarySbWriter;

        public BinarySbWriter(Stream inStream, bool leaveOpen = false, Endian inEndian = Endian.Big, in BaseBinarySbWriter baseBinarySbWriter = null)
            : base(inStream, leaveOpen: leaveOpen)
        {
            if (baseBinarySbWriter == null)
                binarySbWriter = new BaseBinarySbWriter();
                //throw new ArgumentNullException(nameof(baseBinarySbWriter));

            binarySbWriter = baseBinarySbWriter;
            if(binarySbWriter == null)
                binarySbWriter = new BaseBinarySbWriter();

            endian = inEndian;
        }

        public override void Write(DbObject inObj)
        {
            binarySbWriter.Write(this, inObj, endian);
        }
    }
}
