using FMT.FileTools;
using FMT.FileTools.Modding;
using FrostySdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FrostbiteSdk
{
    public class FIFAMod : IFrostbiteMod
    {
        public const ulong HeaderMagic1 = 72155812747760198uL;

        public const ulong HeaderMagic2 = 5498700893333637446uL;

        public const uint Version = 8u;

        private readonly List<string> warnings = new List<string>();
        public FrostbiteModDetails ModDetails { get; set; }

        public string Path
        {
            get;
            set;
        }

        public string Filename
        {
            get;
            set;
        }

        public EGame Game { get; set; }

        public int GameVersion
        {
            get;
        }

        public IEnumerable<string> Warnings => warnings;

        public string FirstWarning => warnings.FirstOrDefault();

        public bool HasWarnings => warnings.Count != 0;

        public bool NewFormat
        {
            get;
        }

        public byte[] ModBytes { get; set; }

        public IEnumerable<BaseModResource> Resources { get; set; }

        public static IFrostbiteMod CurrentFIFAModInstance = null;

        public FIFAModReader ModReader { get; set; }

        public FIFAMod(string gameName, string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }
            FileInfo fileInfo = new FileInfo(filename);
            Filename = fileInfo.Name;
            Path = filename;

            using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                ModReader = new FIFAModReader(fileStream);
                if (!ModReader.IsValid)
                {
                    throw new InvalidDataException("The file is not a valid mod.");
                }
                NewFormat = true;
                GameVersion = ModReader.GameVersion;
                Game = ModReader.Game;
                ModDetails = ModReader.ReadModDetails();
                Resources = ModReader.ReadResources();
                ModDetails.SetIcon(ModReader.GetResourceData(Resources.First()));
                for (int i = 0; i < ModDetails.ScreenshotsCount; i++)
                {
                    byte[] resourceData = ModReader.GetResourceData(Resources.ElementAt(i + 1));
                    if (resourceData != null)
                    {
                        ModDetails.AddScreenshot(resourceData);
                    }
                }

                CurrentFIFAModInstance = this;
            }
        }

        public FIFAMod(Stream stream, bool loadIntoModBytes = true)
        {
            if (stream is FileStream)
                Path = ((FileStream)stream).Name;

            // ----------------------------------------------
            // Get Mod Bytes
            if (loadIntoModBytes)
            {
                var position = stream.Position;
                var nrQuickRead = new NativeReader(stream);
                nrQuickRead.Position = 0;
                ModBytes = nrQuickRead.ReadToEnd();
                stream.Position = position;
            }
            //
            // ----------------------------------------------

            FIFAModReader modReader = new FIFAModReader(stream);
            if (!modReader.IsValid)
            {
                throw new InvalidDataException("The file is not a valid mod.");
            }
            NewFormat = true;
            GameVersion = modReader.GameVersion;
            ModDetails = modReader.ReadModDetails();
            Resources = modReader.ReadResources();
            ModDetails.SetIcon(modReader.GetResourceData(Resources.First()));
            for (int i = 0; i < ModDetails.ScreenshotsCount; i++)
            {
                byte[] resourceData = modReader.GetResourceData(Resources.ElementAt(i + 1));
                if (resourceData != null)
                {
                    ModDetails.AddScreenshot(resourceData);
                }
            }

            CurrentFIFAModInstance = this;

        }

        public byte[] GetResourceData(BaseModResource resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }
            if (ModBytes != null)
            {
                return new FIFAModReader(new MemoryStream(ModBytes)).GetResourceData(resource);
            }
            else
            {
                using FileStream fileStream = new FileStream(Path, FileMode.Open, FileAccess.Read);
                return new FIFAModReader(fileStream).GetResourceData(resource);
            }
        }

        public byte[] GetResourceData(BaseModResource resource, Stream stream)
        {
            stream.Position = 0;
            FrostbiteModReader frostyModReader = new FrostbiteModReader(stream);
            return frostyModReader.GetResourceData(resource);
        }

        public void AddWarning(string warning)
        {
            warnings.Add(warning);
        }

        public void Dispose()
        {
            ModDetails = null;
            ModBytes = null;
        }
    }
}