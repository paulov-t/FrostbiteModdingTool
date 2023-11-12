using FrostySdk.Frostbite.Compilers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHL22PS4Plugin
{
    public class NHL22PS4AssetCompiler : Frostbite2022AssetCompiler
    {
        public override bool RequiresCacheToCompile => false;
        public override bool CanProcessEbx => false;
        public override bool CanProcessRes => false;
        public override bool CanProcessChunks => true;
        public override bool CanProcessTocChunks => false;
    }
}
