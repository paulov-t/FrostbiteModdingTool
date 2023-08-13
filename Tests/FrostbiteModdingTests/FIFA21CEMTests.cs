//using CareerExpansionMod.CEM;
//using CareerExpansionMod.CEM.FIFA;
//using FrostbiteModdingUI.CEM;
using FMT.Logging;
using FrostySdk.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
//using v2k4FIFAModdingCL.MemHack.Core;

namespace FrostbiteModdingTests
{
    [TestClass]
    public class FIFA21CEMTests : ILogger
    {
        public const string GamePath = @"F:\Origin Games\FIFA 21";
        public const string GamePathEXE = @"F:\Origin Games\FIFA 21\FIFA21.exe";

        public void Log(string text, params object[] vars)
        {
            Debug.WriteLine(text);
        }

        public void LogError(string text, params object[] vars)
        {
            Debug.WriteLine(text);
        }

        public void LogWarning(string text, params object[] vars)
        {
            Debug.WriteLine(text);
        }

        //[TestMethod]
        //public void LoadStatsFromLatestCareerSave()
        //{
        //    GameInstanceSingleton.InitializeSingleton(GamePathEXE);

        //    var cem = new CEMCore2("FIFA21");
        //    var ps = cem.GetPlayerStats();
        //    using (var nw = new NativeWriter(new FileStream("_TestExportCSV.csv", FileMode.Create), wide: true))
        //    {
        //        nw.WriteLine("Player Id,Player Name,Season Year,Competition,Appereances,Goals,Assists,Clean Sheets,Average Rating,Minutes Per Game,OVR,Growth");
        //        for (var i = 0; i < ps.Count; i++)
        //        {
        //            nw.WriteLine(
        //                ps[i].PlayerId
        //                + "," + ps[i].PlayerName
        //                + "," + ps[i].SeasonYear
        //                + "," + ps[i].CompName
        //                + "," + ps[i].Apps
        //                + "," + ps[i].Goals
        //                + "," + ps[i].Assists
        //                + "," + ps[i].CleanSheets
        //                + "," + ps[i].AverageRating
        //                + "," + ps[i].MinutesPerGame
        //                + "," + ps[i].OVR
        //                + "," + ps[i].OVRGrowth
        //                );
        //        }
        //    }
        //}

        //public void SaveStatsToCSV(List<FIFAPlayerStat> ps)
        //{
        //    using (var nw = new NativeWriter(new FileStream("_TestExportCSV.csv", FileMode.Create), wide: true))
        //    {
        //        nw.WriteLine("Player Id,Player Name,Season Year,Competition,Appereances,Goals,Assists,Clean Sheets,Average Rating,Minutes Per Game,OVR,Growth");
        //        for (var i = 0; i < ps.Count; i++)
        //        {
        //            nw.WriteLine(
        //                ps[i].PlayerId
        //                + "," + ps[i].PlayerName
        //                + "," + ps[i].SeasonYear
        //                + "," + ps[i].CompName
        //                + "," + ps[i].Apps
        //                + "," + ps[i].Goals
        //                + "," + ps[i].Assists
        //                + "," + ps[i].CleanSheets
        //                + "," + ps[i].AverageRating
        //                + "," + ps[i].MinutesPerGame
        //                + "," + ps[i].OVR
        //                + "," + ps[i].OVRGrowth
        //                );
        //        }
        //    }
        //}

        //[TestMethod]
        //public void LoadUserFinancesFromLatestCareerSave()
        //{
        //    GameInstanceSingleton.InitializeSingleton(GamePathEXE);

        //    var cem = new CEMCore2("FIFA21");
        //    var uf = cem.GetUserFinances().Result;
        //}

        //[TestMethod]
        //public void SaveUserFinancesFromLatestCareerSave()
        //{
        //    GameInstanceSingleton.InitializeSingleton(GamePathEXE);

        //    var cem = new CEMCore2("FIFA21");
        //    var uf = cem.GetUserFinances().Result;
        //    uf.TransferBudget = 999999999;
        //    cem.UpdateUserFinancesInFile();
        //}

        //[TestMethod]
        //public void LoadStatsFromJaysArsenalSave()
        //{
        //    var cem = new CEMCore2("FIFA21");
        //    var newFile = CEMCore2.SetupCareerFile(@"C:\Users\paula\Documents\FIFA 21\settings\Career20210511171702");
        //    var ps = cem.GetPlayerStats();
        //}

        //[TestMethod]
        //public void LoadStatsFromParadoxSchalkeSave()
        //{
        //    var cem = new CEMCore2("FIFA21");
        //    var newFile = CEMCore2.SetupCareerFile(@"C:\Users\paula\Downloads\Career20210609205742_ParadoxSchalke");
        //    var ps = cem.GetPlayerStats();
        //    SaveStatsToCSV(ps);
        //}

        //[TestMethod]
        //public void LoadStatsFromCEMTest()
        //{
        //    var cem = new CEMCore2("FIFA21");
        //    var newFile = CEMCore2.SetupCareerFile(@"C:\Users\paula\Documents\FIFA 21\settings\Career20210514142807");
        //    var ps = cem.GetPlayerStats();
        //}

        //[TestMethod]
        //public void LoadStatsFromSelectedCareer()
        //{
        //    var cem = new CEMCore2("FIFA21");
        //    OpenFileDialog openFileDialog = new OpenFileDialog();
        //    var result = openFileDialog.ShowDialog();
        //    if (result.HasValue && result.Value)
        //    {
        //        var newFile = CEMCore2.SetupCareerFile(openFileDialog.FileName);
        //        var ps = cem.GetPlayerStats();
        //    }
        //}

        public byte[] HexStringToByte(string param1, string param2)
        {
            return new byte[] {
                Convert.ToByte("0x" + param1.Substring(6, 2))
                , Convert.ToByte("0x" + param1.Substring(4, 2))
                , Convert.ToByte("0x" + param1.Substring(2, 2))
                , Convert.ToByte("0x" + param1.Substring(0, 2))
                , Convert.ToByte("0x" + param2.Substring(6, 2))
                , Convert.ToByte("0x" + param2.Substring(4, 2))
                , Convert.ToByte("0x" + param2.Substring(2, 2))
                , Convert.ToByte("0x" + param2.Substring(0, 2))
            };
        }

        public string FlipHexString(string innerHex)
        {
            return innerHex.Substring(6, 2) + innerHex.Substring(4, 2) + innerHex.Substring(2, 2) + innerHex.Substring(0, 2);
        }

        public string HexStringLittleEndian(int number)
        {
            return FlipHexString(number.ToString("X8"));
        }

        public void LogProgress(int progress)
        {
            throw new NotImplementedException();
        }
    }
}
