﻿using FrostySdk.Frostbite;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using v2k4FIFAModdingCL;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class EASFC24Tests : IFMTTest
    {
        public string GamePath => throw new NotImplementedException();

        public string GameName => throw new NotImplementedException();

        public string GameEXE => throw new NotImplementedException();

        public string GamePathEXE => "H:\\EA Games\\FC 24 Closed Beta\\FC24.exe";

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

    }
}
