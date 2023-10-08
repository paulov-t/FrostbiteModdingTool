using FMT.FileTools;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public interface IMeshSetLodWriter
    {
        public void Write(NativeWriter writer, MeshContainer meshContainer, MeshSetLod meshSetLod);

    }
}
