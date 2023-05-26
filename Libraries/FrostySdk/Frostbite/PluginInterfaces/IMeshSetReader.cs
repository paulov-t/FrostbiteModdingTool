using FMT.FileTools;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public interface IMeshSetReader
    {
        public void Read(NativeReader nativeReader, MeshSet meshSet);
    }

    public interface IMeshSetSectionReader
    {
        public void Read(NativeReader nativeReader, MeshSetSection section, int index);
    }
}
