using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using ModdingSupport;

namespace FIFA17Plugin
{
    public class FIFA17AssetCompiler : IAssetCompiler
    {
        public string ModDirectory => "ModData";

        public bool Cleanup(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            throw new System.NotImplementedException();
        }

        public bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            throw new System.NotImplementedException();
        }

        public bool PostCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            // Probably best to do the fifaconfig.exe change here


            throw new System.NotImplementedException();
        }

        public bool PreCompile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            throw new System.NotImplementedException();
        }

        public bool RunGame(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            throw new System.NotImplementedException();
        }
    }
}