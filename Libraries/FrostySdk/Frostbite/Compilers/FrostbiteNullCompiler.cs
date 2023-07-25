﻿using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using ModdingSupport;

namespace FrostySdk.Frostbite.Compilers
{
    /// <summary>
    /// A "compiler" that does nothing at all and passes a true back to the Executor
    /// </summary>
    public class FrostbiteNullCompiler : IAssetCompiler
    {
        public bool Cleanup(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            return true;
        }

        public bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            logger.Log($"NULL Compiler. Doing nothing.");

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
