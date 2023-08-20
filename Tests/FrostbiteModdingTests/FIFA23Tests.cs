using FifaLibrary;
using FMT.Logging;
using FrostbiteModdingUI.CEM;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.ModsAndProjects.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using ProcessMemoryUtilities.Managed;
using SdkGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using v2k4FIFAModding.Frosty;
using v2k4FIFAModdingCL;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class Fifa23Tests : ILogger
    {
        private string prevText = string.Empty;

        public string GamePath
        {
            get
            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\EA Sports\\FIFA 23"))
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

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\EA Sports\\FIFA 23"))
                {
                    if (key != null)
                    {
                        string installDir = key.GetValue("Install Dir").ToString();
                        return installDir + "FIFA23.exe";
                    }
                }
                return string.Empty;
            }
        }

        public string TestMeshesPath
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }

        private Dictionary<string, Stream> _testProjects;

        internal Dictionary<string, Stream> TestProjects
        {
            get
            {
                if (_testProjects != null)
                    return _testProjects;

                _testProjects = FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourcesByName(new string[] { "FIFA23.", ".fbproject" });
                return _testProjects;
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
        public void ReadDataInCareerFile()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE);
            Directory.GetFiles("C:\\Users\\paula\\Documents\\FIFA 23\\settings\\", "Career*")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            var cem = new CEMCore2("FIFA23");

            Stopwatch sw1 = new Stopwatch();
            sw1.Start();
            var stats = cem.GetPlayerStatsAsync().Result;
            sw1.Stop();
            Debug.WriteLine($"Time taken to get stats : {sw1.Elapsed}");
            var statsDoncaster = cem.GetPlayerStatsAsync(142).Result;
        }

        [TestMethod]
        public void EditCareerFile()
        {

            var originalFile = "C:\\Users\\paula\\Documents\\FIFA 23\\settings\\Career20220930120547";
            var newFileFile = "C:\\Users\\paula\\Documents\\FIFA 23\\settings\\Career20220930102999";
            //CareerFile careerFile = new CareerFile(
            //    new FileStream(originalFile, FileMode.Open)
            //    , new FileStream("C:\\RDBM 22\\Templates\\FIFA 23\\fifa_ng_db-meta.XML", FileMode.Open)
            //    );
            CareerFile careerFile = new CareerFile(
                originalFile
                , "C:\\RDBM 22\\Templates\\FIFA 23\\fifa_ng_db-meta.XML"
                );
            File.Copy(originalFile, newFileFile, true);
            careerFile.InGameName = "testtestestestest";

            careerFile.SaveEa(newFileFile);
        }

        [TestMethod]
        public void BuildCache()
        {
            var buildCache = new CacheManager();
            buildCache.Load(GamePathEXE, this, true, true);
            //buildCache.LoadData("FIFA23", GamePath, this, true, true);

            var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            var resItems = AssetManager.Instance.EnumerateRes().ToList();
            var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        [TestMethod]
        public void LoadCacheAndShutdown()
        {
            GCSettings.LatencyMode = GCLatencyMode.Batch;

            var buildCache = new CacheManager();
            buildCache.Load(GamePathEXE, this, true, false);

            //var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            //var resItems = AssetManager.Instance.EnumerateRes().ToList();
            //var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            //var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();

            AssetManager.Instance.Dispose();
            //AssetManager.Instance = null;
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if(true)
            {

            }

        }


        [TestMethod]
        public void BuildCacheIndexing()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("FIFA23", GamePath, this, false, true);
            AssetManager.Instance.DoEbxIndexing();
        }

        [TestMethod]
        public void TestFindProcesses()
        {
            Process.GetProcesses().Where(x => x.ProcessName.Contains("fifa", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x =>
              {
                  var prThread = x.Threads[1];
                  byte[] buffer = new byte[1024];
                  var v = NativeWrapper.ReadProcessMemoryArray<byte>(x.Handle, prThread.StartAddress, buffer);
                  //prThread.
              });
        }

        [TestMethod]
        public void BuildSDK()
        {
            //var buildCache = new BuildCache();
            //buildCache.LoadData("FIFA23", GamePath, this, false, false);

            //GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, false);
            var buildSDK = new BuildSDK();
            buildSDK.Build().Wait();

            //var ebxItems = AssetManager.Instance.EnumerateEbx().ToList();
            //var resItems = AssetManager.Instance.EnumerateRes().ToList();
            //var chunkItems = AssetManager.Instance.EnumerateChunks().ToList();
            //var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        [TestMethod]
        public void BuildSDKFromEXE()
        {
            var buildSDK = new BuildSDKFromEXE();
            buildSDK.Build(GamePathEXE);
        }

        [TestMethod]
        public void ReadSharedTypeDescriptor()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("FIFA23", GamePath, this, false, false);
            EbxSharedTypeDescriptorV2 std = new EbxSharedTypeDescriptorV2("SharedTypeDescriptors.ebx", false);
            EbxSharedTypeDescriptorV2 patchstd = new EbxSharedTypeDescriptorV2("SharedTypeDescriptors_patch.ebx", false);
        }

        [TestMethod]
        public void ReadSimpleGPFile()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var simpleEbxEntry = AssetManager.Instance.GetEbxEntry("fifa/attribulator/gameplay/groups/gp_actor/gp_actor_facialanim_runtime");
            Assert.IsNotNull(simpleEbxEntry);
            var simpleAsset = AssetManager.Instance.GetEbx(simpleEbxEntry);
            if (simpleAsset != null)
            {

            }
        }

        [TestMethod]
        public void ReadHotspotFile()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var ebxEntry = AssetManager.Instance.GetEbxEntry("content/character/kit/kit_0/_default_0/home_0_0/hotspots_0_0_0");
            Assert.IsNotNull(ebxEntry);
            var complexAsset = AssetManager.Instance.GetEbx(ebxEntry);
        }

        [TestMethod]
        public void ModHotspotFile()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var ebxEntry = AssetManager.Instance.GetEbxEntry("content/character/kit/kit_112000/newport_county_112254/home_0_0/hotspots_112254_0_0");
            Assert.IsNotNull(ebxEntry);
            var complexAsset = AssetManager.Instance.GetEbx(ebxEntry);
            ((dynamic)complexAsset.RootObject).Hotspots[0].Bounds.x = -1f;
            ((dynamic)complexAsset.RootObject).Hotspots[0].Rotation = 0.5f;
            ((dynamic)complexAsset.RootObject).Hotspots[1].Rotation = 0.5f;
            ((dynamic)complexAsset.RootObject).Hotspots[2].Rotation = 0.5f;
            ((dynamic)complexAsset.RootObject).Hotspots[3].Rotation = 0.5f;
            ((dynamic)complexAsset.RootObject).Hotspots[4].Rotation = 0.5f;
            AssetManager.Instance.ModifyEbx("content/character/kit/kit_112000/newport_county_112254/home_0_0/hotspots_112254_0_0", complexAsset);

            var testR = "test.fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        testR
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void ReadComplexGPFile()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();


            var ebxEntry = AssetManager.Instance.GetEbxEntry("fifa/attribulator/gameplay/groups/gp_actor/gp_actor_movement_runtime");
            Assert.IsNotNull(ebxEntry);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var complexAsset = AssetManager.Instance.GetEbx(ebxEntry);
            sw.Stop();
            Debug.WriteLine($"{sw.Elapsed}");
        }

        [TestMethod]
        public void ModComplexGPFile()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var ebxEntry = AssetManager.Instance.GetEbxEntry("fifa/attribulator/gameplay/groups/gp_actor/gp_actor_movement_runtime");
            Assert.IsNotNull(ebxEntry);
            var complexAsset = AssetManager.Instance.GetEbx(ebxEntry);
            var dyn = (dynamic)complexAsset.RootObject;
            dyn.ATTR_DribbleJogSpeed = 0.05f;
            dyn.ATTR_DribbleWalkSpeed = 0.05f;
            //dyn.ATTR_JogSpeed = 0.005f;
            //dyn.ATTR_WalkSpeed = 0.005f;
            //dyn.ATTR_DribbleJogSpeed = 0.9f;
            //dyn.ATTR_DribbleWalkSpeed = 0.9f;
            //dyn.ATTR_JogSpeed = 0.9f;
            //dyn.ATTR_WalkSpeed = 0.9f;
            AssetManager.Instance.ModifyEbx("fifa/attribulator/gameplay/groups/gp_actor/gp_actor_movement_runtime", complexAsset);

            var testR = "test.fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        testR
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void ModComplexGPFile2()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var ebxEntry = AssetManager.Instance.GetEbxEntry("fifa/attribulator/gameplay/groups/gp_kickerror/gp_kickerror_passshotcontexteffectshotdriven_runtime");
            Assert.IsNotNull(ebxEntry);
            var complexAsset = AssetManager.Instance.GetEbx(ebxEntry);
            var dyn = (dynamic)complexAsset.RootObject;
            dyn.PASSSHOT_CONTEXTEFFECT_Animation_Lace_Difficulty = 100.0f;
            dyn.PASSSHOT_CONTEXTEFFECT_MissRateVsAttribute.Internal.Points[0].Y = 100.0f;
            dyn.PASSSHOT_CONTEXTEFFECT_MissRateVsAttribute.Internal.Points[1].Y = 100.0f;
            AssetManager.Instance.ModifyEbx("fifa/attribulator/gameplay/groups/gp_kickerror/gp_kickerror_passshotcontexteffectshotdriven_runtime", complexAsset);

            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            ModdingSupport.ModExecutor.UseModData = false;
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        testR
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void ModGPPhysics()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var ebxEntry = AssetManager.Instance.GetEbxEntry("Fifa/Attribulator/Gameplay/groups/gp_physics/gp_physics_airflow_runtime");
            Assert.IsNotNull(ebxEntry);
            var complexAsset = AssetManager.Instance.GetEbx(ebxEntry);
            var dyn = (dynamic)complexAsset.RootObject;
            dyn.Airflow_DragMultiplier = 100;
            AssetManager.Instance.ModifyEbx("Fifa/Attribulator/Gameplay/groups/gp_physics/gp_physics_airflow_runtime", complexAsset);

            var testR = "test.fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            ModdingSupport.ModExecutor.UseModData = true;
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        testR
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();


        }

        [TestMethod]
        public void ModCamera()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var ebxEntry = AssetManager.Instance.GetEbxEntry("broadcasting/attribsys/broadcasting/gameplay_camera_settings/gameplay_camera_settings_default");
            Assert.IsNotNull(ebxEntry);
            var complexAsset = AssetManager.Instance.GetEbx(ebxEntry);
            var dyn = (dynamic)complexAsset.RootObject;
            AssetManager.Instance.ModifyEbx("broadcasting/attribsys/broadcasting/gameplay_camera_settings/gameplay_camera_settings_default", complexAsset);

            var testR = "test.fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            ModdingSupport.ModExecutor.UseModData = true;
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        testR
            //    }.ToArray()).Wait();

            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestAvatarExpansionMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\FIFA 23 Avatar Customization Mod.fbproject");

            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        "test.fbmod"
            //    }.ToArray()).Wait();

            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

            var allLegacy = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        [TestMethod]
        public async Task TestCareerMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = FMTProject.Read(@"G:\Work\FIFA Modding\Career Mod\FIFA-23-Career-Mod\V Career Mod - Version 2a4.fmtproj");
            projectManagement.Project.WriteToMod("test.fbmod", projectManagement.Project.ModSettings);
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            await frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray());
        }

        [TestMethod]
        public void TestGPModProject()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            var projectResult = projectManagement.Project.LoadAsync(@"G:\Work\FIFA Modding\Gameplay mod\FIFA 23\V6\V Gameplay Mod - v6a5.fmtproj").Result;

            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        "test.fbmod"
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestGPMod()
        {
            var modPath = @"G:\Work\FIFA Modding\Gameplay mod\FIFA 23\V7\V Gameplay Mod - v7a3.fbmod";
            //GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, logger: this);

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            var r = frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath,
                new System.Collections.Generic.List<string>() {
                    modPath
                }.ToArray()).Result;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestCombinationMod()
        {
            var modPath2 = @"G:\Work\FIFA Modding\GraphicMod\FIFA 23\V FIFA 23 Licensing Mod v0.2.fbmod";
            var modPath3 = @"G:\Work\FIFA Modding\Career Mod\FIFA-23-Career-Mod\V Career Mod - Version 2a4.fbmod";
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        modPath1, modPath2, modPath3
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { modPath2, modPath3 }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestLargeLegacyMod_FIFAMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath
                , new List<string>() { "G:\\Work\\FIFA Modding\\GraphicMod\\FIFA 23\\EEP23_MainMod_1_0_2_TU13\\EEP_MainMod_1_0_2_TU13.fifamod"

                }.ToArray()).Wait();

        }


        [TestMethod]
        public void TestLargeLegacyModProject()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            var projectResult = projectManagement.Project.LoadAsync(@"G:\Work\FIFA Modding\GraphicMod\FIFA 23\V FIFA 23 Licensing Mod.fbproject").Result;

            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        "test.fbmod"
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        public void DeleteAllBackupsInFolder(string dir)
        {
            int countOfDelete = 0;
            foreach (var tFile in Directory.EnumerateFiles(dir, "*.bak", new EnumerationOptions() { RecurseSubdirectories = true }))
            {
                File.Delete(tFile);
                countOfDelete++;
            }

            Debug.WriteLine($"I have deleted {countOfDelete} *.bak files");
            //foreach(var childDir in Directory.EnumerateDirectories(dir))
            //{
            //    DeleteAllBackupsInFolder(childDir);
            //}
        }

        [TestMethod]
        public void DeleteAllBackups()
        {
            DeleteAllBackupsInFolder(GamePath);
        }

        [TestMethod]
        public void ModEbxFromJson()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var ebxEntry = AssetManager.Instance.GetEbxEntry("fifa/attribulator/gameplay/groups/gp_actor/gp_actor_movement_runtime");
            Assert.IsNotNull(ebxEntry);
            var complexAsset = AssetManager.Instance.GetEbx(ebxEntry);
            AssetManager.Instance.ModifyEbxJson("fifa/attribulator/gameplay/groups/gp_actor/gp_actor_movement_runtime"
                , File.ReadAllText(@"G:\Work\FIFA Modding\Gameplay mod\FIFA 21\gp_actor_movement_runtime.json"));

            //var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            //projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            //paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
            //paulv2k4ModdingExecuter.FrostyModExecutor.UseModData = false;
            //frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        testR
            //    }.ToArray()).Wait();


        }

        [TestMethod]
        public void TestArsenalKitMod()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\GraphicMod\FIFA 22\test kit project.fbproject");

            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            ModdingSupport.ModExecutor.UseModData = true;
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        testR
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestSplashscreenMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\FIFA 23 Splashscreen Loaded.fmtproj");

            var testR = "test.fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            ModdingSupport.ModExecutor.UseModData = false;
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }


        [TestMethod]
        public void LoadSplashscreenTexture()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("FIFA23", GamePath, this, false, true);
            var ebxEntrySplash = AssetManager.Instance.GetEbxEntry("fifa/fesplash/splashscreen/splashscreen");
            var ebxSplash = AssetManager.Instance.GetEbx(ebxEntrySplash);

        }

        [TestMethod]
        public void LoadInitfs()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("FIFA23", GamePath, this, false, true);

        }

        [TestMethod]
        public void LoadInitfsWriteInitfs()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("FIFA23", GamePath, this, false, true);
            var initfs = FileSystem.Instance.ReadInitfs(FileSystem.Instance.LoadKey());
            using (FileStream fsTestInitfs = new FileStream("initfsTest.dat", FileMode.OpenOrCreate))
            {
                FileSystem.Instance.WriteInitfs(fsTestInitfs);
            }
        }

        [TestMethod]
        public void LoadLocaleIni()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            var s = Encoding.UTF8.GetString(FileSystem.Instance.ReadLocaleIni());
            var s2 = Encoding.ASCII.GetString(FileSystem.Instance.ReadLocaleIni());
        }

        [TestMethod]
        public void ReadWriteLocaleIni()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            var localeBytes = FileSystem.Instance.ReadLocaleIni();
            var s = Encoding.UTF8.GetString(localeBytes);

            var skipBootflowText = new StringBuilder("");
            skipBootflowText.AppendLine("");
            skipBootflowText.AppendLine("[]");
            skipBootflowText.AppendLine("SKIP_BOOTFLOW=1");

            if (!s.Contains("SKIP_BOOTFLOW=1", StringComparison.OrdinalIgnoreCase))
                s += skipBootflowText;

            var enData = FileSystem.Instance.WriteLocaleIni(Encoding.UTF8.GetBytes(s));

            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            ModdingSupport.ModExecutor.UseModData = false;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>()
            //    {
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void LoadLegacy()
        {
            var buildCache = new CacheManager();
            buildCache.LoadData("FIFA23", GamePath, this, false, true);

            var ebxFCC = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("legacy", StringComparison.OrdinalIgnoreCase));
            var ebxFile = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("file", StringComparison.OrdinalIgnoreCase));
            var ebxCollector = AssetManager.Instance.EBX.Keys.Where(x => x.Contains("collector", StringComparison.OrdinalIgnoreCase));
            var legacyItems = AssetManager.Instance.EnumerateCustomAssets("legacy").ToList();
        }

        [TestMethod]
        public void LaunchVanillaFromModData()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        testR
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void LaunchVanillaFromNormalFS()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();

            var testR = "test-" + new Random().Next().ToString() + ".fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            ModdingSupport.ModExecutor.UseModData = false;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        testR
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();
        }

        [TestMethod]
        public void LaunchCareerMod()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.StartNewProject();
            projectManagement.Project.Load(@"G:\Work\FIFA Modding\Career Mod\FIFA-22-Career-Mod\Paulv2k4 FIFA 22 Career Mod - Alpha 2.fbproject");

            var testR = "test.fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            ModdingSupport.ModExecutor.UseModData = false;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        testR
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void ExportFaceMesh()
        {
            //GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            //ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            //var project = projectManagement.StartNewProject();
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this);
            //var skinnedMeshEntry = AssetManager.Instance.EnumerateEbx("SkinnedMeshAsset").Where(x => x.Name.ToLower().Contains("head_201942_0_0_mesh")).FirstOrDefault();
            //var skinnedMeshEntry = AssetManager.Instance.EnumerateEbx().Where(x => x.Name.ToLower().Contains("head_201942_0_0_mesh")).FirstOrDefault();
            var skinnedMeshEntry = AssetManager.Instance.EnumerateEbx().Where(x => x.Name.ToLower().Contains("haircap_229037_0_0_mesh")).FirstOrDefault();
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
        public void ImportFaceMesh()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            var project = projectManagement.StartNewProject();
            ////var skinnedMeshEntry = project.AssetManager.EnumerateEbx("SkinnedMeshAsset").Where(x => x.Name.ToLower().Contains("head_192563_0_0_mesh")).FirstOrDefault();
            //var overwriteSkinnedMeshEntry = project.AssetManager.EnumerateEbx().Where(x => x.Name.ToLower().Contains("head_192563_0_0_mesh")).FirstOrDefault();
            //var skinnedMeshEntry = project.AssetManager.EnumerateEbx().Where(x => x.Name.ToLower().Contains("head_250_0_0_mesh")).FirstOrDefault();
            //var overwriteSkinnedMeshEbx = project.AssetManager.GetEbx(overwriteSkinnedMeshEntry);
            //var skinnedMeshEbx = project.AssetManager.GetEbx(skinnedMeshEntry);
            //var resentry = project.AssetManager.GetResEntry(skinnedMeshEntry.Name);
            //var res = project.AssetManager.GetRes(resentry);
            //MeshSet meshSet = new MeshSet(res);

            //FrostySdk.Frostbite.IO.Input.FBXImporter importer = new FrostySdk.Frostbite.IO.Input.FBXImporter();
            //var exporter = new MeshSetToFbxExport();
            //exporter.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, $"{TestMeshesPath}\\head_250_0_0_mesh.fbx", "FBX_2012", "Meters", true, "content/character/rig/skeleton/player/skeleton_player", "*.fbx", meshSet);
            //exporter.Export(AssetManager.Instance, overwriteSkinnedMeshEbx.RootObject, $"{TestMeshesPath}\\head_192563_0_0_mesh.fbx", "FBX_2012", "Meters", true, "content/character/rig/skeleton/player/skeleton_player", "*.fbx", meshSet);

            ////importer.ImportFBX($"{TestMeshesPath}\\head_250_0_0_mesh.fbx", meshSet, overwriteSkinnedMeshEbx, overwriteSkinnedMeshEntry, new FrostySdk.Frostbite.IO.Input.MeshImportSettings()
            //importer.ImportFBX($"{TestMeshesPath}\\head_192563_0_0_mesh.fbx", meshSet, overwriteSkinnedMeshEbx, overwriteSkinnedMeshEntry, new FrostySdk.Frostbite.IO.Input.MeshImportSettings()
            //{
            //    SkeletonAsset = "content/character/rig/skeleton/player/skeleton_player"
            //});

            var overwriteSkinnedMeshEntry = project.AssetManager.EnumerateEbx().Where(x => x.Name.ToLower().Contains("head_192563_0_0_mesh")).FirstOrDefault();
            var overwriteSkinnedMeshEbx = project.AssetManager.GetEbx(overwriteSkinnedMeshEntry);
            var resentry = project.AssetManager.GetResEntry(overwriteSkinnedMeshEntry.Name);
            //var res = project.AssetManager.GetRes(resentry);
            //MeshSet meshSet = new MeshSet(res);
            var exporter = new MeshSetToFbxExport();
            MeshSet meshSet = exporter.LoadMeshSet(overwriteSkinnedMeshEntry);
            exporter.Export(AssetManager.Instance, overwriteSkinnedMeshEbx.RootObject, $"{TestMeshesPath}\\head_192563_0_0_mesh.fbx", "FBX_2012", "Meters", true, "content/character/rig/skeleton/player/skeleton_player", "*.fbx", meshSet);
            FrostySdk.Frostbite.IO.Input.FBXImporter importer = new FrostySdk.Frostbite.IO.Input.FBXImporter();
            importer.ImportFBX($"{TestMeshesPath}\\test.fbx", meshSet, overwriteSkinnedMeshEbx, overwriteSkinnedMeshEntry, new FrostySdk.Frostbite.IO.Input.MeshImportSettings()
            {
                SkeletonAsset = "content/character/rig/skeleton/player/skeleton_player"
            });

            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings() { Title = "Imported mesh test" });
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        //[TestMethod]
        //public void TestImportFaceMeshAndRun()
        //{
        //    var projectManagement = TestImportFaceMesh();
        //    projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());
        //    ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
        //    frostyModExecutor.ForceRebuildOfMods = true;
        //    frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
        //        new System.Collections.Generic.List<string>() {
        //            @"test.fbmod"
        //        }.ToArray()).Wait();

        //}

        //[TestMethod]
        //public void TestDecompressRecompress()
        //{
        //    Oodle.Bind(GamePath);
        //    //var filePath = GamePath + "/data/win32/superbundlelayout/fifa_installpackage_02/cas_01.cas";
        //    var filePath = "F:\\Origin Games\\FIFA 23\\Data\\\\win32\\superbundlelayout\\fifa_installpackage_02\\cas_01.cas";
        //    using (var readerCas = new NativeReader(filePath))
        //    {
        //        var originalSize = 692;
        //        var originalPosition = 4984462;
        //        readerCas.Position = originalPosition;
        //        var originalBytes = readerCas.ReadBytes((int)originalSize);
        //        var decomp = new CasReader(new MemoryStream(originalBytes)).Read();
        //        byte[] recomp = null;
        //        recomp = Utils.CompressFile(decomp, compressionOverride: CompressionType.Oodle, oodleCO: 8);
        //        for (uint i = 4; i < 14; i++)
        //        {
        //            recomp = Utils.CompressFile(decomp, compressionOverride: CompressionType.Oodle, oodleCO: i);
        //            if (recomp.Length <= originalSize)
        //            {

        //            }
        //        }
        //    }
        //}


        [TestMethod]
        public void TestLoadValuesFromLiveTuningUpdate()
        {
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();

            var assets = FileSystem.Instance.LiveTuningUpdate.ReadFIFALiveTuningUpdate();
            var asset = FileSystem.Instance.LiveTuningUpdate.GetLiveTuningUpdateAsset(assets.First().Key);

        }

        /// <summary>
        /// To start without AC you need to alter your Installer.xml
        /// </summary>
        [TestMethod]
        public void TestLoadWithoutAC()
        {
            //ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            //projectManagement.Project = new FrostySdk.FrostbiteProject();

            GameInstanceSingleton.InitializeSingleton(GamePathEXE, false, this);
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            ModdingSupport.ModExecutor.UseACBypass = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() {  }.ToArray()).Wait();

        }

        /// <summary>
        /// To start without AC you need to alter your Installer.xml
        /// </summary>
        //[TestMethod]
        //public void TestLoadWithoutACInModData()
        //{
        //    GameInstanceSingleton.InitializeSingleton(GamePathEXE, false, this);
        //    ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
        //    ModdingSupport.ModExecutor.UseModData = true;
        //    frostyModExecutor.ForceRebuildOfMods = false;
        //    frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
        //        new System.Collections.Generic.List<string>()
        //        {
        //            "C:\\Users\\paula\\Desktop\\V Gameplay Mod - v0.4.fbmod"
        //        }.ToArray()).Wait();
        //    frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        //}

        [TestMethod]
        public void TestLegacyMod_DupEntry()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            AssetManager.Instance.DuplicateEntry(
                AssetManager.Instance.GetCustomAssetEntry("legacy", "data/ui/imgAssets/heads/p192563.dds")
                , "data/ui/imgAssets/heads/p271517.dds"
                );

            projectManagement.Project.WriteToMod("test.fbmod", new FrostySdk.ModSettings());

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        "test.fbmod"
            //    }.ToArray()).Wait();
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void TestFETFIFAMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE);
            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, 
                new System.Collections.Generic.List<string>() {
                    //@"G:\Work\FIFA Modding\GraphicMod\FIFA 23\FIFER Licensing Mod TU4.fifamod"
                    //@"C:\Users\paula\Downloads\eSIM AIO Mod 1.0 - TU#2.fifamod"
                    //@"C:\Users\paula\Downloads\Darwin_Nunez_23.fifamod"
                    //@"C:\Users\paula\Downloads\[FIFA23] Anthem Jackets 22-23 by panda v1.0 TU4.fifamod"
                   @"G:\Work\FIFA Modding\GraphicMod\FIFA 23\FIFA 23 GFX TURF AND PITCH.fifamod"


                }.ToArray()).Wait();
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { @"G:\Work\FIFA Modding\GraphicMod\FIFA 23\FIFER Licensing Mod V2.fifamod" }.ToArray()).Wait();
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { @"G:\Work\FIFA Modding\GraphicMod\FIFA 23\Antony_Matheus_3.0_AlieFFR.fifamod" }.ToArray()).Wait();
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { @"G:\Work\FIFA Modding\GraphicMod\FIFA 23\Enzo_Fernandez_202231_Blonde_AlieFFR.fifamod" }.ToArray()).Wait();

        }

        [TestMethod]
        public void ImportFETFIFAModIntoNewProject()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            FIFAModReader reader = new FIFAModReader(new FileStream(@"G:\Work\FIFA Modding\GraphicMod\FIFA 23\FIFER Licensing Mod V2.fifamod", FileMode.Open));
            projectManagement.Project.Load(reader);
            var r = projectManagement.Project.SaveAsync("test.fbproject", true).Result;
        }


        [TestMethod]
        public void ImportFETFIFAModIntoNewProject2()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            FIFAModReader reader = new FIFAModReader(new FileStream(@"G:\Work\FIFA Modding\GraphicMod\FIFA 23\FIFER Licensing Mod V2.fifamod", FileMode.Open));

            var fmtProj = new FMTProject("test");
            fmtProj.Load(reader);
            fmtProj.Update();
            fmtProj = null;

            fmtProj = FMTProject.Read("test");


        }



        [TestMethod]
        public void LoadUltraSlowGameplayMod()
        {
            GameInstanceSingleton.InitializeSingleton(GamePathEXE, true, this, true);
            ProjectManagement projectManagement = new ProjectManagement(GamePathEXE, this);
            projectManagement.Project = new FrostySdk.FrostbiteProject();
            projectManagement.Project.Load(TestProjects.Single(x => x.Key.EndsWith("FIFA23.UltraSlowDribblingTest.fbproject")).Value);
            var testR = "test.fbmod";
            projectManagement.Project.WriteToMod(testR, new FrostySdk.ModSettings());

            FrostySdk.ModsAndProjects.Projects.FMTProject.Create("FIFA23.UltraSlowDribblingTest");
            FrostySdk.ModsAndProjects.Projects.FMTProject.Read("FIFA23.UltraSlowDribblingTest");
        }

        [TestMethod]
        public void LoadAndLaunchUltraSlowGameplayMod()
        {
            LoadUltraSlowGameplayMod();

            ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
            frostyModExecutor.ForceRebuildOfMods = true;
            //frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, "",
            //    new System.Collections.Generic.List<string>() {
            //        "test.fbmod"
            //    }.ToArray()).Wait();

            frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { "test.fbmod" }.ToArray()).Wait();

        }

        public void LogProgress(int progress)
        {
            throw new NotImplementedException();
        }
    }
}

