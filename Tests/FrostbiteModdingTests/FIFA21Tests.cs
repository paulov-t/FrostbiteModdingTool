using FMT.FileTools;
using Frostbite.FileManagers;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using SdkGenerator;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using v2k4FIFAModding.Frosty;
using v2k4FIFAModdingCL;
using static System.Net.Mime.MediaTypeNames;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class FIFA21Tests : ILogger
    {
        private string prevText = string.Empty;

        public const string ModdingDirectory = @"G:\Work\FIFA Modding\";
        public string GamePath
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\EA Sports\\FIFA 21"))
                {
                    if (key != null)
                    {
                        string installDir = key.GetValue("Install Dir").ToString();
                        return installDir;
                    }
                }
                return string.Empty;
            }
        }
        public string GamePathEXE
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\EA Sports\\FIFA 21"))
                {
                    if (key != null)
                    {
                        string installDir = key.GetValue("Install Dir").ToString();
                        return installDir + "FIFA21.exe";
                    }
                }
                return string.Empty;
            }
        }

        public string TestMeshesPath
        {
            get
            {
                return @"C:\Users\paula\Desktop\";
            }
        }
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

        [TestMethod]
        public void TestBuildCache()
        {
            var buildCache = new CacheManager();
            //buildCache.LoadData("FIFA21", @"E:\Origin Games\FIFA 21", this, false);
            buildCache.LoadData("FIFA21", GamePath, this, true, true);
        }

        [TestMethod]
        public void TestBuildCacheIndexing()
        {
            var buildCache = new CacheManager();
            //buildCache.LoadData("FIFA21", @"E:\Origin Games\FIFA 21", this, false);
            buildCache.LoadData("FIFA21", GamePath, this, false);
            //AssetManager.Instance.ebxGuidList.Clear();
            AssetManager.Instance.DoEbxIndexing();
        }

        [TestMethod]
        public void TestBuildSDK()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("FIFA21", GamePath, this, false);

            var buildSDK = new BuildSDK();
            buildSDK.Build().Wait();
        }

        [TestMethod]
        public void TestLiveLegacySystem()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE);
            var legacyModSupportFile = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + @"\FIFA21Legacy.dll";

            if (!string.IsNullOrEmpty(legacyModSupportFile))
            {
                File.Copy(legacyModSupportFile, @GameInstanceSingleton.Instance.GAMERootPath + "v2k4LegacyModSupport.dll", true);

                var legmodsupportdllpath = @GameInstanceSingleton.Instance.GAMERootPath + @"v2k4LegacyModSupport.dll";
                GameInstanceSingleton.InjectDLL(legmodsupportdllpath);
            }

        }

        [TestMethod]
        public void TestExportFaceMesh()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            var project = projectManagement.StartNewProject();
            var skinnedMeshEntry = project.AssetManager.EnumerateEbx("SkinnedMeshAsset").Where(x => x.Name.ToLower().Contains("head_10264_0_0_mesh")).FirstOrDefault();
            if (skinnedMeshEntry != null)
            {
                var skinnedMeshEbx = project.AssetManager.GetEbx(skinnedMeshEntry);
                if (skinnedMeshEbx != null)
                {
                    var resentry = project.AssetManager.GetResEntry(skinnedMeshEntry.Name);
                    var res = project.AssetManager.GetRes(resentry);

                    var exporter1 = new MeshSetToFbxExport();
                    MeshSet meshSet = exporter1.LoadMeshSet(skinnedMeshEntry);

                    exporter1.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, "test.fbx", "FBX_2012", "Meters", true, "content/character/rig/skeleton/player/skeleton_player", "fbx", meshSet);

                    //exporter2.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, "test.fbx", "2012", "Meters", true, "content/character/rig/skeleton/player/skeleton_player", "fbx", meshSet);
                    //meshToFbxExporter.Export(base.AssetManager, base.Asset, outputFile, "2020", "Meters", false, skeleton, "obj", meshSet);
                    //exporter.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, "test_noSkel.fbx", "FBX_2012", "Centimeters", true, null, "*.fbx", meshSet);

                    //exporter.OnlyFirstLOD = true;
                    //exporter.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, "test_noSkel.obj", "2016", "Meters", true, null, "*.obj", meshSet);
                }
            }
        }



        [TestMethod]
        public ProjectManagement TestImportFaceMesh()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            var project = projectManagement.StartNewProject();
            //var skinnedMeshEntry = project.AssetManager.EnumerateEbx("SkinnedMeshAsset").Where(x => x.Name.ToLower().Contains("head_192563_0_0_mesh")).FirstOrDefault();
            var skinnedMeshEntry = project.AssetManager.EnumerateEbx("SkinnedMeshAsset").Where(x => x.Name.ToLower().Contains("head_192563_0_0_mesh")).FirstOrDefault();
            if (skinnedMeshEntry != null)
            {
                var skinnedMeshEbx = project.AssetManager.GetEbx(skinnedMeshEntry);
                if (skinnedMeshEbx != null)
                {
                    var resentry = project.AssetManager.GetResEntry(skinnedMeshEntry.Name);
                    var res = project.AssetManager.GetRes(resentry);
                    MeshSet meshSet = new MeshSet(res);

                    FrostySdk.Frostbite.IO.Input.FBXImporter importer = new FrostySdk.Frostbite.IO.Input.FBXImporter();
                    //FrostySdk.Frostbite.IO.Input.FT.FBXImporter2 importer = new FrostySdk.Frostbite.IO.Input.FT.FBXImporter2(AssetManager.Instance);
                    var exporter = new MeshSetToFbxExport();
                    exporter.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, "test.fbx", "FBX_2012", "Meters", true, "content/character/rig/skeleton/player/skeleton_player", "*.fbx", meshSet);
                    //importer.ImportFBX("test.fbx", meshSet, skinnedMeshEbx, skinnedMeshEntry, new FrostySdk.Frostbite.IO.Input.MeshImportSettings()
                    importer.ImportFBX(@"C:\Users\paula\Desktop\head_250_0_0_mesh.fbx", meshSet, skinnedMeshEbx, skinnedMeshEntry, new FrostySdk.Frostbite.IO.Input.MeshImportSettings()
                    {
                        SkeletonAsset = "content/character/rig/skeleton/player/skeleton_player"
                    });

                    res = project.AssetManager.GetRes(resentry);
                    meshSet = new MeshSet(res);
                    exporter.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, "test2.fbx", "FBX_2012", "Meters", true, "content/character/rig/skeleton/player/skeleton_player", "*.fbx", meshSet);


                }
            }
            return projectManagement;
        }

        [TestMethod]
        public void TestImportFaceMeshAndRun()
        {
            var projectManagement = TestImportFaceMesh();
            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        @"test.fbmod"
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void InjectSDKGeneratorIntoFIFA()
        {

            int? proc = GameInstanceSingleton.GetProcIDFromName("FIFA21").Result;
            proc = GameInstanceSingleton.GetProcIDFromName("FIFA21").Result;
            while (!proc.HasValue || proc == 0)
            {
                Debug.WriteLine($"Waiting for FIFA to appear");
                proc = GameInstanceSingleton.GetProcIDFromName("FIFA21").Result;
                Thread.Sleep(1000);
            }
            if (proc.HasValue)
            {
                var dllpath = @"G:\Work\FIFA Modding\SDKGenerator\x64\Debug\Publish\Generator.dll";
                if (File.Exists(dllpath))
                {
                    Debug.WriteLine($"Injecting: {dllpath}");
                    //var bl = new Bleak.Injector(Bleak.InjectionMethod.CreateThread, proc.Value, dllpath, false);
                    ////var bl = new Bleak.Injector(proc.Value, dllpath, Bleak.InjectionMethod.CreateThread);
                    //bl.InjectDll();
                    Debug.WriteLine($"Injected: {dllpath}");
                }
            }
        }

        //[TestMethod]
        //public void TestFETSaveFETProjectAndLoad()
        //{
        //    ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
        //    projectManagement.Project = new FrostySdk.FrostbiteProject();
        //    projectManagement.Project.Load(@"G:\Work\FIFA Modding\Gameplay mod\FIFA 21\Paulv2k4 FIFA 21 Gameplay Version 3 Final.fbproject");

        //    FIFAEditorProject editorProject = FIFAEditorProject.ConvertFromFbProject(projectManagement.Project, @"G:\testgpfromfbproject.fifaproject");
        //    //FIFAEditorProject editorProject = new FIFAEditorProject("FIFA21", AssetManager.Instance, AssetManager.Instance.fs);
        //    //editorProject.Save(@"G:\testgpfromfbproject.fifaproject");
        //    //editorProject.Load(@"G:\testgpfromfbproject.fifaproject");


        //}

        [TestMethod]
        public void TestFETFIFAMod()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath,
                new System.Collections.Generic.List<string>() {
                    //@"C:\Users\paula\Downloads\Villalibre Molina.fifamod"
                    //@"E:\Origin Games\FIFA 21\fet gp change.fifamod"
                    //@"G:\Work\FIFA Modding\Gameplay mod\FIFA 21\Paulv2k4 FIFA 21 Gameplay Version 3 Patch 1 FET.fifamod"
                    //@"G:\Work\FIFA Modding\Sky_Sports_Football_TV_Logo_for_the_English_Premier_League.fifamod"
                    //@"G:\Work\FIFA Modding\GraphicMod\FIFA 21\FCB17 Facepack #5 (fix).fifamod"
                    //@"G:\Work\FIFA Modding\GraphicMod\FIFA 21\Mason Greenwood by AlieFFR.fifamod"
                    //@"G:\Work\FIFA Modding\GraphicMod\FIFA 21\Other_Mods_Turfs_by_6ons1.fifamod"
                    @"G:\Work\FIFA Modding\FET FIFA 21 Splashscreen Mod.fifamod"


                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestFETFIFAMod_Legacy()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath,
                new System.Collections.Generic.List<string>() {
                    @"G:\Work\FIFA Modding\GraphicMod\FIFA 21\ALP 3.7.fifamod"
                }.ToArray()).Wait();

        }

        //[TestMethod]
        //public void OpenFETEditorProject()
        //{
        //    ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
        //    FIFAEditorProject editorProject = new FIFAEditorProject("FIFA21", AssetManager.Instance, AssetManager.Instance.fs);
        //    editorProject.Load(@"C:\Users\paula\Downloads\JAYS BOOTPACK.fifaproject");
        //    var count = AssetManager.Instance.EnumerateEbx(modifiedOnly: true).Count();
        //    var res = AssetManager.Instance.EnumerateRes(modifiedOnly: true).ToList();
        //    var count2 = res.Count();
        //    var count3 = AssetManager.Instance.EnumerateChunks(modifiedOnly: true).Count();


        //}

        [TestMethod]
        public void ReadSimpleGPFile()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var simpleEbxEntry = AssetManager.Instance.GetEbxEntry("fifa/attribulator/gameplay/groups/gp_actor/gp_actor_facialanim_runtime");
            Assert.IsNotNull(simpleEbxEntry);
            var simpleAsset = AssetManager.Instance.GetEbx(simpleEbxEntry);
        }

        [TestMethod]
        public void ReadComplexGPFile()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var ebxEntry = AssetManager.Instance.GetEbxEntry("fifa/attribulator/gameplay/groups/gp_actor/gp_actor_movement_runtime");
            Assert.IsNotNull(ebxEntry);
            var complexAsset = AssetManager.Instance.GetEbx(ebxEntry);
        }

        [TestMethod]
        public void TestGPMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\Gameplay mod\FIFA 21\V10\Paulv2k4 FIFA 21 Gameplay Version 10 Alpha 3.fbproject");

            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void TestSplashscreenMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\GraphicMod\FIFA 21\FIFA 21 Test Splashscreen Mod.fbproject");

            if (File.Exists("test.fbmod"))
                File.Delete("test.fbmod");

            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void TestCombinationMod()
        {
            //var modPath1 = @"G:\Work\FIFA Modding\Gameplay mod\FIFA 21\V10\Paulv2k4 FIFA 21 Gameplay Version 10 Alpha 2.fbmod";
            //var modPath3 = @"G:\Work\FIFA Modding\Career Mod\Paulv2k4 Career Realism Mod - V3A1.fbmod";
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void TestGPReadWriteSimple()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            var simpleebxgpname = "fifa/attribulator/gameplay/groups/gp_actor/gp_actor_facialanim_runtime";
            var ebx = AssetManager.Instance.GetEbx(AssetManager.Instance.GetEbxEntry(simpleebxgpname));
            AssetManager.Instance.ModifyEbx(simpleebxgpname, ebx);
            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());
        }


        [TestMethod]
        public void TestGPReadWriteSimpleWith1List()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            var simpleebxgpname = "fifa/attribulator/gameplay/groups/gp_cpuai/gp_cpuai_cpuaiballhandler_runtime";
            var ebx = AssetManager.Instance.GetEbx(AssetManager.Instance.GetEbxEntry(simpleebxgpname));
            AssetManager.Instance.ModifyEbx(simpleebxgpname, ebx);
            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());
        }

        [TestMethod]
        public void TestGPReadWriteComplex()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            var simpleebxgpname = "fifa/attribulator/gameplay/groups/gp_actor/gp_actor_movement_runtime";
            var ebx = AssetManager.Instance.GetEbx(AssetManager.Instance.GetEbxEntry(simpleebxgpname));
            AssetManager.Instance.ModifyEbx(simpleebxgpname, ebx);
            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());
        }

        [TestMethod]
        public void TestArsenalKitMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\GraphicMod\FIFA 21\Arsenal Kit 21-22.fbproject");
            var testR = "test.fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void TestLegacyTextureImport()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            AssetManager.Instance.DoLegacyImageImport(@"G:\Work\FIFA Modding\Career Mod\st_bootflow_v2k4.png"
                , (LegacyFileEntry)AssetManager.Instance.EnumerateCustomAssets("legacy")
                .FirstOrDefault(x => x.Name.Contains("st_bootflow", StringComparison.OrdinalIgnoreCase)));

            var testR = "test.fbmod";;
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void LoadLegacy()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("FIFA21", GamePath, this, false, true);

            var ebxFCC = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("legacy", StringComparison.OrdinalIgnoreCase));
            var ebxFile = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("file", StringComparison.OrdinalIgnoreCase));
            var ebxCollector = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("collector", StringComparison.OrdinalIgnoreCase));
            var ebxFileCollect1 = AssetManager.Instance.EnumerateEbx().Where(x => x.FullPath.Contains("collector", StringComparison.OrdinalIgnoreCase));
            var ebxFileCollect2 = AssetManager.Instance.EnumerateEbx("ChunkFileCollector");
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        [TestMethod]
        public void TestLegacyMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\GraphicMod\FIFA 21\expanded customization.fbproject");

            var testR = "test.fbmod";;
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void TestLegacyMod_CM()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\Career Mod\FIFA-21-Career-Mod\Paulv2k4 Career Realism Mod - V2.4.fbproject");

            var testR = "test.fbmod";;
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void TestLegacyMod_DupEntry()
        {
            DeleteOldTestMods();

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            AssetManager.Instance.DuplicateEntry(
                AssetManager.Instance.GetCustomAssetEntry("legacy", "data/ui/imgAssets/heads/p102064.dds")
                , "data/ui/imgAssets/heads/p258487.dds"
                );

            var testR = "test.fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void TestDuplicateEntry()
        {
            DeleteOldTestMods();

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            AssetManager.Instance.DuplicateEntry(
                AssetManager.Instance.GetEbxEntry("content/character/kit/kit_0/aston_villa_2/home_0_0/jersey_2_0_0_color")
                , "content/character/kit/kit_0/aston_villa_999999/home_0_0/jersey_999999_0_0_color"
                );

            var testR = "test.fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void TestLegacyMod_PlayerLUA()
        {
            DeleteOldTestMods();

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\Career Mod\FIFA-21-Career-Mod\DynamicSystem\Paulv2k4 FIFA 21 Dynamic Mod - Version 6.fbproject");

            var testModFile = $"test{DateTime.Now.Ticks}.fbmod";
            projectManagement.Project.WriteToMod(testModFile, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        /// <summary>
        /// This tests a more complex "compressed" asset
        /// </summary>
        [TestMethod]
        public void TestLegacyMod_CompressedAsset()
        {
            DeleteOldTestMods();

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();

            var ca = AssetManager.Instance.GetCustomAsset("legacy", AssetManager.Instance.GetCustomAssetEntry("legacy", @"dlc/dlc_FootballCompEng/dlc/FootballCompEng/data/Finance/ProfCWRelation.csv"));
            byte[] data = ((MemoryStream)ca).ToArray();


            AssetManager.Instance.ModifyLegacyAsset(@"dlc/dlc_FootballCompEng/dlc/FootballCompEng/data/Finance/ProfCWRelation.csv"
                , new NativeReader(new FileStream(@"G:\Work\FIFA Modding\Career Mod\FIFA-21-Career-Mod\Source lmod\dlc\dlc_FootballCompEng\dlc\FootballCompEng\data\Finance\ProfCWRelation.csv", FileMode.Open)).ReadToEnd()
                //, data
                , false
                );

            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        /// <summary>
        /// This tests a more complex "compressed" asset
        /// </summary>
        [TestMethod]
        public void TestLegacyMod_PlayerValues()
        {
            DeleteOldTestMods();

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();

            var ca = AssetManager.Instance.GetCustomAsset("legacy", AssetManager.Instance.GetCustomAssetEntry("legacy", @"dlc/dlc_FootballCompEng/dlc/FootballCompEng/data/playervalues.ini"));
            byte[] data = ((MemoryStream)ca).ToArray();

            AssetManager.Instance.ModifyLegacyAsset(@"dlc/dlc_FootballCompEng/dlc/FootballCompEng/data/playervalues.ini"
                , new NativeReader(new FileStream(@"G:\Work\FIFA Modding\Career Mod\FIFA-21-Career-Mod\Source lmod\dlc\dlc_FootballCompEng\dlc\FootballCompEng\data\playervalues.ini", FileMode.Open)).ReadToEnd()
                //, data
                , false
                );

            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        /// <summary>
        /// This tests a more complex "compressed" asset
        /// </summary>
        [TestMethod]
        public void TestLegacyMod_PlayerValues_999m()
        {
            DeleteOldTestMods();

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();

            var ca = AssetManager.Instance.GetCustomAsset("legacy", AssetManager.Instance.GetCustomAssetEntry("legacy", @"dlc/dlc_FootballCompEng/dlc/FootballCompEng/data/playervalues.ini"));
            byte[] data = ((MemoryStream)ca).ToArray();

            AssetManager.Instance.ModifyLegacyAsset(@"dlc/dlc_FootballCompEng/dlc/FootballCompEng/data/playervalues.ini"
                , new NativeReader(new FileStream(@"G:\Work\FIFA Modding\Career Mod\FIFA-21-Career-Mod\Source lmod\dlc\dlc_FootballCompEng\dlc\FootballCompEng\data\playervalues_999.ini", FileMode.Open)).ReadToEnd()
                //, data
                , false
                );

            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }


        /// <summary>
        /// This tests a more complex "compressed" asset
        /// </summary>
        [TestMethod]
        public void TestLegacyModRewrite()
        {
            DeleteOldTestMods();

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostbiteProject();

            ChunkFileManager2022 fileManager = new ChunkFileManager2022();
            fileManager.WriteAllLegacy();

            projectManagement.Project.WriteToMod("test.fbmod", new ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        public void DeleteOldTestMods()
        {
            foreach (var s in Directory.GetFiles("/", "*.fbmod"))
            {
                File.Delete(s);
            }
        }


        [TestMethod]
        public void LoadUpDBAndExportTable()
        {
            //DeleteOldTestMods();

            //ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            //projectManagement.Project = new FrostySdk.FrostbiteProject();


            //var DBAssetEntry = AssetManager.Instance.GetCustomAssetEntry("legacy", "data/db/fifa_ng_db.db") as LegacyFileEntry;
            //var DBAssetStream = AssetManager.Instance.GetCustomAsset("legacy", DBAssetEntry);
            //if (DBAssetStream == null)
            //    return;

            //var dbMeta = AssetManager.Instance.GetCustomAssetEntry("legacy", "data/db/fifa_ng_db-meta.xml") as LegacyFileEntry;
            //var DBMetaAssetStream = AssetManager.Instance.GetCustomAsset("legacy", dbMeta);

            //if (DBMetaAssetStream == null)
            //    return;

            //var DB = new DbFile(DBAssetStream, DBMetaAssetStream);

            //var CF = new CareerFile(DBAssetStream, DBMetaAssetStream);

            //var tbl = DB.GetTable("players");
            //var tbl2 = CF.Databases[0].GetTable("players");

            //tbl.ConvertToDataTable().ToCSV("testDumpPlayers.csv");

        }

        [TestMethod]
        public void MergeCSVIntoDBTable()
        {
            //DeleteOldTestMods();

            //ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            //projectManagement.Project = new FrostySdk.FrostbiteProject();


            //var DBAssetEntry = AssetManager.Instance.GetCustomAssetEntry("legacy", "data/db/fifa_ng_db.db") as LegacyFileEntry;
            //var DBAssetStream = AssetManager.Instance.GetCustomAsset("legacy", DBAssetEntry);
            //if (DBAssetStream == null)
            //    return;

            //var dbMeta = AssetManager.Instance.GetCustomAssetEntry("legacy", "data/db/fifa_ng_db-meta.xml") as LegacyFileEntry;
            //var DBMetaAssetStream = AssetManager.Instance.GetCustomAsset("legacy", dbMeta);

            //if (DBMetaAssetStream == null)
            //    return;

            //var DB = new DbFile(DBAssetStream, DBMetaAssetStream);

            //var tbl = DB.GetTable("players");

            //DataTable dtInitial = tbl.ConvertToDataTable();
            //DataTable dtMerger = new DataTable();
            //dtMerger.FromCSV(@"C:\Users\paula\Documents\FM_temp\players_f22.csv").Clone();
            //dtMerger.TableName = dtInitial.TableName;

            //List<int> FoundPK = new List<int>();
            //List<int> NotFoundPK = new List<int>();
            //List<int> PKInserted = new List<int>();

            //Stopwatch stopwatchMerger = new Stopwatch();
            //stopwatchMerger.Start();


            //var primaryKey = "playerid";
            //for (var iInit = 0; iInit < dtInitial.Rows.Count; iInit++)
            //{
            //    bool found = false;
            //    var initRow = dtInitial.Rows[iInit];
            //    var pKey = (int)initRow[primaryKey];
            //    for (var iMerg = 0; iMerg < dtMerger.Rows.Count; iMerg++)
            //    {
            //        found = false;
            //        var mergeRow = dtMerger.Rows[iMerg];
            //        var pKeyMerger = int.Parse(mergeRow[primaryKey].ToString());
            //        if(pKeyMerger == pKey)
            //        {
            //            FoundPK.Add(pKey);
            //            found = true;
            //            for (var iCol = 0; iCol < initRow.Table.Columns.Count; iCol++)
            //            {
            //                for(var jCol = 0; jCol < mergeRow.Table.Columns.Count; jCol++)
            //                {
            //                    if(initRow.Table.Columns[iCol].ColumnName == mergeRow.Table.Columns[jCol].ColumnName)
            //                    {
            //                        initRow[iCol] = mergeRow[jCol];
            //                        break;
            //                    }
            //                }
            //            }
            //            break;
            //        }
            //    }

            //    if (!found)
            //    {
            //        NotFoundPK.Add(pKey);
            //    }



            //}

            //for (var iMerg = 0; iMerg < dtMerger.Rows.Count; iMerg++)
            //{
            //    var mergeRow = dtMerger.Rows[iMerg];
            //    var pKeyMerger = int.Parse(mergeRow[primaryKey].ToString());
            //    if (!FoundPK.Contains(pKeyMerger) && !NotFoundPK.Contains(pKeyMerger))
            //    {
            //        PKInserted.Add(pKeyMerger);
            //    }

            //}

            //stopwatchMerger.Stop();
            //Log($"Merge Took: {stopwatchMerger.Elapsed.ToString(@"mm\:ss")}");
            //Log($"Records Updated: {FoundPK.Count}");
            //Log($"Records Ignored: {NotFoundPK.Count}");
            //Log($"Records Inserted: {PKInserted.Count}");

        }
    }
}
