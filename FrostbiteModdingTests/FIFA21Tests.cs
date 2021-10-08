using FIFAModdingUI;
using FrostbiteSdk.Import;
using FrostySdk.Frostbite;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.Frosty.FET;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using v2k4FIFAModding.Frosty;
using v2k4FIFAModdingCL;
using SdkGenerator;
using FrostySdk.FrostySdk.Resources.Mesh2;
using FrostySdk;
using FrostbiteSdk.Frostbite.FileManagers;
using Frostbite.FileManagers;
using FifaLibrary;
using System.Data;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class FIFA21Tests : ILogger
    {
        private string prevText = string.Empty;

        public const string GamePath = @"F:\Origin Games\FIFA 21";
        public const string GamePathEXE = @"F:\Origin Games\FIFA 21\FIFA21.exe";
        public void Log(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine("[LOGGER] [DEBUG] " + text);
                prevText = text;
            }
        }

        public void LogError(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine("[LOGGER] [ERROR] " + text);
                prevText = text;
            }
        }

        public void LogWarning(string text, params object[] vars)
        {
            if (prevText != text)
            {
                Debug.WriteLine("[LOGGER] [WARNING] " + text);
                prevText = text;
            }
        }

        [TestMethod]
        public void TestBuildCache()
        {
            var buildCache = new BuildCache();
            //buildCache.LoadData("FIFA21", @"E:\Origin Games\FIFA 21", this, false);
            buildCache.LoadData("FIFA21", GamePath, this, true, true);
        }

        [TestMethod]
        public void TestBuildCacheIndexing()
        {
            var buildCache = new BuildCache();
            //buildCache.LoadData("FIFA21", @"E:\Origin Games\FIFA 21", this, false);
            buildCache.LoadData("FIFA21", GamePath, this, false);
            //AssetManager.Instance.ebxGuidList.Clear();
            AssetManager.Instance.DoEbxIndexing();
        }

        [TestMethod]
        public void TestBuildSDK()
        {
            var buildCache = new BuildCache();
            buildCache.LoadData("FIFA21", GamePath, this, false);

            var buildSDK = new BuildSDK();
            buildSDK.Build().Wait();
        }

        [TestMethod]
        public void TestLiveLegacySystem()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE);
            var legacyModSupportFile = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + @"\FIFA.dll";

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
                    var exporter2 = new MeshSetToFbxExport2();
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
            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    @"test.fbmod"
                }.ToArray()).Wait();

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
                    var bl = new Bleak.Injector(Bleak.InjectionMethod.CreateThread, proc.Value, dllpath, false);
                    //var bl = new Bleak.Injector(proc.Value, dllpath, Bleak.InjectionMethod.CreateThread);
                    bl.InjectDll();
                    Debug.WriteLine($"Injected: {dllpath}");
                }
            }
        }

        [TestMethod]
        public void TestFETSaveFETProjectAndLoad()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\Gameplay mod\FIFA 21\Paulv2k4 FIFA 21 Gameplay Version 3 Final.fbproject");

            FIFAEditorProject editorProject = FIFAEditorProject.ConvertFromFbProject(projectManagement.Project, @"G:\testgpfromfbproject.fifaproject");
            //FIFAEditorProject editorProject = new FIFAEditorProject("FIFA21", AssetManager.Instance, AssetManager.Instance.fs);
            //editorProject.Save(@"G:\testgpfromfbproject.fifaproject");
            //editorProject.Load(@"G:\testgpfromfbproject.fifaproject");


        }

        [TestMethod]
        public void TestFETFIFAMod()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "", 
                new System.Collections.Generic.List<string>() {
                    //@"C:\Users\paula\Downloads\Villalibre Molina.fifamod"
                    //@"E:\Origin Games\FIFA 21\fet gp change.fifamod"
                    //@"G:\Work\FIFA Modding\Gameplay mod\FIFA 21\Paulv2k4 FIFA 21 Gameplay Version 3 Patch 1 FET.fifamod"
                    //@"G:\Work\FIFA Modding\Sky_Sports_Football_TV_Logo_for_the_English_Premier_League.fifamod"
                    //@"G:\Work\FIFA Modding\GraphicMod\FIFA 21\FCB17 Facepack #5 (fix).fifamod"
                    @"G:\Work\FIFA Modding\GraphicMod\FIFA 21\Mason Greenwood by AlieFFR.fifamod"


                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestFETFIFAMod_Legacy()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    @"G:\Work\FIFA Modding\GraphicMod\FIFA 21\ALP 3.7.fifamod"
                }.ToArray()).Wait();

        }

        [TestMethod]
        public void OpenFETEditorProject()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            FIFAEditorProject editorProject = new FIFAEditorProject("FIFA21", AssetManager.Instance, AssetManager.Instance.fs);
            editorProject.Load(@"C:\Users\paula\Downloads\JAYS BOOTPACK.fifaproject");
            var count = AssetManager.Instance.EnumerateEbx(modifiedOnly: true).Count();
            var res = AssetManager.Instance.EnumerateRes(modifiedOnly: true).ToList();
            var count2 = res.Count();
            var count3 = AssetManager.Instance.EnumerateChunks(modifiedOnly: true).Count();


        }

        [TestMethod]
        public void TestGPMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE);
            //ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            //projectManagement.Project = new FrostySdk.FrostbiteProject();
            //projectManagement.Project.Load(@"G:\Work\FIFA Modding\Gameplay mod\FIFA 21\Paulv2k4 FIFA 21 Gameplay Version 2 Alpha 12.fbproject");
            //projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());
            
            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    @"G:\Work\FIFA Modding\Gameplay mod\FIFA 21\Paulv2k4 FIFA 21 Gameplay Version 2 Alpha 12.fbmod"
                    //"test.fbmod"
                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestArsenalKitMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\GraphicMod\FIFA 21\Arsenal Kit 21-22.fbproject");
            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testR
                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestLegacyTextureImport()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            AssetManager.Instance.DoLegacyImageImport(@"G:\Work\FIFA Modding\Career Mod\st_bootflow_v2k4.png"
                , (LegacyFileEntry)AssetManager.Instance.EnumerateCustomAssets("legacy")
                .FirstOrDefault(x=>x.Name.Contains("st_bootflow", StringComparison.OrdinalIgnoreCase)));

            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testR
                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestLegacyMod()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\GraphicMod\FIFA 21\expanded customization.fbproject");

            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testR
                }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestLegacyMod_CM()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            //projectManagement.Project.Load(@"G:\Work\FIFA Modding\Career Mod\Paulv2k4 Career Realism Mod - V2 Alpha 5.fbproject");
            //projectManagement.Project.Load(@"G:\Work\FIFA Modding\Career Mod\Paulv2k4 Career Realism Mod - V2 Alpha 6.fbproject");
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\Career Mod\Paulv2k4 Career Realism Mod - V2 Alpha 12.fbproject");

            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testR
                }.ToArray()).Wait();

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
                , true);

            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testR
                }.ToArray()).Wait();

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
                , false);

            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testR
                }.ToArray()).Wait();

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

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    testModFile
                }.ToArray()).Wait();

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

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    "test.fbmod"
                }.ToArray()).Wait();

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

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    "test.fbmod"
                }.ToArray()).Wait();

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

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    "test.fbmod"
                }.ToArray()).Wait();

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

            LegacyFileManager_FMTV2 fileManager = new LegacyFileManager_FMTV2();
            fileManager.WriteAllLegacy();

            projectManagement.Project.WriteToMod("test.fbmod", new ModSettings());

            paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
                new System.Collections.Generic.List<string>() {
                    "test.fbmod"
                }.ToArray()).Wait();

        }

            public void DeleteOldTestMods()
        {
            foreach(var s in Directory.GetFiles("/", "*.fbmod"))
            {
                File.Delete(s);
            }
        }


        [TestMethod]
        public void LoadUpDBAndExportTable()
        {
            DeleteOldTestMods();

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();


            var DBAssetEntry = AssetManager.Instance.GetCustomAssetEntry("legacy", "data/db/fifa_ng_db.db") as LegacyFileEntry;
            var DBAssetStream = AssetManager.Instance.GetCustomAsset("legacy", DBAssetEntry);
            if (DBAssetStream == null)
                return;

            var dbMeta = AssetManager.Instance.GetCustomAssetEntry("legacy", "data/db/fifa_ng_db-meta.xml") as LegacyFileEntry;
            var DBMetaAssetStream = AssetManager.Instance.GetCustomAsset("legacy", dbMeta);

            if (DBMetaAssetStream == null)
                return;

            var DB = new DbFile(DBAssetStream, DBMetaAssetStream);

            var CF = new CareerFile(DBAssetStream, DBMetaAssetStream);

            var tbl = DB.GetTable("players");
            var tbl2 = CF.Databases[0].GetTable("players");

            tbl.ConvertToDataTable().ToCSV("testDumpPlayers.csv");

        }

        [TestMethod]
        public void MergeCSVIntoDBTable()
        {
            DeleteOldTestMods();

            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();


            var DBAssetEntry = AssetManager.Instance.GetCustomAssetEntry("legacy", "data/db/fifa_ng_db.db") as LegacyFileEntry;
            var DBAssetStream = AssetManager.Instance.GetCustomAsset("legacy", DBAssetEntry);
            if (DBAssetStream == null)
                return;

            var dbMeta = AssetManager.Instance.GetCustomAssetEntry("legacy", "data/db/fifa_ng_db-meta.xml") as LegacyFileEntry;
            var DBMetaAssetStream = AssetManager.Instance.GetCustomAsset("legacy", dbMeta);

            if (DBMetaAssetStream == null)
                return;

            var DB = new DbFile(DBAssetStream, DBMetaAssetStream);

            var tbl = DB.GetTable("players");

            DataTable dtInitial = tbl.ConvertToDataTable();
            DataTable dtMerger = new DataTable();
            dtMerger.FromCSV(@"C:\Users\paula\Documents\FM_temp\players_f22.csv").Clone();
            dtMerger.TableName = dtInitial.TableName;

            List<int> FoundPK = new List<int>();
            List<int> NotFoundPK = new List<int>();
            List<int> PKInserted = new List<int>();

            Stopwatch stopwatchMerger = new Stopwatch();
            stopwatchMerger.Start();


            var primaryKey = "playerid";
            for (var iInit = 0; iInit < dtInitial.Rows.Count; iInit++)
            {
                bool found = false;
                var initRow = dtInitial.Rows[iInit];
                var pKey = (int)initRow[primaryKey];
                for (var iMerg = 0; iMerg < dtMerger.Rows.Count; iMerg++)
                {
                    found = false;
                    var mergeRow = dtMerger.Rows[iMerg];
                    var pKeyMerger = int.Parse(mergeRow[primaryKey].ToString());
                    if(pKeyMerger == pKey)
                    {
                        FoundPK.Add(pKey);
                        found = true;
                        for (var iCol = 0; iCol < initRow.Table.Columns.Count; iCol++)
                        {
                            for(var jCol = 0; jCol < mergeRow.Table.Columns.Count; jCol++)
                            {
                                if(initRow.Table.Columns[iCol].ColumnName == mergeRow.Table.Columns[jCol].ColumnName)
                                {
                                    initRow[iCol] = mergeRow[jCol];
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }

                if (!found)
                {
                    NotFoundPK.Add(pKey);
                }


                
            }

            for (var iMerg = 0; iMerg < dtMerger.Rows.Count; iMerg++)
            {
                var mergeRow = dtMerger.Rows[iMerg];
                var pKeyMerger = int.Parse(mergeRow[primaryKey].ToString());
                if (!FoundPK.Contains(pKeyMerger) && !NotFoundPK.Contains(pKeyMerger))
                {
                    PKInserted.Add(pKeyMerger);
                }

            }

            stopwatchMerger.Stop();
            Log($"Merge Took: {stopwatchMerger.Elapsed.ToString(@"mm\:ss")}");
            Log($"Records Updated: {FoundPK.Count}");
            Log($"Records Ignored: {NotFoundPK.Count}");
            Log($"Records Inserted: {PKInserted.Count}");

        }
    }
}
