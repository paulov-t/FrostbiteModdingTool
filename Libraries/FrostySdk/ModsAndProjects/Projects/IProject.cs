using FMT.FileTools;
using FrostbiteSdk;
using FrostySdk.Frosty.FET;
using FrostySdk.Managers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static PInvoke.Kernel32;

namespace FrostySdk.ModsAndProjects.Projects
{
    public interface IProject
    {
        public string FileExtension { get; }

        public bool IsDirty { get; }
        public string Filename { get; set; }
        public string DisplayName { get; }
        public AssetManager AssetManager { get; }
        public ModSettings ModSettings { get; }
        public IEnumerable<AssetEntry> ModifiedAssetEntries { get; }

        public bool Load(in FIFAMod fifaMod);
        public bool Load(in FIFAModReader reader);
        public bool Load(in FrostbiteMod frostbiteMod);
        public bool Load(in string inFilename);
        public bool Load(in Stream inStream);

        public Task<bool> LoadAsync(string fileName, CancellationToken cancellationToken = default(CancellationToken));

        public void WriteToMod(string filename, ModSettings overrideSettings = null);

        public void WriteToFIFAMod(string filename, ModSettings overrideSettings = null);

        public Task<bool> SaveAsync(string overrideFilename, bool updateDirtyState);
    }
}
