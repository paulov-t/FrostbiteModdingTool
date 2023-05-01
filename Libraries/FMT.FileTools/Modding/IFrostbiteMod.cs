using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FMT.FileTools.Modding
{
    public interface IFrostbiteMod : IDisposable
    {
        public static uint[] HashVersions => new uint[]
        {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            (uint)Fnv1a.HashString("FBMod7"),
            (uint)Fnv1a.HashStringByHashDepot("FBMod7")
         };
        public static uint[] HashVersions_Pre2323 => new uint[]
        {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            (uint)Fnv1a.HashStringByHashDepot("FBMod7")
        };

        public static uint CurrentVersion => HashVersions.Last();

        public EGame Game { get; set; }
        public string Filename { get; set; }
        public IEnumerable<BaseModResource> Resources
        {
            get;
            set;
        }

        public FrostbiteModDetails ModDetails { get; set; }

        //public byte[] ModBytes { get; set; }

        public byte[] GetResourceData(BaseModResource resource);

        public byte[] GetResourceData(BaseModResource resource, Stream stream);


    }
}
