using FMT.Logging;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using ModdingSupport;

namespace FrostySdk.Frostbite.Compilers
{
    /// <summary>
    /// A "compiler" that does nothing at all and passes a true back to the Executor
    /// </summary>
    public class FrostbiteNullCompiler : IAssetCompiler
    {
        public virtual string ModDirectory { get; } = "ModData";

        public virtual bool Cleanup(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public virtual bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            logger.Log($"NULL Compiler. Doing nothing.");

            return true;
        }

        public virtual bool PostCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public virtual bool PreCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public virtual bool RunGame(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }
    }
}
