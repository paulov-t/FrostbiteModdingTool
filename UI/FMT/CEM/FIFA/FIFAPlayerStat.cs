// Created with ReClass.NET 1.2 by KN4CK3R

// Warning: The C# code generator doesn't support all node types!

using CareerExpansionMod.CEM.FIFA;
using FMT.FileTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
// optional namespace, only for vectors
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using v2k4FIFAModding.Career.CME.FIFA;
using v2k4FIFAModdingCL;

/// <summary>
/// This must have properties (not fields) for Json to work!
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class FIFAPlayerStat
{
    public int TeamId { get; set; } //0x0000
    public int PlayerId { get; set; } //0x0004
    public byte Competition { get; set; } //0x0008
    public byte ID { get; set; } //0x0009
    public short MinutesPlayed { get; set; } //0x000A

    public int Rating { get; set; } //0x000C
    public int Goals { get; set; } //0x000E -- is byte really
    public byte Appereances { get; set; } //0x0012

    public byte Apps { get { return Appereances; } }

    public byte Assists { get; set; } //0x0013
                                      //public byte N000000AF { get; set; } //0x0014
                                      //public byte N000000AB { get; set; } //0x0015
                                      //public byte N000000B5 { get; set; } //0x0016
    public byte CleanSheets { get; set; } //0x0017
    public int GameDate1 { get; private set; }

    //public int ContractedUntil { get; set; } //0x0018
    //public byte N000000B9 { get; set; } //0x001C
    //public byte N000000BF { get; set; } //0x001D
    //public byte N000000BE { get; set; } //0x001E
    //public byte N000000BC { get; set; } //0x001F
    //public byte N0000008A { get; set; } //0x0020
    //public byte N000000CA { get; set; } //0x0021
    //public byte N000000CE { get; set; } //0x0022
    //public byte N000000D1 { get; set; } //0x0023
    //public byte N000000CF { get; set; } //0x0024
    //public byte N000000CB { get; set; } //0x0025
    //public byte N000000D4 { get; set; } //0x0026
    //public byte N000000CC { get; set; } //0x0027
    //public byte N0000008B { get; set; } //0x0028
    //public byte N000000D7 { get; set; } //0x0029
    //public byte N000000ED { get; set; } //0x002A
    //public byte N0000010B { get; set; } //0x002B
    //public byte N000000EE { get; set; } //0x002C
    //public byte N000000D8 { get; set; } //0x002D
    //public byte N000000F0 { get; set; } //0x002E
    //public byte N000000D9 { get; set; } //0x002F
    //public byte N0000008C { get; set; } //0x0030
    //public byte N000000DB { get; set; } //0x0031
    //public byte N000000F3 { get; set; } //0x0032
    //public byte N0000010F { get; set; } //0x0033
    //public byte N000000F4 { get; set; } //0x0034
    //public byte N000000DC { get; set; } //0x0035
    //public byte N000000F6 { get; set; } //0x0036
    //public byte N000000DD { get; set; } //0x0037
    //public byte N0000008D { get; set; } //0x0038
    //public byte N000000DF { get; set; } //0x0039
    //public byte N000000F9 { get; set; } //0x003A
    //public byte N00000113 { get; set; } //0x003B
    //public byte N000000FA { get; set; } //0x003C
    //public byte N000000E0 { get; set; } //0x003D
    //public byte N000000FC { get; set; } //0x003E
    //public byte N000000E1 { get; set; } //0x003F
    //public byte N0000008E { get; set; } //0x0040
    //public byte N000000E3 { get; set; } //0x0041
    //public byte N000000FF { get; set; } //0x0042
    //public byte N00000117 { get; set; } //0x0043
    //public byte N00000100 { get; set; } //0x0044
    //public byte N000000E4 { get; set; } //0x0045
    //public byte N00000102 { get; set; } //0x0046
    //public byte N000000E5 { get; set; } //0x0047
    //public byte N0000008F { get; set; } //0x0048
    //public byte N000000E7 { get; set; } //0x0049
    //public byte N00000105 { get; set; } //0x004A
    //public byte N0000011B { get; set; } //0x004B
    //public byte N00000106 { get; set; } //0x004C
    //public byte N000000E8 { get; set; } //0x004D
    //public byte N00000108 { get; set; } //0x004E
    //public byte N000000E9 { get; set; } //0x004F
    //public byte N00000090 { get; set; } //0x0050
    //public byte N00000124 { get; set; } //0x0051
    //public byte N00000128 { get; set; } //0x0052
    //public byte N0000012B { get; set; } //0x0053
    //public byte N00000129 { get; set; } //0x0054
    //public byte N00000125 { get; set; } //0x0055
    //public byte N0000012F { get; set; } //0x0056
    //public byte N00000126 { get; set; } //0x0057
    public byte Unk1 { get; set; }
    public byte Unk2 { get; set; }
    public byte Unk3 { get; set; }
    public byte Unk4 { get; set; }
    public byte Unk5 { get; set; }
    public byte Unk6 { get; set; }
    public byte GD1_ID { get; set; }
    public byte GD1_B2 { get; private set; }
    public byte GD1_B3 { get; private set; }
    public byte GD1_B4 { get; private set; }
    public byte GD1_B5 { get; private set; }
    public byte GD1_B6 { get; private set; }
    public byte GD1_B7 { get; private set; }
    public byte GD1_B8 { get; private set; }
    public byte AvgMinutesPlayed2 { get; set; }
    public byte Unk9 { get; set; }
    public ushort TotalPasses { get; set; }
    public ushort TotalShots { get; set; }
    public ushort TotalTackles { get; set; }

    private double ar;
    public double AverageRating
    {
        get
        {
            if (Rating > 100)
            {
                ar = Math.Round(((double)Rating / Appereances) / 10, 2);
            }

            else if (Rating > 0)
            {
                ar = Math.Round((double)Rating / 10, 2);
            }
            return ar;
        }
        set
        {
            ar = value;
        }
    }


    private int? _seasonYear;
    // Seasonal Year -- Calculated at Runtime
    public int? SeasonYear
    {
        get
        {
            if (_seasonYear.HasValue)
            {
                return _seasonYear.Value;
            }


            var ingamedate = new DateTime(2019, 1, 1);
            switch (GameInstanceSingleton.Instance.GAMEVERSION)
            {
                case "FIFA20":
                    break;
                case "FIFA21":
                    ingamedate = ingamedate.AddYears(1);
                    break;
                case "FIFA22":
                    ingamedate = ingamedate.AddYears(2);
                    break;
                case "FIFA23":
                    ingamedate = ingamedate.AddYears(3);
                    break;
            }

            //if (CEMCore.CEMCoreInstance != null && CEMCore.CEMCoreInstance.CoreHack != null && CEMCore.CEMCoreInstance.CoreHack.GetInGameDate().HasValue)
            ////if (CEMCore.CEMCoreInstance != null && CEMCore.CEMCoreInstance.CoreHack != null)
            //	ingamedate = CEMCore.CEMCoreInstance.CoreHack.GetInGameDate().Value;

            //if (CEMCore2.CEMCoreInstance != null && CareerDB1.FIFAUser != null && CareerDB1.FIFAUser.seasoncount > 0)
            //	ingamedate = ingamedate.AddYears(CareerDB1.FIFAUser.seasoncount - 1);

            //else
            //{
            //	CEMCore.CEMCoreInstance = new CEMCore(new CoreHack());
            //	var d = CEMCore.CEMCoreInstance.CoreHack.GetInGameDate();
            //	if (d.HasValue)
            //		ingamedate = d.Value;
            //}
            // Very rigid this and FIXME

            //if (ingamedate.Month >= 1 && ingamedate.Month <= 6)
            //	_seasonYear = ingamedate.Year - 1;
            //else
            _seasonYear = ingamedate.Year;

            return _seasonYear;
        }
        set
        {
            _seasonYear = value;
        }
    }

    private int calenderYear;

    public int CalenderYear
    {
        get
        {

            calenderYear = 2019;
            //if (CEMCore.CEMCoreInstance != null && CEMCore.CEMCoreInstance.CoreHack != null && CEMCore.CEMCoreInstance.CoreHack.GetInGameDate().HasValue)
            //	//if (CEMCore.CEMCoreInstance != null && CEMCore.CEMCoreInstance.CoreHack != null)
            //	calenderYear = CEMCore.CEMCoreInstance.CoreHack.GetInGameDate().Value.Year;

            //if (CEMCore2.CEMCoreInstance != null && CareerDB1.FIFAUser != null && CareerDB1.FIFAUser.seasoncount > 0)
            //	calenderYear = 2019 + CareerDB1.FIFAUser.seasoncount - 1;


            return calenderYear;
        }
        set { calenderYear = value; }
    }

    /// <summary>
    /// Generated Elsewhere
    /// </summary>
    public int OVR { get; set; }
    /// <summary>
    /// Generated Elsewhere
    /// </summary>
    public int OVRGrowth { get; set; }

    public string PlayerName
    {
        get
        {
            var pn = string.Empty;
            //if(CEMCore.CEMCoreInstance != null)
            //         {
            if (
                //CEMCore.CEMCoreInstance.CareerFile != null
                //&& 
                CareerDB1.Current != null
                && CareerDB2.Current != null
                && CareerDB1.UserTeam.GetPlayers().Count > 0
                && PlayerId > 0)
            {

                var userPlayers = CareerDB1.UserPlayers;
                var player = userPlayers.FirstOrDefault(z => z.playerid == PlayerId);
                return player != null ? player.Name : null;
                //CareerDB2.Current.players.Where(x=>x.ItemArray["firstname"])

            }
            //}

            return pn;
        }
    }


    public double GoalsPerGame
    {
        get
        {
            if (Goals > 0 && Appereances > 0)
            {
                return Math.Round(Goals / (double)Appereances, 2);
            }

            return 0;
        }
    }

    public double AssistsPerGame
    {
        get
        {
            if (Assists > 0 && Appereances > 0)
            {
                return Math.Round(Assists / (double)Appereances, 2);
            }

            return 0;
        }
    }

    public double MinutesPerGame
    {
        get
        {
            if (MinutesPlayed > 0 && Appereances > 0)
            {
                return Math.Round(MinutesPlayed / (double)Appereances, 2);
            }

            return 0;
        }
    }

    private static Dictionary<int, string> CompetitionNameToIds
    {
        get
        {
            Dictionary<int, string> objs = new Dictionary<int, string>();
            objs.Add(10, "Premier League");
            objs.Add(26, "Premier League");

            return objs;
        }
    }
    public string CompName
    {
        get
        {
            switch (Competition)
            {
                case 10:
                case 26:
                    return "Premier League";
                case 13:
                    return "EFL Championship";
                case 16:
                    return "EFL League 1";
                case 19:
                case 35:
                case 45:
                    return "EFL League 2";
                case 54:
                    return "European Int'l Cup";
                case 55:
                    return "La Liga";
                case 61:
                    return "Bundesliga";
                case 80:
                case 88:
                //return "Carabao Cup";
                case 106:
                    return "English League Cup";
                case 108:
                    return "Champions League";
                case 125:
                    return "DFB-Pokal";
                case 140:
                case 148:
                    return "FA Cup";
                case 145:
                    return "Community Shield";
                case 191:
                    return "Bundesliga";
                case 192:
                    return "Europa League";
                case 194:
                    return "EFL Trophy";
                case 203:
                    return "Supercopa";
                default:
                    return null;
            }
        }
    }



    public static string ReverseString(string originalString)
    {
        string reverseString = string.Empty;
        foreach (char c in originalString)
        {
            reverseString = c + reverseString;
        }
        return reverseString;
    }

    /// <summary>
    /// A dictionary of PlayerIds and list of addresses for stats
    /// </summary>
    private static Dictionary<int, List<long>> PlayerIdToAddresses;

    //FF 12 00 00 00 8E 15 03 00 ?? ?? ?? ?? ?? ?? ??
    //public static IEnumerable<FIFAPlayerStat> GetPlayerStats(int PlayerId, int TeamId, int? CompId = null)
    //   {
    //	//if (PlayerIdToAddresses == null)
    //	//	PlayerIdToAddresses = new Dictionary<int, List<long>>();


    //		List<long> addresses;

    //		//if (!PlayerIdToAddresses.ContainsKey(PlayerId))
    //		//{
    //			var tidX8 = TeamId.ToString("X8");
    //			tidX8 = tidX8.Substring(6, 2) + " " + tidX8.Substring(4, 2) + " " + tidX8.Substring(2, 2) + " " + tidX8.Substring(0, 2);

    //			//if (tidX8.Length >= 4) 
    //			//{
    //			//	tidX8 = ReverseString(TeamId.ToString("X4"));
    //			//}
    //			//else
    //			//	tidX8 = TeamId.ToString("X2");
    //			var pidX8 = PlayerId.ToString("X8");
    //			pidX8 = pidX8.Substring(6, 2) + " " + pidX8.Substring(4, 2) + " " + pidX8.Substring(2, 2) + " " + pidX8.Substring(0, 2);
    //			//var addr = CoreHack.AOBScan("FF " + tidX8 + " 00 00 00 " + pidX8 + " ?? ?? ?? ?? ?? ?? ??", "PLAYER_STAT");
    //			//var addr = CoreHack.AOBScan("FF " + tidX8 + " 00 00 00 8E 15 03 00", "PLAYER_STAT");

    //			//"A8 07 00 00 03 B8 03 00 ? ? ? 00"
    //			//var aob = "{teamid} {playerid} ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? {teamid}";
    //			//var aob = "{teamid} {playerid} ? ? ? 00";
    //			var aob = "{teamid} {playerid} ? ? ? ? ? ? ? ? ? ? ? ? ? 00 ? ? ? ? ? ? ? ? ? ? ? ? FF";
    //			// 83 07 00 00 D9 E8 03 00 ? ? ? ? ? ? ? ? ? ? ? ? ? 00 ? ? ? ? ? ? ? ? ? ? ? ? FF

    //			if (CompId.HasValue)
    //			{
    //				var cidX8 = CompId.Value.ToString("X2");

    //				//var aob = cidX8 + " " + tidX8 + " 00 00 00 " + pidX8;
    //				aob = aob.Replace("{teamid}", tidX8);
    //				aob = aob.Replace("{playerid}", pidX8);
    //				Debug.WriteLine("DEBUG. " + aob);

    //				addresses = CoreHack.AOBScanList(aob, "PLAYER_STAT");
    //			}
    //			else
    //			{
    //				//var aob = "?? " + tidX8 + " 00 00 00 " + pidX8;
    //				aob = aob.Replace("{teamid}", tidX8);
    //				aob = aob.Replace("{playerid}", pidX8);
    //				Debug.WriteLine("DEBUG. " + aob);

    //				addresses = CoreHack.AOBScanList(aob, "PLAYER_STAT");
    //			}
    //		//}
    //		//else
    //  //         {
    //		//	addresses = PlayerIdToAddresses[PlayerId];
    //		//}

    //		if (addresses.Count > 0)
    //		{
    //			foreach (var addr in addresses)
    //			{
    //				yield return ConvertAddressToStats(addr);
    //			}
    //		}
    //}

    /// <summary>
    /// Works pretty quick but can be unreliable
    /// </summary>
    /// <param name="TeamId"></param>
    /// <returns></returns>
    //public static IEnumerable<FIFAPlayerStat> GetTeamPlayerStats(int TeamId)
    //   {
    //	List<long> addresses;

    //	var tidX8 = TeamId.ToString("X8");
    //	tidX8 = tidX8.Substring(6, 2) + " " + tidX8.Substring(4, 2) + " " + tidX8.Substring(2, 2) + " " + tidX8.Substring(0, 2);

    //	// missing league games
    //	//var aob = "{teamid} ? ? ? 00 ? ? ? ? ? ? ? ? ? ? ? ? ? 00 ? ? ? ? ? 00 ? ? ? 00 ? ? FF ?";
    //	var aob = "{teamid} ? ? ? 00 ? ? ? ? ? ? ? ? ? ? ? ? ? 00 ? ? ? ? ? 00 ? ? ? ? ? ? FF";
    //	aob = aob.Replace("{teamid}", tidX8);

    //	addresses = CoreHack.AOBScanList(aob, "TEAM_PLAYER_STATS_" + TeamId.ToString());
    //	if (addresses.Count > 9)
    //	{
    //		Debug.WriteLine("Scanned " + aob + " and found " + addresses.Count + " addresses");
    //           addresses = addresses.Skip(9).ToList();
    //           //addresses = addresses.ToList();

    //           foreach (var addr in addresses)
    //           {
    //               yield return ConvertAddressToStats(addr);
    //           }
    //       }

    //	//yield return null;
    //}


    /// <summary>
    /// This is incredibly slow!!!
    /// </summary>
    /// <param name="TeamId"></param>
    /// <returns></returns>
    //public static IEnumerable<FIFAPlayerStat> GetTeamPlayerStatsByCareerSave(int TeamId)
    //{
    //	var Team = FIFATeam.GetFIFATeams().FirstOrDefault(z => z.teamid == TeamId);
    //	var ListOfStats = new List<FIFAPlayerStat>(1000);
    //	foreach(var p in Team.GetPlayers())
    //       {
    //		foreach(var ps in GetPlayerStats(p.playerid, TeamId))
    //           {
    //			yield return ps;
    //           }

    //	}
    //}

    public static FIFAPlayerStat ConvertBytesToStats(byte[] bytes)
    {
        using (NativeReader nr = new NativeReader(new MemoryStream(bytes)))
        {
            FIFAPlayerStat playerStat = new FIFAPlayerStat();
            playerStat.TeamId = nr.ReadInt();
            playerStat.PlayerId = nr.ReadInt();
            playerStat.Competition = nr.ReadByte();
            playerStat.ID = nr.ReadByte();
            playerStat.MinutesPlayed = nr.ReadShort();
            playerStat.Rating = nr.ReadShort();
            playerStat.Goals = nr.ReadByte();
            playerStat.Unk1 = nr.ReadByte();
            playerStat.Unk2 = nr.ReadByte();
            playerStat.Unk3 = nr.ReadByte();
            playerStat.Appereances = nr.ReadByte();
            playerStat.Assists = nr.ReadByte();
            playerStat.Unk4 = nr.ReadByte();
            playerStat.Unk5 = nr.ReadByte();
            playerStat.Unk6 = nr.ReadByte();
            playerStat.CleanSheets = nr.ReadByte();
            playerStat.GameDate1 = nr.ReadInt(); // some kind of date
            playerStat.GD1_ID = nr.ReadByte();
            playerStat.GD1_B2 = nr.ReadByte();
            playerStat.GD1_B3 = nr.ReadByte();
            playerStat.GD1_B4 = nr.ReadByte();
            playerStat.GD1_B5 = nr.ReadByte();
            playerStat.GD1_B6 = nr.ReadByte();
            playerStat.GD1_B7 = nr.ReadByte();
            playerStat.GD1_B8 = nr.ReadByte();
            playerStat.GameDate2 = nr.ReadInt();
            playerStat.GD2_ID = nr.ReadByte();
            var unk16 = nr.ReadByte();
            playerStat.TotalPasses = nr.ReadUShort();
            //var unk18 = nr.ReadByte();
            var unk19 = nr.ReadByte();
            var unk20 = nr.ReadByte();
            var unk21 = nr.ReadByte();
            var unk22 = nr.ReadByte();
            playerStat.GameDate3 = nr.ReadInt();
            playerStat.GD3_ID = nr.ReadByte();
            var unk28 = nr.ReadByte();
            var unk29 = nr.ReadByte();
            var unk30 = nr.ReadByte();
            var unk31 = nr.ReadByte();
            var unk32 = nr.ReadByte();
            var unk33 = nr.ReadByte();
            var unk34 = nr.ReadByte();
            playerStat.GameDate4 = nr.ReadInt();
            var unk39 = nr.ReadByte();
            var unk40 = nr.ReadByte();
            var unk41 = nr.ReadByte();
            var unk42 = nr.ReadByte();
            var unk43 = nr.ReadByte();
            var unk44 = nr.ReadByte();
            var unk45 = nr.ReadByte();
            var unk46 = nr.ReadByte();
            playerStat.GameDate5 = nr.ReadInt();
            var unk47 = nr.ReadShort();
            playerStat.TotalShots = nr.ReadUShort();
            var unk48 = nr.ReadShort();
            var unk49 = nr.ReadShort();
            var unk50 = nr.ReadByte();
            var unk51 = nr.ReadByte();
            var unk52 = nr.ReadByte();


            //if (playerStat.PlayerId == 258406)
            //         {
            //	using(NativeWriter nwTest = new NativeWriter(new FileStream("playerStatDebug.dat", FileMode.OpenOrCreate)))
            //             {
            //		nwTest.Write(bytes);
            //             }
            //         }

            if (playerStat.PlayerId > 0 && playerStat.CompName != null)
            {

                var savedplayerstats = FIFAPlayerStat.Load();
                var indexofstat = savedplayerstats.FindIndex(x => x.PlayerId == playerStat.PlayerId && x.SeasonYear == playerStat.SeasonYear && x.CompName == playerStat.CompName);

                if (indexofstat == -1)
                    savedplayerstats.Add(playerStat);
                else
                    savedplayerstats[indexofstat] = playerStat;

                Save(savedplayerstats);

                return playerStat;
            }
            else
            {
                Debug.WriteLine($"Unable to process player {playerStat.PlayerId} for competition id {playerStat.Competition} who played {playerStat.Appereances} games");
            }

            return null;
        }
    }

    //public static FIFAPlayerStat ConvertAddressToStats(long addr)
    //   {
    //	//Debug.WriteLine("GetPlayerStat::Resolved Address::" + addr.ToString("X8"));
    //	var app = Convert.ToByte(CoreHack.MemLib.readByte(CoreHack.ResolveOffset(addr, 0x12).ToString("X8")));
    //	if (app > 0)
    //	{
    //		FIFAPlayerStat playerStat = new FIFAPlayerStat()
    //		{
    //			TeamId = CoreHack.MemLib.readInt(CoreHack.ResolveOffset(addr, 0x0).ToString("X8")),
    //			PlayerId = CoreHack.MemLib.readInt(CoreHack.ResolveOffset(addr, 0x4).ToString("X8")),
    //			Competition = Convert.ToByte(CoreHack.MemLib.readByte(CoreHack.ResolveOffset(addr, 0x8).ToString("X8"))),
    //			ID = Convert.ToByte(CoreHack.MemLib.readByte(CoreHack.ResolveOffset(addr, 0x9).ToString("X8"))),
    //			MinutesPlayed = BitConverter.ToInt16(CoreHack.MemLib.readBytes(CoreHack.ResolveOffset(addr, 0xA).ToString("X8"),2)),
    //			Rating = BitConverter.ToInt16(CoreHack.MemLib.readBytes(CoreHack.ResolveOffset(addr, 0xC).ToString("X8"),2)),
    //			Goals = Convert.ToByte(CoreHack.MemLib.readByte(CoreHack.ResolveOffset(addr, 0xE).ToString("X8"))),
    //			Appereances = Convert.ToByte(CoreHack.MemLib.readByte(CoreHack.ResolveOffset(addr, 0x12).ToString("X8"))),
    //			Assists = Convert.ToByte(CoreHack.MemLib.readByte(CoreHack.ResolveOffset(addr, 0x13).ToString("X8"))),
    //			CleanSheets = Convert.ToByte(CoreHack.MemLib.readByte(CoreHack.ResolveOffset(addr, 0x17).ToString("X8"))),
    //		};

    //		if (playerStat.PlayerId > 0)
    //		{

    //			var savedplayerstats = FIFAPlayerStat.Load();
    //			var indexofstat = savedplayerstats.FindIndex(x => x.PlayerId == playerStat.PlayerId && x.SeasonYear == playerStat.SeasonYear && x.CompName == playerStat.CompName);

    //			//playerStat.SeasonYear = new CoreHack().GameDate.Value;

    //			if (indexofstat == -1)
    //				savedplayerstats.Add(playerStat);
    //			else
    //				savedplayerstats[indexofstat] = playerStat;

    //			Save(savedplayerstats);

    //			return playerStat;
    //		}
    //	}
    //	return null;
    //}

    //public static IEnumerable<FIFAPlayerStat> GetPlayerSeasonalStats(int PlayerId, int TeamId, int? CompId = null)
    //{
    //	// Force a load of the latest stats to update the seasonals
    //	GetPlayerStats(PlayerId, TeamId);

    //	var seasonalStats = Load();
    //	seasonalStats = seasonalStats.Where(x => x.PlayerId == PlayerId).ToList();

    //	//return seasonalStats.Select(x => new { x.SeasonYear }).ToList();

    //	var results = (from a in seasonalStats
    //				   group a by a.SeasonYear into g
    //				   orderby g.Key
    //				   select new FIFAPlayerStat() { SeasonYear = g.Key.HasValue ? g.Key : null, Appereances = Convert.ToByte(g.Sum(x => x.Appereances)), Goals = g.Sum(x => x.Goals), Assists = Convert.ToByte(g.Sum(x => x.Assists)), CleanSheets = Convert.ToByte(g.Sum(x => x.CleanSheets)), AverageRating = Math.Round(g.Average(x => x.AverageRating), 2) }
    //				  ).ToList();
    //	return results;
    //}



    //public static string CEMPlayerStatsDirectory
    //{
    //	get
    //	{
    //		return CEMCore.CEMMyDocumentsDbSaveDirectory;

    //	}
    //}

    public static string CEMPlayerStatsSaveFile
    {
        get
        {
            //if (CEMCore.CEMCoreInstance == null)
            //{
            return "PlayerStats.json";
            //}
            //else
            //	return CEMCore.CEMMyDocumentsDbSaveDirectory + "\\PlayerStats.json";
        }
    }

    public int GameDate3 { get; private set; }
    public byte GD3_ID { get; private set; }
    public int GameDate4 { get; private set; }
    public int GameDate5 { get; private set; }
    public int GameDate2 { get; private set; }
    public byte GD2_ID { get; private set; }

    public static List<FIFAPlayerStat> FIFAPlayerStatsInstance;

    public static List<FIFAPlayerStat> Load()
    {
        if (File.Exists(CEMPlayerStatsSaveFile))
        {
            FIFAPlayerStatsInstance = JsonConvert.DeserializeObject<List<FIFAPlayerStat>>(File.ReadAllText(CEMPlayerStatsSaveFile));
        }
        else
        {
            FIFAPlayerStatsInstance = new List<FIFAPlayerStat>();
        }

        return FIFAPlayerStatsInstance;
    }

    public static void Save(List<FIFAPlayerStat> playerStats)
    {
        lock (FIFAPlayerStatsInstance)
        {
            File.WriteAllText(CEMPlayerStatsSaveFile, JsonConvert.SerializeObject(FIFAPlayerStatsInstance));
        }
    }
    public override bool Equals(object obj)
    {
        var other = obj as FIFAPlayerStat;
        if (ReferenceEquals(this, obj))
            return true;

        if (other != null)
        {
            if (other.PlayerId == this.PlayerId && other.SeasonYear == this.SeasonYear)
            {
                return true;
            }
        }
        return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        return PlayerId * 254;
    }
    public override string ToString()
    {
        return PlayerId.ToString();
    }
}