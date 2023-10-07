using FMT.FileTools;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public interface IMeshSetWriter
    {
        public void Write(NativeWriter writer, MeshSet meshSet, MeshContainer meshContainer);
    }

    public interface IMeshSetSectionWriter
    {
        public void Write(NativeWriter writer, MeshSetSection section, MeshContainer meshContainer);
    }
}
