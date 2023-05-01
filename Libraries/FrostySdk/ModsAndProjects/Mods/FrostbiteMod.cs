using FMT.FileTools;
using FMT.FileTools.Modding;
using FrostySdk.IO;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fnv1a = FMT.FileTools.Fnv1a;

namespace FrostbiteSdk
{
    public class FrostbiteMod : IFrostbiteMod
    {
        public static ulong MagicFrosty => 72155812747760198uL;
        public static ulong MagicFMT => Convert.ToUInt64(Fnv1a.HashString("FMT"));
        public static ulong MagicFMT_Pre2323 => Convert.ToUInt64(Fnv1a.HashStringByHashDepot("FMT"));

        
        public static uint CurrentVersion => IFrostbiteMod.HashVersions.Last();



        private string path;


        private int gameVersion;

        private List<string> warnings = new List<string>();

        private bool bNewFormat;
        private bool disposedValue;

        public FrostbiteModDetails ModDetails { get; set; }

        public string Path => path;


        public int GameVersion => gameVersion;

        public IEnumerable<string> Warnings => warnings;

        public bool HasWarnings => warnings.Count != 0;

        public bool NewFormat => bNewFormat;


        public string Filename { get; set; }
        public IEnumerable<BaseModResource> Resources { get; set; }

        public FrostbiteMod(in string inFilename)
        {
            FileInfo fileInfo = new FileInfo(inFilename);
            Filename = fileInfo.Name;
            path = inFilename;
            using (var fs = new FileStream(path, FileMode.Open))
            {
                ReadFromStream(fs);
            }
        }

        public FrostbiteMod(in Stream stream, in string inFilename)
        {
            path = inFilename;
            ReadFromStream(stream);
        }

        public bool IsEncrypted { get; set; }

        //public byte[] ModBytes { get; set; }
        public EGame Game { get; set; }

        short CompressType = -1;

        byte[] ModBytes = null;

        private void ReadFromStream(Stream stream)
        {
            // Read initial bytes
            stream.Position = 0;

            var nr = new NativeReader(stream);
            // Check for Zip or Zstd
            CompressType = nr.ReadShort();
            if (CompressType == 1)
            {
                throw new NotSupportedException("FMT no longer supports Zip Compressed Frostbite Mods");
                //var m = new MemoryStream(ModBytes, 2, ModBytes.Length - 2);
                //using (ZipFile zipFileReader = ZipFile.Read(m))
                //{
                //    var entry = zipFileReader.Entries.First();
                //    var entryStream = new MemoryStream();
                //    entry.Extract(entryStream);
                //    entryStream.Position = 0;
                //    ModBytes = new NativeReader(entryStream).ReadToEnd();
                //}
            }
            else if (CompressType == 2)
            {
                ModBytes = nr.ReadToEnd();
                stream.Close();
                nr.Dispose();
                //throw new NotSupportedException("FMT no longer supports compressed Frostbite Mods");
                using (var m = new MemoryStream(ModBytes))
                {
                    CasReader casReader = new CasReader(m);
                    ModBytes = casReader.Read();
                }
                stream = new MemoryStream(ModBytes);
            }

            stream.Position = 0;

            // Read internal Bytes
            //using (var innerStream = new MemoryStream(stream))
            {
                using (FrostbiteModReader frostyModReader = new FrostbiteModReader(stream))
                {
                    if (frostyModReader.IsValid)
                    {
                        bNewFormat = true;
                        Game = frostyModReader.Game;
                        gameVersion = frostyModReader.GameVersion;
                        ModDetails = frostyModReader.ReadModDetails();
                        if (ModDetails == null || string.IsNullOrEmpty(ModDetails.Title))
                            throw new Exception("Frostbite Mod Details doesn't have a Title");

                        Resources = frostyModReader.ReadResources();
                        if (Resources == null || !Resources.Any())
                            throw new Exception("Frostbite Mod doesn't have any Resources");

                        ModDetails.SetIcon(frostyModReader.GetResourceData(Resources.First()));
                        for (int i = 0; i < 4; i++)
                        {
                            byte[] resourceData = frostyModReader.GetResourceData(Resources.ElementAt(i + 1));
                            if (resourceData != null)
                            {
                                ModDetails.AddScreenshot(resourceData);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Frostbite Mod is Invalid");
                    }
                }
            }
            //ModBytes = null;
            GC.Collect(4, GCCollectionMode.Forced, true, true);
            //GC.WaitForPendingFinalizers();
            //GC.Collect();
        }

        public byte[] GetResourceData(BaseModResource resource)
        {
            if (CompressType != -1 && ModBytes != null && ModBytes.Length > 0)
            {
                using(var ms = new MemoryStream(ModBytes))
                    return GetResourceData(resource, ms);
            }
            using (FrostbiteModReader frostyModReader = new FrostbiteModReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
            {
                return frostyModReader.GetResourceData(resource);
            }
        }

        public byte[] GetResourceData(BaseModResource resource, Stream stream)
        {
            stream.Position = 0;
            FrostbiteModReader frostyModReader = new FrostbiteModReader(stream);
            {
                return frostyModReader.GetResourceData(resource);
            }
        }

        public void AddWarning(string warning)
        {
            warnings.Add(warning);
        }

        public override string ToString()
        {
            if (ModDetails != null)
            {
                return ModDetails.Title;
            }
            return base.ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    ModDetails = null;
                    Resources = null;
                    if(ModBytes != null)
                    {
                        ModBytes = null;
                        CompressType = -1;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                path = null;
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FrostbiteMod()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}