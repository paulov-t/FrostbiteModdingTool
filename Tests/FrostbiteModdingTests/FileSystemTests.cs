using FMT.Logging;
using FrostySdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostbiteModdingTests
{
    internal class FileSystemTests : ILogger
    {
        public void Log(string text, params object[] vars)
        {
            throw new NotImplementedException();
        }

        public void LogError(string text, params object[] vars)
        {
            throw new NotImplementedException();
        }

        public void LogProgress(int progress)
        {
            throw new NotImplementedException();
        }

        public void LogWarning(string text, params object[] vars)
        {
            throw new NotImplementedException();
        }
    }
}
