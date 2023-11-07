using FMT.Logging;
using FrostySdk.Frostbite.PluginInterfaces;
using ModdingSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Frostbite.Compilers
{
    internal class DirectTextureSwapperCompiler : IAssetCompiler
    {
        public string ModDirectory => throw new NotImplementedException();

        public bool Cleanup(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public bool PostCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public bool PreCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public bool RunGame(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }
    }
}
