using FMT.FileTools;
using FMT.Logging;
using FrostySdk;
using FrostySdk.Frostbite.Compilers;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using ModdingSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FIFA23Plugin
{

    /// <summary>
    /// FIFA 23 Asset Compiler. Solid and works. Uses .cache file to determine what needs editing
    /// Linked to FIFA21BundleAction
    /// </summary>
    public class Fifa23AssetCompilerV1 : Frostbite2022AssetCompiler, IAssetCompiler
    {
        /// <summary>
        /// This is run AFTER the compilation of the fbmod into resource files ready for the Actions to TOC/SB/CAS to be taken
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="logger"></param>
        /// <param name="frostyModExecuter">Frosty Mod Executer object</param>
        /// <returns></returns>
        public override bool Compile(FileSystem fs, ILogger logger, ModExecutor modExecuter)
        {
            base.Compile(fs, logger, modExecuter);

            DateTime dtStarted = DateTime.Now;
            if (!ProfileManager.IsFIFA23DataVersion())
            {
                logger.Log("[ERROR] Wrong compiler used for Game");
                return false;
            }

            bool result = false;
            ErrorCounts.Clear();
            ErrorCounts.Add(ModType.EBX, 0);
            ErrorCounts.Add(ModType.RES, 0);
            ErrorCounts.Add(ModType.CHUNK, 0);
            ModExecutor.UseModData = true;

            result = RunModDataCompiler(logger);

            logger.Log($"Compiler completed in {(DateTime.Now - dtStarted).ToString(@"mm\:ss")}");
            return result;
        }

        private bool RunModDataCompiler(ILogger logger)
        {
            if (!Directory.Exists(FileSystem.Instance.BasePath))
                throw new DirectoryNotFoundException($"Unable to find the correct base path directory of {FileSystem.Instance.BasePath}");

            Directory.CreateDirectory(FileSystem.Instance.BasePath + ModDirectory + "\\Data");
            Directory.CreateDirectory(FileSystem.Instance.BasePath + ModDirectory + "\\Patch");

            logger.Log("Copying files from Data to ModData/Data");
            CopyDataFolder(FileSystem.Instance.BasePath + "\\Data\\", FileSystem.Instance.BasePath + ModDirectory + "\\Data\\", logger);
            logger.Log("Copying files from Patch to ModData/Patch");
            CopyDataFolder(FileSystem.Instance.BasePath + "\\Patch\\", FileSystem.Instance.BasePath + ModDirectory + "\\Patch\\", logger);

            return Run();
        }


        private ModExecutor parent => ModExecuter;

        public bool GameWasPatched => parent.GameWasPatched;

        public List<ChunkAssetEntry> AddedChunks = new List<ChunkAssetEntry>();

        Dictionary<string, DbObject> SbToDbObject = new Dictionary<string, DbObject>();

        private void DeleteBakFiles(string path)
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.FullName.Contains(".bak"))
                {
                    fileInfo.Delete();
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                DeleteBakFiles(dir);
            }
        }

        public bool Run()
        {
            //parent.Logger.Log("Loading files to know what to change.");
            if (parent.modifiedEbx.Count == 0 && parent.modifiedRes.Count == 0 && parent.ModifiedChunks.Count == 0 && parent.modifiedLegacy.Count == 0)
                return true;

            //if (AssetManager.Instance == null)
            //{
            //    CacheManager buildCache = new CacheManager();
            //    buildCache.LoadData(ProfileManager.ProfileName, parent.GamePath, parent.Logger, false, true);
            //}

            if (!ModExecutor.UseModData && GameWasPatched)
            {
                DeleteBakFiles(parent.GamePath);
            }

            parent.Logger.Log("Retreiving list of Modified CAS Files.");
            var dictOfModsToCas = GetModdedCasFiles();

            // Force delete of Live Tuning Updates
            if (dictOfModsToCas.Any(x => x.Value.Any(y => y.NamePath.Contains("gp_", StringComparison.OrdinalIgnoreCase))))
                parent.DeleteLiveUpdates = true;

            parent.Logger.Log("Modifying TOC Chunks.");
            var modifiedTocChunks = ModifyTOCChunks();

            var entriesToNewPosition = WriteNewDataToCasFiles(dictOfModsToCas);
            if (entriesToNewPosition == null || entriesToNewPosition.Count == 0)
                return true;

            bool result = WriteNewDataChangesToSuperBundles(ref entriesToNewPosition);
            result = WriteNewDataChangesToSuperBundles(ref entriesToNewPosition, "native_data");

            if (entriesToNewPosition.Count > 0)
            {
                var entriesErrorText = $"{entriesToNewPosition.Count} Entries were not written to TOC. Parts of the mod have been removed.";
                parent.Logger.Log(entriesErrorText);

                // Write to File Logger the entire list
                FileLogger.WriteLine(entriesErrorText);
                foreach(var entry in entriesToNewPosition)
                {
                    FileLogger.WriteLine(entry.Key.Name);
                }

                // Write to File Logger the error list
                foreach (var entry in AssetEntryErrorList)
                {
                    FileLogger.WriteLine(entry.Key + "::" + entry.Value);
                }
            }

            return result;

        }

    }



}
