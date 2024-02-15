using FC24Plugin;
using FMT.FileTools;
using Frostbite.FileManagers;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.Managers;
using FrostySdk.ModsAndProjects.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModdingSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using v2k4FIFAModding.Frosty;
using v2k4FIFAModdingCL;
using static FrostbiteSdk.Frosty.Abstract.BaseModReader;
using static FrostySdk.FrostbiteModWriter;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class EASFC24Tests : IFMTTest
    {
        public string GamePath => throw new NotImplementedException();

        public string GameName => throw new NotImplementedException();

        public string GameEXE => throw new NotImplementedException();

        public string GamePathEXE => "I:\\EA Games\\EA SPORTS FC 24\\FC24.exe";

        public string TestMeshesPath
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }

        [TestMethod]
        public void BuildCache()
        {
            var buildCache = new CacheManager();
            buildCache.Load(GamePathEXE, this, true, true);

            var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            var resItems = AssetManager.Instance.EnumerateRes().ToList();
            var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        private string prevText = string.Empty;

        public void Log(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine($"[LOGGER][DEBUG][{DateTime.Now.ToShortTimeString()}] {text}");
                prevText = text;
            }
        }

        public void LogError(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine($"[LOGGER][ERROR][{DateTime.Now.ToShortTimeString()}] {text}");
                prevText = text;
            }
        }

        public void LogWarning(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine($"[LOGGER][WARN][{DateTime.Now.ToShortTimeString()}] {text}");
                prevText = text;
            }
        }

        public void LogProgress(int progress)
        {
        }

        [TestMethod]
        public void ExportFaceMesh()
        {
            //GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            //ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            //var project = projectManagement.StartNewProject();
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            //var skinnedMeshEntry = AssetManager.Instance.EnumerateEbx("SkinnedMeshAsset").Where(x => x.Name.ToLower().Contains("head_201942_0_0_mesh")).FirstOrDefault();
            var skinnedMeshEntry = AssetManager.Instance.EnumerateEbx().Where(x => x.Name.ToLower().Contains("head_240_0_0_mesh")).FirstOrDefault();
            //var skinnedMeshEntry = AssetManager.Instance.EnumerateEbx().Where(x => x.Name.ToLower().Contains("haircap_229037_0_0_mesh")).FirstOrDefault();
            //var skinnedMeshEntry = AssetManager.Instance.EnumerateEbx().Where(x => x.Name.ToLower().Contains("haircap_201942_0_0_mesh")).FirstOrDefault();
            //var skinnedMeshEntry = AssetManager.Instance.EnumerateEbx().Where(x => x.Name.ToLower().Contains("hair_201942_0_0_mesh")).FirstOrDefault();
            Assert.IsNotNull(skinnedMeshEntry);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var skinnedMeshEbx = AssetManager.Instance.GetEbx(skinnedMeshEntry);
            Assert.IsNotNull(skinnedMeshEbx);

            var resentry = AssetManager.Instance.GetResEntry(skinnedMeshEntry.Name);
            var res = AssetManager.Instance.GetRes(resentry);

            var exporter1 = new MeshSetToFbxExport();
            MeshSet meshSet = exporter1.LoadMeshSet(skinnedMeshEntry);

            exporter1.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, Path.Combine(TestMeshesPath, "test.fbx"), "FBX_2012", "Meters", true, "content/character/rig/skeleton/player/skeleton_player", "fbx", meshSet);

            sw.Stop();
            Debug.WriteLine($"{sw.Elapsed}");
        }


        [TestMethod]
        public void LoadLegacy()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);

            var ebxFCC = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("legacy", StringComparison.OrdinalIgnoreCase));
            var ebxFile = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("file", StringComparison.OrdinalIgnoreCase));
            var ebxCollector = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("collector", StringComparison.OrdinalIgnoreCase));
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }


        /// <summary>
        /// This test will load as much as it can from a TOC without having the full file system or assetmanager
        /// </summary>
        [TestMethod]
        public void ProcessTOC()
        {
            var tocDataResource = FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.Data.globalsfull.toc");

            FC24TOCFile tocDataFile = new FC24TOCFile(tocDataResource, true, false, false, -1, false);

            var tocPatchResource = FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.Patch.globalsfull.toc");

            FC24TOCFile tocPatchFile = new FC24TOCFile(tocPatchResource, true, false, false, -1, false);

        }


        /// <summary>
        /// This will test the recompress chunks methods
        /// </summary>
        [TestMethod]
        public void LoadLegacyProjectAndRebuildChunks()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);

            //var fmtproj = FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.LegacyFile.RotherhamCrest.fmtproj");
            var fmtproj = FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.LegacyFile.FinanceTest.fmtproj");
            FMTProject project = FMTProject.Read(fmtproj);

            var legacyFileManager = AssetManager.Instance.GetLegacyAssetManager() as ChunkFileManager2022;
            if (legacyFileManager != null)
            {
                Dictionary<string, byte[]> legacyData = new Dictionary<string, byte[]>();
                foreach (LegacyFileEntry lfe in AssetManager.Instance.EnumerateCustomAssets("legacy", true))
                {
                    if (lfe.HasModifiedData)
                    {
                        legacyData.Add(lfe.Name, lfe.ModifiedEntry.Data);
                    }
                }
                
                legacyFileManager.ModifyAssets(legacyData, true);
            }
        }

        [TestMethod]
        public void LoadLegacyRotherhamCrestProjectAndLaunch()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            var fmtproj = FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.LegacyFile.RotherhamCrest.fmtproj");
            FMTProject project = FMTProject.Read(fmtproj);
            project.ModSettings.Author = "RotherhamCrest";
            project.ModSettings.Title = "RotherhamCrest";
            project.WriteToMod("test.fbmod", project.ModSettings);
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void LoadLegacyFinanceProjectAndLaunch()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            var fmtproj = FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.LegacyFile.FinanceTest.fmtproj");
            FMTProject project = FMTProject.Read(fmtproj);
            project.ModSettings.Author = "Finance Test";
            project.ModSettings.Title = "Finance Test";
            project.WriteToMod("test.fbmod", project.ModSettings);
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void LoadGrassTextureProjectAndLaunch()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            var fmtproj = FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.GrassFlow.Test.fmtproj");
            FMTProject project = FMTProject.Read(fmtproj);
            project.ModSettings.Author = "Grass Texture Test";
            project.ModSettings.Title = "Grass Texture Test";
            project.WriteToMod("test.fbmod", project.ModSettings);
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();
        }

        [TestMethod]
        public void LoadUltraSlowGPProjectAndLaunch()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            var fmtproj = FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.Gamplay.UltraSlowUltraMiss.Test.fmtproj");
            FMTProject project = FMTProject.Read(fmtproj);
            project.ModSettings.Author = "Ultra Slow Test";
            project.ModSettings.Title = "Ultra Slow Test";
            project.WriteToMod("test.fbmod", project.ModSettings);
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();
        }

        [TestMethod]
        public void LoadGPModAndLaunch()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, logger: this);
            var fmtmod = FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.GameplayMod.fbmod");
            File.WriteAllBytes("test.fbmod", new NativeReader(fmtmod).ReadToEnd());
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();
        }

    }
}
