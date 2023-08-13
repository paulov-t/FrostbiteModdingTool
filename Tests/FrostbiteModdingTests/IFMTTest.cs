using FMT.Logging;
using FrostySdk.Interfaces;

namespace FrostbiteModdingTests
{
    internal interface IFMTTest : ILogger
    {
        public string GamePath { get; }

        public string GameName { get; }

        public string GameEXE { get; }

        public string GamePathEXE { get; }

        public void BuildCache();
    }
}
