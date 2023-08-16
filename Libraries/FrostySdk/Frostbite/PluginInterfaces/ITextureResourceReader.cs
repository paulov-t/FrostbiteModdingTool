﻿using FMT.FileTools;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public interface ITextureResourceReader
    {
        void ReadInStream(NativeReader nativeReader, Texture texture);
    }
}
