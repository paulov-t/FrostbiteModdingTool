using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SdkGenerator;
using System;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class OtherStuffMethods
    {
        [TestMethod]
        public void DumpFrostyProfiles()
        {
            ProfileManager.DumpFrostyProfile("FIFA20");
        }

        [TestMethod]
        public void DumpFrostyProfilesAll()
        {
            ProfileManager.DumpAllFrostyProfiles();
        }


        [TestMethod]
        public void MemoryTestTOCFile()
        {
            var tocFile = new TOCFile();
            var weakReference = new WeakReference(tocFile);

            tocFile = null;
            // Ryn an operation with leakyObject
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.IsFalse(weakReference.IsAlive);
        }


        [TestMethod]
        public void BuildSDK()
        {
            var buildSDK = new BuildSDK();
            buildSDK.Build().Wait();
        }

        [TestMethod]
        public void BuildSDKFromTempCs()
        {
            var buildSDK = new BuildSDK();
            buildSDK.BuildSdkFromTempCS("G:\\Work\\FIFA Modding\\temp.cs");
        }
    }
}
